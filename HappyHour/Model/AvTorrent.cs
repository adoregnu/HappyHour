using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using HappyHour.Interfaces;

namespace HappyHour.Model
{
    internal class AvTorrent : NotifyPropertyChanged, IAvMedia
    {
        public string Pid { get; set; }
        public string Path { get; set; }
        public string Poster { get; set; }
        public MediaType Type { get; set; }
        public DateTime Date { get; set; }
        public DateType DateType { get; set; }
        public List<string> ScreenShots { get; set; } = new();

        public List<string> Torrents { get; set; } = new();
        public bool Downloaded { get; set; }
        public bool Excluded { get; set; }

        public AvTorrent(string path)
        {
            Type = MediaType.Torrent;
            Path = path;
            Pid = path.Split('\\').Last();
        }

        public List<string> GetFiles()
        {
            return Torrents;
        }
        public void UpdateInfo(string file)
        {
            if (file.EndsWith(".downloaded", StringComparison.OrdinalIgnoreCase))
            {
                Downloaded = true;
            }
            else if (file.EndsWith(".excluded", StringComparison.OrdinalIgnoreCase))
            {
                Excluded = true;
            }
            else if (file.EndsWith("torrent", StringComparison.OrdinalIgnoreCase))
            {
                Torrents.Add(file);
            }
        }
    }
}
