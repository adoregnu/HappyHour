
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.ScrapItems;
using HappyHour.ViewModel;
using HappyHour.Extension;
using HappyHour.Model;

namespace HappyHour.Spider
{
    class SpiderR18 : SpiderBase
    {
        Dictionary<string, string> _xpathDic;

        public override string SearchURL
        {
            get
            {
                return $"{URL}common/search/searchword={Keyword}/";
            }
        }
        public SpiderR18(SpiderViewModel browser) : base(browser)
        {
            Name = "R18";
            URL = "https://www.r18.com/";
            _xpathDic = new Dictionary<string, string>
            {
                { "title",    XPath("//meta[@property='og:title']/@content") },
                { "releasedate", XPath("//dt[contains(.,'Release Date:')]/following-sibling::dd[1]/text()") },
                { "runtime",  XPath("//dt[contains(.,'Runtime:')]/following-sibling::dd[1]/text()") },
                { "director", XPath("//dt[contains(.,'Director:')]/following-sibling::dd[1]/text()") },
                { "set_url",  XPath("//dt[contains(.,'Series:')]/following-sibling::dd[1]/a/@href") },
                { "studio",   XPath("//dt[contains(.,'Studio:')]/following-sibling::dd[1]/a/text()") },
                { "label",    XPath("//dt[contains(.,'Label:')]/following-sibling::dd[1]/text()") },
                { "actor",    XPath("//label[contains(.,'Actress(es):')]/following-sibling::div[1]/span/a/span/text()") },
                { "genre",    XPath("//label[contains(.,'Categories:')]/following-sibling::div[1]/a/text()") },
                { "plot",     XPath("//h1[contains(., 'Product Description')]/following-sibling::p/text()") },
                { "cover",    XPath("//div[contains(@class,'box01')]/img/@src") },
                { "actor_thumb", XPath("//ul[contains(@class,'cmn-list-product03')]//img") },
            };
        }

        public override List<Cookie> CreateCookie()
        {
            return new List<Cookie> {
                new Cookie
                {
                    Name = "mack",
                    Value = "1",
                    Domain = "www.r18.com",
                    Path = "/",
                }
            };
        }

        void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                goto NotFound;

            string pattern = @"id=(h_)?(\d+)?";
            if (Keyword.Contains('-'))
            {
                var apid = Keyword.Split('-');
                pattern += $@"{apid[0].ToLower()}\d*{apid[1]}";
            }
            else
            {
                pattern += Keyword;
            }
            var regex = new Regex(pattern);
            int matchCount = 0;
            string exactUrl = null;
            foreach (string url in list)
            {
                var m = regex.Match(url);
                if (m.Success)
                {
                    exactUrl = url;
                    matchCount++;
                }
            }
            if (matchCount == 0)
                goto NotFound;

            if (matchCount == 1)
            {
                _state = 1;
                var url = HtmlEntity.DeEntitize(exactUrl);
                Browser.Address = url;
            }
            else
            {
                Log.Print("Ambiguous match! Select manually!");
            }
            return;

        NotFound:
            Log.Print($"No exact matched ID");
            OnScrapCompleted();
        }

        ItemR18 _item = null;
        public override void Scrap()
        {
            switch (_state)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//li[starts-with(@class,'item-list')]/a/@href"),
                        OnMultiResult);
                    break;
                case 1:
                    _item = new ItemR18(this)
                    {
                        NumItemsToScrap = _xpathDic.Count,
                    };
                    ParsePage(_item, _xpathDic);
                    _state = 2;
                    break;
                case 2:
                    if (_linkName != "series") break;
                    Dictionary<string, string> seriesXpath = new Dictionary<string, string>
                    {
                        { "series", XPath("//div[@class='cmn-ttl-tabMain01']/h1/text()") } 
                    };

                    _item.NumItemsToScrap = seriesXpath.Count; 
                    ParsePage(_item, seriesXpath);
                    _state = 3;
                    break;
            }
        }
    }
}
