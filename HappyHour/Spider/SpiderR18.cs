
using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderR18 : SpiderBase
    {
        public override string SearchURL => $"{URL}common/search/searchword={Keyword}/";
        public SpiderR18(SpiderViewModel browser) : base(browser)
        {
            Name = "R18";
            URL = "https://www.r18.com/";
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
            if (list.Count == 1 && matchCount == 0)
                exactUrl = list[0];
            else if (matchCount == 0)
                goto NotFound;

            ParsingState++;
            if (matchCount == 1)
            {
                Browser.Address = HtmlEntity.DeEntitize(exactUrl);
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

        ItemR18V2 _item = null;
        public override void OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
        {
            //base.OnJsMessageReceived(msg);
            Log.Print(msg.Message.ToString());
            _item = new ItemR18V2(this);
            ParsePage(_item);
            ParsingState++;
        }

        public override void Scrap()
        {
            Log.Print($"{Name}: ParsingState : {ParsingState}");
            switch (ParsingState)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//li[starts-with(@class,'item-list')]/a/@href"),
                        OnMultiResult);
                    break;
                case 1:
                    Browser.ExecJavaScript(App.ReadResource("R18ShowActor.js"));
                    break;
                case 2:
                    if (_linkName != "series")
                    {
                        break;
                    }
                    _item.Elements = new List<(string name, string element, ElementType type)>
                    { 
                        ("series", "//div[@class='cmn-ttl-tabMain01']/h1/text()", ElementType.XPATH)
                    };
                    ParsePage(_item);
                    ParsingState++;
                    break;
            }
        }
    }
}
