using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Microsoft.EntityFrameworkCore;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

using MvvmDialogs;
using MvvmDialogs.FrameworkDialogs.OpenFile;

using HappyHour.Model;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class ActorInitial : ViewModelBase
    {
        bool _isChecked = false;
        public ActorEditorViewModel ActorEditor;
        public string Initial { get; set; }
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                ActorEditor.OnActorAlphabet(Initial, value);
            }
        }
        public void UnCheck()
        {
            _isChecked = false; 
            RaisePropertyChanged(nameof(IsChecked));
        }
    }

    class ActorEditorViewModel : ViewModelBase, IModalDialogViewModel
    {
        AvActor _actor;
        string _picturePath;
        string _actorName;
        string _newName;
        string _searchText;

        bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            private set => Set(nameof(DialogResult), ref _dialogResult, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                Set(ref _searchText, value);
                RaisePropertyChanged(nameof(ActorNameList));
            }
        }

        ObservableCollection<AvActor> _actors
            = new ObservableCollection<AvActor>();
        public ObservableCollection<AvActor> Actors
        {
            get => _actors;
            private set => Set(ref _actors, value);
        }

        public AvActorName SelectedNameOfActor { get; set; }

        ObservableCollection<AvActorName> _nameListOfOneActor = null;
        public ObservableCollection<AvActorName> NameListOfOneActor
        {
            get => _nameListOfOneActor;
            set => Set(ref _nameListOfOneActor, value);
        }

        public List<ActorInitial> ActorInitials { get; private set; }

        public AvActorName SelectedActorName { get; set; }
        public IEnumerable<AvActorName> ActorNameList
        {
            get
            {
                if (string.IsNullOrEmpty(SearchText))
                    return null;

                return App.DbContext.ActorNames
                    .Where(n => EF.Functions.Like(n.Name, $"%{SearchText}%"))
                    .ToList();
            }
        }

        public AvActor SelectedActor
        {
            get => _actor;
            set
            {
                Set(ref _actor, value);
                if (value != null)
                {
                    NameListOfOneActor = new ObservableCollection<AvActorName>(_actor.Names);
                }
            }
        }

        public string ActorName
        {
            get => _actorName;
            set => Set(ref _actorName, value);
        }

        public string NewName
        {
            get => _newName;
            set => Set(ref _newName, value);
        }

        public string PicturePath
        {
            get => _picturePath;
            set => Set(ref _picturePath, value);
        }

        public IMediaList MediaList { get; set; }
        public IDialogService DialogService { get; set; }

        public ICommand CmdAddNewActor { get; private set; }
        public ICommand CmdDeleteActor { get; private set; }
        public ICommand CmdChangePicture { get; private set; }
        public ICommand CmdBrowsePicture { get; private set; }
        public ICommand CmdAddNewName { get; private set; }
        public ICommand CmdDoubleClick { get; private set; }
        public ICommand CmdActorNameDoubleClick { get; private set; }
        public ICommand CmdMergeActors { get; private set; }
        public ICommand CmdDelNameOfActor { get; private set; }
        public ICommand CmdClearActors { get; private set; }
        public ICommand CmdSave { get; private set; }
        public ICommand CmdDeleteNameOfActor { get; private set; }

        public ActorEditorViewModel()
        {
            CmdBrowsePicture = new RelayCommand(() => PicturePath = ChoosePicture());
            CmdAddNewActor = new RelayCommand(() => OnAddNewActor());
            CmdDeleteActor = new RelayCommand(() => OnDeleteActor());
            CmdChangePicture = new RelayCommand(() => OnChangePicture());
            CmdAddNewName = new RelayCommand(() => OnAddNewName());
            CmdDoubleClick = new RelayCommand(() => OnDoubleClicked());
            CmdActorNameDoubleClick = new RelayCommand(() => OnActorNameDoubleClicked());
            CmdMergeActors = new RelayCommand<object>(
                p => OnMergeActors(p), 
                p => p is IList<object> list && list.Count > 1);
            CmdDelNameOfActor = new RelayCommand(() =>
            {
                App.DbContext.ActorNames.Remove(SelectedNameOfActor);
                NameListOfOneActor.Remove(SelectedNameOfActor);
                SelectedNameOfActor = null;
            });
            CmdClearActors = new RelayCommand(() => OnClearActors());
            CmdSave = new RelayCommand(() => App.DbContext.SaveChanges());
            CmdDeleteNameOfActor = new RelayCommand(() => OnDeleteNameOfActor());

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
            //ActorInitials[1].IsChecked = true;
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
                File.Copy(settings.FileName, $"{App.CurrentPath}\\db\\{fileName}", true);
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

            if (App.DbContext.ActorNames.Any(i => i.Name == ActorName))
            {
                Log.Print($"{NewName} is already exists.");
                return;
            }

            var names = new List<AvActorName> {
                new AvActorName { Name = ActorName }
            };
            var actor = new AvActor
            {
                PicturePath = Path.GetFileName(PicturePath),
                Names = names
            };
            names.ForEach(n => n.Actor = actor);

            App.DbContext.Actors.Add(actor);
            App.DbContext.SaveChanges();

            Actors.Add(actor);
        }

        void OnDeleteActor()
        {
            if (SelectedActor == null) return;

            var names = SelectedActor.Names.ToList();
            foreach (var name in names)
            {
                App.DbContext.ActorNames.Remove(name);
            }
            App.DbContext.Actors.Remove(SelectedActor);
            App.DbContext.SaveChanges();
            Actors.Remove(SelectedActor);

            SelectedActor = null;
            NameListOfOneActor = null;
        }

        void OnChangePicture()
        {
            var file = ChoosePicture();
            if (file == null)
                return;

            SelectedActor.PicturePath = Path.GetFileName(file);
            //App.DbContext.SaveChanges();
        }

        void OnAddNewName()
        {
            if (SelectedActor == null) return;
            if (App.DbContext.ActorNames.Any(i => i.Name == NewName))
            {
                Log.Print($"{NewName} is already exists.");
                return;
            }

            var name = new AvActorName { Name = NewName };
            App.DbContext.ActorNames.Add(name);
            SelectedActor.Names.Add(name);

            try
            {
                App.DbContext.SaveChanges();
                RaisePropertyChanged(nameof(SelectedActor));
                NewName = "";
                NameListOfOneActor.Clear();
                NameListOfOneActor.Concat(_actor.Names);
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        public void OnActorAlphabet(string p, bool isSelected)
        {
            NameListOfOneActor = null;
            SelectedActor = null;
            List<AvActorName> names = null;
            if (p == "All")
            {
                foreach (var initial in ActorInitials)
                {
                    if (initial.Initial != "All") initial.UnCheck();
                }
                if (isSelected)
                {
                    names = App.DbContext.ActorNames
                        .Include(name => name.Actor)
                            //.ThenInclude(actor => actor.Items)
                        .OrderBy(n => n.Name)
                        .ToList();
                    Actors = new ObservableCollection<AvActor>(
                        names.Select(n => n.Actor).Distinct());
                }
                else
                {
                    Actors.Clear();
                }
                return;
            }

            names = App.DbContext.ActorNames
                .Include(name => name.Actor)
                    //.ThenInclude(actor => actor.Items)
                .Where(n => EF.Functions.Like(n.Name, $"{p}%"))
                .OrderBy(n => n.Name)
                .ToList();

            if (names == null || names.Count == 0)
                return;

            var actors = names.Select(n => n.Actor).Distinct();
            foreach (var actor in actors)
            {
                if (actor == null) continue;
                if (isSelected)
                {
                    Actors.Add(actor);
                    //Actors.InsertInPlace(actor, a => a.ToString());
                }
                else
                {
                    Actors.Remove(actor);
                }
            }
        }

        void OnDoubleClicked()
        {
            if (SelectedActor == null)
                return;

            var movies = new List<string>();
            UiServices.WaitCursor(true);
            App.DbContext.Entry(SelectedActor)
                .Collection(a => a.Items).Load();
            foreach (var item in SelectedActor.Items.ToList())
            {
                movies.Add(item.Path);
            }
            if (MediaList != null)
                MediaList.Replace(movies);
            UiServices.WaitCursor(false);
        }

        void OnActorNameDoubleClicked()
        {
            if (SelectedActorName == null)
                return;

            if (Actors.Any(a => a.Names.Contains(SelectedActorName)))
                return;

            App.DbContext.Entry(SelectedActorName)
                .Reference(n => n.Actor).Load();
            Actors.Add(SelectedActorName.Actor);
        }

        void OnClearActors()
        {
            ActorInitials.ForEach(i => i.UnCheck());
            Actors.Clear();
        }

        void OnDeleteNameOfActor()
        {
            if (SelectedNameOfActor == null) return;
            App.DbContext.Entry(SelectedNameOfActor)
                .Reference(n => n.Actor).Load();
            App.DbContext.Entry(SelectedNameOfActor.Actor)
                .Collection(a => a.Names).Load();
            if (SelectedNameOfActor.Actor.Names.Count < 2)
                return;

            var ret = DialogService.ShowMessageBox(this,
                $"Delete {SelectedNameOfActor.Name} from Actor",
                "Warning", MessageBoxButton.YesNo);
            if (ret == MessageBoxResult.Yes)
            {
                //Log.Print($"Delete {SelectedNameOfActor.Name}");
                SelectedNameOfActor.Actor.Names.Remove(SelectedNameOfActor);
                NameListOfOneActor.Remove(SelectedNameOfActor);
            }
        }

        void OnMergeActors(object p)
        {
            AvActor tgtActor = null;
            List<AvActor> selectedActors =
                (p as IList<object>).Select(o => o as AvActor).ToList();
            foreach (var actor in selectedActors)
            {
                if (tgtActor == null)
                {
                    tgtActor = actor;
                    continue;
                }

                App.DbContext.Entry(actor)
                    .Collection(a => a.Items).Load();
                var avItems = actor.Items.ToList();
                foreach (var item in avItems)
                {
                    item.Actors.Remove(actor);
                    item.Actors.Add(tgtActor);
                }
                foreach (var name in actor.Names)
                {
                    if (!tgtActor.Names.Any(n => n.Name.Equals(name.Name,
                        StringComparison.OrdinalIgnoreCase)))
                    {
                        name.Actor = tgtActor;
                        tgtActor.Names.Add(name);
                    }
                }
                App.DbContext.Actors.Remove(actor);
                Actors.Remove(actor);
            }
            //App.DbContext.SaveChanges();
        }
    }
}
