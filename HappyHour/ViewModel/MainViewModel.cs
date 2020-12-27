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
using HappyHour.Model;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class MainViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        string _status;

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

        public bool NasEnabled
        {
            get => _nasEnabled;
            set
            {
                if (value == true && _nasEnabled != value)
                {
                    OnPaneEnabled<BrowserViewModel>(value);
                    Set(ref _nasEnabled, value);
                }
            }
        }
#endif
        readonly IDialogService _dialogService;
        readonly FileListViewModel _fileListMv;

        public MainViewModel(IDialogService dialogService)
        {
            // Order of VieModel creation is important.

            _fileListMv = new FileListViewModel();

            Anchors.Add(_fileListMv);
            Anchors.Add(new DebugLogViewModel());
            Anchors.Add(new StatusLogViewModel());
            Anchors.Add(new ConsoleLogViewModel());

            //Docs.Add(new AvDbViewModel());
            Docs.Add(new MediaListViewModel { FileList = _fileListMv });
            Docs.Add(new PlayerViewModel());
            Docs.Add(new BrowserViewModel());
            Docs.Add(new SpiderViewModel());

            CmdFileToFolder = new RelayCommand(() => OnFileToFolder());
            CmdActorEdtor = new RelayCommand(() => OnActorEditor());
            //KeyDownCommand = new RelayCommand<EventArgs>(e => OnKeyDown(e));

            _dialogService = dialogService;

            MessengerInstance.Register<NotificationMessage<string>>(
                this, OnStatusMessage);

            MessengerInstance.Register<NotificationMessage<MediaItem>>(
                this, OnAvEdit);

            MessengerInstance.Register<NotificationMessageAction<IDialogService>>(
                this, msg => msg.Execute(_dialogService));

            MediaElement.FFmpegMessageLogged += OnMediaFFmpegMessageLogged;

            //for update media list
            _fileListMv.DirChanged.Invoke(this, _fileListMv.CurrDirInfo);
        }
#if false
        void OnPaneEnabled<VMType>(bool enabled)
        {
            if (enabled)
        {
                Docs.Add((Pane)Activator.CreateInstance(typeof(VMType)));
            }
            else
            {
                var spiderMv = Docs.FirstOrDefault(d => d is VMType);
                if (spiderMv != null)
                    Docs.Remove(spiderMv);
            }
            }
#endif
        void OnFileToFolder()
        {
            var dialog = new FileToFolderViewModel { MediaPath = _fileListMv.CurrPath };
            _dialogService.ShowDialog<FileToFolderDialog>(this, dialog);
        }

        void OnActorEditor()
        { 
            var dialog = new ActorEditorViewModel(_dialogService);
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

        void OnAvEdit(NotificationMessage<MediaItem> msg)
        {
            if (msg.Notification != "editAv") return;
            var dialog = new AvEditorViewModel(msg.Content);
            _dialogService.ShowDialog<AvEditorDialog>(this, dialog);
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
