using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;

namespace HappyHour.Spider
{
    class SpiderAvsox : SpiderBase
    {
        public override string SearchURL => $"{URL}search/{Keyword}";

        public SpiderAvsox(SpiderViewModel browser) : base(browser)
        {
            Name = "AVSOX";
            URL = "https://avsox.website/en/";
        }

        void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                goto NotFound;

            HtmlNode anode = null;
            HtmlDocument doc = new HtmlDocument();

            var pid = Keyword;
            if (pid.StartsWith("HEYZO", StringComparison.OrdinalIgnoreCase) ||
                Regex.Match(Keyword, @"^\d{6}(?:_|-)\d{3}$").Success)
            {
                pid = pid.Replace('_', '-');
            }
            foreach (string it in list)
            {
                doc.LoadHtml(it);
                var node = doc.DocumentNode.SelectSingleNode("//date[1]");
                var innerText = node.InnerText.Trim().Replace('_', '-');
                if (innerText.IndexOf(pid, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    anode = doc.DocumentNode.FirstChild;
                    break;
                }
            }

            if (anode == null)
            {
                Log.Print($"Not found {Keyword}");
                goto NotFound;
            }
            ParsingState = 1;
            Browser.Address = anode.Attributes["href"].Value;
            return;

        NotFound:
            OnScrapCompleted();
        }

        public override void Scrap()
        {
            switch (ParsingState)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//a[contains(@class,'movie-box')]"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemAvsox(this));
                    ParsingState = 2;
                    break;
            }
        }
    }
}
