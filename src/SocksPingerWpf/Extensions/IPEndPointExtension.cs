using System;
using System.Globalization;

namespace SocksPingerWpf.Extensions
{
    public static class IpEndPointExtension
    {
        public static (string, int) ParseEndPoint(this string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length != 2) throw new FormatException("Invalid endpoint format");
            if (!int.TryParse(ep[^1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out var port))
                throw new FormatException("Invalid port");
            
            return (ep[0], port);
        }
    }
}