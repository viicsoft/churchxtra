using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ChurchAI.App.Services.Interfaces;

namespace ChurchAI.App.Services;

public class SettingsService : ISettingsService
{
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private readonly string _settingsFilePath;
    private bool _useCloudServices = true;
    private string _cloudApiKey = string.Empty;
    private string _geminiVisionApiKey = string.Empty;
    private bool _autoRouteSecondaryDisplay = true;
    private bool _enableNDIOutput = false;
    private string _ndiSourceName = "ChurchXtra AI Projection";
    private bool _enableWebServerOutput = false;
    private string _selectedDisplayDevice = string.Empty;
    private string _speechApiUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
    private string _speechApiModel = "whisper-large-v3";
    private string _intentApiKey = string.Empty;
    private string _intentApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private string _intentApiModel = "llama-3.1-8b-instant";
    private string _speechProvider = "Google";
    private string _googleCredentialsPath = @"C:\keys\google-key.json";
    private double _audioGain = 1.5;
    private string _activeHymnBook = "Default";
    private bool _enableAutoSplitting = true;
    private int _autoSplitMaxLines = 4;
    private int _autoSplitMaxChars = 220;
    private int _activeBackgroundMediaId = 0;
    private string _activeBackgroundColorHex = "#000000";
    private double _backgroundOverlayOpacity = 0.3;

