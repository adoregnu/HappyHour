using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;

using GalaSoft.MvvmLight.Command;

using Scriban;

using HappyHour.ViewModel;
using HappyHour.Interfaces;
using HappyHour.Model;

namespace HappyHour.Spider
{
    internal class SpiderSehuatang2 : SpiderBase
    {
        private readonly ShtDownloader _downloader;

        protected override IDownloader Downloader => _downloader;

        public int NumPage { get; set; } = 1;
        public List<string> Boards { get; set; }
        public string StopPid { get; set; }
        public string SelectedBoard { set; get; } = "censored";
        public bool StopOnExistingId { get; set; } = true;

        public ICommand CmdStop { get; private set; }
        public SpiderSehuatang2(SpiderViewModel browser) : base(browser)
        {
            Name = "sehuatang2";
            URL = "https://www.sehuatang.org/";
            ScriptName = "Sehuatang2.js";
            _downloader = new ShtDownloader(Browser);

            Boards = new List<string>
            {
                "censored", "uncensored", "subtitle"
            };
            CmdStop = new RelayCommand(() => _itemQueue.Clear());

            SearchMedia = new AvTorrent(GetConf("DataPath")); 
        }

        protected override string GetScript(string name)
        {
            var template = Template.Parse(App.ReadResource(name));
            return template.Render(new { Board = SelectedBoard, PageCount = NumPage });
        }

        private string GetConf(string key)
        {
            if (key == "DataPath")
            {
                if (!App.GConf.Sections.ContainsSection("general"))
                {
                    return null;
                }

                var general = App.GConf["general"];
                var path = !general.ContainsKey("data_path") ?
                    @"c:\tmp\sht" : @$"{general["data_path"]}sehuatang";
                path += $@"\{SelectedBoard}";
                ShtDownloader.CreateDir(path);
                return path;
            }
            return null;
        }

        public override void Navigate2(IAvMedia _)
        {
            IsSpiderWorking = true;
            if (Browser.Address == URL)
            {
                Browser.Address = "";
            }
            Browser.Address = URL;
        }

        protected override void UpdateDb(IDictionary<string, object> items)
        {
            var path = @$"{SearchMedia.Path}\{items["pid"]}";
            Browser.MediaList.AddMedia(path);
        }
    }
}
