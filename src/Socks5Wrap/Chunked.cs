using System;
using System.Net.Sockets;

namespace Socks5Wrap
{
    //WARNING: BETA - Doesn't work as well as intended. Use at your own discretion.
    public class Chunked
    {
        private byte[] _totalbuff;
        private byte[] _finalbuff;
        /// <summary>
        /// Create a new instance of chunked.
        /// </summary>
        /// <param name="f"></param>
        public Chunked(Socket f, byte[] oldbuffer, int size)
        {
            //Find first chunk.
            if (IsChunked(oldbuffer))
            {
                int endofheader = oldbuffer.FindString("\r\n\r\n");
                int endofchunked = oldbuffer.FindString("\r\n", endofheader + 4);
                //
                string chunked = oldbuffer.GetBetween(endofheader + 4, endofchunked);
                //convert chunked data to int.
                int totallen = chunked.FromHex();
                //
                if (totallen > 0)
                {
                    //start a while loop and receive till end of chunk.
                    _totalbuff = new byte[65535];
                    _finalbuff = new byte[size];
                    //remove chunk data before adding.
                    oldbuffer = oldbuffer.ReplaceBetween(endofheader + 4, endofchunked + 2, new byte[] { });
                    Buffer.BlockCopy(oldbuffer, 0, _finalbuff, 0, size);
                    if (f.Connected)
                    {
                        int totalchunksize = 0;
                        int received = f.Receive(_totalbuff, 0, _totalbuff.Length, SocketFlags.None);
                        while ((totalchunksize = GetChunkSize(_totalbuff, received)) != -1)
                        {
                            //add data to final byte buffer.
                            byte[] chunkedData = GetChunkData(_totalbuff, received);
                            byte[] tempData = new byte[chunkedData.Length + _finalbuff.Length];
                            //get data AFTER chunked response.
                            Buffer.BlockCopy(_finalbuff, 0, tempData, 0, _finalbuff.Length);
                            Buffer.BlockCopy(chunkedData, 0, tempData, _finalbuff.Length, chunkedData.Length);
                            //now add to finalbuff.
                            _finalbuff = tempData;
                            //receive again.
                            if (totalchunksize == -2)
                                break;
                            else
                                received = f.Receive(_totalbuff, 0, _totalbuff.Length, SocketFlags.None);

                        }
                        //end of chunk.
                        Console.WriteLine("Got chunk! Size: {0}", _finalbuff.Length);
                    }
                }
                else
                {
                    _finalbuff = new byte[size];
                    Buffer.BlockCopy(oldbuffer, 0, _finalbuff, 0, size);
                }
            }
        }

        public byte[] RawData
        {
            get
            {
                return _finalbuff;
            }
        }

        public byte[] ChunkedData
        {
            get
            {
                //get size from \r\n\r\n and past.
                int location = _finalbuff.FindString("\r\n\r\n") + 4;
                //size
                int size = _finalbuff.Length - location - 7; //-7 is initial end of chunk data.
                return _finalbuff.ReplaceString("\r\n\r\n", "\r\n\r\n" + size.ToHex().Replace("0x", "") + "\r\n");
            }
        }

        public static int GetChunkSize(byte[] buffer, int count)
        {
            //chunk size is first chars till \r\n.
            if(buffer.FindString("\r\n0\r\n\r\n", count - 7) != -1)
            {
                //end of buffer.
                return -2;
            }
            string chunksize = buffer.GetBetween(0, buffer.FindString("\r\n"));
            return chunksize.FromHex();
        }

        public static byte[] GetChunkData(byte[] buffer, int size)
        {
            //parse out the chunk size and return data.
            return buffer.GetInBetween(buffer.FindString("\r\n") + 2, size);
        }

        public static bool IsChunked(byte[] buffer)
        {
            return (IsHttp(buffer) && buffer.FindString("Transfer-Encoding: chunked\r\n") != -1);
        }

        public static bool IsHttp(byte[] buffer)
        {
            return (buffer.FindString("HTTP/1.1") != -1 && buffer.FindString("\r\n\r\n") != -1);
        }
    }
}
