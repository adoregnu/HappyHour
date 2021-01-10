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
    class SpiderPornav : SpiderBase
    {
        readonly Dictionary<string, string> _xpathDic;

        protected override string SearchURL
        {
            get
            {
                return $"{URL}/jp/search?q={Media.Pid}";
            }
        }
        public SpiderPornav(SpiderViewModel browser) : base(browser)
        {
            Name = "PornAv";
            URL = "https://pornav.co";

            _xpathDic = new Dictionary<string, string>
            {
                { "cover", XPath("//div[@class='col-md-7']/img/@src") },
                { "title", XPath("//div[@class='col-md-5']/p[1]/text()") },
                { "info", XPath("//div[@class='col-md-5']/p[2]//text()") },
            };
        }

        public void OnMultiResult(List<object> list)
        {
            Log.Print($"OnMultiResult : {list.Count} items found!");
            if (list.IsNullOrEmpty())
            {
                Browser.StopScrapping(Media);
                return;
            }

            HtmlNode anode = null;
            HtmlDocument doc = new HtmlDocument();
            foreach (string it in list)
            {
                doc.LoadHtml(it);
                var node = doc.DocumentNode.SelectSingleNode("//a");
                if (node.InnerText.IndexOf(Media.Pid, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    anode = node;
                    break;
                }
            }
            if (anode == null)
            {
                Log.Print($"Not found {Media.Pid}");
                Browser.StopScrapping(Media);
                return;
            }
            if (EnableScrapIntoDb)
                _state = 1;
            else
                _state = -1;
            Browser.Address = $"{URL}{anode.Attributes["href"].Value}";
        }
 
        public override void Scrap()
        {
            switch (_state)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//div[@class='product-description']//a"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemPornav(this)
                    {
                        NumItemsToScrap = _xpathDic.Count
                    }, _xpathDic);
                    _state = 2;
                    break;
            }
        }
    }

}
