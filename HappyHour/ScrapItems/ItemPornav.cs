using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CefSharp;

using HappyHour.Extension;
using HappyHour.Spider;

namespace HappyHour.ScrapItems
{
    class ItemPornav : AvItemBase, IScrapItem
    {
        public ItemPornav(SpiderBase spider) : base(spider)
        { 
        }

        protected override void UdpateAvItem()
        {
            //base.UdpateAvItem();
        }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            var ext = Path.GetExtension(e.SuggestedFileName);
            e.SuggestedFileName = $"{PosterPath}{ext}";
        }

        void ParseInfo(List<object> items)
        {
            bool actor = false, date = false;

            foreach (string line in items)
            {
                Match m = null;
                if (actor == false)
                {
                    m = Regex.Match(line, @"出演(?::)?(.+)");
                    if (m.Success)
                    {
                        //Log.Print(m.Groups[1].Value);
                        actor = true;
                        continue;
                    }
                }
                if (date == false)
                {
                    m = Regex.Match(line, @"配信日(?::)?(.+)");
                    if (m.Success)
                    {
                        //Log.Print(m.Groups[1].Value);
                        _avItem.DateReleased = DateTime.ParseExact(
                            m.Groups[1].Value.Trim(), "yyyy/MM/dd", enUS);
                        date = true;
                    }
                }
            }
        }

        void IScrapItem.OnJsResult(string name, List<object> items)
        {
            if (!items.IsNullOrEmpty())
            {
                Interlocked.Increment(ref _numValidItems);
                if (name == "cover")
                {
                    var url = items[0] as string;
                    var ext = url.Split('.').Last();
                    if (!File.Exists($"{PosterPath}.{ext}"))
                    {
                        _spider.Download(url, ref _numItemsToScrap);
                    }
                }
                else if (name == "title")
                {
                    var title = items[1] as string;
                    var start = title.LastIndexOf(_spider.Keyword)
                        + _spider.Keyword.Length;
                    title = title.Substring(start);
                    UpdateTitle(title);
                }
                else if (name == "info")
                {
                    ParseInfo(items);
                }
            }

            CheckCompleted();
        }
    }
}
