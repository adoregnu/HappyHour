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
        private int _index;
        private bool _scrapRunning;
        private bool _skipDownload;

        private string _pid;
        private string _outPath;
        private DateTime _updateTime;
        private dynamic _currPage;
        private readonly string _dataPath;
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
            Name = "sehuatang";
            URL = "https://www.sehuatang.org/";
            ScriptName = "Sehuatang.js";
            _downloader = new ShtDownloader(Browser);

            Boards = new List<string>
            {
                "censored", "uncensored", "subtitle"
            };
            CmdStop = new RelayCommand(() => _scrapRunning = false);

            SearchMedia = new AvTorrent(GetConf("DataPath")); 
        }

        protected override string GetScript(string name)
        {
            var template = Template.Parse(App.ReadResource(name));
            return template.Render(new { Board = SelectedBoard, PageCount = NumPage });
        }

        private void UpdateMedia()
        {
            Browser.MediaList.AddMedia(_outPath);
        }

        private void CreateDir()
        {
            _skipDownload = false;
            DirectoryInfo di = new(_outPath);
            if (!di.Exists)
            {
                Directory.CreateDirectory(_outPath);
                return;
            }
            _skipDownload = true;
            Log.Print($"{Name}: Already downloaded! {_outPath}");
            if (StopOnExistingId)
            {
                _scrapRunning = false;
            }
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
                return !general.ContainsKey("data_path") ?
                    null : $"{general["data_path"]}sehuatang\\{SelectedBoard}\\";
            }
            return null;
        }

        public override void Navigate2(IAvMedia _)
        {
            IsSpiderWorking = true;
            _scrapRunning = true;
            if (Browser.Address == URL)
            {
                Browser.Address = "";
            }
            Browser.Address = URL;
        }

        protected override void UpdateDb(IDictionary<string, object> items)
        { 
        }
    }
}
