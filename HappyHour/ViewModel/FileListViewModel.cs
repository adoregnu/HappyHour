using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

using HappyHour.Interfaces;
using HappyHour.Utils;

namespace HappyHour.ViewModel
{
    class FileListViewModel : Pane, IFileList
    {
        string _currPath;
        DriveInfo _currDrive;
        DirectoryInfo _currDirInfo;
        FileSystemWatcher _fsWatcher;
        DispatcherTimer _refreshTimer; 

        public string CurrPath
        {
            get => _currPath;
            set
            {
                if (Directory.Exists(value))
                {
                    Set(ref _currPath, value);
                    _currDirInfo = new DirectoryInfo(value);
                    ChangeDir(true);
                }
            }
        }

        public DirectoryInfo CurrDirInfo
        {
            get => _currDirInfo;
            set
            {
                if (_currDirInfo != value)
                {
                    Set(ref _currDirInfo, value);
                    ChangeDir();
                }
            }
        }
        public DriveInfo CurrDrive
        {
            get => _currDrive;
            set
            {
                Set(ref _currDrive, value);
                CurrDirInfo = value.RootDirectory;
            }
        }

        public ObservableCollection<FileSystemInfo> FileList { get; private set; }
        public ObservableCollection<DriveInfo> Drives { get; private set; }
        public FileSystemInfo SelectedFile { get; set; }
        public FileListDirChangeEventHandler DirChanged { get; set; }
        public FileListWatcherEventHandler DirModifed { get; set; }

        public ICommand CmdUpDir { get; set; }
        public ICommand CmdRefreshDir { get; set; }
        public ICommand CmdDownToSubfolder { get; set; }
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

            var lastDir = App.GConf["general"]["last_path"];
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
                RaisePropertyChanged(nameof(CurrPath));
            }

            DeinitFsWather();
            InitFsWatcher();
            PopulateFiles();
            DirChanged?.Invoke(this, CurrDirInfo);
            App.GConf["general"]["last_path"] = _currPath;
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
            UiServices.Invoke(delegate { PopulateFiles(); }, true);
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
    }
}
