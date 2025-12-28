using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using MvvmDialogs;

using HappyHour.ViewModel;
namespace HappyHour.Interfaces
{
    delegate void ViewEventHandler(ViewEventArgs arg);
    interface IMainView
    {
        Window Window { get; set; }
        IDialogService DialogService { get; } 
        ObservableCollection<Pane> Docs { get; }
        string StatusMessage { get; set; }
        BrowserBase NewBrowser(string  url = null);
        ViewEventHandler OnViewUpdate { get; set; }
    }
}
