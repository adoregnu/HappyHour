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
        private SpiderBase _selectedSpiderToChains;

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

        public SpiderBase SelectedSpiderToChains
        {
            get => _selectedSpiderToChains;
            set => SetProperty(ref _selectedSpiderToChains, value);
        }

        public IDialogService DialogService { get; set; }
    
        public ICommand CmdAddChain { get; set; }
        public ICommand CmdDelChain { get; set; }
        public ICommand CmdMoveUp { get; set; }
        public ICommand CmdMoveDown { get; set; }
        public SpiderSettingViewModel()
        {
            CmdAddChain = new RelayCommand(OnAddChain);
            CmdDelChain = new RelayCommand(OnDelChain,
                () => SelectedSpiderToChains != null);
            CmdMoveUp = new RelayCommand(OnMoveUp,
                () => SelectedSpiderToChains != null);
            CmdMoveDown = new RelayCommand(OnMoveDown,
                () => SelectedSpiderToChains != null);
        }

        void OnAddChain()
        {
            if (SelectedSpider != SelectedSpiderToChains)
            {
                SelectedSpider.SpiderChain.Add(SelectedSpiderToChains);
            }
        }
        void OnDelChain()
        {
            _selectedSpider.SpiderChain.Remove(SelectedSpiderToChains);
        }
        void OnMoveUp()
        {
            var idx = _selectedSpider.SpiderChain.IndexOf(SelectedSpiderToChains);
            _selectedSpider.SpiderChain.Move(idx, idx - 1);
        }
        void OnMoveDown()
        {
            var idx = _selectedSpider.SpiderChain.IndexOf(SelectedSpiderToChains);
            _selectedSpider.SpiderChain.Move(idx, idx + 1);

        }

    }
}
