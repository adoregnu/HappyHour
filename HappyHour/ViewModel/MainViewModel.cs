using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;

using MvvmDialogs;

using HappyHour.View;
using HappyHour.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HappyHour.ViewModel
{
    internal class MainViewModel : ObservableRecipient, IMainView
    {
        private string _status;
        //private bool _spiderEnabled;

        public ICommand CmdFileToFolder { get; private set; }
        public ICommand CmdActorEdtor { get; private set; }
        public ICommand CmdSpiderSetting { get; private set; }

        public ObservableCollection<Pane> Docs { get; } = new();
        public ObservableCollection<Pane> Anchors { get; } = new();

        public string StatusMessage
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
#if false
        public bool SpiderEnabled
        {
            get => _spiderEnabled;
            set
            {
                Set(ref _spiderEnabled, value);
                var vm = OnPaneEnabled<SpiderViewModel>(value);
                if (value)
                {
                    vm.MediaList = _mediaListMv;
                    vm.DbView = _dbViewMode;
                }
            }
        }
#endif
        public IDialogService DialogService { get; set; }
        private readonly FileListViewModel _fileListMv;
        private readonly MediaListViewModel _mediaListMv;
        private readonly DbViewModel _dbViewMode;

        public MainViewModel(IDialogService dialogService)
        {
            DialogService = dialogService;
            Docs.CollectionChanged += OnCollectionChanged;

            _fileListMv = new FileListViewModel();
            _mediaListMv = new MediaListViewModel { FileList = _fileListMv, };
            _dbViewMode = new DbViewModel { MediaList = _mediaListMv };
            _fileListMv.MediaList = _mediaListMv;

            Anchors.Add(_fileListMv);
            Anchors.Add(_dbViewMode);

            Anchors.Add(new DebugLogViewModel());
            Anchors.Add(new StatusLogViewModel());
            Anchors.Add(new ConsoleLogViewModel());
            Anchors.Add(new ScreenshotViewModel { MediaList = _mediaListMv });

            Docs.Add(_mediaListMv);
            //Docs.Add(new PlayerViewModel { MediaList = _mediaListMv });
            //Docs.Add(new BrowserBase());
            Docs.Add(new SpiderViewModel
            {
                MediaList = _mediaListMv,
                DbView = _dbViewMode
            });

            CmdActorEdtor = new RelayCommand(() => OnActorEditor());
            CmdFileToFolder = new RelayCommand(() => OnFileToFolder());
            CmdSpiderSetting = new RelayCommand(() => OnSpiderSetting());

            //for update media list
            _fileListMv.DirChanged?.Invoke(this, _fileListMv.CurrDirInfo);
        }
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Pane pane in e.NewItems)
                    {
                        pane.MainView = this;
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Pane pane in e.OldItems)
                    {
                        pane.Cleanup();
                        //if (pane is SpiderViewModel) { SpiderEnabled = false; }
                    }
                    break;
            }
        }

        public BrowserBase NewBrowser(string url = null)
        {
            foreach (var doc in Docs)
            {
                if (doc is BrowserBase b && b.HeaderType == "base")
                {
                    if (url != null)
                    {
                        b.Address = url;
                        b.IsSelected = true;
                    }
                    return b;
                }
            }

            var browser = new BrowserBase
            {
                CanClose = true,
                IsActive = true
            };
            if (url != null)
            {
                browser.Address = url;
            }
            Docs.Add(browser);
            return browser;
        }
#if false
        private VMType OnPaneEnabled<VMType>(bool enabled) where VMType : Pane
        {
            Pane pane = null;
            if (enabled)
            {
                pane = (Pane)Activator.CreateInstance(typeof(VMType));
                Docs.Add(pane);
                pane.IsSelected = true;
            }
            else
            {
                pane = Docs.FirstOrDefault(d => d is VMType);
                if (pane != null)
                {
                    Docs.Remove(pane);
                }
            }
            return (VMType)pane;
        }
#endif
        private void OnFileToFolder()
        {
            var dialog = new FileToFolderViewModel { MediaPath = _fileListMv.CurrPath };
            _ = DialogService.ShowDialog<FileToFolderDialog>(this, dialog);
        }

        private void OnActorEditor()
        {
            var dialog = new ActorEditorViewModel
            {
                MediaList = _mediaListMv,
                DialogService = DialogService
            };
            var spiderVm = Docs.FirstOrDefault(d => d is SpiderViewModel) as SpiderViewModel;
            dialog.SpiderList = spiderVm.Spiders;
            DialogService.Show<ActorEditorDialog>(this, dialog);
        }

        private void OnSpiderSetting()
        {
            var spiderVm = Docs.FirstOrDefault(d => d is SpiderViewModel);
            if (spiderVm == null) { return; }

            var dialog = new SpiderSettingViewModel
            {
                Spiders = (spiderVm as SpiderViewModel).Spiders,
                DialogService = DialogService
            };
            dialog.SelectedSpider = dialog.Spiders[1];
            DialogService.Show<SpiderSettingDialog>(this, dialog);
        }
    }
}
