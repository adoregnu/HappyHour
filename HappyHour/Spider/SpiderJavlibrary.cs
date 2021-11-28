﻿using System;
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
            ScriptName = "JavLibrary.js";
        }

        protected override List<Cookie> CreateCookie()
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
    }
}
