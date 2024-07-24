using HappyHour.Interfaces;
using HappyHour.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private string _seriesNameTranslated;
        public string SeriesNameTranslated
        {
            get => _seriesNameTranslated;
            set => SetProperty(ref _seriesNameTranslated, value);
        }

        public ObservableCollection<Series> Series { get; set; } = [];

        public ICommand CmdAddSeriesNameTranslated { get; private set; }
        public ICommand CmdSeriesoDubleClicked { get; private set; }
        public ICommand CmdMergeSeries { get; private set; }

        void OnAddSeriesNameTranslated()
        {
            if (SelectedSeries == null) return;
            SelectedSeries.Name.Add(new ShortText() { Lang = "ko", Text = SeriesNameTranslated });
        }

        async void OnSeriesoDubleClicked()
        {
            _mediaList.LoadItems(await _db.GetMovies(SelectedSeries));
        }

        void OnMergeSeries(object p)
        {
            var selectedSeries = (p as IList<object>).Select(o => o as Series).ToList();
            _db.MergeSeries(selectedSeries, s => Series.Remove(s));
        }

        private async void OnSelectSeries()
        {
            Series.Clear();
            var list = await _db.GetSeries();
            foreach (var item in list)
            {
                Log.Print($"{item.Name.First()}");
            }
            list.ForEach(Series.Add);
        }
    }
}
