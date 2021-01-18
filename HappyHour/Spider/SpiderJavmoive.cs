using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavmoive : SpiderBase
    {
        Dictionary<string, string> _xpathDic;

        public override string SearchURL
        {
            get
            {
                return $"{URL}search.html?k={Keyword}";
            }
        }
        public SpiderJavmoive(SpiderViewModel browser) : base(browser)
        {
            Name = "JavMovie";
            URL = "http://javmovie.com/en/";

            _xpathDic = new Dictionary<string, string>
            {
                { "cover", XPath("//div[@class='movie-cover']/img/@src") },
                { "title", XPath("//div[@class='mdm-info']/h1/text()") },
                { "date", XPath("//div[@class='mdm-info']//tr[2]/td[2]/text()") },
                { "studio", XPath("//div[@class='mdm-info']//tr[5]/td[2]//text()") },
                { "actor", XPath("//td[@class='list-actress']/a/text()") },
                { "genre", XPath("//td[@class='list-genre']/a/text()") },
            };
        }

        void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                return;

            _state = 1;
            if (list.Count > 1)
            {
                Log.Print("Multitple item matched, Select manually.");
            }
            else
            {
                var url = HtmlEntity.DeEntitize(list[0] as string);
                Browser.Address = url;
            }
        }

        ItemJavmovie _item = null; 
        public override void Scrap()
        {
            switch (_state)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//div[@class='movie-thumb']/a/@href"),
                        OnMultiResult);
                    break;
                case 1:
                    _item = new ItemJavmovie(this) {
                        NumItemsToScrap = _xpathDic.Count
                    };
                    ParsePage(_item, _xpathDic);
                    _state = 2;
                    break;
            }
        }
    }
}
