using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchAI.App.ViewModels;

public partial class HymnBookTabViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isActive;
}
