using System.Timers;
using System.IO;
using System.Collections.Generic;

using CefSharp;

using HappyHour.ViewModel;
using HappyHour.Interfaces;

namespace HappyHour.Spider
{
    internal class DefaultDownloader : IDownloader
    {
        private int _numDownload;
        private int _numDownloaded;
        private bool _isEnabled;
        private readonly SpiderViewModel _browser;
        private readonly Dictionary<string, IDictionary<string, object>> _urls = new();
        private readonly Timer _timer;

        private SpiderBase _spider;
        private IDictionary<string, object> _items;

        public DefaultDownloader(SpiderViewModel spider)
        {
            _browser = spider;
            _timer = new Timer(5000)
            {
                AutoReset = false,
            };
            _timer.Elapsed += OnDownloadTimeout;
        }

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

        private void OnBeforeDownload(object sender, DownloadItem e)
        {
            if (!_urls.ContainsKey(e.OriginalUrl))
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
                var dic = _urls[e.OriginalUrl];
                var item = _spider.SearchMedia;
                if (dic.ContainsKey("cover"))
                {
                    e.SuggestedFileName = item.GenPosterPath(e.SuggestedFileName);
                    dic["cover"] = e.SuggestedFileName;
                }
                else if (dic.ContainsKey("thumb"))
                {
                    var path = item.GenActorThumbPath(dic["name"].ToString(), e.SuggestedFileName);
                    dic["thumb"] = Path.GetFileName(path);
                    e.SuggestedFileName = path;
                }
                else
                {
                    Log.Print("OnBeforeDownload: Cancel " + e.SuggestedFileName);
                    e.IsCancelled = true;
                }
            }
            //Log.Print("OnBeforeDownload: " + e.SuggestedFileName);
        }

        private void OnDownloadUpdated(object sender, DownloadItem e)
        {
            if (e.IsComplete)
            {
                _numDownloaded++;
                Log.Print($"{_spider.SearchMedia.Pid} : Download Completed: " +
                    $"({_numDownloaded}/{_numDownload}){e.FullPath}");
                lock (_timer)
                {
                    if (_numDownloaded == _numDownload)
                    {
                        _timer.Enabled = false;
                        UiServices.Invoke(() => _spider.UpdateItems(_items));
                    }
                }
            }
        }

        private void OnDownloadTimeout(object sender, ElapsedEventArgs e)
        {
            lock (_timer)
            {
                if (_numDownloaded == _numDownload) { return; }

                _ = SpiderBase.IterateDynamic(_items, (key, dic) =>
                {
                    if (key is "cover" or "thumb")
                    {
                        if (dic[key].ToString().StartsWith("http",
                            System.StringComparison.OrdinalIgnoreCase))
                        {
                            _ = dic.Remove(key);
                            Log.Print($"{_spider.Name}: Downloading is timed out! Removing {key}");
                        }
                    }
                    return false;
                });

                _numDownloaded = _numDownload = 0;
                UiServices.Invoke(() => _spider.UpdateItems(_items));
            }
        }

        public void Download(SpiderBase spider, IDictionary<string, object> items)
        {
            if (_numDownload != _numDownloaded)
            {
                Log.Print("download is ongoing...");
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
            _ = SpiderBase.IterateDynamic(items, (key, dict) =>
            {
                if (key is "cover" or "thumb") { _numDownload++; }
                return false;
            });

            Log.Print($"{spider.Name}: Num items to download: {_numDownload}");
            if (_numDownload > 0)
            {
                _ = SpiderBase.IterateDynamic(items, (key, dict) =>
                {
                    if (key is "cover" or "thumb")
                    {
                        string url = dict[key].ToString();
                        _urls.Add(url, dict);
                        _browser.Download(url);
                    }
                    return false;
                });
                _timer.Enabled = true;
            }
            else
            {
                spider.UpdateItems(items);
            }
        }
    }
}
