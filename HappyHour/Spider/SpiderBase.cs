using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Globalization;

using Scriban;
using CefSharp;

using GalaSoft.MvvmLight.Command;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;

namespace HappyHour.Spider
{
    delegate void ScrapCompletedHandler(SpiderBase spider);

    class SpiderBase : NotifyPropertyChanged
    {
        static string _keyword;
        public static CultureInfo enUS = new("en-US");

        bool _isCookieSet = false;
        public int ParsingState = -1;

        public SpiderViewModel Browser { get; private set; }
        public ScrapCompletedHandler ScrapCompleted { get; set; }

        public bool SaveDb { get; set; } = true;
        public string URL = null;
        public string Name { get; protected set; } = "";
        public string DataPath { get; set; }
        public string ScriptName { get; set; }
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
            Browser.ImageDownloader.Enable(true);
        }

        public virtual void OnDeselect()
        { 
            Log.Print($"{Name} deselected!");
            Browser.ImageDownloader.Enable(false);
        }

        protected virtual string GetScript(string name)
        { 
            Template template = Template.Parse(App.ReadResource(name));
            return template.Render(new { Pid = Keyword });
        }

        public virtual void Navigate2()
        {
            if (string.IsNullOrEmpty(Keyword))
            {
                Log.Print($"{Name}: Empty keyword!");
                return;
            }
            ParsingState = 0;
            Browser.SelectedSpider = this;
        }


        protected virtual List<Cookie> CreateCookie() { return null; }

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

        protected virtual void OnScrapCompleted()
        {
            if (ParsingState >= 0)
            {
                SaveDb = true;
                ParsingState = -1;
                //Keyword = null;
                //DataPath = null;
                Log.Print($"{Name}: Reset Spider");
            }

            if (ScrapCompleted != null)
            {
                UiServices.Invoke(() => ScrapCompleted?.Invoke(this), true);
            }
        }

        public virtual void Scrap()
        {
            if (ParsingState >= 0 && !string.IsNullOrEmpty(ScriptName))
            {
                Browser.ExecJavaScript(GetScript(ScriptName));
                return;
            }
        }

        public virtual void OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
        {
            dynamic d = msg.Message;
            //Log.Print($"{d.type} : {d.data}");
            if (d.type == "url")
            {
                Browser.Address = d.data;
            }
            else if (d.type == "items")
            {
                if (d.data == 0)
                {
                    Log.Print($"{Name}: No exact matched ID");
                    return;
                }
                try
                {
                    Browser.ImageDownloader.DownloadFiles(this, d);
                }
                catch (Exception ex)
                {
                    Log.Print($"{Name}:", ex);
                }
            }
        }

        protected virtual void UpdateDb(IDictionary<string, object> items)
        {
            new ItemBase2(this).UpdateItems(items);
        }

        public void UpdateItems(IDictionary<string, object> items)
        {
            if (SaveDb)
            {
                UiServices.Invoke(() => UpdateDb(items));
            }
            OnScrapCompleted();
        }

        public override string ToString()
        {
            return URL;
        }
    }
}
