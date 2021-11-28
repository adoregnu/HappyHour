using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderAvwiki : SpiderBase
    {
        public override string SearchURL => $"{URL}?s={Keyword}";

        public SpiderAvwiki(SpiderViewModel browser) : base(browser)
        {
            Name = "AvWiki";
            URL = "https://av-wiki.net/";
            ScriptName = "AvWiki.js";
        }
    }
}
