using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

using CefSharp;
using CefSharp.Wpf;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

using HappyHour.Model;
using HappyHour.Spider;
using HappyHour.ScrapItems;
using HappyHour.CefHandler;
using HappyHour.Utils;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    using SpiderEnum = IEnumerable<SpiderBase>;
    using CefConsoleMsg = NotificationMessage<ConsoleMessageEventArgs>;
    using CefStatusMsg = NotificationMessage<StatusMessageEventArgs>;

    partial class SpiderViewModel : Pane
    {
        int _nextScrappingIndex = 0;
        List<MediaItem> _mediaToScrap;
        MediaItem _selectedMedia;
        IMediaList _mediaList;
        string _pid;

        string address;
        public string Address
        {
            get { return address; }
            set { Set(ref address, value); }
        }

        IWpfWebBrowser webBrowser;
        public IWpfWebBrowser WebBrowser
        {
            get { return webBrowser; }
            set { Set(ref webBrowser, value); }
        }

        public List<SpiderBase> Spiders { get; set; }
        public string Pid
        { 
            get => _pid;
            set => Set(ref _pid, value);
        }

        SpiderBase _selectedSpider;
        public SpiderBase SelectedSpider
        {
            get => _selectedSpider;
            set
            {
                if (value != null)
                {
                    value.SetCookies();
                    if (!value.FromCommand) Address = value.URL;
                    if (value is SpiderSehuatang ss)
                    { 
                        SelectedBoard = ss.Boards[0];
                    }
                }
                Set(ref _selectedSpider, value);
            }
        }

        public ICommand CmdStart { get; private set; }
        public ICommand CmdStop { get; private set; }
        public ICommand CmdReloadUrl { get; private set; }
        public ICommand CmdBack { get; private set; }

        public IMediaList MediaList
        {
            get => _mediaList;
            set
            {
                _mediaList = value;
                _mediaList.ItemSelectedHandler += (o, i) =>
                {
                    _selectedMedia = i;
                    if (i != null) Pid = i.Pid;
                };
            }
        }

        public SpiderViewModel()
        {
            CmdStart = new RelayCommand(() => OnStartScrapping(_selectedMedia,true));
            CmdStop = new RelayCommand(() => StopScrapping(null));
            CmdReloadUrl = new RelayCommand(() => WebBrowser.Reload());
            CmdBack = new RelayCommand(() => WebBrowser.Back());

            Spiders = new List<SpiderBase>
            {
                new SpiderSehuatang(this),
                new SpiderR18(this),
                new SpiderJavlibrary(this),
                new SpiderJavmoive(this),
                new SpiderDmm(this),
                new SpiderMgstage(this),
                new SpiderAVE(this),
                new SpiderJavDb(this),
                new SpiderJavfree(this),
                new SpiderPornav(this),
                new SpiderAvsox(this),
            };
            _selectedSpider = Spiders[0];
            Title = Address = _selectedSpider.URL;

            PropertyChanged += OnPropertyChanged;
            MessengerInstance.Send(new NotificationMessage<SpiderEnum>(Spiders, ""));
        }

        public void StartBatchedScrapping(List<MediaItem> mediaItems = null)
        {
            if (mediaItems != null) _mediaToScrap = mediaItems;
            if (_mediaToScrap.Count <= _nextScrappingIndex)
            {
                _mediaToScrap.Clear();
                _nextScrappingIndex = 0;
                return;
            }
            var media = _mediaToScrap[_nextScrappingIndex++];
            Pid = media.Pid;
            OnStartScrapping(media);
        }

        public void OnStartScrapping(MediaItem mitem, bool bSkipScrap = false)
        {
            SelectedSpider.Navigate(mitem, bSkipScrap);
        }

        public void StopScrapping(MediaItem mitem)
        {
            webBrowser.Stop();
            if (mitem == null)
            {
                SelectedSpider.EnableScrapIntoDb = false;
                return;
            }

            UiServices.Invoke(delegate {
                mitem.UpdateFields();

                if (_nextScrappingIndex > 0)
                    StartBatchedScrapping();
                else
                {
                    SelectedSpider.EnableScrapIntoDb = false;
                    _nextScrappingIndex = 0;
                }
            });
        }

        public DownloadHandler DownloadHandler { get; private set; }
        public void InitBrowser()
        {
            DownloadHandler = new DownloadHandler();

            WebBrowser.MenuHandler = new MenuHandler();
            WebBrowser.DownloadHandler = DownloadHandler;
            WebBrowser.LifeSpanHandler = new PopupHandler();
            //WebBrowser.RequestHandler = new AvRequestHandler();

            WebBrowser.ConsoleMessage += (s, e) =>
            {
                MessengerInstance.Send(new CefConsoleMsg(e, "log"));
            };
            WebBrowser.StatusMessage += (s, e) =>
            { 
                MessengerInstance.Send(new CefStatusMsg(e, "log"));
            };

            WebBrowser.LoadingStateChanged += OnStateChanged;
            _selectedSpider.SetCookies();
        }

        void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(WebBrowser):
                    if (WebBrowser == null) break;
                    InitBrowser();
                    Log.Print("Spider Browser changed!");

                    break;
                case nameof(Address):
                    Title = Address;
                    Log.Print("Spider Address changed: " + Address);
                    break;
            }
        }

        void OnStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                SelectedSpider.Scrap();
            }
        }

        public void Download(string url)
        {
            var host = webBrowser.GetBrowserHost();
            host.StartDownload(url);
        }

        public void ExecJavaScript(string s, OnJsResult callback = null)
        {
            webBrowser.EvaluateScriptAsync(s).ContinueWith(x =>
            {
                var response = x.Result;
                if (!response.Success)
                {
                    Log.Print(response.Message);
                    return;
                }
                if (response.Result == null)
                {
                    callback?.Invoke(null);
                }
                else if (response.Result is List<object> list)
                {
                    callback?.Invoke(list);
                }
                else
                {
                    Log.Print("Result is not list!!");
                }
            });
        }
        public void ExecJavaScript(string s, IScrapItem item, string name)
        {
            webBrowser.EvaluateScriptAsync(s).ContinueWith(x =>
            {
                if (!x.Result.Success)
                {
                    Log.Print(x.Result.Message);
                    return;
                }
                item.OnJsResult(name, x.Result.Result as List<object>);
            });
        }
    }
}
