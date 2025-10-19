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

        protected async override Task Translate(IDictionary<string, object> items)
        {
            var authKey = "634b5865-fd29-3d8b-8c87-2ccbb5f48cb7:fx";
            var translator = new Translator(authKey);

            if (!items.TryGetValue("plot", out object plot) || plot == null)
            {
                return;
            }
 
            var translatedText = await translator.TranslateTextAsync(
                  plot.ToString(),
                  LanguageCode.Japanese,
                  LanguageCode.Korean);
            //Log.Print(translatedText.Text);
            items["plot"] = translatedText.Text + ";ko";
        }
    }
}
