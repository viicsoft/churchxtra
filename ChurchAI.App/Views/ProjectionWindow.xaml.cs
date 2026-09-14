using System.Windows;
using System.Windows.Controls;

namespace ChurchAI.App.Views;

public partial class ProjectionWindow : Window
{
    public event EventHandler? NextRequested;
    public event EventHandler? PreviousRequested;

    public ProjectionWindow()
    {
        InitializeComponent();
    }

    public void ConfigureDisplayMode(bool isSecondaryDisplay)
    {
        // ProjectionWindow is pure output canvas -- LocalControlsOverlay stays collapsed
        LocalControlsOverlay.Visibility = Visibility.Collapsed;
    }

    public bool IsLowerThirdsActive() => 
        LowerThirdOverlayHost.Visibility == Visibility.Visible || 
        LowerThirdPositionContainer.Visibility == Visibility.Visible ||
        ScriptureLowerThirdHost.Visibility == Visibility.Visible ||
        (CustomLowerThirdHost != null && CustomLowerThirdHost.Visibility == Visibility.Visible);

    private bool IsScriptureLtActive()
    {
        if (ScriptureLowerThirdHost != null && ScriptureLowerThirdHost.Visibility == Visibility.Visible && 
            LowerThirdOverlayHost != null && LowerThirdOverlayHost.Visibility == Visibility.Collapsed)
        {
            return true;
        }
        if (LowerThirdOverlayHost != null && LowerThirdOverlayHost.Visibility == Visibility.Visible && 
            ScriptureLowerThirdHost != null && ScriptureLowerThirdHost.Visibility == Visibility.Collapsed)
        {
            return false;
        }
        return _isScriptureLowerThirdsMode;
    }

    public double GetLtTranslateX()
    {
        if (IsScriptureLtActive() && ScriptureLtTranslateTransform != null)
            return ScriptureLtTranslateTransform.X;
        return LtTranslateTransform?.X ?? 0;
    }

    public double GetLtTranslateY()
    {
        if (IsScriptureLtActive() && ScriptureLtTranslateTransform != null)
            return ScriptureLtTranslateTransform.Y;
        return LtTranslateTransform?.Y ?? 0;
    }

    public void SetLtTranslate(double x, double y)
    {
        if (IsScriptureLtActive())
        {
            if (ScriptureLtTranslateTransform != null)
            {
                ScriptureLtTranslateTransform.X = x;
                ScriptureLtTranslateTransform.Y = y;
                IsPositionPendingCommit = true;
                ShowCommitPositionButton();
            }
        }
        else
        {
            if (LtTranslateTransform != null)
            {
                LtTranslateTransform.X = x;
                LtTranslateTransform.Y = y;
                IsPositionPendingCommit = true;
                ShowCommitPositionButton();
            }
        }
    }

    public void SetLtScaleXValue(double val)
    {
        if (LtScaleXSlider != null)
        {
            LtScaleXSlider.Value = val;
        }
    }

    public void SetLtScaleYValue(double val)
    {
        if (LtScaleYSlider != null)
        {
            LtScaleYSlider.Value = val;
        }
    }

    public void SetLtLockAspect(bool isLocked)
    {
        if (LtLockAspectCheck != null)
        {
            LtLockAspectCheck.IsChecked = isLocked;
        }
    }

    public void TriggerCommitLtPos()
    {
        CommitLtPosBtn_Click(this, new RoutedEventArgs());
    }

    public void TriggerNextBtn()
    {
        NextBtn_Click(this, new RoutedEventArgs());
    }
    
