using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

using CefSharp;

using HappyHour.Model;
using HappyHour.Spider;
using HappyHour.Extension;

namespace HappyHour.ScrapItems
{
    class ItemR18V2 : AvItemBase
    {
        private readonly string _actorPicturePath = $"{App.LocalAppData}\\db";
        private readonly Dictionary<string, string> _downloadUrls;
        private readonly Dictionary<string, string> _actorPicturs;
        private readonly List<AvActorName> _actorNames;

        public ItemR18V2(SpiderBase spider) : base(spider)
        {
            _downloadUrls = new Dictionary<string, string>();
            _actorPicturs = new Dictionary<string, string>();
            _actorNames = new List<AvActorName>();
            Elements = new List<(string name, string element, ElementType type)>
            {
                ( "title",    "//meta[@property='og:title']/@content", ElementType.XPATH),
                ( "releasedate", "//h3[contains(.,'Release date')]/following-sibling::div//text()", ElementType.XPATH),
                ( "runtime",  "//h3[contains(.,'Runtime')]/following-sibling::div/text()", ElementType.XPATH),
                ( "director", "//h3[contains(.,'Director')]/following-sibling::div/text()", ElementType.XPATH),
                ( "series",  "//h3[contains(.,'Series')]/following-sibling::div//a/text()", ElementType.XPATH),
                ( "studio",   "//h3[contains(.,'Studio')]/following-sibling::div/a/text()", ElementType.XPATH),
                //( "label",    "//dt[contains(.,'Label:')]/following-sibling::dd[1]/text()", ElementType.XPATH),
                ( "actor",    "//h3[contains(.,'Actresses')]/following-sibling::div/span//text()", ElementType.XPATH),
                ( "genre",    "//h3[contains(.,'Categories')]/following-sibling::div/span//text()", ElementType.XPATH),
                ( "plot",     "//h3[contains(.,'synosis')]/following-sibling::p/text()", ElementType.XPATH),
                ( "cover",    "//div[@class='sc-cTJmaU BaBOz']/img/@src", ElementType.XPATH),
                ( "cover2",    "//div[@class='sc-cTJmaU bAAnmR']/img/@src", ElementType.XPATH),
                ( "actor_thumb", "//div[contains(@class,'actress-switching')]//img", ElementType.XPATH),
            };
        }

        protected override void UdpateAvItem()
        {
            if (_context == null)
            {
                return;
            }
            foreach (var pic in _actorPicturs)
            {
                var actorName = _actorNames.FirstOrDefault(x => x.Name == pic.Key);
                if (actorName != null)
                {
                    //Log.Print($"UpdateAvItem:: name: {pic.Key}, path:{pic.Value} ");
                    actorName.Actor.PicturePath = pic.Value;
                }
            }
            base.UdpateAvItem();
        }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            var ext = Path.GetExtension(e.SuggestedFileName);
            if (!_downloadUrls.ContainsKey(e.OriginalUrl))
            {
                Log.Print($"{e.OriginalUrl} not found in download list!");
                foreach (var url in _downloadUrls)
                {
                    Log.Print($"urls : {url}");
                }
                return;
            }
            if (_downloadUrls[e.OriginalUrl] is "cover" or "cover2")
            {
                e.SuggestedFileName = $"{PosterPath}{ext}";
            }
            else
            {
                //_downloadUrls[e.OriginalUrl] contains actor name
                _actorPicturs.Add(_downloadUrls[e.OriginalUrl], e.SuggestedFileName);
                e.SuggestedFileName = $"{_actorPicturePath}\\{e.SuggestedFileName}";
            }
            Log.Print("OnBeforeDownload: " + e.SuggestedFileName);
        }

        private void ParseDate(string strDate)
        {
            if (string.IsNullOrEmpty(strDate))
            {
                return;
            }
            try
            {
                Match m = Regex.Match(strDate, @"([\w\.]+) (\d+), (\d+)");
                if (!m.Success)
                {
                    return;
                }
                string newdate = $"{m.Groups[1].Value.Substring(0, 3)} {m.Groups[2].Value} {m.Groups[3].Value}";
                _avItem.DateReleased = DateTime.ParseExact(newdate, "MMM d yyyy", enUS);
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        /// <summary>
        /// input string example :
        ///   "Tsubasa Hachino"  or
        ///   "Elly Akira (Elly Arai, Yuka Osawa)"
        /// </summary>
        /// <param name="items"></param>
        private void ParseActorName(List<object> items)
        {
            List<List<AvActorName>> ll = new List<List<AvActorName>>();
            foreach (string item in items)
            {
                string name = item.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                List<AvActorName> list = new();
                Match m = Regex.Match(name, @"([\w\s]+)(?:\n|\s)\((.+)\)");
                if (m.Success)
                {
                    name = m.Groups[1].Value.Trim();
                    Regex regex = new(@"([\w\s]+),?");
                    foreach (Match mm in regex.Matches(m.Groups[2].Value))
                    {
                        list.Add(new AvActorName
                        {
                            Name = mm.Groups[1].Value.Trim()
                        });
                    }
                }
                list.Insert(0, new AvActorName { Name = name });
                ll.Add(list);
                _actorNames.AddRange(list);
            }
            UpdateActor2(ll);
        }

        private void ParseActorThumb(List<object> items)
        {
            Regex regex = new(@"src=""(.+)"" alt=""([\w\s()]+)""");
            foreach (string img in items)
            {
                Match m = regex.Match(img);
                if (!m.Success)
                {
                    continue;
                }

                string url = m.Groups[1].Value;
                string file = $"{_actorPicturePath}\\{url.Split('/').Last()}";
                if (!OverwriteImage && File.Exists(file))
                {
                    continue;
                }

                //Ex) Kaoru Natsuki (Tsubaki Kato)
                string name = m.Groups[2].Value.Split('(')[0].Trim();
                if (!url.EndsWith("nowprinting.gif", StringComparison.OrdinalIgnoreCase))
                {
                    lock (_downloadUrls)
                    {
                        _downloadUrls.Add(url, name);
                        _spider.Download(url, ref _numItemsToScrap);
                    }
                }
            }
        }

        private void ParseCover(string name, string url)
        {
            string ext = url.Split('.').Last();
            if (!OverwriteImage && File.Exists($"{PosterPath}.{ext}"))
            {
                return;
            }

            lock (_downloadUrls)
            {
                _downloadUrls.Add(url, name);
                _spider.Download(url, ref _numItemsToScrap);
            }
        }

        public override void OnJsResult(string name, List<object> items)
        {
            PrintItem(name, items);
            if (!items.IsNullOrEmpty())
            {
                _numValidItems++;
                if (name is "cover" or "cover2")
                {
                    var url = items[0] as string;
                    ParseCover(name, url.Trim());
                }
                else if (name == "actor_thumb")
                {
                    ParseActorThumb(items);
                }
                else if (name == "genre")
                {
                    UpdateGenre(items);
                }
                else if (name == "studio")
                {
                    UpdateStudio(items[0] as string);
                }
                else if (name == "actor")
                {
                    ParseActorName(items);
                }
                else if (name == "releasedate")
                {
                    ParseDate((items[0] as string).Trim());
                }
                else if (name == "title")
                {
                    UpdateTitle(items[0] as string);
                }
                else if (name == "series")
                {
                    UpdateSeries((items[0] as string).Trim());
                }
                else if (name == "plot")
                {
                    if (items[0] is string plot && string.IsNullOrEmpty(plot))
                    {
                        _avItem.Plot = plot.Trim();
                    }
                }
            }

            CheckCompleted();
        }
    }
}
