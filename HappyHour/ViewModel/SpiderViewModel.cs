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

namespace HappyHour.ViewModel
{
    using SpiderEnum = IEnumerable<SpiderBase>;
    using CefConsoleMsg = NotificationMessage<ConsoleMessageEventArgs>;
    using CefStatusMsg = NotificationMessage<StatusMessageEventArgs>;

    partial class SpiderViewModel : Pane
    {
        bool _bStarted = false;
        int _nextScrappingIndex = 0;
        List<MediaItem> _mediaToScrap;
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

        public MediaItem SelectedMedia;

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

        public ICommand CmdReloadUrl { get; private set; }
        public ICommand CmdBack { get; private set; }
        public SpiderViewModel()
        {
            CmdStart = new RelayCommand(() => OnStartScrapping(SelectedMedia,true));
            CmdStop = new RelayCommand(() => StopScrapping(null, true));
            CmdReloadUrl = new RelayCommand(() => WebBrowser.Reload());
            CmdBack = new RelayCommand(() => WebBrowser.Back());

            Spiders = new List<SpiderBase>
            {
                new SpiderSehuatang(this),
                new SpiderR18(this),
                new SpiderJavlibrary(this),
                new SpiderDmm(this),
                new SpiderMgstage(this),
                new SpiderAVE(this),
                new SpiderJavDb(this),
                //new SpiderJavfree(this)
                new SpiderJavmoive(this)
            };
            _selectedSpider = Spiders[0];
            Title = Address = _selectedSpider.URL;

            PropertyChanged += OnPropertyChanged;

            MessengerInstance.Register<NotificationMessage<MediaItem>>(
                this, (msg) => {
                    if (msg.Notification != "mediaSelected")
                        return;

                    SelectedMedia = msg.Content;
                    if (SelectedMedia != null)
                    {
                        Pid = SelectedMedia.Pid;
                        //_bStarted = false;
                    }
                });
 
            MessengerInstance.Send(new NotificationMessage<SpiderEnum>(Spiders, ""));
        }

        public void StartBatchedScrapping(List<MediaItem> mediaItems = null)
        {
            if (SelectedSpider is SpiderSehuatang)
            {
                Log.Print($"Not supported in this spider {SelectedSpider}");
                return;
            }

            if (mediaItems != null) _mediaToScrap = mediaItems;
            if (_mediaToScrap.Count <= _nextScrappingIndex)
            {
                _mediaToScrap.Clear();
                _nextScrappingIndex = 0;
                return;
            }
            var media = _mediaToScrap[_nextScrappingIndex++];
            Pid = media.Pid;
            //SelectedMedia = media;
            OnStartScrapping(media);
        }

        public void OnStartScrapping(MediaItem mitem, bool manualSearch = false)
        {
            if (!(SelectedSpider is SpiderSehuatang) &&
                string.IsNullOrEmpty(Pid))
            {
                Log.Print("No Pid is set!");
                return;
            }

            if (SelectedSpider is SpiderSehuatang || !manualSearch)
                _bStarted = true;
            SelectedSpider.Navigate(mitem);
        }

        public void StopScrapping(MediaItem mitem, bool forceStop = false)
        {
            webBrowser.Stop();
            _bStarted = false;
            if (SelectedSpider is SpiderSehuatang) return;

            if (mitem != null)
            {
                UiServices.Invoke(delegate
                {
                    mitem.UpdateFields();
                });
            }

            if (!forceStop && _nextScrappingIndex > 0)
                StartBatchedScrapping();
            else
                _nextScrappingIndex = 0;
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
            if (!e.IsLoading && _bStarted)
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
