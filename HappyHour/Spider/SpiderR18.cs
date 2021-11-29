using System;
using System.Collections.Generic;
using System.Linq;

using CefSharp;

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
            ScriptName = "R18.js";
        }

        protected override List<Cookie> CreateCookie()
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
    }
}
