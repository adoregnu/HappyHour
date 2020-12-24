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

namespace HappyHour.ViewModel
{
    class MainViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        public ICommand CmdFileToFolder { get; private set; }
        public ICommand CmdActorEdtor { get; private set; }
        //public ICommand KeyDownCommand { get; private set; }

        public ObservableCollection<Pane> Docs { get; }
            = new ObservableCollection<Pane>();
        public ObservableCollection<Pane> Anchors { get; }
            = new ObservableCollection<Pane>();

        string _status;
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                RaisePropertyChanged("Status");
            }
        }

        readonly IDialogService _dialogService;

        public MainViewModel(IDialogService dialogService)
        {
            // Order of VieModel creation is important.
            //Docs.Add(new MediaViewModel());
            //Docs.Add(new AvDbViewModel());
            //Docs.Add(new BrowserViewModel());

            Anchors.Add(new DebugLogViewModel());
            Anchors.Add(new FileListViewModel());
            //Anchors.Add(new StatusLogViewModel());

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
        }

        void OnFileToFolder()
        {
            var dialog = new FileToFolderViewModel();
            bool? ret = _dialogService.ShowDialog<FileToFolderDialog>(this, dialog);
            if (ret.HasValue && ret.Value)
            {
                MessengerInstance.Send(new NotificationMessage<bool>(ret.Value, "FileRenamed"));
            }
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
