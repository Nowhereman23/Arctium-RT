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

using Framework.Logging;
using System;
using Windows.Networking.Sockets;

namespace RealmServer.Network
{
    public class RealmNetwork
    {
        public bool IsWorking { get; set; }
        public volatile bool listenSocket = true;
        StreamSocketListener listener;

        public async void Start(string host, int port)
        {
            try
            {
                listener = new StreamSocketListener();
                listener.ConnectionReceived += listener_ConnectionReceived;
                await listener.BindServiceNameAsync(port.ToString());
                
                IsWorking = true;
            }
            catch (Exception e)
            {
                Log.Message(LogType.ERROR, "{0}", e.Message);
                Log.Message();
            }
        }

        void listener_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            while (true)
            {
                RealmClass realmClient = new RealmClass();
                realmClient.clientSocket = args.Socket;
                realmClient.streamArgs = args;

                realmClient.Recieve(null);
            }
        }
    }
}
