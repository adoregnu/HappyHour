using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

using CefSharp;

namespace HappyHour.CefHandler
{
    class PopupHandler : ILifeSpanHandler
    {
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
            windowInfo.Style = (uint)ProcessWindowStyle.Hidden;
            Log.Print($"Popup windows has supressed. {targetUrl}");
            return false;
        }

        void ILifeSpanHandler.OnAfterCreated(
            IWebBrowser browserControl, IBrowser browser)
        {
        }

        bool ILifeSpanHandler.DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            return false;
        }

        void ILifeSpanHandler.OnBeforeClose(
            IWebBrowser browserControl, IBrowser browser)
        {
        }
    }
}
