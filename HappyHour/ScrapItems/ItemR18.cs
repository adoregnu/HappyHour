using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CefSharp;

using HappyHour.Model;
using HappyHour.Spider;
using HappyHour.Extension;

namespace HappyHour.ScrapItems
{
    class ItemR18 : AvItemBase
    {
        readonly string _actorPicturePath = $"{App.CurrentPath}\\db";
        readonly ConcurrentDictionary<string, string> _downloadUrls;
        readonly Dictionary<string, string> _actorPicturs;
        readonly List<AvActorName> _actorNames;

        public ItemR18(SpiderBase spider) : base(spider)
        { 
            _downloadUrls = new ConcurrentDictionary<string, string>();
            _actorPicturs = new Dictionary<string, string>();
            _actorNames = new List<AvActorName>();
            Elements = new List<(string name, string element, ElementType type)>
            {
                ( "title",    "//meta[@property='og:title']/@content", ElementType.XPATH),
                ( "releasedate", "//dt[contains(.,'Release Date:')]/following-sibling::dd[1]/text()", ElementType.XPATH),
                ( "runtime",  "//dt[contains(.,'Runtime:')]/following-sibling::dd[1]/text()", ElementType.XPATH),
                ( "director", "//dt[contains(.,'Director:')]/following-sibling::dd[1]/text()", ElementType.XPATH),
                ( "set_url",  "//dt[contains(.,'Series:')]/following-sibling::dd[1]/a/@href", ElementType.XPATH),
                ( "studio",   "//dt[contains(.,'Studio:')]/following-sibling::dd[1]/a/text()", ElementType.XPATH),
                ( "label",    "//dt[contains(.,'Label:')]/following-sibling::dd[1]/text()", ElementType.XPATH),
                ( "actor",    "//label[contains(.,'Actress(es):')]/following-sibling::div[1]/span/a/span/text()", ElementType.XPATH),
                ( "genre",    "//label[contains(.,'Categories:')]/following-sibling::div[1]/a/text()", ElementType.XPATH),
                ( "plot",     "//h1[contains(., 'Product Description')]/following-sibling::p/text()", ElementType.XPATH),
                ( "cover",    "//div[contains(@class,'box01')]/img/@src", ElementType.XPATH),
                ( "actor_thumb", "//ul[contains(@class,'cmn-list-product03')]//img", ElementType.XPATH),
            };
        }

        protected override void UdpateAvItem()
        {
            if (_context == null) return;
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
            if (_downloadUrls[e.OriginalUrl] == "cover")
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

        void ParseDate(string strDate)
        {
            if (string.IsNullOrEmpty(strDate)) return;
            try
            {
                var m = Regex.Match(strDate, @"([\w\.]+) (\d+), (\d+)");
                if (!m.Success) return;
                var newdate = string.Format("{0} {1} {2}",
                    m.Groups[1].Value.Substring(0, 3),
                    m.Groups[2].Value,
                    m.Groups[3].Value);

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
        void ParseActorName(List<object> items)
        {
            List<List<AvActorName>> ll = new List<List<AvActorName>>();
            foreach (string item in items)
            {
                var name = item.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                var list = new List<AvActorName>();
                var m = Regex.Match(name, @"([\w\s]+)(?:\n|\s)\((.+)\)");
                if (m.Success)
                {
                    name = m.Groups[1].Value.Trim();
                    var regex = new Regex(@"([\w\s]+),?");
                    foreach (Match mm in regex.Matches(m.Groups[2].Value))
                        list.Add(new AvActorName
                        {
                            Name = mm.Groups[1].Value.Trim()
                        });
                }
                list.Insert(0, new AvActorName { Name = name });
                ll.Add(list);
                _actorNames.AddRange(list);
            }
            UpdateActor2(ll);
        }

        void ParseActorThumb(List<object> items)
        {
            Regex regex = new Regex(@"alt=""([\w\s]+)"" src=""(.+)"" width");
            foreach (string img in items)
            {
                var m = regex.Match(img);
                if (!m.Success)
                    continue;

                var url = m.Groups[2].Value;
                var file = $"{_actorPicturePath}\\{url.Split('/').Last()}";
                if (!OverwriteImage && File.Exists(file))
                    continue;

                var name = m.Groups[1].Value.Trim();
                if (!url.EndsWith("nowprinting.gif"))
                {
                    if (_downloadUrls.TryAdd(url, name))
                    {
                        _spider.Download(url, ref _numItemsToScrap);
                    }
                }
            }
        }

        void ParseCover(string name, string url)
        {
            var ext = url.Split('.').Last();
            if (!OverwriteImage && File.Exists($"{PosterPath}.{ext}"))
                return;

            if (_downloadUrls.TryAdd(url, name))
            {
                _spider.Download(url, ref _numItemsToScrap);
            }
        }

        public override void OnJsResult(string name, List<object> items)
        {
            PrintItem(name, items);
            if (!items.IsNullOrEmpty())
            {
                _numValidItems++;
                if (name == "cover")
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
                else if (name == "set_url")
                {
                    var url = (items[0] as string).Trim();
                    if (!string.IsNullOrEmpty(url))
                        _links.Add(new Tuple<string, string>("series", url));
                }
                else if (name == "series")
                {
                    UpdateSeries((items[0] as string).Trim());
                }
                else if (name == "plot")
                {
                    if (items[0] is string plot && string.IsNullOrEmpty(plot))
                        _avItem.Plot = plot.Trim();
                }
            }

            CheckCompleted();
        }
    }
}
