using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using HappyHour.Interfaces;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    internal class ShtDownloader : IDownloader
    {
        private readonly SpiderViewModel _browser;
        public ShtDownloader(SpiderViewModel browser)
        {
            _browser = browser;
        }
        public void Enable(bool bEnable)
        {
            if (bEnable)
            {
                _browser.DownloadHandler.OnBeforeDownloadFired += OnBeforeDownload;
                _browser.DownloadHandler.OnDownloadUpdatedFired += OnDownloadUpdated;
            }
            else
            {
                _browser.DownloadHandler.OnBeforeDownloadFired -= OnBeforeDownload;
                _browser.DownloadHandler.OnDownloadUpdatedFired -= OnDownloadUpdated;
            }
        }

        private void OnBeforeDownload(object sender, DownloadItem e)
        { 
        }
        private void OnDownloadUpdated(object sender, DownloadItem e)
        { 
        }

        public void Download(SpiderBase spider, IDictionary<string, object> items)
        { 
        }
    }
}
