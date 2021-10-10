using System;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CefSharp;
using Socks5;
using Spectre.Console;

namespace SpysOnePingerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly CancellationTokenSource CancellationToken = new();
        private static readonly Regex IpPort = new(
            "(?<=<font class=\"spy14\">)[\\d]*\\.[\\d]*\\.[\\d]*\\.[\\d]*|(?<=<font class=\"spy2\">:</font>)[\\d]*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Func<Socket> Socks5Factory =
            () => new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        private static MatchCollection _matchCollection;
        private static ProgressTask _uiTask;
        private static StreamWriter _workingProxies;

        private bool _lockFlag = true;
        public MainWindow() => InitializeComponent();

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
                // HACK: Can't send POST request on server using Browser.LoadUrlWithPostData
                Browser.ExecuteScriptAsync(
                    "document.getElementById('xpp').value = '5'; document.querySelector('form[method=\"post\"]').submit()");
            
            lock (this)
            {
                if (!_lockFlag) return;
                AnsiConsole.WriteLine($"Found ips and ports: {count.ToString()}");
                
                if (count != separatedRealIpPortsCount) return;
                
                _lockFlag = false;
                AnsiConsole.Progress().Start(Iterate);
            }
        }

        private static void Iterate(ProgressContext context)
        {
            Action<object> pingAndAdd = PingAndAdd;
            AnsiConsole.WriteLine("Start pinging...");
            _uiTask = context.AddTask("Pinging");

            _workingProxies = new StreamWriter(Path.Combine(Environment.CurrentDirectory, "proxies.txt"));
            
            var tasks = new Task[_matchCollection.Count / 2];
            for (int index = 0; index < _matchCollection.Count; index += 2)
            {
                try
                {
                    tasks[index / 2] = Task.Factory.StartNew(pingAndAdd, index, CancellationToken.Token);
                    CancellationToken.CancelAfter(2000);
                }
                catch (TaskCanceledException) { }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.ReadKey();
                }
            }

            Task.WaitAll(tasks);
            _workingProxies.Close();
            _workingProxies.Dispose();

            Application.Current.Shutdown();
        }

        private static async void PingAndAdd(object state)
        {
            int index = (int) state;
            string ip = _matchCollection[index].Value;
            int port = Convert.ToInt32(_matchCollection[index + 1].Value);

            try
            {
                var socket = await Socks5Proxy.Connect(Socks5Factory,
                    new Socks5Options(ip, port, "www.google.com", 80));
                socket.Disconnect(false);
                socket.Dispose();

                await _workingProxies.WriteLineAsync($"{ip}:{port.ToString()}");
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