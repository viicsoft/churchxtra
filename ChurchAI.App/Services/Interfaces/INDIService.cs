using System.Windows;

namespace ChurchAI.App.Services.Interfaces;

public interface INDIService
{
    void StartBroadcasting(Window windowToCapture);
    void StopBroadcasting();
    bool IsBroadcasting { get; }
}
