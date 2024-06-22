using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using WebPWrapper;
using System.Reflection;

namespace HappyHour.View.Controls
{
    static class ImageUtils
    {
        static BitmapImage ConvertBitmap(Bitmap bitmap, int width)
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
            image.Freeze();
            return image;
        }
        public static BitmapImage ReadImage(string imagePath, int width)
        {
            try
            {
                var ext = Path.GetExtension(imagePath);
                if (ext.Equals(".webp", StringComparison.OrdinalIgnoreCase))
                {
                    WebP webp = new();
                    using var bitmap = webp.Load(imagePath);
                    return ConvertBitmap(bitmap, width);
                }
                else
                {
                    using var bitmap = new Bitmap(imagePath);
                    return ConvertBitmap(bitmap, width);
                }
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                using var bmp = new Bitmap(Assembly.GetEntryAssembly().GetManifestResourceStream(
                    "HappyHour.Resources.default-fallback-image.png"));
                return ConvertBitmap(bmp, width);
            }
        }
    }
}
