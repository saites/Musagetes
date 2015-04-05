using System;
using System.Windows;
using System.Windows.Input;

namespace Musagetes.WpfElements
{
    public class DragAndDropBehavior
    {
        public static readonly DependencyProperty DragAndDropBehaviourProperty =
            DependencyProperty.RegisterAttached("DragAndDropBehaviour", typeof(ICommand), 
            typeof(DragAndDropBehavior), new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.None,
                    OnDragAndDropBehaviourChanged));

        public static ICommand GetDragAndDropBehaviour(DependencyObject d)
        {
            return (ICommand)d.GetValue(DragAndDropBehaviourProperty);
        }

        public static void SetDragAndDropBehaviour(DependencyObject d, ICommand value)
        {
            d.SetValue(DragAndDropBehaviourProperty, value);
        }

        private static void OnDragAndDropBehaviourChanged(DependencyObject d, 
            DependencyPropertyChangedEventArgs e)
        {
            var iCommand = GetDragAndDropBehaviour(d);
            if (iCommand == null) return;

            if (d is UIElement)
                ((UIElement) d).Drop += OnDrop(iCommand);
            else if (d is ContentElement)
                ((ContentElement) d).Drop += OnDrop(iCommand);
            else
                throw new Exception("Element is not UIElement or ContentElement");
        }

        private static DragEventHandler OnDrop(ICommand iCommand)
        {
            return (s, a) =>
            {
                if (iCommand.CanExecute(a.Data))
                    iCommand.Execute(a.Data);
            };
        }
    }
}
