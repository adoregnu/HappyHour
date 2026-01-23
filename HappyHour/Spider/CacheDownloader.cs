using AvalonDock.Properties;
using HappyHour.CefHandler;
using HappyHour.Interfaces;
using HappyHour.Model;
using HappyHour.ViewModel;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace HappyHour.Spider
{
    class CacheDownloader : IDownloader
    {
        public void Enable(bool bEnable) { }

        readonly List<string> _resources = [];
        readonly List<string> _toDownload = [];

        private IDictionary<string, object> _items;

        public void ClearCache()
        {
            Log.Print($"ClearCache :{_resources.Count}");
            foreach (var r in _resources)
            {
                // delete cached file
                try { File.Delete(r); } catch { }
                //Log.Print($"Deleted cached file: {r}");
            }
            _resources.Clear();
        }
        public async Task UpdateDownload(SpiderBase spider, string rpath)
        {
            UiServices.Invoke(async ()  =>
            {
                _resources.Add(rpath);
                await CheckDownload(spider);
            });
        }

        private async Task CheckDownload(SpiderBase spider, bool isCallerDownload = false)
        {
            if (_toDownload.Count == 0 && !isCallerDownload) return;
            Log.Print($"Cache: {_resources.Count}, Download: {_toDownload.Count}");
            if (_toDownload.Count > 0)
            {
                _resources.ForEach(r =>
                {
                    _toDownload.Remove(r);
                });
            }
            if (_toDownload.Count == 0)
            {
                await spider.UpdateItemsAsync(_items);
            }
        }
        public async Task Download(SpiderBase spider, IDictionary<string, object> items)
        {
            _toDownload.Clear();

            List<string> urls = ["cover", "screenshot"];
            _items = items;

            foreach (var url in urls)
            {
                if (!items.TryGetValue(url, out object urlobj) || urlobj == null) continue;

                if (urlobj is List<object> screenshots)
                {
                    for (int i = 0; i < screenshots.Count; i++)
                    {
                        var imgpath = AvImageFilter.GetResourcePath(screenshots[i].ToString());
                        screenshots[i] = imgpath;
                        _toDownload.Add(imgpath);
                    }
                }
                else
                {
                    var imgpath = AvImageFilter.GetResourcePath(urlobj.ToString());
                    items[url] = imgpath;
                    _toDownload.Add(imgpath);
                }
            }
            await CheckDownload(spider, true);
        }
    }
}
