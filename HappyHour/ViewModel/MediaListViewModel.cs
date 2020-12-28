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

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

//using FileListView.Interfaces;
//using FileSystemModels.Models.FSItems.Base;

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

            MessengerInstance.Register<NotificationMessage<SpiderEnum>>(this,
                (msg) => SpiderList = msg.Content.Where(i => i.Name != "sehuatang"));
        }

        void OnDirChanged(object sender, DirectoryInfo msg)
        {
            ClearMedia();
            _serialQueue.Enqueue(() => UpdateMediaList(msg.FullName));
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
            IsBrowsing = false;
            MediaList.Clear();
            foreach (var path in paths)
            {
                InsertMedia(path);
            }
            IsBrowsing = true;
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

        public MediaItem GetMedia(string path)
        {
            try
            {
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
            var item = GetMedia(path);
            if (item == null) return;

            int idx = -1;
            if (item.IsImage)
            {
                idx = MediaList.FindItem(item, i => i.DownloadDt);
            }
            //MediaList.InsertInPlace(item, i => i.DownloadDt);
            if (idx >= 0)
                MediaList.Insert(idx, item);
            else
                MediaList.Add(item);
        }

        void OnMoveItem(object param)
        {
            if (!(param is IList<object> items) || items.Count == 0)
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
            if (!(param is IList<object> items) || items.Count == 0)
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