    private void NextButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        NextRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true; // Prevent closing the window from the border click
    }

    private void PrevButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        PreviousRequested?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            this.WindowState = WindowState.Normal;
            this.Left = -3840;
            this.Top = -2160;
            this.Width = 1920;
            this.Height = 1080;
            this.Topmost = false;
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Right || e.Key == System.Windows.Input.Key.Space)
        {
            NextRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Left)
        {
            PreviousRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void Window_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject depObj)
        {
            if (IsControlOrParent<Slider>(depObj) || IsControlOrParent<Border>(depObj, "LocalControlsOverlay") || IsControlOrParent<Button>(depObj))
            {
                return; // Do NOT close window when interacting with controls or slider!
            }
        }
    }

    private static bool IsControlOrParent<T>(DependencyObject child, string? name = null) where T : DependencyObject
    {
        DependencyObject? parent = child;
        while (parent != null)
        {
            if (parent is T control)
            {
                if (name == null || (control is FrameworkElement fe && fe.Name == name))
                {
                    return true;
                }
            }
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        }
        return false;
    }

    private string _currentRawReference = string.Empty;
    private string _currentRawText = string.Empty;
    private List<string> _currentLines = new();

    public event EventHandler<ChurchAI.Core.Models.ProjectedItem>? DeselectedLinesCreated;

    private bool _isPart2Projected = false;

    public void UpdateContent(string reference, string text)
    {
        LogoViewbox.Visibility = Visibility.Collapsed;
        ContentBorder.Visibility = Visibility.Visible;
        
        _currentRawReference = reference ?? string.Empty;
        _currentRawText = text ?? string.Empty;

        string cleanedText = ChurchAI.Core.Helpers.TextSplitterHelper.CleanScriptureText(text);

        bool isHymn = (reference ?? "").Contains("Hymn", StringComparison.OrdinalIgnoreCase);
        if (isHymn)
        {
            // Strip leading stanza/verse numbers (e.g. "4. So we..." -> "So we...")
            cleanedText = System.Text.RegularExpressions.Regex.Replace(cleanedText, @"^\d+[\.\s\-]+", "");
            ReferenceText.Text = string.Empty;
            ReferenceText.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Prepend verse number superscript/prefix if reference contains verse number (e.g. Song of Solomon 1:7)
            var match = System.Text.RegularExpressions.Regex.Match(reference ?? "", @":(\d+)");
            if (match.Success && !cleanedText.StartsWith(match.Groups[1].Value))
            {
                var verseNumStr = match.Groups[1].Value;
                string superscriptNum = ToSuperscript(verseNumStr);
                cleanedText = $"{superscriptNum} {cleanedText}";
            }

            ReferenceText.Text = reference;
            ReferenceText.Visibility = Visibility.Visible;
        }

        VerseText.Text = cleanedText;
        VerseText.TextAlignment = _currentAlignment;

        // 1. Calculate estimated visual line count based on 30 characters per line
        var rawLines = cleanedText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        int estVisualLines = 0;
        foreach (var l in rawLines)
        {
            estVisualLines += Math.Max(1, (int)Math.Ceiling(l.Trim().Length / 30.0));
        }

        int charCount = cleanedText.Length;

        if (!_isScriptureLowerThirdsMode)
        {
            if (estVisualLines <= 1 && charCount <= 45)
            {
                VerseText.FontSize = 110;
            }
            else if (estVisualLines <= 1 && charCount <= 80)
            {
                VerseText.FontSize = 90;
            }
            else if (estVisualLines <= 2 && charCount <= 120)
            {
                VerseText.FontSize = 78;
            }
            else if (estVisualLines <= 3 && charCount <= 180)
            {
                VerseText.FontSize = 64;
            }
            else if (estVisualLines <= 4 && charCount <= 260)
            {
                VerseText.FontSize = 52;
            }
            else if (estVisualLines <= 6)
            {
                VerseText.FontSize = 42;
            }
            else
            {
                VerseText.FontSize = 34;
            }

            AdjustFontSizeToFitContainer();
        }

        _currentLines = cleanedText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

        if (_isScriptureLowerThirdsMode)
        {
            ContentBorder.Visibility = Visibility.Collapsed;
            if (ScriptureLowerThirdHost != null)
            {
                ScriptureLowerThirdHost.Visibility = Visibility.Visible;
            }
            ApplyScriptureLowerThird();
        }
        else
        {
            if (ScriptureLowerThirdHost != null)
            {
                ScriptureLowerThirdHost.Visibility = Visibility.Collapsed;
            }
            ContentBorder.Visibility = Visibility.Visible;
        }
    }

    private void AdjustFontSizeToFitContainer()
    {
        Dispatcher.InvokeAsync(() =>
        {
            VerseText.UpdateLayout();
            double containerHeight = ContentTextGrid.ActualHeight > 0 ? ContentTextGrid.ActualHeight : 600;
            if (ReferenceText.Visibility == Visibility.Visible && ReferenceText.ActualHeight > 0)
            {
                containerHeight -= (ReferenceText.ActualHeight + 20);
            }

            while (VerseText.ActualHeight > containerHeight && VerseText.FontSize > 20)
            {
                VerseText.FontSize -= 2;
                VerseText.UpdateLayout();
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private TextAlignment _currentAlignment = TextAlignment.Center;

    public void SetTextAlignment(string alignment)
    {
        if (Enum.TryParse<TextAlignment>(alignment, true, out var result))
        {
            _currentAlignment = result;
        }
        else
        {
            _currentAlignment = TextAlignment.Center;
        }

        if (VerseText != null)
        {
            VerseText.TextAlignment = _currentAlignment;
        }
    }

    private bool _isScriptureLowerThirdsMode = false;
    private ChurchAI.Core.Entities.ScriptureLowerThirdTemplate _activeScriptureTemplate = ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.PillCapsuleSand;

    public void SetLowerThirdsMode(bool isLowerThirds)
    {
        _isScriptureLowerThirdsMode = isLowerThirds;
        if (isLowerThirds)
        {
            ContentBorder.Visibility = Visibility.Collapsed;
            ScriptureLowerThirdHost.Visibility = Visibility.Visible;
            BackgroundBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Transparent);
            ApplyScriptureLowerThird();
        }
        else
        {
            ScriptureLowerThirdHost.Visibility = Visibility.Collapsed;
            ContentBorder.Visibility = Visibility.Visible;
            BackgroundBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        }
    }

    public void SetScriptureLowerThirdTemplate(ChurchAI.Core.Entities.ScriptureLowerThirdTemplate template)
    {
        _activeScriptureTemplate = template;
        ApplyScriptureLowerThird();
    }

    private void ApplyScriptureLowerThird()
    {
        if (!_isScriptureLowerThirdsMode || ScriptureLowerThirdHost == null) return;

        // Hide all 5 template grids
        TplPillCapsuleSand.Visibility = Visibility.Collapsed;
        TplDualStackedCoral.Visibility = Visibility.Collapsed;
        TplDualMintGlass.Visibility = Visibility.Collapsed;
        TplPastelSageRose.Visibility = Visibility.Collapsed;
        TplCrimsonVous.Visibility = Visibility.Collapsed;

        string refText = string.IsNullOrWhiteSpace(_currentRawReference) ? "SCRIPTURE" : _currentRawReference;
        string verseText = ChurchAI.Core.Helpers.TextSplitterHelper.CleanScriptureText(string.IsNullOrWhiteSpace(_currentRawText) ? "" : _currentRawText);

        switch (_activeScriptureTemplate)
        {
            case ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.PillCapsuleSand:
                TplPillCapsuleSand.Visibility = Visibility.Visible;
                TplSandVerseText.Text = verseText;
                TplSandRefText.Text = refText.ToUpper();
                break;

            case ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.DualStackedCoralOffWhite:
                TplDualStackedCoral.Visibility = Visibility.Visible;
                TplCoralVerseText.Text = verseText.ToUpper();
                TplCoralRefText.Text = $"SCRIPTURE READING • {refText.ToUpper()}";
                break;

            case ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.DualMintGlassmorphism:
                TplDualMintGlass.Visibility = Visibility.Visible;
                TplMintVerseText.Text = verseText.ToUpper();
                TplMintRefText.Text = refText.ToUpper();
                break;

            case ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.PastelSageRosePill:
                TplPastelSageRose.Visibility = Visibility.Visible;
                TplSageVerseText.Text = verseText;
                TplSageRefText.Text = refText.Replace(" ", "\n");
                break;

            case ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.CrimsonVousBroadcast:
                TplCrimsonVous.Visibility = Visibility.Visible;
                TplCrimsonVerseText.Text = verseText.ToUpper();
                TplCrimsonRefText.Text = refText.ToUpper();
                break;
        }
    }

    public event EventHandler<ChurchAI.Core.Models.ProjectedItem>? Part2ProjectRequested;

    private void NextBtn_Click(object sender, RoutedEventArgs e)
    {
        NextRequested?.Invoke(this, EventArgs.Empty);
    }

    private List<string> GetWrappedLines(string fullText, double fontSize, double containerWidth)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(fullText)) return lines;

        var typeface = new System.Windows.Media.Typeface(VerseText.FontFamily, VerseText.FontStyle, VerseText.FontWeight, VerseText.FontStretch);
        var rawParagraphs = fullText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var paragraph in rawParagraphs)
        {
            var words = paragraph.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            string currentLine = "";
            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
                var formattedText = new System.Windows.Media.FormattedText(
                    testLine,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    System.Windows.Media.Brushes.White,
                    System.Windows.Media.VisualTreeHelper.GetDpi(this).PixelsPerDip
                );

                if (formattedText.Width > containerWidth && !string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }
        }

        return lines;
    }

    private static string ToSuperscript(string numberStr)
    {
        string superscripts = "⁰¹²³⁴⁵⁶⁷⁸⁹";
        string result = "";
        foreach (char c in numberStr)
        {
            if (c >= '0' && c <= '9')
                result += superscripts[c - '0'];
            else
                result += c;
        }
        return result;
    }

    public void ClearContent()
    {
        LogoViewbox.Visibility = Visibility.Collapsed;
        ContentBorder.Visibility = Visibility.Visible;
        ReferenceText.Text = string.Empty;
        VerseText.Text = string.Empty;
        _currentRawReference = string.Empty;
        _currentRawText = string.Empty;

        if (ScriptureLowerThirdHost != null)
        {
            ScriptureLowerThirdHost.Visibility = Visibility.Collapsed;
        }

        // Clear & stop background video
        try { BackgroundVideo.Stop(); } catch { }
        BackgroundVideo.Source = null;
        BackgroundVideo.Visibility = Visibility.Collapsed;

        // Clear YouTube web player
        try
        {
            if (YouTubeWebPlayer.CoreWebView2 != null)
            {
                YouTubeWebPlayer.Source = new Uri("about:blank");
            }
        }
        catch { }
        YouTubeWebPlayer.Visibility = Visibility.Collapsed;

        // Clear background image
        BackgroundImage.Source = null;
        BackgroundImage.Visibility = Visibility.Collapsed;

        // Reset background to solid black
        BackgroundBorder.Background = System.Windows.Media.Brushes.Black;
        BackgroundTintOverlay.Visibility = Visibility.Collapsed;

        // Clear & hide Lower Third overlay graphic
        HideLowerThirdOverlay();
    }

    public void ShowLogo()
    {
        ContentBorder.Visibility = Visibility.Collapsed;
        LogoViewbox.Visibility = Visibility.Visible;
    }

    public void SetLowerThirds(bool isLowerThirds)
    {
        if (isLowerThirds)
        {
            this.Background = System.Windows.Media.Brushes.Transparent;
            BackgroundBorder.Background = System.Windows.Media.Brushes.Transparent;
            ContentBorder.VerticalAlignment = VerticalAlignment.Bottom;
            ContentBorder.Margin = new Thickness(50, 50, 50, 100);
            ContentBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 15, 23, 42)); // Dark Navy semi-transparent
            ReferenceText.FontSize = 30;
            VerseText.FontSize = 50;
        }
        else
        {
            this.Background = System.Windows.Media.Brushes.Black;
            BackgroundBorder.Background = System.Windows.Media.Brushes.Black;
            ContentBorder.VerticalAlignment = VerticalAlignment.Center;
            ContentBorder.Margin = new Thickness(50);
            ContentBorder.Background = System.Windows.Media.Brushes.Transparent;
            ReferenceText.FontSize = 40;
            VerseText.FontSize = 80;
        }
    }

    private void BackgroundVideo_MediaEnded(object sender, RoutedEventArgs e)
    {
        BackgroundVideo.Position = TimeSpan.Zero;
        BackgroundVideo.Play();
    }

    public void SetBackgroundColorHex(string hexColor)
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColor);
            BackgroundBorder.Background = new System.Windows.Media.SolidColorBrush(color);
            BackgroundImage.Visibility = Visibility.Collapsed;
            BackgroundVideo.Stop();
            BackgroundVideo.Visibility = Visibility.Collapsed;
            BackgroundTintOverlay.Visibility = Visibility.Collapsed;
        }
        catch { }
    }

    public void SetBackgroundMedia(ChurchAI.Core.Entities.MediaItem? mediaItem, double overlayOpacity = 0.35)
    {
        if (mediaItem == null)
        {
            SetBackgroundColorHex("#000000");
            return;
        }

        BackgroundTintOverlay.Opacity = overlayOpacity;
        BackgroundTintOverlay.Visibility = overlayOpacity > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (mediaItem.Type == ChurchAI.Core.Entities.MediaType.Color)
        {
            SetBackgroundColorHex(mediaItem.FilePath);
        }
        else if (mediaItem.Type == ChurchAI.Core.Entities.MediaType.Image)
        {
            if (System.IO.File.Exists(mediaItem.FilePath))
            {
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(mediaItem.FilePath, UriKind.Absolute);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    BackgroundImage.Source = bitmap;
                    BackgroundImage.Visibility = Visibility.Visible;

                    BackgroundVideo.Stop();
                    BackgroundVideo.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    SetBackgroundColorHex("#000000");
                }
            }
        }
        else if (mediaItem.Type == ChurchAI.Core.Entities.MediaType.Video)
        {
            if (System.IO.File.Exists(mediaItem.FilePath))
            {
                try
                {
                    YouTubeWebPlayer.Visibility = Visibility.Collapsed;
                    BackgroundImage.Visibility = Visibility.Collapsed;
                    var newUri = new Uri(mediaItem.FilePath, UriKind.Absolute);
                    if (BackgroundVideo.Source != newUri)
                    {
                        BackgroundVideo.Source = newUri;
                        BackgroundVideo.Position = TimeSpan.Zero;
                    }
                    BackgroundVideo.Visibility = Visibility.Visible;
                    BackgroundVideo.Play();
                }
                catch
                {
                    SetBackgroundColorHex("#000000");
                }
            }
        }
        else if (mediaItem.Type == ChurchAI.Core.Entities.MediaType.YouTube)
        {
            SetYouTubeVideo(mediaItem.FilePath);
        }
    }

    private bool _isYtHostMapped = false;

    private async Task EnsureYouTubeHostMappedAsync()
    {
        if (YouTubeWebPlayer.CoreWebView2 == null)
        {
            await YouTubeWebPlayer.EnsureCoreWebView2Async();
        }

        if (!_isYtHostMapped)
        {
            string ytFolder = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ChurchAI",
                "ytplayer"
            );
            System.IO.Directory.CreateDirectory(ytFolder);

            string htmlPath = System.IO.Path.Combine(ytFolder, "player.html");
            string playerHtml = @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <style>
        html, body { margin: 0; padding: 0; width: 100%; height: 100%; overflow: hidden; background: #000; }
        iframe { width: 100%; height: 100%; border: none; }
    </style>
</head>
<body>
    <div id=""player""></div>
    <script>
        var tag = document.createElement('script');
        tag.src = 'https://www.youtube.com/iframe_api';
        var firstScriptTag = document.getElementsByTagName('script')[0];
        firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

        var player;
        function onYouTubeIframeAPIReady() {
            var urlParams = new URLSearchParams(window.location.search);
            var videoId = urlParams.get('v');
            if (videoId) {
                player = new YT.Player('player', {
                    height: '100%',
                    width: '100%',
                    videoId: videoId,
                    playerVars: {
                        'autoplay': 1,
                        'controls': 1,
                        'rel': 0,
                        'enablejsapi': 1,
                        'origin': 'https://churchai.app'
                    },
                    events: {
                        'onReady': function(event) { event.target.playVideo(); }
                    }
                });
            }
        }
    </script>
</body>
</html>";
            System.IO.File.WriteAllText(htmlPath, playerHtml);

            YouTubeWebPlayer.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "churchai.app",
                ytFolder,
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
            );
            YouTubeWebPlayer.CoreWebView2.Settings.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";

            _isYtHostMapped = true;
        }
    }

    public async void SetYouTubeVideo(string videoUrlOrId)
    {
        try
        {
            BackgroundImage.Visibility = Visibility.Collapsed;
            BackgroundVideo.Stop();
            BackgroundVideo.Visibility = Visibility.Collapsed;

            string videoId = ExtractYouTubeVideoId(videoUrlOrId);
            if (!string.IsNullOrEmpty(videoId))
            {
                await EnsureYouTubeHostMappedAsync();
                YouTubeWebPlayer.Source = new Uri($"https://churchai.app/player.html?v={videoId}");
                YouTubeWebPlayer.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            SetBackgroundColorHex("#000000");
        }
    }

    private string ExtractYouTubeVideoId(string urlOrId)
    {
        if (string.IsNullOrWhiteSpace(urlOrId)) return string.Empty;
        urlOrId = urlOrId.Trim();

        if (urlOrId.Length == 11 && !urlOrId.Contains("/") && !urlOrId.Contains("."))
            return urlOrId;

        try
        {
            if (!urlOrId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !urlOrId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                urlOrId = "https://" + urlOrId;
            }

            var uri = new Uri(urlOrId);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (query.AllKeys.Contains("v") && !string.IsNullOrEmpty(query["v"]))
                return query["v"]!;

            if (uri.Host.Contains("youtu.be"))
                return uri.AbsolutePath.Trim('/');

            if (uri.AbsolutePath.Contains("/embed/"))
                return uri.AbsolutePath.Substring(uri.AbsolutePath.IndexOf("/embed/", StringComparison.OrdinalIgnoreCase) + 7).Trim('/');

            if (uri.AbsolutePath.Contains("/watch/"))
                return uri.AbsolutePath.Substring(uri.AbsolutePath.IndexOf("/watch/", StringComparison.OrdinalIgnoreCase) + 7).Trim('/');
        }
        catch { }

        return urlOrId;
    }

    public void SetLowerThirdOverlay(ChurchAI.Core.Entities.LowerThirdItem? lowerThird)
    {
        if (lowerThird == null)
        {
            HideLowerThirdOverlay();
            return;
        }

        // Set Position
        switch (lowerThird.Position)
        {
            case ChurchAI.Core.Entities.LowerThirdPosition.BottomLeft:
                LowerThirdPositionContainer.HorizontalAlignment = HorizontalAlignment.Left;
                LowerThirdPositionContainer.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            case ChurchAI.Core.Entities.LowerThirdPosition.BottomCenter:
                LowerThirdPositionContainer.HorizontalAlignment = HorizontalAlignment.Center;
                LowerThirdPositionContainer.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            case ChurchAI.Core.Entities.LowerThirdPosition.BottomRight:
                LowerThirdPositionContainer.HorizontalAlignment = HorizontalAlignment.Right;
                LowerThirdPositionContainer.VerticalAlignment = VerticalAlignment.Bottom;
                break;
            case ChurchAI.Core.Entities.LowerThirdPosition.TopLeft:
                LowerThirdPositionContainer.HorizontalAlignment = HorizontalAlignment.Left;
                LowerThirdPositionContainer.VerticalAlignment = VerticalAlignment.Top;
                break;
        }

        // Hide all styles first
        LtStyleBlueAngular.Visibility = Visibility.Collapsed;
        LtStyleGoldBadge.Visibility = Visibility.Collapsed;
        LtStyleCyberpunkPurple.Visibility = Visibility.Collapsed;
        LtStyleClassicNavy.Visibility = Visibility.Collapsed;

        // Show active style
        switch (lowerThird.StylePreset)
        {
            case ChurchAI.Core.Entities.LowerThirdStyle.BlueAngular:
                LtStyleBlueAngular.Visibility = Visibility.Visible;
                LtBlueTag.Text = lowerThird.TagText;
                LtBlueTitle.Text = lowerThird.Title;
                LtBlueSubtitle.Text = lowerThird.Subtitle;
                LtBlueTagBorder.Visibility = string.IsNullOrWhiteSpace(lowerThird.TagText) ? Visibility.Collapsed : Visibility.Visible;
                LtBlueSubBorder.Visibility = string.IsNullOrWhiteSpace(lowerThird.Subtitle) ? Visibility.Collapsed : Visibility.Visible;

                if (!string.IsNullOrWhiteSpace(lowerThird.TagBgColorHex)) LtBlueTagBorder.Background = ParseColor(lowerThird.TagBgColorHex, System.Windows.Media.Color.FromRgb(0, 56, 168));
                if (!string.IsNullOrWhiteSpace(lowerThird.TagTextColorHex)) LtBlueTag.Foreground = ParseColor(lowerThird.TagTextColorHex, System.Windows.Media.Colors.White);
                if (!string.IsNullOrWhiteSpace(lowerThird.TitleBgColorHex)) LtBlueTitleBorder.Background = ParseColor(lowerThird.TitleBgColorHex, System.Windows.Media.Color.FromRgb(241, 245, 249));
                if (!string.IsNullOrWhiteSpace(lowerThird.TitleTextColorHex)) LtBlueTitle.Foreground = ParseColor(lowerThird.TitleTextColorHex, System.Windows.Media.Color.FromRgb(15, 23, 42));
                if (!string.IsNullOrWhiteSpace(lowerThird.SubtitleBgColorHex)) LtBlueSubBorder.Background = ParseColor(lowerThird.SubtitleBgColorHex, System.Windows.Media.Color.FromRgb(2, 132, 199));
                if (!string.IsNullOrWhiteSpace(lowerThird.SubtitleTextColorHex)) LtBlueSubtitle.Foreground = ParseColor(lowerThird.SubtitleTextColorHex, System.Windows.Media.Colors.White);
                break;

            case ChurchAI.Core.Entities.LowerThirdStyle.GoldBadge:
                LtStyleGoldBadge.Visibility = Visibility.Visible;
                LtGoldTag.Text = lowerThird.TagText;
                LtGoldTitle.Text = lowerThird.Title;
                LtGoldSubtitle.Text = lowerThird.Subtitle;
                LtGoldSubtitle.Visibility = string.IsNullOrWhiteSpace(lowerThird.Subtitle) ? Visibility.Collapsed : Visibility.Visible;
                break;

            case ChurchAI.Core.Entities.LowerThirdStyle.CyberpunkPurple:
                LtStyleCyberpunkPurple.Visibility = Visibility.Visible;
                LtPurpleTitle.Text = lowerThird.Title;
                LtPurpleSubtitle.Text = lowerThird.Subtitle;
                LtPurpleSubBorder.Visibility = string.IsNullOrWhiteSpace(lowerThird.Subtitle) ? Visibility.Collapsed : Visibility.Visible;
                break;

            case ChurchAI.Core.Entities.LowerThirdStyle.ClassicNavy:
                LtStyleClassicNavy.Visibility = Visibility.Visible;
                LtNavyTag.Text = lowerThird.TagText;
                LtNavyTitle.Text = lowerThird.Title;
                LtNavySubtitle.Text = lowerThird.Subtitle;
                LtNavyTag.Visibility = string.IsNullOrWhiteSpace(lowerThird.TagText) ? Visibility.Collapsed : Visibility.Visible;
                LtNavySubtitle.Visibility = string.IsNullOrWhiteSpace(lowerThird.Subtitle) ? Visibility.Collapsed : Visibility.Visible;
                break;
        }

        LowerThirdOverlayHost.Visibility = Visibility.Visible;
        LtScalePanel.Visibility = Visibility.Visible;
    }

    public void UpdateLowerThirdText(string title, string subtitle, string tagText)
    {
        LtBlueTag.Text = tagText;
        LtBlueTitle.Text = title;
        LtBlueSubtitle.Text = subtitle;

        LtGoldTag.Text = tagText;
        LtGoldTitle.Text = title;
        LtGoldSubtitle.Text = subtitle;

        LtPurpleTitle.Text = title;
        LtPurpleSubtitle.Text = subtitle;

        LtNavyTag.Text = tagText;
        LtNavyTitle.Text = title;
        LtNavySubtitle.Text = subtitle;
    }

    public void HideLowerThirdOverlay()
    {
        LowerThirdOverlayHost.Visibility = Visibility.Collapsed;
        CustomLowerThirdHost.Visibility = Visibility.Collapsed;
        LtScalePanel.Visibility = Visibility.Collapsed;
        CommitLtPosBtn.Visibility = Visibility.Collapsed;
        IsPositionPendingCommit = false;
        LtTranslateTransform.X = 0;
        LtTranslateTransform.Y = 0;
        LtScaleTransform.ScaleX = 1.0;
        LtScaleTransform.ScaleY = 1.0;
        if (LtScaleXSlider != null) LtScaleXSlider.Value = 1.0;
        if (LtScaleYSlider != null) LtScaleYSlider.Value = 1.0;
        CommittedX = 0;
        CommittedY = 0;
        CommittedScaleX = 1.0;
        CommittedScaleY = 1.0;
    }

    public void HideCustomLowerThird()
    {
        if (CustomLowerThirdHost != null)
        {
            CustomLowerThirdHost.Visibility = Visibility.Collapsed;
        }
    }

    private static System.Windows.Media.SolidColorBrush ParseColor(string hex, System.Windows.Media.Color fallback)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                var converted = System.Windows.Media.ColorConverter.ConvertFromString(hex);
                if (converted is System.Windows.Media.Color c) return new System.Windows.Media.SolidColorBrush(c);
            }
        }
        catch { }
        return new System.Windows.Media.SolidColorBrush(fallback);
    }

    public void RenderCustomTemplate(ChurchAI.Core.Entities.CustomTemplate template, string title = "", string subtitle = "", string tagText = "", string reference = "", string verseText = "")
    {
        if (template == null || CustomLtCanvas == null) return;

        // Collapse built-in scripture lower third card and main content box so ONLY the custom template is shown!
        if (ScriptureLowerThirdHost != null) ScriptureLowerThirdHost.Visibility = Visibility.Collapsed;
        if (ContentBorder != null) ContentBorder.Visibility = Visibility.Collapsed;
        if (LowerThirdOverlayHost != null) LowerThirdOverlayHost.Visibility = Visibility.Collapsed;
        if (LogoViewbox != null) LogoViewbox.Visibility = Visibility.Collapsed;

        CustomLtCanvas.Children.Clear();

        foreach (var shape in template.Shapes)
        {
            UIElement elem;
            var fillBrush = ParseColor(shape.FillHex, System.Windows.Media.Color.FromRgb(15, 23, 42));
            var strokeBrush = ParseColor(shape.StrokeHex, System.Windows.Media.Colors.Transparent);

            if (shape.ShapeType == ChurchAI.Core.Entities.ShapeType.Ellipse)
            {
                elem = new System.Windows.Shapes.Ellipse
                {
                    Width = Math.Max(10, shape.Width),
                    Height = Math.Max(10, shape.Height),
                    Fill = fillBrush,
                    Stroke = strokeBrush,
                    StrokeThickness = shape.StrokeThickness,
                    Opacity = shape.Opacity
                };
            }
            else
            {
                var border = new System.Windows.Controls.Border
                {
                    Width = Math.Max(10, shape.Width),
                    Height = Math.Max(10, shape.Height),
                    Background = fillBrush,
                    BorderBrush = strokeBrush,
                    BorderThickness = new Thickness(shape.StrokeThickness),
                    CornerRadius = shape.ShapeType == ChurchAI.Core.Entities.ShapeType.PillCapsule ? new CornerRadius(shape.Height / 2) : new CornerRadius(shape.CornerRadius),
                    Opacity = shape.Opacity
                };

                if (Math.Abs(shape.SkewX) > 0.1)
                {
                    border.LayoutTransform = new System.Windows.Media.SkewTransform(shape.SkewX, 0);
                }
                elem = border;
            }

            System.Windows.Controls.Canvas.SetLeft(elem, shape.X);
            System.Windows.Controls.Canvas.SetTop(elem, shape.Y);
            System.Windows.Controls.Panel.SetZIndex(elem, shape.ZIndex);
            CustomLtCanvas.Children.Add(elem);
        }

        foreach (var txt in template.TextPlaceholders)
        {
            string actualText = txt.BindingField switch
            {
                ChurchAI.Core.Entities.TextBindingField.TagText => string.IsNullOrWhiteSpace(tagText) ? "SPEAKER" : tagText,
                ChurchAI.Core.Entities.TextBindingField.Title => string.IsNullOrWhiteSpace(title) ? "Headline Title" : title,
                ChurchAI.Core.Entities.TextBindingField.Subtitle => string.IsNullOrWhiteSpace(subtitle) ? "Subtitle Description" : subtitle,
                ChurchAI.Core.Entities.TextBindingField.Reference => string.IsNullOrWhiteSpace(reference) ? "SCRIPTURE" : reference,
                ChurchAI.Core.Entities.TextBindingField.VerseText => string.IsNullOrWhiteSpace(verseText) ? "Bible Verse Content" : verseText,
                _ => ""
            };

            var tb = new System.Windows.Controls.TextBlock
            {
                Text = actualText,
                Width = Math.Max(10, txt.Width),
                Height = Math.Max(10, txt.Height),
                FontSize = txt.FontSize,
                FontWeight = txt.FontWeight.Equals("Bold", StringComparison.OrdinalIgnoreCase) ? FontWeights.Bold : FontWeights.Normal,
                Foreground = ParseColor(txt.ForegroundHex, System.Windows.Media.Colors.White),
                TextWrapping = TextWrapping.Wrap
            };

            if (txt.TextAlignment.Equals("Center", StringComparison.OrdinalIgnoreCase)) tb.TextAlignment = TextAlignment.Center;
            else if (txt.TextAlignment.Equals("Right", StringComparison.OrdinalIgnoreCase)) tb.TextAlignment = TextAlignment.Right;
            else tb.TextAlignment = TextAlignment.Left;

            if (Math.Abs(txt.SkewX) > 0.1)
            {
                tb.LayoutTransform = new System.Windows.Media.SkewTransform(txt.SkewX, 0);
            }

            System.Windows.Controls.Canvas.SetLeft(tb, txt.X);
            System.Windows.Controls.Canvas.SetTop(tb, txt.Y);
            System.Windows.Controls.Panel.SetZIndex(tb, txt.ZIndex);
            CustomLtCanvas.Children.Add(tb);
        }

        CustomLowerThirdHost.Visibility = Visibility.Visible;
    }

    public System.Windows.Controls.Grid GetMainGrid() => MainGrid;

    public double CommittedX { get; private set; }
    public double CommittedY { get; private set; }
    public double CommittedScaleX { get; private set; } = 1.0;
    public double CommittedScaleY { get; private set; } = 1.0;
    public bool IsPositionPendingCommit { get; private set; }

    private bool _isUpdatingLtScale;

    private void LtScaleXSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingLtScale) return;
        _isUpdatingLtScale = true;

        if (IsScriptureLtActive() && ScriptureLtScaleTransform != null)
        {
            ScriptureLtScaleTransform.ScaleX = e.NewValue;
            if (LtLockAspectCheck?.IsChecked == true && LtScaleYSlider != null)
            {
                LtScaleYSlider.Value = e.NewValue;
                ScriptureLtScaleTransform.ScaleY = e.NewValue;
            }
        }
        else if (LtScaleTransform != null)
        {
            LtScaleTransform.ScaleX = e.NewValue;
            if (LtLockAspectCheck?.IsChecked == true && LtScaleYSlider != null)
            {
                LtScaleYSlider.Value = e.NewValue;
                LtScaleTransform.ScaleY = e.NewValue;
            }
        }

        IsPositionPendingCommit = true;
        ShowCommitPositionButton();
        _isUpdatingLtScale = false;
    }

    private void LtScaleYSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingLtScale) return;
        _isUpdatingLtScale = true;

        if (IsScriptureLtActive() && ScriptureLtScaleTransform != null)
        {
            ScriptureLtScaleTransform.ScaleY = e.NewValue;
            if (LtLockAspectCheck?.IsChecked == true && LtScaleXSlider != null)
            {
                LtScaleXSlider.Value = e.NewValue;
                ScriptureLtScaleTransform.ScaleX = e.NewValue;
            }
        }
        else if (LtScaleTransform != null)
        {
            LtScaleTransform.ScaleY = e.NewValue;
            if (LtLockAspectCheck?.IsChecked == true && LtScaleXSlider != null)
            {
                LtScaleXSlider.Value = e.NewValue;
                LtScaleTransform.ScaleX = e.NewValue;
            }
        }

        IsPositionPendingCommit = true;
        ShowCommitPositionButton();
        _isUpdatingLtScale = false;
    }

    private void LtLockAspectCheck_Click(object sender, RoutedEventArgs e)
    {
        if (LtLockAspectCheck?.IsChecked == true && LtScaleXSlider != null && LtScaleYSlider != null)
        {
            LtScaleYSlider.Value = LtScaleXSlider.Value;
        }
    }

    private void ShowCommitPositionButton()
    {
        if (CommitLtPosBtn != null)
        {
            CommitLtPosBtn.Visibility = Visibility.Visible;
            CommitLtPosBtn.Content = "✓ Set Position";
            CommitLtPosBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));
        }
    }

    private bool _isDraggingLt;
    private bool _activeDragTargetIsScripture;
    private System.Windows.Point _dragStartMousePoint;
    private double _dragStartTransformX;
    private double _dragStartTransformY;

    private void LowerThird_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isDraggingLt = true;
        _dragStartMousePoint = e.GetPosition(MainGrid);

        if (sender == ScriptureLtPositionContainer || (sender == null && IsScriptureLtActive()))
        {
            _activeDragTargetIsScripture = true;
            _dragStartTransformX = ScriptureLtTranslateTransform?.X ?? 0;
            _dragStartTransformY = ScriptureLtTranslateTransform?.Y ?? 0;
            ScriptureLtPositionContainer?.CaptureMouse();
        }
        else
        {
            _activeDragTargetIsScripture = false;
            _dragStartTransformX = LtTranslateTransform?.X ?? 0;
            _dragStartTransformY = LtTranslateTransform?.Y ?? 0;
            LowerThirdPositionContainer?.CaptureMouse();
        }
        e.Handled = true;
    }

    private void LowerThird_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isDraggingLt)
        {
            var currentMousePoint = e.GetPosition(MainGrid);
            double deltaX = currentMousePoint.X - _dragStartMousePoint.X;
            double deltaY = currentMousePoint.Y - _dragStartMousePoint.Y;

            if (_activeDragTargetIsScripture && ScriptureLtTranslateTransform != null)
            {
                ScriptureLtTranslateTransform.X = _dragStartTransformX + deltaX;
                ScriptureLtTranslateTransform.Y = _dragStartTransformY + deltaY;
            }
            else if (LtTranslateTransform != null)
            {
                LtTranslateTransform.X = _dragStartTransformX + deltaX;
                LtTranslateTransform.Y = _dragStartTransformY + deltaY;
            }

            IsPositionPendingCommit = true;
            ShowCommitPositionButton();
            e.Handled = true;
        }
    }

    private void LowerThird_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isDraggingLt)
        {
            _isDraggingLt = false;
            if (ScriptureLtPositionContainer?.IsMouseCaptured == true) ScriptureLtPositionContainer.ReleaseMouseCapture();
            if (LowerThirdPositionContainer?.IsMouseCaptured == true) LowerThirdPositionContainer.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void CommitLtPosBtn_Click(object sender, RoutedEventArgs e)
    {
        bool isScripture = IsScriptureLtActive();
        CommittedX = isScripture ? (ScriptureLtTranslateTransform?.X ?? 0) : (LtTranslateTransform?.X ?? 0);
        CommittedY = isScripture ? (ScriptureLtTranslateTransform?.Y ?? 0) : (LtTranslateTransform?.Y ?? 0);
        CommittedScaleX = isScripture ? (ScriptureLtScaleTransform?.ScaleX ?? 1.0) : (LtScaleTransform?.ScaleX ?? 1.0);
        CommittedScaleY = isScripture ? (ScriptureLtScaleTransform?.ScaleY ?? 1.0) : (LtScaleTransform?.ScaleY ?? 1.0);
        IsPositionPendingCommit = false;

        CommitLtPosBtn.Content = "✓ Position Set!";
        CommitLtPosBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3B82F6"));
        
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        timer.Tick += (s, args) =>
        {
            timer.Stop();
            if (!IsPositionPendingCommit)
            {
                CommitLtPosBtn.Visibility = Visibility.Collapsed;
            }
        };
        timer.Start();
    }
}
