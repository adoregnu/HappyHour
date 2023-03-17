using System.Collections.Generic;

using MvvmDialogs;

using HappyHour.Spider;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HappyHour.ViewModel
{
    internal class SpiderSettingViewModel : ObservableObject, IModalDialogViewModel
    {
        private SpiderBase _selectedSpider;

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

        public IDialogService DialogService { get; set; }
    }
}
