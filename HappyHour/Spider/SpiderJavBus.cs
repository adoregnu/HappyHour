using HappyHour.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CefSharp;

namespace HappyHour.Spider
{
    class SpiderJavBus : SpiderBase
    {
        public List<string> SearchTypes { get; set; } = new List<string>
        {
            "censored", "uncensored"
        };
        public string SearchType { get; set; } = "censored";

        public override string SearchURL
        {
            get
            {
                if (SearchType == "censored")
                    return $"{URL}search/{Keyword}";
                else
                    return $"{URL}uncensored/search/{Keyword}";
            }
        }

        public SpiderJavBus(SpiderViewModel browser) : base(browser)
        {
            Name = "JavBus";
            URL = "https://www.javbus.com/ja/";
            ScriptName = "JavBus.js";
        }
        protected override List<Cookie> CreateCookie()
        {
            return [
                new Cookie
                {
                    Name = "dv",
                    Value = "1",
                    Domain = "javbus.com",
                    Path = "/",
                    Expires = DateTime.Parse( "Tue, 01 Jan 2030 00:00:00 GMT")
                },
            ];
        }
    }
}
