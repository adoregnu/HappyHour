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
        private string _imagePath;
        private DateTime _updateTime;
        private dynamic _currPage;
        private string _selectedBoard;

        private ILifeSpanHandler _popupHandler;
        private readonly Timer _downloadTimer;
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

            ResourcesToBeFiltered = [];
            _popupHandler = new OffScreenPopupHandler(Browser);
            ReqeustHandler = new ShtRequestHandler(this);
            _downloadTimer = new Timer(TimerCallback, null, Timeout.Infinite, Timeout.Infinite);
            ChainVisibility = System.Windows.Visibility.Collapsed;
            _imagePath = $"{App.Current.LocalAppData}\\covers";
            if (!Directory.Exists(_imagePath))
            { 
                Directory.CreateDirectory(_imagePath);
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
            //string target = _imagePath + $"\\{img.target}";
            bool cover = true;
            foreach (var kv in ResourcesToBeFiltered)
            {
                if (cover)
                {
                    _currentTorrent.CoverPath = kv.Value;
                    cover = false;
                }
                else
                {
                    _currentTorrent.Screenshots.Add(new StringData() { Value = kv.Value });
                }
            }
            _db.SaveChanges();
            Browser.MediaList.AddMedia(_currentTorrent);
        }

        private int _numDownloaded;
        private int _toDownload;
        private async Task DownloadFiles(dynamic article)
        {
            if (_toDownload != _numDownloaded)
            {
                Log.Print($"{Name}: Previous downloading is not completed!");
                return;
            }

            //int i = 0;
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
                    //File.WriteAllText($"{_outPath}\\{_pid}_{count}.magnet", magnet);
                    _currentTorrent.MagnetUrls.Add(new Magnet() {
                        SourceUrl= article.source, MagnetUrl= magnet
                    });
                    count++;
                }
            }

            Log.Print($"{Name}: toDownload : {_toDownload }");
            if (_toDownload == 0)
            {
                await MoveNextItem();
                return;
            }

            if (images != null)
            {
                foreach (dynamic img in images)
                {
                    string target = _imagePath + $"\\{img.target}";
                    ResourcesToBeFiltered.Add(img.url, target);
                }

                foreach (dynamic img in images)
                {
                    _ = ((IJavascriptCallback)img.func).ExecuteAsync();
                }
            }
            //_downloadTimer.Change(2 * 1000, Timeout.Infinite);
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

        private async Task MoveNextItem()
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
                    _currentTorrent = await _db.GetTorrent(item.pid);
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
                await MoveNextItem();
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
                await DownloadFiles(d);
                /*
                else
                {
                    UpdateMedia();
                    MoveNextItem();
                }
                */
            }
            return false;
        }
        protected async override Task OnScrapCompleted(bool bUpdated)
        {
            await base.OnScrapCompleted(bUpdated);
            (Browser.WebBrowser.LifeSpanHandler, _popupHandler) = (_popupHandler, Browser.WebBrowser.LifeSpanHandler);
            _downloadTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public override void Navigate2(IAvMedia media, bool resetChain)
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
            UiServices.Invoke(async () =>
            {
                _numDownloaded++;
                if (_toDownload == _numDownloaded)
                {
                    UpdateMedia();
                    await MoveNextItem();
                }
            });
        }

        private void OnBeforeDownload(object sender, DownloadItem e)
        {
            //e.SuggestedFileName = !e.SuggestedFileName.EndsWith("torrent", StringComparison.OrdinalIgnoreCase) ?
            //    _images[e.OriginalUrl] : $"{_outPath}\\{e.SuggestedFileName}";

            e.SuggestedFileName = $"{_imagePath}\\{e.SuggestedFileName}";
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
                UiServices.Invoke(async () =>
                {
                    UpdateMedia();
                    await MoveNextItem();
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

            //(Browser.WebBrowser.LifeSpanHandler, _popupHandler) = (_popupHandler, Browser.WebBrowser.LifeSpanHandler);
        }

        public override void OnDeselect()
        {
            Log.Print($"{Name} deselected!");
            //base.OnDeselect();
            var dh = Browser.DownloadHandler;
            dh.OnBeforeDownloadFired -= OnBeforeDownload;
            dh.OnDownloadUpdatedFired -= OnDownloadUpdated;

            //(Browser.WebBrowser.LifeSpanHandler, _popupHandler) = (_popupHandler, Browser.WebBrowser.LifeSpanHandler);
        }
    }
}
