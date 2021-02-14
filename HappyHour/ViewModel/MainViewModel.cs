using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MvvmDialogs;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using HappyHour.View;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class MainViewModel : ViewModelBase, IMainView
    {
        string _status;
        bool _nasEnabled;
        bool _spiderEnabled;

        public ICommand CmdFileToFolder { get; private set; }
        public ICommand CmdActorEdtor { get; private set; }

        public ObservableCollection<Pane> Docs { get; }
            = new ObservableCollection<Pane>();
        public ObservableCollection<Pane> Anchors { get; }
            = new ObservableCollection<Pane>();

        public string StatusMessage
        {
            get => _status;
            set => Set(ref _status, value);
        }

        public bool SpiderEnabled
        {
            get => _spiderEnabled;
            set
            {
                Set(ref _spiderEnabled, value);
                var mv = OnPaneEnabled<SpiderViewModel>(value);
                if (value == true)
                {
                    mv.MediaList = _mediaListMv;
                }
            }
        }

        public bool NasEnabled
        {
            get => _nasEnabled;
            set
            {
                Set(ref _nasEnabled, value);
                OnPaneEnabled<NasViewModel>(value);
            }
        }
        public IDialogService DialogService { get; set; }
        readonly FileListViewModel _fileListMv;
        readonly MediaListViewModel _mediaListMv;

        public MainViewModel(IDialogService dialogService)
        {
            DialogService = dialogService;
            Docs.CollectionChanged += OnCollectionChanged;

            _fileListMv = new FileListViewModel();
            _mediaListMv = new MediaListViewModel { FileList = _fileListMv, };

            Anchors.Add(_fileListMv);
            Anchors.Add(new DbViewModel { MediaList = _mediaListMv });

            Anchors.Add(new DebugLogViewModel());
            Anchors.Add(new StatusLogViewModel());
            Anchors.Add(new ConsoleLogViewModel());
            Anchors.Add(new ScreenshotViewModel { MediaList = _mediaListMv });

            Docs.Add(new PlayerViewModel { MediaList = _mediaListMv });
            Docs.Add(_mediaListMv);

            CmdActorEdtor = new RelayCommand(() => OnActorEditor());
            CmdFileToFolder = new RelayCommand(() => OnFileToFolder());

            //for update media list
            _fileListMv.DirChanged?.Invoke(this, _fileListMv.CurrDirInfo);
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Log.Print("Docs.CollectionChanged : " + e.Action.ToString());
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
                    }
                    break;
            }
        }

        public void NewBrowser(string url)
        {
            Docs.Add(new BrowserBase
            {
                Address = url,
                CanClose = true,
                IsSelected = true
            });
        }

        VMType OnPaneEnabled<VMType>(bool enabled) where VMType : Pane
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

        void OnFileToFolder()
        {
            var dialog = new FileToFolderViewModel { MediaPath = _fileListMv.CurrPath };
            DialogService.ShowDialog<FileToFolderDialog>(this, dialog);
        }

        void OnActorEditor()
        { 
            var dialog = new ActorEditorViewModel
            {
                MediaList = _mediaListMv,
                DialogService = DialogService
            };
            DialogService.Show<ActorEditorDialog>(this, dialog);
        }
    }
}
