using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using GalaSoft.MvvmLight.Messaging;

namespace HappyHour.ViewModel
{
    class StatusLogViewModel : TextViewModel
    {
        public StatusLogViewModel()
        {
            Title = "CEF Status Log";
            MessengerInstance.Register<NotificationMessage<StatusMessageEventArgs>>(
                this, OnMessage);
        }

        void OnMessage(NotificationMessage<StatusMessageEventArgs> msg)
        {
            var e = msg.Content;
            if (string.IsNullOrEmpty(e.Value))
                return;

            UiServices.Invoke(delegate ()
            {
                AppendText(e.Value + "\n");
            }, true);
        }
    }
}
