using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using CefSharp;

using HappyHour.Spider;

namespace HappyHour.ScrapItems
{
    class ItemMgstage : AvItemBase, IScrapItem
    {
        int _numDownloadCnt = 0;
        public ItemMgstage(SpiderBase spider) : base(spider)
        {
        }

        //protected override void UdpateAvItem() { }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        { 
            var ext = Path.GetExtension(e.SuggestedFileName);
            if (_numDownloadCnt == 0)
                e.SuggestedFileName = $"{PosterPath}{ext}";
            else
                e.SuggestedFileName = $"{_spider.Media.MediaFolder}\\" +
                    $"{_spider.Media.Pid}_screenshot{_numDownloadCnt}{ext}";
            Interlocked.Increment(ref _numDownloadCnt);
        }

        void ParseCover(string url)
        {
            var ext = url.Split('.').Last();
            if (File.Exists($"{PosterPath}.{ext}"))
                return;

            if (!url.StartsWith("http"))
            {
                _spider.Browser.Download(_spider.URL + url);
            }
            else
            {
                _spider.Browser.Download(url);
            }
            Interlocked.Increment(ref _numItemsToScrap);
        }

        void IScrapItem.OnJsResult(string name, List<object> items)
        {
            PrintItem(name, items);
            if (items != null && items.Count > 0)
            {
                _numValidItems++;
                if (name == "cover")
                {
                    ParseCover(items[0] as string);
                }
                else if (name == "studio")
                {
                    var m = Regex.Match(items[0] as string, @"\]=([\w\d]+)");
                    if (m.Success)
                    {
                        //Log.Print($"\tstudio:{m.Groups[1].Value}");
                        UpdateStudio(m.Groups[1].Value);
                    }
                }
                else if (name == "title")
                {
                    UpdateTitle(items[0] as string);
                }
                else if (name == "releasedate")
                {
                    var strdate = (items[0] as string).Trim(); ;
                    try
                    {
                        _avItem.DateReleased = DateTime.ParseExact(
                            strdate, "yyyy/MM/dd", enUS);
                    }
                    catch (Exception e)
                    {
                        Log.Print(e.Message);
                    }
                }
            }
            CheckCompleted();
        }
    }
}
