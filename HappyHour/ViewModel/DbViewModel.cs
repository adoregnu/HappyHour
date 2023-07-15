using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using HappyHour.Model;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class DbViewModel : Pane, IDbView
    {
        string _selectedType = "Pid";
        string _searchText;

        IMediaList _mediaList;
        readonly AvDbContextPool _dbPoll;

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
                    if (SelectedType != "Pid")
                    {
                        SelectedType = "Pid";
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
                if (_typeToPropertyName.ContainsKey(SelectedType))
                    OnPropertyChanged(_typeToPropertyName[SelectedType]);
            }
        }
        public string SelectedType
        {
            get => _selectedType;
            set
            {
                SearchText = "";
                SetProperty(ref _selectedType, value);
            }
        }
        public List<string> ListType { get; set; }

        public IEnumerable<AvItem> AvItemList => _dbPoll.GetAvMovies(SearchText);
        public IEnumerable<AvActorName> AvActorNameList => _dbPoll.GetActorNames(SearchText);
        public IEnumerable<AvStudio> AvStudioList => _dbPoll.GetAvStudios(SearchText);
        public IEnumerable<AvSeries> AvSeriesList  =>  _dbPoll.GetAvSeries(SearchText);

        AvItem _selectedAvItem;
        public AvItem  SelectedItem
        {
            get => _selectedAvItem;
            set
            {
                SetProperty(ref _selectedAvItem, value);
                if (value == null) return;

                MediaList.AddMedia(value.Path);
            }
        }

        AvActorName _selectedActorName;
        public AvActorName  SelectedActorName
        {
            get => _selectedActorName;
            set
            {
                SetProperty(ref _selectedActorName, value);
                if (value == null) return;

                var movies = _dbPoll.GetAvMovies(value);
                MediaList.LoadItems(movies);
            }
        }
        AvStudio _selectedStudio;
        public AvStudio SelectedStudio
        {
            get => _selectedStudio;
            set
            {
                SetProperty(ref _selectedStudio, value);
                if (value == null) return;

                var movies = _dbPoll.GetAvMovies(value);
                MediaList.LoadItems(movies);
            }
        }

        AvSeries _selectedSeries;
        public AvSeries SelectedSeries
        {
            get => _selectedSeries;
            set
            {
                SetProperty(ref _selectedSeries, value);
                if (value == null) return;

                var movies = _dbPoll.GetAvMovies(value);
                MediaList.LoadItems(movies);
            }
        }

        readonly Dictionary<string, string> _typeToPropertyName = new ()
            {
                { "Pid", nameof(AvItemList) },
                { "Actor", nameof(AvActorNameList) },
                { "Studio", nameof(AvStudioList) },
                { "Series", nameof(AvSeriesList) },
            };

        public DbViewModel()
        {
            Title = "Database";
            ListType = _typeToPropertyName.Keys.ToList();
            _dbPoll = new AvDbContextPool();
        }

        public bool SelectPid(string pid)
        {
            var movies = _dbPoll.GetAvMovies(pid);
            if (movies.Any())
            {
                MediaList.AddMedia(movies.First().Path);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
