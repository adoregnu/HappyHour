using HappyHour.Model;
using HappyHour.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HappyHour.View
{
    /// <summary>
    /// Interaction logic for DbView.xaml
    /// </summary>
    public partial class DbView : UserControl
    {
        public DbView()
        {
            InitializeComponent();
        }
#if false
        void OnListBoxPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = e.OriginalSource as TextBlock;
            var selectedItem = listBoxItem.DataContext;
            if (DataContext is DbViewModel dbVm && selectedItem is Genre genre)
            {
                dbVm.SelectedGenre2 = genre;
            }
            e.Handled = true;
        }
#endif
    }
}
