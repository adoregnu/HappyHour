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
using System.Diagnostics;


using MvvmDialogs.FrameworkDialogs.FolderBrowser;
using MvvmDialogs.FrameworkDialogs;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AsyncAwaitBestPractices.MVVM;

using HappyHour.Extension;
using HappyHour.Model;
using HappyHour.Spider;
using HappyHour.Interfaces;
using HappyHour.View;
using HappyHour.Utilities;
using System.Drawing.Printing;

namespace HappyHour.ViewModel
{
    class PathComparer : IComparer<string>
    {
        public int Compare(string left, string right)
        {
            var ret = left.CompareTo(right);
            Log.Print($"{left} , {right}, {ret}");
            return ret;
        }
    }
    internal class MediaListViewModel : Pane, IMediaList
    {
        private readonly object _lock = new();

        private bool _isBrowsing;
        private bool _searchSubFolder;
        private bool _forceStopScrapping;

        private IFileList _fileList;
        private IAvMedia _selectedMedia;
        private IEnumerable<SpiderBase> _spiderList;
        private List<IAvMedia> _mediasToSearch;

        private readonly MovieDbContext _db = App.Current.DbContext;

        public EventHandler<ViewEventArgs> ViewEventHandler { get; set; }

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

        public SortType SelectedSortType
        {
            get => AvMediaBase.Sort; 
            set
            {
                SetProperty(ref AvMediaBase.Sort, value);
                if (value == SortType.Rating) 
                {
                    RatingSitesVisibility = Visibility.Visible;
                }
                else
                {
                    RatingSitesVisibility = Visibility.Collapsed;
                    SortMedia();
                }
            }
        }
        private List<string> _ratingSites = ["javlibrary", "avdbs" ];
        public List<string> RatingSites
        {
            get => _ratingSites;
            set
            {
                SetProperty(ref _ratingSites, value);
            }
        }
        private string  _selectedRatingSite;
        public string SelectedRatingSite
        {
            get => _selectedRatingSite;
            set
            {
                SetProperty(ref _selectedRatingSite, value);
                AvMovie.SpiderName = value;
                SortMedia();
            }
        }
        private Visibility _ratingSitesVisibility = Visibility.Collapsed;
        public Visibility RatingSitesVisibility
        {
            get => _ratingSitesVisibility;
            set => SetProperty(ref _ratingSitesVisibility, value);
        }

        private List<string> _targetDirs = [];
        public List<string> TargetDirs
        {
            get => _targetDirs;
            set => SetProperty(ref _targetDirs, value);
        }

        public int ScrapDelay { get; set; } = 1000;
        public ISpiderManager SpiderManager { get; set; }

        public ICommand CmdUpdateTargetDirs { get; set; }
        public ICommand CmdExternalPlayer { get; set; }
        public ICommand CmdCopyPath { get; set; }
        public ICommand CmdExclude { get; set; }
        public ICommand CmdDownload { get; set; }
        public IAsyncCommand<string> CmdMove { get; set; }
        public IAsyncCommand CmdMoveWithDialog { get; set; }
        public IAsyncCommand<object> CmdDeleteItem { get; set; }
        public IAsyncCommand<object> CmdClearDb { get; set; }
        public IAsyncCommand<object> CmdEditItem { get; set; }
        public ICommand CmdDoubleClick { get; set; }
        public IAsyncCommand CmdSearchOrphanageMedia { get; set; }
        public IAsyncCommand CmdSearchEmptyActor { get; set; }
        public ICommand CmdScrap { get; private set; }
        public IAsyncCommand CmdTorrents { get; private set; }
        public ICommand CmdStopBatchingScrap { get; set; }
        public IAsyncCommand CmdShowLastUpdated { get; set; }
        public MediaListItemSelected ItemSelectedHandler { get; set; }
        public MediaListItemSelected ItemDoubleClickedHandler { get; set; }

