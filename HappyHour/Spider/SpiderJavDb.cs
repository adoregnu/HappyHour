using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.Model;
using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavDb : SpiderBase
    {
        readonly Dictionary<string, string> _xpathDic;
        public SpiderJavDb(SpiderViewModel browser) : base(browser)
        { 
            Name = "JavDB";
            URL = "https://javdb.com/";

            _xpathDic = new Dictionary<string, string>
            {
                { "title", XPath("//h2[contains(@class, 'title')]/strong/text()") },
                { "cover", XPath("//div[@class='column column-video-cover']/a/@href") },
                { "pid", XPath("//nav[@class='panel video-panel-info']/div[1]/span/text()") },
                { "date", XPath("//nav[@class='panel video-panel-info']/div[2]/span/text()") },
                { "genre", XPath("//nav[@class='panel video-panel-info']/div[7]/span/a/text()") },
                { "actor", XPath("//nav[@class='panel video-panel-info']/div[8]/span//text()") },
            };
        }

        public override List<Cookie> CreateCookie()
        {
            return new List<Cookie>
            {
                new Cookie {
                    Name = "over18",
                    Value = "1",
                    Domain = "javdb.com",
                    Path = "/"
                },
                new Cookie { 
                    Name = "locale",
                    Value = "en",
                    Domain = "javdb.com",
                    Path = "/"
                },
            };
        }

        void OnMultiResult(List<object> list)
        { 
            Log.Print($"OnMultiResult : {list.Count} items found!");
            if (list == null || list.Count == 0)
            {
                Browser.StopScrapping(Media);
                return;
            }
            HtmlDocument doc = new HtmlDocument();
            HtmlNode a = null;
            foreach (string it in list)
            {
                doc.LoadHtml(it);
                var node = doc.DocumentNode.SelectSingleNode("//div[@class='uid']");
                var pid = node.InnerText.Trim();
                if (pid.Equals(Media.Pid, StringComparison.OrdinalIgnoreCase))
                {
                    a = doc.DocumentNode.FirstChild;
                    break;
                }
            }
            if (a == null)
            {
                Browser.StopScrapping(Media);
                return;
            }
            if (StartScrapping)
                _state = 1;
            else
                _state = -1;

            Browser.Address = $"{URL}{a.Attributes["href"].Value.Substring(1)}";
        }

        public override bool Navigate(MediaItem mitem)
        {
            if (!base.Navigate(mitem))
                return false;

            Browser.Address = $"{URL}search?q={Media.Pid}&f=all";
            return true;
        }

        public override void Scrap()
        {
            switch (_state)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//a[@class='box']"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemJavDb(this)
                    {
                        NumItemsToScrap = _xpathDic.Count
                    }, _xpathDic);
                    _state = 2;
                    break;
            }
        }
    }
}
