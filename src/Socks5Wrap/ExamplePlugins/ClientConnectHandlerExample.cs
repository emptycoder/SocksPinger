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

using Socks5Wrap.Plugin;

namespace Socks5Wrap.ExamplePlugins
{
    class ClientConnectHandlerExample : ClientConnectedHandler
    {
        public override bool OnStart()
        {
            return true;
        }

        public override bool OnConnect(TCP.Client client, System.Net.IPEndPoint ip)
        {
            if (ip.Address.ToString() != "127.0.0.1")
                //deny the connection.
                return false;
            return true;
            //With this function you can also Modify the Socket, as it's stored in e.Client.Sock.
        }
        private bool _enabled;
        public override bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
        }
    }
}
