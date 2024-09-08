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
using System.Runtime.InteropServices;
using AsyncAwaitBestPractices.MVVM;
using HappyHour.Extension;

namespace HappyHour.ViewModel
{
    internal class AvEditorViewModel : ObservableObject, IModalDialogViewModel
    {
        public static Task<AvEditorViewModel> CreateAsync(AvMovie am)
        {
            var ret = new AvEditorViewModel(am);
            return ret.InitializeAsync();
        }

        private readonly MovieDbContext _db;// = App.Current.DbContext;

        private bool? _dialogResult;

        private List<Actor> _allActors = [];
        private List<Label> _labels = [];
        private List<Series> _allSereis = [];
        private List<Genre> _allGenres = [];
        private List<Maker> _allMakers = [];
        private Maker _selectedMaker;

        public Movie Movie { get; private set; }
        public List<Genre> Genres { get; private set; } = [];
        public ObservableCollection<Actor> Actors { get; set; } = [];

        public List<Maker> AllMakers
        {
            get => _allMakers;
            set => SetProperty(ref _allMakers, value); 
        } 
        public List<Label> Labels
        {
            get => _labels;
            set => SetProperty(ref _labels, value);
        }
        public List<Series> AllSeries
        {
            get => _allSereis;
            set => SetProperty(ref _allSereis, value); 
        } 
        public List<Genre> AllGenres
        {
            get => _allGenres;
            set => SetProperty(ref _allGenres, value);
        }
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
                    var actors = NotifyTask.Create(_db.GetActors(value).AsTask());
                    actors.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "Result")
                            AllActors = actors.Result;
                    };
                    //Application.Current.Dispatcher.InvokeAsync(async () => AllActors = await _db.GetActors(value));
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
                    Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        Labels = await _db.GetLabels(value);
                    });
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
        public IAsyncCommand<object> CmdAdd { get; private set; }
        public ICommand CmdClosed { get; private set; }
        readonly Movie _movie = null;
        private AvEditorViewModel(AvMovie movie)
        {
            _db = new();
            _movie = movie.MovieInfo;

            CmdAdd = new AsyncCommand<object>(OnAdd);
            CmdRemove = new RelayCommand<object>(OnRemove);
            CmdClosed = new RelayCommand(OnClose);
        }

        private async Task<AvEditorViewModel> InitializeAsync()
        {
            Movie = await _db.GetMovie(_movie.PID, true);
            //(await _db.GetMakers(Movie)).ForEach(Makers.Add);
            //(await _db.GetLabels(Movie)).ForEach(Labels.Add);
            (await _db.GetGenres(Movie)).ForEach(Genres.Add);
            (await _db.GetActors(Movie)).ForEach(Actors.Add);

            //(await _db.GetMakers()).ForEach(AllMakers.Add);
            //(await _db.GetLabels()).ForEach(AllLabels.Add);
            //(await _db.GetSeries()).ForEach(AllSeries.Add);
            //(await _db.GetGenres()).ForEach(AllGenres.Add);
            //(await _db.GetActors()).ForEach(AllActors.Add);
            return this;
        }

        private async Task OnAdd(object item)
        {
            if (item is Maker maker)
            {
                _db.UpdateMaker(Movie, maker);
            }
            else if (item is Label label)
            {
                await _db.UpdateLabel(Movie, label);
            }
            else if (item is Genre genre)
            {
                Movie.Genres.Add(genre);
                genre.Movies.Add(Movie);
            }
            else if (item is Actor actor)
            {
                await _db.UpdateActor(Movie, actor);
                Actors.Clear();
                foreach (var newActor in Movie.Actors)
                {
                    Actors.Add(newActor);
                }
            }
            else if (item is Series series)
            {
                await _db.UpdateSeries(Movie, series);
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
                _db.RemoveActor(Movie, actor);
                Actors.Remove(actor);
            }
        }

        private void OnSave()
        {
        }
        private void OnClose()
        {
            _db.Dispose();
        }
    }
}
