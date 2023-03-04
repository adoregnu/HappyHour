using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Handler;
using HappyHour.Spider;
using HappyHour.ViewModel;

namespace HappyHour.CefHandler
{

    class ImageFilter : IResponseFilter
    {
        private MemoryStream memoryStream;

        string _targetPath;
        public ImageFilter(string targetPath)
        {
            _targetPath = targetPath;
        }
        bool IResponseFilter.InitFilter()
        {
            memoryStream = new MemoryStream();
            return true;
        }
        FilterStatus IResponseFilter.Filter(Stream dataIn, out long dataInRead, Stream dataOut, out long dataOutWritten)
        {
            if (dataIn == null)
            {
                dataInRead = 0;
                dataOutWritten = 0;

                return FilterStatus.Done;
            }

            //Calculate how much data we can read, in some instances dataIn.Length is
            //greater than dataOut.Length
            dataInRead = Math.Min(dataIn.Length, dataOut.Length);
            dataOutWritten = dataInRead;

            var readBytes = new byte[dataInRead];
            dataIn.Read(readBytes, 0, readBytes.Length);
            dataOut.Write(readBytes, 0, readBytes.Length);

            dataIn.Position = 0;
            dataIn.CopyTo(memoryStream);

            //If we read less than the total amount avaliable then we need
            //return FilterStatus.NeedMoreData so we can then write the rest
            if (dataInRead < dataIn.Length)
            {
                return FilterStatus.NeedMoreData;
            }

            return FilterStatus.Done;
        }
        static int Search(byte[] src, byte[] pattern, int bondary)
        {
            for (int i = 0; i < bondary; i++)
            {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }
        public void Dispose()
        {
            byte[] bytes = memoryStream.ToArray();
            byte[] search = Encoding.ASCII.GetBytes("WEBP");
            if (Search(bytes, search, 20) > 0)
            {
                _targetPath = string.Concat(_targetPath.AsSpan(0, _targetPath.LastIndexOf('.')), ".webp");
                Log.Print($"WEBP : {_targetPath}");
            }
            else
            {
                Log.Print($"JPEG : {_targetPath}");
            }
            File.WriteAllBytes(_targetPath ,memoryStream.ToArray());
            memoryStream.Dispose();
            memoryStream = null;
        }
    }
    class AvResourceRequestHandler : ResourceRequestHandler
    {
        readonly SpiderViewModel _spiderVm;
        public AvResourceRequestHandler(SpiderViewModel spider)
        {
            _spiderVm = spider;
        } 

        bool _closeBrowser = false;
        protected override IResponseFilter GetResourceResponseFilter(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            IResponse response)
        {
            var res = _spiderVm.SelectedSpider.ResourcesToBeFiltered;
            if (res == null)
            {
                return null;
            }
            if (res.ContainsKey(request.Url))
            {
                _closeBrowser = true;
                return new ImageFilter(res[request.Url]);
            }
            else
            {
                _closeBrowser = false;
            }
            return null;
        }

        protected override void OnResourceLoadComplete(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            IResponse response,
            UrlRequestStatus status,
            long receivedContentLength)
        {

            if (_closeBrowser)
            {
                browser.CloseBrowser(true);
            }
        }
    }

    class AvRequestHandler : RequestHandler
    {

        readonly SpiderViewModel _spiderVm;
        public AvRequestHandler(SpiderViewModel spider)
        {
            _spiderVm = spider;
        }
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
            return new AvResourceRequestHandler(_spiderVm);
#endif
        }
    }
}
