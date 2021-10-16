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
using Socks5Wrap.Socks;
using Socks5Wrap.Socks5Client.Events;
using Socks5Wrap.TCP;

namespace Socks5Wrap.Socks5Client
{
    public class Socks5Client
    {
        private IPAddress _ipAddress;
        public Client Client;

        private Socket _p;
        private int _port;
        public bool ReqPass;
        public bool SmthReceived { get; private set; }

        private byte[] _halfReceiveBuffer = new byte[4200];
        private int _halfReceivedBufferLength;

        private string _username;
        private string _password;
        private string _dest;
        private int _destport;

        public Encryption.SocksEncryption Enc;

        public IList<AuthTypes> UseAuthTypes { get; set; }

        public event EventHandler<Socks5ClientArgs> OnConnectedEvent = delegate { };
        public event EventHandler<Socks5ClientDataArgs> OnDataReceivedEvent = delegate { };
        public event EventHandler<Socks5ClientDataArgs> OnDataSentEvent = delegate { };
        public event EventHandler<Socks5ClientArgs> OnDisconnectedEvent = delegate { };

        private Socks5Client()
        {
            UseAuthTypes = new List<AuthTypes>(new[] { AuthTypes.None, AuthTypes.Login, AuthTypes.SocksEncrypt });
        }

        public Socks5Client(string ipOrDomain, int port, string dest, int destport, string username = null, string password = null)
            : this()
        {
            //Parse IP?
            if (!IPAddress.TryParse(ipOrDomain, out _ipAddress))
            {
                //not connected.
                try
                {
                    foreach (IPAddress p in Dns.GetHostAddresses(ipOrDomain))
                        if (p.AddressFamily == AddressFamily.InterNetwork)
                        {
                            DoSocks(p, port, dest, destport, username, password);
                            return;
                        }
                }
                catch
                {
                    throw new NullReferenceException();
                }
            }           
            DoSocks(_ipAddress, port, dest, destport, username, password);
        }
        public Socks5Client(IPAddress ip, int port, string dest, int destport, string username = null, string password = null)
            : this()
        {
            DoSocks(ip, port, dest, destport, username, password);
        }

        private void DoSocks(IPAddress ip, int port, string dest, int destport, string username = null, string password = null)
        {
            _ipAddress = ip;
            _port = port;
            //check for username & pw.
            if(username != null && password != null)
            {
                _username = username;
                _password = password;
                ReqPass = true;
            }
            _dest = dest;
            _destport = destport;
        }

        public IAsyncResult ConnectAsync()
        {
            //
            _p = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Client = new Client(_p, 4200);
            Client.OnClientDisconnected += Client_onClientDisconnected;
            return Client.Sock.BeginConnect(new IPEndPoint(_ipAddress, _port), OnConnected, Client);
            //return status?
        }

        void Client_onClientDisconnected(object sender, ClientEventArgs e)
        {
            OnDisconnectedEvent(this, new Socks5ClientArgs(this, SocksError.Expired));
        }

