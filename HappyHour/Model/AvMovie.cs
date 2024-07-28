using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using HappyHour.Extension;
using System.Threading.Tasks;
using CefSharp.DevTools.CSS;

namespace HappyHour.Model
{
    internal enum DateType { Released, Added, Updated }

    internal class AvMovie : AvMediaBase
    {
        public static DateType DateType = DateType.Released;
        private string _actresses;
        private DateTime _date;
        private readonly MovieDbContext _db = App.Current.DbContext;

        public override DateTime Date => _movieInfo == null ? _date
            : DateType == DateType.Released ? MovieInfo.DateReleased
            : MovieInfo.DateAdded;

        public string Actresses
        {
            get => _actresses;
            set => SetProperty(ref _actresses, value);
        }

        public List<string> Files { get; set; } = [];
        public List<string> Subtitles { get; set; } = [];

        private Movie _movieInfo;
        public Movie MovieInfo
        {
            get => _movieInfo;
            set
            {
                SetProperty(ref _movieInfo, value);
                UpdateProperties();
            }
        }

        public AvMovie(string path) : base()
        {
            Path = path;
            Pid = path.Split('\\').Last();
        }
        public AvMovie(Movie movie) : base()
        {
            Pid = movie.PID;
            Path = movie.VideoUrl;
            MovieInfo = movie;
            LoadFiles();
        }

        public void ClearDb()
        {
            if (MovieInfo != null)
            {
                _db.RemoveMovie(_movieInfo);
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
                        //using var context = new AvDbContextPool();
                        //context.UpdateMovie(MovieInfo, movie => { movie.Path = target; });
                    }
                    OnCompleted(this);
                }
            }
            catch (Exception ex)
            {
                Log.Print("move error:", ex);
            }
        }

        public bool Delete()
        {
            try
            {
                ClearDb();
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
                ImageBlob = null;
            }
            else
            {
                if (MovieDbContext.GetLable(MovieInfo) is string label)
                {
                    tmp += label;
                }
                var names = MovieDbContext.GetActorsNames(MovieInfo);
                if (names != null)
                {
                    Actresses = string.Join('\n', names);
                }
                else
                {
                    Actresses = "No Actors";
                }
                if (MovieInfo.Cover != null)
                {
                    ImageBlob = MovieInfo.Cover;
                }
            }
            BriefInfo = tmp;
        }

        private readonly string[] sub_exts = [
            ".smi", ".srt", ".sub", ".ass", ".ssa", ".sup"
        ];
        private  readonly string[] video_exts = [
            ".mp4", ".avi", ".mkv", ".ts", ".wmv", ".m4v"
        ];

        void LoadFiles(string[] files = null)
        {
            Files.Clear();
            Subtitles.Clear();

            try
            {
                files ??= Directory.GetFiles(Path);
                _date = File.GetCreationTime(Path);
            }
            catch (Exception ex)
            {
                Log.Print($"{ex.Message}");
            }

            foreach (string file in files)
            {
                if (sub_exts.Any(s => file.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                {
                    Subtitles.AddInOrder(file, f => f, true);
                }
                else if (video_exts.Any(s => file.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                {
                    Files.AddInOrder(file, f => f, true);
                }
            }
        }

        async public override Task Reload(string[] files)
        {
            LoadFiles(files);

            MovieInfo = await _db.GetMovie(Pid);
            if (MovieInfo == null) return;
            if (MovieInfo.VideoUrl != Path)
            {
                MovieInfo.VideoUrl = Path;
                //App.Current.DbContext.SaveChanges();
            }
        }
    }
}
