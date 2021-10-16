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

using System.Collections.Generic;
using System.Net;
using System.Threading;
using Socks5Wrap.Plugin;
using Socks5Wrap.Socks;
using Socks5Wrap.TCP;

namespace Socks5Wrap.SocksServer
{
    public class Socks5Server
    {
        public int Timeout { get; set; }
        public int PacketSize { get; set; }
        public bool LoadPluginsFromDisk { get; set; }
        public IPAddress OutboundIpAddress { get; set; }

        private TcpServer _server;
        private Thread _networkStats;

        public List<SocksClient> Clients = new List<SocksClient>();
        public Stats Stats;

        private bool _started;

        public Socks5Server(IPAddress ip, int port)
        {
            Timeout = 5000;
            PacketSize = 4096;
            LoadPluginsFromDisk = false;
            Stats = new Stats();
            OutboundIpAddress = IPAddress.Any;
            _server = new TcpServer(ip, port);
            _server.OnClientConnected += _server_onClientConnected;
        }

        public void Start()
        {
            if (_started) return;
            PluginLoader.LoadPluginsFromDisk = LoadPluginsFromDisk;
            PluginLoader.LoadPlugins(); 
            _server.PacketSize = PacketSize;
            _server.Start();
            _started = true;
            //start thread.
            _networkStats = new Thread(new ThreadStart(delegate()
            {
                while (_started)
                {
                    if (Clients.Contains(null))
                        Clients.Remove(null);
                    Stats.ResetClients(Clients.Count);
                    Thread.Sleep(1000);
                }
            }));
            _networkStats.Start();
        }

        public void Stop()
        {
            if (!_started) return;
            _server.Stop();
            for (int i = 0; i < Clients.Count; i++)
            {
                Clients[i].Client.Disconnect();
            }
            Clients.Clear();
            _started = false;
        }

        void _server_onClientConnected(object sender, ClientEventArgs e)
        {
            //Console.WriteLine("Client connected.");
            //call plugins related to ClientConnectedHandler.
            foreach (ClientConnectedHandler cch in PluginLoader.LoadPlugin(typeof(ClientConnectedHandler)))
            {
				try
				{
					if (!cch.OnConnect(e.Client, (IPEndPoint)e.Client.Sock.RemoteEndPoint))
					{
						e.Client.Disconnect();
						return;
					}
				}
				catch
				{
				}
            }
            SocksClient client = new SocksClient(e.Client);
            e.Client.OnDataReceived += Client_onDataReceived;
            e.Client.OnDataSent += Client_onDataSent;
            client.OnClientDisconnected += client_onClientDisconnected;
            Clients.Add(client);
            client.Begin(OutboundIpAddress, PacketSize, Timeout);
        }

        void client_onClientDisconnected(object sender, SocksClientEventArgs e)
        {
            e.Client.OnClientDisconnected -= client_onClientDisconnected;
            e.Client.Client.OnDataReceived -= Client_onDataReceived;
            e.Client.Client.OnDataSent -= Client_onDataSent;
            Clients.Remove(e.Client);
            foreach (ClientDisconnectedHandler cdh in PluginLoader.LoadPlugin(typeof(ClientDisconnectedHandler)))
            {
				try
				{
					cdh.OnDisconnected(sender, e);
				}
				catch
				{
				}
            }
        }

        //All stats data is "Server" bandwidth stats, meaning clientside totals not counted.
        void Client_onDataSent(object sender, DataEventArgs e)
        {
            //Technically we are sending data from the remote server to the client, so it's being "received" 
            Stats.AddBytes(e.Count, ByteType.Received);
            Stats.AddPacket(PacketType.Received);
        }

        void Client_onDataReceived(object sender, DataEventArgs e)
        {
            //Technically we are receiving data from the client and sending it to the remote server, so it's being "sent" 
            Stats.AddBytes(e.Count, ByteType.Sent);
            Stats.AddPacket(PacketType.Sent);
        }
    }
}
