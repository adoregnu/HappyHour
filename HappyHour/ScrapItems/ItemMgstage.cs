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
    class ItemMgstage : AvItemBase
    {
        int _numDownloadCnt = 0;

        public ItemMgstage(SpiderBase spider) : base(spider)
        {
            Elements = new List<(string name, string element, ElementType type)>
            {
                ("title", "//div[@class='common_detail_cover']/h1[@class='tag']/text()", ElementType.XPATH),
                ( "cover", "//a[@id='EnlargeImage']/@href", ElementType.XPATH),
                ( "studio", "//th[contains(., 'メーカー：')]/following-sibling::td/a/text()", ElementType.XPATH),
                ( "runtime", "//th[contains(., '収録時間：')]/following-sibling::td/text()", ElementType.XPATH),
                ( "id", "//th[contains(., '品番：')]/following-sibling::td/text()", ElementType.XPATH),
                ( "releasedate", "//th[contains(., '配信開始日：')]/following-sibling::td/text()", ElementType.XPATH),
                ( "rating", "//th[contains(., '評価：')]/following-sibling::td//text()", ElementType.XPATH),
            };
        }

        //protected override void UdpateAvItem() { }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        { 
            var ext = Path.GetExtension(e.SuggestedFileName);
            if (_numDownloadCnt == 0)
                e.SuggestedFileName = $"{PosterPath}{ext}";
            else
                e.SuggestedFileName = $"{_spider.DataPath}\\" +
                    $"{_spider.Keyword}_screenshot{_numDownloadCnt}{ext}";
            Interlocked.Increment(ref _numDownloadCnt);
        }

        void ParseCover(string url)
        {
            var ext = url.Split('.').Last();
            if (!OverwriteImage && File.Exists($"{PosterPath}.{ext}"))
                return;

            if (!url.StartsWith("http"))
            {
                url = _spider.URL + url;
            }
            _spider.Download(url, ref _numItemsToScrap);
        }

        public override void OnJsResult(string name, List<object> items)
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
                    //var m = Regex.Match(items[0] as string, @"\]=([\w\d]+)");
                    //if (m.Success)
                    {
                        //Log.Print($"\tstudio:{m.Groups[1].Value}");
                        UpdateStudio(items[0] as string);
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
