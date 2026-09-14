using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchAI.App.Services.Interfaces;

public interface INavigationService
{
    ObservableObject CurrentViewModel { get; }
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
}
