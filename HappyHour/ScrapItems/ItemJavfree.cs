using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CefSharp;

using HappyHour.Spider;
using HappyHour.Extension;

namespace HappyHour.ScrapItems
{
    class ItemJavfree : AvItemBase
    {
        public ItemJavfree(SpiderBase spider) : base(spider)
        {
            Elements = new List<(string name, string element, ElementType type)>
            {
                ( "cover", "//div[@class='entry-content']//img[1]/@src", ElementType.XPATH)
            };
        }

        protected override void UdpateAvItem() { }

        protected override void OnBeforeDownload(object sender, DownloadItem e)
        {
            var ext = Path.GetExtension(e.SuggestedFileName);
            e.SuggestedFileName = $"{PosterPath}{ext}";
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
                    _spider.Download(url, ref _numItemsToScrap);
                }
            }
            CheckCompleted();
        }
    }
}
