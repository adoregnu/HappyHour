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

        public override string SearchURL
        {
            get
            {
                return $"{URL}/en/search?q={Keyword}";
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

        public void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                goto NotFound;

            HtmlNode anode = null;
            HtmlDocument doc = new HtmlDocument();
            foreach (string it in list)
            {
                doc.LoadHtml(it);
                var node = doc.DocumentNode.SelectSingleNode("//a");
                if (node.InnerText.IndexOf(Keyword, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    anode = node;
                    break;
                }
            }
            if (anode == null)
                goto NotFound;

            _state = 1;
            Browser.Address = $"{URL}{anode.Attributes["href"].Value}";
            return;
        NotFound:
            OnScrapCompleted();
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
