using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace HappyHour.Converter
{
    class SelectedItem2Visibility : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value is IList<object> list && list.Count > 1)
                return true;
            return false;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
