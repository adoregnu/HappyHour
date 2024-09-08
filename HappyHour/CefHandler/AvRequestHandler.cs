using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation.Peers;
using AvalonDock.Properties;
using CefSharp;
using CefSharp.Handler;
using HappyHour.Extension;
using HappyHour.Spider;
using HappyHour.ViewModel;

namespace HappyHour.CefHandler
{

    class AvImageFilter : IResponseFilter
    {
        private MemoryStream memoryStream;
        readonly SpiderBase _spider;
        string _url;
        public AvImageFilter(string url, SpiderBase spider)
        {
            _url = url;
            _spider = spider;
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
        public static string GetResourcePath(string url)
        {
            var urlcomp = url.Split('/').Skip(2).Take(url.Length - 2);
            var name = string.Join("_", urlcomp);
            return $"{App.Current.LocalAppData}\\covers\\{name}";
        }
        public void Dispose()
        {
            string imgPath = GetResourcePath(_url);
            if (!File.Exists(imgPath))
            {
                byte[] bytes = memoryStream.ToArray();
                File.WriteAllBytes(imgPath, bytes);
            }
            memoryStream.Dispose();
            memoryStream = null;
            _spider.UpdateDownload(imgPath);
        }
    }
    class AvResourceRequestHandler : ResourceRequestHandler
    {
        readonly SpiderViewModel _spider;
        public AvResourceRequestHandler(SpiderViewModel spider)
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
            //var res = _spider.ResourcesToBeFiltered;
            var patterns = _spider.SelectedSpider.UrlPatternsToFilter;
            if (_spider.SelectedSpider.IsSpiderWorking)
            {
                foreach (var pattern in patterns)
                {
                    if (request.Url.Contains(pattern))
                    {
                        return new AvImageFilter(request.Url, _spider.SelectedSpider);
                    }
                }
            }

            return null;
        }
    }
    class AvRequestHandler : RequestHandler
    {
        private readonly AvResourceRequestHandler _resourcehandler;
        private readonly SpiderViewModel _spider;
        public AvRequestHandler(SpiderViewModel spider)
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
