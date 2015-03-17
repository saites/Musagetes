using System.Windows;
using System.Windows.Controls;

namespace Musagetes
{
    /* Taken from StackOverflow answer "How to bind SelectionStart Property of Text Box?"
     * Written by user RandomEngy
     * Edited by Alexander Saites
     * Original Location: http://stackoverflow.com/questions/1175618/how-to-bind-selectionstart-property-of-text-box
     */
    public class SelectionBindingTextBox : TextBox
    {
        public static readonly DependencyProperty BindableSelectionStartProperty =
            DependencyProperty.Register(
                "BindableSelectionStart",
                typeof(int),
                typeof(SelectionBindingTextBox),
                new PropertyMetadata(OnBindableSelectionStartChanged));

        public static readonly DependencyProperty BindableSelectionLengthProperty =
            DependencyProperty.Register(
                "BindableSelectionLength",
                typeof(int),
                typeof(SelectionBindingTextBox),
                new PropertyMetadata(OnBindableSelectionLengthChanged));

        private bool changeFromUI;

        public SelectionBindingTextBox()
        {
            SelectionChanged += OnSelectionChanged;
        }

        public int BindableSelectionStart
        {
            get
            {
                return (int)GetValue(BindableSelectionStartProperty);
            }

            set
            {
                SetValue(BindableSelectionStartProperty, value);
            }
        }

        public int BindableSelectionLength
        {
            get
            {
                return (int)GetValue(BindableSelectionLengthProperty);
            }

            set
            {
                SetValue(BindableSelectionLengthProperty, value);
            }
        }

        private static void OnBindableSelectionStartChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var textBox = dependencyObject as SelectionBindingTextBox;

            if (textBox.changeFromUI)
            {
                textBox.changeFromUI = false;
                return;
            }

            var newValue = (int) args.NewValue;
            textBox.SelectionStart = newValue;
        }

        private static void OnBindableSelectionLengthChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var textBox = dependencyObject as SelectionBindingTextBox;

            if (textBox.changeFromUI)
            {
                textBox.changeFromUI = false;
                return;
            }
            var newValue = (int) args.NewValue;
            textBox.SelectionLength = newValue;
        }

        private void OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (BindableSelectionStart != SelectionStart)
            {
                changeFromUI = true;
                BindableSelectionStart = SelectionStart;
            }

            if (BindableSelectionLength != SelectionLength)
            {
                changeFromUI = true;
                BindableSelectionLength = SelectionLength;
            }
        }
    }
}