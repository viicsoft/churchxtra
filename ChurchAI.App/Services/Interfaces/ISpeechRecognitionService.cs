using System;
using System.Threading.Tasks;

namespace ChurchAI.App.Services.Interfaces;

public class SpeechRecognizedEventArgs : EventArgs
{
    public string Text { get; }
    public bool IsFinal { get; }

    public SpeechRecognizedEventArgs(string text, bool isFinal)
    {
        Text = text;
        IsFinal = isFinal;
    }
}

public class AudioLevelUpdatedEventArgs : EventArgs
{
    public double AudioLevel { get; }

    public AudioLevelUpdatedEventArgs(double audioLevel)
    {
        AudioLevel = audioLevel;
    }
}

public interface ISpeechRecognitionService
{
    event EventHandler<SpeechRecognizedEventArgs> SpeechRecognized;
    event EventHandler<AudioLevelUpdatedEventArgs> AudioLevelUpdated;
    
    bool IsListening { get; }
    Task StartListeningAsync();
    Task StopListeningAsync();
}
