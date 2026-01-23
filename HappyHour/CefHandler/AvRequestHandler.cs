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

    class AvImageFilter(string url, SpiderBase spider) : IResponseFilter
    {
        private MemoryStream memoryStream;

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
            dataIn.ReadExactly(readBytes);
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
            string imgPath = GetResourcePath(url);
            if (!File.Exists(imgPath))
            {
                byte[] bytes = memoryStream.ToArray();
                File.WriteAllBytes(imgPath, bytes);
            }
            memoryStream.Dispose();
            memoryStream = null;
            spider.UpdateDownload(imgPath);
            //Log.Print($"[Resource Filter] Downloaded resource: {url} to {imgPath}");
        }
    }
    class AvResourceRequestHandler(SpiderViewModel spider) : ResourceRequestHandler
    {
        protected override IResponseFilter GetResourceResponseFilter(
            IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            IFrame frame,
            IRequest request,
            IResponse response)
        {
            //var res = _spider.ResourcesToBeFiltered;
            var patterns = spider.SelectedSpider.UrlPatternsToFilter;
            //if (spider.SelectedSpider.IsSpiderWorking)
            {
                foreach (var pattern in patterns)
                {
                    if (request.Url.Contains(pattern))
                    {
                        return new AvImageFilter(request.Url, spider.SelectedSpider);
                    }
                }
            }

            return null;
        }
    }
    class AvRequestHandler(SpiderViewModel spider) : RequestHandler
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
            return new AvResourceRequestHandler(spider);
        }
    }
}
