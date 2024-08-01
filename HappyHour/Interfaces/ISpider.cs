using HappyHour.Spider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Interfaces
{
    internal delegate void ScrapCompletedHandler(SpiderBase spider);

    interface ISpider
    {
        ScrapCompletedHandler OnScanCompleted { get; set; }
        void Scan(SpiderBase spider, IAvMedia media);
        void ScanDone(SpiderBase spider);
    }
}
