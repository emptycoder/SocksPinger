/*
    Socks5 - A full-fledged high-performance socks5 proxy server written in C#. Plugin support included.
    Copyright (C) 2016 ThrDev

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Socks5Wrap.Plugin;
using Socks5Wrap.TCP;

namespace Socks5Wrap.Socks
{
    class SocksTunnel
    {
        public SocksRequest Req;
        public SocksRequest ModifiedReq;

        public SocksClient Client;
        public Client RemoteClient;

        private List<DataHandler> _plugins = new List<DataHandler>();

        private int _timeout = 10000;
        private int _packetSize = 4096;

        public SocksTunnel(SocksClient p, SocksRequest req, SocksRequest req1, int packetSize, int timeout)
        {
            RemoteClient = new Client(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), _packetSize);
            Client = p;
            Req = req;
            ModifiedReq = req1;
            _packetSize = packetSize;
            _timeout = timeout;
        }

        SocketAsyncEventArgs _socketArgs;

        public void Open(IPAddress outbound)
        {
            if (ModifiedReq.Address == null || ModifiedReq.Port <= -1) { Client.Client.Disconnect(); return; }
#if DEBUG
            Console.WriteLine("{0}:{1}", ModifiedReq.Address, ModifiedReq.Port);
#endif
            foreach (ConnectSocketOverrideHandler conn in PluginLoader.LoadPlugin(typeof(ConnectSocketOverrideHandler)))
            if(conn.Enabled)
            {
                Client pm = conn.OnConnectOverride(ModifiedReq);
                if (pm != null)
                {
                    //check if it's connected.
                    if (pm.Sock.Connected)
                    {
                        RemoteClient = pm;
                        //send request right here.
                        byte[] shit = Req.GetData(true);
                        shit[1] = 0x00;
                        //gucci let's go.
                        Client.Client.Send(shit);
                        ConnectHandler(null);
                        return;
                    }
                }
            }
            if (ModifiedReq.Error != SocksError.Granted)
            {
                Client.Client.Send(Req.GetData(true));
                Client.Client.Disconnect();
                return;
            }
            _socketArgs = new SocketAsyncEventArgs { RemoteEndPoint = new IPEndPoint(ModifiedReq.Ip, ModifiedReq.Port) };
            _socketArgs.Completed += socketArgs_Completed;
            RemoteClient.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            RemoteClient.Sock.Bind(new IPEndPoint(outbound, 0));
            if (!RemoteClient.Sock.ConnectAsync(_socketArgs))
                ConnectHandler(_socketArgs);
        }

        void socketArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            
            byte[] request = Req.GetData(true); // Client.Client.Send(Req.GetData());
            if (e.SocketError != SocketError.Success)
            {
                Console.WriteLine("Error while connecting: {0}", e.SocketError.ToString());
                request[1] = (byte)SocksError.Unreachable;
            }
            else
            {
                request[1] = 0x00;
            }

            Client.Client.Send(request);

            if(_socketArgs != null)
            {
                _socketArgs.Completed -= socketArgs_Completed;
                _socketArgs.Dispose();
            }

            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    //connected;
                    ConnectHandler(e);
                    break;               
            }
        }

        private void ConnectHandler(SocketAsyncEventArgs e)
        {
            //start receiving from both endpoints.
            try
            {
                //all plugins get the event thrown.
                foreach (DataHandler data in PluginLoader.LoadPlugin(typeof(DataHandler)))
                    _plugins.Push(data);
                Client.Client.OnDataReceived += Client_onDataReceived;
                RemoteClient.OnDataReceived += RemoteClient_onDataReceived;
                RemoteClient.OnClientDisconnected += RemoteClient_onClientDisconnected;
                Client.Client.OnClientDisconnected += Client_onClientDisconnected;
                RemoteClient.ReceiveAsync();
                Client.Client.ReceiveAsync();
            }
            catch
            {
                RemoteClient.Disconnect();
                Client.Client.Disconnect();
            }
        }
        bool _disconnected;
        void Client_onClientDisconnected(object sender, ClientEventArgs e)
        {
            if (_disconnected) return;
            //Console.WriteLine("Client DC'd");
            _disconnected = true;
            RemoteClient.Disconnect();
            RemoteClient.OnDataReceived -= RemoteClient_onDataReceived;
            RemoteClient.OnClientDisconnected -= RemoteClient_onClientDisconnected;
        }

        void RemoteClient_onClientDisconnected(object sender, ClientEventArgs e)
        {
            
#if DEBUG
            Console.WriteLine("Remote DC'd");
#endif
            if (_disconnected) return;
            //Console.WriteLine("Remote DC'd");
            _disconnected = true;
            Client.Client.Disconnect();
            Client.Client.OnDataReceived -= Client_onDataReceived;
            Client.Client.OnClientDisconnected -= Client_onClientDisconnected;
        }

        void RemoteClient_onDataReceived(object sender, DataEventArgs e)
        {
            e.Request = ModifiedReq;
            foreach (DataHandler f in _plugins)
                f.OnServerDataReceived(this, e);
            Client.Client.Send(e.Buffer, e.Offset, e.Count);
            if (!RemoteClient.Receiving)
                RemoteClient.ReceiveAsync();
            if (!Client.Client.Receiving)
                Client.Client.ReceiveAsync();
        }

        void Client_onDataReceived(object sender, DataEventArgs e)
        {
            e.Request = ModifiedReq;
            foreach (DataHandler f in _plugins)
                f.OnClientDataReceived(this, e);
            
            RemoteClient.Send(e.Buffer, e.Offset, e.Count);
            if (!Client.Client.Receiving)
                Client.Client.ReceiveAsync();
            if (!RemoteClient.Receiving)
                RemoteClient.ReceiveAsync();
        }
    }
}
