using HappyHour.Interfaces;
using HappyHour.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyHour.ViewModel
{
    partial class DbViewModel : Pane, IDbView
    {
        public List<Movie> Movies { get; set; } = [];

        Movie _selectedMovie;
        public Movie SelectedMovie
        {
            get => _selectedMovie;
            set
            {
                SetProperty(ref _selectedMovie, value);
                if (value == null) return;

                MediaList.AddMedia(value);
            }
        }

        private void OnSelectMovies()
        {
            Movies.Clear();
        }

        private async void OnSearchMovies()
        {
            Movies = await _db.GetMovies(SearchText);
        }

        public bool SelectMovie(string pid)
        {
            return false;
        }
    }
}
