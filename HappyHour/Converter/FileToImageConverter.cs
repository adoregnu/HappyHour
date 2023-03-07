using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Imazen.WebP;

namespace HappyHour.Converter
{
    public class FileToImageConverter : IValueConverter
    {
        public static BitmapImage ConvertBitmap(Bitmap bitmap, int width)
        {
            using MemoryStream ms = new();
            bitmap.SetResolution(48, 48);
            bitmap.Save(ms, ImageFormat.Bmp);
            BitmapImage image = new();
            image.BeginInit();
            if (width > 0)
            {
                image.DecodePixelWidth = width;
            }
            image.CacheOption = BitmapCacheOption.OnLoad;
            _ = ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string path)
                {
                    int width = parameter != null ? int.Parse(parameter.ToString()) : 0;
                    if (width is not 0 and < 150)
                    {
                        path = $"{App.LocalAppData}\\db\\" + path;
                    }

                    if (File.Exists(path))
                    {
                        var ext = Path.GetExtension(path);
                        if (ext.Equals(".webp", StringComparison.OrdinalIgnoreCase))
                        {
                            var buf = File.ReadAllBytes(path);
                            using Bitmap bitmap = new SimpleDecoder().DecodeFromBytes(buf,  buf.Length);
                            return ConvertBitmap(bitmap, width);
                        }
                        else
                        {
                            using Bitmap bitmap = new Bitmap(path);
                            return ConvertBitmap(bitmap, width);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Log.Print(ex.Message);
                if (value is string path)
                {
                    Log.Print($"{ex.Message}: {path}, {parameter}");
                }
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
