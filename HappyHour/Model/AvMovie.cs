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
                    using var context = AvDbContextPool.CreateContext();
                    _ = context.Attach(value);
                    _ = context.Items.Remove(_movieInfo);
                    _ = context.SaveChanges();
                }
                _movieInfo = value;
                UpdateProperties();
            }
        }

        public AvMovie(string path) : base()
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

        public void Move(string target, Action<AvMovie> OnCompleted)
        {
            try
            {
                if (char.ToUpper(Path[0], App.Current.enUS) == char.ToUpper(target[0], App.Current.enUS))
                {
                    target += "\\" + Pid;
                    Directory.Move(Path, target);
                    Path = target;
                    if (MovieInfo != null)
                    {
                        using var context = AvDbContextPool.CreateContext();
                        context.Attach(MovieInfo);
                        MovieInfo.Path = target;
                        _ = context.SaveChanges();
                    }
                    OnCompleted(this);
                }
            }
            catch (Exception ex)
            {
                Log.Print("Mvoe:", ex);
            }
        }

        public bool Delete()
        {
            try
            {
                if (MovieInfo != null)
                {
                    using var context = AvDbContextPool.CreateContext();
                    context.Items.Attach(MovieInfo);
                    _ = context.Items.Remove(MovieInfo);
                    _ = context.SaveChanges();
                }
                Directory.Delete(Path, true);
            }
            catch (Exception ex)
            {
                Log.Print("Delete:", ex);
                return false;
            }
            return true;
        }

        private void UpdateProperties()
        {
            string tmp = Pid;
            if (Subtitles.Count > 0)
            {
                tmp += "(sub)";
            }
            tmp += "\n";
            if (MovieInfo == null)
            {
                tmp += Date.ToString("u");
                Actresses = "Not Scrapped";
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
            files ??= Directory.GetFiles(Path);
            Files.Clear();
            Subtitles.Clear();
            _date = File.GetCreationTime(Path);
            foreach (string file in files)
            {
                if (file.Contains("_poster.", StringComparison.OrdinalIgnoreCase))
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
            if (string.IsNullOrEmpty(Poster))
            {
                Poster = "";
            }

            using var context = AvDbContextPool.CreateContext();
            MovieInfo = await context.Items
                .Include(av => av.Studio)
                .Include(av => av.Actors)
                    .ThenInclude(ac => ac.Names)
                .Include(av => av.Genres)
                .FirstOrDefaultAsync(av => av.Pid == Pid);

            UpdateProperties();
        }
    }
}
