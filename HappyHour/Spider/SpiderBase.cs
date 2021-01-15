using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Command;

using Scriban;
using CefSharp;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;
using HappyHour.Model;
using System.Threading;

namespace HappyHour.Spider
{
    delegate void OnJsResultSingle(object items);
    delegate void OnJsResult(List<object> items);
    delegate void ScrapCompletedHandler(SpiderBase spider);

    abstract class SpiderBase : ViewModelBase
    {
        bool _isCookieSet = false;
        string _keyword;

        public SpiderViewModel Browser { get; private set; }
        public string URL = null;
        public string Name { get; protected set; } = "";
        public virtual string SearchURL { get => URL; }
        public bool IsRunning
        {
            get => !string.IsNullOrEmpty(Keyword);
        }

        public string Keyword
        {
            get => _keyword;
            set
            {
                Set(ref _keyword, value);
                RaisePropertyChanged(nameof(IsRunning));
            }
        }
        public string DataPath { get; set; }

        public ScrapCompletedHandler ScrapCompleted { get; set; }

        protected int _state = -1;
        protected string _linkName;

        public SpiderBase(SpiderViewModel br)
        {
            Browser = br;
        }

        public virtual string GetConf(string key)
        {
            throw new Exception("Unknown Config");
        }

        string XPath(string xpath, string jsPath)
        {
            var template = Template.Parse(App.ReadResource(jsPath));
            var result = template.Render(new { XPath = xpath });
            return result;
        }

        protected string XPath(string xpath)
        { 
            return XPath(xpath, @"XPathMulti.sbn.js");
        }
        protected string XPathClick(string xpath)
        { 
            return XPath(xpath, @"XPathClick.sbn.js");
        }

        public virtual void Navigate2()
        {
            if (string.IsNullOrEmpty(Keyword))
            {
                Log.Print("Empty keyword!");
                return;
            }
            _state = 0;
            Browser.SelectedSpider = this;
        }

        public virtual void Navigate(string name, string url)
        {
            _linkName = name;
            Browser.Address = url;
        }

        public virtual List<Cookie> CreateCookie() { return null; }

        public void SetCookies()
        {
            if (_isCookieSet) return;
            var cookies = CreateCookie();
            if (cookies == null) return;

            var cookieManager = Cef.GetGlobalCookieManager();
            foreach (var cookie in cookies)
            {
                cookieManager.SetCookieAsync(URL, cookie);
            }
            _isCookieSet = true;
        }

        public virtual void OnScrapCompleted()
        {
            if (ScrapCompleted != null)
                UiServices.Invoke(delegate { ScrapCompleted?.Invoke(this); }, true);
            else
                Reset();
        }

        public void Reset()
        { 
            Keyword = null;
            DataPath = null;
            Log.Print($"Reset Spider : {Name}");
        }

        protected void ParsePage(IScrapItem item, Dictionary<string, string> dic)
        {
            if (string.IsNullOrEmpty(DataPath))
                return;

            foreach (var xpath in dic )
            {
                Browser.ExecJavaScript(xpath.Value, item, xpath.Key);
            }
            _linkName = null;
        }

        public abstract void Scrap();
        public virtual void Stop() { }
        public virtual void Download(string url, ref int itemToScrap)
        {
            Interlocked.Increment(ref itemToScrap);
            Browser.Download(url);
        }

        public override string ToString()
        {
            return URL;
        }
    }
}
