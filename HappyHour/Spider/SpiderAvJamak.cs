using System.Collections.Generic;
using System.IO;

using CefSharp;
using Scriban;
using HtmlAgilityPack;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderAvJamak : SpiderBase
    {
        public override string SearchURL =>
            $"{URL}bbs/search.php?url=https%3A%2F%2Fav-jamak.com%2Fbbs%2Fsearch.php" +
            $"&stx={Keyword}";

        public SpiderAvJamak(SpiderViewModel browser) : base(browser)
        {
            Name = "av-jamak";
            URL = "https://av-jamak.com/";
        }

        void OnBeforeDownload(object sender, DownloadItem e)
        {
            var ext = Path.GetExtension(e.SuggestedFileName);
            string savePath;
            if (!string.IsNullOrEmpty(DataPath))
                savePath = DataPath + "\\";
            else
                savePath = App.GConf["general"]["data_path"];

            e.SuggestedFileName = $"{savePath}{Keyword}{ext}";
        }

        void OnDownloadUpdated(object sender, DownloadItem e)
        {
            if (e.IsComplete)
            {
                Log.Print($"{Name}: {e.SuggestedFileName} download completed!");
                Browser.MediaList.AddMedia(DataPath);
            }
        }
        public override void OnSelected()
        {
            base.OnSelected();
            var dh = Browser.DownloadHandler;
            dh.OnBeforeDownloadFired += OnBeforeDownload;
            dh.OnDownloadUpdatedFired += OnDownloadUpdated;
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            var dh = Browser.DownloadHandler;
            dh.OnBeforeDownloadFired -= OnBeforeDownload;
            dh.OnDownloadUpdatedFired -= OnDownloadUpdated;
        }

        void OnResult(object result)
        {
            if (result is not List<object> list || list.Count == 0)
            {
                Log.Print($"{Name}: No result");
            }
            else if (list.Count > 1)
            {
                Log.Print($"{Name}: Multiple result! select manually");
            }
            else
            {
                var url = HtmlEntity.DeEntitize(list[0] as string);
                Browser.Address = URL + $"bbs/{url[2..]}"; //remove './'
                return;
            }
            OnScrapCompleted();
        }

        void CheckSearchReulst()
        { 
            ParsingState = 3;
            Browser.ExecJavaScript(XPath("//div[@class='media-body']/" +
                "a[contains(@href, 'table=jamak') or" +
                " contains(@href,'table=bigsub')]/@href"), OnResult);
        }

        void CheckLogin(object result)
        {
            if (result is List<object> list && list.Count > 0)
            {
                ParsingState = 1;
                Browser.Login("AvjamakLogin.js");
                return;
            }
            if (ParsingState == -1)
            {
                OnScrapCompleted();
                return;
            }
            CheckSearchReulst();
        }

        public override void Scrap()
        {
            switch (ParsingState)
            {
                case -1:
                case 0:
                    Browser.ExecJavaScript(
                        XPath("//form[@id='basic_outlogin']"), CheckLogin);
                    return;
                case 1:
                    ParsingState = 2;
                    if (!string.IsNullOrEmpty(Keyword))
                    {
                        Browser.Address = SearchURL;
                        return;
                    };
                    break;
                case 2:
                    CheckSearchReulst();
                    return;
            }
            OnScrapCompleted();
        }
    }
}
