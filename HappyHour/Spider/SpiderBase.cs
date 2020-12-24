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

namespace HappyHour.Spider
{
    delegate void OnJsResult(List<object> items);

    abstract class SpiderBase : ViewModelBase
    {
        public SpiderViewModel Browser { get; private set; }
        public MediaItem Media = null;
        public string URL = null;
        public string Name { get; protected set; } = "";
        public bool FromCommand { get; private set; } = false;
        protected int _state = -1;
        protected string _linkName;

        public ICommand CmdScrap { get; set; }

        public SpiderBase(SpiderViewModel br)
        {
            Browser = br;
            CmdScrap = new RelayCommand<object>((p) => {
                FromCommand = true;
                Browser.SelectedSpider = this;
                if (p is IList<object> items && items.Count > 0)
                {
                    Browser.StartBatchedScrapping(
                        items.Cast<MediaItem>().ToList());
                }
                FromCommand = false;
            });
        }

        string XPath(string xpath, string jsPath)
        {
            var template = Template.Parse(App.ReadResource(jsPath));
            var result = template.Render(new { XPath = xpath });
            //Log.Print(result);
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

        public virtual void Navigate(MediaItem mitem)
        {
            _state = 0;
            Media = mitem;
        }

        public virtual void Navigate(string name, string url)
        {
            _linkName = name;
            Browser.Address = url;
        }

        public virtual List<Cookie> CreateCookie() { return null; }

        bool _isCookieSet = false;
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

        public virtual void OnScrapCompleted(bool isValid, string path = null)
        {
            Browser.StopScrapping(Media);
        }

        protected void ParsePage(IScrapItem item, Dictionary<string, string> dic)
        {
            foreach (var xpath in dic )
            {
                Browser.ExecJavaScript(xpath.Value, item, xpath.Key);
            }
            _linkName = null;
        }

        public abstract void Scrap();

        public override string ToString()
        {
            return URL;
        }
    }
}
