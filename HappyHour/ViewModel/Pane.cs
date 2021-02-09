using System.ComponentModel;
using System.Windows.Input;

using GalaSoft.MvvmLight;

namespace HappyHour.ViewModel
{
    class Pane : ViewModelBase
    {
        public Pane()
        {
            Title = "Unknown";
        }

        private string _title = null;
        [Browsable(false)]
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        private bool _isSelected = false;
        [Browsable(false)]
        public bool IsSelected
        {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }
        public bool CanHide { get; set; }
        public bool CanClose { get; set; } = false;

        public virtual void OnKeyDown(KeyEventArgs e)
        { 
        }
    }
}
