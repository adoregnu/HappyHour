using System;
using System.IO;
using System.Linq;

using HappyHour.Interfaces;

namespace HappyHour.Model
{
    internal abstract class AvMediaBase : NotifyPropertyChanged, IAvMedia
    {
        private string _poster;
        private string _briefInfo;

        public string Pid { get; set; }
        public string Path { get; set; }
        public virtual DateTime Date { get; set; }
        public string Poster
        {
            get => _poster;
            set => Set(ref _poster, value);
        }
        public string BriefInfo
        {
            get => _briefInfo;
            set => Set(ref _briefInfo, value);
        }

        public bool IsPlayable
        {
            get => this is AvMovie;
        }

        public int CompareTo(IAvMedia media)
        {
            int result = Date.CompareTo(media.Date);
            return result == 0 ? Pid.CompareTo(media.Pid) : result;
        }

        public string GenPosterPath(string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName);
            return @$"{Path}\{Pid}_poster{ext}";
        }

        public string GenActorThumbPath(string actorName, string fileName)
        {
            string ext = System.IO.Path.GetExtension(fileName);
            //return $@"{Path}\{Pid}\.actors\{name.Remove(' ', '_')}{ext}";
            return $@"{App.LocalAppData}\db\{actorName.Replace(' ', '_')}{ext}";
        }

        public abstract void Reload(string[] files);

        public static IAvMedia Create(string path)
        {
            string[] video_exts = new string[] {
                ".mp4", ".avi", ".mkv", ".ts", ".wmv", ".m4v"
            };

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
            if (downloaded || excluded) { return null; }

            IAvMedia media = null;
            if (torrent) { media = new AvTorrent(path); }
            else if (movie) { media = new AvMovie(path); }

            if (media != null)
            {
                media.Reload(files);
                return media;
            }
            return null;
        }
    }
}
