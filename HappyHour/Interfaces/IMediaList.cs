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
        Task AddMedia(string path);
        void AddMedia(Movie movie);
        Task LoadItems(List<Movie> movies);

        IEnumerable<SpiderBase> SpiderList { get; set; }
        MediaListItemSelected ItemSelectedHandler { get; set; }
        MediaListItemSelected ItemDoubleClickedHandler { get; set; }
    }
}
