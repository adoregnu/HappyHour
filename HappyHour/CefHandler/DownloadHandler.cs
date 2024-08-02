using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CefSharp;
using HappyHour.Interfaces;

namespace HappyHour.CefHandler
{
    class DownloadHandler : IDownloadHandler
    {
        public event EventHandler<DownloadItem> OnBeforeDownloadFired;
        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

        public bool ShowDialog { get; set; } = false;

        private readonly ISpider _spider = null;
        public DownloadHandler(ISpider spider)
        {
            _spider = spider;
        }

        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return true;
        }

        public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            DownloadItem downloadItem,
            IBeforeDownloadCallback callback)
        {
            OnBeforeDownloadFired?.Invoke(_spider, downloadItem);

            if (callback.IsDisposed)
                return true;

            using (callback)
            {
                if (!downloadItem.IsCancelled)
                {
                    //Log.Print("Before Download: " + downloadItem.SuggestedFileName);
                    callback.Continue(downloadItem.SuggestedFileName, showDialog: ShowDialog);
                }
            }
            return true;
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            DownloadItem downloadItem,
            IDownloadItemCallback callback)
        {
            if (downloadItem.IsComplete)
            {
                //Log.Print("Download completed! " + downloadItem.OriginalUrl);
                if (browser.IsPopup)
                {
                    browser.GetHost().CloseBrowser(false);
                    browser.GetHost().Dispose();
                }
            }
            OnDownloadUpdatedFired?.Invoke(_spider, downloadItem);
            if (downloadItem.IsCancelled)
            {
                Log.Print($"Cancel downalod {downloadItem.SuggestedFileName}");
                callback.Cancel();
            }
        }
    }
}
