using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HappyHour.Model
{
    internal class AvTorrent : AvMediaBase
    {
        public List<string> Screenshots { get; set; } = new();
        public List<string> Torrents { get; set; } = new();
        public bool Downloaded { get; set; }
        public bool Excluded { get; set; }

        public AvTorrent(string path)
        {
            Path = path;
            Pid = path.Split('\\').Last();
        }

        public override void Reload(string[] files = null)
        {
            if (files == null)
            {
                files = Directory.GetFiles(Path);
            }
            Torrents.Clear();
            Screenshots.Clear();
            foreach (string file in files)
            {
                if (file.Contains("_poster.") || file.Contains("_cover."))
                {
                    Poster = file;
                }
                else if (file.Contains("_screenshot", StringComparison.OrdinalIgnoreCase))
                {
                    Screenshots.Add(file);
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
                    Date = File.GetCreationTime(file);
                }
            }
        }
    }
}
