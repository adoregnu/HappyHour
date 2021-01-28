
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CefSharp;
using HtmlAgilityPack;

using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.Spider;

namespace HappyHour.ScrapItems
{
    class ItemAVE : AvItemBase
    {
        public ItemAVE(SpiderBase spider) : base(spider)
        { 
            _avItem.IsCensored = false;
            Elements = new List<(string name, string element, ElementType type)>
            { 
                ( "cover", "//span[@class='grid-gallery']/a/@href", ElementType.XPATH),
                ( "title", "//div[@class='section-title']/h3/text()", ElementType.XPATH),
                ( "product-info", "//div[@class='single-info']", ElementType.XPATH),
            };
        }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            var ext = Path.GetExtension(e.SuggestedFileName);
            e.SuggestedFileName = $"{PosterPath}{ext}";
        }

        void ParseActorName(HtmlNodeCollection nodes)
        {
            List<List<AvActorName>> ll = new List<List<AvActorName>>();
            foreach (var node in nodes)
            {
                var l = new List<AvActorName>
                {
                    new AvActorName { Name = node.InnerText.Trim() }
                };
                ll.Add(l);
            }
            UpdateActor2(ll);
        }

        void ParseGenre(HtmlNodeCollection nodes)
        {
            List<object> genres = new List<object>();
            foreach (var node in nodes)
            {
                genres.Add(node.InnerText.Trim());
            }
            UpdateGenre(genres);
        }

        void ParseProductInfo(List<object> items)
        {
            HtmlDocument doc = new HtmlDocument();
            foreach (string item in items)
            {
                doc.LoadHtml(item);
                var tnode = doc.DocumentNode
                    .SelectSingleNode("//span[@class='title']");

                if (tnode.InnerText.Contains("Starring"))
                {
                    ParseActorName(doc.DocumentNode
                        .SelectNodes("//a"));
                }
                else if (tnode.InnerText.Contains("Studio"))
                {
                    var anode = doc.DocumentNode.SelectSingleNode("//a");
                    UpdateStudio(anode.InnerText.Trim());
                }
                else if (tnode.InnerText.Contains("Category"))
                {
                    ParseGenre(doc.DocumentNode.SelectNodes("//a"));
                }
                else if (tnode.InnerText.Contains("Date"))
                {
                    var node = doc.DocumentNode
                        .SelectSingleNode("//span[@class='value']");
                    var m = Regex.Match(node.InnerText, @"[\d/]+");
                    if (m.Success)
                    {
                        _avItem.DateReleased = DateTime.ParseExact(
                            m.Value, "M/d/yyyy", enUS);
                    }
                }
            }
        }

        public override void OnJsResult(string name, List<object> items)
        { 
            if (name != "product-info")
                PrintItem(name, items);

            if (!items.IsNullOrEmpty())
            {
                Interlocked.Increment(ref _numValidItems);
                if (name == "cover")
                {
                    var url = HtmlEntity.DeEntitize(items[0] as string);
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
                else if (name == "product-info")
                {
                    ParseProductInfo(items);
                }
            }

            CheckCompleted();
        }
    }
}
