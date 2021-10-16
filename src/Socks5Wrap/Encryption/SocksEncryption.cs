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
using System.Security.Cryptography;
using System.Text;
using Socks5Wrap.Socks;

namespace Socks5Wrap.Encryption
{
    public class SocksEncryption
    {
        public SocksEncryption()
        {

        }

        public RSACryptoServiceProvider Key;
        private RSACryptoServiceProvider _remotepubkey;
        private DarthEncrypt _dc;
        private DarthEncrypt _dcc;
        private AuthTypes _auth;

        public void GenerateKeys()
        {
            Key = new RSACryptoServiceProvider(1024);
            _remotepubkey = new RSACryptoServiceProvider(1024);
            _remotepubkey.PersistKeyInCsp = false;
            Key.PersistKeyInCsp = false;
            _dc = new DarthEncrypt();
            _dc.PassPhrase = Utils.RandStr(20);
            _dcc = new DarthEncrypt();
        }

        public byte[] ShareEncryptionKey()
        {
            //share public key.
            return _remotepubkey.Encrypt(Encoding.ASCII.GetBytes(_dc.PassPhrase), false);
        }

        public byte[] GetPublicKey()
        {
            return Encoding.ASCII.GetBytes(Key.ToXmlString(false));
        }

        public void SetEncKey(byte[] key)
        {
            _dcc.PassPhrase = Encoding.ASCII.GetString(key);
        }

        public void SetKey(byte[] key, int offset, int len)
        {
            string e = Encoding.ASCII.GetString(key, offset, len);
            _remotepubkey.FromXmlString(e);
        }

        public void SetType(AuthTypes k)
        {
            _auth = k;
        }

        public AuthTypes GetAuthType()
        {
            return _auth;
        }

        public byte[] ProcessInputData(byte[] buffer, int offset, int count)
        {
            //realign buffer.
            try
            {
                byte[] buff = new byte[count];
                Buffer.BlockCopy(buffer, offset, buff, 0, count);
                switch (_auth)
                {
                    case AuthTypes.SocksBoth:
                        //decrypt, then decompress.
                        byte[] data = _dcc.DecryptBytes(buff);
                        return _dcc.DecompressBytes(data);
                    case AuthTypes.SocksCompress:
                        //compress data.
                        return _dcc.DecompressBytes(buff);
                    case AuthTypes.SocksEncrypt:
                        return _dcc.DecryptBytes(buff);
                    default:
                        return buffer;
                }
            }
            catch {
                return null;
            }
        }

        public byte[] ProcessOutputData(byte[] buffer, int offset, int count)
        {
            //realign buffer.
            try
            {
                byte[] buff = new byte[count - offset];
                Buffer.BlockCopy(buffer, offset, buff, 0, count);
                switch (_auth)
                {
                    case AuthTypes.SocksBoth:
                        //compress, then encrypt.
                        byte[] data = _dc.CompressBytes(buff, 0, count);
                        return _dc.EncryptBytes(data);
                    case AuthTypes.SocksCompress:
                        //compress data.
                        return _dc.CompressBytes(buff, 0, count);
                    case AuthTypes.SocksEncrypt:
                        return _dc.EncryptBytes(buff);
                    default:
                        return buffer;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
