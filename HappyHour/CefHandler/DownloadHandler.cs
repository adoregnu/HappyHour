﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CefSharp;

namespace HappyHour.CefHandler
{
    class DownloadHandler : IDownloadHandler
    {
        public event EventHandler<DownloadItem> OnBeforeDownloadFired;
        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

        public bool ShowDialog { get; set; } = false;

        //string _basePath;
        public DownloadHandler()
        {
            //_basePath = Directory.GetCurrentDirectory() + "\\";
        }

        public void OnBeforeDownload(IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            DownloadItem downloadItem,
            IBeforeDownloadCallback callback)
        {
            OnBeforeDownloadFired?.Invoke(this, downloadItem);

            if (callback.IsDisposed)
                return;

            using (callback)
            {
                //if (!downloadItem.IsCancelled)
                {
                    //Log.Print("Before Download: " + downloadItem.SuggestedFileName);
                    callback.Continue(downloadItem.SuggestedFileName, showDialog: ShowDialog);
                }
            }
        }

        public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser,
            IBrowser browser,
            DownloadItem downloadItem,
            IDownloadItemCallback callback)
        {
            if (downloadItem.IsCancelled)
            {
                Log.Print($"Cancel downalod {downloadItem.SuggestedFileName}");
                callback.Cancel();
            }
            if (downloadItem.IsComplete)
            {
                //Log.Print("Download completed! " + downloadItem.OriginalUrl);
                if (browser.IsPopup)
                {
                    browser.GetHost().CloseBrowser(false);
                    browser.GetHost().Dispose();
                }
            }
            OnDownloadUpdatedFired?.Invoke(this, downloadItem);
        }
    }
}
