using System.Collections.Generic;

using MvvmDialogs;

using GalaSoft.MvvmLight;

using HappyHour.Spider;

namespace HappyHour.ViewModel
{
    internal class SpiderSettingViewModel : ViewModelBase, IModalDialogViewModel
    {
        private SpiderBase _selectedSpider;

        public bool? DialogResult { get; set; }
        public List<SpiderBase> Spiders { get; set; }
        public SpiderBase SelectedSpider
        {
            get => _selectedSpider;
            set
            {
                Set(ref _selectedSpider, value);
                if (value != null) { value.UpdateCheckAll(); }
            }
        }

        public IDialogService DialogService { get; set; }
    }
}
