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
using System.Windows.Data;

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
using HappyHour.View;

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
        readonly object _lock = new object();

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
                if (value != null && _selectedMedia != value)
                {
                    ItemSelectedHandler?.Invoke(this, value);
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
        public IDialogService DialogService { get; set; }

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

        public MediaListItemSelectedEventHandler ItemSelectedHandler { get; set; }
        public MediaListItemSelectedEventHandler ItemDoubleClickedHandler { get; set; }

        public MediaListViewModel()
        {
            Title = "AVList";
            MediaList = new ObservableCollection<MediaItem>();

            BindingOperations.EnableCollectionSynchronization(MediaList, _lock);

            CmdExclude = new RelayCommand<MediaItem>(
                p => OnContextMenu(p, MediaListMenuType.excluded));
            CmdDownload = new RelayCommand<MediaItem>(
                p => OnContextMenu(p, MediaListMenuType.downloaded));
            CmdMoveItem = new RelayCommand<object>(p => OnMoveItem(p));
            CmdMoveItemTo = new RelayCommand<object>(p => OnMoveItemTo(p));
            CmdDeleteItem = new RelayCommand<object>(p => OnDeleteItem(p));
            CmdClearDb = new RelayCommand<object>(p => OnClearDb(p));
            CmdEditItem = new RelayCommand<object>(p => OnEditItem(p));
            CmdSearchOrphanageMedia = new RelayCommand(() => OnSearchOrphanageMedia());
            CmdSearchEmptyActor = new RelayCommand(() => OnSearchEmptyActor());
            CmdDoubleClick = new RelayCommand(() =>
                ItemDoubleClickedHandler?.Invoke(this, SelectedMedia));

            MessengerInstance.Register<NotificationMessage<SpiderEnum>>(this,
                (msg) => SpiderList = msg.Content.Where(i => i.Name != "sehuatang"));
        }

        void OnDirChanged(object sender, DirectoryInfo msg)
        {
            _searchSubFolder = false;
            RaisePropertyChanged(nameof(SearchSubFolder));
            RefreshMediaList(msg);
        }

        async void RefreshMediaList(DirectoryInfo msg)
        { 
            MediaList.Clear();

            bool bSubFolder = _searchSubFolder;
            await Task.Run(() => UpdateMediaList(msg.FullName, bSubFolder));
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
            MediaList.Clear();

            foreach (var m in tmp)
            {
                MediaList.InsertInPlace(m, i => i.DateTime);
            }
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

        void InsertMedia(string path)
        {
            var item = MediaItem.Create(path);
            if (item == null) return;

            MediaList.InsertInPlace(item, i => i.DateTime);
        }

        public async void Replace(IEnumerable<string> paths)
        {
            MediaList.Clear();
            await Task.Run(() => {
                foreach (var path in paths)
                {
                    InsertMedia(path);
                }
            });
        }

        void UpdateMediaList(string path, bool bRecursive = false, int level = 0)
        {
            try
            {
                var dirs = Directory.GetDirectories(path);
                if (dirs.Length == 0 || dirs[0].EndsWith(".actors"))
                {
                    InsertMedia(path);
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

            var settings = new FolderBrowserDialogSettings
            { 
                Description = "Select Target folder"
            };
            bool? success = DialogService.ShowFolderBrowserDialog(this, settings);
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
                var dialog = new AvEditorViewModel(item);
                DialogService.ShowDialog<AvEditorDialog>(this, dialog);
            }
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
                        InsertMedia(currDir);
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

        async void OnSearchOrphanageMedia()
        {
            MediaList.Clear();

            var currDir = _fileList.CurrDirInfo.FullName;
            var dbDirs = await App.DbContext.Items
                .Where(i => EF.Functions.Like(i.Path, $"{currDir}%"))
                .Select(i => i.Path)
                .ToListAsync();

            await Task.Run(() =>
            {
                dbDirs.Sort();
                IterateMedia(currDir, dbDirs);
                Log.Print("Search orphanage media done!");
            });
        }

        async void OnSearchEmptyActor()
        {
            MediaList.Clear();

            var paths = await App.DbContext.Items
                .Include(i => i.Actors)
                .Where(i => i.Actors.Count == 0)
                .Select(i => i.Path)
                .ToListAsync();

            await Task.Run(() => paths.ForEach(p => InsertMedia(p)));
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
