using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace Musagetes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowVm();
        }

        private void TagPrefixBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Down
                || TagPredictionBox.Items.Count <= 0) return;

            Keyboard.Focus(TagPredictionBox);
            ((ListBoxItem)(TagPredictionBox
                .ItemContainerGenerator
                .ContainerFromIndex(0)))
                .Focus();
            e.Handled = true;
        }

        private void TagPredictionBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Up || TagPredictionBox.SelectedIndex != 0) return;

            Keyboard.Focus(TagPrefixBox);
            e.Handled = true;
        }
    }
}