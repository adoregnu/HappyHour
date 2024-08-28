using AsyncAwaitBestPractices.MVVM;
using HappyHour.Interfaces;
using HappyHour.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HappyHour.ViewModel
{
    partial class DbViewModel : Pane, IDbView
    {
        private Genre _selectedGenre;
        private Genre _selectedGenreToMerge;
        public Genre SelectedGenre
        {
            get => _selectedGenre;
            set => SetProperty(ref _selectedGenre, value);
        }
        public Genre SelectedGenreToMerge
        {
            get => _selectedGenreToMerge;
            set => SetProperty(ref _selectedGenreToMerge, value);
        }
        public ObservableCollection<Genre> Genres { get; set; } = [];
        public ObservableCollection<Genre> GenresToMerge { get; set; } = [];

        private string _searchGenre;
        public string SearchGenre
        {
            get => _searchGenre;
            set
            {
                SetProperty(ref _searchGenre, value);
                Genres.Clear();
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var keyword = string.IsNullOrEmpty(value) ? null : value;
                    var genres = await _db.GetGenres(keyword);
                    genres.ForEach(Genres.Add);
                });
            }
        }
        public IAsyncCommand<object> CmdGenresMerge { get; private set; }
        public ICommand CmdMoveDownSelectedGenre { get; private set; }
        public ICommand CmdMoveUpSelectedGenre { get; private set; }
        public IAsyncCommand<object> CmdGenreDoubleClicked { get; private set; }

        private async Task OnGenreDoubleClicked(object obj)
        {
            if (obj is Genre genre)
            {
                UiServices.WaitCursor(true);
                await _mediaList.LoadItems(await _db.GetMovies(genre));
                UiServices.WaitCursor(false);
            }
        }

        async Task OnMergeGenres(object p)
        {
            var selectedGenres = (p as IList<object>).Select(o => o as Genre).ToList();
            await _db.MergeGenres(selectedGenres, g => GenresToMerge.Remove(g));
        }

        async Task OnRemoveGenre(object p)
        {
            if (p is Genre genre)
            {
                await _db.RemoveGenre(genre);
            }
        }

        private async Task OnSelectGenres()
        {
            Genres.Clear();
            GenresToMerge.Clear();
            var list = await _db.GetGenres();
            list.ForEach(Genres.Add);
        }
        private void OnMoveDownGenre()
        {
            if (SelectedGenre != null)
            {
                GenresToMerge.Add(SelectedGenre);
                Genres.Remove(SelectedGenre);
            }
        }
        private void OnMoveUpGenre()
        {
            if (SelectedGenreToMerge != null)
            {
                Genres.Add(SelectedGenreToMerge);
                GenresToMerge.Remove(SelectedGenreToMerge);
            }
        }
    }
}
