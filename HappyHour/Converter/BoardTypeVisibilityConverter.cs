using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using HappyHour.Spider;
namespace HappyHour.Converter
{
    class BoardTypeVisibilityConverter : IValueConverter
    {
        public object Convert(object value_, Type targetType_,
            object parameter_, CultureInfo culture_)
        {
            if (value_ is SpiderSehuatang)
            {
                if ((string)parameter_ == "sehuatang")
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            else// if (value_ is SpiderJavlibrary)
            {
                if ((string)parameter_ == "others")
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
