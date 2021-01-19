using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using GalaSoft.MvvmLight;

using HappyHour.Utils;
using HappyHour.Model;

namespace HappyHour.Model
{
    public enum OrderType { 
        ByDateReleased,
        ByDateAdded,
        ByDateUpdated,
        ByDateDownload,
    };
    public class MediaItem : ViewModelBase
    {
        string _bgImagePath;
        DateTime _dateDownloaded;
        static AvDbContext _dbContext;

        public static OrderType OrderType { get; set; } = OrderType.ByDateReleased;

        public DateTime DateTime
        {
            get
            {
                if (_avItem == null)
                    return _dateDownloaded;
                else if (OrderType == OrderType.ByDateReleased)
                    return _avItem.DateReleased;
                else if (OrderType == OrderType.ByDateAdded)
                    return _avItem.DateAdded;
                else // OrderType.ByDateUpdated
                    return _avItem.DateModifed;
            }
        }

        public string MediaName { get; private set; }
        public string MediaFile { get; private set; }
        public string MediaFolder { get; private set; }
        public string Torrent { get; private set; }
        public string BgImagePath
        {
            get => !string.IsNullOrEmpty(_bgImagePath) ?
                        $"{MediaFolder}\\{_bgImagePath}" : null;
            private set => Set(ref _bgImagePath, value);
        }
        public string Pid { get; private set; }

        public bool IsDownload { get; private set; } = false;
        public bool IsExcluded { get; private set; } = false;
        public bool IsImage { get; private set; } = true;
        public bool IsMediaFolder
        {
            get { return !string.IsNullOrEmpty(MediaFile); }
        }
        public List<string> Screenshots { get; private set; } = new List<string>();

        public static string[] VideoExts = new string[] {
            ".mp4", ".avi", ".mkv", ".ts", ".wmv", ".m4v"
        };

        public string Info
        {
            get
            {
                if (AvItem == null) return Pid;
                var studio = AvItem.Studio != null ?
                    AvItem.Studio.Name : "Studio Unknown";
                return $"{AvItem.Pid}\n{studio}";
            }
        }

        public string Actors
        {
            get
            {
                if (AvItem == null) return "Not Scrapped";
                return AvItem.ActorsName();
            }
        }

        AvItem _avItem = null;
        public AvItem AvItem
        {
            get => _avItem;
            set
            {
                Set(ref _avItem, value);
                RaisePropertyChanged(nameof(Info));
                RaisePropertyChanged(nameof(Actors));
                RaisePropertyChanged(nameof(BgImagePath));
            }
        }

        public static MediaItem Create(string path)
        {
            try
            {
                if (_dbContext == null)
                    _dbContext = App.DbContext;//new AvDbContext();
                var item = new MediaItem(path);
                if (!item.IsExcluded && !item.IsDownload && item.IsMediaFolder)
                    return item;
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
            return null;
        }

        public MediaItem(string path = null)
        {
            if (!string.IsNullOrEmpty(path))
            {
                MediaFolder = path;
                UpdateFields();
            }
        }

        static readonly SerialQueue _serialQueue = new SerialQueue();
        void OnMoveDone(string newPath, Action<MediaItem> OnComplete)
        {
            MediaFolder = newPath;
            if (AvItem != null)
            {
                AvItem.Path = MediaFolder;
                _dbContext.SaveChanges();
            }
            OnComplete?.Invoke(this);
        }

        void CopyFolder(string targetPath, Action<MediaItem> OnComplete)
        {
#if false
            int prev = 0;
            Log.Print($"move {MediaFolder} => {targetPath}");
            var ret = FileTransferManager.CopyWithProgress(
                MediaFolder, targetPath, p =>
                {
                    var curr = (int)p.Percentage;
                    if (prev != curr && curr % 1 == 0)
                    {
                        Log.Print(string.Format("{0}%, {1:f2}Mb/sec",
                            curr, p.BytesPerSecond / (1024 * 1024)));
                        prev = (int)p.Percentage;
                    }
                }, false);

            Log.Print(ret.ToString());
            if (ret == TransferResult.Success)
            {
                Directory.Delete(MediaFolder, true);
                OnMoveDone(targetPath + "\\" + Pid, OnComplete);
            }
#endif
        }
        public bool MoveItem(string targetPath, Action<MediaItem> OnComplete = null)
        {
            try
            {
                if (char.ToUpper(MediaFolder[0]) == char.ToUpper(targetPath[0]))
                {
                    targetPath += "\\" + Pid;
                    Directory.Move(MediaFolder, targetPath);
                    OnMoveDone(targetPath, OnComplete);
                }
                else
                {
                    _serialQueue.Enqueue(() => CopyFolder(targetPath, OnComplete));
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
            return false;
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
                Directory.Delete(MediaFolder, true);
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

        void UpdateMediaField(string path)
        {
            MediaFile = path;
            MediaFolder = Path.GetDirectoryName(path);
            Pid = MediaFolder.Split('\\').Last();
            _dateDownloaded = File.GetLastWriteTime(path);
            MediaName = $"{Pid}\n" + DateTime.ToString("u");
        }

        public void RefreshAvInfo()
        {
            RaisePropertyChanged(nameof(Info));
            RaisePropertyChanged(nameof(Actors));
            RaisePropertyChanged(nameof(BgImagePath));
        }

        public async void ReloadAvItem()
        {
            AvItem = await _dbContext.Items
                .Include(i => i.Studio)
                .Include(i => i.Genres)
                .Include(i => i.Actors)
                    .ThenInclude(a => a.Names)
                .FirstOrDefaultAsync(i => i.Pid == Pid);
        }

        public void UpdateFields()
        {
            foreach (var file in Directory.GetFiles(MediaFolder))
            {
                UpdateField(file);
                if (IsExcluded || IsDownload) return;
            }
            ReloadAvItem();
        }

        public void UpdateField(string path)
        {
            string fname = Path.GetFileName(path);
            if (fname.EndsWith("torrent"))
            {
                Torrent = path;
            }
            else if (fname.Contains("screenshot"))
            {
                Screenshots.Add(path);
            }
            else if (fname.Contains("_poster."))
            {
                BgImagePath = fname;
            }
            else if (fname.Contains("-fanart."))
            {
                var ext = Path.GetExtension(fname);
                var head = path.Substring(0, path.LastIndexOf('-'));
                var target = $"{head}_poster{ext}";
                if (!File.Exists(target))
                {
                    File.Move(path, target);
                    BgImagePath = Path.GetFileName(target);
                }
            }
            else if (string.IsNullOrEmpty(BgImagePath) && fname.Contains("_thumbnail."))
            {
                BgImagePath = fname;
            }
            else if (fname.EndsWith(".downloaded"))
            {
                IsDownload = true;
            }
            else if (fname.EndsWith(".excluded"))
            {
                IsExcluded = true;
            }
            else if (fname.Contains("_cover."))
            {
                UpdateMediaField(path);
            }
            else if (VideoExts.Any(s => fname.EndsWith(s, StringComparison.CurrentCultureIgnoreCase)))
            {
                IsImage = false;
                UpdateMediaField(path);
                if (AvItem == null)
                {
                    ReloadAvItem();
                }
            }
        }
    }
}
