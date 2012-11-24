/*
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

using Framework.Constants;
using Framework.Network.Packets;
using System;
using System.Linq;
using System.Text;
using Windows.Storage;

namespace Framework.Logging.PacketLogging
{
    public sealed class PacketLog
    {
        public static async void WritePacket(string clientInfo, PacketWriter serverPacket = null, PacketReader clientPacket = null)
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile logFile = await localFolder.CreateFileAsync("Packet.log", CreationCollisionOption.OpenIfExists);
            StringBuilder outPut = new StringBuilder();

            if (serverPacket != null)
            {
                outPut.AppendLine(String.Format("Client: {0}", clientInfo));

                if (Enum.IsDefined(typeof(LegacyMessage), serverPacket.Opcode))
                {
                    outPut.AppendLine("Type: LegacyMessage");
                    outPut.AppendLine(String.Format("Name: {0}", Enum.GetName(typeof(LegacyMessage), serverPacket.Opcode)));
                }
                else if (Enum.IsDefined(typeof(JAMCMessage), serverPacket.Opcode))
                {
                    outPut.AppendLine("Type: JAMCMessage");
                    outPut.AppendLine(String.Format("Name: {0}", Enum.GetName(typeof(JAMCMessage), serverPacket.Opcode)));
                }
                else if (Enum.IsDefined(typeof(Message), serverPacket.Opcode))
                {
                    outPut.AppendLine("Type: Message");
                    outPut.AppendLine(String.Format("Name: {0}", Enum.GetName(typeof(Message), serverPacket.Opcode)));
                }
                else
                {
                    outPut.AppendLine("Type: JAMCCMessage");
                    outPut.AppendLine(String.Format("Name: {0}", Enum.GetName(typeof(JAMCCMessage), serverPacket.Opcode)));
                }

                outPut.AppendLine(String.Format("Value: 0x{0:X} ({1})", serverPacket.Opcode, serverPacket.Opcode));
                outPut.AppendLine(String.Format("Length: {0}", serverPacket.Size - 2));

                outPut.AppendLine("|----------------------------------------------------------------|");
                outPut.AppendLine("| 00  01  02  03  04  05  06  07  08  09  0A  0B  0C  0D  0E  0F |");
                outPut.AppendLine("|----------------------------------------------------------------|");
                outPut.Append("|");

                if (serverPacket.Size - 2 != 0)
                {
                    var data = serverPacket.ReadDataToSend().ToList();
                    data.RemoveRange(0, 4);

                    byte count = 0;
                    foreach (var b in data)
                    {
                        if (b <= 0xF)
                            outPut.Append(String.Format(" 0{0:X} ", b));
                        else
                            outPut.Append(String.Format(" {0:X} ", b));

                        if (count == 15)
                        {
                            outPut.Append("|");
                            outPut.AppendLine();
                            outPut.Append("|");
                            count = 0;
                        }
                        else
                            count++;
                    }

                    outPut.AppendLine("");
                    outPut.AppendLine("|----------------------------------------------------------------|");
                }

                outPut.AppendLine("");
                outPut.AppendLine("");
            }

            if (clientPacket != null)
            {
                outPut.AppendLine(String.Format("Client: {0}", clientInfo));
                outPut.AppendLine("Type: ClientMessage");

                if (Enum.IsDefined(typeof(ClientMessage), clientPacket.Opcode))
                    outPut.AppendLine(String.Format("Name: {0}", clientPacket.Opcode));
                else
                    outPut.AppendLine(String.Format("Name: {0}", "Unknown"));

                outPut.AppendLine(String.Format("Value: 0x{0:X} ({1})", (ushort)clientPacket.Opcode, (ushort)clientPacket.Opcode));
                outPut.AppendLine(String.Format("Length: {0}", clientPacket.Size));

                outPut.AppendLine("|----------------------------------------------------------------|");
                outPut.AppendLine("| 00  01  02  03  04  05  06  07  08  09  0A  0B  0C  0D  0E  0F |");
                outPut.AppendLine("|----------------------------------------------------------------|");
                outPut.Append("|");

                if (clientPacket.Size - 2 != 0)
                {
                    var data = clientPacket.Storage.ToList();

                    byte count = 0;
                    foreach (var b in data)
                    {

                        if (b <= 0xF)
                            outPut.Append(String.Format(" 0{0:X} ", b));
                        else
                            outPut.Append(String.Format(" {0:X} ", b));

                        if (count == 15)
                        {
                            outPut.Append("|");
                            outPut.AppendLine();
                            outPut.Append("|");
                            count = 0;
                        }
                        else
                            count++;
                    }

                    outPut.AppendLine();
                    outPut.Append("|----------------------------------------------------------------|");
                }

                outPut.AppendLine("");
                outPut.AppendLine("");
            }

            await FileIO.AppendTextAsync(logFile, outPut.ToString());
        }
    }
}
