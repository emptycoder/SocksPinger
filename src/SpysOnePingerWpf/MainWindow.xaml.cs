using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using CefSharp;
using Socks5Wrap.Socks5Client;
using Spectre.Console;
using SpysOnePingerWpf.Extensions;

namespace SpysOnePingerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly Settings Settings = new(Path.Combine(App.ApplicationPath, "settings.json"));
        private static readonly Regex IpPort = new(
            "(?<=<font class=\"spy14\">)[\\d]*\\.[\\d]*\\.[\\d]*\\.[\\d]*|(?<=<font class=\"spy2\">:</font>)[\\d]*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly (string, int) PingAddress = Settings.Get<string>("pingAddress").ParseEndPoint();
        private static MatchCollection _matchCollection;

        private bool _lockFlag;
        public MainWindow() => InitializeComponent();

        private async void Browser_OnFrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            const int defaultIpPortsCount = 30;
            const int separatedDefaultIpPortsCount = defaultIpPortsCount * 2;

            int separatedRealIpPortsCount = Settings.Get<string>("xpp") switch
            {
                "0" => 30,
                "1" => 50,
                "2" => 100,
                "3" => 200,
                "4" => 300,
                "5" => 500,
                _ => throw new ArgumentOutOfRangeException($"XPP Argument exception")
            } * 2;

            var result = await Browser.GetSourceAsync();
            
            if (string.IsNullOrWhiteSpace(result)) return;
            var matchCollection = IpPort.Matches(result);
            var count = matchCollection.Count;
            if (count == separatedDefaultIpPortsCount)
            {
                // HACK: Can't send POST request on server using Browser.LoadUrlWithPostData
                Browser.ExecuteScriptAsync(
                    $"document.getElementById('xpp').value = '{Settings.Get<string>("xpp")}'; document.querySelector('form[method=\"post\"]').submit()");
                return;
            }
            
            lock (this)
            {
                if (_lockFlag && count != separatedRealIpPortsCount) return;
                AnsiConsole.WriteLine($"Find proxies: {count.ToString()}");
                
                _lockFlag = true;
                _matchCollection = matchCollection;
                AnsiConsole.Progress().Start(CreateTasks);
            }
        }

        private static void CreateTasks(ProgressContext context)
        {
            AnsiConsole.WriteLine("Start pinging...");
            var uiTask = context.AddTask("Pinging");

            var tasks = new (IAsyncResult, string)[_matchCollection.Count / 2];
            for (int index = 0; index < _matchCollection.Count; index += 2)
            {
                try
                {
                    var client = new Socks5Client(_matchCollection[index].Value,
                        Convert.ToInt32(_matchCollection[index + 1].Value),
                        PingAddress.Item1,
                        PingAddress.Item2);
                    client.OnConnected += (_, _) =>
                    {
                        client.Client.Disconnect();
                        client.Client.Dispose();
                        uiTask.Increment(100 / (_matchCollection.Count / 2.0));
                    };
                    tasks[index / 2] = (client.ConnectAsync(), $"{_matchCollection[index].Value}:{_matchCollection[index + 1].Value}");
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }

            using var streamWriter = new StreamWriter(Settings.Get<string>("outputPath"));
            foreach (var (asyncResult, ipPort) in tasks)
            {
                asyncResult.AsyncWaitHandle.WaitOne();
                if (asyncResult.IsCompleted)
                    streamWriter.WriteLine(ipPort);
            }
            streamWriter.Close();

            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
    }
}