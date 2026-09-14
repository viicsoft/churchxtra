using System.Windows;
using System.Windows.Input;
using ChurchAI.App.ViewModels;

namespace ChurchAI.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            this.DragMove();
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is System.Windows.Controls.TextBox || e.OriginalSource is System.Windows.Controls.ComboBox)
        {
            return;
        }

        if (DataContext is MainWindowViewModel mainVm)
        {
            if (mainVm.NavigationService.CurrentViewModel is ProjectionViewModel projVm)
            {
                if (e.Key == Key.Down)
                {
                    if (projVm.MoveLiveProjectionDownCommand.CanExecute(null))
                    {
                        projVm.MoveLiveProjectionDownCommand.Execute(null);
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Up)
                {
                    if (projVm.MoveLiveProjectionUpCommand.CanExecute(null))
                    {
                        projVm.MoveLiveProjectionUpCommand.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }
    }

    protected override void OnClosed(System.EventArgs e)
    {
        base.OnClosed(e);
        System.Windows.Application.Current?.Shutdown();
        System.Environment.Exit(0);
    }
}