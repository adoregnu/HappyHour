using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CefSharp;

using HappyHour.Spider;
using HappyHour.Extension;

namespace HappyHour.ScrapItems
{
    class ItemSehuatang : ItemBase
    {
        public DateTime DateTime;

        bool _skipDownload = false;
        string _pid = null;
        string _outPath = null;
        Dictionary<string, int> _images = null;
        readonly ManualResetEvent _eventPidParsed = new ManualResetEvent(false);

        public ItemSehuatang(SpiderBase spider) : base(spider)
        {
            Elements = new List<(string name, string element, ElementType type)>
            {
                ( "pid",  "//span[@id='thread_subject']/text()", ElementType.XPATH),
                ( "date", "(//em[contains(@id, 'authorposton')]/span/@title)[1]", ElementType.XPATH),
                ( "files", "//a[contains(., '.torrent')]", ElementType.XPATH_CLICK),
                ( "images", "(//td[contains(@id, 'postmessage_')])[1]//img[contains(@id, 'aimg_')]/@file", ElementType.XPATH) 
            };
        }

        void CheckCompleted()
        {
            lock (_spider)
            {
                Interlocked.Increment(ref _numScrapedItem);
                Log.Print($"{_numScrapedItem}/{NumItemsToScrap}");
                if (_numScrapedItem == NumItemsToScrap)
                {
                    _spider.DataPath = _outPath;
                    _spider.OnScrapCompleted();
                    Clear();
                }
            }
        }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            if (e.SuggestedFileName.EndsWith("torrent"))
            {
                e.SuggestedFileName = $"{_outPath}\\{e.SuggestedFileName}";
            }
            else
            {
                string postfix;
                var idx = _images[e.SuggestedFileName];
                var ext = Path.GetExtension(e.SuggestedFileName);
                if (idx == 0)
                    postfix = "cover";
                else
                    postfix = $"screenshot{idx}";
                e.SuggestedFileName = _outPath + $"\\{_pid}_{postfix}{ext}";
            }
            //Log.Print($"{_pid} file to store: {e.SuggestedFileName}");
        }

        protected override void OnDownloadUpdated(object sender, DownloadItem e)
        {
            if (e.IsComplete)
            {
                Log.Print($"{_pid} download completed: {e.FullPath}");
                try
                {
                    File.SetLastWriteTime(e.FullPath, DateTime);
                }
                catch (Exception ex)
                {
                    Log.Print(ex.Message);
                }

                CheckCompleted();
            }
        }

        void ParsePid(string title)
        {
            var m = Regex.Match(title, @"[\d\w\-_]+", RegexOptions.CultureInvariant);
            if (!m.Success)
            {
                Log.Print($"Could not find pid pattern in [] {title}");
                goto setEvent;
            }

            _pid = m.Groups[0].Value;
            _outPath += _spider.GetConf("DataPath") + _pid;

            var di = new DirectoryInfo(_outPath);
            if (!di.Exists)
            {
                Directory.CreateDirectory(_outPath);
            }
            else
            {
                Log.Print($"Already downloaded! {_outPath}");
                if (_spider.GetConf("StopOnExist") == "True")
                {
                    _spider.Stop();
                }
                _skipDownload = true;
            }
        setEvent:
            _eventPidParsed.Set();
        }

        void ParseImage(List<object> items)
        {
            _eventPidParsed.WaitOne();
            if (_skipDownload) return;

            _images = new Dictionary<string, int>();
            int i = 0;
            foreach (string f in items)
            {
                _images.Add(f.Split('/').Last(), i);
                string url = f;
                if (!f.StartsWith("http"))
                {
                    url = _spider.URL + f;
                }
                _spider.Download(url, ref _numItemsToScrap);
                i++;
            }
        }
#if false
        bool IsTorrentDownloaded()
        {
            var files = Directory.GetFiles(_outPath);
            if (files.Any(f => f.EndsWith(".torrent")))
                return true;
            else 
                return false;
        }
#endif
        public override void OnJsResult(string name, List<object> items)
        {
            PrintItem(name, items);

            if (!items.IsNullOrEmpty())
            {
                if (name == "pid")
                {
                    ParsePid(items[0] as string);
                }
                else if (name == "date")
                {
                    try
                    {
                        DateTime = DateTime.Parse(items[0] as string);
                    }
                    catch (Exception)
                    {
                        DateTime = DateTime.Now;
                    }
                }
                else if (name == "images")
                {
                    ParseImage(items);
                }
            }
            else if (name == "files")
            {
                _eventPidParsed.WaitOne();
                if (!_skipDownload/* || !IsTorrentDownloaded()*/) 
                    Interlocked.Increment(ref _numItemsToScrap);
            }
            CheckCompleted();
        }
    }
}
