using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using CefSharp;
using HappyHour.Interfaces;
using HappyHour.ViewModel;

namespace HappyHour.CefHandler
{
    class PopupHandler : ILifeSpanHandler
    {
        readonly IMainView _mainVm;
        readonly PopupRequestHandler popupRequestHandler = new();
        public PopupHandler(IMainView vm)
        {
            _mainVm = vm;
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
            //windowInfo.Style = (uint)ProcessWindowStyle.Minimized;
            //Log.Print($"Popup windows has supressed. {targetUrl}");

            UiServices.Invoke(() =>
            {
                //browserControl.Address
                popupRequestHandler.RefAddress = browserControl.Address;

                var newtap = _mainVm.NewBrowser();
                newtap.RequestHandler = popupRequestHandler;
                newtap.Address = targetUrl;
            });
            return true;
        }

        void ILifeSpanHandler.OnAfterCreated(
            IWebBrowser browserControl, IBrowser browser)
        {
            //Log.Print("ILifeSpanHandler.OnAfterCreated");
        }

        bool ILifeSpanHandler.DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            //Log.Print("ILifeSpanHandler.DoClose");
            return false;
        }

        void ILifeSpanHandler.OnBeforeClose(
            IWebBrowser browserControl, IBrowser browser)
        {
            //Log.Print("ILifeSpanHandler.OnBeforeClose");
        }
    }
}
