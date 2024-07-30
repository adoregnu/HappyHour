using CefSharp;
using CommunityToolkit.Mvvm.Messaging;
using HappyHour.Interfaces;

namespace HappyHour.ViewModel
{
    class StatusLogViewModel : TextViewModel, IRecipient<StatusMessageEventArgs>
    {
        public StatusLogViewModel(IMainView mainView) : base(mainView)
        {
            Title = "CEF Status Log";
            Messenger.Register(this);
            //Messenger.Register<StatusLogViewModel, StatusMessageEventArgs>(this, static (r, m) => r.Receive(m));
        }
#if false
        protected override void OnActivated()
        {
            //base.OnActivated();
            Messenger.Register(this);
        }
#endif
        public void Receive(StatusMessageEventArgs msg)
        {
            //var e = msg.Content;
            if (string.IsNullOrEmpty(msg.Value))
                return;

            UiServices.Invoke(delegate ()
            {
                AppendText(msg.Value + "\n");
            }, true);
        }

    }
}
