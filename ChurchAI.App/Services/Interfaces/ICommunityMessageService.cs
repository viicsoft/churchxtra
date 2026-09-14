using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace ChurchAI.App.Services.Interfaces;

public interface ICommunityMessageService
{
    bool IsServerRunning { get; }
    string ServerUrl { get; }
    BitmapImage? QrCodeImage { get; }
    ObservableCollection<CommunityMessage> ReceivedMessages { get; }
    
    event EventHandler<CommunityMessageEventArgs>? MessageReceived;

    void StartServer();
    void StopServer();
}

public class CommunityMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "Community Alert";
    public string Text { get; set; } = string.Empty;
    public string Tag { get; set; } = "green"; // red, yellow, green
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string TagLabel => Tag switch
    {
        "red" => "Project Now",
        "yellow" => "Project Later",
        "green" => "Project Soon",
        _ => "Normal"
    };

    public string TagColor => Tag switch
    {
        "red" => "#EF4444",
        "yellow" => "#F59E0B",
        "green" => "#10B981",
        _ => "#94A3B8"
    };
}

public class CommunityMessageEventArgs : EventArgs
{
    public CommunityMessage Message { get; }
    public CommunityMessageEventArgs(CommunityMessage message)
    {
        Message = message;
    }
}
