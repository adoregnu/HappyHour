using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using HappyHour.Interfaces;
using HappyHour.Extension;

namespace HappyHour.Model
{
    internal enum DateType { Released, Added, Updated }

    internal class AvMovie : NotifyPropertyChanged, IAvMedia
    {
        private string _poster;
        private string _briefInfo;
        private string _actresses;
        private AvItem _movieInfo;

        public string Pid { get; set; }
        public string Path { get; set; }
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
        public string Actresses
        {
            get => _actresses;
            set => Set(ref _actresses, value);
        }
        public DateTime Date { get; set; }

        public List<string> Movies { get; set; } = new();
        public List<string> SubTitles { get; set; } = new();

        public AvItem MovieInfo
        {
            get => _movieInfo;
            set
            {
                //TODO: !!!
                if (_movieInfo != null && value == null)
                {
                    App.DbContext.Items.Remove(_movieInfo);
                    App.DbContext.SaveChanges();
                }
                _movieInfo = value;
            }
        }

        public AvMovie(string path)
        {
            Path = path;
            Pid = path.Split('\\').Last();
        }

        private async void UpdateProperties()
        {
            MovieInfo = await App.DbContext.Items
                .Include(av => av.Studio)
                .Include(av => av.Actors)
                    .ThenInclude(ac => ac.Names)
                .FirstOrDefaultAsync(av => av.Pid == Pid);

            string tmp = Pid;
            if (SubTitles.Count > 0)
            {
                tmp += "(sub)\n";
            }
            if (MovieInfo == null)
            {
                tmp += Date.ToString("u");
                Actresses = "Empty Actress";
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

        public void Reload(string[] files)
        {
            if (files == null)
            {
                files = Directory.GetFiles(Path);
            }
            Movies.Clear();
            SubTitles.Clear();
            Date = File.GetCreationTime(Path);
            foreach (string file in files)
            {
                if (file.Contains("_poster."))
                {
                    Poster = file;
                }
                else if (sub_exts.Any(s => file.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                {
                    SubTitles.AddInOrder(file, f => f, true);
                }
                else if (video_exts.Any(s => file.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                {
                    Movies.AddInOrder(file, f => f, true);
                }
            }
            UpdateProperties();
        }
    }
}
