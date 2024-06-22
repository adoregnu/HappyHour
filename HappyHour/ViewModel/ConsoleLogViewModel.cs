using log4net;
using CefSharp;
using CommunityToolkit.Mvvm.Messaging;

namespace HappyHour.ViewModel
{
    class ConsoleLogViewModel : TextViewModel, IRecipient<ConsoleMessageEventArgs>
    {
        static readonly ILog logger = LogManager.GetLogger("CefConsoleLogger");
        public ConsoleLogViewModel()
        {
            Title = "CEF Console Log";
            Messenger.Register(this);
        }

        public void Receive(ConsoleMessageEventArgs msg)
        {
            if (string.IsNullOrEmpty(msg.Message))
                return;

            if (msg.Message.StartsWith("Third-party cookie"))
                return;

            UiServices.Invoke(delegate ()
            {
                AppendText(msg.Message + "\n");
                logger.Info(msg.Message);
            }, true);
        }
    }
}
