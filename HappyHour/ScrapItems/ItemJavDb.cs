
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.Spider;

namespace HappyHour.ScrapItems
{
    class ItemJavDb : AvItemBase, IScrapItem
    {
        public ItemJavDb(SpiderBase spider) : base(spider)
        {
            _avItem.IsCensored = false;
        }

        //protected override void UdpateAvItem() { }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            var ext = Path.GetExtension(e.SuggestedFileName);
            e.SuggestedFileName = $"{PosterPath}{ext}";
        }

        void ParseActor(List<object> items)
        {
            List<List<AvActorName>> ll = new List<List<AvActorName>>();
            foreach (string name in items)
            {
                ll.Add(new List<AvActorName> {
                    new AvActorName { Name = name }
                });
            }
            UpdateActor2(ll);
        }

        void IScrapItem.OnJsResult(string name, List<object> items)
        {
            PrintItem(name, items);

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
                    UpdateTitle(items[0] as string);
                }
                else if (name == "studio")
                {
                    UpdateStudio(items[0] as string);
                }
                else if (name == "genre")
                {
                    UpdateGenre(items);
                }
                else if (name == "actor")
                {
                    ParseActor(items);
                }
                else if (name == "date")
                {
                    var strdate = (items[0] as string).Trim();
                    _avItem.DateReleased = DateTime.ParseExact(
                        strdate, "yyyy-MM-dd", enUS);
                }
            }

            CheckCompleted();
        }
    }
}
