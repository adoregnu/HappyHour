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
    using ElementTupleList = List<(string name, string element, ElementType type)>;
    abstract class ItemBase : IScrapItem
    {
        readonly protected SpiderBase _spider;
        ElementTupleList _elements;
        protected int _numItemsToScrap = 0;
        bool _isHandlerInit = false;

        public static CultureInfo enUS = new CultureInfo("en-US");
        public ElementTupleList Elements
        {
            get => _elements;
            set
            {
                if (value == null) return;
                _elements = value;
                NumItemsToScrap = value.Count;
            }
        }

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
        }

        protected virtual void OnBeforeDownload(object sender, DownloadItem e) { }
        protected virtual void OnDownloadUpdated(object sender, DownloadItem e) { }

        public virtual void Init()
        {
            if (!_isHandlerInit)
            {
                var dh = _spider.Browser.DownloadHandler;
                dh.OnBeforeDownloadFired += OnBeforeDownload;
                dh.OnDownloadUpdatedFired += OnDownloadUpdated;
                _isHandlerInit = true;
            }
        }

        public virtual void Clear()
        {
            var dh = _spider.Browser.DownloadHandler;
            dh.OnBeforeDownloadFired -= OnBeforeDownload;
            dh.OnDownloadUpdatedFired -= OnDownloadUpdated;
            _spider.OnScrapCompleted();
            Log.Print("ItemBase::Clear()");
        }

        static protected void PrintItem(string name, List<object> items)
        {
            Log.Print("{0} : scrapped {1}", name,
                items != null ? items.Count : 0);
            if (items == null) return;
            foreach (string it in items)
            {
                Log.Print($"\t{name}: {it.Trim()}");
            }
        }

        public abstract void OnJsResult(string name, List<object> items);
    }
}
