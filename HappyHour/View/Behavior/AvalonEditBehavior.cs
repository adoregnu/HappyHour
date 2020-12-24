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
                //AssociatedObject.DataContextChanged += DataContextChanged;
                SearchPanel.Install(AssociatedObject);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
            {
                AssociatedObject.TextChanged -= AssociatedObjectOnTextChanged;
                //AssociatedObject.DataContextChanged -= DataContextChanged;
            }
        }
#if false
        private void DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is TextViewModel)
            {
                var editor = sender as TextEditor;
                (e.NewValue as TextViewModel).AddElementGenerator(editor.TextArea.TextView);
            }
        }
#endif
        private void AssociatedObjectOnTextChanged(object sender, EventArgs eventArgs)
        {
            var textEditor = sender as TextEditor;

            if (textEditor != null && _prevLineCount != textEditor.LineCount)
            {
                textEditor.ScrollToLine(textEditor.LineCount);
            }
            _prevLineCount = textEditor.LineCount;
        }
    }
}
