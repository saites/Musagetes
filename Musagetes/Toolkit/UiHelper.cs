using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Musagetes.Toolkit
{
    public static class UiHelper
    {
        /// <summary>
        /// Finds a parent of a given item on the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="child">A direct or indirect child of the
        /// queried item.</param>
        /// <returns>The first parent item that matches the submitted
        /// type parameter. If not matching item can be found, a null
        /// reference is being returned.</returns>
        public static T TryFindParent<T>(this DependencyObject child)
            where T : DependencyObject
        {
            var parentObject = GetParentObject(child);
            while (parentObject != null)
            {
                var parent = parentObject as T;
                if (parent != null) return parent;
                child = parentObject;
                parentObject = GetParentObject(child);
            }
            return null;
        }

        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetParent"/> method, which also
        /// supports content elements. Keep in mind that for content element,
        /// this method falls back to the logical tree of the element!
        /// </summary>
        /// <param name="child">The item to be processed.</param>
        /// <returns>The submitted item's parent, if available. Otherwise
        /// null.</returns>
        public static DependencyObject GetParentObject(this DependencyObject child)
        {
            if (child == null) return null;

            //handle content elements separately
            var contentElement = child as ContentElement;
            if (contentElement == null)
                return (child is FrameworkElement ? ((FrameworkElement) child).Parent : null)
                       ?? VisualTreeHelper.GetParent(child);

            var parent = ContentOperations.GetParent(contentElement);
            if (parent != null) return parent;

            var fce = contentElement as FrameworkContentElement;
            return fce != null ? fce.Parent : null;
        }

        /// <summary>
        /// Determines if a mouse point is over a scrollbar,
        /// in which case you probably don't want to start
        /// drag and drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="mousePosition"></param>
        /// <returns></returns>
        public static bool IsMouseOverScrollbar(object sender, Point mousePosition)
        {
            if (!(sender is Visual)) return false;

            var hit = VisualTreeHelper.HitTest((Visual) sender, mousePosition);

            if (hit == null) return false;

            var dObj = hit.VisualHit;
            while (dObj != null)
            {
                if (dObj is ScrollBar) return true;

                if ((dObj is Visual) || (dObj is Visual3D)) 
                    dObj = VisualTreeHelper.GetParent(dObj);
                else dObj = LogicalTreeHelper.GetParent(dObj);
            }

            return false;
        }
    }
}
