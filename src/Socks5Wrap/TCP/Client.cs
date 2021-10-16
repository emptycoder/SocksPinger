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
using System.Net.Sockets;

namespace Socks5Wrap.TCP
{
    public class Client
    {
        public event EventHandler<ClientEventArgs> OnClientDisconnected;

        public event EventHandler<DataEventArgs> OnDataReceived = delegate { };
        public event EventHandler<DataEventArgs> OnDataSent = delegate { };

        public Socket Sock { get; set; }
        private byte[] _buffer;
        private int _packetSize = 4096;
        public bool Receiving;

        public Client(Socket sock, int packetSize)
        {
            //start the data exchange.
            Sock = sock;
            OnClientDisconnected = delegate { };
            _buffer = new byte[packetSize];
            _packetSize = packetSize;
            sock.ReceiveBufferSize = packetSize;
        }

        private void DataReceived(IAsyncResult res)
        {
            Receiving = false;
            try
            {
                SocketError err = SocketError.Success;
                if(_disposed)
                    return;
                int received = ((Socket)res.AsyncState).EndReceive(res, out err);
                if (received <= 0 || err != SocketError.Success)
                {
                    Disconnect();
                    return;
                }
                DataEventArgs data = new DataEventArgs(this, _buffer, received);
                OnDataReceived(this, data);
            }
            catch (Exception ex)
            {
                #if DEBUG
 #if DEBUG
 Console.WriteLine(ex.ToString()); 
#endif 
#endif
                Disconnect();
            }
        }

        public int Receive(byte[] data, int offset, int count)
        {
            try
            {
                int received = Sock.Receive(data, offset, count, SocketFlags.None);
                if (received <= 0)
                {
                    Disconnect();
                    return -1;
                }
                DataEventArgs dargs = new DataEventArgs(this, data, received);
                //this.onDataReceived(this, dargs);
                return received;
            }
            catch (Exception ex)
            {
                #if DEBUG
  Console.WriteLine(ex.ToString()); 
#endif 
                Disconnect();
                return -1;
            }
        }

        public IAsyncResult ReceiveAsync(int buffersize = -1)
        {
            try
            {
                if (buffersize > -1)
                {
                    _buffer = new byte[buffersize];
                }
                Receiving = true;
                return Sock.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(DataReceived), Sock);
            }
            catch(Exception ex)
            {
                #if DEBUG
 Console.WriteLine(ex.ToString()); 
#endif
                Disconnect();
            }

            return null;
        }


        public void Disconnect()
        {
            try
            {
                //while (Receiving) Thread.Sleep(10);
                if (!_disposed)
                {
                    if (Sock != null && Sock.Connected)
                    {
                        OnClientDisconnected(this, new ClientEventArgs(this));
                        Sock.Close();
                        //this.Sock = null;
                        return;
                    }
                    else
                        OnClientDisconnected(this, new ClientEventArgs(this));
                    Dispose();
                }
            }
            catch { }
        }

        private void DataSent(IAsyncResult res)
        {
            try
            {
                int sent = ((Socket)res.AsyncState).EndSend(res);
                if (sent < 0)
                {
                    Sock.Shutdown(SocketShutdown.Both);
                    Sock.Close();
                    return;
                }
                DataEventArgs data = new DataEventArgs(this, new byte[0] {}, sent);
                OnDataSent(this, data);
            }
            catch (Exception ex) {
#if DEBUG
 Console.WriteLine(ex.ToString()); 
#endif 
            }
        }

        public bool Send(byte[] buff)
        {
            return Send(buff, 0, buff.Length);
        }

        public void SendAsync(byte[] buff, int offset, int count)
        {
            try
            {
                if (Sock != null && Sock.Connected)
                {
                    Sock.BeginSend(buff, offset, count, SocketFlags.None, DataSent, Sock);
                }
            }
            catch (Exception ex)
            {
                #if DEBUG
 Console.WriteLine(ex.ToString()); 
#endif
                Disconnect();
            }
        }

        public bool Send(byte[] buff, int offset, int count)
        {
            try
            {
                if (Sock != null)
                {
                    if (Sock.Send(buff, offset, count, SocketFlags.None) <= 0)
                    {
                        Disconnect();
                        return false;
                    }
                    DataEventArgs data = new DataEventArgs(this, buff, count);
                    OnDataSent(this, data);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                #if DEBUG
 #if DEBUG
 Console.WriteLine(ex.ToString()); 
#endif 
#endif
                Disconnect();
                return false;
            }
        }

        private bool _disposed;

        // Public implementation of Dispose pattern callable by consumers. 
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {

            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                // Free any other managed objects here. 
                //
                Sock = null;
                _buffer = null;
                OnClientDisconnected = null;
                OnDataReceived = null;
                OnDataSent = null;
            }

            // Free any unmanaged objects here. 
            //
            
        }
    }
}
