using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Microsoft.EntityFrameworkCore;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

using MvvmDialogs;
using MvvmDialogs.FrameworkDialogs.FolderBrowser;

using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.Utils;
using HappyHour.Spider;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    using SpiderEnum = IEnumerable<SpiderBase>;

    enum MediaListMenuType
    { 
        excluded, downloaded, scrap
    }
    class MediaListViewModel : Pane, IMediaList
    {
        static readonly SerialQueue _serialQueue = new SerialQueue();

        MediaItem _selectedMedia = null;
        bool _isBrowsing = false;
        bool _sortByDateReleased = true;
        bool _sortByDateAdded = false;
        bool _searchSubFolder = false;
        IFileList _fileList;

        public MediaItem SelectedMedia
        {
            get => _selectedMedia;
            set
            {
                if (_selectedMedia != value)
                {
                    MessengerInstance.Send(new NotificationMessage<MediaItem>(
                        value, "mediaSelected"));
                }
                Set(ref _selectedMedia, value);
            }
        }
        public bool IsBrowsing
        {
            get => _isBrowsing;
            set => Set(ref _isBrowsing, value);
        }

        public ObservableCollection<MediaItem> MediaList { get; private set; }
        public SpiderEnum SpiderList { get; private set; }
        public bool SearchSubFolder
        {
            get => _searchSubFolder;
            set
            {
                Set(ref _searchSubFolder, value);
                RefreshMediaList(_fileList.CurrDirInfo);
            }
        }
        public bool SorByDateReleased
        {
            get => _sortByDateReleased;
            set
            {
                Set(ref _sortByDateReleased, value);
                if (value)
                {
                    MediaItem.OrderType = OrderType.ByDateReleased;
                    SorByDateAdded = !value;
                    SortMedia();
                }
            }
        }
        public bool SorByDateAdded
        {
            get => _sortByDateAdded;
            set
            {
                Set(ref _sortByDateAdded, value);
                if (value)
                {
                    MediaItem.OrderType = OrderType.ByDateAdded;
                    SorByDateReleased = !value;
                    SortMedia();
                }
            }
        }

        public IFileList FileList
        {
            get => _fileList;
            set
            {
                if (_fileList != value)
                {
                    _fileList = value;
                    _fileList.DirChanged += OnDirChanged;
                    _fileList.DirModifed += OnDirModifed;
                }
            }
        }

        public ICommand CmdExclude { get; set; }
        public ICommand CmdDownload { get; set; }
        public ICommand CmdMoveItem { get; set; }
        public ICommand CmdMoveItemTo { get; set; }
        public ICommand CmdDeleteItem { get; set; }
        public ICommand CmdClearDb { get; set; }
        public ICommand CmdEditItem { get; set; }
        public ICommand CmdDoubleClick { get; set; }
        public ICommand CmdSearchOrphanageMedia { get; set; }
        public ICommand CmdSearchEmptyActor { get; set; }

        public MediaListViewModel()
        {
            Title = "AVList";
            MediaList = new ObservableCollection<MediaItem>();

            CmdExclude = new RelayCommand<MediaItem>(
                p => OnContextMenu(p, MediaListMenuType.excluded));
            CmdDownload = new RelayCommand<MediaItem>(
                p => OnContextMenu(p, MediaListMenuType.downloaded));
            CmdMoveItem = new RelayCommand<object>(p => OnMoveItem(p));
            CmdMoveItemTo = new RelayCommand<object>(p => OnMoveItemTo(p));
            CmdDeleteItem = new RelayCommand<object>(p => OnDeleteItem(p));
            CmdClearDb = new RelayCommand<object>(p => OnClearDb(p));
            CmdEditItem = new RelayCommand<object>(p => OnEditItem(p));
            CmdDoubleClick = new RelayCommand(() => OnDoubleClicked());
            CmdSearchOrphanageMedia = new RelayCommand(() => OnSearchOrphanageMedia());
            CmdSearchEmptyActor = new RelayCommand(() => OnSearchEmptyActor());

            MessengerInstance.Register<NotificationMessage<SpiderEnum>>(this,
                (msg) => SpiderList = msg.Content.Where(i => i.Name != "sehuatang"));
        }

        void OnDirChanged(object sender, DirectoryInfo msg)
        {
            _searchSubFolder = false;
            RaisePropertyChanged(nameof(SearchSubFolder));
            RefreshMediaList(msg);
        }

        void RefreshMediaList(DirectoryInfo msg)
        { 
            ClearMedia();
            _serialQueue.Enqueue(() =>
                UpdateMediaList(msg.FullName, _searchSubFolder));
        }

        void OnDirModifed(object sender, FileSystemEventArgs e)
        {
            //Log.Print($"{e.ChangeType}");
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Deleted:
                {
                    var media = MediaList.FirstOrDefault(m => m.Pid == e.Name);
                    if (media != null)
                        MediaList.Remove(media);
                    break;
                }
                case WatcherChangeTypes.Created:
                    //AddMedia(e.FullPath);
                    break;
                case WatcherChangeTypes.Renamed:
                {
                    RenamedEventArgs re = e as RenamedEventArgs;
                    var media = MediaList.FirstOrDefault(m => m.Pid == re.OldName);
                    if (media != null)
                        MediaList.Remove(media);
                    break;
                }
            }
        }

        void SortMedia()
        {
            var tmp = MediaList.ToList();
            IsBrowsing = true;
            MediaList.Clear();
            foreach (var m in tmp)
            {
                //MediaList.Add(m);
                MediaList.InsertInPlace(m, i => i.DateTime);
            }
            IsBrowsing = false;
        }

        void ClearMedia()
        {
            IsBrowsing = true;
            MediaList.Clear();
            IsBrowsing = false;
        }

        public void AddMedia(string itemPath)
        {
            _serialQueue.Enqueue(() => UpdateMediaList(itemPath, true));
        }

        public void RemoveMedia(string path)
        {
            IsBrowsing = true;
            var medias = MediaList.Where(i => i.MediaFile.StartsWith(path,
                    StringComparison.CurrentCultureIgnoreCase)).ToList();
            medias.ForEach(x => MediaList.Remove(x));
            IsBrowsing = false; 
        }

        public void Replace(IEnumerable<string> paths)
        {
            IsBrowsing = true;
            MediaList.Clear();
            foreach (var path in paths)
            {
                InsertMedia(path);
            }
            IsBrowsing = false;
        }

        void UpdateMediaList(string path, bool bRecursive = false, int level = 0)
        {
            try
            {
                var dirs = Directory.GetDirectories(path);
                if (dirs.Length == 0 || dirs[0].EndsWith(".actors"))
                {
                    InsertMediaAsync(path);
                }
                else if (bRecursive || level < 1)
                {
                    foreach (var dir in dirs)
                    {
                        UpdateMediaList(dir, bRecursive, level + 1);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        void InsertMediaAsync(string path)
        {
            UiServices.Invoke(delegate
            {
                InsertMedia(path);
            }, true);
            //Thread.Sleep(10);
        }

        void InsertMedia(string path)
        {
            var item = MediaItem.Create(path);
            if (item == null) return;

            MediaList.InsertInPlace(item, i => i.DateTime);
        }

        void OnMoveItem(object param)
        {
            if (param is not IList<object> items || items.Count == 0)
                    return;

            foreach (var item in items.Cast<MediaItem>().ToList())
            {
                if (item.MoveItem())
                {
                    MediaList.Remove(item);
                }
            }
        }
        void OnMoveItemTo(object param)
        {
            if (param is not IList<object> items || items.Count == 0)
                    return;

            IDialogService dlgSvc = null;
            MessengerInstance.Send(new NotificationMessageAction<IDialogService>(
                "queryDialogService", p => { dlgSvc = p; }));

            if (dlgSvc == null)
            {
                Log.Print("Could not find dialog service!");
                return;
            }
            var settings = new FolderBrowserDialogSettings
            { 
                Description = "Select Target folder"
            };
            bool? success = dlgSvc.ShowFolderBrowserDialog(this, settings);
            if (success == null || success == false)
            {
                Log.Print("Target folder is not selected!");
                return;
            }
            Log.Print(settings.SelectedPath);
            foreach (var item in items.Cast<MediaItem>().ToList())
            {
                item.MoveItem(settings.SelectedPath, mitem => {
                    if (mitem != null)
                    { 
                        MediaList.Remove(mitem);
                    }
                });
            }
        }

        void OnDeleteItem(object param)
        {
            if (param is not IList<object> items || items.Count == 0)
                return;

            foreach (var item in items.Cast<MediaItem>().ToList())
            {
                if (item.DeleteItem())
                {
                    MediaList.Remove(item);
                }
            }
        }

        void OnClearDb(object param)
        {
            if (param is not IList<object> items || items.Count == 0)
                return;

            foreach (var item in items.Cast<MediaItem>().ToList())
            {
                item.ClearDb();
            }
        }

        void OnEditItem(object param)
        {
            if (param is MediaItem item && item.AvItem != null)
            {
                MessengerInstance.Send(new NotificationMessage<MediaItem>(
                    item, "editAv"));
            }
        }

        void OnDoubleClicked()
        {
            MessengerInstance.Send(new NotificationMessage<MediaItem>(
                SelectedMedia, "MediaItemDblClicked"));
        }

        void IterateMedia(string currDir, List<string> dbDirs)
        {
            try
            {
                var dirs = Directory.GetDirectories(currDir);
                if (dirs.Length == 0 || dirs[0].EndsWith(".actors"))
                {
                    if (dbDirs.BinarySearch(currDir) < 0)
                    {
                        InsertMediaAsync(currDir);
                    }
                }
                else 
                {
                    foreach (var dir in dirs)
                    {
                        IterateMedia(dir, dbDirs);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        void OnSearchOrphanageMedia()
        {
            UiServices.WaitCursor(true);
            ClearMedia();

            var currDir = _fileList.CurrDirInfo.FullName;
            var dbDirs = App.DbContext.Items
                .Where(i => EF.Functions.Like(i.Path, $"{currDir}%"))
                .Select(i => i.Path)
                .ToList();

            _serialQueue.Enqueue(() =>
            {
                dbDirs.Sort();
                IterateMedia(currDir, dbDirs);
                Log.Print("Search orphanage media done!");
            });
            UiServices.WaitCursor(false);
        }

        void OnSearchEmptyActor()
        { 
            UiServices.WaitCursor(true);
            var paths = App.DbContext.Items
                .Include(i => i.Actors)
                .Where(i => i.Actors.Count == 0)
                .Select(i => i.Path)
                .ToList();

            Replace(paths);
            UiServices.WaitCursor(false);
        }

        void OnContextMenu(MediaItem item, MediaListMenuType type)
        {
            if (item == null) return;
            var dir = Path.GetDirectoryName(item.MediaFile);
            if (type == MediaListMenuType.downloaded)
            {
                try
                {
                    var torrent = Path.GetFileName(item.Torrent);
                    File.Copy(item.Torrent, App.GConf["general"]["torrent_path"] + torrent);
                    File.Create($"{dir}\\.{type}").Dispose();
                    MediaList.Remove(item);
                    Log.Print($"Makrk downloaded {item.Torrent}");
                }
                catch (Exception ex)
                {
                    Log.Print(ex.Message, ex);
                }
            }
            else
            { 
                File.Create($"{dir}\\.{type}").Dispose();
                MediaList.Remove(item);
                Log.Print($"Mark excluded {item.Torrent}");
            }
        }
    }
}
