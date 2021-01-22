using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Scriban;
using CefSharp;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;
using HappyHour.Extension;

namespace HappyHour.Spider
{
    delegate void ScrapCompletedHandler(SpiderBase spider);

    abstract class SpiderBase : NotifyPropertyChanged
    {
        bool _isCookieSet = false;
        string _keyword;

        protected int ParsingState = -1;
        protected string _linkName;

        public SpiderViewModel Browser { get; private set; }
        public ScrapCompletedHandler ScrapCompleted { get; set; }

        public string URL = null;
        public string Name { get; protected set; } = "";
        public string DataPath { get; set; }
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

        public SpiderBase(SpiderViewModel br)
        {
            Browser = br;
        }

        public virtual string GetConf(string key)
        {
            throw new Exception("Unknown Config");
        }

        static string XPath(string xpath, string jsPath)
        {
            var template = Template.Parse(App.ReadResource(jsPath));
            var result = template.Render(new { XPath = xpath });
            return result;
        }

        public static string XPath(string xpath)
        { 
            return XPath(xpath, @"XPathMulti.sbn.js");
        }
        public static string XPathClick(string xpath)
        { 
            return XPath(xpath, @"XPathClick.sbn.js");
        }
        public static string XPathClickSingle(string xpath)
        { 
            return XPath(xpath, @"XPathClickSingle.sbn.js");
        }

        public virtual void Navigate2()
        {
            if (string.IsNullOrEmpty(Keyword))
            {
                Log.Print("Empty keyword!");
                return;
            }
            ParsingState = 0;
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
                UiServices.Invoke(() => ScrapCompleted?.Invoke(this), true);
            else
                Reset();
        }

        public void Reset()
        {
            ParsingState = -1;
            Keyword = null;
            DataPath = null;
            Log.Print($"Reset Spider : {Name}");
        }

        protected void ParsePage(IScrapItem item, Dictionary<string, string> dic)
        {
            if (string.IsNullOrEmpty(DataPath))
            {
                OnScrapCompleted();
                return;
            }

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

        protected bool CheckResult(object result, out List<string> list)
        {
            list = result.ToList<string>();
            if (list.IsNullOrEmpty())
            {
                Log.Print("CheckResult: result is empty or null!");
                return false;
            }
            Log.Print($"CheckResult: {list.Count} items found!");
            return true;
        }

        public override string ToString()
        {
            return URL;
        }
    }
}
