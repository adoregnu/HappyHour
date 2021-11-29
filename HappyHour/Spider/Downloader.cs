using System.IO;
using System.Collections.Generic;

using CefSharp;

using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    internal class Downloader
    {
        private int _numDownloading;
        private int _numDownloadDone;
        private bool _downloadOngoing;
        private bool _isEnabled;
        private readonly string _actorPicturePath;
        private readonly SpiderViewModel _browser;
        private readonly Dictionary<string, IDictionary<string, object>> _urls = new();

        private SpiderBase _spider;
        private IDictionary<string, object> _items;

        public Downloader(SpiderViewModel spider)
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
            else if(_isEnabled && !bEnable)
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
                e.SuggestedFileName =  $"{_spider.DataPath}\\{_spider.Keyword}_poster{ext}";
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
                Log.Print($"Download Completed: {e.FullPath}");
                _numDownloadDone++;
                if (_downloadOngoing == false && _numDownloadDone == _numDownloading)
                {
                    _spider.UpdateItems(_items);
                }
            }
        }

        private void Download(string url, IDictionary<string, object> item)
        {
            _numDownloading++;
            _urls.Add(url, item);
            _browser.Download(url);
        }

        public void DownloadFiles(SpiderBase spider, IDictionary<string, object> items)
        {
            if (_numDownloading != _numDownloadDone || _downloadOngoing)
            {
                Log.Print("download is ongoing...");
                return;
            }
            _numDownloading = 0;
            _numDownloadDone = 0;
            _downloadOngoing = true;
            _urls.Clear();

            _spider = spider;
            _items = items;

            foreach (var item in items)
            {
                if (item.Value == null)
                {
                    continue;
                }
                Log.Print($"{spider.Keyword} : {item.Key} = {item.Value?.ToString()}");
                if (item.Key == "cover")
                {
                    Download(item.Value.ToString(), items);
                }
                else if (item.Key == "actor")
                {
                    var actors = (List<object>)item.Value;
                    foreach (IDictionary<string, object> actor in actors)
                    {
                        if (actor.ContainsKey("thumb"))
                        {
                            Download(actor["thumb"].ToString(), actor);
                        }
                    }
                }
            }
            if (_numDownloading == 0)
            {
                spider.UpdateItems(items);
            }
            _downloadOngoing = false;
        }
    }
}
