using System.Linq;
using System.Collections.Generic;

using CefSharp;

using HappyHour.Spider;
using HappyHour.CefHandler;
using HappyHour.Interfaces;
using System;

namespace HappyHour.ViewModel
{
    internal class SpiderViewModel : BrowserBase
    {
        private IMediaList _mediaList;
        private SpiderBase _selectedSpider;

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
        public Downloader ImageDownloader { get; set; }

        public IMediaList MediaList
        {
            get => _mediaList;
            set
            {
                if (value == null)
                {
                    return;
                }

                _mediaList = value;
                _mediaList.SpiderList = Spiders;
                _mediaList.ItemSelectedHandler += (o, i) =>
                {
                    if (SelectedSpider == null)
                    {
                        return;
                    }
                    SelectedSpider.SelectedMedia = i ?? null;
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
                //new SpiderPornav(this),
                new SpiderAvsox(this),
                new SpiderAvdbs(this),
                new SpiderAvJamak(this),
            };
        }

        public void SetSpider(SpiderBase spider)
        {
            if (spider == null)
            {
                return;
            }

            if (_selectedSpider != spider)
            {
                if (_selectedSpider != null)
                {
                    _selectedSpider.OnDeselect();
                }
                spider.OnSelected();
                spider.SetCookies();
                UpdateBrowserHeader(spider.Name);
            }

            string newUrl = string.IsNullOrEmpty(spider.Keyword) ? spider.URL : spider.SearchURL;
            if (Address == newUrl)
            {
                Address = "";
            }
            Log.Print($"Set new url : {newUrl}");
            Address = newUrl;
        }

        private void UpdateBrowserHeader(string spiderName)
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
            WebBrowser.JavascriptMessageReceived += OnJavascriptMessageReceived;
            //WebBrowser.FrameLoadEnd += OnFrameLoaded;
            SelectedSpider = Spiders[0];
            SelectedSpider.SetCookies();
            Address = SelectedSpider.URL;
#if false
            _timer = new Timer(10)
            {
                AutoReset = false
            };
            _timer.Elapsed += (s, e) => SelectedSpider.Scrap();
#endif
            ImageDownloader = new Downloader(this);
        }

        //private Timer _timer;
        private void OnStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                Log.Print($"Loading Done. Number of frames:{e.Browser.GetFrameCount()}");
                if (IsAddressChanged)
                {
                    IsAddressChanged = false;
                    SelectedSpider.Scrap();
#if false
                    if (_timer.Enabled)
                    {
                        _timer.Stop();
                    }
                    _timer.Start();
#endif
                }
            }
        }
        private void SearchText(dynamic d)
        {
            if (d.action == "google_search")
            {
                string query = d.data.Replace(' ', '+');
                Address = $"https://www.google.com/search?as_epq={query}";
            }
            else if (d.action == "google_translate")
            {
                Address = "https://translate.google.com/" +
                    $"?hl=ko&tab=rT&sl=auto&tl=ko&text={d.data}&op=translate";
            }
            else if (d.action == "pid_search_in_db")
            {
                DbView.SearchText = d.data;
            }
            else if (d.action == "pid_search_in_spider")
            {
                SpiderBase spider = Spiders.FirstOrDefault(s => s.Name == d.spider);
                if (spider != null)
                {
                    spider.Keyword = d.data;
                    SelectedSpider = spider;
                }
            }
        }

        private void OnJavascriptMessageReceived(object sender,
            JavascriptMessageReceivedEventArgs e)
        {
            //Log.Print(e.Message.ToString());
            try
            {
                dynamic d = e.Message;
                if (d.type == "text")
                {
                    UiServices.Invoke(() => SearchText(d));
                }
                else
                {
                    SelectedSpider.OnJsMessageReceived(e);
                }
            }
            catch (Exception ex)
            {
                Log.Print("Error in parsing js message", ex);
            }
        }
#if false
        void OnFrameLoaded(object sender, FrameLoadEndEventArgs e)
        {
            ExecJavaScript(App.ReadResource("Highlight.js"));
            Log.Print($"FrameLoaded isMain:{e.Frame.IsMain}");
        }
#endif
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
