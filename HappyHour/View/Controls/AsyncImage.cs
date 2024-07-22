using HappyHour.Model;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HappyHour.View.Controls
{
    public class AsyncImage : Image
    {
        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register(
                nameof(ImagePath), typeof(string), typeof(AsyncImage),
                new PropertyMetadata(async (o, e) =>
                    await ((AsyncImage)o).LoadImageAsync((AsyncImage)o, (string)e.NewValue)));

        public static readonly DependencyProperty ImageBlobProperty =
            DependencyProperty.Register(
                nameof(ImageBlob), typeof(ImageBlob), typeof(AsyncImage),
                new PropertyMetadata(async (o, e) =>
                    await ((AsyncImage)o).LoadImageAsync((AsyncImage)o, (ImageBlob)e.NewValue)));


        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }
        public ImageBlob ImageBlob
        {
            get { return (ImageBlob)GetValue(ImageBlobProperty); }
            set { SetValue(ImageBlobProperty, value); }
        }

        private async Task LoadImageAsync(AsyncImage ctrl, string imagePath)
        {
            int width = (int)(double)ctrl.GetValue(WidthProperty);
            //Log.Print($"path:{imagePath}, width:{width}");
            Source = await Task.Run(() =>
            {
                return ImageUtils.ReadImage(imagePath, width);
            });
        }
        private async Task LoadImageAsync(AsyncImage ctrl, ImageBlob blob)
        {
            int width = (int)(double)ctrl.GetValue(WidthProperty);
            //Log.Print($"blob, width:{width}");
            Source = await Task.Run(() =>
            {
                return ImageUtils.LoadImage(blob, width);
            });
        }
    }
}