        public bool Send(byte[] buffer, int offset, int length)
        {
            try
            {
                //buffer sending.
                int offst = 0;
                while(true)
                {
                    byte[] outputdata = Enc.ProcessOutputData(buffer, offst, (length - offst > 4092 ? 4092 : length - offst));
                    offst += (length - offst > 4092 ? 4092 : length - offst);
                    //craft headers & shit.
                    //send outputdata's length firs.t
                    if (Enc.GetAuthType() != AuthTypes.Login && Enc.GetAuthType() != AuthTypes.None)
                    {
                        byte[] datatosend = new byte[outputdata.Length + 4];
                        Buffer.BlockCopy(outputdata, 0, datatosend, 4, outputdata.Length);
                        Buffer.BlockCopy(BitConverter.GetBytes(outputdata.Length), 0, datatosend, 0, 4);
                        outputdata = null;
                        outputdata = datatosend;
                    }
                    Client.Send(outputdata, 0, outputdata.Length);
                    if (offst >= buffer.Length)
                    {
                        //exit;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool Send(byte[] buffer)
        {
            return Send(buffer, 0, buffer.Length);
        }

        public int Receive(byte[] buffer, int offset, int count)
        {
            //this should be packet header.
            try
            {
                if (Enc.GetAuthType() != AuthTypes.Login && Enc.GetAuthType() != AuthTypes.None)
                {
                    if(_halfReceivedBufferLength > 0)
                    {
                        if (_halfReceivedBufferLength <= count)
                        {
                            Buffer.BlockCopy(_halfReceiveBuffer, 0, buffer, offset, _halfReceivedBufferLength);
                            _halfReceivedBufferLength = 0;
                            return _halfReceivedBufferLength;
                        }
                        else
                        {
                            Buffer.BlockCopy(_halfReceiveBuffer, 0, buffer, offset, count);
                            _halfReceivedBufferLength = _halfReceivedBufferLength - count;
                            Buffer.BlockCopy(_halfReceiveBuffer, count, _halfReceiveBuffer, 0, count);

                            return count;
                        }
                    }

                    count = Math.Min(4200, count);

                    byte[] databuf = new byte[4200];
                    int got = Client.Receive(databuf, 0, 4200);

                    int packetsize = BitConverter.ToInt32(databuf, 0);
                    byte[] processed = Enc.ProcessInputData(databuf, 4, packetsize);

                    Buffer.BlockCopy(databuf, 0, buffer, offset, count);
                    Buffer.BlockCopy(databuf, count, _halfReceiveBuffer, 0, packetsize - count);
                    _halfReceivedBufferLength = packetsize - count;
                    return count;
                }
                else
                {
                    return Client.Receive(buffer, offset, count);
                }
            }
            catch (Exception)
            {
                //disconnect.
                Client.Disconnect();
                throw;
            }
        }

        public void ReceiveAsync()
        {
            if (Enc.GetAuthType() != AuthTypes.Login && Enc.GetAuthType() != AuthTypes.None)
            {
                Client.ReceiveAsync(4);
            }
            else
            {
                Client.ReceiveAsync(4096);
            }
        }


        private void Client_onDataReceived(object sender, DataEventArgs e)
        {
            SmthReceived = true;
            //this should be packet header.
            try
            {
                if (Enc.GetAuthType() != AuthTypes.Login && Enc.GetAuthType() != AuthTypes.None)
                {
                    //get total number of bytes.
                    int torecv = BitConverter.ToInt32(e.Buffer, 0);
                    byte[] newbuff = new byte[torecv];

                    int recvd = Client.Receive(newbuff, 0, torecv);
                    if (recvd == torecv)
                    {
                        byte[] output = Enc.ProcessInputData(newbuff, 0, recvd);
                        //receive full packet.
                        e.Buffer = output;
                        e.Offset = 0;
                        e.Count = output.Length;
                        OnDataReceivedEvent(this, new Socks5ClientDataArgs(this, e.Buffer, e.Count, e.Offset));
                    }
                }
                else
                {
                    OnDataReceivedEvent(this, new Socks5ClientDataArgs(this, e.Buffer, e.Count, e.Offset));
                }
            }
            catch (Exception ex)
            {
                //disconnect.
                Client.Disconnect();
                throw ex;
            }
        }

        public bool Connect()
        {
            try
            {
                _p = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Client = new Client(_p, 65535);
                Client.Sock.Connect(new IPEndPoint(_ipAddress, _port));
                //try the greeting.
                //Client.onDataReceived += Client_onDataReceived;
                if(Socks.DoSocksAuth(this, _username, _password))
                    if (Socks.SendRequest(Client, Enc, _dest, _destport) == SocksError.Granted) {
                        Client.OnDataReceived += Client_onDataReceived;
                        return true;
                    }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void OnConnected(IAsyncResult res)
        {
            Client = (Client)res.AsyncState;
            try
            {
                Client.Sock.EndConnect(res);
            }
            catch
            {
                OnConnectedEvent(this, new Socks5ClientArgs(null, SocksError.Failure));
                return;
            }
            if (Socks.DoSocksAuth(this, _username, _password))
            {
                SocksError p = Socks.SendRequest(Client, Enc, _dest, _destport);
                Client.OnDataReceived += Client_onDataReceived;
                OnConnectedEvent(this, new Socks5ClientArgs(this, p));
                
            }
            else
                OnConnectedEvent(this, new Socks5ClientArgs(this, SocksError.Failure));
        }
        
        public bool Connected => Client != null && Client.Sock.Connected;
        //send.
    }
}
