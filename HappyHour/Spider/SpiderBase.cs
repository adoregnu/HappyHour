using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;

using Scriban;
using CefSharp;

using GalaSoft.MvvmLight.Command;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;
using HappyHour.Extension;

namespace HappyHour.Spider
{
    delegate void ScrapCompletedHandler(SpiderBase spider);

    abstract class SpiderBase : NotifyPropertyChanged
    {
        bool _isCookieSet = false;
        static string _keyword;

        public int ParsingState = -1;
        protected string _linkName;

        public SpiderViewModel Browser { get; private set; }
        public ScrapCompletedHandler ScrapCompleted { get; set; }

        public bool SaveDb { get; set; } = true;
        public string URL = null;
        public string Name { get; protected set; } = "";
        public string DataPath { get; set; }
        public virtual string SearchURL { get => URL; }

        public string Keyword
        {
            get => _keyword;
            set => Set(ref _keyword, value);
        }

        public ICommand CmdSearch { get; private set; }

        public SpiderBase(SpiderViewModel br)
        {
            Browser = br;
            CmdSearch = new RelayCommand(() => { SaveDb = false;  Navigate2(); });
        }

        public virtual void OnSelected()
        {
            Log.Print($"{Name} selected!");
        }

        public virtual void OnDeselect()
        { 
            Log.Print($"{Name} deselected!");
        }

        public virtual string GetConf(string key)
        {
            throw new Exception("Unknown Config");
        }

        static string XPath(string xpath, string jsPath)
        {
            Template template = Template.Parse(App.ReadResource(jsPath));
            string result = template.Render(new { XPath = xpath });
            return result;
        }

        protected string GetScript(string name)
        { 
            Template template = Template.Parse(App.ReadResource(name));
            return template.Render(new { Pid = Keyword });
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
            if (_isCookieSet)
            {
                return;
            }
            List<Cookie> cookies = CreateCookie();
            if (cookies == null)
            {
                return;
            }

            ICookieManager cookieManager = Cef.GetGlobalCookieManager();
            foreach (Cookie cookie in cookies)
            {
                cookieManager.SetCookieAsync(URL, cookie);
            }
            _isCookieSet = true;
        }

        public virtual void OnScrapCompleted()
        {
            Reset();
            if (ScrapCompleted != null)
            {
                UiServices.Invoke(() => ScrapCompleted?.Invoke(this), true);
            }
        }

        public void Reset()
        {
            if (ParsingState >= 0)
            {
                SaveDb = true;
                ParsingState = -1;
                //Keyword = null;
                //DataPath = null;
                Browser.MediaList.AddMedia(DataPath);
                Log.Print($"Reset Spider : {Name}");
            }
        }

        protected void ParsePage(IScrapItem item)
        {
            (item as ItemBase).Init();
            foreach ((string name, string element, ElementType type) in item.Elements)
            {
                if (type == ElementType.XPATH)
                    Browser.ExecJavaScript(XPath(element), item, name);
                else if (type == ElementType.XPATH_CLICK)
                    Browser.ExecJavaScript(XPathClick(element), item, name);
            }
            _linkName = null;
        }

        public abstract void Scrap();
        public virtual void Stop() { }
        public virtual void Download(string url, ref int itemToScrap)
        {
            _ = Interlocked.Increment(ref itemToScrap);
            Browser.Download(url);
        }

        public virtual void OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
        { 
        }

        static protected bool CheckResult(object result, out List<string> list)
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
