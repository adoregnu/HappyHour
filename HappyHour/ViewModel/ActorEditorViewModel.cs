using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using Microsoft.EntityFrameworkCore;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MvvmDialogs;
using MvvmDialogs.FrameworkDialogs.OpenFile;

using HappyHour.Model;
using HappyHour.Interfaces;
using HappyHour.Spider;
using CommunityToolkit.Mvvm.Messaging;
using FFmpeg.AutoGen;

namespace HappyHour.ViewModel
{
    internal class ActorInitial : ObservableObject
    {
        private bool _isChecked;

        public ActorEditorViewModel ActorEditor;
        public string Initial { get; set; }
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                SetProperty(ref _isChecked, value);
                ActorEditor.OnActorAlphabet(Initial, value);
            }
        }
        public void UnCheck()
        {
            _isChecked = false;
            OnPropertyChanged(nameof(IsChecked));
        }
    }

    internal class ActorEditorViewModel : ObservableRecipient, IModalDialogViewModel, IRecipient<ViewEventArgs>
    {
        private AvActor _actor;
        private string _picturePath;
        private string _actorName;
        private string _newName;
        private string _searchText;
        private bool? _dialogResult = false;

        private readonly AvDbContextPool _dbPool = new ();
        private ObservableCollection<AvActorName> _nameListOfOneActor;
        private ObservableCollection<AvActor> _actors = new();

        public List<ActorInitial> ActorInitials { get; private set; }
        public AvActorName SelectedActorName { get; set; }
        public List<SpiderBase> SpiderList { get; set; }

        public bool? DialogResult
        {
            get => _dialogResult;
            //private set => SetProperty(nameof(DialogResult), ref _dialogResult, value);
            private set => SetProperty(ref _dialogResult, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                OnPropertyChanged(nameof(ActorNameList));
            }
        }

        public ObservableCollection<AvActor> Actors
        {
            get => _actors;
            private set => SetProperty(ref _actors, value);
        }

        public AvActorName SelectedNameOfActor { get; set; }

        public ObservableCollection<AvActorName> NameListOfOneActor
        {
            get => _nameListOfOneActor;
            set => SetProperty(ref _nameListOfOneActor, value);
        }

        public IEnumerable<AvActorName> ActorNameList => _dbPool.SearchActorName(SearchText);

        public AvActor SelectedActor
        {
            get => _actor;
            set
            {
                SetProperty(ref _actor, value);
                if (value != null)
                {
                    NameListOfOneActor = new ObservableCollection<AvActorName>(_actor.Names);
                }
            }
        }

        public string ActorName
        {
            get => _actorName;
            set => SetProperty(ref _actorName, value);
        }

        public string NewName
        {
            get => _newName;
            set => SetProperty(ref _newName, value);
        }

        public string PicturePath
        {
            get => _picturePath;
            set => SetProperty(ref _picturePath, value);
        }

        public IMediaList MediaList { get; set; }
        public IDialogService DialogService { get; set; }

        public ICommand CmdAddNewActor { get; private set; }
        public ICommand CmdDeleteActorFromDb { get; private set; }
        public ICommand CmdChangePicture { get; private set; }
        public ICommand CmdBrowsePicture { get; private set; }
        public ICommand CmdAddNewName { get; private set; }
        public ICommand CmdDoubleClick { get; private set; }
        public ICommand CmdDeleteActorFromList { get; private set; }
        public ICommand CmdActorNameDoubleClick { get; private set; }
        public ICommand CmdMergeActors { get; private set; }
        public ICommand CmdClearActors { get; private set; }
        public ICommand CmdDeleteNameOfActor { get; private set; }
        public ICommand CmdClosed { get; private set; }
  
        public ActorEditorViewModel()
        {
            CmdBrowsePicture = new RelayCommand(() => PicturePath = ChoosePicture());
            CmdAddNewActor = new RelayCommand(OnAddNewActor);
            CmdDeleteActorFromDb = new RelayCommand(OnDeleteActor);
            CmdChangePicture = new RelayCommand(OnChangePicture);
            CmdAddNewName = new RelayCommand(OnAddNewName);
            CmdDoubleClick = new RelayCommand(OnDoubleClicked);
            CmdDeleteActorFromList = new RelayCommand(() => {
                if (SelectedActor != null) Actors.Remove(SelectedActor);
            });
            CmdActorNameDoubleClick = new RelayCommand(OnActorNameDoubleClicked);
            CmdMergeActors = new RelayCommand<object>(
                OnMergeActors, 
                p => p is IList<object> list && list.Count > 1);
            CmdClearActors = new RelayCommand(OnClearActors);
            CmdDeleteNameOfActor = new RelayCommand(OnDeleteNameOfActor);
            CmdClosed = new RelayCommand(OnClose);
 
            ActorInitials = Enumerable.Range('A', 'Z' - 'A' + 1)
                .Select(c => new ActorInitial
                {
                    ActorEditor = this,
                    Initial = ((char)c).ToString(),
                }).ToList();
            ActorInitials.Insert(0, new ActorInitial
            {
                ActorEditor = this,
                Initial = "All",
            });
            Messenger.Register(this);
        }

        public void Receive(ViewEventArgs msg)
        {
            if (msg.Message != "RefreshActors")
            {
                return;
            }

            Log.Print(msg.Message);
            List<ActorInitial> initials = new();
            ActorInitials.ForEach(i => { if (i.IsChecked) initials.Add(i); });
            OnClearActors();
            initials.ForEach(i => i.IsChecked = true);
        }
        string ChoosePicture()
        { 
            var settings = new OpenFileDialogSettings
            {
                Title = "Select Actor Pciture",
                InitialDirectory = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyPictures),
                Filter = "Image files (*.png, *.jpg)|*.png;*.jpg|All files (*.*)|*.*"
            };

            bool? success = DialogService.ShowOpenFileDialog(this, settings);
            if (success != true)
                return null;

            try
            {
                var fileName = Path.GetFileName(settings.FileName);
                File.Copy(settings.FileName, $"{App.Current.LocalAppData}\\db\\{fileName}", true);
                return settings.FileName;
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
            return null;
        }

        void OnAddNewActor()
        {
            if (string.IsNullOrEmpty(ActorName) ||
                string.IsNullOrEmpty(PicturePath))
            {
                Log.Print("Actor name or picture path is empty!");
                return;
            }

            var actor = _dbPool.AddActor(ActorName, PicturePath);
            if (actor != null)
            {
                Actors.Add(actor);
            }
        }

        void OnDeleteActor()
        {
            if (SelectedActor == null) return;

            _dbPool.DeleteActor(SelectedActor);
            Actors.Remove(SelectedActor);

            SelectedActor = null;
            NameListOfOneActor = null;
        }

        private void OnChangePicture()
        {
            string file = ChoosePicture();
            if (file == null) { return; }

            _dbPool.UpdateActor(SelectedActor, (actor) => {
                actor.PicturePath = Path.GetFileName(file);
            });
        }

        private void OnAddNewName()
        {
            if (SelectedActor == null) { return; }

            _dbPool.AddActorName(SelectedActor, NewName);
            OnPropertyChanged(nameof(SelectedActor));

            NewName = "";
            NameListOfOneActor.Clear();
        }
        public async void OnActorAlphabet(string p, bool isSelected)
        {
            if (p == "All")
            {
                foreach (var initial in ActorInitials)
                {
                    if (initial.Initial != "All") { initial.UnCheck(); }
                }
            }

            if (isSelected)
            {
                var actors = await _dbPool.GetActorsByInitial(p);
                actors.ForEach(Actors.Add);
            }
            else if (p == "All")
            {
                Actors.Clear();
            }
            else
            {
                List<AvActor> tmpList = new();
                foreach (var actor in Actors)
                {
                    if (actor.Names.Any(n => n.Name.StartsWith(p)))
                    {
                        tmpList.Add(actor);
                    }
                }
                tmpList.ForEach(a => Actors.Remove(a));
            }
        }
        private async void OnDoubleClicked()
        {
            if (SelectedActor == null) { return; }

            var movies = await _dbPool.GetMoviesByActor(SelectedActor);
            MediaList?.LoadItems(movies);
        }

        private void OnActorNameDoubleClicked()
        {
            if (SelectedActorName == null) { return; }

            if (Actors.Any(a => a.Names.Contains(SelectedActorName))) { return; }

            var actor = _dbPool.GetActorByName(SelectedActorName);
            if (actor != null)
            {
                Actors.Add(actor);
            }
        }

        private void OnClearActors()
        {
            ActorInitials.ForEach(i => i.UnCheck());
            Actors.Clear();
        }

        private void OnDeleteNameOfActor()
        {
            if (SelectedNameOfActor == null) { return; }

            var ret = DialogService.ShowMessageBox(this,
                $"Delete {SelectedNameOfActor.Name} from Actor",
                "Warning", MessageBoxButton.YesNo);
            if (ret == MessageBoxResult.Yes)
            {
                _dbPool.DeleteActorName(SelectedNameOfActor);
            }
            NameListOfOneActor.Remove(SelectedNameOfActor);
        }

        private void OnMergeActors(object p)
        {
            var selectedActors = (p as IList<object>).Select(o => o as AvActor).ToList();
            _dbPool.MergeActors(selectedActors, (a) => Actors.Remove(a));
        }

        private void OnClose()
        {
            Messenger.Unregister<ViewEventArgs>(this);
        }
    }
}
