using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HappyHour.Model;

namespace HappyHour.Interfaces
{
    public delegate void MediaListItemSelectedEventHandler(object sender, MediaItem item);
    interface IMediaList
    {
        void AddMedia(string path);
        void RemoveMedia(string path);
        void Replace(IEnumerable<string> paths);

        MediaListItemSelectedEventHandler ItemSelectedHandler { get; set; }
        MediaListItemSelectedEventHandler ItemDoubleClickedHandler { get; set; }
    }
}