        public MediaListViewModel(IMainView mainView) : base(mainView)
        {
            Title = "AVList";
            MediaList = [];
            SelectedMedias = [];

            BindingOperations.EnableCollectionSynchronization(MediaList, _lock);

            CmdShowLastUpdated = new AsyncCommand(LastUpdatedMovies);
            CmdExternalPlayer = new RelayCommand<AvMovie>(PlayMedia);
            CmdExclude = new RelayCommand<AvTorrent>(ExcludeFromList);
            CmdDownload = new RelayCommand<AvTorrent>(DownloadMedia);
            CmdCopyPath = new RelayCommand<IAvMedia>(p => Clipboard.SetText(p.Path));
            CmdMove = new AsyncCommand<string>(Move);
            CmdMoveWithDialog = new AsyncCommand(MoveWithDialog);
            CmdDeleteItem = new AsyncCommand<object>(Delete);
            CmdClearDb = new AsyncCommand<object>(ClearDb);
            CmdEditItem = new AsyncCommand<object>(EditMovieInfo);
            CmdSearchOrphanageMedia = new AsyncCommand(SearchOrphanage);
            CmdSearchEmptyActor = new AsyncCommand(OnSearchEmptyActor);
            CmdTorrents = new AsyncCommand(OnTorrents);
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
                p => _mediasToSearch == null);
            CmdStopBatchingScrap = new RelayCommand(
                () => _forceStopScrapping = true);
            CmdUpdateTargetDirs = new RelayCommand(UpdateTargetDirectories);
        }

        private List<string> GetUniquePidPrefix()
        {
            List<string> uniquPidPrefix = [];
            var selectedItems = SelectedMedias?.Cast<AvMovie>().ToList();
            foreach (var item in selectedItems)
            {
                //split item.Pid by '-' and get all part except last part
                string prefix = string.Join('-', item.Pid.Split('-')[..^1]);
                if (!uniquPidPrefix.Contains(prefix))
                {
                    uniquPidPrefix.Add(prefix);
                }
            }
            return uniquPidPrefix;
        }

        private List<Maker> GetUniqueMaker()
        {
            List<Maker> uniqueMakers = [];
            foreach (var item in SelectedMedias?.Cast<AvMovie>().ToList())
            {
                if (item.MovieInfo != null && item.MovieInfo.Maker != null)
                {
                    var maker = item.MovieInfo.Maker;
                    if (!uniqueMakers.Contains(maker))
                    {
                        uniqueMakers.Add(maker);
                        Log.Print($"{maker}");
                    }
                }
            }
            return uniqueMakers;
        }

        public void UpdateTargetDirectories()
        {
            TargetDirs = [];
            try
            {
                var selectedItems = SelectedMedias?.Cast<AvMovie>().ToList();
                if (selectedItems?.Any() == true)
                {
                    List<string> targetDirs = [];
                    // currentDir is Path field without last path component of any selected item
                    var current = string.Join("\\", selectedItems[0].Path.Split('\\')[..^1]);
                    foreach (var uniq in GetUniquePidPrefix())
                    {
                        var folders = _db.GetFoldersStartsWithPid(uniq, current);
                        targetDirs.AddRange(folders);
                    }

                    if (targetDirs.Count == 0)
                    {
                        foreach (var maker in GetUniqueMaker())
                        {
                            var folders = _db.GetFoldersByMaker(maker, current);

                            targetDirs.AddRange(folders);
                        }
                    }

                    TargetDirs = targetDirs;
                }
            }
            catch (Exception ex)
            {
                Log.Print($"UpdateTargetDirectories 오류: {ex.Message}");
            }
        }

        private async Task Move(string targetDir)
        {
            List<string> sourceItems = [];
            foreach (var item in SelectedMedias?.Cast<AvMovie>().ToList())
            {
                sourceItems.Add(item.Path);
            }
            await FileCopyUtility.MoveItemsWithProgressAsync(sourceItems, targetDir, true, true, MainView.Window);
            //await FileCopyUtility.MoveFolderWithRoboSharpAsync(sourceItems[0], targetDir, true, true, null);
        }

