using System.Windows;
using System.Windows.Controls;

namespace ChurchAI.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void BrowseGoogleCredentials_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Select Google Service Account Credentials JSON File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            if (DataContext is ViewModels.SettingsViewModel vm)
            {
                vm.GoogleCredentialsPath = openFileDialog.FileName;
            }
        }
    }
}
