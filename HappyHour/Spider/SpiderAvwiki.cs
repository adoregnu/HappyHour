using System.Text.RegularExpressions;

using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    internal class SpiderAvwiki : SpiderBase
    {
        public override string SearchURL => $"{URL}?s={Keyword}";

        public SpiderAvwiki(SpiderViewModel browser) : base(browser)
        {
            Name = "AvWiki";
            URL = "https://av-wiki.net/";
            ScriptName = "AvWiki.js";
        }
        protected override void AdjustKeyword()
        {
            Keyword = Regex.Replace(Keyword, @"^\d+", "");
        }
    }
}
