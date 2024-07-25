using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

//using GalaSoft.MvvmLight;
//using GalaSoft.MvvmLight.Command;

using MvvmDialogs;

using HappyHour.Model;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HappyHour.ViewModel
{
    internal class AvEditorViewModel : ObservableObject, IModalDialogViewModel
    {
        private readonly MovieDbContext _db = App.Current.DbContext;
        private string _searchActorName;

        private Maker _maker;
        private Label _label;
        private Series _series;

        private bool? _dialogResult;
        private bool _actorChanged;
        private bool _genreChanges;

        public Movie Movie { get; private set; }
        public Label Label 
        {
            get => _label;
            set
            {
                if (value != null)
                {
                    SetProperty(ref _label, value);
                    Movie.Label = value;
                }
            }
        }

        public Series Series
        {
            get => _series;
            set
            {
                if (value != null)
                {
                    SetProperty(ref _series, value);
                    Movie.Series = value;
                }
            }
        }
        public string Title { get; private set; }
        public Label SelectedLabel { get; set; }
        public Series SelectedSeries { get; set; }
        public Actor SelectedActor { get; set; }
        public Genre SelectedGenre { get; set; }

        public ObservableCollection<Actor> Actors { get; set; } = [];
        public ObservableCollection<Genre> Genres { get; set; } = [];

        public IEnumerable<Series> AllSeries { get; private set; }
        public IEnumerable<Genre> AllGenres { get; private set; }

        private readonly IEnumerable<Actor> _actorsSearched = [];
        public IEnumerable<Actor> ActorsSearched
        {
            get
            {
                if (string.IsNullOrEmpty(SearchActorName))
                {
                    return _actorsSearched;
                }
                return _actorsSearched.Where(a => a.ToString().IndexOf(SearchActorName,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
        public IEnumerable<Label> AllLables { get; private set; }
        public string SearchActorName
        {
            get => _searchActorName;
            set
            {
                SetProperty(ref _searchActorName, value);
                //OnPropertyChanged(nameof(AllActors));
            }
        }

        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

        public ICommand CmdSetLabel { get; private set; }
        public ICommand CmdSetSeries { get; private set; }
        public ICommand CmdAddActor { get; private set; }
        public ICommand CmdRemoveActor { get; private set; }
        public ICommand CmdAddGenre { get; private set; }
        public ICommand CmdRemoveGenre { get; private set; }
        public ICommand CmdSave { get; private set; }

        public AvEditorViewModel(AvMovie movie)
        {
            Title = movie.Pid;

            CmdSetLabel = new RelayCommand(() => Label = SelectedLabel);
            CmdSetSeries = new RelayCommand(() => Series = SelectedSeries);
            CmdAddActor = new RelayCommand(OnAddActor);
            CmdRemoveActor = new RelayCommand(OnRemoveActor);
            CmdAddGenre = new RelayCommand(OnAddGnere);
            CmdSave = new RelayCommand(OnSave);

            _db.GetActors(Movie).ForEach(Actors.Add);
            _db.GetGenres(Movie).ForEach(Genres.Add);
        }

        private void OnAddActor()
        {
            if (SelectedActor == null) return;

            if (!Actors.Any(a => a == SelectedActor))
            {
                Actors.Add(SelectedActor);
                //Movie.Actors.Add(SelectedActor);
            }
        }

        private void OnRemoveActor()
        {
            if (SelectedActor != null)
            {
                Actors.Remove(SelectedActor);
                _actorChanged = true;
            }
        }

        private void OnAddGnere()
        {
            if (SelectedGenre == null) return;

            if (!Genres.Any(g => g == SelectedGenre))
            {
                Genres.Add(SelectedGenre);
                _genreChanges = true;
            }
        }

        private void OnSave()
        {
#if false
            using var context = AvDbContextPool.CreateContext();
            _ = context.Items.Attach(Av);
            if (_actorChanged)
            {
                Av.Actors = Actors;
            }
            if (_genreChanges)
            {
                Av.Genres = Genres;
            }

            if (_amovie.MovieInfo == null)
            {
                Av.DateAdded = DateTime.Now;
                Av.DateModifed = DateTime.Now;
            }
           // _amovie.MovieInfo = Av;
            context.SaveChanges();
#endif
        }
    }
}
