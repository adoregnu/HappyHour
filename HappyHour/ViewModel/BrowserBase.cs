﻿using System.ComponentModel;

using CefSharp;
using CefSharp.Wpf;

using HappyHour.CefHandler;
using CommunityToolkit.Mvvm.Messaging;

namespace HappyHour.ViewModel
{
    delegate void OnJsResult(object items);

    class BrowserBase : Pane
    {
        string _address;
        string _headerType;
        IWpfWebBrowser _webBrowser;

        public bool IsAddressChanged = false;
        public bool IsEnabled { get; set; } = true;
            
        public string HeaderType
        {
            get => _headerType;
            set => SetProperty(ref _headerType, value);
        }

        public string Address
        {
            get { return _address; }
            set { SetProperty(ref _address, value); }
        }

        public IWpfWebBrowser WebBrowser
        {
            get { return _webBrowser; }
            set { SetProperty(ref _webBrowser, value); }
        }

        public IRequestHandler RequestHandler { get; set; }

        public BrowserBase()
        {
            Title = "Browser";
            HeaderType = "base";
            PropertyChanged += OnPropertyChanged;
            Address = "www.google.com";
        }

        protected virtual void InitBrowser()
        {
            WebBrowser.ConsoleMessage += (s, e) => Messenger.Send(e);
            WebBrowser.StatusMessage += (s, e) => Messenger.Send(e);
            if (RequestHandler != null)
            {
                WebBrowser.RequestHandler = RequestHandler;
            }
            
            WebBrowser.MenuHandler = new MenuHandler();
            WebBrowser.LifeSpanHandler = new PopupHandler(MainView);
        }

        protected virtual void OnAddressChanged()
        {
            //Title = Address;
            Log.Print("Address changed: " + Address);
            IsAddressChanged = true;
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
                    OnAddressChanged();
                    break;
            }
        }

        public void Download(string url)
        {
            var host = WebBrowser.GetBrowserHost();
            host.StartDownload(url);
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
            if (!CanExecuteJS())
            {
                return;
            }

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
#if false
        public void Login(string jsTpl)
        {
            var tpl = Template.Parse(App.ReadResource(jsTpl));
            if (!App.GConf.Sections.ContainsSection("general"))
            {
                return;
            }
            if (!App.GConf["general"].ContainsKey("userid") ||
                !App.GConf["general"].ContainsKey("password"))
            {
                return;
            }

            var js = tpl.Render(new
            {
                Userid = App.GConf["general"]["userid"],
                Password = App.GConf["general"]["password"]
            });
            WebBrowser.ExecuteScriptAsync(js);
        }
#endif
        public override void Cleanup()
        {
            PropertyChanged -= OnPropertyChanged;
            WebBrowser.Dispose();
            base.Cleanup();
        }
    }
}
