using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Handler;

namespace HappyHour.CefHandler
{
    public class AvResourceRequestHandler : ResourceRequestHandler
    {
        protected override CefReturnValue OnBeforeResourceLoad(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            IRequestCallback callback)
        {
            var headers = request.Headers;
            foreach (string key in headers.Keys)
            {
                Log.Print($"{key} : {headers[key]}");
            }

            return base.OnBeforeResourceLoad(
                chromiumWebBrowser,
                browser,
                frame,
                request,
                callback);
        }
    }

    class AvRequestHandler : RequestHandler
    {
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
#if false
            return base.GetResourceRequestHandler(
                chromiumWebBrowser,
                browser,
                frame,
                request,
                isNavigation,
                isDownload,
                requestInitiator,
                ref disableDefaultHandling);
#else
            return new AvResourceRequestHandler();
#endif
        }
    }
}
