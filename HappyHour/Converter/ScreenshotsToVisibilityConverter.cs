using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using HappyHour.Model;
namespace HappyHour.Converter
{
    class ScreenshotsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var mediaItem = value as MediaItem;
            if (mediaItem == null ||
                mediaItem.Screenshots == null ||
                mediaItem.Screenshots.Count == 0)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
