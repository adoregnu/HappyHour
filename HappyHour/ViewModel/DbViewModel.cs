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
using AsyncAwaitBestPractices.MVVM;
using System.Windows;
using System.IO;

namespace HappyHour.ViewModel
{

    partial class DbViewModel : Pane, IDbView
    {
        string _searchText;

        private IMediaList _mediaList;
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
                    if (i != null)
                    {
                        SelectedSearchMovieType = "PID";
                        SearchMovieText = i.Pid;
                    }
                };
            }
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }
        private static void RefreshItems<T>(ValueTask<List<T>> task, ObservableCollection<T> target)
        {
            NotifyTask.Create(task.AsTask())
                .PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "Result")
                    {
                        target.Clear();
                        List<T> items = ((dynamic)s).Result;
                        items.ForEach(target.Add);
                        }
                };
        }

       public IAsyncCommand<object> CmdRemove { get; private set; }
        public IAsyncCommand CmdReloadAll { get; private set; }
        public IAsyncCommand CmdSaveAll { get; private set; }
        public DbViewModel(IMainView mainView) : base(mainView) 
        {
            Title = "Database";
            CmdReloadAll = new AsyncCommand(OnReloadAll);
            CmdRemove = new AsyncCommand<object>(OnRemove);
            CmdSaveAll = new AsyncCommand(OnSaveAll);

            InitMakers();
            InitMovie();
            InitGenres();

            CmdMergeSeries = new AsyncCommand<object>(
                OnMergeSeries, p => p is IList<object> list && list.Count > 1);

            CmdSeriesoDubleClicked = new AsyncCommand(OnSeriesoDubleClicked);

            CmdAddSeriesNameTranslated = new RelayCommand(OnAddSeriesNameTranslated);
            //MainView.OnViewUpdate += ReceiveAsync;

        }

        async void ReceiveAsync(ViewEventArgs e)
        {
            if (e.Message != "Refresh")
            {
                return;
            }
            await OnReloadAll();
        }

        private async Task OnSaveAll()
        {
            await _db.SaveChangesAsync();
            //await _db.RemoveTextFromLongText();
        }

        private async Task OnReloadAll()
        {
            List<string> ListType = [
                nameof(Movies),
                nameof(Makers),
                nameof(Series),
                nameof(Genres)
            ];
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

        private async Task OnRemove(object item)
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
            else if (item is Genre genre)
            {
                await _db.RemoveGenre(genre);
                Genres.Remove(genre);
            }
        }
    }
}
