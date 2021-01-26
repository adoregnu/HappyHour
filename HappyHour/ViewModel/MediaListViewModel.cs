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

using GalaSoft.MvvmLight.Command;

using MvvmDialogs.FrameworkDialogs.FolderBrowser;

using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.Spider;
using HappyHour.Interfaces;
using HappyHour.View;

namespace HappyHour.ViewModel
{
    class MediaListViewModel : Pane, IMediaList
    {
        readonly object _lock = new object();

        MediaItem _selectedMedia = null;
        bool _isBrowsing = false;
        bool _sortByDateReleased = true;
        bool _sortByDateAdded = false;
        bool _searchSubFolder = false;
        IFileList _fileList;
        IEnumerable<SpiderBase> _spiderList;

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

        public IEnumerable<SpiderBase> SpiderList
        {
            get => _spiderList;
            set
            {
                IEnumerable<SpiderBase> list = null;
                if (value != null)
                    list = value.Where(l => l.Name != "sehuatang");
                Set(ref _spiderList, list);
            }
        }
        public ObservableCollection<MediaItem> MediaList { get; private set; }
        public ObservableCollection<MediaItem> SelectedMedias { get; private set; }
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

        public IMainView MainView { get; set; }
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
        public ICommand CmdMoveItemTo { get; set; }
        public ICommand CmdDeleteItem { get; set; }
        public ICommand CmdClearDb { get; set; }
        public ICommand CmdEditItem { get; set; }
        public ICommand CmdDoubleClick { get; set; }
        public ICommand CmdSearchOrphanageMedia { get; set; }
        public ICommand CmdSearchEmptyActor { get; set; }
        public ICommand CmdScrap { get; private set; }
        public ICommand CmdStopBatchingScrap { get; set; }

        public MediaListItemSelectedEventHandler ItemSelectedHandler { get; set; }
        public MediaListItemSelectedEventHandler ItemDoubleClickedHandler { get; set; }

        public MediaListViewModel()
        {
            Title = "AVList";
            MediaList = new ObservableCollection<MediaItem>();
            SelectedMedias = new ObservableCollection<MediaItem>();

            BindingOperations.EnableCollectionSynchronization(MediaList, _lock);

            CmdExclude = new RelayCommand<MediaItem>(
                p => { if (p != null) { p.Download(); MediaList.Remove(p); } });
            CmdDownload = new RelayCommand<MediaItem>(
                p => { if (p != null) { p.Exclude(); MediaList.Remove(p); } });
            CmdMoveItemTo = new RelayCommand<object>(
                p => OnMoveItemTo(p.ToList<MediaItem>()));
            CmdDeleteItem = new RelayCommand<object>(
                p => OnDeleteItem(p.ToList<MediaItem>()));
            CmdClearDb = new RelayCommand<object>(
                p => p.ToList<MediaItem>().ForEach(m => m.ClearDb()));
            CmdEditItem = new RelayCommand<object>(
                p => OnEditItem(p));
            CmdSearchOrphanageMedia = new RelayCommand(
                () => OnSearchOrphanageMedia());
            CmdSearchEmptyActor = new RelayCommand(
                () => OnSearchEmptyActor());
            CmdDoubleClick = new RelayCommand(() =>
                ItemDoubleClickedHandler?.Invoke(this, SelectedMedia));
            CmdScrap = new RelayCommand<object>(p => OnScrapAvInfo(p as SpiderBase));
            CmdStopBatchingScrap = new RelayCommand(
                () => _mitemsToSearch.Clear(),
                () => _mitemsToSearch != null);
        }

        void OnDirChanged(object sender, DirectoryInfo msg)
        {
            _searchSubFolder = false;
            RaisePropertyChanged(nameof(SearchSubFolder));
            RefreshMediaList(msg);
        }


