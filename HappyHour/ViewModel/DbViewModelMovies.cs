using AsyncAwaitBestPractices.MVVM;
using CefSharp.DevTools.CSS;
using HappyHour.Extension;
using HappyHour.Interfaces;
using HappyHour.Model;
using HappyHour.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HappyHour.ViewModel
{
    partial class DbViewModel : Pane, IDbView
    {
        private NotifyTask<List<Movie>> movies;
        public NotifyTask<List<Movie>> Movies => movies;

        public List<string> SearchMovieTypes => ["PID" ,"Title", "Plot"];
        public string _selectedSearchMovieType = "PID";
        public string SelectedSearchMovieType
        {
            get => _selectedSearchMovieType;
            set => SetProperty(ref _selectedSearchMovieType, value);
        }

        Movie _selectedMovie;
        public Movie SelectedMovie
        {
            get => _selectedMovie;
            set
            {
                SetProperty(ref _selectedMovie, value);
                if (value == null) return;

                var movie = NotifyTask.Create(_db.GetMovie(value.PID, false).AsTask());
                movie.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Result")
                    {
                        UiServices.WaitCursor(true);
                        MediaList.AddMedia(movie.Result);
                        UiServices.WaitCursor(false);
                    }
                };
             }
        }
        private string _searchMovieText;
        public string SearchMovieText
        {
            get => _searchMovieText;
            set
            {
                SetProperty(ref _searchMovieText, value);
                if (string.IsNullOrEmpty(value)) return;

                movies = NotifyTask.Create(_db.GetMoviesFast(value, SelectedSearchMovieType).AsTask());
                movies.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Result")
                    {
                        OnPropertyChanged(nameof(Movies));
                    }
                };
            }
        }
        public IAsyncCommand<object> CmdEditMovie { get; private set; }

        private void InitMovie()
        {
            CmdEditMovie = new AsyncCommand<object>(OnEditMovie);
        }

        async Task OnEditMovie(object obj)
        {
            if (obj is Movie movie)
            {
                UiServices.WaitCursor(true);
                var dialog = await AvEditorViewModel.CreateAsync(movie);
                UiServices.WaitCursor(false);
                MainView.DialogService.ShowDialog<AvEditorDialog>(this, dialog);
            }
        }

        public bool SelectMovie(string pid)
        {
            return false;
        }
    }
}
