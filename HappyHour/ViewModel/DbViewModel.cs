using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using HappyHour.Model;
using HappyHour.Interfaces;
using System.Collections.ObjectModel;
using HappyHour.Extension;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;

namespace HappyHour.ViewModel
{

    partial class DbViewModel : Pane, IDbView
    {
        string _selectedType = "Movies";
        string _searchText;

        IMediaList _mediaList;
        private readonly MovieDbContext _db =  App.Current.DbContext;

        public IMediaList MediaList
        {
            get => _mediaList;
            set
            {
                if (_mediaList != null) return;

                SetProperty(ref _mediaList, value);
                _mediaList.ItemSelectedHandler += (o, i) =>
                {
                    if (i == null) return;
                    if (SelectedType != "Movies")
                    {
                        SelectedType = "Movies";
                    }
                    SearchText = i.Pid;
                };
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                if (_typeToPropertyName.TryGetValue(SelectedType, out string type))
                {
                    OnPropertyChanged(type);
                }
            }
        }
        public string SelectedType
        {
            get => _selectedType;
            set
            {
                SearchText = "";
                SetProperty(ref _selectedType, value);
                OnTypeChanged();
            }
        }
        public List<string> ListType { get; set; }

        public ObservableCollection<Actor> Actors { get; set; } = [];
        public ObservableCollection<Movie> Movies { get; set; } = [];

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

        ActorName _selectedName;
        public ActorName  SelectedName
        {
            get => _selectedName;
            set
            {
                SetProperty(ref _selectedName, value);
                if (value == null) return;

                //var movies = _dbPoll.GetAvMovies(value);
                //MediaList.LoadItems(movies);
            }
        }
        readonly Dictionary<string, string> _typeToPropertyName = new ()
            {
                { "Movies", nameof(Movies) },
                { "Actors", nameof(Actors) },
                { "Makers", nameof(Makers) },
                { "Series", nameof(Series) },
                { "Genres", nameof(Genres) },
            };

        public DbViewModel()
        {
            Title = "Database";
            ListType = _typeToPropertyName.Keys.ToList();

            CmdGenresMerge = new RelayCommand<object>(
                OnMergeGenres, p => p is IList<object> list && list.Count > 1);
            CmdGenreDoubleClick = new RelayCommand(OnGenreDoubleClicked);
        }

        private void OnTypeChanged()
        {
            string changeFunction = $"OnSelect{SelectedType}";
            MethodInfo mi = GetType().GetMethod(changeFunction,
                BindingFlags.NonPublic | BindingFlags.Instance);
            mi?.Invoke(this, null);

        }

        public bool SelectMovie(string pid)
        {
#if false
            //var movies = _dbPoll.GetAvMovies(pid);
            if (movies.Any())
            {
                MediaList.AddMedia(movies.First().Path);
                return true;
            }
            else
#endif
            {
                return false;
            }
        }
    }
}
