using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using Scriban;
using CefSharp;
using CefSharp.Wpf;

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

        public bool IsAddressChanged = false;

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

        public BrowserBase()
        {
            Title = "Browser";
            HeaderType = "base";
            PropertyChanged += OnPropertyChanged;
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
            WebBrowser.MenuHandler = new MenuHandler();
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

        public override void Cleanup()
        {
            PropertyChanged -= OnPropertyChanged;
            WebBrowser.Dispose();
            base.Cleanup();
        }
    }
}
