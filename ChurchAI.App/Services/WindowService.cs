using System.Windows;
using ChurchAI.App.Services.Interfaces;

namespace ChurchAI.App.Services;

public class WindowService : IWindowService
{
    public void Maximize()
    {
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.WindowState = WindowState.Maximized;
        }
    }

    public void Minimize()
    {
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }
    }

    public void Restore()
    {
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }
    }

    public void Close()
    {
        Application.Current.MainWindow?.Close();
    }
}
