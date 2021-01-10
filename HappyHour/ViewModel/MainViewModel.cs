using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using MvvmDialogs;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Command;

using Unosquare.FFME;
using Unosquare.FFME.Common;

using HappyHour.View;
using System.Collections.Specialized;

namespace HappyHour.ViewModel
{
    class MainViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        string _status;
        bool _nasEnabled;

        public ICommand CmdFileToFolder { get; private set; }
        public ICommand CmdActorEdtor { get; private set; }
        //public ICommand KeyDownCommand { get; private set; }

        public ObservableCollection<Pane> Docs { get; }
            = new ObservableCollection<Pane>();
        public ObservableCollection<Pane> Anchors { get; }
            = new ObservableCollection<Pane>();

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                RaisePropertyChanged(nameof(Status));
            }
        }
#if false
        bool _spiderEnabled;
        bool _nasEnabled;

        public bool SpiderEnabled
        {
            get => _spiderEnabled;
            set
            {
                if (value == true &&_spiderEnabled != value)
                {
                    OnPaneEnabled<SpiderViewModel>(value);
                    Set(ref _spiderEnabled, value);
                }
            }
        }
#endif
        public bool NasEnabled
        {
            get => _nasEnabled;
            set
            {
                Set(ref _nasEnabled, value);
                OnPaneEnabled<BrowserViewModel>(value);
            }
        }
        readonly IDialogService _dialogService;
        readonly FileListViewModel _fileListMv;
        readonly MediaListViewModel _mediaListMv;

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            // Order of VieModel creation is important.
            _fileListMv = new FileListViewModel();
            _mediaListMv = new MediaListViewModel
            {
                FileList = _fileListMv,
                DialogService = dialogService
            };

            Anchors.Add(_fileListMv);
            Anchors.Add(new DbViewModel { MediaList = _mediaListMv });

            Anchors.Add(new DebugLogViewModel());
            Anchors.Add(new StatusLogViewModel());
            Anchors.Add(new ConsoleLogViewModel());
            Anchors.Add(new ScreenshotViewModel { MediaList = _mediaListMv });

            Docs.Add(_mediaListMv);
            Docs.Add(new SpiderViewModel { MediaList = _mediaListMv });
            Docs.Add(new PlayerViewModel { MediaList = _mediaListMv });

            CmdActorEdtor = new RelayCommand(() => OnActorEditor());
            CmdFileToFolder = new RelayCommand(() => OnFileToFolder());
            //KeyDownCommand = new RelayCommand<EventArgs>(e => OnKeyDown(e));

            MessengerInstance.Register<NotificationMessage<string>>(
                this, OnStatusMessage);

            MediaElement.FFmpegMessageLogged += OnMediaFFmpegMessageLogged;

            //for update media list
            _fileListMv.DirChanged.Invoke(this, _fileListMv.CurrDirInfo);
            Docs.CollectionChanged += DocsCollectionChanged;
        }

        void DocsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    foreach (var doc in e.OldItems)
                    {
                        (doc as GalaSoft.MvvmLight.ViewModelBase).Cleanup();
                    }
                    break;
            }
        }

        void OnPaneEnabled<VMType>(bool enabled)
        {
            if (enabled)
            {
                var pane = (Pane)Activator.CreateInstance(typeof(VMType));
                Docs.Add(pane);
                pane.IsSelected = true;
            }
            else
            {
                var spiderMv = Docs.FirstOrDefault(d => d is VMType);
                if (spiderMv != null)
                    Docs.Remove(spiderMv);
            }
        }

        void OnFileToFolder()
        {
            var dialog = new FileToFolderViewModel { MediaPath = _fileListMv.CurrPath };
            _dialogService.ShowDialog<FileToFolderDialog>(this, dialog);
        }

        void OnActorEditor()
        { 
            var dialog = new ActorEditorViewModel
            {
                MediaList = _mediaListMv,
                DialogService = _dialogService
            };
            _dialogService.Show<ActorEditorDialog>(this, dialog);
        }
#if false
        void OnKeyDown(EventArgs e)
        {
            foreach (var doc in Docs)
            {
                doc.OnKeyDown(e as KeyEventArgs);
            }
        }
#endif
        void OnStatusMessage(NotificationMessage<string> msg)
        {
            if (!msg.Notification.EndsWith("Status")) return;
            if (msg.Notification == "UpdateStatus")
            {
                Status = msg.Content;
            }
            else if (msg.Notification == "ClearStatus")
            { 
                Status = "";
            }
        }

        void OnMediaFFmpegMessageLogged(object sender, MediaLogMessageEventArgs e)
        {
            if (e.MessageType != MediaLogMessageType.Warning &&
                e.MessageType != MediaLogMessageType.Error)
                return;

            if (string.IsNullOrWhiteSpace(e.Message) == false &&
                e.Message.ContainsOrdinal("Using non-standard frame rate"))
                return;

            Log.Print(e.Message);
        }
    }
}
