using System;
using System.ComponentModel;

namespace ChurchAI.App.Services.Interfaces;

public interface ISettingsService : INotifyPropertyChanged
{
    bool UseCloudServices { get; set; }
    string CloudApiKey { get; set; }
    string GeminiVisionApiKey { get; set; }
    bool AutoRouteSecondaryDisplay { get; set; }
    bool EnableNDIOutput { get; set; }
    string NDISourceName { get; set; }
    bool EnableWebServerOutput { get; set; }
    string SelectedDisplayDevice { get; set; }
    string SpeechApiUrl { get; set; }
    string SpeechApiModel { get; set; }
    string IntentApiKey { get; set; }
    string IntentApiUrl { get; set; }
    string IntentApiModel { get; set; }
    string SpeechProvider { get; set; }
    string GoogleCredentialsPath { get; set; }
    double AudioGain { get; set; }
    string ActiveHymnBook { get; set; }
    bool EnableAutoSplitting { get; set; }
    int AutoSplitMaxLines { get; set; }
    int AutoSplitMaxChars { get; set; }
    int ActiveBackgroundMediaId { get; set; }
    string ActiveBackgroundColorHex { get; set; }
    double BackgroundOverlayOpacity { get; set; }
    void Save();
    void Load();
}
