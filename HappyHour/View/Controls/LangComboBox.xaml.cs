using CommunityToolkit.Mvvm.ComponentModel;
using HappyHour.Model;
using HappyHour.ViewModel;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections;
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

namespace HappyHour.View.Controls
{
    /// <summary>
    /// Interaction logic for LangEditBox.xaml
    /// </summary>
    public partial class LangComboBox : UserControl
    {
        public IEnumerable TextSource 
        {
            get { return (IEnumerable)GetValue(TextSourceValueProperty); }
            set { SetValue(TextSourceValueProperty, value); }
        }
        public static readonly DependencyProperty TextSourceValueProperty =
          DependencyProperty.Register(
              "TextSource",
              typeof(IEnumerable),
              typeof(LangComboBox),
              new PropertyMetadata(null, OnTextSourceChanged));


        private static void OnTextSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //var lbox = sender as LangComboBox;
        }
        private void Translate(object sender, RoutedEventArgs e)
        {
            //HashSet<MText> textSources = (HashSet<MText>)TextSource;
            var dataContext = new TranslatorViewModel(_textList.SelectedItem as MText)
            {
                //TextSources = TextSource
            };

            var dialog = new TranslatorDialog() { DataContext = dataContext };
            dialog.ShowDialog();
        }
        public LangComboBox()
        {
            InitializeComponent();
        }
    }
}
