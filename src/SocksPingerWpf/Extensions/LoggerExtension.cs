using System;
using System.IO;

namespace SocksPingerWpf.Extensions
{
    public static class LoggerExtension
    {
        private const string CmdErrorsFileName = "error.log";
        private static readonly object LockableObj = 12;
        private static StreamWriter _streamWriter;
        
        public static Exception Log(this Exception exception)
        {
            lock (LockableObj)
            {
                _streamWriter = new StreamWriter(Path.Combine(App.ApplicationPath, CmdErrorsFileName), true);
                _streamWriter.WriteLine(exception);
                _streamWriter.Close();
            }

            return exception;
        }
        
        public static string Log(this string text)
        {
            lock (LockableObj)
            {
                _streamWriter = new StreamWriter(Path.Combine(App.ApplicationPath, CmdErrorsFileName), true);
                _streamWriter.WriteLine(text);
                _streamWriter.Close();
            }

            return text;
        }
    }
}