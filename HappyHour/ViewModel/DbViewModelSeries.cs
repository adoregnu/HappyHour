using AsyncAwaitBestPractices.MVVM;
using HappyHour.Interfaces;
using HappyHour.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HappyHour.ViewModel
{
    partial class DbViewModel : Pane, IDbView
    {
        Series _selectedSeries;
        public Series SelectedSeries
        {
            get => _selectedSeries;
            set => SetProperty(ref _selectedSeries, value);
        }

        private string _searchSeries;
        public string SearchSeries
        {
            get => _searchSeries;
            set
            {
                SetProperty(ref _searchSeries, value);
                Series.Clear();
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        var series = await _db.GetSeries(value);
                        series.ForEach(Series.Add);
                    }
                });
            }
        }

        private string _seriesNameTranslated;
        public string SeriesNameTranslated
        {
            get => _seriesNameTranslated;
            set => SetProperty(ref _seriesNameTranslated, value);
        }

        public ObservableCollection<Series> Series { get; set; } = [];

        public ICommand CmdAddSeriesNameTranslated { get; private set; }
        public IAsyncCommand CmdSeriesoDubleClicked { get; private set; }
        public IAsyncCommand<object> CmdMergeSeries { get; private set; }

        void OnAddSeriesNameTranslated()
        {
            if (SelectedSeries == null) return;
            SelectedSeries.Name.Add(new ShortText() { Lang = "ko", Text = SeriesNameTranslated });
        }

        async Task OnSeriesoDubleClicked()
        {
            UiServices.WaitCursor(true);
            await _mediaList.LoadItems(await _db.GetMovies(SelectedSeries));
            UiServices.WaitCursor(false);
        }

        async Task OnMergeSeries(object p)
        {
            var selectedSeries = (p as IList<object>).Select(o => o as Series).ToList();
            await _db.MergeSeries(selectedSeries, s => Series.Remove(s));
        }

        private async Task OnSelectSeries()
        {
            Series.Clear();
            var list = await _db.GetSeries();
            list.ForEach(Series.Add);
        }
    }
}
