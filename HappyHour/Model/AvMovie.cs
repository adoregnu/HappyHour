using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using HappyHour.Extension;
using System.Threading.Tasks;
using HappyHour.Interfaces;
using CefSharp.DevTools.WebAudio;
using System.Windows.Navigation;

namespace HappyHour.Model
{
    internal class AvMovie : AvMediaBase
    {
        private readonly MovieDbContext _db = App.Current.DbContext;

        private string _actresses;
        private DateTime _dateCreated;
        public static string SpiderName = "";

        public string Actresses
        {
            get => _actresses;
            set => SetProperty(ref _actresses, value);
        }

        public override DateTime Date
        {
            get
            {
                if (_movieInfo == null) return _dateCreated;

                return Sort switch
                {
                    SortType.DateAdded => _movieInfo.DateAdded,
                    SortType.DateReleased => _movieInfo.DateReleased,
                    _ => _dateCreated
                };
            }
        }

        public List<string> Files { get; set; } = [];
        public List<string> Subtitles { get; set; } = [];

        public string ColoredFileState
        {
            get => Files.IsNullOrEmpty() ? "MistyRose" : "AliceBlue";
        }

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
        ImageBlob _imageBlob;
        public ImageBlob ImageBlob
        {
            get => _imageBlob;
            set => SetProperty(ref _imageBlob, value);
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

        public Rating GetRate(string spiderName)
        {
            if (MovieInfo == null || MovieInfo.Ratings == null) return null;
            return MovieInfo.Ratings.FirstOrDefault(r => r.SiteUrl.Contains(spiderName, StringComparison.OrdinalIgnoreCase));
        }
        int CompareRating(IAvMedia media)
        {
            string site = SpiderName;
            var thisRate = GetRate(site);
            var otherRate = ((AvMovie)media).GetRate(site);
            if (thisRate == null && otherRate == null) return 0;
            if (thisRate != null && otherRate == null) return -1;
            if (otherRate != null && thisRate ==  null) return 1;
            return thisRate.Rate > otherRate.Rate ? -1 : 1;
        }
        public override int CompareTo(IAvMedia media)
        {
            return Sort switch
            {
                SortType.Rating => CompareRating(media),
                _ => base.CompareTo(media)
            };
        }
        public void ClearDb(bool realClear = false)
        {
            if (MovieInfo != null)
            {
                _db.RemoveMovie(_movieInfo, realClear);
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
                tmp += _dateCreated.ToString("u");
                Actresses = "Not Scrapped";
                ImageBlob = null;
            }
            else
            {
                if (MovieDbContext.GetLable(MovieInfo) is string label)
                {
                    tmp += label;
                }
                var names = MovieDbContext.GetActorsNames(MovieInfo, "ko");
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
                _dateCreated = File.GetCreationTime(Path);
            } catch { }

            if (files == null) return;

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

        public override void Reload(string[] files)
        {
            LoadFiles(files);
        }

        public async override Task ReloadAsync(string[] files)
        {
            LoadFiles(files);

            MovieInfo = await _db.GetMovie(Pid, false);
            if (MovieInfo == null) return;
            if (MovieInfo.VideoUrl != Path)
            {
                Log.Print($"{MovieInfo.VideoUrl}, {Path}");
                MovieInfo.VideoUrl = Path;
            }
        }
    }
}
