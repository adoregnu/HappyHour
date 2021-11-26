using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavmoive : SpiderBase
    {
        public override string SearchURL => $"{URL}search.html?k={Keyword}";
        public SpiderJavmoive(SpiderViewModel browser) : base(browser)
        {
            Name = "JavMovie";
            URL = "http://javmovie.com/en/";
            ScriptName = "JavMovie.js";
        }
#if false
        void OnMultiResult(object result)
        {
            if (!CheckResult(result, out List<string> list))
                return;

            ParsingState = 1;
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

        public override void Scrap()
        {
            switch (ParsingState)
            {
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//div[@class='movie-thumb']/a/@href"),
                        OnMultiResult);
                    break;
                case 1:
                    ParsePage(new ItemJavmovie(this));
                    ParsingState = 2;
                    break;
            }
        }
#endif
    }
}
