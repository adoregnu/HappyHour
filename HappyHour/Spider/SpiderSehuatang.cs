using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;

using GalaSoft.MvvmLight.Command;

using Scriban;

using CefSharp;

using HappyHour.ViewModel;
using HappyHour.Interfaces;

namespace HappyHour.Spider
{
    internal class SpiderSehuatang : SpiderBase
    {
        private int _index;
        private int _pageNum = 1;
        private bool _scrapRunning;
        private bool _skipDownload;

        private string _pid;
        private string _outPath;
        private DateTime _updateTime;
        private dynamic _currPage;

        private readonly string _dataPath;
        private readonly Dictionary<string, string> _images;

        public int NumPage { get; set; } = 1;
        public List<string> Boards { get; set; }
        public string SelectedBoard { set; get; } = "censored";
        public string StopPid { get; set; }
        public bool StopOnExistingId { get; set; } = true;

        public ICommand CmdStop { get; private set; }
        public SpiderSehuatang(SpiderViewModel browser) : base(browser)
        {
            Name = "sehuatang";
            URL = "https://www.sehuatang.org/";
            ScriptName = "Sehuatang.js";

            Boards = new List<string>
            {
                "censored", "uncensored", "subtitle"
            };
            CmdStop = new RelayCommand(() => _scrapRunning = false);

            _dataPath = GetConf("DataPath");
            _images = new Dictionary<string, string>();
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

        private int _numDownloaded;
        private int _toDownload;
        private void DownloadFiles(dynamic article)
        {
            if (_toDownload != _numDownloaded)
            {
                Log.Print($"{Name}: Previous downloading is not completed!");
                return;
            }
            List<object> images = article.images;
            List<object> files = article.files;

            int i = 0;
            _images.Clear();
            _numDownloaded = 0;
            _toDownload = images.Count + files.Count;
            Log.Print($"{Name}: toDownload : {_toDownload }");

            files.ForEach(f => ((IJavascriptCallback)f).ExecuteAsync());
            foreach (string file in images)
            {
                string postfix = (i == 0) ? "cover" : $"screenshot{i}";
                string target = _outPath + $"\\{_pid}_{postfix}{Path.GetExtension(file)}";

                _images.Add(file, target);
                Browser.Download(file);
                i++;
            }
        }

        private bool MoveNextPage()
        {
            Match m = Regex.Match(_currPage.curr_url, @"-(?<page>\d+)\.html");
            if (!m.Success)
            {
                Log.Print($"{Name}: Invalid page url format! {0}", _currPage.curr_ur);
                return false;
            }
            _pageNum = int.Parse(m.Groups["page"].Value, App.enUS) + 1;
            if (_pageNum > NumPage)
            {
                Log.Print($"{Name}: Parsing done!!");
                return false;
            }

            Browser.Address = Regex.Replace(_currPage.curr_url,
                @"\d+\.html", $"{_pageNum}.html");
            return true;
        }

        private void MoveNextItem()
        {
            List<object> list = _currPage.data;
            if (list.Count > _index)
            {
                dynamic item = list[_index++];
                if (!string.IsNullOrEmpty(StopPid) &&
                    _pid.Equals(StopPid, StringComparison.OrdinalIgnoreCase))
                {
                    _scrapRunning = false;
                }
                else
                {
                    _pid = item.pid;
                    _outPath = _dataPath + item.pid;
                    CreateDir();
                }
                if (_scrapRunning)
                {
                    Browser.MainView.StatusMessage =
                        $"Article:{_index}/{list.Count}, Page:{_pageNum}/{NumPage}";
                    Browser.Address = item.url;
                }
            }
            else
            {
                _scrapRunning = MoveNextPage();
            }

            if (!_scrapRunning)
            {
                OnScrapCompleted(false);
            }
        }

        public override void OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
        {
            dynamic d = msg.Message;
            if (d.type == "url")
            {
                Browser.Address = d.data;
            }
            else if (d.type == "url_list")
            {
                Log.Print($"{Name}: current page:{d.curr_url}, miss:{d.miss}");
                _index = 0;
                _currPage = d;
                MoveNextItem();
            }
            else if (d.type == "items")
            {
                Log.Print($"{Name}: article {_pid} = {d.pid}");
                _updateTime = DateTime.Parse(d.date);
                if (!_skipDownload)
                {
                    DownloadFiles(d);
                }
                else
                {
                    UpdateMedia();
                    MoveNextItem();
                }
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
            _pageNum = 1;
            _scrapRunning = true;
            if (Browser.Address == URL)
            {
                Browser.Address = "";
            }
            Browser.Address = URL;
        }

        private void OnBeforeDownload(object sender, DownloadItem e)
        {
            e.SuggestedFileName = !e.SuggestedFileName.EndsWith("torrent", StringComparison.OrdinalIgnoreCase) ?
                _images[e.OriginalUrl] : $"{_outPath}\\{e.SuggestedFileName}";
        }

        private void OnDownloadUpdated(object sender, DownloadItem e)
        {
            if (!e.IsComplete)
            {
                return;
            }

            _numDownloaded++;
            Log.Print($"{Name}: download completed({_numDownloaded}/{_toDownload}): {e.FullPath}");
            try
            {
                File.SetLastWriteTime(e.FullPath, _updateTime);
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
            if (_toDownload == _numDownloaded)
            {
                UiServices.Invoke( () => {
                    UpdateMedia();
                    MoveNextItem();
                });
            }
        }

        public override void OnSelected()
        {
            //base.OnSelected();
            Log.Print($"{Name} selected!");
            var dh = Browser.DownloadHandler;
            dh.OnBeforeDownloadFired += OnBeforeDownload;
            dh.OnDownloadUpdatedFired += OnDownloadUpdated;
        }

        public override void OnDeselect()
        {
            Log.Print($"{Name} deselected!");
            //base.OnDeselect();
            var dh = Browser.DownloadHandler;
            dh.OnBeforeDownloadFired -= OnBeforeDownload;
            dh.OnDownloadUpdatedFired -= OnDownloadUpdated;
        }
    }
}
