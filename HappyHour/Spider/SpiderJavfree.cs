using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavfree : SpiderBase
    {
        public override string SearchURL
        {
            get
            {
                return $"{URL}?s={Keyword}";
            }
        }

        public SpiderJavfree(SpiderViewModel browser) : base(browser)
        {
            Name = "Javfree";
            URL = "https://javfree.me/";
        }

        public void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                goto NotFound;

            var regex = new Regex($@"{Keyword.ToLower()}");
            string exactUrl = null;
            foreach (string url in list)
            {
                var m = regex.Match(url);
                if (m.Success)
                {
                    exactUrl = url;
                    break;
                }
            }
            if (exactUrl == null)
                goto NotFound;

            ParsingState = 1;
            Browser.Address = exactUrl;
            return;

        NotFound:
            Log.Print("No matched Pid!");
            OnScrapCompleted();
        }

        public override void Scrap()
        {
            Dictionary<string, string> xpathDic = new Dictionary<string, string>
            {
                { "cover", XPath("//div[@class='entry-content']//img[1]/@src") }
            };
            switch (ParsingState)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//h2[@class='entry-title']/a/@href"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemJavfree(this)
                    {
                        NumItemsToScrap = xpathDic.Count
                    }, xpathDic);
                    ParsingState = 2;
                    break;
            }
        }
    }
}
