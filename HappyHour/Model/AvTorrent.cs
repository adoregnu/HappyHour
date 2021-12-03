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
        public string BriefInfo => $"{Pid}\n{Date}";
        public DateTime Date { get; set; }
        public List<string> ScreenShots { get; set; } = new();

        public List<string> Torrents { get; set; } = new();
        public bool Downloaded { get; set; }
        public bool Excluded { get; set; }

        public AvTorrent(string path)
        {
            Path = path;
            Pid = path.Split('\\').Last();
        }

        public void Reload(string[] files = null)
        {
            if (files == null)
            {
                files = Directory.GetFiles(Path);
            }
            Torrents.Clear();
            foreach (string file in files)
            {
                if (file.Contains("_poster.") || file.Contains("_cover."))
                {
                    Poster = file;
                }
                else if (file.EndsWith(".downloaded", StringComparison.OrdinalIgnoreCase))
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
}
