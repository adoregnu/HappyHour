using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class Pane : ObservableRecipient
    {
        public IMainView MainView { get; set; }
        public ICommand CmdClose { get; set; }

        public Pane()
        {
            Title = "Unknown";
            CmdClose = new RelayCommand(() => OnClose());
        }

        private string _title = null;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        private bool _isSelected = false;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public bool CanHide { get; set; }
        public bool CanClose { get; set; } = false;
        public new bool IsActive { get; set; } = true; 

        public virtual void OnKeyDown(KeyEventArgs e)
        {
        }
        protected virtual void OnClose()
        {
            Log.Print("OnClose:" + Title);
            MainView.Docs.Remove(this);
        }
        public virtual void Cleanup()
        {
            OnDeactivated();
        }
    }
}
