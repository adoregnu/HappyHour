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
        public void Dispose()
        {
            byte[] bytes = memoryStream.ToArray();
            byte[] search = Encoding.ASCII.GetBytes("WEBP");
            if (bytes.IndexOf(search, 20) > 0)
            {
                _targetPath = string.Concat(_targetPath.AsSpan(0, _targetPath.LastIndexOf('.')), ".webp");
                Log.Print($"WEBP : {_targetPath}");
            }
            else
            {
                Log.Print($"JPEG : {_targetPath}");
            }
            File.WriteAllBytes(_targetPath, bytes);
            memoryStream.Dispose();
            memoryStream = null;
        }
    }
    class AvResourceRequestHandler : ResourceRequestHandler
    {
        readonly SpiderBase _spider;
         public AvResourceRequestHandler(SpiderBase spider)
        {
            _spider = spider;
        }

        protected override IResponseFilter GetResourceResponseFilter(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            IResponse response)
        {
            var res = _spider.ResourcesToBeFiltered;
            if (res != null  && res.TryGetValue(request.Url, out string value))
            {
                return new ImageFilter(value);
            }
            return null;
        }
        protected override CefReturnValue OnBeforeResourceLoad(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            IRequestCallback callback)
        {
            request.SetReferrer(_spider.Browser.Address, ReferrerPolicy.Default);
            return CefReturnValue.Continue;
        }
    }

    class AvRequestHandler : RequestHandler
    {

        readonly SpiderBase _spider;
        public AvRequestHandler(SpiderBase spider)
        {
            _spider = spider;
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
            return new AvResourceRequestHandler(_spider);
        }
    }
}
