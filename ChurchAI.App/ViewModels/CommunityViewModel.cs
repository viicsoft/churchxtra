using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using ChurchAI.App.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChurchAI.App.ViewModels;

public partial class CommunityViewModel : ViewModelBase
{
    private readonly ICommunityMessageService _messageService;
    private readonly IProjectionService _projectionService;

    public bool IsServerRunning => _messageService.IsServerRunning;
    public string ServerUrl => _messageService.ServerUrl;
    public BitmapImage? QrCodeImage => _messageService.QrCodeImage;
    public ObservableCollection<CommunityMessage> ReceivedMessages => _messageService.ReceivedMessages;

    [ObservableProperty]
    private bool _showQrCodeView = false;

    public CommunityViewModel(ICommunityMessageService messageService, IProjectionService projectionService)
    {
        _messageService = messageService;
        _projectionService = projectionService;

        _messageService.MessageReceived += (s, e) =>
        {
            ShowQrCodeView = false;
        };
    }

    [RelayCommand]
    private void ShowQrCode()
    {
        ShowQrCodeView = true;
    }

    [RelayCommand]
    private void ShowMessages()
    {
        ShowQrCodeView = false;
    }

    [RelayCommand]
    private void RefreshQr()
    {
        ShowQrCodeView = true;
    }

    [RelayCommand]
    private void ToggleServer()
    {
        if (_messageService.IsServerRunning)
        {
            _messageService.StopServer();
        }
        else
        {
            _messageService.StartServer();
        }
        
        OnPropertyChanged(nameof(IsServerRunning));
        OnPropertyChanged(nameof(ServerUrl));
        OnPropertyChanged(nameof(QrCodeImage));
    }

    [RelayCommand]
    private void CopyUrl()
    {
        try
        {
            if (!string.IsNullOrEmpty(ServerUrl))
            {
                System.Windows.Clipboard.SetText(ServerUrl);
            }
        }
        catch { }
    }

    [RelayCommand]
    private void ProjectMessage(CommunityMessage? message)
    {
        if (message == null) return;
        _projectionService.ProjectVerse(string.Empty, message.Text);
    }

    [RelayCommand]
    private void AddMessageToSchedule(CommunityMessage? message)
    {
        if (message == null) return;
        _projectionService.AddToQueue(message.Title, message.Text);
    }

    [RelayCommand]
    private void DiscardMessage(CommunityMessage? message)
    {
        if (message == null) return;
        ReceivedMessages.Remove(message);
    }
}
