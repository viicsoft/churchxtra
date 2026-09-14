using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ChurchAI.App.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ChurchAI.App.Services;

public class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private ObservableObject? _currentViewModel;

    public ObservableObject CurrentViewModel
    {
        get => _currentViewModel!;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        if (_currentViewModel is IDisposable disposable)
        {
            try
            {
                disposable.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error disposing viewmodel: {ex.Message}");
            }
        }
        CurrentViewModel = _serviceProvider.GetRequiredService<TViewModel>();
    }
}
