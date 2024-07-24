using System.Collections.Generic;

using MvvmDialogs;

using HappyHour.Spider;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Linq;
using HappyHour.Extension;

namespace HappyHour.ViewModel
{
    internal class SpiderSettingViewModel : ObservableObject, IModalDialogViewModel
    {
        private SpiderBase _selectedSpider;
        private SpiderBase _selectedSpiderToChain;
        private SpiderBase _selectedSpiderFromChain;

        public bool? DialogResult { get; set; }
        public List<SpiderBase> Spiders { get; set; }
        public SpiderBase SelectedSpider
        {
            get => _selectedSpider;
            set
            {
                SetProperty(ref _selectedSpider, value);
                value?.UpdateCheckAll();
            }
        }

        public SpiderBase SelectedSpiderToChain
        {
            get => _selectedSpiderToChain;
            set => SetProperty(ref _selectedSpiderToChain, value);
        }
        public SpiderBase SelectedSpiderFromChain
        {
            get => _selectedSpiderFromChain;
            set => SetProperty(ref _selectedSpiderFromChain, value);
        }
        public IDialogService DialogService { get; set; }
    
        public ICommand CmdAddChain { get; set; }
        public ICommand CmdDelChain { get; set; }
        public ICommand CmdMoveUp { get; set; }
        public ICommand CmdMoveDown { get; set; }
        public SpiderSettingViewModel()
        {
            CmdAddChain = new RelayCommand(OnAddChain);
            CmdDelChain = new RelayCommand(OnDelChain);
            CmdMoveUp = new RelayCommand(OnMoveUp);
            CmdMoveDown = new RelayCommand(OnMoveDown);
        }

        void OnAddChain()
        {
            if (SelectedSpiderToChain != null && !SelectedSpider.SpiderChain.Any(s => s == SelectedSpiderToChain))
            {
                SelectedSpider.SpiderChain.Add(SelectedSpiderToChain);
            }
        }
        void OnDelChain()
        {
            if (_selectedSpiderFromChain == null) return;

            _selectedSpider.SpiderChain.Remove(SelectedSpiderFromChain);
        }
        void OnMoveUp()
        {
            if (_selectedSpiderFromChain == null) return;

            var idx = _selectedSpider.SpiderChain.IndexOf(SelectedSpiderFromChain);
            if (idx > 0)
            {
                _selectedSpider.SpiderChain.Move(idx, idx - 1);
            }
        }
        void OnMoveDown()
        {
            if (_selectedSpiderFromChain == null) return;

            var idx = _selectedSpider.SpiderChain.IndexOf(SelectedSpiderFromChain);
            if (idx < _selectedSpider.SpiderChain.Count - 1)
            {
                _selectedSpider.SpiderChain.Move(idx, idx + 1);
            }
        }

    }
}