    public int ActiveBackgroundMediaId
    {
        get => _activeBackgroundMediaId;
        set
        {
            if (_activeBackgroundMediaId != value)
            {
                _activeBackgroundMediaId = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string ActiveBackgroundColorHex
    {
        get => _activeBackgroundColorHex;
        set
        {
            if (_activeBackgroundColorHex != value)
            {
                _activeBackgroundColorHex = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public double BackgroundOverlayOpacity
    {
        get => _backgroundOverlayOpacity;
        set
        {
            if (_backgroundOverlayOpacity != value)
            {
                _backgroundOverlayOpacity = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool EnableAutoSplitting
    {
        get => _enableAutoSplitting;
        set
        {
            if (_enableAutoSplitting != value)
            {
                _enableAutoSplitting = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public int AutoSplitMaxLines
    {
        get => _autoSplitMaxLines;
        set
        {
            if (_autoSplitMaxLines != value)
            {
                _autoSplitMaxLines = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public int AutoSplitMaxChars
    {
        get => _autoSplitMaxChars;
        set
        {
            if (_autoSplitMaxChars != value)
            {
                _autoSplitMaxChars = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool UseCloudServices
    {
        get => _useCloudServices;
        set
        {
            if (_useCloudServices != value)
            {
                _useCloudServices = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string CloudApiKey
    {
        get => _cloudApiKey;
        set
        {
            if (_cloudApiKey != value)
            {
                _cloudApiKey = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string GeminiVisionApiKey
    {
        get => _geminiVisionApiKey;
        set
        {
            if (_geminiVisionApiKey != value)
            {
                _geminiVisionApiKey = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool AutoRouteSecondaryDisplay
    {
        get => _autoRouteSecondaryDisplay;
        set
        {
            if (_autoRouteSecondaryDisplay != value)
            {
                _autoRouteSecondaryDisplay = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool EnableNDIOutput
    {
        get => _enableNDIOutput;
        set
        {
            if (_enableNDIOutput != value)
            {
                _enableNDIOutput = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string NDISourceName
    {
        get => _ndiSourceName;
        set
        {
            if (_ndiSourceName != value)
            {
                _ndiSourceName = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public bool EnableWebServerOutput
    {
        get => _enableWebServerOutput;
        set
        {
            if (_enableWebServerOutput != value)
            {
                _enableWebServerOutput = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string SelectedDisplayDevice
    {
        get => _selectedDisplayDevice;
        set
        {
            if (_selectedDisplayDevice != value)
            {
                _selectedDisplayDevice = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string SpeechApiUrl
    {
        get => _speechApiUrl;
        set
        {
            if (_speechApiUrl != value)
            {
                _speechApiUrl = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string SpeechApiModel
    {
        get => _speechApiModel;
        set
        {
            if (_speechApiModel != value)
            {
                _speechApiModel = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string IntentApiKey
    {
        get => _intentApiKey;
        set
        {
            if (_intentApiKey != value)
            {
                _intentApiKey = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string IntentApiUrl
    {
        get => _intentApiUrl;
        set
        {
            if (_intentApiUrl != value)
            {
                _intentApiUrl = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string IntentApiModel
    {
        get => _intentApiModel;
        set
        {
            if (_intentApiModel != value)
            {
                _intentApiModel = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string SpeechProvider
    {
        get => _speechProvider;
        set
        {
            if (_speechProvider != value)
            {
                _speechProvider = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string GoogleCredentialsPath
    {
        get => _googleCredentialsPath;
        set
        {
            if (_googleCredentialsPath != value)
            {
                _googleCredentialsPath = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public double AudioGain
    {
        get => _audioGain;
        set
        {
            if (_audioGain != value)
            {
                _audioGain = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public string ActiveHymnBook
    {
        get => _activeHymnBook;
        set
        {
            if (_activeHymnBook != value)
            {
                _activeHymnBook = value;
                OnPropertyChanged();
                Save();
            }
        }
    }

    public SettingsService()
    {
        var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChurchAI");
        Directory.CreateDirectory(appDataFolder);
        _settingsFilePath = Path.Combine(appDataFolder, "settings.json");
        Load();
    }

    public void Load()
    {
        if (File.Exists(_settingsFilePath))
        {
            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                var data = JsonSerializer.Deserialize<SettingsData>(json);
                if (data != null)
                {
                    _useCloudServices = data.UseCloudServices;
                    _cloudApiKey = data.CloudApiKey ?? string.Empty;
                    _geminiVisionApiKey = data.GeminiVisionApiKey ?? string.Empty;
                    _autoRouteSecondaryDisplay = data.AutoRouteSecondaryDisplay;
                    _enableNDIOutput = data.EnableNDIOutput;
                    _ndiSourceName = data.NDISourceName ?? "ChurchXtra AI Projection";
                    _enableWebServerOutput = data.EnableWebServerOutput;
                    _selectedDisplayDevice = data.SelectedDisplayDevice ?? string.Empty;
                    _speechApiUrl = data.SpeechApiUrl ?? "https://api.groq.com/openai/v1/audio/transcriptions";
                    _speechApiModel = data.SpeechApiModel ?? "whisper-large-v3";
                    _intentApiKey = data.IntentApiKey ?? string.Empty;
                    _intentApiUrl = data.IntentApiUrl ?? "https://api.groq.com/openai/v1/chat/completions";
                    _intentApiModel = data.IntentApiModel ?? "llama-3.1-8b-instant";
                    _speechProvider = data.SpeechProvider ?? "Google";
                    _googleCredentialsPath = data.GoogleCredentialsPath ?? @"C:\keys\google-key.json";
                    _audioGain = data.AudioGain > 0 ? data.AudioGain : 1.5;
                    _activeHymnBook = data.ActiveHymnBook ?? "Default";
                    _enableAutoSplitting = data.EnableAutoSplitting;
                    _autoSplitMaxLines = data.AutoSplitMaxLines > 0 ? data.AutoSplitMaxLines : 4;
                    _autoSplitMaxChars = data.AutoSplitMaxChars > 0 ? data.AutoSplitMaxChars : 220;
                }
            }
            catch
            {
                // Ignore load errors and use defaults
            }
        }
    }

    public void Save()
    {
        try
        {
            var data = new SettingsData
            {
                UseCloudServices = _useCloudServices,
                CloudApiKey = _cloudApiKey,
                GeminiVisionApiKey = _geminiVisionApiKey,
                AutoRouteSecondaryDisplay = _autoRouteSecondaryDisplay,
                EnableNDIOutput = _enableNDIOutput,
                NDISourceName = _ndiSourceName,
                EnableWebServerOutput = _enableWebServerOutput,
                SelectedDisplayDevice = _selectedDisplayDevice,
                SpeechApiUrl = _speechApiUrl,
                SpeechApiModel = _speechApiModel,
                IntentApiKey = _intentApiKey,
                IntentApiUrl = _intentApiUrl,
                IntentApiModel = _intentApiModel,
                SpeechProvider = _speechProvider,
                GoogleCredentialsPath = _googleCredentialsPath,
                AudioGain = _audioGain,
                ActiveHymnBook = _activeHymnBook,
                EnableAutoSplitting = _enableAutoSplitting,
                AutoSplitMaxLines = _autoSplitMaxLines,
                AutoSplitMaxChars = _autoSplitMaxChars
            };
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private class SettingsData
    {
        public bool UseCloudServices { get; set; }
        public string? CloudApiKey { get; set; }
        public string? GeminiVisionApiKey { get; set; }
        public bool AutoRouteSecondaryDisplay { get; set; } = true;
        public bool EnableNDIOutput { get; set; } = false;
        public string? NDISourceName { get; set; }
        public bool EnableWebServerOutput { get; set; } = false;
        public string? SelectedDisplayDevice { get; set; }
        public string? SpeechApiUrl { get; set; }
        public string? SpeechApiModel { get; set; }
        public string? IntentApiKey { get; set; }
        public string? IntentApiUrl { get; set; }
        public string? IntentApiModel { get; set; }
        public string? SpeechProvider { get; set; }
        public string? GoogleCredentialsPath { get; set; }
        public double AudioGain { get; set; } = 1.5;
        public string? ActiveHymnBook { get; set; }
        public bool EnableAutoSplitting { get; set; } = true;
        public int AutoSplitMaxLines { get; set; } = 4;
        public int AutoSplitMaxChars { get; set; } = 220;
    }
}
