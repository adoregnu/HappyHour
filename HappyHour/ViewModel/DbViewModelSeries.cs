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
        Series _selectedSeries;
        public Series SelectedSeries
        {
            get => _selectedSeries;
            set
            {
                SetProperty(ref _selectedSeries, value);
            }
        }

        public ObservableCollection<Series> Series { get; set; } = [];

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
