using System.Collections.Generic;
using System.Text.RegularExpressions;

using CefSharp;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderDmm : SpiderBase
    {
        public override string SearchURL => $"{URL}search/=/searchstr={Keyword}/";
        public SpiderDmm(SpiderViewModel browser) : base(browser)
        {
            Name = "DMM";
            URL = "https://www.dmm.co.jp/";
            ScriptName = "Dmm.js";
            RegexPatterns = [
                @"\d*(\w+)-(\d+)"
            ];
        }

        protected override List<Cookie> CreateCookie()
        {
            return [
                new() {
                    Name = "cklg",
                    Value = "en",
                    Domain = ".dmm.co.jp",
                    Path = "/"
                },
                new() {
                    Name = "age_check_done",
                    Value = "1",
                    Domain = ".dmm.co.jp",
                    Path = "/"
                }
            ];
        }
    }
}
