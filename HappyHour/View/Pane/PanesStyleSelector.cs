using System.Windows;
using System.Windows.Controls;

using HappyHour.ViewModel;

namespace HappyHour.View.Pane
{
    class PanesStyleSelector : StyleSelector
    {
        public Style DocStyle { get; set; }
        public Style AnchorStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is DebugLogViewModel || item is FileListViewModel ||
                item is ScreenshotViewModel || item is DbViewModel)
                return AnchorStyle;

            if (item is ViewModel.Pane)
                return DocStyle;

            return base.SelectStyle(item, container);
        }
    }
}
