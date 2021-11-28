using System.Collections.Generic;
using System.IO;

using CefSharp;
using Scriban;

using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderAvJamak : SpiderBase
    {
        //public override string SearchURL =>
        //    $"{URL}bbs/search.php?url=https%3A%2F%2Fav-jamak.com%2Fbbs%2Fsearch.php" +
        //    $"&stx={Keyword}";
        public override string SearchURL => $"{URL}/bbs/search.php" +
            $"?srows=10&gr_id=jamak&sfl=wr_subject&stx={Keyword}&sop=and";

        public SpiderAvJamak(SpiderViewModel browser) : base(browser)
        {
            Name = "av-jamak";
            //URL = "https://av-jamak.com/";
            //URL = "https://av-jamack.com/";
            URL = "https://avjamak01.com/";
            ScriptName = "AvJamak.js";
        }

        protected override string GetScript(string name)
        {
            string userid = "";
            string password = "";
            if (App.GConf.Sections.ContainsSection("avjamak"))
            {
                var avjamak = App.GConf["avjamak"];
                if (avjamak.ContainsKey("userid") &&
                    avjamak.ContainsKey("password"))
                {
                    userid = avjamak["userid"];
                    password = avjamak["password"];
                }
            } 
 
            Template template = Template.Parse(App.ReadResource(name));
            return template.Render(new {
                Pid = Keyword, Userid = userid, Password = password
            });
        }

        public override void Scrap()
        {
            Browser.ExecJavaScript(GetScript(ScriptName));
        }

        void OnBeforeDownload(object sender, DownloadItem e)
        {
            var ext = Path.GetExtension(e.SuggestedFileName);
            string savePath;
            if (!string.IsNullOrEmpty(DataPath))
                savePath = DataPath + "\\";
            else
                savePath = App.GConf["general"]["data_path"];

            if (!Directory.Exists(savePath))
                Log.Print($"{savePath} does not exist.");

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
            Log.Print($"{Name} selected!");
            var dh = Browser.DownloadHandler;
            dh.OnBeforeDownloadFired += OnBeforeDownload;
            dh.OnDownloadUpdatedFired += OnDownloadUpdated;
        }

        public override void OnDeselect()
        {
            Log.Print($"{Name} deselected!");
            var dh = Browser.DownloadHandler;
            dh.OnBeforeDownloadFired -= OnBeforeDownload;
            dh.OnDownloadUpdatedFired -= OnDownloadUpdated;
        }
    }
}
