﻿/*
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
using Socks5Wrap.Encryption;
using Socks5Wrap.Plugin;
using Socks5Wrap.TCP;

namespace Socks5Wrap.Socks
{
    public class SocksClient
    {
        public event EventHandler<SocksClientEventArgs> OnClientDisconnected = delegate { };

        public Client Client;
        public int Authenticated { get; private set; }
        public SocksClient(Client cli)
        {
            Client = cli;
        }
        private SocksRequest _req1;
        public SocksRequest Destination { get { return _req1; } }
        public void Begin(IPAddress outboundInterface, int packetSize, int timeout)
        {
            Client.OnClientDisconnected += Client_onClientDisconnected;
            List<AuthTypes> authtypes = Socks5.RequestAuth(this);
            if (authtypes.Count <= 0)
            {
                Client.Send(new byte[] { 0x00, 0xFF });
                Client.Disconnect();
                return;
            }
            Authenticated = 0;
            SocksEncryption w = null;
            List<object> lhandlers = PluginLoader.LoadPlugin(typeof(LoginHandler));
            //check out different auth types, none will have no authentication, the rest do.
            if (lhandlers.Count > 0 && (authtypes.Contains(AuthTypes.SocksBoth) || authtypes.Contains(AuthTypes.SocksEncrypt) || authtypes.Contains(AuthTypes.SocksCompress) || authtypes.Contains(AuthTypes.Login)))
            {
                //this is the preferred method.
                w = Socks5.RequestSpecialMode(authtypes, Client);
                foreach (LoginHandler lh in lhandlers)
                {
					//request login.
					User user = Socks5.RequestLogin(this);
					if (user == null)
					{
						Client.Disconnect();
						return;
					}
					LoginStatus status = lh.HandleLogin(user);
					Client.Send(new byte[] { (byte)HeaderTypes.Socks5, (byte)status });
					if (status == LoginStatus.Denied)
					{
						Client.Disconnect();
						return;
					}
					else if (status == LoginStatus.Correct)
					{
						Authenticated = (w.GetAuthType() == AuthTypes.Login ? 1 : 2);
						break;
					}
                }
            }
            else if (authtypes.Contains(AuthTypes.None))
            {
                //no authentication.
                if (lhandlers.Count <= 0)
                {
                    //unsupported methods y0
                    Authenticated = 1;
                    Client.Send(new byte[] { (byte)HeaderTypes.Socks5, (byte)HeaderTypes.Zero });
                }
                else
                {
                    //unsupported.
                    Client.Send(new byte[] { (byte)HeaderTypes.Socks5, (byte)AuthTypes.Unsupported });
                    Client.Disconnect();
                    return;
                }
            }
            else
            {
                //unsupported.
                Client.Send(new byte[] { (byte)HeaderTypes.Socks5, (byte)AuthTypes.Unsupported });
                Client.Disconnect();
                return;
            }
            //Request Site Data.
            if (Authenticated == 1)
            {
                w = new SocksEncryption();
                w.SetType(AuthTypes.Login);
                SocksRequest req = Socks5.RequestTunnel(this, w);
                if (req == null) { Client.Disconnect(); return; }
                _req1 = new SocksRequest(req.StreamType, req.Type, req.Address, req.Port);
                //call on plugins for connect callbacks.
                foreach (ConnectHandler conn in PluginLoader.LoadPlugin(typeof(ConnectHandler)))
					if (conn.OnConnect(_req1) == false)
					{
						req.Error = SocksError.Failure;
						Client.Send(req.GetData(true));
						Client.Disconnect();
						return;
					}  
                //Send Tunnel Data back.
                SocksTunnel x = new SocksTunnel(this, req, _req1, packetSize, timeout);
                x.Open(outboundInterface);
            }
            else if (Authenticated == 2)
            {
                SocksRequest req = Socks5.RequestTunnel(this, w);
                if (req == null) { Client.Disconnect(); return; }
                _req1 = new SocksRequest(req.StreamType, req.Type, req.Address, req.Port);
                foreach (ConnectHandler conn in PluginLoader.LoadPlugin(typeof(ConnectHandler)))
					if (conn.OnConnect(_req1) == false)
					{
						req.Error = SocksError.Failure;
						Client.Send(req.GetData(true));
						Client.Disconnect();
						return;
					}  
                //Send Tunnel Data back.
                SocksSpecialTunnel x = new SocksSpecialTunnel(this, w, req, _req1, packetSize, timeout);
                x.Open(outboundInterface);
            }
        }

        void Client_onClientDisconnected(object sender, ClientEventArgs e)
        {
            OnClientDisconnected(this, new SocksClientEventArgs(this));
            Client.OnClientDisconnected -= Client_onClientDisconnected;
            //added to clear up memory
        }
    }
    public class User
    {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public IPEndPoint Ip { get; private set; }
        public User(string un, string pw, IPEndPoint ip)
        {
            Username = un;
            Password = pw;
            Ip = ip;
        }
    }
}
