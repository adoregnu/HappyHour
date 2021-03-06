﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CefSharp;

using HtmlAgilityPack;

using HappyHour.Extension;
using HappyHour.Spider;
using HappyHour.Model;

namespace HappyHour.ScrapItems
{
    class ItemJavmovie : AvItemBase
    {
        public ItemJavmovie(SpiderBase spider) : base(spider)
        {
            Elements = new List<(string name, string element, ElementType type)>
            {
                ( "cover", "//div[@class='movie-cover']/img/@src", ElementType.XPATH),
                ( "title", "//div[@class='mdm-info']/h1/text()", ElementType.XPATH),
                ( "date", "//div[@class='mdm-info']//tr[2]/td[2]/text()", ElementType.XPATH),
                ( "studio", "//div[@class='mdm-info']//tr[5]/td[2]//text()", ElementType.XPATH),
                ( "actor", "//td[@class='list-actress']/a/text()", ElementType.XPATH),
                ( "genre", "//td[@class='list-genre']/a/text()", ElementType.XPATH),
            };
        }

        //protected override void UdpateAvItem() { }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            var ext = Path.GetExtension(e.SuggestedFileName);
            e.SuggestedFileName = $"{PosterPath}{ext}";
        }

        void ParseActorName(List<object> items)
        {
            List<List<AvActorName>> ll = new List<List<AvActorName>>();
            foreach (string actor in items)
            {
                var name = actor.Trim();
                var list = new List<AvActorName>();
                var m = Regex.Match(name, @"([\w\s]+) \((.+)\)");
                if (m.Success)
                {
                    name = m.Groups[1].Value.Trim();
                    var regex = new Regex(@"([\w\s]+),?");
                    foreach (Match mm in regex.Matches(m.Groups[2].Value))
                    {
                        list.Add(new AvActorName { Name = mm.Groups[1].Value.Trim() });
                    }
                }
                list.Insert(0, new AvActorName { Name = name });
                ll.Add(list);
            }
            UpdateActor2(ll);
        }

        public override void OnJsResult(string name, List<object> items)
        {
            PrintItem(name, items);
            if (!items.IsNullOrEmpty())
            {
                Interlocked.Increment(ref _numValidItems);
                if (name == "cover")
                {
                    var url = (items[0] as string).Trim();
                    var ext = url.Split('.').Last();
                    if (!File.Exists($"{PosterPath}.{ext}"))
                    {
                        _spider.Download(url, ref _numItemsToScrap);
                    }
                }
                else if (name == "date")
                {
                    var m = Regex.Match(items[0] as string, @"[\d\-]+");
                    if (m.Success)
                    {
                        _avItem.DateReleased = DateTime.ParseExact(
                            m.Value, "yyyy-MM-dd", enUS);
                    }
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
                    ParseActorName(items);
                }
                else if (name == "title")
                {
                    UpdateTitle(items[0] as string);
                }
            }

            CheckCompleted();
        }
    }
}
