using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using MvvmDialogs;

using HappyHour.Model;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace HappyHour.ViewModel
{
    class AvEditorViewModel : ViewModelBase, IModalDialogViewModel
    {
        readonly MediaItem _mediaItem;
        readonly IEnumerable<AvActor> _allActors;
        string _searchActorName;

        AvStudio _studio;
        AvSeries _series;

        bool? _dialogResult;

        public AvItem Av { get; private set; }
        public AvStudio Studio
        {
            get => _studio;
            set
            {
                if (value != null)
                {
                    Set(ref _studio, value);
                    Av.Studio = value;
                }
            }
        }

        public AvSeries Series
        {
            get => _series;
            set
            {
                if (value != null)
                {
                    Set(ref _series, value);
                    Av.Series = value;
                }
            }
        }
        public ObservableCollection<AvActor> Actors { get; set; }
        public ObservableCollection<AvGenre> Genres { get; set; }

        public IEnumerable<AvSeries> AllSeries { get; private set; }
        public IEnumerable<AvGenre> AllGenres { get; private set; }
        public IEnumerable<AvActor> AllActors
        {
            get
            { 
                if (string.IsNullOrEmpty(SearchActorName))
                    return _allActors;
                return _allActors.Where(
                    a => a.ToString().IndexOf(SearchActorName,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
        public IEnumerable<AvStudio> AllStudios { get; private set; }
        public string SearchActorName
        {
            get => _searchActorName;
            set
            {
                Set(ref _searchActorName, value);
                RaisePropertyChanged(nameof(AllActors));
            }
        }

        public bool? DialogResult
        {
            get => _dialogResult;
            private set => Set(nameof(DialogResult), ref _dialogResult, value);
        }

        public string Title { get; private set; }
        public AvStudio SelectedStudio { get; set; }
        public AvSeries SelectedSeries { get; set; }
        public AvActor SelectedActor { get; set; }
        public AvGenre SelectedGenre { get; set; }

        public AvActor SelectedAvActor { get; set; }
        public AvGenre SelectedAvGenre { get; set; }

        public ICommand CmdSetStudio { get; private set; }
        public ICommand CmdSetSeries { get; private set; }
        public ICommand CmdAddActor { get; private set; }
        public ICommand CmdRemoveActor { get; private set; }
        public ICommand CmdAddGenre { get; private set; }
        public ICommand CmdSave { get; private set; }

        public AvEditorViewModel(MediaItem mediaItem)
        {
            _mediaItem = mediaItem;
            Title = mediaItem.Pid;

            Av = mediaItem.AvItem;
            Actors = new ObservableCollection<AvActor>(Av.Actors);
            Genres = new ObservableCollection<AvGenre>(Av.Genres);
            Studio = Av.Studio;
            Series = Av.Series;

            CmdSetStudio = new RelayCommand(() => Studio = SelectedStudio);
            CmdSetSeries = new RelayCommand(() => Series = SelectedSeries);
            CmdAddActor = new RelayCommand(() => OnAddActor());
            CmdRemoveActor = new RelayCommand(() => OnRemoveActor());
            CmdAddGenre = new RelayCommand(() => OnAddGnere());
            CmdSave = new RelayCommand(() => OnSave());

            AllSeries = App.DbContext.Series.ToList();
            AllGenres = App.DbContext.Genres.ToList();

            var names = App.DbContext.ActorNames
                .Include(n => n.Actor)
                .Where(n => n.Actor != null)
                .OrderBy(n => n.Name)
                .ToList();
            _allActors = names.Select(n => n.Actor).Distinct();
            AllStudios = App.DbContext.Studios.ToList();
        }

        bool _actorChanged = false;
        void OnAddActor()
        {
            if (!Actors.Any(a => a == SelectedActor))
            {
                Actors.Add(SelectedActor);
                _actorChanged = true;
            }
        }

        void OnRemoveActor()
        {
            if (SelectedAvActor != null)
            {
                Actors.Remove(SelectedAvActor);
                _actorChanged = true;
            }
        }

        bool _genreChanges = false;
        void OnAddGnere()
        {
            if (!Genres.Any(g => g == SelectedGenre))
                Genres.Add(SelectedGenre);
        }

        void OnSave()
        {
            if (_actorChanged)
                Av.Actors = Actors;
            if (_genreChanges)
                Av.Genres = Genres;
            App.DbContext.SaveChanges();

            _mediaItem.ReloadAvItem();
        }
    }
}
