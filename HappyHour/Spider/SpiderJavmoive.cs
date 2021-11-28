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
    }
}
