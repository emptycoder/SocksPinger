using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Socks5Wrap.Socks5Client;
using Spectre.Console;
using SocksPingerWpf.Extensions;

namespace SocksPingerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly string PingAddressEndpoint = App.Settings.Get<string>("pingAddress");
        private static readonly (string, int) PingAddress = PingAddressEndpoint.ParseEndPoint();

        public MainWindow() => InitializeComponent();

        private async void Browser_OnLoaded(object sender, RoutedEventArgs e)
        {
            var directoryPath = Path.Combine(App.ApplicationPath, "cache");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            
            var matchCollection = await Browser.ParseOrGetFromCacheAsync(directoryPath);
            Hide();
            
            // matchCollection.Count always divisible by two
            AnsiConsole.WriteLine($"Find proxies: {(matchCollection.Count / 2).ToString()}");
            AnsiConsole.Progress().Start(CreateTasks);

            void CreateTasks(ProgressContext context)
            {
                AnsiConsole.WriteLine("Start pinging...");
                var successBar = context.AddTask("Successfully pinged");
                var declineBar = context.AddTask("Can't connect or ping");
                var progressBar = context.AddTask("Progress");

                int ipAndPortsCount = matchCollection.Count / 2;
                double incrementValueForTick = 100.0 / ipAndPortsCount;
                
                var tasks = new Task<string>[ipAndPortsCount];
                for (int matchIndex = 0, taskIndex = 0; taskIndex < ipAndPortsCount; matchIndex += 2, taskIndex++)
                {
                    string ip = matchCollection[matchIndex].Value;
                    int port = Convert.ToInt32(matchCollection[matchIndex + 1].Value);
                    tasks[taskIndex] = Task.Run(() => CreatePingTask(ip, port));
                }
                
                using var streamWriter = new StreamWriter(App.Settings.Get<string>("outputPath"));
                foreach (Task<string> task in tasks)
                {
                    var taskResult = task.Result;
                    if (taskResult is not null)
                    {
                        streamWriter.WriteLine(taskResult);
                        successBar.Increment(incrementValueForTick);
                    }
                    else
                        declineBar.Increment(incrementValueForTick);
                    
                    progressBar.Increment(incrementValueForTick);
                }

                streamWriter.Close();

                Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
            }
        }

        private static async Task<string> CreatePingTask(string ip, int port)
        {
            TaskCompletionSource<string> taskCompletionSource = new();
            Socks5Client socks5Client = null;
            try
            {
                socks5Client = new Socks5Client(ip, port, PingAddress.Item1, PingAddress.Item2);
                socks5Client.OnConnectedEvent += (_, args) =>
                {
                    if (args.Client is null) return;
                    args.Client.ReceiveAsync();
                    args.Client.Send(Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {PingAddressEndpoint}\r\n\r\n"));
                };
                socks5Client.OnDataReceivedEvent += (_, _) =>
                {
                    taskCompletionSource.TrySetResult($"{ip}:{port.ToString()}");
                };
                socks5Client.ConnectAsync();
                taskCompletionSource.Task.Wait(TimeSpan.FromMilliseconds(App.Settings.Get<int>("timeoutMs")));
                if (taskCompletionSource.Task.IsCompleted)
                    return await taskCompletionSource.Task;
                
                taskCompletionSource.SetCanceled();
                return null;
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            finally
            {
                // HACK: Sometimes socks5Client.Connected throws exceptions
                try
                {
                    if (socks5Client is not null && socks5Client.Connected)
                        socks5Client.Client?.Disconnect();
                    socks5Client?.Client?.Dispose();
                }
                catch
                {
                    // ignored
                }
            }
            
            return null;
        }
    }
}