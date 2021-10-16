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

namespace Socks5Wrap.TCP
{
    public class BandwidthCounter
    {
        /// <summary>
        /// Class to manage an adapters current transfer rate
        /// </summary>
        class MiniCounter
        {
            public ulong Bytes;
            public ulong Kbytes;
            public ulong Mbytes;
            public ulong Gbytes;
            public ulong Tbytes;
            public ulong Pbytes;
            DateTime _lastRead = DateTime.Now;

            /// <summary>
            /// Adds bits(total misnomer because bits per second looks a lot better than bytes per second)
            /// </summary>
            /// <param name="count">The number of bits to add</param>
            public void AddBytes(ulong count)
            {
                Bytes += count;
                while (Bytes > 1024)
                {
                    Kbytes++;
                    Bytes -= 1024;
                }
                while (Kbytes > 1024)
                {
                    Mbytes++;
                    Kbytes -= 1024;
                }
                while (Mbytes > 1024)
                {
                    Gbytes++;
                    Mbytes -= 1024;
                }
                while (Gbytes > 1024)
                {
                    Tbytes++;
                    Gbytes -= 1024;
                }
                while (Tbytes > 1024)
                {
                    Pbytes++;
                    Tbytes -= 1024;
                }
            }


            public ulong BytesPerSec()
            {
                if (Gbytes > 0)
                {
                    double ret = (double)Gbytes + ((double)((double)Mbytes / 1024));
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;

                    _lastRead = DateTime.Now;
                    
                    return (ulong)(((ret * 1024) * 1024) * 1024);
                }
                else if (Mbytes > 0)
                {
                    double ret = (double)Mbytes + ((double)((double)Kbytes / 1024));
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;

                    _lastRead = DateTime.Now;

                    return (ulong)((ret * 1024) * 1024);
                }
                else if (Kbytes > 0)
                {
                    double ret = (double)Kbytes + ((double)((double)Bytes / 1024));
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;
                    _lastRead = DateTime.Now;

                    return (ulong)(ret * 1024);
                }
                else
                {
                    double ret = Bytes;
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;
                    _lastRead = DateTime.Now;

                    return (ulong)ret;
                }
            }

            /// <summary>
            /// Returns the bits per second since the last time this function was called
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if (Pbytes > 0)
                {
                    double ret = (double)Pbytes + ((double)((double)Tbytes / 1024));
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;

                    _lastRead = DateTime.Now;
                    string s = ret.ToString();
                    if (s.Length > 6)
                        s = s.Substring(0, 6);
                    return s + " PB";
                }
                else if (Tbytes > 0)
                {
                    double ret = (double)Tbytes + ((double)((double)Gbytes / 1024));
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;

                    _lastRead = DateTime.Now;
                    string s = ret.ToString();
                    if (s.Length > 6)
                        s = s.Substring(0, 6);
                    return s + " TB";
                }
                else if (Gbytes > 0)
                {
                    double ret = (double)Gbytes + ((double)((double)Mbytes / 1024));
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;

                    _lastRead = DateTime.Now;
                    string s = ret.ToString();
                    if (s.Length > 6)
                        s = s.Substring(0, 6);
                    return s + " GB";
                }
                else if (Mbytes > 0)
                {
                    double ret = (double)Mbytes + ((double)((double)Kbytes / 1024));
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;

                    _lastRead = DateTime.Now;
                    string s = ret.ToString();
                    if (s.Length > 6)
                        s = s.Substring(0, 6);
                    return s + " MB";
                }
                else if (Kbytes > 0)
                {
                    double ret = (double)Kbytes + ((double)((double)Bytes / 1024));
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;
                    _lastRead = DateTime.Now;
                    string s = ret.ToString();
                    if (s.Length > 6)
                        s = s.Substring(0, 6);
                    return s + " KB";
                }
                else
                {
                    double ret = Bytes;
                    ret = ret / (DateTime.Now - _lastRead).TotalSeconds;
                    _lastRead = DateTime.Now;
                    string s = ret.ToString();
                    if (s.Length > 6)
                        s = s.Substring(0, 6);
                    return s + " B";
                }
            }
        }

        private ulong _bytes;
        private ulong _kbytes;
        private ulong _mbytes;
        private ulong _gbytes;
        private ulong _tbytes;
        private ulong _pbytes;
        MiniCounter _perSecond = new MiniCounter();

        /// <summary>
        /// Empty constructor, because thats constructive
        /// </summary>
        public BandwidthCounter()
        {

        }

        /// <summary>
        /// Accesses the current transfer rate, returning the text
        /// </summary>
        /// <returns></returns>
        public string GetPerSecond()
        {
            string s = _perSecond.ToString() + "/s";
            _perSecond = new MiniCounter();
            return s;
        }

        public ulong GetPerSecondNumeric()
        {
            ulong val = _perSecond.BytesPerSec();
            _perSecond = new MiniCounter();
            return val;
        }

        /// <summary>
        /// Adds bytes to the total transfered
        /// </summary>
        /// <param name="count">Byte count</param>
        public void AddBytes(ulong count)
        {
            // overflow max
            _perSecond.AddBytes(count);
            _bytes += count;
            while (_bytes > 1024)
            {
                _kbytes++;
                _bytes -= 1024;
            }
            while (_kbytes > 1024)
            {
                _mbytes++;
                _kbytes -= 1024;
            }
            while (_mbytes > 1024)
            {
                _gbytes++;
                _mbytes -= 1024;
            }
            while (_gbytes > 1024)
            {
                _tbytes++;
                _gbytes -= 1024;
            }
            while (_tbytes > 1024)
            {
                _pbytes++;
                _tbytes -= 1024;
            }
        }

        /// <summary>
        /// Prints out a relevant string for the bits transfered
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_pbytes > 0)
            {
                double ret = (double)_pbytes + ((double)((double)_tbytes / 1024));
                string s = ret.ToString();
                if (s.Length > 6)
                    s = s.Substring(0, 6);
                return s + " Pb";
            }
            else if (_tbytes > 0)
            {
                double ret = (double)_tbytes + ((double)((double)_gbytes / 1024));

                string s = ret.ToString();
                if (s.Length > 6)
                    s = s.Substring(0, 6);
                return s + " TB";
            }
            else if (_gbytes > 0)
            {
                double ret = (double)_gbytes + ((double)((double)_mbytes / 1024));
                string s = ret.ToString();
                if (s.Length > 6)
                    s = s.Substring(0, 6);
                return s + " GB";
            }
            else if (_mbytes > 0)
            {
                double ret = (double)_mbytes + ((double)((double)_kbytes / 1024));

                string s = ret.ToString();
                if (s.Length > 6)
                    s = s.Substring(0, 6);
                return s + " MB";
            }
            else if (_kbytes > 0)
            {
                double ret = (double)_kbytes + ((double)((double)_bytes / 1024));

                string s = ret.ToString();
                if (s.Length > 6)
                    s = s.Substring(0, 6);
                return s + " KB";
            }
            else
            {
                string s = _bytes.ToString();
                if (s.Length > 6)
                    s = s.Substring(0, 6);
                return s + " b";
            }
        }
    }
}
