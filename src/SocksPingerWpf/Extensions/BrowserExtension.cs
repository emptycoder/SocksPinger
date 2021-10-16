using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Wpf;

namespace SocksPingerWpf.Extensions
{
    public static class BrowserExtension
    {
        private static readonly Regex IpPortRegex = new(App.Settings.Get<string>("matchRegex"), 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static async Task<MatchCollection> ParseOrGetFromCacheAsync(this ChromiumWebBrowser browser, string cachePath)
        {
            var path = Path.Combine(cachePath, string.Concat("cache_", DateTime.UtcNow.ToString("MM-dd-yy"), ".html"));
            if (App.Settings.Get<bool>("isCacheRequests") && File.Exists(path)) 
                return IpPortRegex.Matches(await File.ReadAllTextAsync(path));

            await browser.LoadUrlAsync(App.Settings.Get<string>("serviceAddress"));
            TaskCompletionSource<(MatchCollection, string)> taskCompletionSource = new();
            EventHandler<FrameLoadEndEventArgs> frameLoadEnd = BrowserOnFrameLoadEnd;
            browser.FrameLoadEnd += frameLoadEnd;
            // HACK: Can't send POST request on server using Browser.LoadUrlWithPostData
            browser.ExecuteScriptAsyncWhenPageLoaded(App.Settings.Get<string>("executedJsOnStart"));

            async void BrowserOnFrameLoadEnd(object sender, FrameLoadEndEventArgs e)
            {
                var source = await browser.GetSourceAsync();
            
                if (string.IsNullOrWhiteSpace(source)) return;
                var matches = IpPortRegex.Matches(source);
                var count = matches.Count;
            
                if (count != App.Settings.Get<int>("matchesCount")) return;
                
                taskCompletionSource.SetResult((matches, source));
            }

            var (matchCollection, result) = await taskCompletionSource.Task;
            browser.FrameLoadEnd -= frameLoadEnd;
            await File.WriteAllTextAsync(path, result);
            
            return matchCollection;
        }
    }
}