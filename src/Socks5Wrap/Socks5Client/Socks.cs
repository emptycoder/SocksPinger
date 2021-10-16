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
using System.Text;
using Socks5Wrap.Encryption;
using Socks5Wrap.Socks;
using Socks5Wrap.TCP;

namespace Socks5Wrap.Socks5Client
{
    public class Socks
    {
        public static AuthTypes Greet(Client client, IList<AuthTypes> supportedAuthTypes = null)
        {
            if (supportedAuthTypes == null)
                supportedAuthTypes = new[] { AuthTypes.None, AuthTypes.Login, AuthTypes.SocksEncrypt };

            // https://www.ietf.org/rfc/rfc1928.txt [Page 3]
            var bytes = new byte[supportedAuthTypes.Count + 2];
            bytes[0] = 0x05; // protocol version - socks5
            bytes[1] = (byte)supportedAuthTypes.Count;
            for (var i = 0; i < supportedAuthTypes.Count; i++)
            {
                bytes[i + 2] = (byte)supportedAuthTypes[i];
            }
            client.Send(bytes);

            byte[] buffer = new byte[512];
            int received = client.Receive(buffer, 0, buffer.Length);
            if(received > 0)
            {
                //check for server version.
                if (buffer[0] == 0x05)
                {
                    return (AuthTypes)buffer[1];
                }
            }
            return 0;
        }

        public static int SendLogin(Client cli, string username, string password)
        {
            byte[] x = new byte[username.Length + password.Length + 3];
            int total = 0;
            x[total++] = 0x01;
            x[total++] = Convert.ToByte(username.Length);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(username), 0, x, 2, username.Length);
            total += username.Length;
            x[total++] = Convert.ToByte(password.Length); 
            Buffer.BlockCopy(Encoding.ASCII.GetBytes(password), 0, x, total, password.Length);
            //send request.
            cli.Send(x);
            byte[] buffer = new byte[512];
            cli.Receive(buffer, 0, buffer.Length);
            if (buffer[1] == 0x00)
            {
                return 1;
            }
            else if (buffer[1] == 0xFF)
            {
                return 0;
            }
            return 0;
        }

        public static SocksError SendRequest(Client cli, SocksEncryption enc, string ipOrDomain, int port)
        {
            AddressType type;
            IPAddress ipAddress;
            if (!IPAddress.TryParse(ipOrDomain, out ipAddress))
                //it's a domain. :D (hopefully).
                type = AddressType.Domain;
            else
                type = AddressType.Ip;
            SocksRequest sr = new SocksRequest(StreamTypes.Stream, type, ipOrDomain, port);
            //send data.
            byte[] p = sr.GetData(false);
            p[1] = 0x01;
            //process data.
            cli.Send(enc.ProcessOutputData(p, 0, p.Length));
            byte[] buffer = new byte[512];
            //process input data.
            int recv = cli.Receive(buffer, 0, buffer.Length);
            if(recv == -1)
            {
                return SocksError.Failure;
            }
            byte[] buff = enc.ProcessInputData(buffer, 0, recv);
            
            return (SocksError)buff[1];
        }

        public static bool DoSocksAuth(Socks5Client p, string username, string password)
        {
            AuthTypes auth = Greet(p.Client, p.UseAuthTypes);
            if (auth == AuthTypes.Unsupported)
            {
                p.Client.Disconnect();
                return false;
            }
            p.Enc = new SocksEncryption();
            if (auth != AuthTypes.None)
            {
                switch (auth)
                {
                    case AuthTypes.Login:
                        //logged in.
                        p.Enc.SetType(AuthTypes.Login);
                        //just reqeust login?

                        break;
                    case AuthTypes.SocksBoth:
                        //socksboth.
                        p.Enc.SetType(AuthTypes.SocksBoth);
                        p.Enc.GenerateKeys();
                        //send public key.
                        p.Client.Send(p.Enc.GetPublicKey());
                        //now receive key.

                        byte[] buffer = new byte[4096];
                        int keysize = p.Client.Receive(buffer, 0, buffer.Length);
                        p.Enc.SetKey(buffer, 0, keysize);
                        //let them know we got it
                        //now receive our encryption key.
                        int enckeysize = p.Client.Receive(buffer, 0, buffer.Length);
                        //decrypt with our public key.
                        byte[] newkey = new byte[enckeysize];
                        Buffer.BlockCopy(buffer, 0, newkey, 0, enckeysize);
                        p.Enc.SetEncKey(p.Enc.Key.Decrypt(newkey, false));
                        //now we share our encryption key.
                        p.Client.Send(p.Enc.ShareEncryptionKey());

                        break;
                    case AuthTypes.SocksEncrypt:
                        p.Enc.SetType(AuthTypes.SocksEncrypt);
                        p.Enc.GenerateKeys();
                        //send public key.
                        p.Client.Send(p.Enc.GetPublicKey());
                        //now receive key.

                        buffer = new byte[4096];
                        keysize = p.Client.Receive(buffer, 0, buffer.Length);
                        p.Enc.SetKey(buffer, 0, keysize);
                        //now receive our encryption key.
                        enckeysize = p.Client.Receive(buffer, 0, buffer.Length);
                        //decrypt with our public key.
                        newkey = new byte[enckeysize];
                        Buffer.BlockCopy(buffer, 0, newkey, 0, enckeysize);
                        p.Enc.SetEncKey(p.Enc.Key.Decrypt(newkey, false));
                        //now we share our encryption key.

                        p.Client.Send(p.Enc.ShareEncryptionKey());

                        //socksencrypt.
                        break;
                    case AuthTypes.SocksCompress:
                        p.Enc.SetType(AuthTypes.SocksCompress);
                        //sockscompress.
                        break;
                    default:
                        p.Client.Disconnect();
                        return false;
                }
                if (p.Enc.GetAuthType() != AuthTypes.Login)
                {
                    //now receive login params.
                    byte[] buff = new byte[1024];
                    int recv = p.Client.Receive(buff, 0, buff.Length);
                    //check for 
                    if (recv > 0)
                    {
                        //check if socks5 version is 5
                        if (buff[0] == 0x05)
                        {
                            //good.
                            if (buff[1] == (byte)AuthTypes.Login)
                            {
                                if (username == null || password == null) { p.Client.Sock.Close(); return false; }
                                int ret = SendLogin(p.Client, username, password);
                                if (ret != 1)
                                {
                                    p.Client.Sock.Close();
                                    return false;
                                }
                            }
                            else
                            {
                                //idk? close for now.
                                p.Client.Disconnect();
                                return false;
                            }
                        }
                    }
                    else
                    {
                        p.Client.Disconnect();
                        return false;
                    }
                }
                else
                {
                    if (username == null || password == null) { p.Client.Sock.Close(); return false; }
                    int ret = SendLogin(p.Client,username, password);
                    if (ret != 1)
                    {
                        p.Client.Sock.Close();
                        return false;
                    }
                }
            }
            return true;
        }
    }
}