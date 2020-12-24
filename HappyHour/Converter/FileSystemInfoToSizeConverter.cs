using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace HappyHour.Converter
{
    public class FileSystemInfoToSizeConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "unknown";
            if (value is FileInfo fi)
            {
                return $"{fi.Length}";
            }
            else if (value is DirectoryInfo di)
            {
                return "<DIR>";
            }
            return "";
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
