using HappyHour.Spider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Interfaces
{
    interface IDownloader
    {
        void Enable(bool bEnable);
        Task Download(SpiderBase spider, IDictionary<string, object> items);
    }
}
