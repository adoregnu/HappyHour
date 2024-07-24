using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace HappyHour.View.Behavior
{
    class ListboxBehavior : Behavior<ListBox>
    {
        public bool AutoScroll
        {
            get { return (bool)GetValue(AutoScrollProperty); }
            set { SetValue(AutoScrollProperty, value); }
        }
        public bool AutoCopy
        {
            get { return (bool)GetValue(AutoCopyProperty); }
            set { SetValue(AutoCopyProperty, value); }
        }

        public static readonly DependencyProperty AutoScrollProperty =
            DependencyProperty.Register(
                "AutoScroll",
                typeof(bool),
                typeof(ListboxBehavior),
                new PropertyMetadata(null));

        public static readonly DependencyProperty AutoCopyProperty =
            DependencyProperty.RegisterAttached(
                "AutoCopy",
                typeof(bool),
                typeof(ListboxBehavior),
                new UIPropertyMetadata(AutoCopyChanged));

        /// <summary>
        ///  When Beahvior is attached
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
        }

        static void AutoCopyChanged(DependencyObject obj_, DependencyPropertyChangedEventArgs e_)
        {
            if (obj_ is ListBox listBox)
            {
                if ((bool)e_.NewValue)
                {
                    ExecutedRoutedEventHandler handler =
                        (sender_, arg_) =>
                        {
                            if (listBox.SelectedItem != null)
                            {
                                //Copy what ever your want here
                                Clipboard.SetDataObject(listBox.SelectedItem.ToString());
                            }
                        };

                    var command = new RoutedCommand("Copy", typeof(ListBox));
                    command.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control, "Copy"));
                    listBox.CommandBindings.Add(new CommandBinding(command, handler));
                }
            }
        }
        /// <summary>
        /// On Selection Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AssociatedObject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox)
            {
                return;
            }

            ListBox listBox = (sender as ListBox);
            if (listBox.SelectedItem == null)
            {
                return;
            }

            if (AutoScroll)
            {
                listBox.Dispatcher.BeginInvoke(() =>
                {
                    listBox.UpdateLayout();
                    listBox.ScrollIntoView(listBox.SelectedItem);
                });
            }
        }
        /// <summary>
        /// When behavior is detached
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;
        }
    }
}
