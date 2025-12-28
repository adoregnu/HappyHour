using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using HappyHour.Model;

namespace HappyHour.View.Controls
{
    static class ImageUtils
    {
        private static BitmapImage CreateBitmapImage(byte[] data, int width)
        {
            var image = new BitmapImage();
            image.BeginInit();
            if (width > 0)
            {
                image.DecodePixelWidth = width;
            }
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = new MemoryStream(data);
            image.EndInit();
            image.Freeze();
            return image;
        }

        private static BitmapImage GetFallbackImage(int width)
        {
            using var stream = Assembly.GetEntryAssembly()!.GetManifestResourceStream(
                "HappyHour.Resources.default-fallback-image.png");
            using var ms = new MemoryStream();
            stream!.CopyTo(ms);
            return CreateBitmapImage(ms.ToArray(), width);
        }

        public static BitmapImage ReadImage(string imagePath, int width)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(imagePath);
                return CreateBitmapImage(bytes, width);
            }
            catch (Exception e)
            {
                Log.Print(e.Message);
                return GetFallbackImage(width);
            }
        }

        public static BitmapImage LoadImage(ImageBlob blob, int width)
        {
            if (blob?.Data is { Length: > 0 })
            {
                try
                {
                    return CreateBitmapImage(blob.Data, width);
                }
                catch (Exception e)
                {
                    Log.Print(e.Message);
                }
            }

            return GetFallbackImage(width);
        }
    }
}
