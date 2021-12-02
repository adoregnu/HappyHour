using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace HappyHour.Model
{
    public enum OrderType { 
        ByDateReleased,
        ByDateAdded,
        ByDateUpdated,
        ByDateDownload,
    };
    public class MediaItem : NotifyPropertyChanged
    {
        private static AvDbContext _dbContext;
        private DateTime _dateDownloaded;

        public static OrderType OrderType { get; set; } = OrderType.ByDateReleased;
        public DateTime DateTime
        {
            get
            {
                if (_avItem == null)
                {
                    return _dateDownloaded;
                }
                else if (OrderType == OrderType.ByDateReleased)
                {
                    return _avItem.DateReleased;
                }
                else if (OrderType == OrderType.ByDateAdded)
                {
                    return _avItem.DateAdded;
                }
                else
                {
                    // OrderType.ByDateUpdated
                    return _avItem.DateModifed;
                }
            }
        }

        public string Pid { get; private set; }
        public string MediaPath { get; private set; }
        public string MediaFile => MediaFiles.Count > 0 ? MediaFiles[0] : null;
        public string Torrent { get; private set; }
        public string Poster { get; set; }

        public List<string> MediaFiles;
        public List<string> Subtitles;
        public List<string> Screenshots { get; set; }

        public bool IsDownload { get; private set; }
        public bool IsExcluded { get; private set; }
        public bool IsImage { get; private set; } = true;
        public bool IsMediaFolder => !string.IsNullOrEmpty(MediaFile);

        public string Info
        {
            get
            {
                string pid = Pid;
                if (Subtitles != null)
                {
                    pid += "(sub)";
                }
                if (AvItem == null)
                {
                    return $"{pid}\n" + _dateDownloaded.ToString("u");
                }
                string studio = AvItem.Studio != null ? AvItem.Studio.Name : "Unknown Studio";
                return $"{pid}\n{studio}";
            }
        }

        public string Actors => AvItem == null ? "Not Scrapped" : AvItem.ActorsName();

        private AvItem _avItem;
        public AvItem AvItem
        {
            get => _avItem;
            set
            {
                Set(ref _avItem, value);
                RefreshAvInfo();
            }
        }

        public static MediaItem Create(string path)
        {
            try
            {
                if (_dbContext == null)
                {
                    _dbContext = App.DbContext;//new AvDbContext();
                }
                MediaItem item = new(path);
                if (!item.IsExcluded && !item.IsDownload && item.IsMediaFolder)
                {
                    return item;
                }
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
            return null;
        }

        private MediaItem(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("Not found " + path);
            }
            MediaPath = path;
            Pid = path.Split('\\').Last();
            ReloadAvItem();
        }

        private void OnMoveDone(string newPath, Action<MediaItem> OnComplete)
        {
            MediaPath = newPath;
            if (AvItem != null)
            {
                AvItem.Path = MediaPath;
                _dbContext.SaveChanges();
            }
            OnComplete?.Invoke(this);
        }

        public void MoveItem(string targetPath, Action<MediaItem> OnComplete = null)
        {
            try
            {
                if (char.ToUpper(MediaPath[0], App.enUS) == char.ToUpper(targetPath[0], App.enUS))
                {
                    targetPath += "\\" + Pid;
                    Directory.Move(MediaPath, targetPath);
                    OnMoveDone(targetPath, OnComplete);
                }
                //TODO: copy file if target is different drive
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        public void Download()
        {
            try
            {
                string dir = Path.GetDirectoryName(MediaFile);
                string torrent = Path.GetFileName(Torrent);
                File.Copy(Torrent, App.GConf["general"]["torrent_path"] + torrent);
                File.Create($"{dir}\\.downloaded").Dispose();
                Log.Print($"Makrk downloaded {Torrent}");
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        public void Exclude()
        {
            string dir = Path.GetDirectoryName(MediaFile);
            File.Create($"{dir}\\.excluded").Dispose();
            Log.Print($"Mark excluded {Torrent}");
        }

        public bool DeleteItem()
        {
            try
            {
                if (AvItem != null)
                {
                    _dbContext.Items.Remove(AvItem);
                    _dbContext.SaveChanges();
                    AvItem = null;
                }
                Directory.Delete(MediaPath, true);
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
                return false;
            }
            return true;
        }

        public void ClearDb()
        {
            if (AvItem != null)
            {
                _dbContext.Items.Remove(AvItem);
                _dbContext.SaveChanges();
                AvItem = null;
            }
        }

        public void RefreshAvInfo()
        {
            RaisePropertyChanged(nameof(Info));
            RaisePropertyChanged(nameof(Actors));
            RaisePropertyChanged(nameof(Poster));
        }

        public async void ReloadAvItem()
        {
            UpdateFields();
            if (IsImage)
            {
                return;
            }

            AvItem = await _dbContext.Items
                .Include(i => i.Studio)
                .Include(i => i.Genres)
                .Include(i => i.Actors)
                    .ThenInclude(a => a.Names)
                .FirstOrDefaultAsync(i => i.Pid == Pid);
        }

        private void UpdateFields()
        {
            MediaFiles = new List<string>();
            Subtitles = null;
            foreach (string file in Directory.GetFiles(MediaPath))
            {
                UpdateField(file);
                if (IsExcluded || IsDownload)
                {
                    return;
                }
            }
            if (MediaFile == null)
            {
                return;
            }

            _dateDownloaded = File.GetLastWriteTime(MediaFile);
            MediaFiles.Sort();
            if (Subtitles != null)
            {
                Subtitles.Sort();
            }
        }

        private void UpdateField(string path)
        {
            string[] vexts = new string[] {
                ".mp4", ".avi", ".mkv", ".ts", ".wmv", ".m4v"
            };
            string[] subs = new string[] {
                ".smi", ".srt", ".sub", ".ass", ".ssa", ".sup"
            };

            string fname = Path.GetFileName(path);
            if (fname.EndsWith("torrent", StringComparison.OrdinalIgnoreCase))
            {
                Torrent = path;
            }
            else if (fname.Contains("screenshot"))
            {
                if (Screenshots == null)
                {
                    Screenshots = new List<string>();
                }
                Screenshots.Add(path);
            }
            else if (fname.Contains("_poster."))
            {
                Poster = path;
            }
            else if (fname.EndsWith(".downloaded", StringComparison.OrdinalIgnoreCase))
            {
                IsDownload = true;
            }
            else if (fname.EndsWith(".excluded", StringComparison.OrdinalIgnoreCase))
            {
                IsExcluded = true;
            }
            else if (fname.Contains("_cover."))
            {
                MediaFiles.Add(path);
                Poster = path;
            }
            else if (subs.Any(s => fname.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                if (Subtitles == null)
                {
                    Subtitles = new List<string>();
                }
                Subtitles.Add(path);
            }
            else if (vexts.Any(s => fname.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
            {
                IsImage = false;
                MediaFiles.Add(path);
            }
        }
    }
}
