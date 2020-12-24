using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CefSharp;
using GalaSoft.MvvmLight.Messaging;

namespace HappyHour.ViewModel
{
    class ConsoleLogViewModel : TextViewModel
    {
        public ConsoleLogViewModel()
        {
            Title = "CEF Console Log";
            MessengerInstance.Register<NotificationMessage<ConsoleMessageEventArgs>>(
                this, OnConsoleMessage);
        }

        void OnConsoleMessage(NotificationMessage<ConsoleMessageEventArgs> msg)
        {
            var e = msg.Content;
            if (string.IsNullOrEmpty(e.Message))
                return;

            UiServices.Invoke(delegate ()
            {
                AppendText(e.Message + "\n");
            }, true);
        }
    }
}
