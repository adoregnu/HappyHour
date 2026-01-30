using AsyncAwaitBestPractices.MVVM;
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
using CommunityToolkit.Mvvm.Input;
using HappyHour.Extension;
using System.ComponentModel;

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
        private ObservableCollection<Maker> _makersToMerge = [];
        public ObservableCollection<Maker> MakersToMerge
        {
            get => _makersToMerge;
            set => SetProperty(ref _makersToMerge, value);
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
        private Maker _selectedMakerToMerge;
        public Maker SelectedMakerToMerge
        {
            get => _selectedMakerToMerge;
            set => SetProperty(ref _selectedMakerToMerge, value);
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
                RefreshItems( _db.GetMakers(value), Makers);
            }
        }
        private string _searchLabel;
        public string SearchLabel
        {
            get => _searchLabel;
            set
            {
                SetProperty(ref _searchLabel, value);
                RefreshItems(_db.GetLabels(value), Labels);
            }
        }

        public ICommand CmdMergeMakers { get; private set; }
        public ICommand CmdMoveMakerLeft { get; private set; }
        public ICommand CmdMoveMakerRight { get; private set; }
        public IAsyncCommand<object, object> CmdMergeLables { get; private set; }
        public IAsyncCommand CmdLabelDoubleClicked { get; private set; }
        public IAsyncCommand CmdMakerDoubleClicked { get; private set; }

        void InitMakers()
        {
            CmdMergeMakers = new RelayCommand<object>(
                OnMergeMakers, p => p is IList<object> list && list.Count > 1);
            CmdMergeLables = new AsyncCommand<object, object>(
                OnMergeLabels, p => p is IList<object> list && list.Count > 1);
            CmdLabelDoubleClicked = new AsyncCommand(OnLabelDoubleClicked);
            CmdMakerDoubleClicked = new AsyncCommand(OnMakerDoubleClicked);
            CmdMoveMakerLeft = new RelayCommand(OnMoveMakerLeft);
            CmdMoveMakerRight = new RelayCommand(OnMoveMakerRight);
        }

        void OnMoveMakerRight()
        {
            if (SelectedMaker != null)
            {
                MakersToMerge.Add(SelectedMaker);
                Makers.Remove(SelectedMaker);
            }
        }
        void OnMoveMakerLeft()
        {
            if (SelectedMakerToMerge != null)
            {
                Makers.Add(SelectedMakerToMerge);
                MakersToMerge.Remove(SelectedMakerToMerge);
            }
        }

        void OnMergeMakers(object m)
        {
            var selectedMakers = (m as IList<object>).Select(o => o as Maker).ToList();
            _db.MergeMakers(selectedMakers, m => MakersToMerge.Remove(m));
        }
        async Task OnMergeLabels(object m)
        {
            var selectedLabels = (m as IList<object>).Select(o => o as Label).ToList();
            await _db.MargeLabels(selectedLabels, m => Labels.Remove(m));
        }

        private async Task OnLabelDoubleClicked()
        {
            UiServices.WaitCursor(true);
            await _mediaList.LoadItems(await _db.GetMovies(SelectedLabel));
            UiServices.WaitCursor(false);
        }

        private async Task OnMakerDoubleClicked()
        {
            UiServices.WaitCursor(true);
            await _mediaList.LoadItems(await _db.GetMovies(SelectedMaker));
            UiServices.WaitCursor(false);
        }

        private async Task OnSelectMakers()
        {
            Makers.Clear();
            Labels.Clear();
            MakersToMerge.Clear();
            var makers = await _db.GetMakers();
            makers.ForEach(Makers.Add);

            //var llist = await _db.GetLabels();
            //llist.ForEach(Labels.Add);
        }
    }
}
