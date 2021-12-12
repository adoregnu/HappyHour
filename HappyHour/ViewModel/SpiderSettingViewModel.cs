using System.Collections.Generic;

using MvvmDialogs;

using GalaSoft.MvvmLight;

using HappyHour.Spider;

namespace HappyHour.ViewModel
{
    internal class SpiderSettingViewModel : ViewModelBase, IModalDialogViewModel
    {
        private SpiderBase _selectedSpider;
        private bool? _checkAll;

        public bool? DialogResult { get; set; }
        public bool? CheckAll
        {
            get => _checkAll;
            set
            {
                Set(ref _checkAll, value);
                UpdateCheck(value);
            }
        }
        public List<SpiderBase> Spiders { get; set; }
        public SpiderBase SelectedSpider
        {
            get => _selectedSpider;
            set
            {
                Set(ref _selectedSpider, value);
                UpdateCheckAll();
            }
        }

        public IDialogService DialogService { get; set; }

        private void UpdateCheck(bool? check)
        {
            if (check == null) { return; }
            SelectedSpider.ScrapItems.ForEach(i =>
            {
                i.CanUpdate = check.Value;
            });
        }

        public void UpdateCheckAll()
        {
            int checkCount = 0;
            SelectedSpider.ScrapItems.ForEach(i =>
            {
                if (i.CanUpdate) { checkCount++; }
            });
            CheckAll = checkCount == SelectedSpider.ScrapItems.Count ? true
                : checkCount == 0 ? false : null;
        }
    }
}
