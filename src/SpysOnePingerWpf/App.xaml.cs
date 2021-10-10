using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace SpysOnePingerWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
	    private static readonly string ApplicationPath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase ??
	                                                     Environment.CurrentDirectory;

	    [DllImport("Kernel32")]
        private static extern void AllocConsole();
        
        [DllImport("Kernel32")]
        private static extern void FreeConsole();
        
        static App()
        {
            AllocConsole();

            // AppDomain.CurrentDomain.AssemblyResolve += Resolver;
            // InitializeCefSharp();
        }
        
                    
		// [MethodImpl(MethodImplOptions.NoInlining)]
  //       private static void InitializeCefSharp()
  //       {
	 //        if (!Environment.Is64BitProcess) throw new Exception("Not supported!");
	 //        string runtimesPath = Path.Combine(ApplicationPath, "runtimes", "win-x64");
	 //        var settings = new CefSettings
	 //        {
		//         BrowserSubprocessPath = Path.Combine(runtimesPath, "native", "CefSharp.BrowserSubprocess.exe")
	 //        };
  //
	 //        Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
  //       }
  //
  //       private static Assembly Resolver(object sender, ResolveEventArgs args)
  //       {
	 //        if (!Environment.Is64BitProcess) throw new Exception("Not supported!");
	 //        if (!args.Name.StartsWith("CefSharp")) return null;
	 //        string runtimesPath = Path.Combine(ApplicationPath, "runtimes", "win-x64");
  //
	 //        var assemblyName = $"{args.Name.Split(',', 2)[0]}.dll";
  //           var archSpecificPath = Path.Combine(runtimesPath, "lib", "netcoreapp3.1", assemblyName);
  //
  //           return File.Exists(archSpecificPath)? Assembly.LoadFile(archSpecificPath) : null;
  //       }

	    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	    {
		    using var streamWriter = new StreamWriter(Path.Combine(ApplicationPath, "release.log"));
		    streamWriter.WriteLine(e.Exception);
		    streamWriter.Close();
	    }
        private void Application_Exit(object sender, ExitEventArgs e) => FreeConsole();
    }
}