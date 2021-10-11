using System;
using System.IO;

namespace SpysOnePingerWpf.Extensions
{
    public static class LoggerExtension
    {
        private static readonly object LockableObj = 12;
        private static StreamWriter _streamWriter;
        
        public static Exception Log(this Exception exception)
        {
            lock (LockableObj)
            {
                _streamWriter = new StreamWriter(Path.Combine(App.ApplicationPath, "release.log"), true);
                _streamWriter.WriteLine(exception);
                _streamWriter.Close();
            }

            return exception;
        }
        
        public static string Log(this string text)
        {
            lock (LockableObj)
            {
                _streamWriter = new StreamWriter(Path.Combine(App.ApplicationPath, "release.log"), true);
                _streamWriter.WriteLine(text);
                _streamWriter.Close();
            }

            return text;
        }
    }
}