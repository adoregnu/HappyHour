using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using HappyHour.Extension;

namespace HappyHour.Model
{
    internal enum DateType { Released, Added, Updated }

    internal class AvMovie : AvMediaBase
    {
        public static DateType DateType = DateType.Released;
        private string _actresses;
        private AvItem _movieInfo;
        private DateTime _date;

        public override DateTime Date => _movieInfo == null ? _date
            : DateType == DateType.Released ? MovieInfo.DateReleased
            : DateType == DateType.Added ? MovieInfo.DateAdded
            : MovieInfo.DateModifed;

        public string Actresses
        {
            get => _actresses;
            set => Set(ref _actresses, value);
        }

        public List<string> Files { get; set; } = new();
        public List<string> Subtitles { get; set; } = new();

        public AvItem MovieInfo
        {
            get => _movieInfo;
            set
            {
                if (_movieInfo != null && value == null)
                {
                    App.DbContext.Items.Remove(_movieInfo);
                    App.DbContext.SaveChanges();
                }
                if (_movieInfo == value)
                {
                    UpdateProperties();
                }
                _movieInfo = value;
            }
        }

        public AvMovie(string path)
        {
            Path = path;
            Pid = path.Split('\\').Last();
        }

        public void ClearDb()
        {
            if (MovieInfo != null)
            {
                MovieInfo = null;
            }
        }

        private void UpdateProperties()
        {
            string tmp = Pid;
            if (Subtitles.Count > 0)
            {
                tmp += "(sub)\n";
            }
            if (MovieInfo == null)
            {
                tmp += Date.ToString("u");
                Actresses = "";
            }
            else
            {
                tmp += MovieInfo.Studio == null ? "" : MovieInfo.Studio.Name;
                Actresses = MovieInfo.ActorsName();
            }
            BriefInfo = tmp;
        }

        private readonly string[] sub_exts = new string[] {
            ".smi", ".srt", ".sub", ".ass", ".ssa", ".sup"
        };
        private  readonly string[] video_exts = new string[] {
            ".mp4", ".avi", ".mkv", ".ts", ".wmv", ".m4v"
        };

        public override async void Reload(string[] files)
        {
            if (files == null)
            {
                files = Directory.GetFiles(Path);
            }
            Files.Clear();
            Subtitles.Clear();
            _date = File.GetCreationTime(Path);
            foreach (string file in files)
            {
                if (file.Contains("_poster."))
                {
                    Poster = file;
                }
                else if (sub_exts.Any(s => file.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                {
                    Subtitles.AddInOrder(file, f => f, true);
                }
                else if (video_exts.Any(s => file.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                {
                    Files.AddInOrder(file, f => f, true);
                }
            }

            MovieInfo = await App.DbContext.Items
                .Include(av => av.Studio)
                .Include(av => av.Actors)
                    .ThenInclude(ac => ac.Names)
                .FirstOrDefaultAsync(av => av.Pid == Pid);

            UpdateProperties();
        }
    }
}
