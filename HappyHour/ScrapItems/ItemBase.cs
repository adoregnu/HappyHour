using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Globalization;

using CefSharp;

using HappyHour.Model;
using HappyHour.Spider;

namespace HappyHour.ScrapItems
{
    abstract class ItemBase
    {
        public static CultureInfo enUS = new CultureInfo("en-US");

        readonly protected SpiderBase _spider;
        protected int _numItemsToScrap = 0;

        public int NumItemsToScrap
        {
            get => _numItemsToScrap;
            set
            {
                _numItemsToScrap = value;
                _numScrapedItem = 0;
                _numValidItems = 0;
            }
        }

        protected int _numScrapedItem = 0;
        protected int _numValidItems = 0;

        public ItemBase(SpiderBase spider)
        {
            _spider = spider;
            var dh = _spider.Browser.DownloadHandler;
            dh.OnBeforeDownloadFired += OnBeforeDownload;
            dh.OnDownloadUpdatedFired += OnDownloadUpdated;
        }

        protected virtual void OnBeforeDownload(object sender, DownloadItem e) { }
        protected virtual void OnDownloadUpdated(object sender, DownloadItem e) { }

        protected virtual void Clear()
        {
            var dh = _spider.Browser.DownloadHandler;
            dh.OnBeforeDownloadFired -= OnBeforeDownload;
            dh.OnDownloadUpdatedFired -= OnDownloadUpdated;
            Log.Print("ItemBase::Clear()");
        }

        protected void PrintItem(string name, List<object> items)
        {
            Log.Print("{0} : scrapped {1}", name,
                items != null ? items.Count : 0);
            if (items == null) return;
            foreach (string it in items)
            {
                Log.Print($"\t{name}: {it.Trim()}");
            }
        }
    }
}
