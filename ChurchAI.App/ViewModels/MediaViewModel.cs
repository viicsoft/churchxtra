using System.IO;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Entities;
using Microsoft.Win32;

namespace ChurchAI.App.ViewModels;

public partial class MediaViewModel : ObservableObject
{
    private readonly IMediaService _mediaService;
    private readonly IProjectionService _projectionService;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<MediaItem> _mediaItems = new();

    [ObservableProperty]
    private ObservableCollection<MediaItem> _youTubeItems = new();

    [ObservableProperty]
    private ObservableCollection<MediaItem> _uploadedMediaItems = new();

    [ObservableProperty]
    private ObservableCollection<MediaItem> _backgroundItems = new();

    [ObservableProperty]
    private MediaItem? _selectedMediaItem;

    [ObservableProperty]
    private int _selectedSubTabIndex = 0; // 0: YouTube, 1: Upload, 2: Backgrounds

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isAddYouTubeModalOpen;

    [ObservableProperty]
    private string _youTubeUrlInput = string.Empty;

    [ObservableProperty]
    private string _youTubeTitleInput = string.Empty;

    [ObservableProperty]
    private string _youTubeCategoryInput = "Video";

    public MediaViewModel(IMediaService mediaService, IProjectionService projectionService, ISettingsService settingsService)
    {
        _mediaService = mediaService;
        _projectionService = projectionService;
        _settingsService = settingsService;

        _ = LoadMediaAsync();
    }

    [RelayCommand]
    public async Task LoadMediaAsync()
    {
        try
        {
            var items = await _mediaService.GetMediaItemsAsync();
            MediaItems = new ObservableCollection<MediaItem>(items);

            // Filter into 3 dedicated sub-collections
            YouTubeItems = new ObservableCollection<MediaItem>(
                MediaItems.Where(i => i.Type == MediaType.YouTube)
            );

            UploadedMediaItems = new ObservableCollection<MediaItem>(
                MediaItems.Where(i => i.Type == MediaType.Image || i.Type == MediaType.Video)
            );

            BackgroundItems = new ObservableCollection<MediaItem>(
                MediaItems.Where(i => i.Type == MediaType.Color || i.Category == "Preset" || i.Category == "Background" || i.Type == MediaType.Image)
            );
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading media: {ex.Message}";
        }
    }

    [RelayCommand]
    public void SelectSubTab(string indexStr)
    {
        if (int.TryParse(indexStr, out int idx))
        {
            SelectedSubTabIndex = idx;
        }
    }

    [RelayCommand]
    public async Task ImportMediaAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import Media for Projection",
            Filter = "All Supported Media (*.png;*.jpg;*.jpeg;*.webp;*.mp4;*.avi;*.mov;*.wmv)|*.png;*.jpg;*.jpeg;*.webp;*.mp4;*.avi;*.mov;*.wmv|Images (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Videos (*.mp4;*.avi;*.mov;*.wmv)|*.mp4;*.avi;*.mov;*.wmv",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            int importedCount = 0;
            foreach (var fileName in dialog.FileNames)
            {
                try
                {
                    await _mediaService.ImportMediaAsync(fileName, "Custom");
                    importedCount++;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error importing {Path.GetFileName(fileName)}: {ex.Message}";
                }
            }

            if (importedCount > 0)
            {
                StatusMessage = $"Successfully imported {importedCount} media item(s).";
                SelectedSubTabIndex = 1; // Auto switch to Media Upload tab
                await LoadMediaAsync();
            }
        }
    }

    [RelayCommand]
    public void SetAsBackground(MediaItem? item)
    {
        var target = item ?? SelectedMediaItem;
        if (target == null) return;

        _settingsService.ActiveBackgroundMediaId = target.Id;
        _projectionService.SetBackgroundMedia(target);
        StatusMessage = $"Projection background changed to '{target.Name}'";
    }

    [RelayCommand]
    public void SetBlackBackground()
    {
        _settingsService.ActiveBackgroundMediaId = 0;
        _settingsService.ActiveBackgroundColorHex = "#000000";
        _projectionService.SetBackgroundColorHex("#000000");
        StatusMessage = "Projection background set to default Black";
    }

    [RelayCommand]
    public void ProjectLive(MediaItem? item)
    {
        var target = item ?? SelectedMediaItem;
        if (target == null) return;

        _projectionService.ProjectStandaloneMedia(target);
        StatusMessage = $"Now projecting '{target.Name}' live full screen";
    }

    [RelayCommand]
    public async Task DeleteMediaAsync(MediaItem? item)
    {
        var target = item ?? SelectedMediaItem;
        if (target == null || target.IsPreset) return;

        await _mediaService.DeleteMediaAsync(target.Id);
        StatusMessage = $"Deleted media '{target.Name}'";
        await LoadMediaAsync();
    }

    [RelayCommand]
    public void OpenAddYouTubeModal()
    {
        YouTubeUrlInput = string.Empty;
        YouTubeTitleInput = string.Empty;
        YouTubeCategoryInput = "Video";
        IsAddYouTubeModalOpen = true;
    }

    [RelayCommand]
    public void CloseAddYouTubeModal()
    {
        IsAddYouTubeModalOpen = false;
    }

    [RelayCommand]
    public async Task AddYouTubeVideoAsync()
    {
        if (string.IsNullOrWhiteSpace(YouTubeUrlInput))
        {
            StatusMessage = "Please enter a valid YouTube video URL or ID.";
            return;
        }

        string videoId = ExtractYouTubeVideoId(YouTubeUrlInput);
        if (string.IsNullOrWhiteSpace(videoId))
        {
            StatusMessage = "Could not extract a valid YouTube video ID.";
            return;
        }

        string title = string.IsNullOrWhiteSpace(YouTubeTitleInput)
            ? $"YouTube Video ({videoId})"
            : YouTubeTitleInput;

        string thumbnailUrl = $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";

        var item = new MediaItem
        {
            Name = title,
            FilePath = YouTubeUrlInput.Trim(),
            Type = MediaType.YouTube,
            Category = string.IsNullOrWhiteSpace(YouTubeCategoryInput) ? "Video" : YouTubeCategoryInput,
            IsPreset = false,
            ThumbnailPath = thumbnailUrl,
            DateAdded = DateTime.UtcNow
        };

        await _mediaService.AddMediaAsync(item);
        IsAddYouTubeModalOpen = false;
        StatusMessage = $"Added YouTube video '{title}' to media library.";
        SelectedSubTabIndex = 0; // Auto switch to YouTube tab
        await LoadMediaAsync();
    }

    private string ExtractYouTubeVideoId(string urlOrId)
    {
        if (string.IsNullOrWhiteSpace(urlOrId)) return string.Empty;
        urlOrId = urlOrId.Trim();
        if (urlOrId.Length == 11 && !urlOrId.Contains("/")) return urlOrId;

        try
        {
            var uri = new Uri(urlOrId);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (query.AllKeys.Contains("v")) return query["v"] ?? urlOrId;
            if (uri.Host.Contains("youtu.be")) return uri.AbsolutePath.Trim('/');
            if (uri.AbsolutePath.Contains("/embed/")) return uri.AbsolutePath.Replace("/embed/", "").Trim('/');
        }
        catch { }

        return urlOrId;
    }
}
