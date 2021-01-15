using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CefSharp;
using HtmlAgilityPack;
using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderAVE : SpiderBase
    {
        public override string SearchURL
        {
            get
            {
                return $"{URL}search_Products.aspx?languageID=1" +
                $"&dept_id=29&keyword={Keyword}&searchby=keyword";
            }
        }

        public SpiderAVE(SpiderViewModel browser) : base(browser)
        {
            Name = "AVE";
            URL = "https://www.aventertainments.com/";
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

        void OnMultiResult(List<object> list)
        { 
            Log.Print($"OnMultiResult : {list.Count} items found!");
            if (list.IsNullOrEmpty())
            {
                OnScrapCompleted();
                return;
            }

            _state = 1;
            if (list.Count > 1)
            {
                Log.Print("Multiple matched. Select manually!");
            }
            else
            {
                var url = HtmlEntity.DeEntitize(list[0] as string);
                Browser.Address = url;
            }
        }

        public override void Scrap()
        {
            var xpathDic = new Dictionary<string, string>
            {
                { "cover", XPath("//span[@class='grid-gallery']/a/@href") },
                { "title", XPath("//div[@class='section-title']/h3/text()") },
                { "product-info", XPath("//div[@class='single-info']") },

            };
            switch (_state)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//p[@class='product-title']/a/@href"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemAVE(this)
                    {
                        NumItemsToScrap = xpathDic.Count
                    }, xpathDic);
                    _state = 2;
                    break;
            }
        }
    }
}
