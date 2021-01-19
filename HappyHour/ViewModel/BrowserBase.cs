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

    delegate void OnJsResult(object items);

    class BrowserBase : Pane
    {
        string _address;
        string _headerType;
        IWpfWebBrowser _webBrowser;

        public string HeaderType
        {
            get => _headerType;
            set => Set(ref _headerType, value);
        }

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
        public DownloadHandler DownloadHandler { get; private set; }

        public ICommand CmdReloadUrl { get; private set; }
        public ICommand CmdBackward { get; private set; }
        public ICommand CmdForward { get; private set; }

        public BrowserBase()
        {
            Title = "Browser";
            HeaderType = "base";
            PropertyChanged += OnPropertyChanged;

            CmdReloadUrl = new RelayCommand(() => WebBrowser.Reload());
            CmdBackward = new RelayCommand(
                () => WebBrowser.Back(),
                () => WebBrowser != null && WebBrowser.CanGoBack);
            CmdForward = new RelayCommand(
                () => WebBrowser.Forward(),
                () => WebBrowser != null && WebBrowser.CanGoForward);
        }

        protected virtual void InitBrowser()
        {
            WebBrowser.ConsoleMessage += (s, e) =>
            {
                MessengerInstance.Send(new CefConsoleMsg(e, "log"));
            };
            WebBrowser.StatusMessage += (s, e) =>
            {
                MessengerInstance.Send(new CefStatusMsg(e, "log"));
            };
            DownloadHandler = new DownloadHandler();
            WebBrowser.DownloadHandler = DownloadHandler;

            //Address = "https://www.google.com";
        }

        void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(WebBrowser):
                    if (WebBrowser == null) break;
                    InitBrowser();
                    break;

                case nameof(Address):
                    Title = Address;
                    Log.Print("Address changed: " + Address);
                    break;
            }
        }

        public void Download(string url)
        {
            WebBrowser.Dispatcher.Invoke(delegate {
                var host = WebBrowser.GetBrowserHost();
                host.StartDownload(url);
            });
        }

        protected bool CanExecuteJS()
        {
            if (!WebBrowser.CanExecuteJavascriptInMainFrame)
            {
                Log.Print("V8Context is not ready!");
                return false;
            }

            return true;
        }

        public void ExecJavaScript(string s, OnJsResult callback = null)
        {
            if (!CanExecuteJS()) return;

            WebBrowser.EvaluateScriptAsync(s).ContinueWith(x =>
            {
                var response = x.Result;
                if (!response.Success)
                {
                    Log.Print("ExecJavaScript:: " + response.Message);
                    return;
                }
                callback?.Invoke(response.Result);
            });
        }

        public override void Cleanup()
        {
            PropertyChanged -= OnPropertyChanged;
            WebBrowser.Dispose();
            base.Cleanup();
        }
    }
}
