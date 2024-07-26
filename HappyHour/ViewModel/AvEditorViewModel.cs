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

        private List<Actor> _allActors = [];

        public Movie Movie { get; private set; }
        public string Title { get; private set; }

        public List<Maker> Makers { get; private set; }
        public List<Label> Labels { get; private set; }
        public List<Series> Series { get; private set; }
        public List<Genre> Genres { get; private set; }
        public List<Actor> Actors { get; set; } = [];

        public List<Maker> AllMakers { get; private set; }
        public List<Label> AllLabels { get; private set; }
        public List<Series> AllSeries { get; private set; }
        public List<Genre> AllGenres { get; private set; }
        public List<Actor> AllActors
        {
            get => _allActors;
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                //OnPropertyChanged(nameof(AllActors));
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
            Title = movie.Pid;

            CmdAdd = new RelayCommand<object>(OnAdd);
            CmdRemove = new RelayCommand<object>(OnRemove);
        }

        private async Task<AvEditorViewModel> InitializeAsync()
        {
            (await _db.GetMakers(Movie)).ForEach(Makers.Add);
            (await _db.GetLabels(Movie)).ForEach(Labels.Add);
            (await _db.GetGenres(Movie)).ForEach(Genres.Add);
            (await _db.GetActors(Movie)).ForEach(Actors.Add);

            (await _db.GetMakers()).ForEach(AllMakers.Add);
            (await _db.GetLabels()).ForEach(AllLabels.Add);
            (await _db.GetGenres()).ForEach(AllGenres.Add);
            //(await _db.GetActors()).ForEach(AllActors.Add);

            return this;
        }

        private void OnAdd(object item)
        {
            if (item is Maker maker)
            {
                Movie.Maker = maker;
                if (!maker.Labels.Contains(Movie.Label))
                {
                    maker.Labels.Add(Movie.Label);
                }
            }
            else if (item is Label label)
            {
                Movie.Label = label;
                label.Movies.Add(Movie);
                if (!Movie.Maker.Labels.Contains(label))
                {
                    Movie.Maker.Labels.Add(label);
                }
            }
            else if (item is Genre genre)
            {
                Movie.Genres.Add(genre);
                genre.Movies.Add(Movie);
            }
            else if (item is Actor actor)
            {
                Movie.Actors.Add(actor);
                actor.Movies.Add(Movie);
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
