using CommunityToolkit.Mvvm.ComponentModel;

namespace ChurchAI.App.ViewModels;

public partial class TranslationTabViewModel : ObservableObject
{
    public string Abbreviation { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isActive;
}
