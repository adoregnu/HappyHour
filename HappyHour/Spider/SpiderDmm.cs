
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using CefSharp;
using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderDmm : SpiderBase
    {
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

        void OnMultiResult(List<object> list)
        {
            Log.Print($"OnMultiResult : {list.Count} items found!");
            if (list.IsNullOrEmpty())
            {
                Browser.StopScrapping(Media);
                return;
            }
            var apid = Media.Pid.Split('-');
            var regex = new Regex($@"cid=(h_)?(\d+)?{apid[0].ToLower()}");
            int matchCount = 0;
            string exactUrl = null;
            foreach (string url in list)
            {
                var m = regex.Match(url);
                if (m.Success)
                {
                    exactUrl = url;
                    _state = 1;
                    matchCount++;
                }
            }

            if (matchCount == 1)
            {
                Browser.Address = exactUrl;
            }
            else if (matchCount > 1)
            {
                Log.Print("Ambguous match! Select manually!");
            }
            else
            {
                Browser.StopScrapping(Media);
                Log.Print("No Exact match ID");
            }
        }

        public override void Navigate(MediaItem mitem)
        {
            base.Navigate(mitem);
            Browser.Address = $"{URL}mono/-/search/=/searchstr={Media.Pid}/";
        }

        public override void Scrap()
        {
            switch (_state)
            {
                case 0:
                    Browser.ExecJavaScript(XPath("//p[@class='tmb']/a/@href"), OnMultiResult);
                    break;
                case 1:
                    Browser.StopScrapping(Media);
                    break;
            }
        }
    }
}
