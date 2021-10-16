using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using SocksPingerWpf.Extensions;

namespace SocksPingerWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        internal static readonly string ApplicationPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase ??
                                                          Environment.CurrentDirectory;
        internal static readonly Settings Settings = new(Path.Combine(ApplicationPath, "settings.json"));

        [DllImport("Kernel32")]
        private static extern void AllocConsole();

        [DllImport("Kernel32")]
        private static extern void FreeConsole();

        static App() => AllocConsole();
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
            e.Exception.Log();
        private void Application_Exit(object sender, ExitEventArgs e) => FreeConsole();
    }
}