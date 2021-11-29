using log4net;
using CefSharp;
using GalaSoft.MvvmLight.Messaging;

namespace HappyHour.ViewModel
{
    class ConsoleLogViewModel : TextViewModel
    {
        static ILog logger = LogManager.GetLogger("CefConsoleLogger");
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
                logger.Info(e.Message);
            }, true);
        }
    }
}
