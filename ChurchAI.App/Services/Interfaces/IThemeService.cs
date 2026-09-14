namespace ChurchAI.App.Services.Interfaces;

public interface IThemeService
{
    void SetTheme(bool isDark);
    bool IsDarkTheme { get; }
}
