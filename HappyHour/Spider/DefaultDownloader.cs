using System;
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
        private readonly string _actorPicturePath;
        private readonly SpiderViewModel _browser;
        private readonly Dictionary<string, IDictionary<string, object>> _urls = new();

        private SpiderBase _spider;
        private IDictionary<string, object> _items;

        public DefaultDownloader(SpiderViewModel spider)
        {
            _browser = spider;
            _actorPicturePath = $"{App.LocalAppData}\\db";
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
            string ext = Path.GetExtension(e.SuggestedFileName);
            if (!_urls.ContainsKey(e.OriginalUrl))
            {
                Log.Print($"{e.OriginalUrl} not found in download list!");
                foreach (var url in _urls)
                {
                    Log.Print($"urls : {url.Key} : {url.Value}");
                }
                return;
            }

            var dic = _urls[e.OriginalUrl];
            if (dic.ContainsKey("cover"))
            {
                var item = _spider.SearchMedia;
                e.SuggestedFileName = $"{item.Path}\\{item.Pid}_poster{ext}";
                dic["cover"] = e.SuggestedFileName;
            }
            else
            {
                dic["thumb"] = $"{dic["name"].ToString().Replace(' ', '_')}{ext}";
                e.SuggestedFileName = $"{_actorPicturePath}\\{dic["thumb"]}";
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
                if (_numDownloaded == _numDownload)
                {
                    UiServices.Invoke(() => _spider.UpdateItems(_items));
                }
            }
        }

        public void Download(SpiderBase spider, IDictionary<string, object> items)
        {
            if (_numDownload != _numDownloaded)
            {
                Log.Print("download is ongoing...");
                return;
            }
            _numDownload = 0;
            _numDownloaded = 0;
            _urls.Clear();

            _spider = spider;
            _items = items;
            _ = SpiderBase.IterateDynamic(items, (key, dict) =>
            {
                if (key is "cover" or "thumb") { _numDownload++; }
                return false;
            });

            Log.Print($"{spider.Name}: _numDownload: {_numDownload}");
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
            }
            else
            {
                spider.UpdateItems(items);
            }
        }
    }
}
