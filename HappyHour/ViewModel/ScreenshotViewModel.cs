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
    internal class ScreenshotViewModel : Pane
    {
        private List<string> _screenshots;
        private IMediaList _mediaList;

        public List<string> ScreenshotList
        {
            get => _screenshots;
            set => Set(ref _screenshots, value);
        }

        public IMediaList MediaList
        {
            get => _mediaList;
            set
            {
                _mediaList = value;
                _mediaList.ItemSelectedHandler += OnMediaItemSelected;
            }
        }

        public ScreenshotViewModel()
        {
            Title = "Screenshot";
        }

        private void OnMediaItemSelected(object sender, IAvMedia item)
        {
            ScreenshotList = item is AvTorrent avt ? avt.Screenshots : null;
        }
    }
}
