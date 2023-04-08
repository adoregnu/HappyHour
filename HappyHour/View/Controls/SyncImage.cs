using System.Windows;

namespace HappyHour.View.Controls
{
    public class SyncImage : System.Windows.Controls.Image
    {
        public static readonly DependencyProperty ImagePathProperty =
            DependencyProperty.Register(
                nameof(ImagePath), typeof(string), typeof(SyncImage),
                new PropertyMetadata((o, e) => ((SyncImage)o).LoadImage((string)e.NewValue)));

        public string ImagePath
        {
            get { return (string)GetValue(ImagePathProperty); }
            set { SetValue(ImagePathProperty, value); }
        }

        private void LoadImage(string imagePath)
        {
            Source = ImageUtils.ReadImage(imagePath, (int)Width);
        }
    }
}
