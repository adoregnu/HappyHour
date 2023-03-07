using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using CefSharp;
using CefSharp.OffScreen;
using HappyHour.Interfaces;
using HappyHour.ViewModel;

namespace HappyHour.CefHandler
{
    class OffScreenPopupHandler : ILifeSpanHandler
    {
        readonly SpiderViewModel _spiderVm;
        readonly List<ChromiumWebBrowser> _browserCache = new();
        public OffScreenPopupHandler(SpiderViewModel vm)
        {
            _spiderVm = vm;
        }
        bool ILifeSpanHandler.OnBeforePopup(
            IWebBrowser browserControl,
            IBrowser browser,
            IFrame frame,
            string targetUrl,
            string targetFrameName,
            WindowOpenDisposition targetDisposition,
            bool userGesture,
            IPopupFeatures popupFeatures,
            IWindowInfo windowInfo,
            IBrowserSettings browserSettings,
            ref bool noJavascriptAccess,
            out IWebBrowser newBrowser)
        {
            //Set newBrowser to null unless your attempting to host 
            //the popup in a new instance of ChromiumWebBrowser
            newBrowser = null;
            foreach (var bc in _browserCache)
            {
                if (!bc.IsLoading)
                {
                    bc.LoadUrl(targetUrl);
                    return true;
                }
            }

            _browserCache.Add(new ChromiumWebBrowser(targetUrl)
            {
                RequestHandler = _spiderVm.SelectedSpider.ReqeustHandler,
                DownloadHandler = _spiderVm.DownloadHandler
            });
            return true;
        }

        void ILifeSpanHandler.OnAfterCreated(
            IWebBrowser browserControl, IBrowser browser)
        {
            Log.Print("ILifeSpanHandler.OnAfterCreated");
        }

        bool ILifeSpanHandler.DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            Log.Print("ILifeSpanHandler.DoClose");
            return false;
        }

        void ILifeSpanHandler.OnBeforeClose(
            IWebBrowser browserControl, IBrowser browser)
        {
            Log.Print("ILifeSpanHandler.OnBeforeClose");
        }
    }
}
