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
        public ObservableCollection<Maker> Makers { get; set; } = [];
        public ObservableCollection<Label> Labels { get; set; } = [];

        private Maker _selectedMaker;
        public Maker SelectedMaker
        {
            get => _selectedMaker;
            set
            {
                SetProperty(ref _selectedMaker, value);
                if (value == null) return;

                //var movies = _dbPoll.GetAvMovies(value);
                //MediaList.LoadItems(movies);
            }
        }
        private Label _selectedLabel;
        public Label SelectedLabel
        {
            get => _selectedLabel;
            set
            {
                SetProperty(ref _selectedLabel, value);
                if (value == null) return;

                //var movies = _dbPoll.GetAvMovies(value);
                //MediaList.LoadItems(movies);
            }
        }
        private async void OnSelectMakers()
        {
            Makers.Clear();
            Labels.Clear();
            var mlist = await _db.GetMakers();
            mlist.ForEach(Makers.Add);

            var llist = await _db.GetLabels();
            llist.ForEach(Labels.Add);
        }
    }
}
