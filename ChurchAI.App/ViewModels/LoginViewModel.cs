using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChurchAI.App.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isInitialButtonVisible = false;

    [ObservableProperty]
    private bool _isLoginFormVisible = false;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public Action? OnLoginSuccess { get; set; }

    public LoginViewModel()
    {
        // Start the 5 second timer for the initial button
        _ = StartTimerAsync();
    }

    private async Task StartTimerAsync()
    {
        await Task.Delay(5000);
        IsInitialButtonVisible = true;
    }

    [RelayCommand]
    private void ShowLoginForm()
    {
        IsInitialButtonVisible = false;
        IsLoginFormVisible = true;
    }

    [RelayCommand]
    private void Login(object? parameter)
    {
        ErrorMessage = string.Empty;

        // In case they used a PasswordBox, we might need to get password from parameter,
        // but typically we can just bind it or use a behaviour. Let's just assume simple binding for now.
        // Actually WPF PasswordBox doesn't support direct binding easily.
        // I will pass the PasswordBox to the command.
        
        var passwordBox = parameter as System.Windows.Controls.PasswordBox;
        if (passwordBox != null)
        {
            Password = passwordBox.Password;
        }

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Username and password are required.";
            return;
        }

        if (Username.Trim() == "viicsoft" && Password == "viicsoft")
        {
            OnLoginSuccess?.Invoke();
        }
        else
        {
            ErrorMessage = "Invalid username or password.";
        }
    }
}
