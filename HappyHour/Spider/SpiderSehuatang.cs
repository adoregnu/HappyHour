using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;

using Scriban;

using CefSharp;

using HappyHour.ViewModel;
using HappyHour.Interfaces;
using HappyHour.CefHandler;
using CefSharp.Handler;
using CommunityToolkit.Mvvm.Input;
using System.Threading;
using System.Threading.Tasks;
using HappyHour.Model;

namespace HappyHour.Spider
{
    internal class SpiderSehuatang : SpiderBase
    {
        private int _index;
        private int _pageNum = 1;
        private bool _scrapRunning;
        private int _numDuplicatedPid;

        private string _pid;
        private DateTime _updateTime;
        private dynamic _currPage;
        private string _selectedBoard;

        private readonly TorrentDbContext _db = new();
        private Torrent _currentTorrent;

        public int NumPage { get; set; } = 1;
        public List<string> Boards { get; set; }
        public string SelectedBoard
        {
            get => _selectedBoard;
            set => Set(ref _selectedBoard, value);
        } 
        public string PidToStop { get; set; }
        public bool StopOnExistingId { get; set; }

        public ICommand CmdStop { get; private set; }
        public SpiderSehuatang(SpiderViewModel browser) : base(browser)
        {
            Name = "sehuatang";
            URL = "https://www.sehuatang.org/";
            ScriptName = "Sehuatang.js";
            Boards = [ "censored", "uncensored", "subtitle" ];

            CmdStop = new RelayCommand(() => _scrapRunning = false);
            SelectedBoard = "censored";

            UrlPatternsToFilter = ["tupian/forum"];
            ChainVisibility = System.Windows.Visibility.Collapsed;
            var imagePath = $"{App.Current.LocalAppData}\\covers\\";
            if (!Directory.Exists(imagePath))
            { 
                Directory.CreateDirectory(imagePath);
            }
        }

        protected override string GetScript(string name)
        {
            string common = App.ReadResource("Common.js");
            var template = Template.Parse(common + App.ReadResource(name));
            return template.Render(new { Board = SelectedBoard, PageCount = NumPage });
        }

        private void UpdateMedia()
        {
            if (_currentTorrent.StatusCode == 'D' || _currentTorrent.StatusCode == 'E')
                return;

            _db.SaveChanges();
            Browser.MediaList.AddMedia(_currentTorrent);
        }

        readonly List<string> _resources = [];
        public override void UpdateDownload(string rpath)
        {
            UiServices.Invoke(()  => {
                _resources.Add(rpath);
                CheckDownload();
            }); 
        }

        private void CheckDownload()
        {
            if (_toDownload.Count == 0) return;

            _resources.ForEach(r => { _toDownload.Remove(r); });
            Log.Print($"to download  : {_toDownload.Count}");
            if (_toDownload.Count == 0)
            {
                UpdateMedia();
                MoveNextItem();
                _resources.Clear();
                _toDownload.Clear();
            }
        }

        readonly List<string> _toDownload = [];
        private void DownloadFiles(dynamic article)
        {
            void processImages(Action<dynamic, string> action)
            {
                if (article.images is List<object> images)
                {
                    foreach (dynamic img in images)
                    {
                        var imgpath = AvImageFilter.GetResourcePath(img.url);
                        action?.Invoke(img, imgpath );
                        _toDownload.Add(imgpath);
                    }
                    CheckDownload();
                }
            }

            if (_currentTorrent.MagnetUrls.Any(url => url.SourceUrl == article.source))
            {
                processImages(null);
                return;
            }

            if (article.magnet is List<object> magnets)
            {
                foreach (string magnet in magnets.Cast<string>())
                {
                    _currentTorrent.MagnetUrls.Add(new Magnet() {
                        SourceUrl= article.source, MagnetUrl= magnet
                    });
                }
            }

            processImages((img, imgpath) => {
                if (img.target.Contains("_cover"))
                {
                    _currentTorrent.CoverPath = imgpath;
                }
                else
                {
                    _currentTorrent.Screenshots.Add(new StringData() { Value = imgpath });
                }
            });
        }

        private bool MoveNextPage()
        {
            Match m = Regex.Match(_currPage.curr_url, @"-(?<page>\d+)\.html");
            if (!m.Success)
            {
                Log.Print($"{Name}: Invalid page url format! {0}", _currPage.curr_ur);
                return false;
            }
            _pageNum = int.Parse(m.Groups["page"].Value, App.Current.enUS) + 1;
            if (_pageNum > NumPage)
            {
                Log.Print($"{Name}: Parsing done!!");
                return false;
            }

            Browser.Address = Regex.Replace(_currPage.curr_url, @"\d+\.html", $"{_pageNum}.html");
            return true;
        }

        private async void MoveNextItem()
        {
            List<object> list = _currPage.data;
            if (list.Count > _index)
            {
                dynamic item = list[_index++];
                if (!string.IsNullOrEmpty(PidToStop) &&
                    _pid.Equals(PidToStop, StringComparison.OrdinalIgnoreCase))
                {
                    _scrapRunning = false;
                    _currentTorrent = null;
                }
                else
                {
                    _pid = item.pid;
                    _currentTorrent = _db.GetTorrent(item.pid);
                    if (_currentTorrent.Date == _updateTime)
                    {
                        _numDuplicatedPid++;
                        Log.Print($"{_pid} is already crawled.");
                    }
                    else
                    {
                        _currentTorrent.Date = _updateTime;
                        _currentTorrent.StatusCode = 'N';
                    }

                    if (StopOnExistingId && _numDuplicatedPid > 3)
                    {
                        _scrapRunning = false;
                    }
                }
                if (_scrapRunning)
                {
                    Browser.MainView.StatusMessage = $"Article:{_index}/{list.Count}, Page:{_pageNum}/{NumPage}";
                    Browser.Address = item.url;
                }
            }
            else
            {
                _scrapRunning = MoveNextPage();
            }

            if (!_scrapRunning)
            {
                await OnScrapCompleted(false);
            }
        }

        public async override ValueTask<bool> OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
        {
            dynamic d = msg.Message;
            if (d.type == "url")
            {
                Browser.Address = d.data;
                return true;
            }
            else if (d.type == "url_list")
            {
                Log.Print($"{Name}: current page:{d.curr_url}, miss:{d.miss}");
                _index = 0;
                _currPage = d;
                MoveNextItem();
                return true;
            }
            else if (d.type == "items")
            {
                if (d.data == 0)
                {
                    await OnScrapCompleted(false);
                    return true;
                }
                Log.Print($"{Name}: article {_pid} = {d.pid}");
                _updateTime = DateTime.Parse(d.date);
                DownloadFiles(d);
            }
            return false;
        }

        public override void Navigate2(IAvMedia media, bool resetChain)
        {
            IsSpiderWorking = true;
            _numDuplicatedPid = 0;
            _pageNum = 1;
            _scrapRunning = true;
            _resources.Clear();
            _toDownload.Clear();

            if (Browser.Address == URL)
            {
                Browser.Address = "";
            }
            Browser.Address = URL;
        }
    }
}
