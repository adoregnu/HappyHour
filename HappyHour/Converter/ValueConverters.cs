#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable CA1812 // Remove classes that are apparently never instantiated

using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using HappyHour.Model;
using HappyHour.Spider;

namespace HappyHour.Converter
{

    internal class AvMediaTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is AvMovie ? "movie" : "image";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
    internal class PosterToVisivility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path)
            {
                return Path.Exists(path) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return value != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    internal class SpiderTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is SpiderSehuatang ? "sehuatang" :
                value is SpiderSukebei ? "sukebei" : "default";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    internal class NullToVisivilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
#pragma warning restore CA1812 // Remove classes that are apparently never instantiated
#pragma warning restore SA1649 // File name must match first type name