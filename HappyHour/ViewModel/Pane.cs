using System.ComponentModel;
using System.Windows.Input;

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

using HappyHour.Interfaces;
namespace HappyHour.ViewModel
{
    class Pane : ViewModelBase
    {
        public IMainView MainView { get; set; }
        public ICommand CmdClose { get; set; }

        public Pane()
        {
            Title = "Unknown";
            CmdClose = new RelayCommand(() => OnClose());
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

        protected virtual void OnClose()
        {
            Log.Print("OnClose:" + Title);
            MainView.Docs.Remove(this);
        }
    }
}
