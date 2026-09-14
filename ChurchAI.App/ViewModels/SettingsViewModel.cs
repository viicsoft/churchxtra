using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChurchAI.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase, System.IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IHymnRepository _hymnRepository;
    private readonly ISpeechRecognitionService _speechRecognitionService;

    [ObservableProperty]
    private bool _isListening;

    [ObservableProperty]
    private string _speechStatus = "Microphone Idle";

    [ObservableProperty]
    private double _currentAudioLevel;

    public ObservableCollection<DisplayItem> AvailableDisplays { get; } = new();
    public ObservableCollection<string> AvailableHymnbooks { get; } = new();

    public string SelectedHymnbook
    {
        get => _settingsService.ActiveHymnBook;
        set
        {
            if (_settingsService.ActiveHymnBook != value)
            {
                _settingsService.ActiveHymnBook = value;
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty]
    private string _customHymnbookName = string.Empty;

    [ObservableProperty]
    private string _hymnUploadStatus = string.Empty;

    public string SelectedDisplayDevice
    {
        get => _settingsService.SelectedDisplayDevice;
        set
        {
            if (_settingsService.SelectedDisplayDevice != value)
            {
                _settingsService.SelectedDisplayDevice = value;
                OnPropertyChanged();
            }
        }
    }

    public bool EnableAutoSplitting
    {
        get => _settingsService.EnableAutoSplitting;
        set
        {
            if (_settingsService.EnableAutoSplitting != value)
            {
                _settingsService.EnableAutoSplitting = value;
                OnPropertyChanged();
            }
        }
    }

    public bool UseCloudServices
    {
        get => _settingsService.UseCloudServices;
        set
        {
            if (_settingsService.UseCloudServices != value)
            {
                _settingsService.UseCloudServices = value;
                OnPropertyChanged();
            }
        }
    }

    public string CloudApiKey
    {
        get => _settingsService.CloudApiKey;
        set
        {
            if (_settingsService.CloudApiKey != value)
            {
                _settingsService.CloudApiKey = value;
                OnPropertyChanged();
            }
        }
    }

    public string GeminiVisionApiKey
    {
        get => _settingsService.GeminiVisionApiKey;
        set
        {
            if (_settingsService.GeminiVisionApiKey != value)
            {
                _settingsService.GeminiVisionApiKey = value;
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty]
    private string _geminiTestStatus = string.Empty;

    [RelayCommand]
    private async Task TestGeminiVisionKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(GeminiVisionApiKey))
        {
            GeminiTestStatus = "⚠️ Please enter a valid Gemini API Key first.";
            return;
        }

        GeminiTestStatus = "⏳ Testing API key with Google AI Services...";
        try
        {
            using var http = new System.Net.Http.HttpClient();
            var res = await http.GetAsync($"https://generativelanguage.googleapis.com/v1beta/models?key={GeminiVisionApiKey}");
            if (res.IsSuccessStatusCode)
            {
                GeminiTestStatus = "✅ Connection Successful! AI Image Extraction is active.";
            }
            else
            {
                GeminiTestStatus = $"❌ Connection Failed (HTTP {(int)res.StatusCode}). Please check key.";
            }
        }
        catch (System.Exception ex)
        {
            GeminiTestStatus = $"❌ Error: {ex.Message}";
        }
    }

    public bool AutoRouteSecondaryDisplay
    {
        get => _settingsService.AutoRouteSecondaryDisplay;
        set
        {
            if (_settingsService.AutoRouteSecondaryDisplay != value)
            {
                _settingsService.AutoRouteSecondaryDisplay = value;
                OnPropertyChanged();
            }
        }
    }

    public bool EnableNDIOutput
    {
        get => _settingsService.EnableNDIOutput;
        set
        {
            if (_settingsService.EnableNDIOutput != value)
            {
                _settingsService.EnableNDIOutput = value;
                OnPropertyChanged();
            }
        }
    }

    public string NDISourceName
    {
        get => _settingsService.NDISourceName;
        set
        {
            if (_settingsService.NDISourceName != value)
            {
                _settingsService.NDISourceName = value;
                OnPropertyChanged();
            }
        }
    }

    public bool EnableWebServerOutput
    {
        get => _settingsService.EnableWebServerOutput;
        set
        {
            if (_settingsService.EnableWebServerOutput != value)
            {
                _settingsService.EnableWebServerOutput = value;
                OnPropertyChanged();
                UpdateWebServerInfo();
            }
        }
    }

    public string SpeechApiUrl
    {
        get => _settingsService.SpeechApiUrl;
        set
        {
            if (_settingsService.SpeechApiUrl != value)
            {
                _settingsService.SpeechApiUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string SpeechApiModel
    {
        get => _settingsService.SpeechApiModel;
        set
        {
            if (_settingsService.SpeechApiModel != value)
            {
                _settingsService.SpeechApiModel = value;
                OnPropertyChanged();
            }
        }
    }

    public string IntentApiKey
    {
        get => _settingsService.IntentApiKey;
        set
        {
            if (_settingsService.IntentApiKey != value)
            {
                _settingsService.IntentApiKey = value;
                OnPropertyChanged();
            }
        }
    }

    public string IntentApiUrl
    {
        get => _settingsService.IntentApiUrl;
        set
        {
            if (_settingsService.IntentApiUrl != value)
            {
                _settingsService.IntentApiUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public string IntentApiModel
    {
        get => _settingsService.IntentApiModel;
        set
        {
            if (_settingsService.IntentApiModel != value)
            {
                _settingsService.IntentApiModel = value;
                OnPropertyChanged();
            }
        }
    }

    public string SpeechProvider
    {
        get => _settingsService.SpeechProvider;
        set
        {
            if (_settingsService.SpeechProvider != value)
            {
                _settingsService.SpeechProvider = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGoogleProviderSelected));
                OnPropertyChanged(nameof(IsWhisperProviderSelected));
            }
        }
    }

    public string GoogleCredentialsPath
    {
        get => _settingsService.GoogleCredentialsPath;
        set
        {
            if (_settingsService.GoogleCredentialsPath != value)
            {
                _settingsService.GoogleCredentialsPath = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsGoogleProviderSelected => SpeechProvider == "Google";
    public bool IsWhisperProviderSelected => SpeechProvider != "Google";

    [ObservableProperty]
    private string _webServerUrl = string.Empty;

    [ObservableProperty]
    private System.Windows.Media.Imaging.BitmapImage? _qRCodeImage;

    public SettingsViewModel(
        ISettingsService settingsService, 
        IHymnRepository hymnRepository, 
        ISpeechRecognitionService speechRecognitionService)
    {
        _settingsService = settingsService;
        _hymnRepository = hymnRepository;
        _speechRecognitionService = speechRecognitionService;

        _speechRecognitionService.SpeechRecognized += OnSpeechRecognized;
        _speechRecognitionService.AudioLevelUpdated += OnAudioLevelUpdated;
        IsListening = _speechRecognitionService.IsListening;
        if (IsListening)
        {
            if (UseCloudServices)
            {
                SpeechStatus = SpeechProvider == "Google" 
                    ? "Cloud microphone active. Google Streaming Speech is listening..." 
                    : "Cloud microphone active. Groq/OpenAI is listening...";
            }
            else
            {
                SpeechStatus = "Offline microphone active. Whisper is listening...";
            }
        }

        UpdateWebServerInfo();
        LoadAvailableDisplays();
        _ = LoadHymnbooksAsync();
    }

    private void UpdateWebServerInfo()
    {
        if (!EnableWebServerOutput)
        {
            WebServerUrl = string.Empty;
            QRCodeImage = null;
            return;
        }

        string ipAddress = "127.0.0.1";
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ip = System.Linq.Enumerable.FirstOrDefault(host.AddressList, i => i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (ip != null) ipAddress = ip.ToString();
        }
        catch { }

        WebServerUrl = $"http://{ipAddress}:8080";

        try
        {
            using (var qrGenerator = new QRCoder.QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(WebServerUrl, QRCoder.QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCoder.PngByteQRCode(qrCodeData))
            {
                byte[] graphic = qrCode.GetGraphic(20);
                using (var ms = new System.IO.MemoryStream(graphic))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    QRCodeImage = bitmap;
                }
            }
        }
        catch { }
    }

    private void LoadAvailableDisplays()
    {
        AvailableDisplays.Clear();
        AvailableDisplays.Add(new DisplayItem { DeviceName = string.Empty, DisplayName = "Automatic (Secondary Display)" });
        try
        {
            var screens = WpfScreenHelper.Screen.AllScreens.ToList();
            for (int i = 0; i < screens.Count; i++)
            {
                var screen = screens[i];
                var label = $"Display {i + 1} {(screen.Primary ? "(Primary)" : "")} - {screen.Bounds.Width}x{screen.Bounds.Height}";
                AvailableDisplays.Add(new DisplayItem { DeviceName = screen.DeviceName, DisplayName = label });
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task UploadHymnbookAsync()
    {
        if (string.IsNullOrWhiteSpace(CustomHymnbookName))
        {
            HymnUploadStatus = "Please enter a Hymnbook Name first!";
            return;
        }

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            DefaultExt = ".pdf",
            Filter = "All Supported Files (*.pdf;*.txt;*.json;*.csv;*.xml;*.docx)|*.pdf;*.txt;*.json;*.csv;*.xml;*.docx|PDF Files (*.pdf)|*.pdf|Text Files (*.txt)|*.txt|JSON Files (*.json)|*.json|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Select Hymnbook Document File (PDF, TXT, JSON, CSV, DOCX)"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                HymnUploadStatus = "Extracting document and parsing hymns...";
                var hymns = await ChurchAI.App.Helpers.DocumentTextExtractor.ExtractHymnsFromFileAsync(dlg.FileName);
                if (hymns.Count == 0)
                {
                    HymnUploadStatus = "No valid hymns found in file! Check format (numbers followed by lyrics).";
                    return;
                }

                await _hymnRepository.SaveHymnBookAsync(CustomHymnbookName, hymns);
                HymnUploadStatus = $"Successfully uploaded {hymns.Count} hymns to '{CustomHymnbookName}'!";
                
                // Refresh list
                await LoadHymnbooksAsync();
                SelectedHymnbook = CustomHymnbookName;
                CustomHymnbookName = string.Empty;
            }
            catch (System.Exception ex)
            {
                HymnUploadStatus = $"Error parsing file: {ex.Message}";
            }
        }
    }

    private static List<Hymn> ParseHymns(string fileContent)
    {
        var lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        var hymns = new List<Hymn>();
        Hymn? currentHymn = null;
        var lyricsBuilder = new System.Text.StringBuilder();

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                if (currentHymn != null && lyricsBuilder.Length > 0)
                {
                    lyricsBuilder.AppendLine();
                }
                continue;
            }

            if (int.TryParse(trimmed, out int num) && num > 0 && num < 1000)
            {
                if (currentHymn != null)
                {
                    currentHymn.Lyrics = lyricsBuilder.ToString().Trim();
                    currentHymn.Title = ExtractTitleFromLyrics(currentHymn.Lyrics);
                    hymns.Add(currentHymn);
                }

                currentHymn = new Hymn
                {
                    Number = num,
                    Title = string.Empty
                };
                lyricsBuilder.Clear();
            }
            else
            {
                if (currentHymn != null)
                {
                    lyricsBuilder.AppendLine(rawLine);
                }
            }
        }

        if (currentHymn != null)
        {
            currentHymn.Lyrics = lyricsBuilder.ToString().Trim();
            currentHymn.Title = ExtractTitleFromLyrics(currentHymn.Lyrics);
            hymns.Add(currentHymn);
        }

        return hymns;
    }

    private static string ExtractTitleFromLyrics(string lyrics)
    {
        if (string.IsNullOrWhiteSpace(lyrics)) return "Untitled Hymn";
        var firstLine = lyrics.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                              .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
        if (string.IsNullOrEmpty(firstLine)) return "Untitled Hymn";
        var cleaned = firstLine;
        if (char.IsDigit(cleaned[0]))
        {
            int index = 0;
            while (index < cleaned.Length && (char.IsDigit(cleaned[index]) || cleaned[index] == '.' || char.IsWhiteSpace(cleaned[index])))
            {
                index++;
            }
            cleaned = cleaned.Substring(index).Trim();
        }
        cleaned = cleaned.TrimEnd(',', '.', ';', ':', '!', '?');
        return string.IsNullOrEmpty(cleaned) ? "Untitled Hymn" : cleaned;
    }

    private async Task LoadHymnbooksAsync()
    {
        var books = await _hymnRepository.GetAvailableHymnBooksAsync();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            AvailableHymnbooks.Clear();
            foreach (var bk in books)
            {
                AvailableHymnbooks.Add(bk);
            }
            if (!AvailableHymnbooks.Contains(SelectedHymnbook))
            {
                SelectedHymnbook = "Default";
            }
        });
    }

    [RelayCommand]
    private async Task ToggleListeningAsync()
    {
        if (IsListening)
        {
            IsListening = false;
            SpeechStatus = "Microphone Idle";
            await _speechRecognitionService.StopListeningAsync();
        }
        else
        {
            IsListening = true;
            SpeechStatus = "Google listening...";
            await _speechRecognitionService.StartListeningAsync();
        }
    }

    private void OnAudioLevelUpdated(object? sender, AudioLevelUpdatedEventArgs e)
    {
        CurrentAudioLevel = e.AudioLevel * 5.0; // scale up for visual effect
        if (!IsListening && _speechRecognitionService.IsListening)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => IsListening = true);
        }
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        var text = e.Text;
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (text.StartsWith("["))
            {
                SpeechStatus = text.Trim('[', ']');
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                SpeechStatus = $"Heard: \"{text}\"";
            }
            IsListening = _speechRecognitionService.IsListening;
        });
    }

    public void Dispose()
    {
        _speechRecognitionService.SpeechRecognized -= OnSpeechRecognized;
        _speechRecognitionService.AudioLevelUpdated -= OnAudioLevelUpdated;
    }
}

public class DisplayItem
{
    public string DeviceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
