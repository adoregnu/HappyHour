using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using HappyHour.ViewModel;
using HappyHour.Spider;

namespace HappyHour.Converter
{
    class BrowserHeaderTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {

            if (value is SpiderViewModel spider)
            {
                if (spider.SelectedSpider is SpiderSehuatang)
                    return "Sehuatang";
                else
                    return "Spider";
            }
            return "Base";
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
