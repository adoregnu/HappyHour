using CefSharp.DevTools.CSS;
using HappyHour.Extension;
using HappyHour.Interfaces;
using HappyHour.Model;
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
        private string _searchPid;
        public string SearchPid
        {
            get => _searchPid;
            set
            {
                SetProperty(ref _searchPid, value);
                if (string.IsNullOrEmpty(value)) return;

                movies = NotifyTask.Create(_db.GetMoviesFast(value).AsTask());
                movies.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Result")
                    {
                        OnPropertyChanged(nameof(Movies));
                    }
                };
            }
        }

        public bool SelectMovie(string pid)
        {
            return false;
        }
    }
}
