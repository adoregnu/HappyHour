using log4net;
using CefSharp;
using CommunityToolkit.Mvvm.Messaging;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class ConsoleLogViewModel : TextViewModel, IRecipient<ConsoleMessageEventArgs>
    {
        static readonly ILog logger = LogManager.GetLogger("CefConsoleLogger");
        public ConsoleLogViewModel(IMainView mainView) : base(mainView)
        {
            Title = "CEF Console Log";
            Messenger.Register(this);
        }

        public void Receive(ConsoleMessageEventArgs msg)
        {
            if (string.IsNullOrEmpty(msg.Message))
                return;

            if (msg.Message.StartsWith("Third-party cookie") ||
                msg.Message.StartsWith("[object") ||
                msg.Message.StartsWith("A parser-blocking")
                )
                return;

            UiServices.Invoke(delegate ()
            {
                AppendText(msg.Message + "\n");
                logger.Info(msg.Message);
            }, true);
        }
    }
}
