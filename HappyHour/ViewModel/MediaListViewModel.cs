using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
using System.Diagnostics;

namespace HappyHour.ViewModel
{
    internal class MediaListViewModel : Pane, IMediaList
    {
        private readonly object _lock = new();

        private MediaItem _selectedMedia;
        private bool _isBrowsing;
        private bool _sortByDateReleased = true;
        private bool _sortByDateAdded;
        private bool _searchSubFolder;
        private bool _forceStopScrapping;
        private IFileList _fileList;
        private IEnumerable<SpiderBase> _spiderList;

        public MediaItem SelectedMedia
        {
            get => _selectedMedia;
            set
            {
                ItemSelectedHandler?.Invoke(this, value);
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
                {
                    list = value.Where(l => l.Name != "sehuatang");
                }
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
                    _fileList.FileSelected += OnFileSelected;
                }
            }
        }
        public ICommand CmdExternalPlayer { get; set; }
        public ICommand CmdCopyPath { get; set; }
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

        public MediaListItemSelected ItemSelectedHandler { get; set; }
        public MediaListItemSelected ItemDoubleClickedHandler { get; set; }

        public MediaListViewModel()
        {
            Title = "AVList";
            MediaList = new ObservableCollection<MediaItem>();
            SelectedMedias = new ObservableCollection<MediaItem>();

            BindingOperations.EnableCollectionSynchronization(MediaList, _lock);

            CmdExternalPlayer = new RelayCommand<MediaItem>(p =>
            {
                if (p != null)
                {
                    new Process
                    {
                        StartInfo = new ProcessStartInfo(p.MediaFile)
                        {
                            UseShellExecute = true
                        }
                    }.Start();
                }
            });
            CmdCopyPath = new RelayCommand<MediaItem>(
                p => Clipboard.SetText(p.MediaPath));

            CmdExclude = new RelayCommand<MediaItem>(
                p => { if (p != null) { p.Exclude(); MediaList.Remove(p); } });
            CmdDownload = new RelayCommand<MediaItem>(
                p => { if (p != null) { p.Download(); MediaList.Remove(p); } });
            CmdMoveItemTo = new RelayCommand<object>(p => OnMoveItemTo(p.ToList<MediaItem>()));
            CmdDeleteItem = new RelayCommand<object>(p => OnDeleteItem(p.ToList<MediaItem>()));
            CmdClearDb = new RelayCommand<object>(p => p.ToList<MediaItem>().ForEach(m => m.ClearDb()));
            CmdEditItem = new RelayCommand<object>(p => OnEditItem(p));
            CmdSearchOrphanageMedia = new RelayCommand(() => OnSearchOrphanageMedia());
            CmdSearchEmptyActor = new RelayCommand( () => OnSearchEmptyActor());
            CmdDoubleClick = new RelayCommand(() => ItemDoubleClickedHandler?.Invoke(this, SelectedMedia));
            CmdScrap = new RelayCommand<object>(
                p => OnScrapAvInfo(p as SpiderBase),
                p => _mitemsToSearch == null);
            CmdStopBatchingScrap = new RelayCommand(
                () => _forceStopScrapping = true,
                () => _mitemsToSearch != null);
        }

        private void OnDirChanged(object sender, DirectoryInfo msg)
        {
            _searchSubFolder = false;
            RaisePropertyChanged(nameof(SearchSubFolder));
            RefreshMediaList(msg);
        }

