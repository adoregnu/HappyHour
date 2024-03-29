﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using CefSharp;

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
            ScriptName = "JavDb.js";
        }

        protected override List<Cookie> CreateCookie()
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
        protected override void AdjustKeyword()
        {
            Keyword = Regex.Replace(Keyword, @"^\d+", "");
        }
    }
}
