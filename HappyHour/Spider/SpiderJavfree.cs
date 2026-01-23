using DeepL;
using DeepL.Model;
using HappyHour.Interfaces;
using HappyHour.ViewModel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

        protected override void AdjustKeyword()
        {
            var m = Regex.Match(Keyword, @"^\d+[a-zA-Z]+");
            if (m.Success)
            {
                Keyword = Regex.Replace(Keyword, @"^\d+", "");
            }
        }
    }
}
