using HappyHour.Interfaces;
using HappyHour.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HappyHour.ViewModel
{
    partial class DbViewModel : Pane, IDbView
    {
        Genre _selectedGenre;
        public Genre SelectedGenre
        {
            get => _selectedGenre;
            set
            {
                SetProperty(ref _selectedGenre, value);
                GenreNames = value.Name;
            }
        }
        public ObservableCollection<Genre> Genres { get; set; } = [];

        IEnumerable<ShortText> _genreNames;
        public IEnumerable<ShortText> GenreNames 
        {
            get => _genreNames;
            set => SetProperty(ref _genreNames, value);
        }

        public ICommand CmdGenresMerge { get; private set; }
        public ICommand CmdGenreDoubleClicked { get; private set; }

        private async void OnGenreDoubleClicked()
        {
            _mediaList.LoadItems(await _db.GetMovies(SelectedGenre));
        }

        void OnMergeGenres(object p)
        {
            var selectedGenres = (p as IList<object>).Select(o => o as Genre).ToList();
            _db.MergeGenres(selectedGenres, g => Genres.Remove(g));
        }

        private async Task OnSelectGenres()
        {
            Genres.Clear();
            var list = await _db.GetGenres();
            list.ForEach(Genres.Add);
        }
    }
}
