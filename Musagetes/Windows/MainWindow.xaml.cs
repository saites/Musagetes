using System.Windows.Controls;
using System.Windows.Input;
using Musagetes.ViewModels;

namespace Musagetes.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainWindowVm();
            DataContext = vm;
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