using HappyHour.Interfaces;
using HappyHour.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HappyHour.ViewModel
{
    partial class DbViewModel : Pane, IDbView
    {
        public ObservableCollection<Movie> Movies { get; set; } = [];

        Movie _selectedMovie;
        public Movie SelectedMovie
        {
            get => _selectedMovie;
            set
            {
                SetProperty(ref _selectedMovie, value);
                if (value == null) return;

                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    UiServices.WaitCursor(true);
                    MediaList.AddMedia(await _db.GetMovie(value.PID, false));
                    UiServices.WaitCursor(false);
                });
            }
        }
        private string _searchPid;
        public string SearchPid
        {
            get => _searchPid;
            set
            {
                SetProperty(ref _searchPid, value);
                Movies.Clear();
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        var movies = await _db.GetMoviesFast(value);
                        movies.ForEach(Movies.Add);
                    }
                });
            }
        }

        public bool SelectMovie(string pid)
        {
            return false;
        }
    }
}
