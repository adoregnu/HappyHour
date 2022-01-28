using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HappyHour.Model;
using HappyHour.Spider;

namespace HappyHour.Interfaces
{
    internal delegate void MediaListItemSelected(object sender, IAvMedia item);

    internal interface IMediaList
    {
        void AddMedia(string path);
        void LoadItems(List<AvItem> movies);

        IEnumerable<SpiderBase> SpiderList { get; set; }
        MediaListItemSelected ItemSelectedHandler { get; set; }
        MediaListItemSelected ItemDoubleClickedHandler { get; set; }
    }
}
