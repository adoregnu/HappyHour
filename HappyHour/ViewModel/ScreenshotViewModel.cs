using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GalaSoft.MvvmLight.Messaging;

using HappyHour.Interfaces;
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

        public IMediaList MediaList { get; set; }

        public ScreenshotViewModel()
        {
            Title = "Screenshot";
            MediaList.ItemSelectedHandler += OnMediaItemSelected;
        }

        void OnMediaItemSelected(object sender, MediaItem item)
        {
            if (item != null)
                ScreenshotList = item.Screenshots;
            else
                ScreenshotList = null;
        }
    }
}