        private void OnDirModifed(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Deleted:
                    {
                        MediaItem media = MediaList.FirstOrDefault(m => m.Pid == e.Name);
                        if (media != null)
                        {
                            _ = MediaList.Remove(media);
                        }
                    }
                    break;
                case WatcherChangeTypes.Renamed:
                    {
                        RenamedEventArgs re = e as RenamedEventArgs;
                        MediaItem media = MediaList.FirstOrDefault(m => m.Pid == re.OldName);
                        if (media != null)
                        {
                            _ = MediaList.Remove(media);
                        }
                    }
                    break;
            }
        }

        private void OnFileSelected(object sender, FileSystemInfo e)
        {
            MediaItem media = MediaList.FirstOrDefault(m => m.MediaPath == e.FullName);
            if (media != null)
            {
                SelectedMedia = media;
            }
        }

        public void RemoveMedia(string path)
        {
            IsBrowsing = true;
            List<MediaItem> medias = MediaList.Where(i => i.MediaFile.StartsWith(path,
                    StringComparison.CurrentCultureIgnoreCase)).ToList();
            medias.ForEach(x => MediaList.Remove(x));
            IsBrowsing = false;
        }

        public void AddMedia(string path)
        {
            MediaItem media = MediaList.FirstOrDefault(m => m.MediaPath == path);
            if (media == null)
            {
                MediaItem item = MediaItem.Create(path);
                if (item != null)
                {
                    MediaList.InsertInPlace(item, i => i.DateTime);
                }
            }
            else
            {
                media.ReloadAvItem();
            }
        }

        private void OnMoveItemTo(List<MediaItem> mitems)
        {
            FolderBrowserDialogSettings settings = new()
            {
                Description = "Select Target folder",
                SelectedPath = mitems[0].MediaPath
            };
            bool? success = MainView.DialogService.ShowFolderBrowserDialog(this, settings);
            if (success is null or false)
            {
                Log.Print("Target folder is not selected!");
                return;
            }
            Log.Print(settings.SelectedPath);
            foreach (MediaItem item in mitems)
            {
                item.MoveItem(settings.SelectedPath, mitem =>
                {
                    if (mitem != null)
                    {
                        MediaList.Remove(mitem);
                    }
                });
            }
        }

        private void OnDeleteItem(List<MediaItem> mitems)
        {
            foreach (MediaItem item in mitems)
            {
                if (item.DeleteItem())
                {
                    MediaList.Remove(item);
                }
            }
        }

        private void OnEditItem(object param)
        {
            if (param is not MediaItem item)
            {
                return;
            }

            AvEditorViewModel dialog = new(item);
            MainView.DialogService.Show<AvEditorDialog>(this, dialog);
        }

        private void UpdateMediaList(string path, CancellationToken token,
            bool bRecursive = false, int level = 0)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            try
            {
                string[] dirs = Directory.GetDirectories(path);
                if (dirs.Length == 0 || dirs[0].EndsWith(".actors", StringComparison.OrdinalIgnoreCase))
                {
                    AddMedia(path);
                }
                else if (bRecursive || level < 1)
                {
                    foreach (string dir in dirs)
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

        private void IterateMedia(string currDir, List<string> dbDirs, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
            try
            {
                string[] dirs = Directory.GetDirectories(currDir);
                if (dirs.Length == 0 || dirs[0].EndsWith(".actors", StringComparison.OrdinalIgnoreCase))
                {
                    if (dbDirs.BinarySearch(currDir) < 0)
                    {
                        AddMedia(currDir);
                    }
                }
                else
                {
                    foreach (string dir in dirs)
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

        private Task _runningTask;
        private CancellationTokenSource _tokenSource;

        private void CancelTaskIfRunning()
        {
            if (_runningTask != null && !_runningTask.IsCompleted)
            {
                _tokenSource.Cancel();
                _runningTask.Wait();
            }
        }

        private async void SortMedia()
        {
            CancelTaskIfRunning();
            List<MediaItem> tmp = MediaList.ToList();
            MediaList.Clear();

            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;
            _runningTask = Task.Run(() =>
            {
                foreach (MediaItem m in tmp)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    MediaList.InsertInPlace(m, i => i.DateTime);
                }
            }, token);
            await _runningTask;
        }

        private async void RefreshMediaList(DirectoryInfo msg)
        {
            CancelTaskIfRunning();
            MediaList.Clear();

            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;
            bool bSubFolder = _searchSubFolder;
            _runningTask = Task.Run(() => UpdateMediaList(msg.FullName, token, bSubFolder), token);
            await _runningTask;
        }

        public async void Replace(IEnumerable<string> paths)
        {
            CancelTaskIfRunning();
            MediaList.Clear();
            IsSelected = true;

            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            _runningTask = Task.Run(() =>
            {
                foreach (string path in paths)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    AddMedia(path);
                }
            }, token);
            await _runningTask;
        }

        private async void OnSearchOrphanageMedia()
        {
            CancelTaskIfRunning();

            MediaList.Clear();

            string currDir = _fileList.CurrDirInfo.FullName;
            List<string> dbDirs = await App.DbContext.Items
                .Where(i => EF.Functions.Like(i.Path, $"{currDir}%"))
                .Select(i => i.Path)
                .ToListAsync();

            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            _runningTask = Task.Run(() =>
            {
                dbDirs.Sort();
                IterateMedia(currDir, dbDirs, token);
                Log.Print("Search orphanage media done!");
            }, token);
            await _runningTask;
        }

        private async void OnSearchEmptyActor()
        {
            CancelTaskIfRunning();

            MediaList.Clear();

            List<string> paths = await App.DbContext.Items
                .Include(i => i.Actors)
                .Where(i => i.Actors.Count == 0)
                .Select(i => i.Path)
                .ToListAsync();

            _tokenSource = new CancellationTokenSource();
            CancellationToken token = _tokenSource.Token;

            _runningTask = Task.Run(() =>
            {
                foreach (string p in paths)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    AddMedia(p);
                }
            }, token);
            await _runningTask;
        }

        private List<MediaItem> _mitemsToSearch;
        private void OnScrapCompleted(SpiderBase spider)
        {
            if (_mitemsToSearch.Count > 0)
            {
                _mitemsToSearch.RemoveAt(0);
            }
            OnScrapAvInfo(spider);
        }

        private void OnScrapAvInfo(SpiderBase spider)
        {
            if (_mitemsToSearch == null)
            {
                _forceStopScrapping = false;
                _mitemsToSearch = SelectedMedias.ToList();
                spider.ScrapCompleted += OnScrapCompleted;
            }

            if (!_forceStopScrapping && _mitemsToSearch.Count > 0)
            {
                MainView.StatusMessage = $"{_mitemsToSearch.Count} remained";
                spider.Navigate2(_mitemsToSearch[0]);
            }
            else
            {
                spider.ScrapCompleted -= OnScrapCompleted;
                MainView.StatusMessage = "";
                _mitemsToSearch = null;
            }
        }
    }
}
