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
using Socks5Wrap.Encryption;
using Socks5Wrap.Plugin;
using Socks5Wrap.TCP;

namespace Socks5Wrap.Socks
{
    class SocksSpecialTunnel
    {
         public SocksRequest Req;
        public SocksRequest ModifiedReq;

        public SocksClient Client;
        public Client RemoteClient;

        private List<DataHandler> _plugins = new List<DataHandler>();

        private int _timeout = 10000;
        private int _packetSize = 4096;
        private SocksEncryption _se;

        public SocksSpecialTunnel(SocksClient p, SocksEncryption ph, SocksRequest req, SocksRequest req1, int packetSize, int timeout)
        {
            RemoteClient = new Client(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), _packetSize);
            Client = p;
            Req = req;
            ModifiedReq = req1;
            _packetSize = packetSize;
            _timeout = timeout;
            _se = ph; 
        }

        public void Open(IPAddress outboundIp)
        {
            if (ModifiedReq.Address == null || ModifiedReq.Port <= -1) { Client.Client.Disconnect(); return; }
#if DEBUG
            Console.WriteLine("{0}:{1}", ModifiedReq.Address, ModifiedReq.Port);
#endif
            foreach (ConnectSocketOverrideHandler conn in PluginLoader.LoadPlugin(typeof(ConnectSocketOverrideHandler)))
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
                        //process packet.
                        byte[] output = _se.ProcessOutputData(shit, 0, shit.Length);
                        //gucci let's go.
                        Client.Client.Send(output);
                        ConnectHandler(null);
                        return;
                    }
                }
            }
            var socketArgs = new SocketAsyncEventArgs { RemoteEndPoint = new IPEndPoint(ModifiedReq.Ip, ModifiedReq.Port) };
            socketArgs.Completed += socketArgs_Completed;
            RemoteClient.Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            RemoteClient.Sock.Bind(new IPEndPoint(outboundIp, 0));
            if (!RemoteClient.Sock.ConnectAsync(socketArgs))
                ConnectHandler(socketArgs);
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

            byte[] encreq = _se.ProcessOutputData(request, 0, request.Length);
            Client.Client.Send(encreq);

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
                Client.Client.Sock.ReceiveBufferSize = 4200;
                Client.Client.Sock.SendBufferSize = 4200;
                foreach (DataHandler data in PluginLoader.LoadPlugin(typeof(DataHandler)))
                    _plugins.Push(data);
                Client.Client.OnDataReceived += Client_onDataReceived;
                RemoteClient.OnDataReceived += RemoteClient_onDataReceived;
                RemoteClient.OnClientDisconnected += RemoteClient_onClientDisconnected;
                Client.Client.OnClientDisconnected += Client_onClientDisconnected;
                Client.Client.ReceiveAsync();
                RemoteClient.ReceiveAsync();
            }
            catch
            {
            }
        }
        bool _disconnected;
        void Client_onClientDisconnected(object sender, ClientEventArgs e)
        {
#if DEBUG
            Console.WriteLine("Client DC'd");
#endif
            if (_disconnected) return;
            _disconnected = true;
            RemoteClient.Disconnect();
        }

        void RemoteClient_onClientDisconnected(object sender, ClientEventArgs e)
        {
#if DEBUG
            Console.WriteLine("Remote DC'd");
#endif
           /* if (disconnected) return;
            disconnected = true;
            //Client.Client.Disconnect();
            disconnected = true;*/
        }

        void RemoteClient_onDataReceived(object sender, DataEventArgs e)
        {
            e.Request = ModifiedReq;
            try
            {
                foreach (DataHandler f in _plugins)
	                f.OnServerDataReceived(this, e);
                //craft headers & shit.
                byte[] outputdata = _se.ProcessOutputData(e.Buffer, e.Offset, e.Count);
                byte[] datatosend = new byte[outputdata.Length + 4];
                Buffer.BlockCopy(outputdata, 0, datatosend, 4, outputdata.Length);
                Buffer.BlockCopy(BitConverter.GetBytes(outputdata.Length), 0, datatosend, 0, 4);
                //send outputdata's length first.
                Client.Client.Send(datatosend);
                if(!RemoteClient.Receiving)
                    RemoteClient.ReceiveAsync();
                if (!Client.Client.Receiving)
                    Client.Client.ReceiveAsync();
            }
            catch
            {
                //Client.Client.Disconnect();
                RemoteClient.Disconnect();
            }
        }

        void Client_onDataReceived(object sender, DataEventArgs e)
        {
            e.Request = ModifiedReq;
            //this should be packet header.
            try
            {
                int packetsize = BitConverter.ToInt32(e.Buffer, 0);
                byte[] newbuff = new byte[packetsize];
                //yey
                //process packet.
                byte[] output = _se.ProcessInputData(e.Buffer, 4, packetsize);
                e.Buffer = null;
                e.Buffer = output;
                e.Offset = 0;
                e.Count = output.Length;
                //receive full packet.
                foreach (DataHandler f in _plugins)
                    f.OnClientDataReceived(this, e);
                RemoteClient.SendAsync(e.Buffer, e.Offset, e.Count);
                if (!Client.Client.Receiving)
                    Client.Client.ReceiveAsync();
                if (!RemoteClient.Receiving)
                    RemoteClient.ReceiveAsync();
            }
            catch
            {
                //disconnect.
                Client.Client.Disconnect();
                RemoteClient.Disconnect();
            }
        }
    }
}
