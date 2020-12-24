using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.ViewModel
{
    class FileListViewModel : Pane
    {
        string _currPath;
        FileSystemWatcher _fsWatcher;

        public string CurrPath
        {
            get => _currPath;
            set => Set(ref _currPath, value);
        }

        public ObservableCollection<FileSystemInfo> FileList { get; private set; }
        public FileSystemInfo SelectedFile { get; set; }

        public FileListViewModel()
        {
            Title = "Files";
            CurrPath = "c:\\Works";
            FileList = new ObservableCollection<FileSystemInfo>();

            OnDirPathChanged(CurrPath);
        }

        void OnDirPathChanged(string path)
        {
            InitFilesystemWatcher(path);
            PopulateFiles(path);
        }

        void InitFilesystemWatcher(string path)
        {
            _fsWatcher = new FileSystemWatcher {
                Path = path,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            _fsWatcher.Created += OnFsChanged;
            _fsWatcher.Deleted += OnFsChanged;
            _fsWatcher.Renamed += OnFileRenamed;
            _fsWatcher.EnableRaisingEvents = true;
        }

        void OnFsChanged(object source, FileSystemEventArgs e)
        {
#if false
            FileSystemInfo fsInfo = null;
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    break;
                case WatcherChangeTypes.Deleted:
                    fsInfo = FileList.FirstOrDefault(f => f.Name == e.Name);
                    if (fsInfo != null) FileList.Remove(fsInfo);
                    break;
                case WatcherChangeTypes.Renamed:
                    break;
            }
#endif
            UiServices.Invoke(delegate { 
                PopulateFiles(CurrPath);
            }, true);
        }

        void OnFileRenamed(object source, RenamedEventArgs e)
        { 
            UiServices.Invoke(delegate { 
                PopulateFiles(CurrPath);
            }, true);
        }

        void PopulateFiles(string path)
        {
            FileList.Clear();
            var currDir = new DirectoryInfo(path);

            foreach (var fi in currDir.EnumerateDirectories())
            {
                FileList.Add(fi);
            }

            foreach (var fi in currDir.EnumerateFiles())
            {
                FileList.Add(fi);
            }
        }
    }
}
