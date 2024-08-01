using CommunityToolkit.Mvvm.ComponentModel;
using HappyHour.Model;
using Microsoft.EntityFrameworkCore.Query;
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

namespace HappyHour.View.Controls
{
    /// <summary>
    /// Interaction logic for LangEditBox.xaml
    /// </summary>
    public partial class LangComboBox : UserControl
    {
        public IEnumerable<MText> TextSource 
        {
            get { return (IEnumerable<MText>)GetValue(TextSourceValueProperty); }
            set { SetValue(TextSourceValueProperty, value); }
        }
        public static readonly DependencyProperty TextSourceValueProperty =
          DependencyProperty.Register(
              "TextSource",
              typeof(IEnumerable<MText>),
              typeof(LangComboBox),
              new PropertyMetadata(null, OnTextSourceChanged));


        private static void OnTextSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //var lbox = sender as LangComboBox;
        }

        public LangComboBox()
        {
            InitializeComponent();
        }
    }
}
