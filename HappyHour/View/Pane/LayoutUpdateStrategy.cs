using System.Linq;
using System.Diagnostics;
using System.Windows.Controls;
using System.Collections.Generic;

using AvalonDock.Layout;

using HappyHour.ViewModel;
using System.Windows;

namespace HappyHour.View.Pane
{
    class LayoutUpdateStrategy : ILayoutUpdateStrategy
    {
        public bool BeforeInsertAnchorable(LayoutRoot layout,
            LayoutAnchorable anchorableToShow,
            ILayoutContainer destinationContainer)
        {
            LayoutAnchorablePane pane = null;
            if (anchorableToShow.Content is TextViewModel ||
                anchorableToShow.Content is ScreenshotViewModel)
            {
                pane = layout.Descendents().OfType<LayoutAnchorablePane>()
                            .FirstOrDefault(d => d.Name == "bottom");
            }
            else if (anchorableToShow.Content is FileListViewModel ||
                anchorableToShow.Content is DbViewModel)
            { 
                pane = layout.Descendents().OfType<LayoutAnchorablePane>()
                            .FirstOrDefault(d => d.Name == "left");
            }
            if (pane != null)
            {
                anchorableToShow.CanHide = false;
                anchorableToShow.CanClose = false;
                pane.Children.Add(anchorableToShow);
                return true;
            }
            return false;
        }

        public void AfterInsertAnchorable(LayoutRoot layout,
            LayoutAnchorable anchorableShown)
        {
        }

        public bool BeforeInsertDocument(LayoutRoot layout,
            LayoutDocument anchorableToShow,
            ILayoutContainer destinationContainer)
        {
            return false;
        }

        public void AfterInsertDocument(LayoutRoot layout,
            LayoutDocument anchorableShown)
        {
#if true
            if (anchorableShown.Content is SpiderViewModel ||
                anchorableShown.Content is BrowserViewModel ||
                anchorableShown.Content is PlayerViewModel)
            {
                var parentDocumentGroup = anchorableShown.FindParent<LayoutDocumentPaneGroup>();
                var parentDocumentPane = anchorableShown.Parent as LayoutDocumentPane;

                if (parentDocumentGroup == null)
                {
                    var grandParent = parentDocumentPane.Parent;
                    parentDocumentGroup = new LayoutDocumentPaneGroup
                    {
                        Orientation = Orientation.Horizontal,
                    };
                    grandParent.ReplaceChild(parentDocumentPane, parentDocumentGroup);
                    parentDocumentGroup.Children.Add(parentDocumentPane);

                    parentDocumentGroup.Orientation = Orientation.Horizontal;
                    var indexOfParentPane = parentDocumentGroup.IndexOfChild(parentDocumentPane);
                    parentDocumentGroup.InsertChildAt(indexOfParentPane + 1,
                            new LayoutDocumentPane(anchorableShown)
                            {
                                DockWidth = new GridLength(1.5, GridUnitType.Star)
                            });

                }
                else
                {
                    var indexOfParentPane = parentDocumentGroup.IndexOfChild(parentDocumentPane);
                    var documentPane = parentDocumentGroup.Children[indexOfParentPane + 1] as LayoutDocumentPane;
                    //var numChildren = documentPane.ChildrenCount;
                    documentPane.InsertChildAt(0, anchorableShown);
                }
                anchorableShown.IsActive = true;
                //anchorableShown.Root.CollectGarbage();
            }
#endif
        }
    }
}
