using CefSharp.DevTools.CSS;
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
using System.Windows.Markup;

namespace HappyHour.ViewModel
{
    partial class DbViewModel : Pane, IDbView
    {
        private ObservableCollection<Maker> _makers = [];
        public ObservableCollection<Maker> Makers
        {
            get => _makers;
            set => SetProperty(ref _makers, value);
        }
        public ObservableCollection<Label> Labels { get; set; } = [];

        private Maker _selectedMaker;
        public Maker SelectedMaker
        {
            get => _selectedMaker;
            set
            {
                SetProperty(ref _selectedMaker, value);
                if (value == null) return;

                Labels.Clear();

                _db.GetLabels(value).ForEach(Labels.Add);
            }
        }
        private Label _selectedLabel;
        public Label SelectedLabel
        {
            get => _selectedLabel;
            set => SetProperty(ref _selectedLabel, value);
        }

        private string _searchMaker;
        public string SearchMaker
        {
            get => _searchMaker;
            set
            {
                SetProperty(ref _searchMaker, value);
                if (!string.IsNullOrEmpty(value))
                {
                    Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        Makers.Clear();
                        var makers = await _db.GetMakers(value);
                        makers.ForEach(Makers.Add);
                    });
                }
            }
        }

        public ICommand CmdMergeMakers { get; private set; }
        public ICommand CmdMergeLables { get; private set; }
        public ICommand CmdLabelDoubleClicked { get; private set; }
        public ICommand CmdMakerDoubleClicked { get; private set; }

        void OnMergeMakers(object m)
        {
            var selectedMakers = (m as IList<object>).Select(o => o as Maker).ToList();
            _db.MergeMakers(selectedMakers, m => Makers.Remove(m));
            OnPropertyChanged(nameof(Makers));
        }
        void OnMergeLabels(object m)
        {
            var selectedLabels = (m as IList<object>).Select(o => o as Label).ToList();
            _db.MargeLabels(selectedLabels, m => Labels.Remove(m));
        }

        private async void OnLabelDoubleClicked()
        {
            _mediaList.LoadItems(await _db.GetMovies(SelectedLabel));
        }

        private async void OnMakerDoubleClicked()
        {
            _mediaList.LoadItems(await _db.GetMovies(SelectedMaker));
        }

        private async Task OnSelectMakers()
        {
            Makers.Clear();
            Labels.Clear();
            var makers = await _db.GetMakers();
            makers.ForEach(Makers.Add);

            //var llist = await _db.GetLabels();
            //llist.ForEach(Labels.Add);
        }
    }
}
