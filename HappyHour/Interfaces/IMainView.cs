using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MvvmDialogs;

using HappyHour.ViewModel;
namespace HappyHour.Interfaces
{
    interface IMainView
    {
        IDialogService DialogService { get; } 
        ObservableCollection<Pane> Docs { get; }
        string StatusMessage { get; set; }
    }
}
