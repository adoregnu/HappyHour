using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavlibrary : SpiderBase
    {
        public override string SearchURL => $"{URL}vl_searchbyid.php?keyword={Keyword}";
        public SpiderJavlibrary(SpiderViewModel browser) : base(browser)
        {
            Name = "Javlibrary";
            URL = "https://www.javlibrary.com/en/";
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

        void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
            {
                ParsePage(new ItemJavlibrary(this));
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
                    ParsingState = 1;
                    Browser.Address = $"{URL}{href}";
                    return;
                }
            }
            OnScrapCompleted();
        }

        public override void Scrap()
        {
            switch (ParsingState)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//div[@class='videos']/div/a"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemJavlibrary(this));
                    ParsingState = 2;
                    break;
            }
        }
    }
}
