using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavDb : SpiderBase
    {
        public override string SearchURL => $"{URL}search?q={Keyword}&f=all"; 

        public SpiderJavDb(SpiderViewModel browser) : base(browser)
        { 
            Name = "JavDB";
            URL = "https://javdb.com/";
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

        void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                goto NotFound;

            HtmlDocument doc = new HtmlDocument();
            HtmlNode a = null;
            foreach (string it in list)
            {
                doc.LoadHtml(it);
                var node = doc.DocumentNode.SelectSingleNode("//div[@class='uid']");
                var pid = node.InnerText.Trim();
                if (pid.Equals(Keyword, StringComparison.OrdinalIgnoreCase))
                {
                    a = doc.DocumentNode.FirstChild;
                    break;
                }
            }
            if (a == null)
                goto NotFound;

            ParsingState = 1;
            Browser.Address = $"{URL}{a.Attributes["href"].Value.Substring(1)}";
            return;
        NotFound:
            OnScrapCompleted();
        }

        public override void Scrap()
        {
            switch (ParsingState)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//a[@class='box']"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemJavDb(this));
                    ParsingState = 2;
                    break;
            }
        }
    }
}
