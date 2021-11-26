using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CefSharp;

using HtmlAgilityPack;

using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderAVE : SpiderBase
    {
        public override string SearchURL =>
            $"{URL}search_Products.aspx?languageID=1" +
            $"&dept_id=29&keyword={Keyword}&searchby=keyword";

        public SpiderAVE(SpiderViewModel browser) : base(browser)
        {
            Name = "AVE";
            URL = "https://www.aventertainments.com/";
            ScriptName = "AVE.js";
        }

        public override List<Cookie> CreateCookie()
        {
            return new List<Cookie> 
            {
                new Cookie
                { 
                    Name = "__utmt",
                    Value = "1",
                    Domain = ".aventertainments.com",
                    Path = "/"
                }
            };
        }
#if false
        void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
            {
                OnScrapCompleted();
                return;
            }
            
            ParsingState = 1;
            if (list.Count > 1)
            {
                Log.Print("Multiple matched. Select manually!");
            }
            else
            {
                var url = HtmlEntity.DeEntitize(list[0]);
                Browser.Address = url;
            }
        }

        public override void Scrap()
        {
            switch (ParsingState)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//p[@class='product-title']/a/@href"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemAVE(this));
                    ParsingState = 2;
                    break;
            }
        }
#endif
    }
}
