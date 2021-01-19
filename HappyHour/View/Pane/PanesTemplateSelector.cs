using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using AvalonDock.Layout;

using HappyHour.ViewModel;
namespace HappyHour.View.Pane
{
    class PanesTemplateSelector : DataTemplateSelector
    {
        public PanesTemplateSelector()
        { 
        }

        public DataTemplate TextViewTemplate { get; set; }
        //public DataTemplate SpiderViewTemplate { get; set; }
        public DataTemplate BrowserViewTemplate { get; set; }
        public DataTemplate MediaListViewTemplate { get; set; }
        public DataTemplate PlayerViewTemplate { get; set; }
        public DataTemplate FileViewTemplate { get; set; }
        public DataTemplate AvDbViewTemplate { get; set; }
        public DataTemplate ScreenshotViewTemplate { get; set; }
        public DataTemplate DbViewTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TextViewModel)
                return TextViewTemplate;
            if (item is FileListViewModel)
                return FileViewTemplate;
            if (item is MediaListViewModel)
                return MediaListViewTemplate;
            //if (item is SpiderViewModel)
            //    return SpiderViewTemplate;
            if (item is PlayerViewModel)
                return PlayerViewTemplate;
            if (item is BrowserBase)
                return BrowserViewTemplate;
            if (item is ScreenshotViewModel)
                return ScreenshotViewTemplate;
            if (item is DbViewModel)
                return DbViewTemplate;
#if false
            if (item is MediaViewModel)
                return MediaViewTemplate;
            if (item is AvDbViewModel)
                return AvDbViewTemplate;
#endif
            return base.SelectTemplate(item, container);
        }
    }
}
