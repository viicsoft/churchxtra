using System.Collections.ObjectModel;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.App.Views;
using ChurchAI.Core.Models;

namespace ChurchAI.App.Services;

public class ProjectionService : IProjectionService
{
    private ProjectionWindow? _projectionWindow;
    private readonly ISettingsService _settingsService;
    private readonly INDIService _ndiService;
    private readonly IWebServerService _webServerService;
    private readonly IMediaService? _mediaService;
    private bool _isLowerThirds;
    public ObservableCollection<ProjectedItem> ProjectionQueue { get; } = new();

    public ProjectedItem? CurrentlyProjectedItem { get; private set; }
    public event EventHandler? CurrentlyProjectedItemChanged;
    public event EventHandler<ProjectedItem>? DeselectedLinesPushed;
    public event EventHandler? NextItemRequested;

    public ChurchAI.Core.Entities.LowerThirdItem? ActiveLowerThird { get; private set; }
    public event EventHandler<ChurchAI.Core.Entities.LowerThirdItem?>? ActiveLowerThirdChanged;

    public object? CurrentProjectionWindow => _projectionWindow;
    public event EventHandler? ProjectionWindowChanged;

    public ProjectionService(
        ISettingsService settingsService, 
        INDIService ndiService, 
        IWebServerService webServerService,
        IMediaService? mediaService = null)
    {
        _settingsService = settingsService;
        _ndiService = ndiService;
        _webServerService = webServerService;
        _mediaService = mediaService;

        _settingsService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ISettingsService.EnableNDIOutput) || e.PropertyName == nameof(ISettingsService.EnableWebServerOutput))
            {
                if (_settingsService.EnableNDIOutput || _settingsService.EnableWebServerOutput)
                {
                    System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
                    {
                        EnsureProjectionWindow(showOnPhysicalScreen: false);
                    });
                }
                else
                {
                    HideProjection();
                }
            }
        };

        // Auto-start background projection window on startup so live preview monitor and output capture are ready immediately
        System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
        {
            EnsureProjectionWindow(showOnPhysicalScreen: false);
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private void EnsureProjectionWindow(bool showOnPhysicalScreen = false)
    {
        if (_projectionWindow == null)
        {
            _projectionWindow = new ProjectionWindow();
            _projectionWindow.SetLowerThirds(_isLowerThirds);

            // Restore active background (Video, Image, or Preset) on window creation
            if (_mediaService != null)
            {
                _ = Task.Run(async () =>
                {
                    var activeMedia = await _mediaService.GetActiveBackgroundAsync();
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        if (activeMedia != null)
                        {
                            var opacity = CurrentlyProjectedItem != null && !string.IsNullOrEmpty(CurrentlyProjectedItem.Text)
                                ? _settingsService.BackgroundOverlayOpacity
                                : 0.0;
                            _projectionWindow?.SetBackgroundMedia(activeMedia, opacity);
                        }
                        else if (!string.IsNullOrEmpty(_settingsService.ActiveBackgroundColorHex))
                        {
                            _projectionWindow?.SetBackgroundColorHex(_settingsService.ActiveBackgroundColorHex);
                        }
                    });
                });
            }
            else if (!string.IsNullOrEmpty(_settingsService.ActiveBackgroundColorHex))
            {
                _projectionWindow.SetBackgroundColorHex(_settingsService.ActiveBackgroundColorHex);
            }

            // Position offscreen by default for NDI / Web capture before physical show
            _projectionWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
            _projectionWindow.Left = -3840;
            _projectionWindow.Top = -2160;
            _projectionWindow.Width = 1920;
            _projectionWindow.Height = 1080;
            _projectionWindow.WindowStyle = System.Windows.WindowStyle.None;

            _projectionWindow.DeselectedLinesCreated += (s, item) =>
            {
                DeselectedLinesPushed?.Invoke(this, item);
            };

            _projectionWindow.Part2ProjectRequested += (s, item) =>
            {
                CurrentlyProjectedItem = item;
                CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
            };

            _projectionWindow.NextRequested += (s, e) =>
            {
                NextItemRequested?.Invoke(this, EventArgs.Empty);
            };

            _projectionWindow.Closed += (s, e) => 
            {
                _ndiService.StopBroadcasting();
                _webServerService.StopBroadcasting();
                _projectionWindow = null;
                ProjectionWindowChanged?.Invoke(this, EventArgs.Empty);
            };

            // Show offscreen so WPF creates HWND handle and layout engine renders 1920x1080 frames
            _projectionWindow.Show();
            ProjectionWindowChanged?.Invoke(this, EventArgs.Empty);

            if (CurrentlyProjectedItem != null)
            {
                if (CurrentlyProjectedItem.Reference == "Logo")
                {
                    _projectionWindow.ShowLogo();
                }
                else if (string.IsNullOrEmpty(CurrentlyProjectedItem.Text))
                {
                    _projectionWindow.ClearContent();
                }
                else
                {
                    _projectionWindow.UpdateContent(CurrentlyProjectedItem.Reference, CurrentlyProjectedItem.Text);
                }
            }
            else
            {
                _projectionWindow.ClearContent();
            }
        }

        if (_settingsService.EnableNDIOutput && _projectionWindow != null)
        {
            _ndiService.StartBroadcasting(_projectionWindow);
        }
        if (_settingsService.EnableWebServerOutput && _projectionWindow != null)
        {
            _webServerService.StartBroadcasting(_projectionWindow);
        }

        if (showOnPhysicalScreen)
        {
            ShowProjectionOnScreen();
        }
    }

    public void ShowProjection()
    {
        EnsureProjectionWindow(showOnPhysicalScreen: true);
    }

    private void ShowProjectionOnScreen()
    {
        if (_projectionWindow == null) return;

        var screens = WpfScreenHelper.Screen.AllScreens;
        WpfScreenHelper.Screen? targetScreen = null;

        if (!string.IsNullOrEmpty(_settingsService.SelectedDisplayDevice))
        {
            targetScreen = screens.FirstOrDefault(s => s.DeviceName == _settingsService.SelectedDisplayDevice);
        }

        if (targetScreen == null && _settingsService.AutoRouteSecondaryDisplay && screens.Count() > 1)
        {
            targetScreen = screens.FirstOrDefault(s => !s.Primary) ?? screens.ElementAt(1);
        }

        _projectionWindow.WindowState = System.Windows.WindowState.Normal;
        _projectionWindow.WindowStyle = System.Windows.WindowStyle.None;
        _projectionWindow.Topmost = true;

        if (targetScreen != null)
        {
            _projectionWindow.ConfigureDisplayMode(!targetScreen.Primary);
            _projectionWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
            var workingArea = targetScreen.WorkingArea;
            _projectionWindow.Left = workingArea.Left;
            _projectionWindow.Top = workingArea.Top;
            _projectionWindow.Width = workingArea.Width;
            _projectionWindow.Height = workingArea.Height;
        }
        else
        {
            // Fallback: Place on primary monitor
            var primaryScreen = screens.FirstOrDefault(s => s.Primary) ?? screens.FirstOrDefault();
            if (primaryScreen != null)
            {
                _projectionWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                var workingArea = primaryScreen.WorkingArea;
                _projectionWindow.Left = workingArea.Left;
                _projectionWindow.Top = workingArea.Top;
                _projectionWindow.Width = workingArea.Width;
                _projectionWindow.Height = workingArea.Height;
            }
        }

        _projectionWindow.Show();
        
        System.Windows.Application.Current?.Dispatcher?.InvokeAsync(() =>
        {
            if (_projectionWindow != null)
                _projectionWindow.WindowState = System.Windows.WindowState.Maximized;
        }, System.Windows.Threading.DispatcherPriority.Loaded);

        _projectionWindow.Activate();
        _projectionWindow.Focus();
    }

    public void HideProjection()
    {
        // Move projection window offscreen so video playback, lower thirds, and live card preview stay running
        if (_projectionWindow != null)
        {
            _projectionWindow.WindowState = System.Windows.WindowState.Normal;
            _projectionWindow.Left = -3840;
            _projectionWindow.Top = -2160;
            _projectionWindow.Width = 1920;
            _projectionWindow.Height = 1080;
            _projectionWindow.Topmost = false;
        }
    }

    public void ProjectVerse(string reference, string text, bool showWindow = false)
    {
        EnsureProjectionWindow(showWindow);

        string cleanedText = ChurchAI.Core.Helpers.TextSplitterHelper.CleanScriptureText(text);

        if (ActiveCustomTemplate != null)
        {
            _projectionWindow?.RenderCustomTemplate(
                ActiveCustomTemplate,
                title: reference,
                subtitle: cleanedText,
                tagText: "SCRIPTURE",
                reference: reference,
                verseText: cleanedText
            );
        }
        else
        {
            _projectionWindow?.HideCustomLowerThird();
            _projectionWindow?.SetTextAlignment(_currentTextAlignment);
            _projectionWindow?.UpdateContent(reference, cleanedText);
        }

        // Ensure active background has dark tint overlay enabled for scripture readability
        if (_mediaService != null && _projectionWindow != null)
        {
            _ = Task.Run(async () =>
            {
                var activeMedia = await _mediaService.GetActiveBackgroundAsync();
                if (activeMedia != null)
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        _projectionWindow?.SetBackgroundMedia(activeMedia, _settingsService.BackgroundOverlayOpacity);
                    });
                }
            });
        }

        CurrentlyProjectedItem = new ProjectedItem { Reference = reference, Text = cleanedText };
        CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SplitQueueItem(ProjectedItem item, string firstPart, string secondPart)
    {
        if (item == null) return;
        int index = ProjectionQueue.IndexOf(item);
        if (index >= 0)
        {
            item.Text = firstPart;
            ProjectionQueue.Insert(index + 1, new ProjectedItem
            {
                Reference = item.Reference + " (Cont.)",
                Text = secondPart
            });
        }

        // If this item was currently projected, refresh live display
        if (CurrentlyProjectedItem != null && (CurrentlyProjectedItem == item || CurrentlyProjectedItem.Reference == item.Reference))
        {
            ProjectVerse(item.Reference, item.Text);
        }
    }

    public void ClearScreen()
    {
        EnsureProjectionWindow(false);
        _settingsService.ActiveBackgroundMediaId = 0;
        _projectionWindow?.ClearContent();
        _projectionWindow?.HideLowerThirdOverlay();
        ActiveLowerThird = null;
        CurrentlyProjectedItem = null;
        CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
        ActiveLowerThirdChanged?.Invoke(this, null);
    }
    
    public void ShowLogo()
    {
        EnsureProjectionWindow(false);
        _projectionWindow?.ShowLogo();
        CurrentlyProjectedItem = new ProjectedItem { Reference = "Logo", Text = "" };
        CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public void AddToQueue(string reference, string text)
    {
        ProjectionQueue.Add(new ProjectedItem { Reference = reference, Text = text });
    }

    public void RemoveFromQueue(ProjectedItem item)
    {
        if (ProjectionQueue.Contains(item))
        {
            ProjectionQueue.Remove(item);
        }
    }

    private int _currentQueueIndex = -1;

    public void ProjectNext()
    {
        if (ProjectionQueue.Count == 0) return;
        _currentQueueIndex++;
        if (_currentQueueIndex >= ProjectionQueue.Count) _currentQueueIndex = 0; // loop back
        
        var nextItem = ProjectionQueue[_currentQueueIndex];
        ProjectVerse(nextItem.Reference, nextItem.Text);
    }

    public void ProjectPrevious()
    {
        if (ProjectionQueue.Count == 0) return;
        _currentQueueIndex--;
        if (_currentQueueIndex < 0) _currentQueueIndex = ProjectionQueue.Count - 1; // loop to end
        
        var prevItem = ProjectionQueue[_currentQueueIndex];
        ProjectVerse(prevItem.Reference, prevItem.Text);
    }

    public void SetBackgroundMedia(ChurchAI.Core.Entities.MediaItem? mediaItem)
    {
        EnsureProjectionWindow(false);
        if (mediaItem == null)
        {
            _settingsService.ActiveBackgroundMediaId = 0;
            _projectionWindow?.SetBackgroundMedia(null);
        }
        else
        {
            _settingsService.ActiveBackgroundMediaId = mediaItem.Id;
            double opacity = (CurrentlyProjectedItem != null && !string.IsNullOrEmpty(CurrentlyProjectedItem.Text))
                ? _settingsService.BackgroundOverlayOpacity
                : 0.0;
            _projectionWindow?.SetBackgroundMedia(mediaItem, opacity);

            if (CurrentlyProjectedItem == null || string.IsNullOrEmpty(CurrentlyProjectedItem.Text))
            {
                CurrentlyProjectedItem = new ProjectedItem { Reference = mediaItem.Name, Text = string.Empty };
                CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void SetBackgroundColorHex(string hexColor)
    {
        EnsureProjectionWindow(false);
        _settingsService.ActiveBackgroundMediaId = 0;
        _settingsService.ActiveBackgroundColorHex = hexColor;
        _projectionWindow?.SetBackgroundColorHex(hexColor);

        if (CurrentlyProjectedItem == null || string.IsNullOrEmpty(CurrentlyProjectedItem.Text))
        {
            CurrentlyProjectedItem = new ProjectedItem { Reference = "Solid Color Background", Text = string.Empty };
            CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ProjectStandaloneMedia(ChurchAI.Core.Entities.MediaItem mediaItem)
    {
        EnsureProjectionWindow(showOnPhysicalScreen: false);
        _projectionWindow?.ClearContent();
        _projectionWindow?.SetBackgroundMedia(mediaItem, overlayOpacity: 0.0);
        _settingsService.ActiveBackgroundMediaId = mediaItem.Id;

        CurrentlyProjectedItem = new ProjectedItem { Reference = mediaItem.Name, Text = string.Empty };
        CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ProjectLowerThird(ChurchAI.Core.Entities.LowerThirdItem lowerThird)
    {
        EnsureProjectionWindow(false);
        ActiveLowerThird = lowerThird;
        _projectionWindow?.SetLowerThirdOverlay(lowerThird);
        ActiveLowerThirdChanged?.Invoke(this, ActiveLowerThird);
    }

    public void UpdateActiveLowerThirdText(string title, string subtitle, string tagText)
    {
        if (ActiveLowerThird != null)
        {
            ActiveLowerThird.Title = title;
            ActiveLowerThird.Subtitle = subtitle;
            ActiveLowerThird.TagText = tagText;
            _projectionWindow?.UpdateLowerThirdText(title, subtitle, tagText);
            ActiveLowerThirdChanged?.Invoke(this, ActiveLowerThird);
        }
    }

    public void UpdateActiveLowerThirdColors(string tagBg, string tagTxt, string titleBg, string titleTxt, string subBg, string subTxt)
    {
        if (ActiveLowerThird != null)
        {
            ActiveLowerThird.TagBgColorHex = tagBg;
            ActiveLowerThird.TagTextColorHex = tagTxt;
            ActiveLowerThird.TitleBgColorHex = titleBg;
            ActiveLowerThird.TitleTextColorHex = titleTxt;
            ActiveLowerThird.SubtitleBgColorHex = subBg;
            ActiveLowerThird.SubtitleTextColorHex = subTxt;
            _projectionWindow?.SetLowerThirdOverlay(ActiveLowerThird);
            ActiveLowerThirdChanged?.Invoke(this, ActiveLowerThird);
        }
    }

    public ChurchAI.Core.Entities.ScriptureLowerThirdTemplate ActiveScriptureTemplate { get; private set; } = ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.PillCapsuleSand;
    public ChurchAI.Core.Entities.CustomTemplate? ActiveCustomTemplate { get; private set; }
    private bool _isLowerThirdsMode = false;

    public void SetLowerThirds(bool isLowerThirds)
    {
        _isLowerThirdsMode = isLowerThirds;
        EnsureProjectionWindow(false);
        _projectionWindow?.SetLowerThirdsMode(isLowerThirds);
        if (!isLowerThirds)
        {
            _projectionWindow?.HideCustomLowerThird();
        }
        CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
    }

    private string _currentTextAlignment = "Center";

    public void SetTextAlignment(string alignment)
    {
        _currentTextAlignment = alignment ?? "Center";
        EnsureProjectionWindow(false);
        _projectionWindow?.SetTextAlignment(_currentTextAlignment);
    }

    public void SetScriptureLowerThirdTemplate(ChurchAI.Core.Entities.ScriptureLowerThirdTemplate template)
    {
        ActiveScriptureTemplate = template;
        ActiveCustomTemplate = null;
        EnsureProjectionWindow(false);
        _projectionWindow?.HideCustomLowerThird();
        _projectionWindow?.SetScriptureLowerThirdTemplate(template);
        CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetActiveCustomTemplate(ChurchAI.Core.Entities.CustomTemplate? template)
    {
        ActiveCustomTemplate = template;
        EnsureProjectionWindow(false);
        if (template != null)
        {
            if (CurrentlyProjectedItem != null)
            {
                _projectionWindow?.RenderCustomTemplate(
                    template,
                    title: CurrentlyProjectedItem.Reference,
                    subtitle: CurrentlyProjectedItem.Text,
                    tagText: "SCRIPTURE",
                    reference: CurrentlyProjectedItem.Reference,
                    verseText: CurrentlyProjectedItem.Text
                );
            }
            else
            {
                _projectionWindow?.RenderCustomTemplate(template);
            }
        }
        else
        {
            _projectionWindow?.HideCustomLowerThird();
        }
        CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
    }

    public void HideLowerThird()
    {
        ActiveLowerThird = null;
        ActiveCustomTemplate = null;
        _projectionWindow?.HideLowerThirdOverlay();
        _projectionWindow?.HideCustomLowerThird();
        ActiveLowerThirdChanged?.Invoke(this, null);
    }
}
