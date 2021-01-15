using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.Model;
using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavlibrary : SpiderBase
    {
        Dictionary<string, string> _xpathDic;
        public override string SearchURL
        {
            get
            {
                return $"{URL}vl_searchbyid.php?keyword={Keyword}";
            }
        }
        public SpiderJavlibrary(SpiderViewModel browser) : base(browser)
        {
            Name = "Javlibrary";
            URL = "https://www.javlibrary.com/en/";
            _xpathDic = new Dictionary<string, string>
            {
                { "title",  XPath("//*[@id='video_title']/h3/a/text()") },
                { "id",     XPath("//*[@id='video_id']//td[2]/text()") },
                { "date",   XPath("//*[@id='video_date']//td[2]/text()") },
                { "director", XPath("//*[@id='video_director']//*[@class='director']/a/text()") },
                { "studio", XPath("//*[@id='video_maker']//*[@class='maker']/a/text()") },
                { "cover",  XPath("//*[@id='video_jacket_img']/@src") },
                { "rating", XPath("//*[@id='video_review']//*[@class='score']/text()") },
                { "genre",  XPath("//*[@id='video_genres']//*[@class='genre']//text()") },
                { "actor",  XPath("//*[@id='video_cast']//*[@class='cast']") },
            };
        }
        public override List<Cookie> CreateCookie()
        {
            return new List<Cookie>
            {
                new Cookie
                {
                    Name = "over18",
                    Value = "18",
                    Domain = "www.javlibrary.com",
                    Path = "/"
                }
            };
        }

        void OnMultiResult(List<object> list)
        {
            if (list == null || list.Count == 0)
            {
                ParsePage(new ItemJavlibrary(this)
                {
                    NumItemsToScrap = _xpathDic.Count
                }, _xpathDic);
                return;
            }

            HtmlDocument doc = new HtmlDocument();
            foreach (string url in list)
            {
                doc.LoadHtml(url);
                var div = doc.DocumentNode.SelectSingleNode("//div[@class='id']").InnerText;
                if (div.Trim().Equals(Keyword, StringComparison.OrdinalIgnoreCase))
                {
                    var href = doc.DocumentNode.FirstChild.Attributes["href"].Value;
                    _state = 1;
                    Browser.Address = $"{URL}{href}";
                    return;
                }
            }
            OnScrapCompleted();
        }

        public override void Scrap()
        {
            switch (_state)
            {
            case 0:
                Browser.ExecJavaScript(XPath("//div[@class='videos']/div/a"),
                    OnMultiResult);
                break;
            case 1:
                ParsePage(new ItemJavlibrary(this)
                {
                    NumItemsToScrap = _xpathDic.Count
                }, _xpathDic);
                _state = 2;
                break;
            }
        }
    }
}
