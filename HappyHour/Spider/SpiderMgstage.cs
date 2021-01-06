using System;
using System.Collections.Generic;
using System.Linq;

using CefSharp;

using HappyHour.Model;
using HappyHour.ScrapItems;
using HappyHour.ViewModel;

namespace HappyHour.Spider
{
    class SpiderMgstage : SpiderBase
    {
        public SpiderMgstage(SpiderViewModel browser) : base(browser)
        {
            Name = "MGStage";
            URL = "https://www.mgstage.com/";

        }

        public override List<Cookie> CreateCookie()
        {
            return new List<Cookie> {
                new Cookie
                {
                    Name = "adc",
                    Value = "1",
                    Domain = "mgstage.com",
                    Path = "/"
                }
            };
        }

        public override bool Navigate(MediaItem mitem)
        {
            if (!base.Navigate(mitem))
                return false;

            Browser.Address = $"{URL}product/product_detail/{Media.Pid.ToUpper()}/";
            return true;
        }

        public override void Scrap()
        {
            Dictionary<string, string> _xpathDic = new Dictionary<string, string>
            {
                { "title",   XPath("//div[@class='common_detail_cover']/h1[@class='tag']/text()") },
                { "cover",   XPath("//a[@id='EnlargeImage']/@href") },
                { "studio",  XPath("//th[contains(., 'メーカー：')]/following-sibling::td/a/@href") },
                { "runtime", XPath("//th[contains(., '収録時間：')]/following-sibling::td/text()") },
                { "id",      XPath("//th[contains(., '品番：')]/following-sibling::td/text()") },
                { "releasedate", XPath("//th[contains(., '配信開始日：')]/following-sibling::td/text()") },
                { "rating",  XPath("//th[contains(., '評価：')]/following-sibling::td//text()") },
            };

            if (StartScrapping)
            {
                ParsePage(new ItemMgstage(this)
                {
                    NumItemsToScrap = _xpathDic.Count
                }, _xpathDic);
            }
        }
    }
}
