using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HappyHour.Interfaces;

namespace HappyHour.Model
{
    class AvTorrent : NotifyPropertyChanged, IAvMedia
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

        public string[] GetFiles()
        {
            return null;
        }
        public void UpdateInfo(string file)
        {
            if (file.EndsWith(".downloaded"))
            {
                Downloaded = true;
            }
            else if (file.EndsWith(".excluded"))
            {
                Excluded = true;
            }
            else if (file.EndsWith("torrent"))
            {
                Torrents.Add(file);
            }
        }
    }
}
