using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderAvwiki : SpiderBase
    {
        public override string SearchURL
        {
            get => $"{URL}?s={Keyword}";
        }

        public SpiderAvwiki(SpiderViewModel browser) : base(browser)
        {
            Name = "AvWiki";
            URL = "https://av-wiki.net/";
        }

        void OnSearchResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                goto NotFound;

            if (list.Count > 1)
            {
                Log.Print("Multiple results found!");
                goto NotFound;
            }
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(list[0] as string);
            var node = doc.DocumentNode.SelectSingleNode("//h2[@class='archive-header-title']/a");
            Log.Print("Title : " + node.InnerText);
            node = doc.DocumentNode.SelectSingleNode("//ul[contains(@class,'post-meta')]/li/a");
            Log.Print("Studio :" + node.InnerText);
            node = doc.DocumentNode.SelectSingleNode("//li[@class='actress-name']//a");
            Log.Print($"Actress : {node.InnerText}, url={node.Attributes["href"].Value}");

        NotFound:
            OnScrapCompleted();
            _state = -1;
        }

        public override void Scrap()
        {
            switch (_state)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//article[@class='archive-list']"),
                        OnSearchResult);
                    break;
                case 1:
                    break;
            }
        }
    }
}
