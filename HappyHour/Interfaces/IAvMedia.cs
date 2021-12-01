using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Interfaces
{
    internal enum MediaType { Torrent, Movie }
    internal enum DateType { Released, Added, Updated }

    internal interface IAvMedia
    {
        string Pid { get; set; }
        string Path { get; set; }
        string Poster { get; set; }
        DateTime Date { get; set; }
        MediaType Type { get; set; }
        DateType DateType { get; set; }

        List<string> GetFiles();
        void UpdateInfo(string file);
    }
}
