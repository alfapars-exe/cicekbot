using Metin2Bot.UI.ViewModels;
using System.Windows;

namespace Metin2Bot.UI.Views
{
    public partial class WindowPickerDialog : Window
    {
        public WindowPickerDialog()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is WindowPickerViewModel vm && vm.SelectedWindow is null)
            {
                MessageBox.Show("Lütfen bir pencere seçin.", "Eksik seçim", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
