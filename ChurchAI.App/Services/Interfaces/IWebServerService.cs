using System;
using System.Windows;

namespace ChurchAI.App.Services.Interfaces;

public interface IWebServerService
{
    bool IsBroadcasting { get; }
    void StartBroadcasting(Window windowToCapture);
    void StopBroadcasting();
}
