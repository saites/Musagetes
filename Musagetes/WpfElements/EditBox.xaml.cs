using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MvvmFoundation.Wpf;

namespace Musagetes.WpfElements
{
    /// <summary>
    /// Interaction logic for EditBox.xaml
    /// </summary>
    public partial class EditBox 
    {
        public EditBox()
        {
            InitializeComponent();

            CategoryTextBlock.MouseLeftButtonDown += 
                CategoryDoubleClick;
            CategoryTextBox.LostFocus +=
                CategoryLostFocus;
            CategoryTextBox.KeyDown +=
                TextBoxKeyDown;
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Escape) return;

            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                ItemText = _oldText;
                CategoryTextBox.Text = _oldText;
            }

            var ancestor = CategoryTextBox.Parent;
            while (ancestor != null)
            {
                var element = ancestor as UIElement;
                if (element != null && element.Focusable)
                {
                    element.Focus();
                    break;
                }

                ancestor = VisualTreeHelper.GetParent(ancestor);
            }
        }

        private void CategoryLostFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            InEditMode = false;
            CategoryTextBlock.Visibility = Visibility.Visible;
            CategoryTextBox.Visibility = Visibility.Collapsed;
            CategoryTextBox.Focusable = false;
        }

        private string _oldText;
        private void CategoryDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (InEditMode) e.Handled = true;
            if (e.ClickCount != 2) return;
            e.Handled = true;
            InEditMode = true;
        }

        public ICommand BeginEditCommand
        {
            get
            {
                return new RelayCommand(BeginEdit);
            }
        }

        private void BeginEdit()
        {
            if (CanEdit != null && !CanEdit())
            {
                InEditMode = false;
                return;
            }
            CategoryTextBlock.Visibility = Visibility.Collapsed;
            CategoryTextBox.Visibility = Visibility.Visible;
            CategoryTextBox.Focusable = true;
            _oldText = ItemText;
            CategoryTextBox.SelectAll();
            Keyboard.Focus(CategoryTextBox);
        }

        public static readonly DependencyProperty CanEditProperty =
            DependencyProperty.Register("CanEdit", typeof (Func<bool>),
                typeof (EditBox), new PropertyMetadata(default(Func<bool>)));

        public Func<bool> CanEdit
        {
            get { return (Func<bool>) GetValue(CanEditProperty); }
            set { SetValue(CanEditProperty, value);}
        } 

        public static readonly DependencyProperty TextBlockStyleProperty = 
            DependencyProperty.Register("TextBlockStyle", typeof (Style), 
            typeof (EditBox), new PropertyMetadata(null));

        public Style TextBlockStyle
        {
            get { return (Style) GetValue(TextBlockStyleProperty); }
            set { SetValue(TextBlockStyleProperty, value); }
        }

        public static readonly DependencyProperty ItemTextProperty = 
            DependencyProperty.Register("ItemText", typeof (string),
            typeof (EditBox), new FrameworkPropertyMetadata(
                string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string ItemText
        {
            get { return (string) GetValue(ItemTextProperty); }
            set { SetValue(ItemTextProperty, value); }
        }

        public static readonly DependencyProperty TextBoxStyleProperty =
            DependencyProperty.Register("TextBoxStyle", typeof (Style),
                typeof (EditBox), new PropertyMetadata(null));

        public Style TextBoxStyle
        {
            get { return (Style) GetValue(TextBoxStyleProperty); }
            set { SetValue(TextBoxStyleProperty, value); }
        }

        public static readonly DependencyProperty InEditModeProperty = 
            DependencyProperty.Register("InEditMode", typeof (bool), 
            typeof (EditBox), new PropertyMetadata(false, EditModeChanged));

        private static void EditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var editBox = d as EditBox;
            if (editBox == null) return;
            if ((bool) e.NewValue)
                editBox.BeginEdit();
        }

        public bool InEditMode
        {
            get { return (bool) GetValue(InEditModeProperty); }
            set { SetValue(InEditModeProperty, value); }
        }
    }
}
