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

        void OnMultiResult(List<object> list)
        {
            Log.Print($"OnMultiResult: {list.Count} items found!");
            if (list.IsNullOrEmpty())
            {
                Browser.StopScrapping(Media);
                return;
            }
            if (StartScrapping)
                _state = 1;
            else
                _state = -1;

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

        public override bool Navigate(MediaItem mitem)
        {
            //http://javmovie.com/en/search.html?k=XC-1379
            if (!base.Navigate(mitem))
                return false;

            Browser.Address = $"{URL}search.html?k={Media.Pid}";
            return true;
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
