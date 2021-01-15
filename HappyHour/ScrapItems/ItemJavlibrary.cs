using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.Spider;
using HappyHour.Extension;
using HappyHour.Model;

namespace HappyHour.ScrapItems
{
    class ItemJavlibrary : AvItemBase, IScrapItem
    {
        public ItemJavlibrary(SpiderBase spider) :base(spider)
        {
        }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            if (e.TotalBytes > 1024*10)
                //!e.SuggestedFileName.Contains("now_printing"))
                //!e.SuggestedFileName.Contains("removed"))
            {
                var ext = Path.GetExtension(e.SuggestedFileName);
                e.SuggestedFileName = $"{PosterPath}{ext}";
            }
            else
            {
                e.SuggestedFileName = "";
            }
        }
        void ParseActor(List<object> items)
        {
            List<List<AvActorName>> ll = new List<List<AvActorName>>();
            HtmlDocument doc = new HtmlDocument();
            foreach (string cast in items)
            {
                var l = new List<AvActorName>();
                doc.LoadHtml(cast);
                foreach (var span in doc.DocumentNode.SelectNodes("//span/span"))
                {
                    if (span.Attributes["class"].Value.Contains("icn_")) continue;
                    var name = span.InnerText.Trim().Split(' ').Reverse();
                    var aname = NameMap.ActorName(string.Join(" ", name));
                    l.Add(new AvActorName { Name = aname.Trim()});
                }
                if (l.Count > 0) ll.Add(l);
            }
            if (ll.Count > 0)
                UpdateActor2(ll);
        }

        void ParseCover(string url)
        {
            var ext = url.Split('.').Last();
            if (File.Exists($"{PosterPath}.{ext}")) return;

            _spider.Download(url, ref _numItemsToScrap);
        }

        void ParseDate(string date)
        {
            _avItem.DateReleased = DateTime.ParseExact(
                date.Trim(), "yyyy-MM-dd", enUS);
        }

        void IScrapItem.OnJsResult(string name, List<object> items)
        {
            PrintItem(name, items);
            if (!items.IsNullOrEmpty())
            {
                _numValidItems++;
                if (name == "actor")
                    ParseActor(items);
                else if (name == "cover")
                    ParseCover(items[0] as string);
                else if (name == "title")
                {
                    var title = (items[0] as string).Trim();
                    if (title.StartsWith(_spider.Keyword, StringComparison.OrdinalIgnoreCase))
                        title = title.Substring(_spider.Keyword.Length+1);

                    UpdateTitle(title);
                }
                else if (name == "date")
                    ParseDate(items[0] as string);
                else if (name == "studio")
                    UpdateStudio((items[0] as string).Trim());
                else if (name == "genre")
                    UpdateGenre(items);
            }
            CheckCompleted();
        }
    }
}
