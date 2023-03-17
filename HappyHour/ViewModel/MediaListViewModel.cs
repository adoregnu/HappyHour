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

//using GalaSoft.MvvmLight.Command;

using MvvmDialogs.FrameworkDialogs.FolderBrowser;

using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.Spider;
using HappyHour.Interfaces;
using HappyHour.View;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace HappyHour.ViewModel
{
    internal class MediaListViewModel : Pane, IMediaList
    {
        private readonly object _lock = new();

        private bool _isBrowsing;
        private bool _sortByDateReleased = true;
        private bool _sortByDateAdded;
        private bool _searchSubFolder;
        private bool _forceStopScrapping;

        private IFileList _fileList;
        private IAvMedia _selectedMedia;
        private IEnumerable<SpiderBase> _spiderList;
        private List<IAvMedia> _mitemsToSearch;

        public IAvMedia SelectedMedia
        {
            get => _selectedMedia;
            set
            {
                if (value != null)
                {
                    ItemSelectedHandler?.Invoke(this, value);
                }
                _ = SetProperty(ref _selectedMedia, value);
            }
        }
        public bool IsBrowsing
        {
            get => _isBrowsing;
            set => SetProperty(ref _isBrowsing, value);
        }

        public IEnumerable<SpiderBase> SpiderList
        {
            get => _spiderList;
            set
            {
                IEnumerable<SpiderBase> list = null;
                if (value != null)
                {
                    list = value.Where(l => l.Name != "sehuatang" && l.Name != "Sukebei");
                }
                _ = SetProperty(ref _spiderList, list);
            }
        }
        public ObservableCollection<IAvMedia> MediaList { get; private set; }
        public ObservableCollection<IAvMedia> SelectedMedias { get; private set; }
        public bool SearchSubFolder
        {
            get => _searchSubFolder;
            set
            {
                _ = SetProperty(ref _searchSubFolder, value);
                RefreshMediaList(_fileList.CurrDirInfo);
            }
        }
        public bool SorByDateReleased
        {
            get => _sortByDateReleased;
            set
            {
                _ = SetProperty(ref _sortByDateReleased, value);
                if (value)
                {
                    AvMovie.DateType = DateType.Released;
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
                _ = SetProperty(ref _sortByDateAdded, value);
                if (value)
                {
                    AvMovie.DateType = DateType.Added;
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
        public ICommand CmdDownloadTorrent { get; set; }
        public ICommand CmdDownloadMagnet { get; set; }
        public ICommand CmdMoveItemTo { get; set; }
        public ICommand CmdDeleteItem { get; set; }
        public ICommand CmdClearDb { get; set; }
        public ICommand CmdEditItem { get; set; }
        public ICommand CmdDoubleClick { get; set; }
        public ICommand CmdSearchOrphanageMedia { get; set; }
        public ICommand CmdSearchEmptyActor { get; set; }
        public ICommand CmdScrap { get; private set; }
        public ICommand CmdStopBatchingScrap { get; set; }
        public ICommand CmdNewMovie { get; set; }
        public MediaListItemSelected ItemSelectedHandler { get; set; }
        public MediaListItemSelected ItemDoubleClickedHandler { get; set; }

        public MediaListViewModel()
        {
            Title = "AVList";
            MediaList = new ObservableCollection<IAvMedia>();
            SelectedMedias = new ObservableCollection<IAvMedia>();

            BindingOperations.EnableCollectionSynchronization(MediaList, _lock);

            CmdNewMovie = new RelayCommand(() => LastUpdatedMovies());
            CmdExternalPlayer = new RelayCommand<AvMovie>(p => PlayMedia(p));
            CmdExclude = new RelayCommand<AvTorrent>(p => ExcludeFromList(p));
            CmdDownloadTorrent = new RelayCommand<AvTorrent>(p => DownloadMedia(p, "torrent"));
            CmdDownloadMagnet = new RelayCommand<AvTorrent>(p => DownloadMedia(p,  "magnet"));
            CmdCopyPath = new RelayCommand<IAvMedia>(p => Clipboard.SetText(p.Path));
            CmdMoveItemTo = new RelayCommand<object>(p => MoveTo(p.ToList<AvMovie>()));
            CmdDeleteItem = new RelayCommand<object>(p => Delete(p.ToList<AvMovie>()));
            CmdClearDb = new RelayCommand<object>(p => ClearDb(p.ToList<AvMovie>()));
            CmdEditItem = new RelayCommand<object>(p => EditMovieInfo(p));
            CmdSearchOrphanageMedia = new RelayCommand(() => SearchOrphanage());
            CmdSearchEmptyActor = new RelayCommand(() => OnSearchEmptyActor());
            CmdDoubleClick = new RelayCommand(() =>
            {
                if (ItemDoubleClickedHandler != null)
                {
                    ItemDoubleClickedHandler.Invoke(this, SelectedMedia);
                }
                else
                {
                    PlayMedia(SelectedMedia as AvMovie);
                }
            });
            CmdScrap = new RelayCommand<object>(
                p => OnScrapAvInfo(p as SpiderBase),
                p => _mitemsToSearch == null);
            CmdStopBatchingScrap = new RelayCommand(
                () => _forceStopScrapping = true,
                () => _mitemsToSearch != null);
        }

        private static void PlayMedia(AvMovie media)
        {
            if (media != null)
            {
                _ = new Process
                {
                    StartInfo = new ProcessStartInfo(media.Files[0])
                    {
                        UseShellExecute = true
                    }
                }.Start();
            }
        }

        private void ExcludeFromList(AvTorrent media)
        {
            if (media != null)
            {
                media.Exclude();
                _ = MediaList.Remove(media);
            }
        }

        private void DownloadMedia(AvTorrent media, string site)
        {
            if (media != null)
            {
                media.Download(site);
                _ = MediaList.Remove(media);
            }
        }

        private static void ClearDb(List<AvMovie> list)
        {
            if (list != null)
            {
                list.ForEach(m => m.ClearDb());
            }
        }

        private void OnDirChanged(object sender, DirectoryInfo msg)
        {
            _searchSubFolder = false;
            OnPropertyChanged(nameof(SearchSubFolder));
            RefreshMediaList(msg);
        }

        private void OnDirModifed(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Deleted:
                    {
                        var media = MediaList.FirstOrDefault(m => m.Pid == e.Name);
                        if (media != null)
                        {
                            _ = MediaList.Remove(media);
                        }
                    }
                    break;
                case WatcherChangeTypes.Renamed:
                    {
                        var re = e as RenamedEventArgs;
                        var media = MediaList.FirstOrDefault(m => m.Pid == re.OldName);
                        if (media != null)
                        {
                            _ = MediaList.Remove(media);
                        }
                    }
                    break;
                case WatcherChangeTypes.Created:
                    break;
                case WatcherChangeTypes.Changed:
                    break;
                case WatcherChangeTypes.All:
                    break;
                default:
                    break;
            }
        }

        private void OnFileSelected(object sender, FileSystemInfo e)
        {
            var media = MediaList.FirstOrDefault(m => m.Path == e.FullName);
            if (media != null)
            {
                SelectedMedia = media;
            }
        }

        public void AddMedia(string path)
        {
            var media = MediaList.FirstOrDefault(m => m.Path == path);
            if (media == null)
            {
                var item = AvMediaBase.Create(path);
                if (item != null)
                {
                    MediaList.AddInOrder(item, i => i);
                }
            }
            else
            {
                media.Reload();
            }
        }

        private void MoveTo(List<AvMovie> mitems)
        {
            FolderBrowserDialogSettings settings = new()
            {
                Description = "Select Target folder",
                SelectedPath = mitems[0].Path
            };
            bool? success = MainView.DialogService.ShowFolderBrowserDialog(this, settings);
            if (success is null or false)
            {
                Log.Print("Target folder is not selected!");
                return;
            }
            Log.Print($"Move selected movie to {settings.SelectedPath}");
            foreach (var item in mitems)
            {
                item.Move(settings.SelectedPath, m =>
                {
                    if (m != null)
                    {
                        _ = MediaList.Remove(m);
                    }
                });
            }
        }

        private void Delete(List<AvMovie> mitems)
        {
            foreach (var item in mitems)
            {
                if (item.Delete())
                {
                    _ = MediaList.Remove(item);
                }
            }
        }

        private void EditMovieInfo(object param)
        {
            if (param is not AvMovie item)
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
                if (dirs.Length == 0 ||
                    dirs.Any(dir => dir.Contains("actors") || dir.Contains("sukebei")))
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
                Log.Print($"UpdateMediaList: {ex.Message}");
            }
        }

        private void IterateMedia(string currDir, List<string> dbDirs, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            if (currDir.Contains("Western"))
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
                        string pid = currDir.Split('\\').Last();
                        var item = App.DbContext.Items.FirstOrDefault(it => it.Pid == pid);
                        if (item != null)
                        {
                            item.Path = currDir;
                            App.DbContext.SaveChanges();
                        }
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
                Log.Print("IterateMedia: " + ex.Message);
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
            var tmp = MediaList.ToList();
            MediaList.Clear();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            _runningTask = Task.Run(() =>
            {
                foreach (var m in tmp)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    MediaList.AddInOrder(m, i => i);
                }
            }, token);
            await _runningTask;
        }

        private async void RefreshMediaList(DirectoryInfo msg)
        {
            CancelTaskIfRunning();
            MediaList.Clear();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            bool bSubFolder = _searchSubFolder;
            _runningTask = Task.Run(() => UpdateMediaList(msg.FullName, token, bSubFolder), token);
            await _runningTask;
        }

        private async void SearchOrphanage()
        {
            CancelTaskIfRunning();
            MediaList.Clear();

            string currDir = _fileList.CurrDirInfo.FullName;
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

        public async void LoadItems(List<AvItem> movies)
        {
            CancelTaskIfRunning();
            MediaList.Clear();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            _runningTask = Task.Run(() =>
            {
                foreach (var movie in movies)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    if (Directory.Exists(movie.Path))
                    {
                        AddMedia(movie.Path);
                    }
                    else
                    {
                        App.DbContext.Items.Remove(movie);
                    }
                }
            }, token);

            await _runningTask;
        }

        private async void OnSearchEmptyActor()
        {
            var movies = await App.DbContext.Items
                .Include(i => i.Actors)
                .Where(i => i.Actors.Count == 0)
                //.Select(i => i.Path)
                .ToListAsync();

            LoadItems(movies);
        }

        private async void LastUpdatedMovies()
        {
            var movies = await App.DbContext.Items
                .OrderByDescending(i => i.DateAdded)
                .Take(20).ToListAsync();

            LoadItems(movies);
        }

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
