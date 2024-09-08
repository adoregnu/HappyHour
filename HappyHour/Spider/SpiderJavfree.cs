using HappyHour.Interfaces;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderJavfree : SpiderBase
    {
        private readonly CacheDownloader _cacheDownloader = new();
        public override string SearchURL => $"{URL}?s={Keyword}";
        protected override IDownloader Downloader => _cacheDownloader;

        public SpiderJavfree(SpiderViewModel browser) : base(browser)
        {
            Name = "Javfree";
            URL = "https://javfree.me/";
            ScriptName = "Javfree.js";

            UrlPatternsToFilter = ["cf.javfree.me/HLIC"];
        }
    }
}
