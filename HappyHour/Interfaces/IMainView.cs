using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MvvmDialogs;

namespace HappyHour.Interfaces
{
    interface IMainView
    {
        IDialogService DialogService { get; }
        string StatusMessage { get; set; }
    }
}
