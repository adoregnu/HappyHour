using AvalonDock.Properties;
using HappyHour.CefHandler;
using HappyHour.Interfaces;
using HappyHour.Model;
using HappyHour.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Spider
{
    class CacheDownloader : IDownloader
    {
        public void Enable(bool bEnable) { }

        readonly List<string> _resources = [];
        readonly List<string> _toDownload = [];

        private IDictionary<string, object> _items;

        public async Task UpdateDownload(SpiderBase spider, string rpath)
        {
            _resources.Add(rpath);
            await CheckDownload(spider);
        }

        private async Task CheckDownload(SpiderBase spider)
        {
            if (_toDownload.Count == 0) return;

            _resources.ForEach(r => {
                _toDownload.Remove(r);
            });
            Log.Print($"to download  : {_toDownload.Count}");
            if (_toDownload.Count == 0)
            {
                await spider.UpdateItemsAsync(_items);
                _resources.Clear();
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
            await CheckDownload(spider);
        }
    }
}
