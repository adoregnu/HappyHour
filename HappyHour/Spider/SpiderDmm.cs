
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
        }

        public override List<Cookie> CreateCookie()
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

        void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                goto NotFound;

            var apid = Keyword.Split('-');
            var regex = new Regex($@"cid=(h_)?(\d+)?{apid[0].ToLower()}");
            int matchCount = 0;
            string exactUrl = null;
            foreach (string url in list)
            {
                var m = regex.Match(url);
                if (m.Success)
                {
                    exactUrl = url;
                    matchCount++;
                }
            }
            if (matchCount == 0)
                goto NotFound;

            if (matchCount == 1)
            {
                ParsingState = 1;
                Browser.Address = exactUrl;
            }
            else if (matchCount > 1)
            {
                Log.Print("Ambguous match! Select manually!");
            }
            return;

        NotFound:
            Log.Print("No Exact match ID");
            OnScrapCompleted();
        }

        public override void Scrap()
        {
            switch (ParsingState)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//p[@class='tmb']/a/@href"),
                        OnMultiResult);
                    break;
                case 1:
                    OnScrapCompleted();
                    break;
            }
        }
    }
}
