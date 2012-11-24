﻿/*
 * Copyright (C) 2012 Arctium <http://>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants.Authentication;
using Framework.Cryptography;
using Framework.Database;
using Framework.Logging;
using Framework.Network.Packets;
using Framework.ObjectDefines;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace RealmServer.Network
{
    public class RealmClass
    {
        public static Account account { get; set; }
        public static List<Realm> Realms = new List<Realm>();
        public static RealmNetwork realm;
        public SRP6 SecureRemotePassword { get; set; }
        public StreamSocket clientSocket;
        public StreamSocketListenerConnectionReceivedEventArgs streamArgs;
        byte[] DataBuffer;

        public RealmClass()
        {
            account = new Account();
            SecureRemotePassword = new SRP6();
        }

        void HandleRealmData(byte[] data)
        {
            PacketReader reader = new PacketReader(data, false);
            ClientLink cmd = (ClientLink)reader.ReadUInt8();

            switch (cmd)
            {
                case ClientLink.CMD_AUTH_LOGON_CHALLENGE:
                case ClientLink.CMD_AUTH_RECONNECT_CHALLENGE:
                    HandleAuthLogonChallenge(this, reader);
                    break;
                case ClientLink.CMD_AUTH_LOGON_PROOF:
                case ClientLink.CMD_AUTH_RECONNECT_PROOF:
                    HandleAuthLogonProof(this, reader);
                    break;
                case ClientLink.CMD_REALM_LIST:
                    HandleRealmList(this, reader);
                    break;
                default:
                    Log.Message(LogType.NORMAL, "Received unknown ClientLink: {0}", cmd);
                    break;
            }
        }

        public void HandleAuthLogonChallenge(RealmClass session, PacketReader data)
        {
            Log.Message(LogType.NORMAL, "AuthLogonChallenge");

            data.Skip(10);
            ushort ClientBuild = data.ReadUInt16();
            data.Skip(8);
            account.Language = data.ReadStringFromBytes(4);
            data.Skip(4);

            account.IP = data.ReadIPAddress();
            account.Name = data.ReadAccountName();

            SQLResult result = DB.Realms.Select("SELECT id, name, password, expansion, gmlevel, securityFlags FROM accounts WHERE name = '{0}'", account.Name);
            var res = result.Read("id", "name", "password", "expansion", "securityFlags");

            PacketWriter logonChallenge = new PacketWriter();
            logonChallenge.WriteUInt8((byte)ClientLink.CMD_AUTH_LOGON_CHALLENGE);
            logonChallenge.WriteUInt8(0);

            if (result.Count != 0)
            {
                account.Id = result.Read<Int32>("id");
                account.Expansion = result.Read<Byte>("expansion");
                account.SecurityFlags = result.Read<Byte>("securityFlags");

                DB.Realms.Execute("UPDATE accounts SET ip = '{0}', language = '{1}' WHERE id = {2}", account.IP, account.Language, account.Id);

                byte[] username = Encoding.UTF8.GetBytes(result.Read<String>("name").ToUpper());
                byte[] password = Encoding.UTF8.GetBytes(result.Read<String>("password").ToUpper());

                // WoW 5.1.0.16309 (5.1.0)
                if (ClientBuild == 16309)
                {
                    session.SecureRemotePassword.CalculateX(username, password);
                    byte[] buf = new byte[0x10];
                    SRP6.RAND_bytes(buf, 0x10);

                    logonChallenge.WriteUInt8((byte)AuthResults.WOW_SUCCESS);
                    logonChallenge.WriteBytes(session.SecureRemotePassword.B);
                    logonChallenge.WriteUInt8(1);
                    logonChallenge.WriteUInt8(session.SecureRemotePassword.g[0]);
                    logonChallenge.WriteUInt8(0x20);
                    logonChallenge.WriteBytes(session.SecureRemotePassword.N);
                    logonChallenge.WriteBytes(session.SecureRemotePassword.salt);
                    logonChallenge.WriteBytes(buf);

                    // Security flags
                    logonChallenge.WriteUInt8(account.SecurityFlags);

                    // Enable authenticator
                    if ((account.SecurityFlags & 4) != 0)
                        logonChallenge.WriteUInt8(1);
                }
            }
            else
                logonChallenge.WriteUInt8((byte)AuthResults.WOW_FAIL_UNKNOWN_ACCOUNT);

            session.Send(logonChallenge);
        }

        public void HandleAuthAuthenticator(RealmClass session, PacketReader data)
        {
            Log.Message(LogType.NORMAL, "AuthAuthenticator");
        }

        public void HandleAuthLogonProof(RealmClass session, PacketReader data)
        {
            Log.Message(LogType.NORMAL, "AuthLogonProof");

            PacketWriter logonProof = new PacketWriter();

            byte[] a = new byte[32];
            byte[] m1 = new byte[20];

            Array.Copy(DataBuffer, 1, a, 0, 32);
            Array.Copy(DataBuffer, 33, m1, 0, 20);

            session.SecureRemotePassword.CalculateU(a);
            session.SecureRemotePassword.CalculateM2(m1);
            session.SecureRemotePassword.CalculateK();

            foreach (var b in session.SecureRemotePassword.K)
                if (b < 0x10)
                    account.SessionKey += "0" + String.Format("{0:X}", b);
                else
                    account.SessionKey += String.Format("{0:X}", b);

            logonProof.WriteUInt8((byte)ClientLink.CMD_AUTH_LOGON_PROOF);
            logonProof.WriteUInt8(0);
            logonProof.WriteBytes(session.SecureRemotePassword.M2);
            logonProof.WriteUInt32(0x800000);
            logonProof.WriteUInt32(0);
            logonProof.WriteUInt16(0);

            DB.Realms.Execute("UPDATE accounts SET sessionkey = '{0}' WHERE id = {1}", account.SessionKey, account.Id);

            session.Send(logonProof);
        }

        public void HandleRealmList(RealmClass session, PacketReader data)
        {
            Log.Message(LogType.NORMAL, "RealmList");

            PacketWriter realmData = new PacketWriter();

            foreach (var r in Realms)
            {
                realmData.WriteUInt8(1);
                realmData.WriteUInt8(0);
                realmData.WriteUInt8(0);
                realmData.WriteCString(r.Name);
                realmData.WriteCString(r.IP + ":" + r.Port);
                realmData.WriteFloat(0);
                realmData.WriteUInt8(0);  // CharCount
                realmData.WriteUInt8(1);
                realmData.WriteUInt8(0x2C);
            }

            PacketWriter realmList = new PacketWriter();
            realmList.WriteUInt8((byte)ClientLink.CMD_REALM_LIST);
            realmList.WriteUInt16((ushort)(realmData.BaseStream.Length + 8));
            realmList.WriteUInt32(0);
            realmList.WriteUInt16((ushort)Realms.Count);
            realmList.WriteBytes(realmData.ReadDataToSend());
            realmList.WriteUInt8(0);
            realmList.WriteUInt8(0x10);

            session.Send(realmList);
        }

        public void Recieve(IAsyncAction operation)
        {
            while (realm.listenSocket)
            {
                Task.Delay(1);
                //if (clientSocket.Available > 0)
                {
                    DataReader reader = new DataReader(clientSocket.InputStream);
                    var size = reader.UnconsumedBufferLength;
                    DataBuffer = new byte[size];

                    var buff = reader.ReadBuffer(size);
                    CryptographicBuffer.CopyToByteArray(buff, out DataBuffer);

                    HandleRealmData(DataBuffer);
                }
            }

            clientSocket.Dispose();
        }

        public async void Send(PacketWriter packet)
        {
            DataBuffer = packet.ReadDataToSend(true);

            try
            {
                DataWriter writer = new DataWriter(clientSocket.OutputStream);
                writer.WriteBytes(DataBuffer);

                await writer.StoreAsync();
                packet.Flush();
            }
            catch (Exception ex)
            {
                Log.Message(LogType.ERROR, "{0}", ex.Message);
                Log.Message();

                clientSocket.Dispose();
            }
        }
    }
}