        void OnDirModifed(object sender, FileSystemEventArgs e)
        {
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

        public void RemoveMedia(string path)
        {
            IsBrowsing = true;
            var medias = MediaList.Where(i => i.MediaFile.StartsWith(path,
                    StringComparison.CurrentCultureIgnoreCase)).ToList();
            medias.ForEach(x => MediaList.Remove(x));
            IsBrowsing = false; 
        }

        public void AddMedia(string path)
        {
            var item = MediaItem.Create(path);
            if (item == null) return;

            MediaList.InsertInPlace(item, i => i.DateTime);
        }

        void OnMoveItemTo(List<MediaItem> mitems)
        {
            var settings = new FolderBrowserDialogSettings
            {
                Description = "Select Target folder",
                SelectedPath = mitems[0].MediaPath
            };
            bool? success = MainView.DialogService.ShowFolderBrowserDialog(this, settings);
            if (success == null || success == false)
            {
                Log.Print("Target folder is not selected!");
                return;
            }
            Log.Print(settings.SelectedPath);
            foreach (var item in mitems)
            {
                item.MoveItem(settings.SelectedPath, mitem => {
                    if (mitem != null)
                    { 
                        MediaList.Remove(mitem);
                    }
                });
            }
        }

        void OnDeleteItem(List<MediaItem> mitems)
        {
            foreach (var item in mitems)
            {
                if (item.DeleteItem())
                {
                    MediaList.Remove(item);
                }
            }
        }

        void OnEditItem(object param)
        {
            if (param is not MediaItem item)
                return;

            var dialog = new AvEditorViewModel(item);
            MainView.DialogService.Show<AvEditorDialog>(this, dialog);
        }

        void UpdateMediaList(string path, CancellationToken token,
            bool bRecursive = false, int level = 0)
        {
            if (token.IsCancellationRequested)
                return;

            try
            {
                var dirs = Directory.GetDirectories(path);
                if (dirs.Length == 0 || dirs[0].EndsWith(".actors"))
                {
                    AddMedia(path);
                }
                else if (bRecursive || level < 1)
                {
                    foreach (var dir in dirs)
                    {
                        UpdateMediaList(dir, token, bRecursive, level + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        void IterateMedia(string currDir, List<string> dbDirs, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;
            try
            {
                var dirs = Directory.GetDirectories(currDir);
                if (dirs.Length == 0 || dirs[0].EndsWith(".actors"))
                {
                    if (dbDirs.BinarySearch(currDir) < 0)
                    {
                        AddMedia(currDir);
                    }
                }
                else 
                {
                    foreach (var dir in dirs)
                    {
                        IterateMedia(dir, dbDirs, token);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        Task _runningTask = null;
        CancellationTokenSource _tokenSource;

        void CancelTaskIfRunning()
        { 
            if (_runningTask != null && !_runningTask.IsCompleted)
            {
                _tokenSource.Cancel();
                _runningTask.Wait();
            }
        }

        async void SortMedia()
        {
            CancelTaskIfRunning();
            var tmp = MediaList.ToList();
            MediaList.Clear();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            _runningTask = Task.Run(() => {
                foreach (var m in tmp)
                {
                    if (token.IsCancellationRequested) break;
                    MediaList.InsertInPlace(m, i => i.DateTime);
                }
            }, token);
            await _runningTask;
        }

        async void RefreshMediaList(DirectoryInfo msg)
        {
            CancelTaskIfRunning();
            MediaList.Clear();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            bool bSubFolder = _searchSubFolder;
            _runningTask = Task.Run(
                () => UpdateMediaList(msg.FullName, token, bSubFolder), token);
            await _runningTask;
        }

        public async void Replace(IEnumerable<string> paths)
        {
            CancelTaskIfRunning();
            MediaList.Clear();
            IsSelected = true;

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            _runningTask = Task.Run(() => {
                foreach (var path in paths)
                {
                    if (token.IsCancellationRequested)
                        break;
                    AddMedia(path);
                }
            }, token);
            await _runningTask;
        }

        async void OnSearchOrphanageMedia()
        {
            CancelTaskIfRunning();

            MediaList.Clear();

            var currDir = _fileList.CurrDirInfo.FullName;
            var dbDirs = await App.DbContext.Items
                .Where(i => EF.Functions.Like(i.Path, $"{currDir}%"))
                .Select(i => i.Path)
                .ToListAsync();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            _runningTask = Task.Run(() =>
            {
                dbDirs.Sort();
                IterateMedia(currDir, dbDirs, token);
                Log.Print("Search orphanage media done!");
            }, token);
            await _runningTask;
        }

        async void OnSearchEmptyActor()
        {
            CancelTaskIfRunning();

            MediaList.Clear();

            var paths = await App.DbContext.Items
                .Include(i => i.Actors)
                .Where(i => i.Actors.Count == 0)
                .Select(i => i.Path)
                .ToListAsync();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            _runningTask = Task.Run(() => {
                foreach (var p in paths)
                {
                    if (token.IsCancellationRequested)
                        break;
                    AddMedia(p);
                }
            }, token);
            await _runningTask;
        }

        List<MediaItem> _mitemsToSearch;
        void OnScrapCompleted(SpiderBase spider)
        {
            Log.Print($"{_mitemsToSearch[0].Pid} Scrap completed ");
            spider.ScrapCompleted -= OnScrapCompleted;

            if (_mitemsToSearch.Count > 0)
            {
                _mitemsToSearch[0].ReloadAvItem();
                _mitemsToSearch.RemoveAt(0);
            }
            OnScrapAvInfo(spider);
        }

        void OnScrapAvInfo(SpiderBase spider)
        {
            if (_mitemsToSearch == null)
                _mitemsToSearch = SelectedMedias.ToList();

            if (_mitemsToSearch.Count > 0)
            {
                MainView.StatusMessage = $"{_mitemsToSearch.Count} remained";
                spider.ScrapCompleted += OnScrapCompleted;
                spider.Keyword = _mitemsToSearch[0].Pid;
                spider.DataPath = _mitemsToSearch[0].MediaPath;
                spider.Navigate2();
            }
            else
            {
                MainView.StatusMessage = "";
                _mitemsToSearch = null;
                spider.Reset();
            }
        }
     }
}
