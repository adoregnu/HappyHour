using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;

//using GalaSoft.MvvmLight.Command;

using HappyHour.Interfaces;
using HappyHour.Model;

namespace HappyHour.ViewModel
{
    internal class FileListViewModel : Pane, IFileList
    {
        private string _currPath;
        private DriveInfo _currDrive;
        private DirectoryInfo _currDirInfo;
        private FileSystemInfo _selectedFile;
        private FileSystemWatcher _fsWatcher;
        private readonly DispatcherTimer _refreshTimer;
        private IMediaList _mediaList;
        private bool _selecFromMediaList;

        public string CurrPath
        {
            get => _currPath;
            set
            {
                if (Directory.Exists(value))
                {
                    SetProperty(ref _currPath, value);
                    _currDirInfo = new DirectoryInfo(value);
                    ChangeDir(true);
                }
            }
        }

        string _newName;
        public string NewName
        {
            get => _newName;
            set
            {
                _newName = value;
                Log.Print($"{SelectedFile.Name}, {value}");
            }
        }

        public DirectoryInfo CurrDirInfo
        {
            get => _currDirInfo;
            set
            {
                if (_currDirInfo != value)
                {
                    SetProperty(ref _currDirInfo, value);
                    ChangeDir();
                }
            }
        }
        public DriveInfo CurrDrive
        {
            get => _currDrive;
            set
            {
                SetProperty(ref _currDrive, value);
                CurrDirInfo = value.RootDirectory;
            }
        }

        public ObservableCollection<FileSystemInfo> FileList { get; private set; }
        public ObservableCollection<DriveInfo> Drives { get; private set; }
        public FileSystemInfo SelectedFile
        {
            get => _selectedFile;
            set
            {
                SetProperty(ref _selectedFile, value);
                if (!_selecFromMediaList && value != null)
                {
                    FileSelected?.Invoke(this, value);
                }
            }
        }
        public FileListDirChangeEventHandler DirChanged { get; set; }
        public FileListWatcherEventHandler DirModifed { get; set; }
        public FileListFileSelectEventHandler FileSelected { get; set; }

        public IMediaList MediaList
        {
            get => _mediaList;
            set
            {
                if (_mediaList != value)
                {
                    _mediaList = value;
                    _mediaList.ItemSelectedHandler += OnMediaSelected;
                }
            }
        }

        public ICommand CmdUpDir { get; set; }
        public ICommand CmdRefreshDir { get; set; }
        public ICommand CmdDownToSubfolder { get; set; }
        public ICommand CmdRename { get; set; }
        public FileListViewModel()
        {
            Title = "Files";
            FileList = new ObservableCollection<FileSystemInfo>();
            Drives = new ObservableCollection<DriveInfo>();
            CmdUpDir = new RelayCommand(() => UpDir());
            CmdRefreshDir = new RelayCommand(() =>
            {
                PopulateFiles();
                DirChanged?.Invoke(this, CurrDirInfo);
            });
            CmdDownToSubfolder = new RelayCommand(() => EnterSubFolder());
            CmdRename = new RelayCommand<object>(p => RenameFile(p));

            var lastDir = App.Current.GConf["general"]["last_path"];
            var driveName = lastDir.Substring(0, 2);

            RefreshDrive();

            _currDrive = Drives.FirstOrDefault(
                d => d.Name.StartsWith(driveName, StringComparison.OrdinalIgnoreCase));
            if (_currDrive == null) lastDir = "c:\\";
            CurrDirInfo = new DirectoryInfo(lastDir);

            _refreshTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(200),
            };
            _refreshTimer.Tick += OnRefreshTimerExpired;
        }
 
        void RefreshDrive()
        {
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.DriveType == DriveType.CDRom)
                    continue;
                Drives.Add(drive);
            }
            if (CurrDrive == null) CurrDrive = Drives[0];
        }

        public void EnterSubFolder()
        { 
            //Log.Print($"DoubleClicked {SelectedFile.FullName}");
            if (SelectedFile is DirectoryInfo)
            {
                CurrDirInfo = SelectedFile as DirectoryInfo;
            }
        }

        void UpDir()
        {
            if (CurrDirInfo.Parent != null)
            {
                CurrDirInfo = CurrDirInfo.Parent;
        }
        }

        void ChangeDir(bool bManual = false)
        {
            if (!bManual)
            {
                _currPath = CurrDirInfo.FullName;
                OnPropertyChanged(nameof(CurrPath));
            }

            DeinitFsWather();
            InitFsWatcher();
            PopulateFiles();
            DirChanged?.Invoke(this, CurrDirInfo);
            App.Current.GConf["general"]["last_path"] = _currPath;
        }

        void InitFsWatcher()
        {
            _fsWatcher = new FileSystemWatcher {
                Path = CurrDirInfo.FullName,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            _fsWatcher.Created += (src, e) => RefreshFiles(e);
            _fsWatcher.Deleted += (src, e) => RefreshFiles(e);
            _fsWatcher.Renamed += (src, e) => RefreshFiles(e);
            _fsWatcher.EnableRaisingEvents = true;
        }

        void DeinitFsWather()
        {
            if (_fsWatcher == null) return;
            _fsWatcher.EnableRaisingEvents = false;
            _fsWatcher.Dispose();
            _fsWatcher = null;
        }

        void RefreshFiles(FileSystemEventArgs e)
        {
            UiServices.Invoke(delegate { DirModifed?.Invoke(this, e); }, true);

            if (_refreshTimer.IsEnabled)
            {
                _refreshTimer.Interval = TimeSpan.FromMilliseconds(200);
            }
            else
            {
                _refreshTimer.Start();
            }
        }

        void OnRefreshTimerExpired(object sender, EventArgs e)
        { 
            _refreshTimer.Stop();
            UiServices.Invoke(() => PopulateFiles(), true);
        }

        void PopulateFiles()
        {
            FileList.Clear();

            foreach (var fi in CurrDirInfo.EnumerateDirectories())
            {
                FileList.Add(fi);
            }

            foreach (var fi in CurrDirInfo.EnumerateFiles())
            {
                FileList.Add(fi);
            }
        }

        private void RenameFile(object p)
        {
            if (p is not Tuple<string, object> tuple)
            {
                return;
            }
            if (SelectedFile == null)
            {
                return;
            }

            Log.Print($"{tuple.Item1}, {SelectedFile.Name}");
            if (tuple.Item1 == SelectedFile.Name)
            {
                return;
            }

            try
            {
                string target = Path.GetDirectoryName(SelectedFile.FullName);
                target += $"\\{tuple.Item1}";
                if (SelectedFile is DirectoryInfo)
                {
                    Directory.Move(SelectedFile.FullName, target);
                }
                else
                {
                    File.Move(SelectedFile.FullName, target);
                }
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        private void OnMediaSelected(object _, IAvMedia avMedia)
        {
            FileSystemInfo fi = FileList.FirstOrDefault(f => f.FullName == avMedia.Path);
            if (fi != null)
            {
                _selecFromMediaList = true;
                SelectedFile = fi;
                _selecFromMediaList = false;
            }
        }
    }
}
