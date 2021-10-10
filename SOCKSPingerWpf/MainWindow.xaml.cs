using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;
using Org.Mentalis.Network.ProxySocket;
using Spectre.Console;

namespace SOCKSPingerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly Regex IpPort = new(
            "(?<=<font class=\"spy14\">)[\\d]*\\.[\\d]*\\.[\\d]*\\.[\\d]*|(?<=<font class=\"spy2\">:</font>)[\\d]*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static MatchCollection _matchCollection;
        private static ProgressTask _uiTask;
        private static StreamWriter _workingProxies;

        private bool _lockFlag = true;
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Browser_OnFrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            const int defaultIpPortsCount = 30;
            const int separatedDefaultIpPortsCount = defaultIpPortsCount * 2;
            
            const int realIpPortsCount = 500;
            const int separatedRealIpPortsCount = realIpPortsCount * 2;
            
            var result = await Browser.GetSourceAsync();
            
            if (string.IsNullOrWhiteSpace(result)) return;
            _matchCollection = IpPort.Matches(result);
            var count = _matchCollection.Count;
            if (count == separatedDefaultIpPortsCount)
            {
                 Browser.ExecuteScriptAsync(
                    "document.getElementById('xpp').value = '5'; document.querySelector('form[method=\"post\"]').submit()");
            }
            
            if (!_lockFlag) return;
            
            AnsiConsole.WriteLine($"---!>Found ips and ports: {count.ToString()}");
            
            lock (this)
            {
                if (count == separatedRealIpPortsCount)
                {
                    _lockFlag = false;
                    AnsiConsole.Progress().Start(Iterate);
                }
            }
        }

        private static void Iterate(ProgressContext context)
        {
            Action<object> pingAndAdd = PingAndAdd;
            Console.WriteLine("---!>Start pinging...");
            _uiTask = context.AddTask("Pinging");

            _workingProxies = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "proxies.txt"));
            try
            {
                var tasks = new Task[_matchCollection.Count / 2];
                for (int index = 0; index < _matchCollection.Count; index += 2)
                {
                    tasks[index / 2] = Task.Factory.StartNew(pingAndAdd, index);
                }

                Task.WaitAll(tasks);
                AnsiConsole.WriteLine("DONE!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _workingProxies.Close();
                _workingProxies.Dispose();
            }
            
            Application.Current.Shutdown();
        }

        private static void PingAndAdd(object state)
        {
            int index = (int) state;
            string ip = _matchCollection[index].Value;
            int port = Convert.ToInt32(_matchCollection[index + 1].Value);

            try
            {
                ProxySocket proxySocket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = 1,
                    ProxyType = ProxyTypes.Socks5,
                    ProxyEndPoint = new IPEndPoint(IPAddress.Parse(ip), port)
                };
                proxySocket.Connect("www.google.com", 80);

                proxySocket.Disconnect(false);
                proxySocket.Dispose();
                
                _workingProxies.WriteLine($"{ip}:{port.ToString()}");
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                _uiTask.Increment(100 / (_matchCollection.Count / 2.0));
            }
        }
    }
}