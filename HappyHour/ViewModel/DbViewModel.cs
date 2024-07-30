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
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading;

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
#if false
                _mediaList.ItemSelectedHandler += (o, i) =>
                {
                    if (i == null) return;
                    if (SelectedType != "Movies")
                    {
                        SelectedType = "Movies";
                    }
                    SearchText = i.Pid;
                };
#endif
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                if (ListType.Any(type => type == SelectedType))
                {
                    //OnSearchTextUpdated(type);
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
                OnTypeChanged(value);
            }
        }
        public List<string> ListType { get; set; } = [
                nameof(Movies), nameof(Makers), nameof(Series), nameof(Genres)
        ];

       public ICommand CmdReload { get; private set; }
       public ICommand CmdRemove { get; private set; }
        public ICommand CmdReloadAll { get; private set; }
        public DbViewModel(IMainView mainView) : base(mainView) 
        {
            Title = "Database";
            //ListType = [.. _typeToPropertyName.Keys];
            CmdReload = new RelayCommand(async () => await OnTypeChanged(SelectedType));
            CmdReloadAll = new RelayCommand(async () => await OnReloadAll());
            CmdRemove = new RelayCommand<object>(OnRemove);

            CmdGenresMerge = new RelayCommand<object>(
                OnMergeGenres, p => p is IList<object> list && list.Count > 1);
            CmdMergeMakers = new RelayCommand<object>(
                OnMergeMakers, p => p is IList<object> list && list.Count > 1);
            CmdMergeLables = new RelayCommand<object>(
                OnMergeLabels, p => p is IList<object> list && list.Count > 1);
            CmdMergeSeries = new RelayCommand<object>(
                OnMergeSeries, p => p is IList<object> list && list.Count > 1);

            CmdGenreDoubleClicked = new RelayCommand(OnGenreDoubleClicked);
            CmdLabelDoubleClicked = new RelayCommand(OnLabelDoubleClicked);
            CmdMakerDoubleClicked = new RelayCommand(OnMakerDoubleClicked);
            CmdSeriesoDubleClicked = new RelayCommand(OnSeriesoDubleClicked);

            CmdAddSeriesNameTranslated = new RelayCommand(OnAddSeriesNameTranslated);
            MainView.ViewEventHandler += ReceiveAsync;
        }

        async void ReceiveAsync(object sender, ViewEventArgs e)
        {
            if (e.Message != "Refresh")
            {
                return;
            }
            Log.Print($"DbViewModel received {e.Message}");
            await OnReloadAll();
        }

        private async Task OnReloadAll()
        {
            foreach (var type in ListType)
            {
                await OnTypeChanged(type);
            }
        }

        private async Task OnTypeChanged(string type)
        {
            string changeFunction = $"OnSelect{type}";
            MethodInfo mi = GetType().GetMethod(changeFunction,
                BindingFlags.NonPublic | BindingFlags.Instance);
            var task = (Task)mi?.Invoke(this, null);
            if (task != null)
            {
                await task;
            }
        }

        private void OnSearchTextUpdated(string type)
        {
            string searchFunction = $"OnSearch{SelectedType}";
            MethodInfo mi = GetType().GetMethod(searchFunction,
                BindingFlags.NonPublic | BindingFlags.Instance);
            mi?.Invoke(this, null);
        }

        private async void OnRemove(object item)
        {
            if (item is Maker maker)
            {
                _db.RemoveMaker(maker);
                await OnSelectMakers();
            }
            else if (item is Label label)
            {
                _db.RemoveLabel(label);
                Labels.Remove(label);
            }
            else if (item is Series series)
            {
                _db.RemoveSeries(series);
                Series.Remove(series);
            }
        }
    }
}
