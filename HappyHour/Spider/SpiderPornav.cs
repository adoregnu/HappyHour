using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HappyHour.ViewModel;
using HappyHour.ScrapItems;

namespace HappyHour.Spider
{
    class SpiderPornav : SpiderBase
    {
        public override string SearchURL => $"{URL}/en/search?q={Keyword}";
        public SpiderPornav(SpiderViewModel browser) : base(browser)
        {
            Name = "PornAv";
            URL = "https://pornav.co";
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

            ParsingState = 1;
            Browser.Address = $"{URL}{anode.Attributes["href"].Value}";
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
                        XPath("//div[@class='product-description']//a"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemPornav(this));
                    ParsingState = 2;
                    break;
            }
        }
    }

}
