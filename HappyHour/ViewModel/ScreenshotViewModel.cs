using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;

using HappyHour.Model;

namespace HappyHour.ViewModel
{
    class ScreenshotViewModel : Pane
    {
        List<string> _screenshots;
        public List<string> ScreenshotList
        {
            get => _screenshots;
            set => Set(ref _screenshots, value);
        }

        public ScreenshotViewModel()
        {
            Title = "Screenshot";
            MessengerInstance.Register<NotificationMessage<MediaItem>>(
                this, (msg) =>
                {
                    if (msg.Notification != "mediaSelected") return;
                    ScreenshotList =  msg.Content.Screenshots;
                });
        }
    }
}
