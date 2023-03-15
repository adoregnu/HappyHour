using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvalonDock.Properties;
using CefSharp;
using CefSharp.Handler;
using HappyHour.Extension;
using HappyHour.Spider;

namespace HappyHour.CefHandler
{
    class PopupResourceRequestHandler : ResourceRequestHandler
    {
         public string RefAddress { set; get; }
        protected override CefReturnValue OnBeforeResourceLoad(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            IRequestCallback callback)
        {
            request.SetReferrer(RefAddress, ReferrerPolicy.Default);
            return CefReturnValue.Continue;
        }
    }

    class PopupRequestHandler : RequestHandler
    {
        public string RefAddress
        {
            set
            {
                _resRequestHandler.RefAddress = value;
            }
        }
        PopupResourceRequestHandler _resRequestHandler = new();

        protected override IResourceRequestHandler GetResourceRequestHandler(
                IWebBrowser chromiumWebBrowser,
                IBrowser browser,
                IFrame frame,
                IRequest request,
                bool isNavigation,
                bool isDownload,
                string requestInitiator,
                ref bool disableDefaultHandling)
        {
            return _resRequestHandler;// new PopupResourceRequestHandler(_refAddress);
        }
    }
}
