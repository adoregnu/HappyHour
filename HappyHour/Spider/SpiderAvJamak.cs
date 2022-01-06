using System.IO;

using CefSharp;
using Scriban;

using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    internal class SpiderAvJamak : SpiderBase
    {
        public override string SearchURL => $"{URL}/bbs/search.php" +
            $"?srows=10&gr_id=jamak&sfl=wr_subject&stx={Keyword}&sop=and";

        public SpiderAvJamak(SpiderViewModel browser) : base(browser)
        {
            Name = "AvJamak";
            //URL = "https://av-jamak.com/";
            //URL = "https://av-jamack.com/";
            URL = "https://avjamak.net/";
            ScriptName = "AvJamak.js";
            IsSpiderWorking = true;
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

            var template = Template.Parse(App.ReadResource(name));
            return template.Render(new
            {
                Pid = Keyword,
                Userid = userid,
                Password = password
            });
        }

        private void OnBeforeDownload(object sender, DownloadItem e)
        {
            string ext = Path.GetExtension(e.SuggestedFileName);
            string savePath = SelectedMedia != null ?
                SelectedMedia.Path : App.GConf["general"]["data_path"];
            if (!Directory.Exists(savePath))
            {
                Log.Print($"{savePath} does not exist.");
            }
            e.SuggestedFileName = $"{savePath}\\{SelectedMedia.Pid}{ext}";
        }

        private void OnDownloadUpdated(object sender, DownloadItem e)
        {
            if (e.IsComplete)
            {
                Log.Print($"{Name}: {e.SuggestedFileName} download completed!");
                if (SelectedMedia != null)
                {
                    SelectedMedia.Reload();
                }
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
