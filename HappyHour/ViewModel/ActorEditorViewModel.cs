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
                _isChecked = value;
                ActorEditor.OnActorAlphabet(Initial, value);
            }
        }
        public void UnCheck()
        {
            _isChecked = false;
            OnPropertyChanged(nameof(IsChecked));
        }
    }

    internal class ActorEditorViewModel : ObservableObject, IModalDialogViewModel
    {
        private AvActor _actor;
        private string _picturePath;
        private string _actorName;
        private string _newName;
        private string _searchText;
        private bool? _dialogResult = false;

        private ObservableCollection<AvActorName> _nameListOfOneActor;
        private ObservableCollection<AvActor> _actors = new();
        private List<ScrapItem> ScrapItems { get; set; }

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

        public IEnumerable<AvActorName> ActorNameList =>
            string.IsNullOrEmpty(SearchText) ? null
                    : App.DbContext.ActorNames
                    .Where(n => EF.Functions.Like(n.Name, $"%{SearchText}%"))
                    .ToList();

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
            CmdAddNewActor = new RelayCommand(() => OnAddNewActor());
            CmdDeleteActorFromDb = new RelayCommand(() => OnDeleteActor());
            CmdChangePicture = new RelayCommand(() => OnChangePicture());
            CmdAddNewName = new RelayCommand(() => OnAddNewName());
            CmdDoubleClick = new RelayCommand(() => OnDoubleClicked());
            CmdDeleteActorFromList = new RelayCommand(() => {
                if (SelectedActor != null) Actors.Remove(SelectedActor);
            });
            CmdActorNameDoubleClick = new RelayCommand(() => OnActorNameDoubleClicked());
            CmdMergeActors = new RelayCommand<object>(
                p => OnMergeActors(p), 
                p => p is IList<object> list && list.Count > 1);
            CmdClearActors = new RelayCommand(() => OnClearActors());
            CmdDeleteNameOfActor = new RelayCommand(() => OnDeleteNameOfActor());
            CmdClosed = new RelayCommand(() => OnClose());
 
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

            ScrapItems = new()
            {
                new ScrapItem() { CanUpdate = false, Name = "title" },
                new ScrapItem() { CanUpdate = false, Name = "date" },
                //new ScrapItem() { CanUpdate = true, Name = "runtime" },
                //new ScrapItem() { CanUpdate = true, Name = "director" },
                new ScrapItem() { CanUpdate = false, Name = "series" },
                new ScrapItem() { CanUpdate = false, Name = "studio" },
                new ScrapItem() { CanUpdate = false, Name = "genre" },
                new ScrapItem() { CanUpdate = false, Name = "plot" },
                new ScrapItem() { CanUpdate = false, Name = "cover" },
                new ScrapItem() { CanUpdate = true, Name = "actor" },
                new ScrapItem() { CanUpdate = false, Name = "rating" },
            };
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
                File.Copy(settings.FileName, $"{App.LocalAppData}\\db\\{fileName}", true);
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

        private void OnChangePicture()
        {
            string file = ChoosePicture();
            if (file == null) { return; }

            SelectedActor.PicturePath = Path.GetFileName(file);
            //App.DbContext.SaveChanges();
            DialogResult = true;
        }

        private void OnAddNewName()
        {
            if (SelectedActor == null) { return; }
            if (App.DbContext.ActorNames.Any(i => i.Name == NewName))
            {
                Log.Print($"{NewName} is already exists.");
                return;
            }

            var name = App.DbContext.ActorNames.Add(new AvActorName { Name = NewName });
            SelectedActor.Names.Add(name.Entity);

            try
            {
                _ = App.DbContext.SaveChanges();
                OnPropertyChanged(nameof(SelectedActor));
                NewName = "";
                NameListOfOneActor.Clear();
                //NameListOfOneActor.Concat(_actor.Names);
            }
            catch (Exception ex)
            {
                Log.Print(ex.Message);
            }
        }

        public async void OnActorAlphabet(string p, bool isSelected)
        {
            NameListOfOneActor = null;
            SelectedActor = null;
            if (p == "All")
            {
                foreach (var initial in ActorInitials)
                {
                    if (initial.Initial != "All") { initial.UnCheck(); }
                }
                if (isSelected)
                {
                    var allActors = await App.DbContext.Actors
                        .Include(a => a.Names)
                        //.Include(a => a.Items)
                        .Where(a => a.Items.Count > 0)
                        .OrderByDescending(a => a.DateAdded)
                        .ToListAsync();

                    //var allActors = allNames.Select(n => n.Actor).Distinct();
                    Actors = new ObservableCollection<AvActor>(allActors);
                }
                else
                {
                    Actors.Clear();
                }
                return;
            }

            var namesStartOf = await App.DbContext.ActorNames
                .Include(name => name.Actor)
                    .ThenInclude(a => a.Names)
                .Where(n => EF.Functions.Like(n.Name, $"{p}%"))
                .Where(n => n.Actor != null)
                .OrderBy(n => n.Name)
                .ToListAsync();

            var actors = namesStartOf.Select(n => n.Actor).Distinct();

            if (!actors.Any()) { return; }

            foreach (var actor in actors)
            {
                if (isSelected)
                {
                    Actors.Add(actor);
                }
                else
                {
                    _ = Actors.Remove(actor);
                }
            }
        }

        private async void OnDoubleClicked()
        {
            if (SelectedActor == null) { return; }

            await App.DbContext.Entry(SelectedActor)
                .Collection(a => a.Items).LoadAsync();
            var movies = SelectedActor.Items.ToList();

            if (MediaList != null)
            {
                MediaList.LoadItems(movies);
            }
        }

        private void OnActorNameDoubleClicked()
        {
            if (SelectedActorName == null) { return; }

            if (Actors.Any(a => a.Names.Contains(SelectedActorName))) { return; }

            App.DbContext.Entry(SelectedActorName)
                .Reference(n => n.Actor).Load();
            if (SelectedActorName.Actor == null)
            {
                Log.Print($"Dangling ActorName {SelectedActorName}");
                return;
            }
            Actors.Add(SelectedActorName.Actor);
        }

        private void OnClearActors()
        {
            ActorInitials.ForEach(i => i.UnCheck());
            Actors.Clear();
        }

        private void OnDeleteNameOfActor()
        {
            if (SelectedNameOfActor == null) { return; }
            App.DbContext.Entry(SelectedNameOfActor)
                .Reference(n => n.Actor).Load();
            App.DbContext.Entry(SelectedNameOfActor.Actor)
                .Collection(a => a.Names).Load();
            if (SelectedNameOfActor.Actor.Names.Count < 2) { return; }

            var ret = DialogService.ShowMessageBox(this,
                $"Delete {SelectedNameOfActor.Name} from Actor",
                "Warning", MessageBoxButton.YesNo);
            if (ret == MessageBoxResult.Yes)
            {
                //Log.Print($"Delete {SelectedNameOfActor.Name}");
                _ = SelectedNameOfActor.Actor.Names.Remove(SelectedNameOfActor);
                _ = App.DbContext.ActorNames.Remove(SelectedNameOfActor);
                _ = NameListOfOneActor.Remove(SelectedNameOfActor);
            }

            DialogResult = true;
        }

        private void OnMergeActors(object p)
        {
            AvActor tgtActor = null;
            var selectedActors = (p as IList<object>).Select(o => o as AvActor).ToList();
            foreach (var actor in selectedActors)
            {
                if (tgtActor == null)
                {
                    tgtActor = actor;
                    continue;
                }

                App.DbContext.Entry(actor).Collection(a => a.Items).Load();
                var avItems = actor.Items.ToList();
                foreach (var item in avItems)
                {
                    _ = item.Actors.Remove(actor);
                    if (!item.Actors.Any(a => a.Id == tgtActor.Id))
                    {
                        item.Actors.Add(tgtActor);
                    }
                }
                foreach (var name in actor.Names)
                {
                    if (!tgtActor.Names.Any(n => n.Name.Equals(name.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        name.Actor = tgtActor;
                        tgtActor.Names.Add(name);
                    }
                }
                _ = App.DbContext.Actors.Remove(actor);
                _ = Actors.Remove(actor);
            }
            //App.DbContext.SaveChanges();
            DialogResult = true;
        }

        private void OnClose()
        {
            if (DialogResult.Value)
            {
                App.DbContext.SaveChanges();
            }
        }
    }
}
