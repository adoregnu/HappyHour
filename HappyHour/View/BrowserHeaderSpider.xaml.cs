using System;
using System.Collections.Generic;
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
    /// Interaction logic for BrowserHeaderSpider.xaml
    /// </summary>
    public partial class BrowserHeaderSpider : UserControl
    {
        public BrowserHeaderSpider()
        {
            InitializeComponent();
        }

        private void OnRegexPatternKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (sender is ComboBox comboBox)
            {
                comboBox.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
            }
        }

        private void OnRegexPatternSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (comboBox.SelectedItem is string selectedPattern)
                {
                    comboBox.Text = selectedPattern;
                }
                comboBox.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();
            }
        }
    }
}
