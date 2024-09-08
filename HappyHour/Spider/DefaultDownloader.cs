using System.Timers;
using System.IO;
using System.Collections.Generic;

using CefSharp;

using HappyHour.ViewModel;
using HappyHour.Interfaces;
using System.Windows;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using HappyHour.Model;

namespace HappyHour.Spider
{
    internal class DefaultDownloader : IDownloader
    {
        private int _numDownload;
        private int _numDownloaded;
        private bool _isEnabled;
        private readonly SpiderViewModel _browser;
        private readonly Dictionary<string, (string, IDictionary<string, object>)> _urls = new();
        private readonly Timer _timer;

        private SpiderBase _spider;
        private IDictionary<string, object> _items;

        public DefaultDownloader(SpiderViewModel spider)
        {
            _browser = spider;
            _timer = new Timer(10000)
            {
                AutoReset = false,
            };
            _timer.Elapsed += OnDownloadTimeout;
        }
        public async Task UpdateDownload(SpiderBase spider,string rpath) { }

        public void Enable(bool bEnable)
        {
            if (!_isEnabled && bEnable)
            {
                _browser.DownloadHandler.OnBeforeDownloadFired += OnBeforeDownload;
                _browser.DownloadHandler.OnDownloadUpdatedFired += OnDownloadUpdated;
                _isEnabled = true;
            }
            else if (_isEnabled && !bEnable)
            {
                _browser.DownloadHandler.OnBeforeDownloadFired -= OnBeforeDownload;
                _browser.DownloadHandler.OnDownloadUpdatedFired -= OnDownloadUpdated;
                _isEnabled = false;
            }
        }

        private static string GetTempFileName(string orgName)
        {
            return $@"{Path.GetTempPath()}/{Path.GetFileName(orgName)}";
        }
        private void OnBeforeDownload(object sender, DownloadItem e)
        {
            if (!_urls.TryGetValue(e.OriginalUrl, out var dict))
            {
                Log.Print($"{e.OriginalUrl} not found in download list!");
                foreach (var url in _urls)
                {
                    Log.Print($"urls : {url.Key} : {url.Value}");
                }
                e.IsCancelled = true;
                return;
            }

            lock (_timer)
            {
                var movie = _spider.SearchMedia;
                if (dict.Item1 == "cover")
                {
                    e.SuggestedFileName = GetTempFileName(e.SuggestedFileName);
                    dict.Item2["cover"] = e.SuggestedFileName;
                }
                else if (dict.Item1 == "screenshot")
                {
                    e.SuggestedFileName = movie.GenPosterPath(e.SuggestedFileName, true);
                    dict.Item2["screenshot"] = e.SuggestedFileName;
                }
                else if (dict.Item1 == "thumb")
                {
                    e.SuggestedFileName = GetTempFileName(e.SuggestedFileName);
                    dict.Item2["thumb"] = e.SuggestedFileName;
                }
                else
                {
                    Log.Print("OnBeforeDownload: Cancel " + e.SuggestedFileName);
                    e.IsCancelled = true;
                }
                if (File.Exists(e.SuggestedFileName))
                {
                    File.Delete(e.SuggestedFileName);
                }
            }
            //Log.Print("OnBeforeDownload: " + e.SuggestedFileName);
        }

        private void OnDownloadUpdated(object sender, DownloadItem e)
        {
            if (e.IsComplete)
            {
                lock (_timer)
                {
                    _numDownloaded++;
                    if (_numDownloaded != _numDownload)
                    {
                        return;
                    }
                    _timer.Stop();
                }
                Log.Print($"{_spider.SearchMedia.Pid} : Download Completed: " +
                    $"({_numDownloaded}/{_numDownload}){e.FullPath}");

                Application.Current.Dispatcher.InvokeAsync(async () => await _spider.UpdateItemsAsync(_items));
            }
        }

        readonly List<string> _item2download = ["cover", "thumb", "screenshot", "picture"];
        private void OnDownloadTimeout(object sender, ElapsedEventArgs e)
        {
            lock (_timer)
            {
                if (_numDownloaded == _numDownload) { return; }

                _ = SpiderBase.IterateDynamic(_items, (key, dic) =>
                {
                    if (_item2download.Contains(key))
                    {
                        if (dic[key].ToString().StartsWith("http", System.StringComparison.OrdinalIgnoreCase))
                        {
                            _ = dic.Remove(key);
                            Log.Print($"{_spider.Name}: Downloading is timed out! Removing {key}");
                        }
                    }
                    return false;
                });
                _numDownloaded = _numDownload = 0;
            }

            //Application.Current.Dispatcher.InvokeAsync(async () => await _spider.UpdateItems(_items));
        }

        public async Task Download(SpiderBase spider, IDictionary<string, object> items)
        {
            if (_numDownload != _numDownloaded)
            {
                Log.Print($"download is ongoing... _numDownload:{_numDownload} != _numDownloaded:{_numDownloaded}");
                return;
            }
            if (spider == null || spider.SearchMedia == null)
            {
                Log.Print("No Movie selected!");
                return;
            }
            _numDownload = 0;
            _numDownloaded = 0;
            _urls.Clear();
            _timer.Stop();

            _spider = spider;
            _items = items;
            SpiderBase.IterateDynamic(items, (key, dict) =>
            {
                if (_item2download.Contains(key)) { _numDownload++; }
                return false;
            });

            Log.Print($"{spider.Name}: Num items to download: {_numDownload}");
            if (_numDownload > 0)
            {
                SpiderBase.IterateDynamic(items, (key, dict) =>
                {
                    if (_item2download.Contains(key))
                    {
                        if (dict[key] is IDictionary<string, object> popupimage)
                        {
                            if (popupimage.TryGetValue("func", out object callback))//.ExecuteAsync();
                            {
                                string url = popupimage["img_url"].ToString();
                                _urls.Add(url, (key, dict));

                                ((IJavascriptCallback)callback).ExecuteAsync();//.ContinueWith((response) => { });
                            }
                        }
                        else
                        {
                            string url = dict[key].ToString();
                            _urls.Add(url, (key, dict));
                            _browser.Download(url);
                        }
                    }
                    return false;
                });
                _timer.Enabled = true;
            }
            else
            {
                await spider.UpdateItemsAsync(items);
            }
        }
    }
}
