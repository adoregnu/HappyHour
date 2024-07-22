using System.Collections.Generic;

using MvvmDialogs;

using HappyHour.Spider;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Linq;

namespace HappyHour.ViewModel
{
    internal class SpiderSettingViewModel : ObservableObject, IModalDialogViewModel
    {
        private SpiderBase _selectedSpider;
        private SpiderBase _selectedChain;

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

        public SpiderBase SelectedChain
        {
            get => _selectedChain;
            set => SetProperty(ref _selectedChain, value);
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
        }
        void OnDelChain()
        {
        }
        void OnMoveUp()
        {
        }
        void OnMoveDown()
        {
        }

    }
}
