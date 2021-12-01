using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Interfaces
{
    enum MediaType { Torrent, Movie }
    enum DateType { Released, Added, Updated }

    interface IAvMedia
    {
        string Pid { get; set; }
        string Path { get; set; }
        string Poster { get; set; }
        DateTime Date { get; set; }
        MediaType Type { get; set; }
        DateType DateType { get; set; }
        public List<string> ScreenShots { get; set; }

        string[] GetFiles();
        void UpdateInfo(string file);
    }
}
