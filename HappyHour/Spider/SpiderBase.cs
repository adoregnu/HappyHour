using System;
using System.Collections.Generic;
using System.Windows.Input;

using Scriban;
using CefSharp;

using GalaSoft.MvvmLight.Command;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;
using HappyHour.Model;

namespace HappyHour.Spider
{
    internal delegate void ScrapCompletedHandler(SpiderBase spider);

    internal class SpiderBase : NotifyPropertyChanged
    {
        private static string _keyword;
        private bool _isCookieSet;
        private MediaItem _selectedMedia;

        public int ParsingState = -1;

        public SpiderViewModel Browser { get; private set; }
        public ScrapCompletedHandler ScrapCompleted { get; set; }
        public MediaItem SelectedMedia
        {
            get => _selectedMedia;
            set
            {
                if (value != null)
                {
                    Keyword = value.Pid;
                }
                _selectedMedia = value;
            }
        }
        public MediaItem SearchMedia { get; set; }

        public string URL;
        public string Name { get; protected set; } = "Base";
        public string ScriptName { get; set; }
        public virtual string SearchURL => URL;

        public string Keyword
        {
            get => _keyword;
            set => Set(ref _keyword, value);
        }

        public ICommand CmdSearch { get; private set; }

        public SpiderBase(SpiderViewModel br)
        {
            Browser = br;
            CmdSearch = new RelayCommand(() => { Navigate2(); });
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

        public virtual void Navigate2(MediaItem searchMedia = null)
        {
            if (string.IsNullOrEmpty(Keyword) && searchMedia == null)
            {
                Log.Print($"{Name}: Empty keyword!");
                return;
            }
            SearchMedia = searchMedia;
            Browser.SelectedSpider = this;
        }

        protected virtual void OnScrapCompleted(bool bUpdated)
        {
            if (ParsingState >= 0)
            {
                ParsingState = -1;
                Log.Print($"{Name}: Reset Spider");
            }

            if (bUpdated && SearchMedia != null)
            {
                SearchMedia.ReloadAvItem();
            }
            SearchMedia = null;
            ScrapCompleted?.Invoke(this);
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
                    UiServices.Invoke(() => OnScrapCompleted(false));
                    return;
                }
                try
                {
                    if (SearchMedia != null)
                    {
                        Browser.ImageDownloader.DownloadFiles(this, d);
                    }
                }
                catch (Exception ex)
                {
                    Log.Print($"{Name}:", ex);
                }
            }
        }

        protected virtual void UpdateDb(IDictionary<string, object> items)
        {
            try
            {
                new ItemBase2(SearchMedia).UpdateItems(items);
            }
            catch (Exception ex)
            {
                Log.Print($"{Name}: UpdateDb failed.", ex);
            }
        }

        public void UpdateItems(IDictionary<string, object> items)
        {
            UiServices.Invoke(() =>
            {
                UpdateDb(items);
                OnScrapCompleted(true);
            });
        }

        public override string ToString()
        {
            return URL;
        }
    }
}
