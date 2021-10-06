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
using HappyHour.Spider;
using HappyHour.Model;

namespace HappyHour.ScrapItems
{
    class ItemAvsox : AvItemBase
    {
        readonly List<AvActorName> _actorNames;
        readonly Dictionary<string, string> _actorPicturs;
        readonly string _actorPicturePath = $"{App.LocalAppData}\\db";

        public ItemAvsox(SpiderBase spider) : base(spider)
        {
            _avItem.IsCensored = false;
            _actorNames = new List<AvActorName>();
            _actorPicturs = new Dictionary<string, string>();

            Elements = new List<(string name, string element, ElementType type)>
            {
                ( "cover", "//a[@class='bigImage']/@href", ElementType.XPATH),
                ( "info", "//div[contains(@class, 'col-md-3')]", ElementType.XPATH),
                ( "title", "//div[@class='container']/h3/text()", ElementType.XPATH),
                ( "actor", "//a[@class='avatar-box']", ElementType.XPATH)
            };
        }

        //protected override void UdpateAvItem() { }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            if (_actorPicturs.ContainsKey(e.OriginalUrl))
            {
                var ext = Path.GetExtension(e.OriginalUrl);
                var fname = $"{_actorPicturs[e.OriginalUrl]}{ext}".Replace(' ', '_');
                e.SuggestedFileName = $"{_actorPicturePath}\\{fname}";

                var an = _actorNames.FirstOrDefault(n => n.Name == _actorPicturs[e.OriginalUrl]);
                if (an != null)
                    an.Actor.PicturePath = fname;
            }
            else
            {
                var ext = Path.GetExtension(e.SuggestedFileName);
                e.SuggestedFileName = $"{PosterPath}{ext}";
            }
        }

        void ParseActor(List<object> items)
        {
            HtmlDocument doc = new HtmlDocument();
            foreach (string html in items)
            {
                var ll = new List<List<AvActorName>>();
                doc.LoadHtml(html);
                var nameNode = doc.DocumentNode.SelectSingleNode("//span");
                var actorName = nameNode.InnerText.Trim();
                //Log.Print($"name : {actorName}");
                var list = new List<AvActorName> {
                    new AvActorName { Name = actorName }
                };

                var imageNode = doc.DocumentNode.SelectSingleNode("//img");
                var imgUrl = imageNode.Attributes["src"].Value;
                if (_actorPicturs.ContainsKey(imgUrl)) continue;

                ll.Add(list);
                UpdateActor2(ll);

                _actorNames.AddRange(list);
                //Log.Print($"image lilnk : {imgUrl}");
                if (!imgUrl.EndsWith("nowprinting.gif") &&
                    !_actorPicturs.ContainsKey(imgUrl))
                {
                    //Interlocked.Increment(ref _numItemsToScrap);
                    _actorPicturs.Add(imgUrl, actorName);
                    //_spider.Browser.Download(imgUrl);
                    _spider.Download(imgUrl, ref _numItemsToScrap);
                }
            }
        }

        void ParseInfo(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            var node = doc.DocumentNode.SelectSingleNode("//*[text()[contains(., 'Release Date')]]");
            var relDate = node.ParentNode.InnerText.Split(':').Last();

            _avItem.DateReleased = DateTime.ParseExact(
                relDate.Trim(), "yyyy-MM-dd", enUS);

            node = doc.DocumentNode.SelectSingleNode("//*[text()[contains(., 'Studio')]]");
            do {
                node = node.NextSibling;
            } while (node.Name != "p");
            UpdateStudio(node.InnerText);
        }

        public override void OnJsResult(string name, List<object> items)
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
                else if (name == "actor")
                {
                    ParseActor(items);
                }
                else if (name == "info")
                {
                    ParseInfo(items[0] as string);
                }
                else if (name == "title")
                {
                    var title = items[0] as string;
                    var start = title.IndexOf(_spider.Keyword) + _spider.Keyword.Length;
                    title = title.Substring(start);
                    UpdateTitle(title);
                }
#if false
                else if (name == "release")
                {
                    var strDate = items[0] as string;
                    _avItem.DateReleased = DateTime.ParseExact(
                        strDate.Trim(), "yyyy-MM-dd", enUS);
                }
                else if (name == "studio")
                {
                    UpdateStudio(items[0] as string);
                }
#endif
            }
            CheckCompleted();
        }
    }
}
