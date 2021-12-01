using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HappyHour.Interfaces;

namespace HappyHour.Model
{
    static class AvMediaManager
    {
        const string tag = "AvMediaManager::";
        static public void Download(IAvMedia media)
        {
            try
            {
                var dir = Path.GetDirectoryName(media.Path);
                foreach (var file in media.GetFiles())
                {
                    var torrent = Path.GetFileName(file);
                    File.Copy(torrent, App.GConf["general"]["torrent_path"] + torrent);
                }
                File.Create($"{dir}\\.downloaded").Dispose();
                Log.Print($"{tag}Mark downloaded {media.Path}");
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        static public void Exclude(IAvMedia media)
        {
            try
            {
                var dir = Path.GetDirectoryName(media.Path);
                File.Create($"{dir}\\.excluded").Dispose();
                Log.Print($"{tag}Mark excluded {media.Path}");
            }
            catch (Exception ex)
            { 
                Log.Print($"{tag}", ex.Message);
            }
        }

        static public void Delete(IAvMedia media)
        {
            try
            {
                Directory.Delete(media.Path, true);
            }
            catch (Exception ex)
            {
                Log.Print($"{tag}", ex.Message);
            }
        }

        static public bool Move(IAvMedia media, string target, Action<IAvMedia> OnCompleted)
        {
            return true;
        }

        static public IAvMedia Create(string path)
        {
            string[] video_exts = new string[] {
                ".mp4", ".avi", ".mkv", ".ts", ".wmv", ".m4v"
            };

            IAvMedia media = null;
            var files = Directory.GetFiles(path);
            if (files.Length > 20)
            {
                Log.Print("Too many files in media folder!");
                return null;
            }
            if (files.Any(f => f.EndsWith("torrent", StringComparison.OrdinalIgnoreCase)))
            {
                media = new AvTorrent(path);
            }
            else if (video_exts.Any(e => files.Any(f => f.EndsWith(e, StringComparison.OrdinalIgnoreCase))))
            {
                media = new AvMovie(path);
            }

            foreach (var file in files)
            {
                if (file.Contains("_poster."))
                {
                    media.Poster = file;
                }
                else if (file.Contains("screenshot"))
                {
                    media.ScreenShots.Add(file);
                }
                else
                {
                    media.UpdateInfo(file);
                }
            }
            return null;
        }
    }
}
