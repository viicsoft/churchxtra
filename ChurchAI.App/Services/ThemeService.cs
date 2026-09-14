using System;
using ChurchAI.App.Services.Interfaces;

namespace ChurchAI.App.Services;

public class ThemeService : IThemeService
{
    public bool IsDarkTheme { get; private set; } = true;

    public void SetTheme(bool isDark)
    {
        IsDarkTheme = isDark;
        // In a real application, this would swap out ResourceDictionaries in Application.Current.Resources
    }
}
