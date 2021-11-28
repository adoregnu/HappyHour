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

        protected override List<Cookie> CreateCookie()
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
    }
}
