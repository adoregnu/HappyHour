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

            ScrapItems.ForEach(i =>
            {
                if (i.Name is not "actor" and not "series")
                {
                    i.CanUpdate = false;
                }
            });
        }

        protected override void AdjustKeyword()
        {
            Keyword = Regex.Replace(Keyword, @"^\d+", "");
        }
    }
}
