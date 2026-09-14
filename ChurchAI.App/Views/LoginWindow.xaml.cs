using System;
using System.Windows;
using ChurchAI.App.ViewModels;

namespace ChurchAI.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.OnLoginSuccess = () =>
        {
            this.Deactivated -= Window_Deactivated;
            this.DialogResult = true;
        };
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }
}
