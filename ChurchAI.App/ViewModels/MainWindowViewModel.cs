using System.Windows.Input;
using ChurchAI.App.Services.Interfaces;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchAI.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IWindowService _windowService;
    public INavigationService NavigationService { get; }

    public MainWindowViewModel(INavigationService navigationService, IWindowService windowService)
    {
        NavigationService = navigationService;
        _windowService = windowService;
        Navigate("Projection");
    }

    [RelayCommand]
    private void Minimize() => _windowService.Minimize();

    [RelayCommand]
    private void MaximizeRestore() 
    {
        _windowService.Maximize();
    }

    [RelayCommand]
    private void Close() => _windowService.Close();

    [ObservableProperty]
    private bool _isCommunityActive;

    [ObservableProperty]
    private string _activeViewName = "Projection";

    [RelayCommand]
    private void Navigate(string viewName)
    {
        IsCommunityActive = viewName == "Community";
        ActiveViewName = viewName;

        switch (viewName)
        {
            case "BibleSearch":
                NavigationService.NavigateTo<BibleSearchViewModel>();
                break;
            case "Projection":
                NavigationService.NavigateTo<ProjectionViewModel>();
                break;
            case "History":
                NavigationService.NavigateTo<HistoryViewModel>();
                break;
            case "Settings":
                NavigationService.NavigateTo<SettingsViewModel>();
                break;
            case "Community":
                NavigationService.NavigateTo<CommunityViewModel>();
                break;
        }
    }
}
