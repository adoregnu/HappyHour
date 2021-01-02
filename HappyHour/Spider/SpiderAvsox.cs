using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.ViewModel;
using HappyHour.ScrapItems;

namespace HappyHour.Spider
{
    class SpiderAvsox : SpiderBase
    {
        readonly Dictionary<string, string> _xpathDic;

        public SpiderAvsox(SpiderViewModel browser) : base(browser)
        {
            Name = "AVSOX";
            URL = "https://avsox.website/en/";

            _xpathDic = new Dictionary<string, string>
            {
                { "cover", XPath("//a[@class='bigImage']/@href") },
                { "info", XPath("//div[contains(@class, 'col-md-3')]") },
                { "title", XPath("//div[@class='container']/h3/text()") },
                //{ "release", XPath("//div[contains(@class, 'col-md-3')]/p[2]/text()") },
                //{ "studio", XPath("//div[contains(@class, 'col-md-3')]/p[5]/a/text()") },
                //{ "series", XPath("//div[contains(class, 'col-md-3')]/p[7]/a/text()") }
                //{ "genre", XPath("//span[@class='genre']/a/text()") }
                { "actor", XPath("//a[@class='avatar-box']") }
            };
        }

        public override void Navigate(MediaItem mitem)
        {
            base.Navigate(mitem);
            Browser.Address = $"{URL}search/{Media.Pid}";
        }

        void OnMultiResult(List<object> list)
        { 
            Log.Print($"OnMultiResult : {list.Count} items found!");
            if (list.IsNullOrEmpty())
            {
                Browser.StopScrapping(Media);
                return;
            }

            HtmlNode anode = null;
            HtmlDocument doc = new HtmlDocument();

            var pid = Media.Pid;
            if (pid.StartsWith("HEYZO", StringComparison.OrdinalIgnoreCase))
                pid = pid.Replace('_', '-');
            
            foreach (string it in list)
            {
                doc.LoadHtml(it);
                var node = doc.DocumentNode.SelectSingleNode("//date[1]");
                if (node.InnerText.IndexOf(pid, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    anode = doc.DocumentNode.FirstChild;
                    break;
                }
            }

            if (anode == null)
            {
                Log.Print($"Not found {Media.Pid}");
                Browser.StopScrapping(Media);
                return;
            }
            _state = 1;
            Browser.Address = anode.Attributes["href"].Value;
        }

        public override void Scrap()
        {
            switch (_state)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//a[contains(@class,'movie-box')]"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemAvsox(this)
                    {
                        NumItemsToScrap = _xpathDic.Count
                    }, _xpathDic);
                    _state = 2;
                    break;
            }
        }
    }
}
