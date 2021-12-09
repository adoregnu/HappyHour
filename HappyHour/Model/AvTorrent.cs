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

        public AvTorrent(string path)
        {
            Path = path;
            Pid = path.Split('\\').Last();
        }

        public void Download()
        {
            try
            {
                foreach (string file in Torrents)
                {
                    string torrent = System.IO.Path.GetFileName(file);
                    File.Copy(file, App.GConf["general"]["torrent_path"] + torrent);
                }
                File.Create($"{Path}\\.downloaded").Dispose();
                Log.Print($"Mark downloaded {Path}");
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        public void Exclude()
        {
            try
            {
                File.Create($"{Path}\\.excluded").Dispose();
                Log.Print($"Mark excluded {Path}");
            }
            catch (Exception ex)
            {
                Log.Print("Exclude: ", ex.Message);
            }
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
                else if (file.EndsWith("torrent", StringComparison.OrdinalIgnoreCase))
                {
                    Torrents.Add(file);
                    Date = File.GetCreationTime(file);
                }
            }
            BriefInfo = $"{Pid}\n{Date}";
        }
    }
}
