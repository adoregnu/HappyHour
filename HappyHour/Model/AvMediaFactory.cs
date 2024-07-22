using HappyHour.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.Model
{
    internal class AvMediaFactory
    {
        public static async ValueTask<IAvMedia> Create(string path)
        {
            string[] video_exts = [
                ".mp4", ".avi", ".mkv", ".ts", ".wmv", ".m4v"
            ];

            string[] files;
            try
            {
                files = Directory.GetFiles(path, "*", new EnumerationOptions { RecurseSubdirectories = true });
            }
            catch (Exception ex)
            {
                Log.Print("Create AvMedia failed!", ex);
                return null;
            }
            if (files.Length > 20)
            {
                Log.Print("Too many files in media folder!");
                return null;
            }
            bool downloaded = false, excluded = false;
            bool torrent = false, movie = false;

            foreach (string file in files)
            {
                if (file.EndsWith(".downloaded", StringComparison.OrdinalIgnoreCase))
                {
                    downloaded = true;
                }
                else if (file.EndsWith(".excluded", StringComparison.OrdinalIgnoreCase))
                {
                    excluded = true;
                }
                else if (file.EndsWith("torrent", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith("magnet", StringComparison.OrdinalIgnoreCase))
                {
                    torrent = true;
                }
                else if (video_exts.Any(e => file.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
                {
                    movie = true;
                }
            }
            if (downloaded || excluded)  return null;

            IAvMedia media = null;
            if (torrent)  media = new AvTorrent(path);
            else if (movie) media = new AvMovie(path);

            if (media != null)
            {
                await media.Reload(files);
                return media;
            }
            return null;
        }
    }
}
