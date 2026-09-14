using System;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;

namespace ChurchAI.App.Services;

public class SpeechRecognitionResolver : ISpeechRecognitionService
{
    private readonly ISettingsService _settings;
    private readonly ISpeechRecognitionService _localService;
    private readonly ISpeechRecognitionService _cloudService;
    private readonly ISpeechRecognitionService _googleService;

    public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;
    public event EventHandler<AudioLevelUpdatedEventArgs>? AudioLevelUpdated;

    public SpeechRecognitionResolver(ISettingsService settings, SpeechRecognitionService localService, CloudSpeechRecognitionService cloudService, GoogleSpeechRecognitionService googleService)
    {
        _settings = settings;
        _localService = localService;
        _cloudService = cloudService;
        _googleService = googleService;

        _localService.SpeechRecognized += (s, e) => { if (!_settings.UseCloudServices) SpeechRecognized?.Invoke(this, e); };
        _cloudService.SpeechRecognized += (s, e) => { if (_settings.UseCloudServices && _settings.SpeechProvider != "Google") SpeechRecognized?.Invoke(this, e); };
        _googleService.SpeechRecognized += (s, e) => { if (_settings.UseCloudServices && _settings.SpeechProvider == "Google") SpeechRecognized?.Invoke(this, e); };
        
        _localService.AudioLevelUpdated += (s, e) => { if (!_settings.UseCloudServices) AudioLevelUpdated?.Invoke(this, e); };
        _cloudService.AudioLevelUpdated += (s, e) => { if (_settings.UseCloudServices && _settings.SpeechProvider != "Google") AudioLevelUpdated?.Invoke(this, e); };
        _googleService.AudioLevelUpdated += (s, e) => { if (_settings.UseCloudServices && _settings.SpeechProvider == "Google") AudioLevelUpdated?.Invoke(this, e); };
    }

    private ISpeechRecognitionService ActiveService =>
        !_settings.UseCloudServices ? _localService :
        (_settings.SpeechProvider == "Google" ? _googleService : _cloudService);

    public Task StartListeningAsync()
    {
        return ActiveService.StartListeningAsync();
    }

    public Task StopListeningAsync()
    {
        return ActiveService.StopListeningAsync();
    }

    public bool IsListening => ActiveService.IsListening;
}
