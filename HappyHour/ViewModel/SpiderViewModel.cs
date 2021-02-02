using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

using CefSharp;

using GalaSoft.MvvmLight.Command;

using HappyHour.Spider;
using HappyHour.ScrapItems;
using HappyHour.CefHandler;
using HappyHour.Extension;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class SpiderViewModel : BrowserBase
    {
        string _keyword;
        IMediaList _mediaList;
        SpiderBase _selectedSpider;

        public List<SpiderBase> Spiders { get; set; }
        public string Keyword
        { 
            get => _keyword;
            set => Set(ref _keyword, value);
        }
        public string DataPath { get; set; }
        public SpiderBase SelectedSpider
        {
            get => _selectedSpider;
            set
            {
                if (value == null) return;
                if (_selectedSpider != value)
                {
                    if (_selectedSpider != null)
                        _selectedSpider.OnDeselect();
                    value.OnSelected();
                    UpdateBrowserHeader(value.Name);
                    value.SetCookies();
                    Set(ref _selectedSpider, value);
                }

                if (string.IsNullOrEmpty(value.Keyword))
                    Address = value.URL;
                else
                    Address = value.SearchURL;
            }
        }
        public DownloadHandler DownloadHandler { get; private set; }

        public ICommand CmdStart { get; private set; }
        public ICommand CmdStop { get; private set; }

        public IMainView MainView { get; set; }
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
                    if (i != null)
                    {
                        DataPath = i.MediaPath;
                        Keyword = i.Pid;
                    }
                    else
                    { 
                        DataPath = null;
                        Keyword = null;
                    }
                };
            }
        }

        public SpiderViewModel() : base()
        {
            CmdStart = new RelayCommand(() =>
            {
                SelectedSpider.Keyword = Keyword;
                SelectedSpider.Navigate2();
            });
            CmdStop = new RelayCommand(() => SelectedSpider.Stop());

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
        }

        void OnStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                SelectedSpider.Scrap();
            }
        }

        void OnFrameLoaded(object sender, FrameLoadEndEventArgs e)
        {
            ExecJavaScript(App.ReadResource("Highlight.js"));
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
