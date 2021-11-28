using System;
using System.Collections.Generic;
using System.Linq;

using CefSharp;

using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderMgstage : SpiderBase
    {
        public override string SearchURL => $"{URL}product/product_detail/{Keyword.ToUpper()}/";
        public SpiderMgstage(SpiderViewModel browser) : base(browser)
        {
            Name = "MGStage";
            URL = "https://www.mgstage.com/";
            ScriptName = "Mgstage.js";
        }

        protected override List<Cookie> CreateCookie()
        {
            return new List<Cookie> {
                new Cookie
                {
                    Name = "adc",
                    Value = "1",
                    Domain = "mgstage.com",
                    Path = "/"
                }
            };
        }
    }
}
