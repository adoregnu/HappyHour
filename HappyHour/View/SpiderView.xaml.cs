using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using CefSharp;

namespace HappyHour.View
{
    /// <summary>
    /// Interaction logic for BrowserView.xaml
    /// </summary>
    public partial class SpiderView : UserControl
    {
        public SpiderView()
        {
            InitializeComponent();
            //imageLoaingButton.IsChecked = true;
        }
#if false
        void EnableImageLoading(object sender, RoutedEventArgs e)
        {
            //browser.BrowserSettings.ImageLoading = CefState.Enabled;
        }

        void DisableImageLoading(object sender, RoutedEventArgs e)
        { 
            //browser.BrowserSettings.ImageLoading = CefState.Disabled;
        }
#endif
    }
}
