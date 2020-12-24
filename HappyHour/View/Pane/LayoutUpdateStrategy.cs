using System.Linq;
using System.Diagnostics;
using System.Windows.Controls;
using System.Collections.Generic;

using AvalonDock.Layout;

using HappyHour.ViewModel;

namespace HappyHour.View.Pane
{
    class LayoutUpdateStrategy : ILayoutUpdateStrategy
    {
        public bool BeforeInsertAnchorable(LayoutRoot layout,
            LayoutAnchorable anchorableToShow,
            ILayoutContainer destinationContainer)
        {
            LayoutAnchorablePane pane = null;
            if (anchorableToShow.Content is DebugLogViewModel)
            {
                pane = layout.Descendents().OfType<LayoutAnchorablePane>()
                            .FirstOrDefault(d => d.Name == "bottom");
            }
            else if (anchorableToShow.Content is FileListViewModel)
            { 
                pane = layout.Descendents().OfType<LayoutAnchorablePane>()
                            .FirstOrDefault(d => d.Name == "left");
            }
            if (pane != null)
            {
                //anchorableToShow.CanHide = false;
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
#if false
            if (anchorableShown.Content is BrowserViewModel)
            {
                var parentDocumentGroup = anchorableShown.FindParent<LayoutDocumentPaneGroup>();
                var parentDocumentPane = anchorableShown.Parent as LayoutDocumentPane;

                if (parentDocumentGroup == null)
                {
                    var grandParent = parentDocumentPane.Parent;
                    parentDocumentGroup = new LayoutDocumentPaneGroup
                    {
                        Orientation = Orientation.Horizontal
                    };
                    grandParent.ReplaceChild(parentDocumentPane, parentDocumentGroup);
                    parentDocumentGroup.Children.Add(parentDocumentPane);
                }
                parentDocumentGroup.Orientation = Orientation.Horizontal;
                var indexOfParentPane = parentDocumentGroup.IndexOfChild(parentDocumentPane);
                parentDocumentGroup.InsertChildAt(indexOfParentPane + 1,
                    new LayoutDocumentPane(anchorableShown));
                anchorableShown.IsActive = true;
                //anchorableShown.Root.CollectGarbage();
            }
#endif
        }
    }
}
