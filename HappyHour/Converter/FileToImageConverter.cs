using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using HappyHour.Model;

namespace HappyHour.Converter
{
    public class FileToImageConverter : IValueConverter
    {
        public static BitmapImage ConvertBitmap(Bitmap bitmap, int width)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Bmp);
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                if (width > 0)
                    image.DecodePixelWidth = width;
                image.CacheOption = BitmapCacheOption.OnLoad;
                ms.Seek(0, SeekOrigin.Begin);
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }

        public object Convert(object value_, Type targetType_,
            object parameter_, CultureInfo culture_)
        {
            try
            {
                if (value_ is string path)
                {
                    var width = parameter_ != null ?
                        int.Parse(parameter_.ToString()) : 0;
                    // $"{App.CurrentPath}
                    if (width != 0 && width < 150)
                        path = "db\\" + path;
 
                    using var bmpTemp = new Bitmap(path);
                    return ConvertBitmap(bmpTemp, width);
                }
            }
            catch (Exception ex)
            {
                //Log.Print(ex.Message);
                if (value_ is string path)
                    Log.Print($"{ex.Message}: {path}, {parameter_}");
            }
            return new BitmapImage(new Uri(@"pack://application:,,,/" +
                            "Resources/default-fallback-image.png"));
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
