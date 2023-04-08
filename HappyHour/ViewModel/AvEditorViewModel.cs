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
        private readonly AvMovie _amovie;
        private readonly IEnumerable<AvActor> _allActors;
        private string _searchActorName;

        private AvStudio _studio;
        private AvSeries _series;

        private bool? _dialogResult;
        private bool _actorChanged;
        private bool _genreChanges;

        public AvItem Av { get; private set; }
        public AvStudio Studio
        {
            get => _studio;
            set
            {
                if (value != null)
                {
                    SetProperty(ref _studio, value);
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
                    SetProperty(ref _series, value);
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
                {
                    return _allActors;
                }
                return _allActors.Where(a => a.ToString().IndexOf(SearchActorName,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }
        public IEnumerable<AvStudio> AllStudios { get; private set; }
        public string SearchActorName
        {
            get => _searchActorName;
            set
            {
                SetProperty(ref _searchActorName, value);
                OnPropertyChanged(nameof(AllActors));
            }
        }

        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
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
        public ICommand CmdRemoveGenre { get; private set; }
        public ICommand CmdSave { get; private set; }

        public AvEditorViewModel(AvMovie movie)
        {
            _amovie = movie;
            Title = movie.Pid;

            CmdSetStudio = new RelayCommand(() => Studio = SelectedStudio);
            CmdSetSeries = new RelayCommand(() => Series = SelectedSeries);
            CmdAddActor = new RelayCommand(OnAddActor);
            CmdRemoveActor = new RelayCommand(OnRemoveActor);
            CmdAddGenre = new RelayCommand(OnAddGnere);
            CmdSave = new RelayCommand(OnSave);

            if (movie.MovieInfo != null)
            {
                Av = movie.MovieInfo;
                Actors = new ObservableCollection<AvActor>(Av.Actors);
                Genres = new ObservableCollection<AvGenre>(Av.Genres);
                Studio = Av.Studio;
                Series = Av.Series;
            }
            else
            {
                Av = new AvItem
                {
                    Path = movie.Path,
                    Pid = movie.Pid,
                };
                Actors = new ObservableCollection<AvActor>();
                Genres = new ObservableCollection<AvGenre>();

                //App.Current.DbContext.Items.Attach(Av);
            }

            using var context = AvDbContextPool.CreateContext();
            //context.Items.Attach(Av);

            AllSeries = context.Series.ToList();
            AllGenres = context.Genres.ToList();

            var names = context.ActorNames
                .Include(n => n.Actor)
                .Where(n => n.Actor != null)
                .OrderBy(n => n.Name)
                .ToList();
            _allActors = names.Select(n => n.Actor).Distinct();
            AllStudios = context.Studios.ToList();
        }

        private void OnAddActor()
        {
            if (SelectedActor == null) return;

            if (!Actors.Any(a => a == SelectedActor))
            {
                Actors.Add(SelectedActor);
                _actorChanged = true;
            }
        }

        private void OnRemoveActor()
        {
            if (SelectedAvActor != null)
            {
                Actors.Remove(SelectedAvActor);
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
            _amovie.MovieInfo = Av;
            context.SaveChanges();
        }
    }
}
