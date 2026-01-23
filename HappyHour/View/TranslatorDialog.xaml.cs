using MvvmDialogs; // added
using System;
using System.Collections.Generic;
using System.ComponentModel; // added
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
using System.Windows.Shapes;

namespace HappyHour.View
{
    /// <summary>
    /// Interaction logic for TranslatorDialog.xaml
    /// </summary>
    public partial class TranslatorDialog : Window
    {
        public TranslatorDialog()
        {
            InitializeComponent();
            this.DataContextChanged += TranslatorDialog_DataContextChanged;
        }

        private void TranslatorDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyPropertyChanged oldViewModel)
            {
                oldViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewValue is INotifyPropertyChanged newViewModel)
            {
                newViewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IModalDialogViewModel.DialogResult))
            {
                if (DataContext is IModalDialogViewModel viewModel && viewModel.DialogResult.HasValue)
                {
                    this.DialogResult = viewModel.DialogResult;
                }
            }
        }
    }
}