        private async Task MoveWithDialog()
        {
            var settings = new FolderBrowserDialogSettings
            {
                Description = "Select target folder to move files",
                ShowNewFolderButton = true
            };

            var success = MainView.DialogService.ShowFolderBrowserDialog(this, settings);
            if (success == true && !string.IsNullOrEmpty(settings.SelectedPath))
            {
                await Move(settings.SelectedPath);
            }
        }

        private static void PlayMedia(AvMovie media)
        {
            if (media != null && media.Files.Count > 0)
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

        private void DownloadMedia(AvTorrent media)
        {
            if (media != null)
            {
                media.Download();
                _ = MediaList.Remove(media);
            }
        }

        private async Task ClearDb(object obj/*List<AvMovie> list*/)
        {
            var list = obj.ToList<AvMovie>();
            if (list == null) return;

            foreach (var movie in list)
            {
                await movie.ClearDb(true);
            }
                
            //list.ForEach(m => m.ClearDb(true));
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
        public void AddMedia(Movie movie)
        {
            var item = new AvMovie(movie);
            MediaList.AddInOrder(item, i => i);
        }
        public void AddMedia(Torrent torrent)
        {
            var item = new AvTorrent(torrent);
            MediaList.AddInOrder(item,  i => i, true);
        }

        public async Task AddMedia(string path)
        {
            var media = MediaList.FirstOrDefault(m => m.Path == path);
            if (media == null)
            {
                media = new AvMovie(path);
                await media.ReloadAsync();
                MediaList.AddInOrder(media, i => i);
            }
        }

        private async Task Delete(object obj/*List<AvMovie> mitems*/)
        {
            var mitems = obj.ToList<AvMovie>();
            foreach (var item in mitems)
            {
                if (await item.Delete())
                {
                    _ = MediaList.Remove(item);
                }
            }

            Messenger.Send(new ViewEventArgs("RefreshActors", null));
        }

        private async Task EditMovieInfo(object param)
        {
            if (param is not AvMovie item  || item  == null)
            {
                return;
            }
            if (item.MovieInfo == null)
            {
                Log.Print($"{item.Path}");
                return;
            }

            UiServices.WaitCursor(true);
            var dialog = await AvEditorViewModel.CreateAsync(item);
            UiServices.WaitCursor(false);
            MainView.DialogService.ShowDialog<AvEditorDialog>(this, dialog);
        }

        private async Task UpdateMediaList(string path, CancellationToken token,
            bool bRecursive = false, int level = 0)
        {
            if (token.IsCancellationRequested) return;

            try
            {
                string[] files = Directory.GetFiles(path);
                if (files.Length > 0 && files.Any(f => 
                    AvMovie.video_exts.Any(x =>
                        f.EndsWith(x, StringComparison.OrdinalIgnoreCase))))
                {
                    await AddMedia(path);
                }
                else if (bRecursive || level < 1)
                {
                    string[] dirs = Directory.GetDirectories(path);
                    foreach (string dir in dirs)
                    {
                        await UpdateMediaList(dir, token, bRecursive, level + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Print($"UpdateMediaList: {ex.Message}");
                //_tokenSource?.Cancel();
            }
        }

        private async Task IterateMedia(Stack<string> dirStack, List<string> dbDirs, CancellationToken token)
        {
            int dirCOunt = 0;
            while (dirStack.TryPop(out string stackdir))
            {
                if (token.IsCancellationRequested) return;
                if (stackdir.Contains("Western")) continue;
                string[] dirs;

                try { dirs = Directory.GetDirectories(stackdir); }
                catch { continue; }

                if ((dirs.Length == 0 || dirs.Any(d => d.EndsWith(".actors")))  && Directory.GetFiles(stackdir)
                    .Any(f => AvMovie.video_exts.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase))))
                {

                    dirCOunt++;
                    if (dbDirs.Find(dir => dir == stackdir) == null)
                    {
                        await AddMedia(stackdir);
                    }
                    continue;
                }

                foreach (string dir in dirs)
                {
                    dirStack.Push(dir);
                }
            }

            Log.Print($"Real dir count : {dirCOunt}");
        }

        private CancellationTokenSource _tokenSource;

        private void CancelTaskIfRunning()
        {
            _tokenSource?.Cancel();
        }

        private async void SortMedia()
        {
            CancelTaskIfRunning();
            var tmp = MediaList.ToList();
            MediaList.Clear();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            await Task.Run(() =>
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
        }

        private async void RefreshMediaList(DirectoryInfo msg)
        {
            CancelTaskIfRunning();
            MediaList.Clear();

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;
            bool bSubFolder = _searchSubFolder;
            await UpdateMediaList(msg.FullName, token, bSubFolder);
        }

        private async Task SearchOrphanage()
        {
            CancelTaskIfRunning();
            MediaList.Clear();

            string currDir = _fileList.CurrDirInfo.FullName;
            if (!currDir.EndsWith('\\')) currDir += "\\";

            var dbDirs = await _db.GetMovieUrls(currDir);

            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            Stack<string> dirstack = [];
            dirstack.Push(currDir);
            await IterateMedia(dirstack, dbDirs, token);
            await _db.SaveChangesAsync();
            Log.Print("Search orphanage media done!");
        }

        public async Task LoadItems(List<Movie> movies)
        {
            MediaList.Clear();
            await Task.Run(() =>
            {
                foreach (var movie in movies)
                {
                    AddMedia(movie);
                }
            });
        }

        private async Task OnSearchEmptyActor()
        {
            UiServices.WaitCursor(true);
            await LoadItems(await _db.GetMovies((Movie m) => m.Actors.Count() == 0));
            //await LoadItems(await _db.GetMoviesEmptyFieldOf(""));
            UiServices.WaitCursor(false);
        }

        private async Task LastUpdatedMovies()
        {
            UiServices.WaitCursor(true);
            await LoadItems(await _db.GetMovies(null, 40));
            UiServices.WaitCursor(false);
        }

        private async Task OnTorrents()
        {
            using var db = new TorrentDbContext();
            var torrents = db.GetTorrents();
            MediaList.Clear();
            await Task.Run(() =>
            {
                foreach (var torrent in torrents)
                {
                    AddMedia(torrent);
                }
            });
        }

        private void OnScrapCompleted(SpiderBase spider, bool bSuccess)
        {
            if (bSuccess)
            {
                MainView.OnViewUpdate?.Invoke(new ViewEventArgs("Refresh", null));
                if (_mediasToSearch.Count > 0)
                {
                    _mediasToSearch.RemoveAt(0);
                }
            }
            else
            {
                _mediasToSearch.Clear();
            }

            if (_mediasToSearch.Count > 0)
            {
                Timer timer = null;
                Log.Print($"delay {ScrapDelay}ms");
                void callback(object state)
                {
                    UiServices.Invoke(() => OnScrapAvInfo(spider));
                    timer.Dispose();
                }
                timer = new Timer(callback, null, ScrapDelay, Timeout.Infinite);
            }
            else
            {
                OnScrapAvInfo(spider);
            }
        }

        private void OnScrapAvInfo(SpiderBase spider)
        {
            if (_mediasToSearch == null)
            {
                _forceStopScrapping = false;
                _mediasToSearch = [.. SelectedMedias];
                SpiderManager.OnScrapCompleted = OnScrapCompleted;
            }

            if (!_forceStopScrapping && _mediasToSearch.Count > 0)
            {
                MainView.StatusMessage = $"{_mediasToSearch.Count} remained";
                SpiderManager.Scan(spider, _mediasToSearch[0]);
            }
            else
            {
                SpiderManager.ScanDone(spider);
                MainView.StatusMessage = "";
                _mediasToSearch = null;
            }
        }
    }
}
