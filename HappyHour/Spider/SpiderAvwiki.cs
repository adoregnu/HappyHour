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
