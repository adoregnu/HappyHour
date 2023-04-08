using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.AvalonEdit.Folding;

using HappyHour.ViewModel;
using Microsoft.Xaml.Behaviors;

namespace HappyHour.View.Behavior
{
    class AvalonEditBehavior : Behavior<TextEditor> 
    {
        int _prevLineCount = 0;
        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
            {
                AssociatedObject.TextChanged += AssociatedObjectOnTextChanged;
                AssociatedObject.DataContextChanged += DataContextChanged;
                SearchPanel.Install(AssociatedObject);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
                AssociatedObject.DataContextChanged -= DataContextChanged;
            }
        }

        //bool _autoscroll = true;
        TextViewModel _viewModel = null;
        private void DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel = e.NewValue as TextViewModel;
        }
        private void AssociatedObjectOnTextChanged(object sender, EventArgs eventArgs)
        {
            if (!_viewModel.AutoScrollEnabled) return;

            var textEditor = sender as TextEditor;
            if (textEditor != null && _prevLineCount != textEditor.LineCount)
            {
                textEditor.ScrollToLine(textEditor.LineCount);
            }
            _prevLineCount = textEditor.LineCount;
        }
    }
}
