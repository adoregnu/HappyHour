using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using MvvmDialogs;

using HappyHour.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;

namespace HappyHour.ViewModel
{
    internal class AvEditorViewModel : ObservableObject, IModalDialogViewModel
    {
        public static Task<AvEditorViewModel> CreateAsync(AvMovie am)
        {
            var ret = new AvEditorViewModel(am);
            return ret.InitializeAsync();
        }

        private readonly MovieDbContext _db = App.Current.DbContext;

        private bool? _dialogResult;

        private List<Actor> _actors = [];
        private List<Actor> _allActors;
        private Maker _selectedMaker;

        public Movie Movie { get; private set; }
        public List<Genre> Genres { get; private set; } = [];
        public List<Actor> Actors
        {
            get => _actors;
            set => SetProperty(ref _actors, value);
        }

        public List<Maker> AllMakers { get; private set; } = [];
        public List<Label> Labels { get; private set; } = [];
        public List<Series> AllSeries { get; private set; } = [];
        public List<Genre> AllGenres { get; private set; } = [];
        public List<Actor> AllActors
        {
            get => _allActors;
            set => SetProperty(ref _allActors, value);
        }

        public Maker SelectedMaker
        {
            get => _selectedMaker;
            set
            {
                SetProperty(ref _selectedMaker, value);
                if (value != null)
                {
                    Labels = _db.GetLabels(value);
                    OnPropertyChanged(nameof(Labels));
                }
            }
        }

        private string _searchActorName;
        public string SearchActorName
        {
            get => _searchActorName;
            set
            {
                SetProperty(ref _searchActorName, value);
                if (!string.IsNullOrEmpty(value))
                {
                    Application.Current.Dispatcher.InvokeAsync(async () => AllActors = await _db.GetActors(value));
                }
            }
        }

        private string _searchMaker;
        public string SearchMaker
        {
            get => _searchMaker;
            set
            {
                SetProperty(ref _searchMaker, value);
                if (!string.IsNullOrEmpty(value))
                {
                    Application.Current.Dispatcher.InvokeAsync(async () => AllMakers = await _db.GetMakers(value));
                }
            }
        }

        private string _searchLabel;
        public string SearchLabel
        {
            get => _searchLabel;
            set
            {
                SetProperty(ref _searchLabel, value);
                if (!string.IsNullOrEmpty(value))
                {
                    Application.Current.Dispatcher.InvokeAsync(async () => Labels = await _db.GetLabels(value));
                }
            }
        }
        private string _searchGenre;
        public string SearchGenre
        {
            get => _searchGenre;
            set
            {
                SetProperty(ref _searchGenre, value);
                if (!string.IsNullOrEmpty(value))
                {
                    Application.Current.Dispatcher.InvokeAsync(async () => AllGenres = await _db.GetGenres(value));
                }
            }
        }
        private string _searchSeries;
        public string SearchSeries
        {
            get => _searchSeries;
            set
            {
                SetProperty(ref _searchSeries, value);
                if (!string.IsNullOrEmpty(value))
                {
                    Application.Current.Dispatcher.InvokeAsync(async () => AllSeries = await _db.GetSeries(value));
                }
            }
        }
        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

        public ICommand CmdSave { get; private set; }

        public ICommand CmdRemove { get; private set; }
        public ICommand CmdAdd { get; private set; }
        private AvEditorViewModel(AvMovie movie)
        {
            Movie = movie.MovieInfo;

            CmdAdd = new RelayCommand<object>(OnAdd);
            CmdRemove = new RelayCommand<object>(OnRemove);
        }

        private async Task<AvEditorViewModel> InitializeAsync()
        {
            //(await _db.GetMakers(Movie)).ForEach(Makers.Add);
            //(await _db.GetLabels(Movie)).ForEach(Labels.Add);
            (await _db.GetGenres(Movie)).ForEach(Genres.Add);
            (await _db.GetActors(Movie)).ForEach(Actors.Add);

            (await _db.GetMakers()).ForEach(AllMakers.Add);
            //(await _db.GetLabels()).ForEach(AllLabels.Add);
            (await _db.GetSeries()).ForEach(AllSeries.Add);
            (await _db.GetGenres()).ForEach(AllGenres.Add);
            //(await _db.GetActors()).ForEach(AllActors.Add);
            return this;
        }

        private void OnAdd(object item)
        {
            if (item is Maker maker)
            {
                _db.UpdateMaker(Movie, maker);
            }
            else if (item is Label label)
            {
                _db.UpdateLabel(Movie, label);
            }
            else if (item is Genre genre)
            {
                Movie.Genres.Add(genre);
                genre.Movies.Add(Movie);
            }
            else if (item is Actor actor)
            {
                _db.UpdateActor(Movie, actor);
                Actors = [.. Movie.Actors];
            }
            else if (item is Series series)
            {
                Movie.Series = series;
                series.Movies.Add(Movie);
            }
            OnPropertyChanged(nameof(Movie));
        }
        private void OnRemove(object item)
        {
            if (item is Genre genre)
            {
                genre.Movies.Remove(Movie);
                Movie.Genres.Remove(genre);
            }
            else if (item is Actor actor)
            {
                actor.Movies.Remove(Movie);
                Movie.Actors.Remove(actor);
            }
            OnPropertyChanged(nameof(Movie));
        }

        private void OnSave()
        {
        }
    }
}
