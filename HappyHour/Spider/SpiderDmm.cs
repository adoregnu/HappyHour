
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CefSharp;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderDmm : SpiderBase
    {
        public override string SearchURL => $"{URL}mono/-/search/=/searchstr={Keyword}/";

        public SpiderDmm(SpiderViewModel browser) : base(browser)
        {
            Name = "DMM";
            URL = "http://www.dmm.co.jp/";
            ScriptName = "Dmm.js";
        }

        protected override List<Cookie> CreateCookie()
        {
            return new List<Cookie> { 
                new Cookie
                { 
                    Name = "cklg",
                    Value = "en",
                    Domain = ".dmm.co.jp",
                    Path = "/"
                },
                new Cookie
                { 
                    Name = "age_check_done",
                    Value = "1",
                    Domain = ".dmm.co.jp",
                    Path = "/"
                }
            };
        }
    }
}
