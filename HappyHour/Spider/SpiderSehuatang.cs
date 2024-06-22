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

namespace HappyHour.Spider
{
    internal class SpiderSehuatang : SpiderBase
    {
        private int _index;
        private int _pageNum = 1;
        private bool _scrapRunning;
        private bool _dirExists;
        private int _numDuplicatedPid;

        private string _pid;
        private string _outPath;
        private DateTime _updateTime;
        private dynamic _currPage;
        private string _selectedBoard;
        private string _dataPath;

        private readonly Dictionary<string, string> _images = new();
        private ILifeSpanHandler _popupHandler;
        private Timer _downloadTimer;

        public int NumPage { get; set; } = 1;
        public List<string> Boards { get; set; }
        public string SelectedBoard
        {
            get => _selectedBoard;
            set
            {
                Set(ref _selectedBoard, value);
                if (value != null)
                {
                    _dataPath = App.Current.GetConf("general", "data_path") ?? @"d:\tmp\sehuatang";
                    _dataPath += @$"\{value}\";
                }
            }
        } 
        public string PidToStop { get; set; }
        public bool StopOnExistingId { get; set; }

        public ICommand CmdStop { get; private set; }
        public SpiderSehuatang(SpiderViewModel browser) : base(browser)
        {
            Name = "sehuatang";
            URL = "https://www.sehuatang.org/";
            //URL = "https://tupd.xsmy54s.com/tupian/forum/202302/28/205120gxjzpj8jkk558kea.jpg";
            ScriptName = "Sehuatang.js";

            Boards = new List<string>
            {
                "censored", "uncensored", "subtitle"
            };
            CmdStop = new RelayCommand(() => _scrapRunning = false);
            SelectedBoard = "censored";

            ResourcesToBeFiltered = new Dictionary<string, string>();
            //browser.RequestHandler = new AvRequestHandler(this);

            _popupHandler = new OffScreenPopupHandler(Browser);
            ReqeustHandler = new AvRequestHandler(this);
            _downloadTimer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        protected override string GetScript(string name)
        {
            string common = App.ReadResource("Common.js");
            var template = Template.Parse(common + App.ReadResource(name));
            return template.Render(new { Board = SelectedBoard, PageCount = NumPage });
        }

        private void UpdateMedia()
        {
            Browser.MediaList.AddMedia(_outPath);
        }
        private void CreateDir()
        {
            _dirExists = false;
            DirectoryInfo di = new(_outPath);
            if (!di.Exists)
            {
                _ = Directory.CreateDirectory(_outPath);
                return;
            }
            if (Directory.GetFiles(_outPath)
                .Any(f => f.EndsWith(".torrent") || f.EndsWith(".magnet")))
            {
                _dirExists = true;
                Log.Print($"{Name}: Already downloaded! {_outPath}");
                _numDuplicatedPid++;
                if (StopOnExistingId && _numDuplicatedPid > 3)
                {
                    _scrapRunning = false;
                }
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

            //int i = 0;
            _images.Clear();
            _numDownloaded = 0;

            var images = article.images as List<object>;
            //var files = article.files as List<object>;
            if (images != null) { _toDownload = images.Count; }
            //if (files != null) { _toDownload += files.Count; }
            if (article.magnet is List<object> magnets)
            {
                int count = 1;
                foreach (string magnet in magnets.Cast<string>())
                {
                    File.WriteAllText($"{_outPath}\\{_pid}_{count}.magnet", magnet);
                    count++;
                }
            }

            Log.Print($"{Name}: toDownload : {_toDownload }");
            if (_toDownload == 0)
            {
                MoveNextItem();
                return;
            }

            if (images != null)
            {
                foreach (dynamic img in images)
                {
                    string target = _outPath + $"\\{img.target}";
                    ResourcesToBeFiltered.Add(img.url, target);
                }
                foreach (dynamic img in images)
                {
                    ((IJavascriptCallback)img.func).ExecuteAsync();
                    //_numDownloaded++;
                }
            }
            //files?.ForEach(fn => ((IJavascriptCallback)fn).ExecuteAsync());
            _downloadTimer.Change(2 * 1000, Timeout.Infinite);
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
                if (!string.IsNullOrEmpty(PidToStop) &&
                    _pid.Equals(PidToStop, StringComparison.OrdinalIgnoreCase))
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
                //Thread.Sleep(1000);
            }

            if (!_scrapRunning)
            {
                OnScrapCompleted(false);
            }
        }

        public override bool OnJsMessageReceived(JavascriptMessageReceivedEventArgs msg)
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
                    OnScrapCompleted(false);
                    return true;
                }
                Log.Print($"{Name}: article {_pid} = {d.pid}");
                _updateTime = DateTime.Parse(d.date);
                if (!_dirExists || OverwritePoster)
                {
                    DownloadFiles(d);
                }
                else
                {
                    UpdateMedia();
                    MoveNextItem();
                }
            }
            return false;
        }

        protected override void OnScrapCompleted(bool bUpdated)
        {
            base.OnScrapCompleted(bUpdated);
            (Browser.WebBrowser.LifeSpanHandler, _popupHandler) = (_popupHandler, Browser.WebBrowser.LifeSpanHandler);
            _downloadTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public override void Navigate2(IAvMedia _)
        {
            IsSpiderWorking = true;
            _numDuplicatedPid = 0;
            _pageNum = 1;
            _scrapRunning = true;

            ResourcesToBeFiltered.Clear();
            (Browser.WebBrowser.LifeSpanHandler, _popupHandler) = (_popupHandler, Browser.WebBrowser.LifeSpanHandler);

            if (Browser.Address == URL)
            {
                Browser.Address = "";
            }
            Browser.Address = URL;
        }

        private void OnBeforeDownload(object sender, DownloadItem e)
        {
            //e.SuggestedFileName = !e.SuggestedFileName.EndsWith("torrent", StringComparison.OrdinalIgnoreCase) ?
            //    _images[e.OriginalUrl] : $"{_outPath}\\{e.SuggestedFileName}";

            e.SuggestedFileName = $"{_outPath}\\{e.SuggestedFileName}";
        }

        private void TimerCallback(object state)
        {
            _downloadTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Log.Print("Download timed out!!");
            UiServices.Invoke(() =>
            {
                _numDownloaded = _toDownload = 0;
                UpdateMedia();
                MoveNextItem();
            });
        }

        public override void UpdateDownload()
        {
            UiServices.Invoke(() =>
            {
                _numDownloaded++;
                if (_toDownload == _numDownloaded)
                {
                    UpdateMedia();
                    MoveNextItem();
                }
            });
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
                UiServices.Invoke(() =>
                {
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
