using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HappyHour.ViewModel;

namespace HappyHour.View.Behavior
{
    public static class ContextMenuBehavior
    {
        public static readonly DependencyProperty UpdateCommandProperty =
            DependencyProperty.RegisterAttached(
                "UpdateCommand",
                typeof(ICommand),
                typeof(ContextMenuBehavior),
                new PropertyMetadata(null, OnUpdateCommandChanged));

        public static ICommand GetUpdateCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(UpdateCommandProperty);
        }

        public static void SetUpdateCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(UpdateCommandProperty, value);
        }

        private static void OnUpdateCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ContextMenu contextMenu)
            {
                if (e.OldValue is ICommand oldCommand)
                {
                    contextMenu.Opened -= OnContextMenuOpened;
                }

                if (e.NewValue is ICommand newCommand)
                {
                    contextMenu.Opened += OnContextMenuOpened;
                }
            }
        }

        private static void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu contextMenu)
            {
                var command = GetUpdateCommand(contextMenu);
                if (command?.CanExecute(null) == true)
                {
                    command.Execute(null);
                }
            }
        }
    }
}