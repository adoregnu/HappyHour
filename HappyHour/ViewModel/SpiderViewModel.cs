using System.Linq;
using System.Timers;
using System.Collections.Generic;

using CefSharp;

using HappyHour.Spider;
using HappyHour.ScrapItems;
using HappyHour.CefHandler;
using HappyHour.Extension;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class SpiderViewModel : BrowserBase
    {
        IMediaList _mediaList;
        SpiderBase _selectedSpider;

        public List<SpiderBase> Spiders { get; set; }
        public SpiderBase SelectedSpider
        {
            get => _selectedSpider;
            set
            {
                SetSpider(value);
                Set(ref _selectedSpider, value);
            }
        }
        public DownloadHandler DownloadHandler { get; private set; }

        public IMediaList MediaList
        {
            get => _mediaList;
            set
            {
                if (value == null) return;

                _mediaList = value;
                _mediaList.SpiderList = Spiders;
                _mediaList.ItemSelectedHandler += (o, i) =>
                {
                    if (SelectedSpider == null) return;
                    if (i != null)
                    {
                        SelectedSpider.DataPath = i.MediaPath;
                        SelectedSpider.Keyword = i.Pid;
                    }
                    else
                    {
                        SelectedSpider.DataPath = null;
                        SelectedSpider.Keyword = null;
                    }
                };
            }
        }

        public IDbView DbView { get; set; }

        public SpiderViewModel() : base()
        {
            Spiders = new List<SpiderBase>
            {
                new SpiderSehuatang(this),
                new SpiderR18(this),
                new SpiderJavlibrary(this),
                new SpiderJavmoive(this),
                new SpiderAvwiki(this),
                new SpiderMgstage(this),
                new SpiderDmm(this),
                new SpiderAVE(this),
                new SpiderJavDb(this),
                new SpiderJavBus(this),
                new SpiderJavfree(this),
                new SpiderPornav(this),
                new SpiderAvsox(this),
                new SpiderAvdbs(this),
                new SpiderAvJamak(this),
            };
            SelectedSpider = Spiders[0];
            Address = SelectedSpider.URL;
        }

        public void SetSpider(SpiderBase spider)
        {
            if (spider == null) return;
            if (_selectedSpider != spider)
            {
                if (_selectedSpider != null)
                {
                    _selectedSpider.OnDeselect();
                    if (spider.OverrideKeyword && !string.IsNullOrEmpty(_selectedSpider.Keyword))
                    {
                        spider.Keyword = _selectedSpider.Keyword;
                    }
                }
                spider.OnSelected();
                spider.SetCookies();
                UpdateBrowserHeader(spider.Name);
            }

            string newUrl;
            if (string.IsNullOrEmpty(spider.Keyword))
            {
                newUrl = spider.URL;
            }
            else
            {
                newUrl = spider.SearchURL;
            }

            if (Address == newUrl)
            {
                Address = "";
            }
            Log.Print($"Set new url : {newUrl}");
            Address = newUrl;
            spider.OverrideKeyword = true;
        }

        void UpdateBrowserHeader(string spiderName)
        {
            HeaderType = spiderName switch
            {
                "sehuatang" or "JavBus" => spiderName,
                _ => "spider",
            };
        }

        protected override void InitBrowser()
        {
            base.InitBrowser();
            DownloadHandler = new DownloadHandler();
            WebBrowser.DownloadHandler = DownloadHandler;
            WebBrowser.MenuHandler = new MenuHandler(this);
            WebBrowser.LifeSpanHandler = new PopupHandler();

            WebBrowser.LoadingStateChanged += OnStateChanged;
            //WebBrowser.JavascriptMessageReceived += OnJavascriptMessageReceived;
            //WebBrowser.FrameLoadEnd += OnFrameLoaded;
            SelectedSpider.SetCookies();
            _timer = new Timer(100)
            {
                AutoReset = false
            };
            _timer.Elapsed += (s, e) => SelectedSpider.Scrap();
        }

        Timer _timer;
        void OnStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                Log.Print($"Loading Done. Number of frames:{e.Browser.GetFrameCount()}");
                if (_timer.Enabled)
                {
                    Log.Print("Timer already enabled!");
                    _timer.Stop();
                }
                _timer.Start();
            }
        }

        void OnFrameLoaded(object sender, FrameLoadEndEventArgs e)
        {
            //ExecJavaScript(App.ReadResource("Highlight.js"));
            //Log.Print($"FrameLoaded isMain:{e.Frame.IsMain}");
        }

        void OnJavascriptMessageReceived(object sender, JavascriptMessageReceivedEventArgs e)
        {
            Log.Print(e.Message.ToString());
        }

        public void ExecJavaScript(string s, IScrapItem item, string name)
        {
            if (!CanExecuteJS()) return;

            WebBrowser.EvaluateScriptAsync(s).ContinueWith(x =>
            {
                if (!x.Result.Success)
                {
                    Log.Print(x.Result.Message);
                    return;
                }
                item.OnJsResult(name, x.Result.Result.ToList<object>());
            });
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (_mediaList != null)
            {
                _mediaList.SpiderList = null;
                _mediaList.ItemSelectedHandler = null;
            }
            _mediaList = null;
            MainView = null;
        }
    }
}
