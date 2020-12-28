using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using CefSharp;
using CefSharp.Wpf;

using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

using HappyHour.CefHandler;

namespace HappyHour.ViewModel
{
    using CefConsoleMsg = NotificationMessage<ConsoleMessageEventArgs>;
    using CefStatusMsg = NotificationMessage<StatusMessageEventArgs>;

    class BrowserViewModel : Pane
    {
        string _address;
        readonly string _nasUrl;
        IWpfWebBrowser _webBrowser;
        int _numLoading = 0;

        public string Address
        {
            get { return _address; }
            set { Set(ref _address, value); }
        }

        public IWpfWebBrowser WebBrowser
        {
            get { return _webBrowser; }
            set { Set(ref _webBrowser, value); }
        }

        public ICommand CmdReloadUrl { get; private set; }
        public ICommand CmdBackward { get; private set; }
        public ICommand CmdForward { get; private set; }

        public BrowserViewModel()
        {
            Title = "Browser";
            _nasUrl = App.GConf["general"]["nas_url"];
            PropertyChanged += OnPropertyChanged;
        }

        void InitBrowser()
        {
            CmdReloadUrl = new RelayCommand(() => WebBrowser.Reload());
            CmdBackward = new RelayCommand(
                () => WebBrowser.Back(),
                () => WebBrowser.CanGoBack);
            CmdForward = new RelayCommand(
                () => WebBrowser.Forward(),
                () => WebBrowser.CanGoForward);

            WebBrowser.MenuHandler = new MenuHandler();

            WebBrowser.ConsoleMessage += (s, e) =>
            {
                MessengerInstance.Send(new CefConsoleMsg(e, "log"));
            };
            WebBrowser.StatusMessage += (s, e) =>
            {
                MessengerInstance.Send(new CefStatusMsg(e, "log"));
            };
            //WebBrowser.LoadingStateChanged += OnStateChanged;
            WebBrowser.FrameLoadEnd += OnFrameLoaded;
            Address = _nasUrl;
        }

        void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(WebBrowser):
                    if (WebBrowser == null) break;
                    InitBrowser();
                    Log.Print("Browser changed!");

                    break;
                case nameof(Address):
                    Title = Address;
                    Log.Print("Address changed: " + Address);
                    break;
            }
        }
#if false
        void OnStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading && Address == _nasUrl && _numLoading < 2)
            {
                Log.Print($"Num loading :{_numLoading}");
                WebBrowser.ExecuteScriptAsync(App.ReadResource("NasLogin.js"));
                _numLoading++;
            }
        }
#endif
        void OnFrameLoaded(object sender, FrameLoadEndEventArgs e)
        {
            if (Address != _nasUrl) return;

            Log.Print($"Num loading :{++_numLoading}, isMain {e.Frame.IsMain}");
            if (e.Frame.IsMain)
            { 
                WebBrowser.ExecuteScriptAsync(App.ReadResource("NasLogin.js"));
            }
            //if (_numLoading == 4)
            //    WebBrowser.ExecuteScriptAsync(App.ReadResource("NasLogin.js"));
        }
    }
}
