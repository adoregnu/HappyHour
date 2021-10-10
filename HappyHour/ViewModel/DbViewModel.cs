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

        public IMediaList MediaList
        {
            get => _mediaList;
            set
            {
                if (_mediaList != null) return;

                Set(ref _mediaList, value);
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
                Set(ref _searchText, value);
                if (_typeToPropertyName.ContainsKey(SelectedType))
                    RaisePropertyChanged(_typeToPropertyName[SelectedType]);
            }
        }
        public string SelectedType
        {
            get => _selectedType;
            set
            {
                SearchText = "";
                Set(ref _selectedType, value);
            }
        }
        public List<string> ListType { get; set; }

        public IEnumerable<AvItem> AvItemList
        { 
            get
            {
                if (string.IsNullOrEmpty(SearchText))
                    return null;

                return App.DbContext.Items
                    .Where(i => EF.Functions.Like(i.Pid, $"%{SearchText}%"))
                    .ToList();
            }
        }
        public IEnumerable<AvActorName> AvActorNameList
        {
            get
            { 
                if (string.IsNullOrEmpty(SearchText))
                    return App.DbContext.ActorNames
                        .OrderBy(a => a.Name)
                        .ToList();

                return App.DbContext.ActorNames
                    .Where(i => EF.Functions.Like(i.Name, $"%{SearchText}%"))
                    .OrderBy(a => a.Name)
                    .ToList();
            }
        }
        public IEnumerable<AvStudio> AvStudioList
        {
            get
            {
                if (string.IsNullOrEmpty(SearchText))
                    return App.DbContext.Studios
                        .OrderBy(s => s.Name)
                        .ToList();

                return App.DbContext.Studios
                    .Where(s => EF.Functions.Like(s.Name, $"{SearchText}%"))
                    .OrderBy(s => s.Name)
                    .ToList();
            }
        }

        public IEnumerable<AvSeries> AvSeriesList
        {
            get
            {
                if (string.IsNullOrEmpty(SearchText))
                    return App.DbContext.Series
                        .ToList();

                return App.DbContext.Series
                    .Where(s => EF.Functions.Like(s.Name, $"%{SearchText}%"))
                    .ToList();
            }
        }

        AvItem _selectedAvItem;
        public AvItem  SelectedItem
        {
            get => _selectedAvItem;
            set
            {
                Set(ref _selectedAvItem, value);
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
                Set(ref _selectedActorName, value);
                if (value == null) return;

                App.DbContext.Entry(value)
                    .Reference(n => n.Actor).Load();
                App.DbContext.Entry(value.Actor)
                    .Collection(a => a.Items).Load();

                var paths = value.Actor.Items
                    .Select(i => i.Path).ToList();
                MediaList.Replace(paths);
            }
        }
        AvStudio _selectedStudio;
        public AvStudio SelectedStudio
        {
            get => _selectedStudio;
            set
            {
                Set(ref _selectedStudio, value);
                if (value == null) return;

                var paths = App.DbContext.Items
                    .Where(i => i.Studio == value)
                    .Select(i => i.Path)
                    .ToList();
                MediaList.Replace(paths);
            }
        }

        AvSeries _selectedSeries;
        public AvSeries SelectedSeries
        {
            get => _selectedSeries;
            set
            {
                Set(ref _selectedSeries, value);
                if (value == null) return;

                var paths = App.DbContext.Items
                    .Where(i => i.Series == value)
                    .Select(i => i.Path)
                    .ToList();
                MediaList.Replace(paths);
            }
        }

        readonly Dictionary<string, string> _typeToPropertyName
            = new Dictionary<string, string>
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
        }
    }
}
