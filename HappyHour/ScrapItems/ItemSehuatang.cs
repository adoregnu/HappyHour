﻿using System;
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
    class ItemSehuatang : ItemBase, IScrapItem
    {
        public DateTime DateTime;

        bool _skipDownload = false;
        string _pid = null;
        string _outPath = null;
        Dictionary<string, int> _images = null;
        ManualResetEvent _eventPidParsed = new ManualResetEvent(false);

        public ItemSehuatang(SpiderBase spider) : base(spider)
        {
        }

        void CheckCompleted()
        {
            lock (_spider)
            {
                Interlocked.Increment(ref _numScrapedItem);
                Log.Print($"{_numScrapedItem}/{NumItemsToScrap}");
                if (_numScrapedItem == NumItemsToScrap)
                {
                    _spider.OnScrapCompleted(_outPath);
                    Clear();
                }
            }
        }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            if (e.SuggestedFileName.EndsWith("torrent"))
            {
                e.SuggestedFileName = _outPath + e.SuggestedFileName;
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
                e.SuggestedFileName = _outPath + $"{_pid}_{postfix}{ext}";
            }
            Log.Print($"{_pid} file to store: {e.SuggestedFileName}");
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
            _outPath += _spider.GetConf("DataPath") + _pid + "\\";

            var di = new DirectoryInfo(_outPath);
            if (!di.Exists)
            {
                Directory.CreateDirectory(_outPath);
            }
            else
            {
                Log.Print($"Already downloaded! {_outPath}");
                if (_spider.Browser.StopOnExistingId)
                    _spider.EnableScrapIntoDb = false;
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
                Interlocked.Increment(ref _numItemsToScrap);
                if (!f.StartsWith("http"))
                {
                    _spider.Browser.Download(_spider.URL + f);
                }
                else
                {
                    _spider.Browser.Download(f);
                }
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
        void IScrapItem.OnJsResult(string name, List<object> items)
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
