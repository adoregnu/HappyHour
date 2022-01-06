using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CefSharp;

using HappyHour.Interfaces;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    internal class ShtDownloader : IDownloader
    {
        private readonly SpiderViewModel _browser;
        private readonly Dictionary<string, string> _images = new();
        private SpiderBase _spider;
        private IDictionary<string, object> _items;
        private DateTime _updateTime;

        private string _outPath;
        private int _numDownloaded;
        private int _toDownload;

        public ShtDownloader(SpiderViewModel browser)
        {
            _browser = browser;
        }
        public void Enable(bool bEnable)
        {
            if (bEnable)
            {
                _browser.DownloadHandler.OnBeforeDownloadFired += OnBeforeDownload;
                _browser.DownloadHandler.OnDownloadUpdatedFired += OnDownloadUpdated;
            }
            else
            {
                _browser.DownloadHandler.OnBeforeDownloadFired -= OnBeforeDownload;
                _browser.DownloadHandler.OnDownloadUpdatedFired -= OnDownloadUpdated;
            }
        }

        private void OnBeforeDownload(object sender, DownloadItem e)
        {
            e.SuggestedFileName = !e.SuggestedFileName.EndsWith("torrent", StringComparison.OrdinalIgnoreCase) ?
                _images[e.OriginalUrl] : $"{_outPath}\\{e.SuggestedFileName}";
        }
        private void OnDownloadUpdated(object sender, DownloadItem e)
        {
            if (!e.IsComplete)
            {
                return;
            }

            _numDownloaded++;
            Log.Print($"download completed({_numDownloaded}/{_toDownload}): {e.FullPath}");
            try
            {
                File.SetLastWriteTime(e.FullPath, _updateTime);
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
            if (_toDownload == _numDownloaded)
            {
                UiServices.Invoke(() => _spider.UpdateItems(_items));
            }
        }

        public static bool CreateDir(string path)
        {
            DirectoryInfo di = new(path);
            if (!di.Exists)
            {
                Directory.CreateDirectory(path);
                return true;
            }
            return false;
        }

        public void Download(SpiderBase spider, IDictionary<string, object> items)
        { 
            if (_toDownload != _numDownloaded)
            {
                Log.Print($"Previous downloading is not completed!");
                return;
            }
            _spider = spider;
            _items = items;

            _images.Clear();
            _toDownload = 0;
            _numDownloaded = 0;
            
            _updateTime = DateTime.Parse(items["date"].ToString());
            _outPath = @$"{_spider.SearchMedia.Path}\{items["pid"]}";
            if (!CreateDir(_outPath))
            {
                Log.Print($"Already download! {_outPath}");
                return;
            }

            if (items.ContainsKey("images") && items["images"] is List<object> images)
            {
                _toDownload = images.Count;
            }
            else
            {
                images = null;
            }
            if (items.ContainsKey("files") && items["files"] is List<object> files)
            {
                files.ForEach(f =>
                {
                    if (f is IJavascriptCallback cb)
                    {
                        _toDownload++;
                        cb.ExecuteAsync();
                    }
                });
            }

            int i = 0;
            foreach (string file in images)
            {
                string postfix = (i == 0) ? "cover" : $"screenshot{i}";
                string target = $"{_outPath}\\{items["pid"]}_{postfix}{Path.GetExtension(file)}";

                _images.Add(file, target);
                _browser.Download(file);
            }
        }
    }
}
