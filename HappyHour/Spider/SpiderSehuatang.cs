using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Interop;

using GalaSoft.MvvmLight.Messaging;

using CefSharp;
using FFmpeg.AutoGen;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;
using HappyHour.Model;

namespace HappyHour.Spider
{
    class SpiderSehuatang : SpiderBase
    {
        int _pageNum = 1;
        int _index = 0;
        bool _isPageChanged = false;
        bool _scrapRunning = false;
        string _currentPage = null;
        List<object> _articlesInPage = null;
        Dictionary<string, string> _xpathDic;

        public int NumPage { get; set; }  = 1;
        public List<string> Boards { get; set; }
        public string SelectedBoard { set; get; } = "censored";

        public SpiderSehuatang(SpiderViewModel browser) : base(browser)
        {
            Name = "sehuatang";
            URL = "https://www.sehuatang.org/";
            _xpathDic = new Dictionary<string, string>
            {
                { "censored",   XPath("//a[contains(., '亚洲有码原创')]/@href") },
                { "uncensored", XPath("//a[contains(., '亚洲无码原创')]/@href") },
                { "subtitle",   XPath("//a[contains(., '高清中文字幕')]/@href") },
                { "articles",   XPath("//tbody[contains(@id, 'normalthread_')]" +
                                    "/tr/td[1]/a/@href") },
                { "pid",  XPath("//span[@id='thread_subject']/text()") },
                { "date", XPath("(//em[contains(@id, 'authorposton')]" +
                                    "/span/@title)[1]") },
                { "files",  XPathClick("//a[contains(., '.torrent')]") },
                { "images", XPath("(//td[contains(@id, 'postmessage_')])[1]" +
                                    "//img[contains(@id, 'aimg_')]/@file") }
            };
            Boards = new List<string>
            {
                "censored", "uncensored", "subtitle"
            };
        }

        public override string GetConf(string key)
        {
            if (key == "DataPath")
            {
                return $"{App.GConf["general"]["data_path"]}sehuatang\\{SelectedBoard}\\";
            }
            else if (key == "StopOnExist")
            {
                return Browser.StopOnExistingId.ToString();
            }

            return base.GetConf(key);
        }

        void ParsePage()
        {
            string[] keys = { "pid", "date", "files", "images" };
            var item = new ItemSehuatang(this) { NumItemsToScrap = keys.Length  };
            var list = _xpathDic.Where(i => keys.Contains(i.Key));
            foreach (var xpath in list)
            {
                Browser.ExecJavaScript(xpath.Value, item, xpath.Key);
            }
        }

        string GetNextPage(string str)
        {
            var m = Regex.Match(str, @"-(?<page>\d+)\.html");
            if (!m.Success)
            {
                Log.Print("Invalid page url format! {0}", str);
                return null;
            }
            _pageNum = int.Parse(m.Groups["page"].Value) + 1;
            if (_pageNum > NumPage)
                return null;
            return Regex.Replace(str, @"\d+\.html", $"{_pageNum}.html");
        }

        void MovePage(object result)
        {
            _state = 1;
            _index = 0;
            _isPageChanged = true;
            if (result is List<object> items && items.Count == 1)
            {
                _currentPage = items[0].ToString();
            }
            else
            {
                _currentPage = GetNextPage(_currentPage);
            }

            if (!string.IsNullOrEmpty(_currentPage))
            {
                //Log.Print("Move Page to " + Browser.Address);
                Browser.Address = URL + _currentPage;
            }
        }

        public void MoveArticle(object result)
        {
            if (_isPageChanged)
            {
                _articlesInPage = result as List<object>;
                _isPageChanged = false;
            }
            MessengerInstance.Send(new NotificationMessage<string>(
                $"{_index}/{_articlesInPage.Count} {_pageNum}/{NumPage}",
                "UpdateStatus"));

            if (_articlesInPage.Count > _index)
            {
                _state = 2;
                string article = _articlesInPage[_index++].ToString();
                Browser.Address = URL + article;
            }
            else
            {
                MovePage(null);
            }
        }

        public override void OnScrapCompleted()
        {
            Browser.MediaList.AddMedia(DataPath);
            if(_scrapRunning)
                MoveArticle(null);
        }

        public override void Navigate2()
        {
            _state = 0;
            _pageNum = 1;
            _scrapRunning = true;
            Browser.Address = URL;
        }
        public override void Stop()
        {
            _scrapRunning = false;
        }

        public override void Scrap()
        {
            if (!_scrapRunning) return;

            switch (_state)
            {
                case 0:
                    Browser.ExecJavaScript(_xpathDic[SelectedBoard], MovePage);
                    break;
                case 1:
                    Browser.ExecJavaScript(_xpathDic["articles"], MoveArticle);
                    break;
                case 2:
                    ParsePage();
                    break;
            }
        }
    }
}
