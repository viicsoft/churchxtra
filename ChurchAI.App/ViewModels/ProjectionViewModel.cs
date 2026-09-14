using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Interfaces;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Models;
using ChurchAI.App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChurchAI.App.ViewModels;

public partial class ProjectionViewModel : ViewModelBase, IDisposable
{
    public BibleSearchViewModel? BibleSearch { get; }
    public HymnSearchViewModel? HymnSearch { get; }
    public HistoryViewModel? History { get; }
    public SettingsViewModel? Settings { get; }
    public CommunityViewModel? Community { get; }
    public MediaViewModel? Media { get; }
    public IProjectionService ProjectionService => _projectionService;
    private readonly ILowerThirdRepository? _lowerThirdRepository;

    [ObservableProperty]
    private bool _isLowerThirdModalOpen;

    [ObservableProperty]
    private string _lowerThirdTagText = "SPEAKER";

    [ObservableProperty]
    private string _lowerThirdTitle = "Pastor John Doe";

    [ObservableProperty]
    private string _lowerThirdSubtitle = "Lead Pastor - Service Theme";

    [ObservableProperty]
    private string _lowerThirdTagBgColor = "#0038A8";

    [ObservableProperty]
    private string _lowerThirdTagTextColor = "#FFFFFF";

    [ObservableProperty]
    private string _lowerThirdTitleBgColor = "#F1F5F9";

    [ObservableProperty]
    private string _lowerThirdTitleTextColor = "#0F172A";

    [ObservableProperty]
    private string _lowerThirdSubBgColor = "#0284C7";

    [ObservableProperty]
    private string _lowerThirdSubTextColor = "#FFFFFF";

    partial void OnLowerThirdTagBgColorChanged(string value) => UpdateLiveLowerThirdColors();
    partial void OnLowerThirdTagTextColorChanged(string value) => UpdateLiveLowerThirdColors();
    partial void OnLowerThirdTitleBgColorChanged(string value) => UpdateLiveLowerThirdColors();
    partial void OnLowerThirdTitleTextColorChanged(string value) => UpdateLiveLowerThirdColors();
    partial void OnLowerThirdSubBgColorChanged(string value) => UpdateLiveLowerThirdColors();
    partial void OnLowerThirdSubTextColorChanged(string value) => UpdateLiveLowerThirdColors();

    private void UpdateLiveLowerThirdColors()
    {
        ResetLowerThirdButtonLabels();
        if (_projectionService.ActiveLowerThird != null)
        {
            _projectionService.UpdateActiveLowerThirdColors(
                LowerThirdTagBgColor, LowerThirdTagTextColor,
                LowerThirdTitleBgColor, LowerThirdTitleTextColor,
                LowerThirdSubBgColor, LowerThirdSubTextColor
            );
        }
    }

    [ObservableProperty]
    private ChurchAI.Core.Entities.LowerThirdStyle _selectedLowerThirdStyle = ChurchAI.Core.Entities.LowerThirdStyle.BlueAngular;

    [ObservableProperty]
    private ChurchAI.Core.Entities.LowerThirdPosition _selectedLowerThirdPosition = ChurchAI.Core.Entities.LowerThirdPosition.BottomLeft;

    public ObservableCollection<ChurchAI.Core.Entities.LowerThirdItem> SavedLowerThirds { get; } = new();

    [ObservableProperty]
    private ChurchAI.Core.Entities.LowerThirdItem? _activeLowerThird;

    [ObservableProperty]
    private string _sendLowerThirdButtonText = "🚀 Send / Update Live";

    [RelayCommand]
    private async Task OpenTemplateBuilderAsync(string categoryStr)
    {
        var category = categoryStr.Equals("Scripture", StringComparison.OrdinalIgnoreCase) 
            ? CustomTemplateCategory.Scripture 
            : CustomTemplateCategory.Speaker;

        var customService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ICustomTemplateService>(((App)System.Windows.Application.Current).Host.Services);
        var aiService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IAITemplateExtractorService>(((App)System.Windows.Application.Current).Host.Services);
        var settingsService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ISettingsService>(((App)System.Windows.Application.Current).Host.Services);

        var vm = new CustomTemplateBuilderViewModel(customService, aiService, settingsService)
        {
            Category = category
        };

        var window = new CustomTemplateBuilderWindow { DataContext = vm };
        window.ShowDialog();

        await RefreshCustomTemplatesAsync();
    }

    public ObservableCollection<CustomTemplate> CustomSpeakerTemplates { get; } = new();

    public async Task RefreshCustomTemplatesAsync()
    {
        try
        {
            var customService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ICustomTemplateService>(((App)System.Windows.Application.Current).Host.Services);
            var customScriptures = await customService.GetTemplatesByCategoryAsync(CustomTemplateCategory.Scripture);
            var customSpeakers = await customService.GetTemplatesByCategoryAsync(CustomTemplateCategory.Speaker);

            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                var builtIns = ScriptureTemplates.Where(x => x.CustomTemplate == null).ToList();
                ScriptureTemplates.Clear();
                foreach (var b in builtIns) ScriptureTemplates.Add(b);

                foreach (var ct in customScriptures)
                {
                    ScriptureTemplates.Add(new ScriptureTemplateOption
                    {
                        CustomTemplate = ct,
                        DisplayName = $"📖 [Custom] {ct.Name}"
                    });
                }

                CustomSpeakerTemplates.Clear();
                foreach (var cs in customSpeakers)
                {
                    CustomSpeakerTemplates.Add(cs);
                }
            });
        }
        catch { }
    }

    [ObservableProperty]
    private string _saveLowerThirdButtonText = "💾 Save Preset";

    partial void OnLowerThirdTagTextChanged(string value) => ResetLowerThirdButtonLabels();
    partial void OnLowerThirdTitleChanged(string value) => ResetLowerThirdButtonLabels();
    partial void OnLowerThirdSubtitleChanged(string value) => ResetLowerThirdButtonLabels();
    partial void OnSelectedLowerThirdStyleChanged(ChurchAI.Core.Entities.LowerThirdStyle value) => ResetLowerThirdButtonLabels();
    partial void OnSelectedLowerThirdPositionChanged(ChurchAI.Core.Entities.LowerThirdPosition value) => ResetLowerThirdButtonLabels();

    private void ResetLowerThirdButtonLabels()
    {
        SendLowerThirdButtonText = "🚀 Send / Update Live";
        SaveLowerThirdButtonText = "💾 Save Preset";
    }

    [ObservableProperty]
    private bool _isCommunityMessagePopupOpen;

    [ObservableProperty]
    private string _currentMessageText = string.Empty;

    [ObservableProperty]
    private string _currentMessageHeader = "Community Alert";

    [ObservableProperty]
    private string _currentMessageTagColor = "#10B981";

    [ObservableProperty]
    private string _currentMessageTagLabel = "Project Soon";

    public ObservableCollection<ProjectedItem> ProjectionQueue => _projectionService.ProjectionQueue;

    [ObservableProperty]
    private ProjectedItem? _selectedQueueItem;

    [ObservableProperty]
    private string _quickProjectText = string.Empty;

    [ObservableProperty]
    private bool _isLowerThirds;

    [ObservableProperty]
    private ProjectedItem? _currentlyProjectedItem;

    // AI & Microphone State
    [ObservableProperty]
    private string _liveTranscript = "Welcome to ChurchXtra AI Control Room. Click the microphone to start listening...";

    [ObservableProperty]
    private string _speechStatus = "Microphone Idle";

    [ObservableProperty]
    private bool _isListening;

    [ObservableProperty]
    private double _currentAudioLevel;

    [ObservableProperty]
    private bool _isLiveLineEditMode;

    [ObservableProperty]
    private bool _isFullViewMode;

    [ObservableProperty]
    private bool _isFullViewControlsExpanded;

    [ObservableProperty]
    private bool _isLeftCardExpanded = false;

    [ObservableProperty]
    private bool _isCenterCardExpanded = false;

    [ObservableProperty]
    private bool _isRightCardExpanded = false;

    [ObservableProperty]
    private int _navHostColumn = 0;

    [RelayCommand]
    private void ToggleLeftCardExpand()
    {
        IsLeftCardExpanded = !IsLeftCardExpanded;
        NavHostColumn = IsLeftCardExpanded ? 2 : 0;
    }

    [RelayCommand]
    private void ToggleCenterCardExpand()
    {
        IsCenterCardExpanded = !IsCenterCardExpanded;
    }

    [RelayCommand]
    private void ToggleRightCardExpand()
    {
        IsRightCardExpanded = !IsRightCardExpanded;
    }

    [ObservableProperty]
    private ChurchAI.Core.Entities.ScriptureLowerThirdTemplate _selectedScriptureTemplate = ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.PillCapsuleSand;

    [ObservableProperty]
    private int _selectedLowerThirdSubTabIndex = 0;

    [RelayCommand]
    private void SelectScriptureOption(ScriptureTemplateOption? option)
    {
        if (option == null) return;
        SelectedScriptureOption = option;
        SendLowerThirdButtonText = "🟢 LIVE ON SCREEN";
    }

    [ObservableProperty]
    private ScriptureTemplateOption? _selectedScriptureOption;

    partial void OnSelectedScriptureOptionChanged(ScriptureTemplateOption? value)
    {
        if (value == null) return;
        if (value.CustomTemplate != null)
        {
            _projectionService.SetActiveCustomTemplate(value.CustomTemplate);
        }
        else
        {
            _projectionService.SetScriptureLowerThirdTemplate(value.Template);
        }
    }

    partial void OnSelectedScriptureTemplateChanged(ChurchAI.Core.Entities.ScriptureLowerThirdTemplate value)
    {
        _projectionService.SetScriptureLowerThirdTemplate(value);
    }

    public ObservableCollection<ScriptureTemplateOption> ScriptureTemplates { get; } = new()
    {
        new ScriptureTemplateOption { Template = ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.PillCapsuleSand, DisplayName = "💊 Pill Capsule Sand" },
        new ScriptureTemplateOption { Template = ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.DualStackedCoralOffWhite, DisplayName = "🥞 Dual Stacked Coral" },
        new ScriptureTemplateOption { Template = ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.DualMintGlassmorphism, DisplayName = "🌿 Dual Mint Glass" },
        new ScriptureTemplateOption { Template = ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.PastelSageRosePill, DisplayName = "🌸 Pastel Sage & Rose" },
        new ScriptureTemplateOption { Template = ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.CrimsonVousBroadcast, DisplayName = "🔴 Crimson Broadcast" }
    };

    public ObservableCollection<LiveLineItem> LiveLineItems { get; } = new();

    private string _committedTranscript = string.Empty;
    private string _lastSearchedQuoteText = string.Empty;
    private CancellationTokenSource? _quoteCts;

    // Popup Modal State
    [ObservableProperty]
    private string _editableReference = string.Empty;

    [ObservableProperty]
    private string _selectedTranslationAbbreviation = "KJV";

    [ObservableProperty]
    private string _previewHeader = "Preview - No Verse Selected";

    [ObservableProperty]
    private string _previewText = "Select a scripture to preview it here.";

    [ObservableProperty]
    private ChurchAI.Core.Entities.Verse? _activeDetectedVerse;



    [ObservableProperty]
    private string _announcementTitle = "Announcement";

    [ObservableProperty]
    private string _announcementText = string.Empty;

    [ObservableProperty]
    private bool _projectRealTime = false;

    [ObservableProperty]
    private string _customLyricsTitle = string.Empty;

    [ObservableProperty]
    private string _customLyricsText = string.Empty;

    [ObservableProperty]
    private bool _isCustomHymnPopupOpen;

    public ObservableCollection<ChurchAI.Core.Models.ProjectedItem> SavedAnnouncements { get; } = new();

    [ObservableProperty]
    private ChurchAI.Core.Models.ProjectedItem? _selectedSavedAnnouncement;

    partial void OnSelectedSavedAnnouncementChanged(ChurchAI.Core.Models.ProjectedItem? value)
    {
        if (value != null)
        {
            AnnouncementTitle = value.Reference;
            AnnouncementText = value.Text;
            PreviewAnnouncement();
        }
    }

    partial void OnAnnouncementTitleChanged(string value)
    {
        if (ProjectRealTime)
        {
            ProjectAnnouncementRealTime();
        }
    }

    partial void OnAnnouncementTextChanged(string value)
    {
        if (ProjectRealTime)
        {
            ProjectAnnouncementRealTime();
        }
    }

    private void ProjectAnnouncementRealTime()
    {
        PreviewHeader = AnnouncementTitle;
        PreviewText = AnnouncementText;
        ActiveDetectedVerse = null;
        _projectionService.ProjectVerse(AnnouncementTitle, AnnouncementText, showWindow: false);
    }

    [ObservableProperty]
    private ObservableCollection<ChurchAI.Core.Entities.Verse> _allTranslationsForPopup = new();

    [ObservableProperty]
    private ObservableCollection<ChurchAI.Core.Entities.Verse> _succeedingVerses = new();

    [ObservableProperty]
    private string _succeedingSearchReference = string.Empty;

    public ObservableCollection<ChurchAI.Core.Models.ProjectedItem> DetectedSpeechScriptures { get; } = new();
    public ObservableCollection<ChurchAI.Core.Models.ProjectedItem> DetectedSpeechHymns { get; } = new();
    public ObservableCollection<ChurchAI.Core.Models.ProjectedItem> DetailedContentItems { get; } = new();

    [ObservableProperty]
    private ChurchAI.Core.Models.ProjectedItem? _selectedDetailedItem;

    [ObservableProperty]
    private bool _isScriptureListeningEnabled = true;

    [ObservableProperty]
    private bool _isHymnListeningEnabled = true;

    [ObservableProperty]
    private ChurchAI.Core.Models.ProjectedItem? _selectedSpeechScripture;

    partial void OnSelectedSpeechScriptureChanged(ChurchAI.Core.Models.ProjectedItem? value)
    {
        if (value != null)
        {
            PreviewHeader = value.Reference;
            PreviewText = value.Text;
            ActiveDetectedVerse = null;
            SelectedSpeechHymn = null; // Prevent double highlights
        }
    }

    [ObservableProperty]
    private ChurchAI.Core.Models.ProjectedItem? _selectedSpeechHymn;

    partial void OnSelectedSpeechHymnChanged(ChurchAI.Core.Models.ProjectedItem? value)
    {
        if (value != null)
        {
            PreviewHeader = value.Reference;
            PreviewText = value.Text;
            ActiveDetectedVerse = null;
            SelectedSpeechScripture = null; // Prevent double highlights
        }
    }

    [RelayCommand]
    private async Task SelectSucceedingVerseAsync(ChurchAI.Core.Entities.Verse verse)
    {
        if (verse == null) return;

        var succeeding = await _bibleRepository.GetSucceedingVersesAsync(verse.BookId, verse.Chapter, verse.VerseNumber, 50);
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            SucceedingVerses.Clear();
            DetailedContentItems.Clear();

            DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
            {
                Reference = verse.DisplayReference,
                Text = verse.Text
            });

            if (succeeding != null)
            {
                foreach (var sv in succeeding)
                {
                    SucceedingVerses.Add(sv);
                    DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                    {
                        Reference = sv.DisplayReference,
                        Text = sv.Text
                    });
                }
            }
            ActiveDetectedVerse = verse;
        });
    }

    private readonly IProjectionService _projectionService;
    private readonly IBibleRepository _bibleRepository;
    private readonly ICommunityMessageService? _communityMessageService;
    private readonly IBibleReferenceParser _referenceParser;
    private readonly ISpeechRecognitionService _speechRecognitionService;
    private readonly IAIIntentService _aiIntentService;
    private readonly IHymnRepository _hymnRepository;
    private readonly ISettingsService? _settingsService;

    private readonly System.Collections.Generic.HashSet<string> _spottedInCurrentUtterance = new(StringComparer.OrdinalIgnoreCase);

    private static readonly System.Collections.Generic.Dictionary<string, string> LocalCommonTopics = new(StringComparer.OrdinalIgnoreCase)
    {
        { "john 3 16", "john 3 16" },
        { "for god so loved the world", "john 3 16" },
        { "the lord is my shepherd", "psalm 23 1" },
        { "the lord is my shepherd i shall not want", "psalm 23 1" },
        { "yea though i walk through the valley", "psalm 23 4" },
        { "valley of the shadow of death", "psalm 23 4" },
        { "i can do all things through christ", "philippians 4 13" },
        { "i can do all things", "philippians 4 13" },
        { "in the beginning god created", "genesis 1 1" },
        { "in the beginning was the word", "john 1 1" },
        { "the word became flesh", "john 1 14" },
        { "trust in the lord with all your heart", "proverbs 3 5" },
        { "lean not on your own understanding", "proverbs 3 5" },
        { "be still and know that i am god", "psalm 46 10" },
        { "fruit of the spirit", "galatians 5 22" },
        { "fruits of the spirit", "galatians 5 22" },
        { "armor of god", "ephesians 6 11" },
        { "full armor of god", "ephesians 6 11" },
        { "put on the whole armor of god", "ephesians 6 11" },
        { "love is patient love is kind", "1 corinthians 13 4" },
        { "love is patient", "1 corinthians 13 4" },
        { "faith hope and love", "1 corinthians 13 13" },
        { "greatest of these is love", "1 corinthians 13 13" },
        { "great commission", "matthew 28 19" },
        { "go into all the world", "mark 16 15" },
        { "make disciples of all nations", "matthew 28 19" },
        { "all have sinned and come short", "romans 3 23" },
        { "for all have sinned", "romans 3 23" },
        { "wages of sin is death", "romans 6 23" },
        { "gift of god is eternal life", "romans 6 23" },
        { "if god is for us who can be against us", "romans 8 31" },
        { "more than conquerors", "romans 8 37" },
        { "nothing can separate us from the love of god", "romans 8 38" },
        { "seek first the kingdom of god", "matthew 6 33" },
        { "seek ye first", "matthew 6 33" },
        { "i know the plans i have for you", "jeremiah 29 11" },
        { "plans to prosper you", "jeremiah 29 11" },
        { "plans for peace and not of evil", "jeremiah 29 11" },
        { "they that wait upon the lord", "isaiah 40 31" },
        { "mount up with wings as eagles", "isaiah 40 31" },
        { "fear not for i am with you", "isaiah 41 10" },
        { "be strong and courageous", "joshua 1 9" },
        { "do not be afraid nor dismayed", "joshua 1 9" },
        { "as for me and my house", "joshua 24 15" },
        { "we will serve the lord", "joshua 24 15" },
        { "prodigal son", "luke 15 11" },
        { "parable of the prodigal son", "luke 15 11" },
        { "good samaritan", "luke 10 25" },
        { "parable of the good samaritan", "luke 10 25" },
        { "sower and the seed", "matthew 13 3" },
        { "parable of the sower", "matthew 13 3" },
        { "mustard seed", "matthew 13 31" },
        { "faith as a mustard seed", "matthew 17 20" },
        { "lost sheep", "luke 15 3" },
        { "ninety and nine", "luke 15 4" },
        { "lost coin", "luke 15 8" },
        { "talents", "matthew 25 14" },
        { "parable of the talents", "matthew 25 14" },
        { "ten virgins", "matthew 25 1" },
        { "wise and foolish virgins", "matthew 25 1" },
        { "rich man and lazarus", "luke 16 19" },
        { "pharisee and the tax collector", "luke 18 9" },
        { "pharisee and publican", "luke 18 9" },
        { "barren fig tree", "luke 13 6" },
        { "unforgiving servant", "matthew 18 21" },
        { "wheat and tares", "matthew 13 24" },
        { "pearl of great price", "matthew 13 45" },
        { "water into wine", "john 2 1" },
        { "wedding at cana", "john 2 1" },
        { "jesus feeds 5000", "matthew 14 15" },
        { "jesus feeds five thousand", "matthew 14 15" },
        { "feeding of the five thousand", "matthew 14 15" },
        { "feeding the 4000", "matthew 15 32" },
        { "jesus walks on water", "matthew 14 25" },
        { "walking on the sea", "matthew 14 25" },
        { "calming the storm", "mark 4 35" },
        { "peace be still", "mark 4 39" },
        { "raising of lazarus", "john 11 43" },
        { "lazarus come forth", "john 11 43" },
        { "healing of blind bartimaeus", "mark 10 46" },
        { "blind bartimaeus", "mark 10 46" },
        { "woman with the issue of blood", "mark 5 25" },
        { "hem of his garment", "matthew 9 20" },
        { "healing the paralytic", "mark 2 1" },
        { "cleansing of ten lepers", "luke 17 11" },
        { "ten lepers", "luke 17 11" },
        { "healing the centurions servant", "matthew 8 5" },
        { "jairus daughter", "mark 5 22" },
        { "miraculous catch of fish", "luke 5 4" },
        { "creation of the world", "genesis 1 1" },
        { "adam and eve", "genesis 2 7" },
        { "fall of man", "genesis 3 1" },
        { "cain and abel", "genesis 4 1" },
        { "cain killed abel", "genesis 4 8" },
        { "cain kills abel", "genesis 4 8" },
        { "noahs ark", "genesis 6 14" },
        { "great flood", "genesis 7 11" },
        { "tower of babel", "genesis 11 1" },
        { "call of abraham", "genesis 12 1" },
        { "binding of isaac", "genesis 22 1" },
        { "jacobs ladder", "genesis 28 10" },
        { "jacob wrestles with god", "genesis 32 24" },
        { "joseph and his coat of many colors", "genesis 37 3" },
        { "joseph coat of many colors", "genesis 37 3" },
        { "burning bush", "exodus 3 2" },
        { "ten plagues of egypt", "exodus 7 14" },
        { "parting of the red sea", "exodus 14 21" },
        { "red sea crossing", "exodus 14 21" },
        { "ten commandments", "exodus 20 2" },
        { "the ten commandments", "exodus 20 2" },
        { "golden calf", "exodus 32 1" },
        { "walls of jericho", "joshua 6 1" },
        { "fall of jericho", "joshua 6 20" },
        { "sun standing still", "joshua 10 12" },
        { "samson and delilah", "judges 16 4" },
        { "ruth and naomi", "ruth 1 16" },
        { "where you go i will go", "ruth 1 16" },
        { "calling of samuel", "1 samuel 3 1" },
        { "speak lord for your servant hears", "1 samuel 3 10" },
        { "samuel anoints david", "1 samuel 16 13" },
        { "david and goliath", "1 samuel 17 4" },
        { "david kills goliath", "1 samuel 17 50" },
        { "david and jonathan", "1 samuel 18 1" },
        { "elijah and the prophets of baal", "1 kings 18 20" },
        { "fire from heaven", "1 kings 18 38" },
        { "chariot of fire", "2 kings 2 11" },
        { "naaman healed of leprosy", "2 kings 5 1" },
        { "shadrach meshach and abednego", "daniel 3 12" },
        { "fiery furnace", "daniel 3 19" },
        { "handwriting on the wall", "daniel 5 5" },
        { "daniel in the lions den", "daniel 6 16" },
        { "jonah and the whale", "jonah 1 17" },
        { "valley of dry bones", "ezekiel 37 1" },
        { "dry bones live again", "ezekiel 37 3" },
        { "birth of jesus", "luke 2 1" },
        { "nativity of jesus", "luke 2 1" },
        { "wise men visit jesus", "matthew 2 1" },
        { "star of bethlehem", "matthew 2 2" },
        { "baptism of jesus", "matthew 3 13" },
        { "temptation of jesus", "matthew 4 1" },
        { "sermon on the mount", "matthew 5 1" },
        { "the beatitudes", "matthew 5 3" },
        { "salt and light", "matthew 5 13" },
        { "salt of the earth", "matthew 5 13" },
        { "light of the world", "matthew 5 14" },
        { "lords prayer", "matthew 6 9" },
        { "the lords prayer", "matthew 6 9" },
        { "golden rule", "matthew 7 12" },
        { "do unto others", "matthew 7 12" },
        { "transfiguration of jesus", "matthew 17 1" },
        { "triumphal entry", "matthew 21 1" },
        { "cleansing of the temple", "matthew 21 12" },
        { "last supper", "matthew 26 26" },
        { "gethsemane", "matthew 26 36" },
        { "agony in the garden", "matthew 26 36" },
        { "peter denies jesus", "matthew 26 69" },
        { "crucifixion of jesus", "matthew 27 32" },
        { "jesus wept", "john 11 35" },
        { "resurrection of jesus", "luke 24 1" },
        { "empty tomb", "luke 24 2" },
        { "road to emmaus", "luke 24 13" },
        { "doubting thomas", "john 20 24" },
        { "ascension of jesus", "acts 1 9" },
        { "day of pentecost", "acts 2 1" },
        { "holy spirit descends", "acts 2 1" },
        { "conversion of paul", "acts 9 1" },
        { "road to damascus", "acts 9 3" },
        { "paul and silas in prison", "acts 16 25" },
        { "shipwreck of paul", "acts 27 13" },
        { "new heaven and new earth", "revelation 21 1" },
        { "alpha and omega", "revelation 22 13" },
        { "in the beginning god created the heavens and the earth", "genesis 1 1" },
        { "let there be light", "genesis 1 3" },
        { "and god saw that it was good", "genesis 1 4" },
        { "god created man in his own image", "genesis 1 27" },
        { "image of god", "genesis 1 27" },
        { "be fruitful and multiply", "genesis 1 28" },
        { "breath of life", "genesis 2 7" },
        { "tree of life", "genesis 2 9" },
        { "tree of knowledge of good and evil", "genesis 2 9" },
        { "not good that man should be alone", "genesis 2 18" },
        { "bone of my bones and flesh of my flesh", "genesis 2 23" },
        { "leave his father and mother and cleave to his wife", "genesis 2 24" },
        { "serpent deceived me", "genesis 3 13" },
        { "dust you are and to dust you shall return", "genesis 3 19" },
        { "am i my brothers keeper", "genesis 4 9" },
        { "blood of your brother cries out to me", "genesis 4 10" },
        { "walked with god and he was not", "genesis 5 24" },
        { "rainbow in the clouds", "genesis 9 13" },
        { "rainbow covenant", "genesis 9 13" },
        { "father of many nations", "genesis 17 5" },
        { "is anything too hard for the lord", "genesis 18 14" },
        { "shall not the judge of all the earth do right", "genesis 18 25" },
        { "sodom and gomorrah", "genesis 19 24" },
        { "lots wife turned into a pillar of salt", "genesis 19 26" },
        { "pillar of salt", "genesis 19 26" },
        { "the lord will provide", "genesis 22 14" },
        { "jehovah jireh", "genesis 22 14" },
        { "birthright for a bowl of stew", "genesis 25 34" },
        { "esau sold his birthright", "genesis 25 34" },
        { "voice of jacob but the hands of esau", "genesis 27 22" },
        { "jacob wrestled with the angel", "genesis 32 24" },
        { "your name shall no longer be called jacob but israel", "genesis 32 28" },
        { "you intended to harm me but god intended it for good", "genesis 50 20" },
        { "draw off your sandals for the place where you stand is holy ground", "exodus 3 5" },
        { "i am who i am", "exodus 3 14" },
        { "i am that i am", "exodus 3 14" },
        { "land flowing with milk and honey", "exodus 3 8" },
        { "let my people go", "exodus 5 1" },
        { "blood on the doorposts", "exodus 12 7" },
        { "passover lamb", "exodus 12 21" },
        { "pillar of cloud by day and pillar of fire by night", "exodus 13 21" },
        { "the lord will fight for you and you shall hold your peace", "exodus 14 14" },
        { "the lord is my strength and my song", "exodus 15 2" },
        { "manna from heaven", "exodus 16 14" },
        { "bread from heaven", "exodus 16 4" },
        { "water out of the rock", "exodus 17 6" },
        { "kingdom of priests and a holy nation", "exodus 19 6" },
        { "you shall have no other gods before me", "exodus 20 3" },
        { "honor your father and your mother", "exodus 20 12" },
        { "you shall not murder", "exodus 20 13" },
        { "you shall not steal", "exodus 20 15" },
        { "you shall not covet", "exodus 20 17" },
        { "eye for an eye and tooth for a tooth", "exodus 21 24" },
        { "ark of the covenant", "exodus 25 10" },
        { "mercy seat", "exodus 25 17" },
        { "the lord is compassionate and gracious slow to anger", "exodus 34 6" },
        { "abounding in love and faithfulness", "exodus 34 6" },
        { "love your neighbor as yourself", "leviticus 19 18" },
        { "the lord bless you and keep you", "numbers 6 24" },
        { "the lord make his face shine upon you", "numbers 6 25" },
        { "and give you peace", "numbers 6 26" },
        { "priestly blessing", "numbers 6 24" },
        { "bronze serpent", "numbers 21 8" },
        { "balaams donkey", "numbers 22 28" },
        { "god is not a man that he should lie", "numbers 23 19" },
        { "hear o israel the lord our god the lord is one", "deuteronomy 6 4" },
        { "the shema", "deuteronomy 6 4" },
        { "love the lord your god with all your heart and with all your soul", "deuteronomy 6 5" },
        { "man does not live on bread alone", "deuteronomy 8 3" },
        { "the lord your god goes with you he will never leave you nor forsake you", "deuteronomy 31 6" },
        { "meditate on it day and night", "joshua 1 8" },
        { "then you will be prosperous and successful", "joshua 1 8" },
        { "rahab and the spies", "joshua 2 1" },
        { "scarlet cord", "joshua 2 18" },
        { "choose this day whom you will serve", "joshua 24 15" },
        { "gideon and the fleece", "judges 6 36" },
        { "gideons three hundred", "judges 7 7" },
        { "sword of the lord and of gideon", "judges 7 20" },
        { "samson tearing the lion", "judges 14 6" },
        { "out of the eater came something to eat", "judges 14 14" },
        { "samson bringing down the pillars", "judges 16 29" },
        { "entreat me not to leave you", "ruth 1 16" },
        { "your people shall be my people and your god my god", "ruth 1 16" },
        { "ebenezer stone", "1 samuel 7 12" },
        { "thus far the lord has helped us", "1 samuel 7 12" },
        { "to obey is better than sacrifice", "1 samuel 15 22" },
        { "man looks on the outward appearance but the lord looks on the heart", "1 samuel 16 7" },
        { "the battle is the lords", "1 samuel 17 47" },
        { "smooth stones from the brook", "1 samuel 17 40" },
        { "how are the mighty fallen", "2 samuel 1 19" },
        { "you are the man", "2 samuel 12 7" },
        { "give your servant a discerning heart", "1 kings 3 9" },
        { "wisdom of solomon", "1 kings 3 16" },
        { "queen of sheba visited solomon", "1 kings 10 1" },
        { "how long will you waver between two opinions", "1 kings 18 21" },
        { "still small voice", "1 kings 19 12" },
        { "gentle whisper", "1 kings 19 12" },
        { "seven thousand in israel who have not bowed to baal", "1 kings 19 18" },
        { "double portion of your spirit", "2 kings 2 9" },
        { "chariot of fire and horses of fire", "2 kings 2 11" },
        { "widows jar of oil", "2 kings 4 1" },
        { "is it well with the child it is well", "2 kings 4 26" },
        { "open his eyes lord so that he may see", "2 kings 6 17" },
        { "those who are with us are more than those who are with them", "2 kings 6 16" },
        { "wash seven times in the jordan", "2 kings 5 10" },
        { "naked i came from my mothers womb and naked i will depart", "job 1 21" },
        { "the lord gave and the lord has taken away", "job 1 21" },
        { "blessed be the name of the lord", "job 1 21" },
        { "shall we accept good from god and not trouble", "job 2 10" },
        { "i know that my redeemer lives", "job 19 25" },
        { "where were you when i laid the foundations of the earth", "job 38 4" },
        { "i had heard of you by the hearing of the ear but now my eye sees you", "job 42 5" },
        { "blessed is the man who walks not in the counsel of the ungodly", "psalm 1 1" },
        { "he shall be like a tree planted by rivers of water", "psalm 1 3" },
        { "out of the mouth of babes and infants", "psalm 8 2" },
        { "what is man that you are mindful of him", "psalm 8 4" },
        { "crowned him with glory and honor", "psalm 8 5" },
        { "the fool says in his heart there is no god", "psalm 14 1" },
        { "the heavens declare the glory of god", "psalm 19 1" },
        { "the law of the lord is perfect converting the soul", "psalm 19 7" },
        { "let the words of my mouth and the meditation of my heart be acceptable", "psalm 19 14" },
        { "my god my god why have you forsaken me", "psalm 22 1" },
        { "they pierced my hands and my feet", "psalm 22 16" },
        { "he makes me lie down in green pastures", "psalm 23 2" },
        { "he leads me beside still waters", "psalm 23 2" },
        { "he restores my soul", "psalm 23 3" },
        { "my cup overflows", "psalm 23 5" },
        { "goodness and mercy shall follow me all the days of my life", "psalm 23 6" },
        { "i will dwell in the house of the lord forever", "psalm 23 6" },
        { "the earth is the lords and everything in it", "psalm 24 1" },
        { "who may ascend the hill of the lord", "psalm 24 3" },
        { "he who has clean hands and a pure heart", "psalm 24 4" },
        { "lift up your heads o ye gates", "psalm 24 7" },
        { "king of glory shall come in", "psalm 24 7" },
        { "the lord is my light and my salvation whom shall i fear", "psalm 27 1" },
        { "the lord is the strength of my life", "psalm 27 1" },
        { "one thing i ask of the lord this only do i seek", "psalm 27 4" },
        { "to dwell in the house of the lord all the days of my life", "psalm 27 4" },
        { "weeping may endure for a night but joy comes in the morning", "psalm 30 5" },
        { "into your hands i commit my spirit", "psalm 31 5" },
        { "taste and see that the lord is good", "psalm 34 8" },
        { "blessed is the one who takes refuge in him", "psalm 34 8" },
        { "the lord is close to the brokenhearted", "psalm 34 18" },
        { "many are the afflictions of the righteous but the lord delivers him out of them all", "psalm 34 19" },
        { "delight yourself in the lord and he will give you the desires of your heart", "psalm 37 4" },
        { "commit your way to the lord trust also in him", "psalm 37 5" },
        { "i have been young and now am old yet i have never seen the righteous forsaken", "psalm 37 25" },
        { "as the deer pants for streams of water so my soul pants for you o god", "psalm 42 1" },
        { "deep calls to deep", "psalm 42 7" },
        { "god is our refuge and strength an ever present help in trouble", "psalm 46 1" },
        { "there is a river whose streams make glad the city of god", "psalm 46 4" },
        { "god is within her she will not fall", "psalm 46 5" },
        { "have mercy on me o god according to your unfailing love", "psalm 51 1" },
        { "wash me and i shall be whiter than snow", "psalm 51 7" },
        { "create in me a pure heart o god", "psalm 51 10" },
        { "renew a steadfast spirit within me", "psalm 51 10" },
        { "do not cast me away from your presence", "psalm 51 11" },
        { "do not take your holy spirit from me", "psalm 51 11" },
        { "restore to me the joy of your salvation", "psalm 51 12" },
        { "sacrifices of god are a broken spirit", "psalm 51 17" },
        { "broken and contrite heart", "psalm 51 17" },
        { "cast your burden on the lord and he shall sustain you", "psalm 55 22" },
        { "when i am afraid i put my trust in you", "psalm 56 3" },
        { "be exalted o god above the heavens", "psalm 57 5" },
        { "truly my soul finds rest in god", "psalm 62 1" },
        { "he alone is my rock and my salvation", "psalm 62 2" },
        { "your steadfast love is better than life", "psalm 63 3" },
        { "let god arise let his enemies be scattered", "psalm 68 1" },
        { "father to the fatherless defender of widows", "psalm 68 5" },
        { "whom have i in heaven but you", "psalm 73 25" },
        { "my flesh and my heart may fail but god is the strength of my heart", "psalm 73 26" },
        { "satisfy us in the morning with your unfailing love", "psalm 90 14" },
        { "teach us to number our days that we may gain a heart of wisdom", "psalm 90 12" },
        { "he who dwells in the shelter of the most high", "psalm 91 1" },
        { "abide under the shadow of the almighty", "psalm 91 1" },
        { "my refuge and my fortress my god in whom i trust", "psalm 91 2" },
        { "he will cover you with his feathers", "psalm 91 4" },
        { "under his wings you will find refuge", "psalm 91 4" },
        { "a thousand may fall at your side ten thousand at your right hand", "psalm 91 7" },
        { "he will command his angels concerning you to guard you in all your ways", "psalm 91 11" },
        { "they will lift you up in their hands so that you will not strike your foot against a stone", "psalm 91 12" },
        { "righteous will flourish like a palm tree", "psalm 92 12" },
        { "make a joyful noise unto the lord all ye lands", "psalm 100 1" },
        { "serve the lord with gladness come before his presence with singing", "psalm 100 2" },
        { "know that the lord he is god it is he who made us and not we ourselves", "psalm 100 3" },
        { "we are his people and the sheep of his pasture", "psalm 100 3" },
        { "enter into his gates with thanksgiving and into his courts with praise", "psalm 100 4" },
        { "for the lord is good his mercy is everlasting", "psalm 100 5" },
        { "bless the lord o my soul and all that is within me bless his holy name", "psalm 103 1" },
        { "who forgives all your sins and heals all your diseases", "psalm 103 3" },
        { "who redeems your life from the pit", "psalm 103 4" },
        { "crowns you with love and compassion", "psalm 103 4" },
        { "as far as the east is from the west so far has he removed our transgressions", "psalm 103 12" },
        { "as a father has compassion on his children so the lord has compassion on those who fear him", "psalm 103 13" },
        { "the lord said to my lord sit at my right hand", "psalm 110 1" },
        { "the fear of the lord is the beginning of wisdom", "psalm 111 10" },
        { "from the rising of the sun to the place where it sets", "psalm 113 3" },
        { "name of the lord is to be praised", "psalm 113 3" },
        { "not unto us o lord not unto us but to your name give glory", "psalm 115 1" },
        { "precious in the sight of the lord is the death of his saints", "psalm 116 15" },
        { "praise the lord all you nations", "psalm 117 1" },
        { "his mercy endures forever", "psalm 118 1" },
        { "the stone the builders rejected has become the cornerstone", "psalm 118 22" },
        { "this is the day the lord has made we will rejoice and be glad in it", "psalm 118 24" },
        { "blessed is he who comes in the name of the lord", "psalm 118 26" },
        { "how can a young man keep his way pure by living according to your word", "psalm 119 9" },
        { "i have hidden your word in my heart that i might not sin against you", "psalm 119 11" },
        { "open my eyes that i may see wonderful things in your law", "psalm 119 18" },
        { "your word is a lamp to my feet and a light to my path", "psalm 119 105" },
        { "the unfolding of your words gives light", "psalm 119 130" },
        { "great peace have those who love your law", "psalm 119 165" },
        { "i lift up my eyes to the hills where does my help come from", "psalm 121 1" },
        { "my help comes from the lord the maker of heaven and earth", "psalm 121 2" },
        { "he will not let your foot slip he who watches over you will not slumber", "psalm 121 3" },
        { "he who watches over israel will neither slumber nor sleep", "psalm 121 4" },
        { "the lord is your keeper the lord is your shade upon your right hand", "psalm 121 5" },
        { "sun shall not harm you by day nor the moon by night", "psalm 121 6" },
        { "the lord will keep you from all harm he will watch over your life", "psalm 121 7" },
        { "i was glad when they said to me let us go into the house of the lord", "psalm 122 1" },
        { "pray for the peace of jerusalem", "psalm 122 6" },
        { "those who trust in the lord are like mount zion which cannot be shaken", "psalm 125 1" },
        { "when the lord brought back the captives of zion we were like men who dreamed", "psalm 126 1" },
        { "those who sow in tears shall reap with songs of joy", "psalm 126 5" },
        { "unless the lord builds the house the laborers build in vain", "psalm 127 1" },
        { "unless the lord watches over the city the guards stand watch in vain", "psalm 127 1" },
        { "he gives sleep to those he loves", "psalm 127 2" },
        { "children are a heritage from the lord", "psalm 127 3" },
        { "out of the depths i cry to you o lord", "psalm 130 1" },
        { "if you o lord kept a record of sins who could stand", "psalm 130 3" },
        { "with you there is forgiveness", "psalm 130 4" },
        { "how good and pleasant it is when brothers dwell together in unity", "psalm 133 1" },
        { "like precious oil poured on the head", "psalm 133 2" },
        { "by the rivers of babylon we sat and wept", "psalm 137 1" },
        { "how can we sing the songs of the lord in a foreign land", "psalm 137 4" },
        { "o lord you have searched me and known me", "psalm 139 1" },
        { "where can i go from your spirit where can i flee from your presence", "psalm 139 7" },
        { "for you created my inmost being you knit me together in my mothers womb", "psalm 139 13" },
        { "i praise you because i am fearfully and wonderfully made", "psalm 139 14" },
        { "search me god and know my heart test me and know my anxious thoughts", "psalm 139 23" },
        { "see if there is any offensive way in me and lead me in the way everlasting", "psalm 139 24" },
        { "set a guard over my mouth o lord keep watch over the door of my lips", "psalm 141 3" },
        { "let my prayer be set before you as incense", "psalm 141 2" },
        { "the lord is gracious and compassionate slow to anger and rich in love", "psalm 145 8" },
        { "the lord is good to all his compassion is over all he has made", "psalm 145 9" },
        { "the lord is near to all who call on him to all who call on him in truth", "psalm 145 18" },
        { "he heals the brokenhearted and binds up their wounds", "psalm 147 3" },
        { "he determines the number of the stars and calls them each by name", "psalm 147 4" },
        { "let everything that has breath praise the lord", "psalm 150 6" }
    };

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _spokenPhraseIndex = new(StringComparer.OrdinalIgnoreCase);

    public ProjectionViewModel(
        IProjectionService projectionService, 
        IBibleRepository bibleRepository, 
        IBibleReferenceParser referenceParser, 
        ISpeechRecognitionService speechRecognitionService, 
        IAIIntentService aiIntentService,
        IHymnRepository hymnRepository,
        ISettingsService? settingsService = null,
        BibleSearchViewModel? bibleSearch = null,
        HymnSearchViewModel? hymnSearch = null,
        HistoryViewModel? history = null,
        SettingsViewModel? settings = null,
        ICommunityMessageService? communityMessageService = null,
        CommunityViewModel? community = null,
        MediaViewModel? media = null,
        ILowerThirdRepository? lowerThirdRepository = null)
    {
        _projectionService = projectionService;
        _bibleRepository = bibleRepository;
        _communityMessageService = communityMessageService;
        _referenceParser = referenceParser;
        _speechRecognitionService = speechRecognitionService;
        _aiIntentService = aiIntentService;
        _hymnRepository = hymnRepository;
        _settingsService = settingsService;
        _lowerThirdRepository = lowerThirdRepository;
        BibleSearch = bibleSearch;
        HymnSearch = hymnSearch;
        History = history;
        Settings = settings;
        Community = community;
        Media = media;

        _projectionService.ActiveLowerThirdChanged += (s, item) =>
        {
            ActiveLowerThird = item;
        };

        // Initialize default 14 translation tabs synchronously
        string[] initialAbbrs = new[] { "KJV", "WEB", "ASV", "BBE", "BSB", "YLT", "Darby", "Geneva1599", "AKJV", "CPDV", "DRC", "LEB", "UKJV", "Webster" };
        foreach (var abbr in initialAbbrs)
        {
            TranslationTabs.Add(new TranslationTabViewModel
            {
                Abbreviation = abbr,
                IsActive = string.Equals(abbr, SelectedTranslationAbbreviation, StringComparison.OrdinalIgnoreCase)
            });
        }
        _ = LoadAvailableTranslationsAsync();

        // Initialize preset common topics
        foreach (var kvp in LocalCommonTopics)
        {
            _spokenPhraseIndex[kvp.Key] = kvp.Value;
        }

        // Asynchronously preload 100,000+ phrases from SQLite database into memory
        Task.Run(async () =>
        {
            try
            {
                var dbPhrases = await _bibleRepository.GetAllSpokenPhrasesAsync();
                if (dbPhrases != null)
                {
                    foreach (var kvp in dbPhrases)
                    {
                        _spokenPhraseIndex[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch { }
        });

        if (_communityMessageService != null)
        {
            _communityMessageService.MessageReceived += OnCommunityMessageReceived;
        }

        if (BibleSearch != null)
        {
            BibleSearch.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BibleSearchViewModel.SelectedResult))
                {
                    if (BibleSearch.SelectedResult != null)
                    {
                        SelectBibleSearchVerse(BibleSearch.SelectedResult);
                    }
                }
            };
        }

        if (HymnSearch != null)
        {
            HymnSearch.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(HymnSearchViewModel.SelectedResult))
                {
                    if (HymnSearch.SelectedResult != null)
                    {
                        SelectHymnSearchVerse(HymnSearch.SelectedResult);
                    }
                }
            };
        }

        _projectionService.CurrentlyProjectedItemChanged += OnCurrentlyProjectedItemChanged;
        _projectionService.DeselectedLinesPushed += (s, item) =>
        {
            SafeInvoke(() =>
            {
                var existing = DetailedContentItems.FirstOrDefault(x => x.Reference == item.Reference);
                if (existing != null)
                {
                    DetailedContentItems.Remove(existing);
                }

                // Insert directly AFTER currently projected item in DetailedContentItems
                int insertIndex = -1;
                if (CurrentlyProjectedItem != null)
                {
                    string currentBase = CurrentlyProjectedItem.Reference.Split('(')[0].Trim();
                    for (int i = 0; i < DetailedContentItems.Count; i++)
                    {
                        string itemBase = DetailedContentItems[i].Reference.Split('(')[0].Trim();
                        if (DetailedContentItems[i].Reference == CurrentlyProjectedItem.Reference || itemBase == currentBase)
                        {
                            insertIndex = i;
                        }
                    }
                }

                if (insertIndex >= 0 && insertIndex < DetailedContentItems.Count)
                {
                    DetailedContentItems.Insert(insertIndex + 1, item);
                }
                else
                {
                    DetailedContentItems.Insert(0, item);
                }
            });
        };
        _projectionService.NextItemRequested += (s, e) =>
        {
            SafeInvoke(() =>
            {
                MoveLiveProjectionDown();
            });
        };
        CurrentlyProjectedItem = _projectionService.CurrentlyProjectedItem;

        _speechRecognitionService.SpeechRecognized += OnSpeechRecognized;
        _speechRecognitionService.AudioLevelUpdated += OnAudioLevelUpdated;

        IsListening = _speechRecognitionService.IsListening;

        if (!IsListening)
        {
            _ = StartListeningOnStartupAsync();
        }
    }

    private async void SelectBibleSearchVerse(ChurchAI.Core.Entities.Verse verse)
    {
        if (verse == null) return;
        SelectedTranslationAbbreviation = verse.Book?.Translation?.Abbreviation ?? SelectedTranslationAbbreviation;
        try
        {
            var succeeding = await _bibleRepository.GetSucceedingVersesAsync(verse.BookId, verse.Chapter, verse.VerseNumber, 50);
            SafeInvoke(() =>
            {
                DetailedContentItems.Clear();
                DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                {
                    Reference = verse.DisplayReference,
                    Text = verse.Text
                });

                if (succeeding != null)
                {
                    foreach (var sv in succeeding)
                    {
                        DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                        {
                            Reference = sv.DisplayReference,
                            Text = sv.Text
                        });
                    }
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error selecting search verse: {ex.Message}");
        }
    }

    private void SelectHymnSearchVerse(ChurchAI.Core.Entities.Hymn hymn)
    {
        if (hymn == null) return;
        try
        {
            var lyrics = hymn.Lyrics ?? string.Empty;
            var verses = lyrics.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            SafeInvoke(() =>
            {
                DetailedContentItems.Clear();
                for (int i = 0; i < verses.Length; i++)
                {
                    var verseText = verses[i].Trim();
                    var verseNumber = i + 1;
                    DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                    {
                        Reference = $"{hymn.Title} (Hymn {hymn.Number} - Verse {verseNumber})",
                        Text = verseText
                    });
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error selecting search hymn: {ex.Message}");
        }
    }

    private async void OnCurrentlyProjectedItemChanged(object? sender, EventArgs e)
    {
        CurrentlyProjectedItem = _projectionService.CurrentlyProjectedItem;
        if (CurrentlyProjectedItem != null)
        {
            if (!CurrentlyProjectedItem.Reference.Contains("Part ") && !System.Text.RegularExpressions.Regex.IsMatch(CurrentlyProjectedItem.Reference, @"\(\d+/\d+\)"))
            {
                _activeItemRawRef = CurrentlyProjectedItem.Reference;
                _activeItemRawText = CurrentlyProjectedItem.Text;

                var hymnMatch = System.Text.RegularExpressions.Regex.Match(CurrentlyProjectedItem.Reference ?? "", @"\bHymn\s*#?\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (hymnMatch.Success)
                {
                    string newHymnNumber = hymnMatch.Groups[1].Value;
                    if (_activeHymnNumber != newHymnNumber)
                    {
                        _activeHymnNumber = newHymnNumber;
                        SelectedLinesPerSplitOption = "Off";
                        _linesPerSplitCount = 0;
                    }
                }
                else
                {
                    _activeHymnNumber = null;
                    SelectedLinesPerSplitOption = "Off";
                    _linesPerSplitCount = 0;
                }

                if (_linesPerSplitCount > 0)
                {
                    ApplyLineSplitToActiveItem();
                }

                await PopulateDetailedContentItemsAsync(CurrentlyProjectedItem);
            }
        }
    }

    partial void OnIsLowerThirdsChanged(bool value)
    {
        _projectionService.SetLowerThirds(value);
    }

    [RelayCommand]
    private void StartProjection()
    {
        _projectionService.ShowProjection();
    }

    [RelayCommand]
    private void StopProjection()
    {
        _projectionService.HideProjection();
    }

    [RelayCommand]
    private void ClearScreen()
    {
        _projectionService.ClearScreen();
    }

    [RelayCommand]
    private void ShowLogo()
    {
        _projectionService.ShowLogo();
    }

    [RelayCommand]
    private void ToggleLiveLineEditMode()
    {
        IsLiveLineEditMode = !IsLiveLineEditMode;
        if (IsLiveLineEditMode)
        {
            RebuildLiveLineItems();
        }
    }

    [RelayCommand]
    private void ToggleFullViewMode()
    {
        IsFullViewMode = !IsFullViewMode;
    }

    [RelayCommand]
    private void ToggleFullViewControls()
    {
        IsFullViewControlsExpanded = !IsFullViewControlsExpanded;
    }

    private void RebuildLiveLineItems()
    {
        LiveLineItems.Clear();
        if (CurrentlyProjectedItem == null || string.IsNullOrWhiteSpace(CurrentlyProjectedItem.Text)) return;

        var lines = CurrentlyProjectedItem.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var item = new LiveLineItem { LineText = line.Trim(), IsSelected = true };
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LiveLineItem.IsSelected))
                {
                    OnLiveLineSelectionChanged();
                }
            };
            LiveLineItems.Add(item);
        }
    }

    private void OnLiveLineSelectionChanged()
    {
        if (CurrentlyProjectedItem == null) return;

        var selectedLines = LiveLineItems.Where(x => x.IsSelected).Select(x => x.LineText).ToList();
        var deselectedLines = LiveLineItems.Where(x => !x.IsSelected).Select(x => x.LineText).ToList();

        if (selectedLines.Count > 0)
        {
            string selectedText = string.Join("\n", selectedLines);
            _projectionService.ProjectVerse(CurrentlyProjectedItem.Reference, selectedText);
        }

        if (deselectedLines.Count > 0)
        {
            string deselectedText = string.Join("\n", deselectedLines);
            var deselectedItem = new ChurchAI.Core.Models.ProjectedItem
            {
                Reference = CurrentlyProjectedItem.Reference.Contains("(Part 2)") ? CurrentlyProjectedItem.Reference : $"{CurrentlyProjectedItem.Reference} (Part 2)",
                Text = deselectedText
            };

            var existing = DetailedContentItems.FirstOrDefault(x => x.Reference == deselectedItem.Reference);
            if (existing != null)
            {
                DetailedContentItems.Remove(existing);
            }
            DetailedContentItems.Insert(0, deselectedItem);
        }
    }

    [RelayCommand]
    private async Task QuickProject()
    {
        if (!string.IsNullOrWhiteSpace(QuickProjectText))
        {
            var parsed = _referenceParser.Parse(QuickProjectText);
            if (parsed != null && parsed.Verse.HasValue)
            {
                var verse = await _bibleRepository.GetVerseAsync(1, parsed.BookName, parsed.Chapter, parsed.Verse.Value);
                if (verse != null)
                {
                    _projectionService.ProjectVerse($"{parsed.BookName} {parsed.Chapter}:{parsed.Verse.Value}", verse.Text);
                    QuickProjectText = string.Empty;
                    return;
                }
            }
            
            // Fallback: Just project what was typed if no verse found
            _projectionService.ProjectVerse("Message", QuickProjectText);
            QuickProjectText = string.Empty;
        }
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
            SpeechStatus = "Initializing Microphone...";
            await _speechRecognitionService.StartListeningAsync();
        }
    }

    private async Task StartListeningOnStartupAsync()
    {
        await Task.Delay(500); // Small initialization delay
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            IsListening = true;
            SpeechStatus = "Google listening...";
        });
        try
        {
            await _speechRecognitionService.StartListeningAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting speech service on startup: {ex.Message}");
        }
    }

    private void OnAudioLevelUpdated(object? sender, AudioLevelUpdatedEventArgs e)
    {
        CurrentAudioLevel = e.AudioLevel * 5.0; // Scale up for visual effect
    }

    private async void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        var text = e.Text;
        var isFinal = e.IsFinal;

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

        if (text.StartsWith("[")) return;

        if (isFinal)
        {
            _spottedInCurrentUtterance.Clear();
        }

        try 
        {
            // 1. Instantaneous Hymn Spotting (Sliding Window on Interim + Final)
            if (IsHymnListeningEnabled)
            {
                var activeHymnBook = _settingsService?.ActiveHymnBook ?? "Default";
                var matchedHymn = await _hymnRepository.MatchHymnByLyricsAsync(text, activeHymnBook);
                if (matchedHymn != null)
                {
                    var hymnKey = $"hymn_{matchedHymn.Number}";
                    if (!_spottedInCurrentUtterance.Contains(hymnKey))
                    {
                        _spottedInCurrentUtterance.Add(hymnKey);
                        
                        // Split the lyrics by double newlines to isolate individual verses
                        var verses = matchedHymn.Lyrics.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                        
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            ActiveDetectedVerse = null;

                            // Insert only the first verse as the representative for this hymn on the left card
                            var firstVerseText = verses.Length > 0 ? verses[0].Trim() : string.Empty;
                            var verseItem = new ChurchAI.Core.Models.ProjectedItem
                            {
                                Reference = $"{matchedHymn.Title} (Hymn {matchedHymn.Number})",
                                Text = firstVerseText
                            };

                            if (!DetectedSpeechHymns.Any(di => di.Reference == verseItem.Reference))
                            {
                                DetectedSpeechHymns.Insert(0, verseItem);

                                // Set initial preview/header to Verse 1
                                PreviewHeader = verseItem.Reference;
                                PreviewText = verseItem.Text;
                            }

                            // Trim collection to size limit (50 items max)
                            while (DetectedSpeechHymns.Count > 50)
                            {
                                DetectedSpeechHymns.RemoveAt(DetectedSpeechHymns.Count - 1);
                            }
                        });
                    }
                }
            }

            if (IsScriptureListeningEnabled)
            {
                // 1. Instantaneous Explicit Regex Matches with Stateful Book Context Listener - Sub-millisecond (0.01ms)
                var directMatch = _referenceParser.ParseWithContext(text);
                if (directMatch != null)
                {
                    var directKey = directMatch.ToString();
                    if (!_spottedInCurrentUtterance.Contains(directKey))
                    {
                        if (isFinal || directMatch.Verse.HasValue)
                        {
                            _spottedInCurrentUtterance.Add(directKey);
                            await ResolveAndPushReferenceAsync(directKey, isFinal: true);
                        }
                    }
                }

                // 2. Instantaneous In-Memory Phrase Spotting (120,000+ index) - Sub-millisecond (0.01ms)
                var normalizedInput = new string(text.ToLowerInvariant().Where(c => !char.IsPunctuation(c)).ToArray()).Trim();
                foreach (var topic in _spokenPhraseIndex)
                {
                    if (normalizedInput.Contains(topic.Key))
                    {
                        if (!_spottedInCurrentUtterance.Contains(topic.Key))
                        {
                            _spottedInCurrentUtterance.Add(topic.Key);
                            await ResolveAndPushReferenceAsync(topic.Value, isFinal: true);
                        }
                    }
                }

                // 3. Bible Passage / Quote Spotting on both interim and final text (Database Search)
                await MatchAndPushBibleQuoteAsync(text);

                // 4. Cloud / Ollama LLM Deep Multi-Verse Extraction on Final Pauses
                if (isFinal)
                {
                    string? extractedIntent = await _aiIntentService.ExtractBibleReferenceIntentAsync(text);
                    if (!string.IsNullOrWhiteSpace(extractedIntent))
                    {
                        var parts = extractedIntent.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var part in parts)
                        {
                            var trimmedPart = part.Trim();
                            var parsed = _referenceParser.Parse(trimmedPart);
                            if (parsed != null)
                            {
                                var parsedKey = parsed.ToString();
                                if (!_spottedInCurrentUtterance.Contains(parsedKey))
                                {
                                    _spottedInCurrentUtterance.Add(parsedKey);
                                    await ResolveAndPushReferenceAsync(parsedKey, isFinal: true);
                                }
                            }
                        }
                    }
                }
            }
        } 
        catch (Exception ex)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => SpeechStatus = $"Error: Exception - {ex.Message}");
        }
    }

    private string CleanBibleQuoteText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Convert to lowercase and remove punctuation
        string cleaned = text.ToLowerInvariant();
        cleaned = new string(cleaned.Where(c => !char.IsPunctuation(c)).ToArray());

        // Define common prefixes to strip
        string[] prefixes = new[]
        {
            "the bible says that", "the bible says", "the scripture says that", "the scripture says",
            "the scriptures say that", "the scriptures say", "as the bible says", "as the scripture says",
            "as the scriptures say", "for the bible says", "for the scripture says", "in the book of",
            "scriptures say", "scripture says", "bible says", "jesus said", "paul wrote",
            "the bible teaches", "bible teaches", "word of god says"
        };

        foreach (var prefix in prefixes)
        {
            if (cleaned.StartsWith(prefix))
            {
                cleaned = cleaned.Substring(prefix.Length).Trim();
                break; // Only strip the first matched prefix
            }
        }

        return cleaned.Trim();
    }

    private async Task MatchAndPushBibleQuoteAsync(string text)
    {
        string cleaned = CleanBibleQuoteText(text);
        if (string.IsNullOrWhiteSpace(cleaned)) return;

        var words = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 4) return;

        int significantWords = words.Count(w => w.Length >= 4);
        if (significantWords < 2) return;

        if (cleaned == _lastSearchedQuoteText) return;
        _lastSearchedQuoteText = cleaned;

        // Cancel previous pending quote search task to prioritize latest phrase
        _quoteCts?.Cancel();
        _quoteCts = new CancellationTokenSource();
        var ct = _quoteCts.Token;

        try
        {
            await Task.Delay(80, ct);
            if (ct.IsCancellationRequested) return;

            int translationId = await _bibleRepository.GetTranslationIdAsync(SelectedTranslationAbbreviation);
            ChurchAI.Core.Entities.Verse? matchedVerse = null;

            // Step 1: Early-Trigger Signature Prefix Match (First 5-6 Words)
            string signaturePrefix = string.Join(" ", words.Take(Math.Min(6, words.Length)));
            var prefixMatches = await _bibleRepository.SearchAsync(translationId, signaturePrefix);

            if (prefixMatches != null && prefixMatches.Count() == 1)
            {
                matchedVerse = prefixMatches.First();
            }

            // Step 2: Full Clean Phrase Search if prefix was ambiguous or yielded no single match
            if (matchedVerse == null)
            {
                if (ct.IsCancellationRequested) return;
                var fullMatches = await _bibleRepository.SearchAsync(translationId, cleaned);
                if (fullMatches != null && fullMatches.Any())
                {
                    matchedVerse = fullMatches.OrderBy(v => Math.Abs(v.Text.Length - cleaned.Length)).First();
                }
            }

            // Step 3: Fallback Search in core translations
            if (matchedVerse == null)
            {
                var translations = new[] { "KJV", "WEB", "ASV", "BBE" };
                foreach (var abbr in translations)
                {
                    if (ct.IsCancellationRequested) break;
                    if (abbr.Equals(SelectedTranslationAbbreviation, StringComparison.OrdinalIgnoreCase)) continue;

                    int fallbackId = await _bibleRepository.GetTranslationIdAsync(abbr);
                    var fallbackPrefixMatches = await _bibleRepository.SearchAsync(fallbackId, signaturePrefix);
                    if (fallbackPrefixMatches != null && fallbackPrefixMatches.Count() == 1)
                    {
                        matchedVerse = fallbackPrefixMatches.First();
                        break;
                    }

                    var fallbackFullMatches = await _bibleRepository.SearchAsync(fallbackId, cleaned);
                    if (fallbackFullMatches != null && fallbackFullMatches.Any())
                    {
                        matchedVerse = fallbackFullMatches.OrderBy(v => Math.Abs(v.Text.Length - cleaned.Length)).First();
                        break;
                    }
                }
            }

            if (matchedVerse != null && !ct.IsCancellationRequested)
            {
                var referenceKey = matchedVerse.DisplayReference;
                if (!_spottedInCurrentUtterance.Contains(referenceKey))
                {
                    _spottedInCurrentUtterance.Add(referenceKey);
                    await ResolveAndPushReferenceAsync(referenceKey, isFinal: true);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignored - cancelled in favor of newer audio phrase
        }
        catch
        {
            // Keep streaming speech processing alive
        }
    }

    private async Task ResolveAndPushReferenceAsync(string referenceStr, bool isFinal)
    {
        var reference = _referenceParser.Parse(referenceStr);
        if (reference == null) return;

        int verseNumber = reference.Verse ?? 1;
        var verses = await _bibleRepository.GetVerseInAllTranslationsAsync(reference.BookName, reference.Chapter, verseNumber);
        if (verses != null && verses.Any())
        {
            var defaultVerse = verses.FirstOrDefault(v => v.Book?.Translation?.Abbreviation == SelectedTranslationAbbreviation) 
                               ?? verses.FirstOrDefault();

            // 1. Immediately push card and active verse UI elements
            SafeInvoke(() =>
            {
                EditableReference = $"{reference.BookName} {reference.Chapter}:{verseNumber}";
                AllTranslationsForPopup.Clear();
                foreach (var v in verses)
                {
                    AllTranslationsForPopup.Add(v);
                }
                ActiveDetectedVerse = defaultVerse;

                if (defaultVerse != null)
                {
                    var scriptureItem = new ChurchAI.Core.Models.ProjectedItem
                    {
                        Reference = $"{reference.BookName} {reference.Chapter}:{verseNumber}",
                        Text = defaultVerse.Text
                    };

                    if (!DetectedSpeechScriptures.Any(di => di.Reference == scriptureItem.Reference))
                    {
                        DetectedSpeechScriptures.Insert(0, scriptureItem);
                        if (DetectedSpeechScriptures.Count > 50)
                        {
                            DetectedSpeechScriptures.RemoveAt(DetectedSpeechScriptures.Count - 1);
                        }
                    }
                }
            });

            // 2. Load succeeding 50 verses in background so UI rendering is non-blocking
            if (defaultVerse != null)
            {
                _ = Task.Run(async () =>
                {
                    var succeeding = await _bibleRepository.GetSucceedingVersesAsync(defaultVerse.BookId, defaultVerse.Chapter, defaultVerse.VerseNumber, 50);
                    if (succeeding != null && succeeding.Any())
                    {
                        SafeInvoke(() =>
                        {
                            SucceedingVerses.Clear();
                            foreach (var sv in succeeding)
                            {
                                SucceedingVerses.Add(sv);
                            }
                        });
                    }
                });
            }
        }
    }

    private System.Threading.CancellationTokenSource? _clickCancelSource;

    [RelayCommand]
    private async Task PreviewSpeechItemAsync(ChurchAI.Core.Models.ProjectedItem? item)
    {
        if (item == null) return;

        // Cancel any pending click
        _clickCancelSource?.Cancel();
        _clickCancelSource = new System.Threading.CancellationTokenSource();
        var token = _clickCancelSource.Token;

        try
        {
            // Wait briefly to see if a double-click is executed (cancelling this single-click)
            await Task.Delay(250, token);

            PreviewHeader = item.Reference;
            PreviewText = item.Text;

            // Project it directly (without showing the window/output screen, showing only in live card)
            _projectionService.AddToQueue(item.Reference, item.Text);
            _projectionService.ProjectVerse(item.Reference, item.Text, showWindow: false);

            // Populate the right card details view
            await PopulateDetailedContentItemsAsync(item);
        }
        catch (TaskCanceledException)
        {
            // Cancelled due to double-click
        }
    }

    [RelayCommand]
    private void ProjectSpeechItem(ChurchAI.Core.Models.ProjectedItem? item)
    {
        if (item != null)
        {
            _clickCancelSource?.Cancel();
            _projectionService.AddToQueue(item.Reference, item.Text);
            ProjectItemWithSplitRules(item);
            PreviewHeader = item.Reference;
            PreviewText = item.Text;
            ActiveDetectedVerse = null;
        }
    }

    [RelayCommand]
    private void ProjectPreview()
    {
        if (!string.IsNullOrWhiteSpace(PreviewHeader) && !string.IsNullOrWhiteSpace(PreviewText))
        {
            _projectionService.AddToQueue(PreviewHeader, PreviewText);
            _projectionService.ProjectVerse(PreviewHeader, PreviewText);
        }
    }

    private int ParseHymnNumber(string reference)
    {
        if (string.IsNullOrEmpty(reference)) return -1;

        var match = System.Text.RegularExpressions.Regex.Match(reference, @"\bHymn\s*#?\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int num))
        {
            return num;
        }

        return -1;
    }

    private async Task PopulateDetailedContentItemsAsync(ChurchAI.Core.Models.ProjectedItem item)
    {
        try
        {
            SafeInvoke(() => DetailedContentItems.Clear());

            if (item.Reference.Contains("(Hymn") || item.Reference.Contains("Hymn ") || item.Reference.Contains("Hymn:"))
            {
                int hymnNumber = ParseHymnNumber(item.Reference);
                if (hymnNumber != -1)
                {
                    var activeHymnBook = _settingsService?.ActiveHymnBook ?? "Default";
                    var hymn = await _hymnRepository.GetHymnByNumberAsync(hymnNumber, activeHymnBook)
                            ?? await _hymnRepository.GetHymnByNumberAsync(hymnNumber, "Default")
                            ?? (await _hymnRepository.GetAllHymnsAsync()).FirstOrDefault(h => h.Number == hymnNumber);

                    if (hymn != null)
                    {
                        var verses = hymn.Lyrics.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                        SafeInvoke(() =>
                        {
                            for (int i = 0; i < verses.Length; i++)
                            {
                                var verseText = verses[i].Trim();
                                var verseNumber = i + 1;
                                DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                                {
                                    Reference = $"{hymn.Title} (Hymn {hymn.Number} - Verse {verseNumber})",
                                    Text = verseText
                                });
                            }
                        });
                    }
                }
            }
            else
            {
                var reference = _referenceParser.Parse(item.Reference);
                if (reference != null)
                {
                    int verseNum = reference.Verse ?? 1;
                    var verses = await _bibleRepository.GetVerseInAllTranslationsAsync(reference.BookName, reference.Chapter, verseNum);
                    if (verses != null && verses.Any())
                    {
                        var defaultVerse = verses.FirstOrDefault(v => v.Book?.Translation?.Abbreviation == SelectedTranslationAbbreviation) 
                                           ?? verses.FirstOrDefault();
                        if (defaultVerse != null)
                        {
                            var succeeding = await _bibleRepository.GetSucceedingVersesAsync(defaultVerse.BookId, defaultVerse.Chapter, defaultVerse.VerseNumber, 50);
                            SafeInvoke(() =>
                            {
                                DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                                {
                                    Reference = defaultVerse.DisplayReference,
                                    Text = defaultVerse.Text
                                });

                                if (succeeding != null)
                                {
                                    foreach (var sv in succeeding)
                                    {
                                        DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                                        {
                                            Reference = sv.DisplayReference,
                                            Text = sv.Text
                                        });
                                    }
                                }
                            });
                        }
                    }
                }
                else
                {
                    SafeInvoke(() =>
                    {
                        DetailedContentItems.Add(item);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error populating detailed content: {ex.Message}");
        }
    }

    [ObservableProperty]
    private string _selectedTextAlignmentOption = "Center";

    public ObservableCollection<string> TextAlignmentOptions { get; } = new()
    {
        "Center",
        "Left",
        "Right"
    };

    partial void OnSelectedTextAlignmentOptionChanged(string value)
    {
        _projectionService.SetTextAlignment(value);
    }

    [ObservableProperty]
    private string _selectedLinesPerSplitOption = "Off";

    public ObservableCollection<string> LinesPerSplitOptions { get; } = new()
    {
        "Off",
        "1 Line",
        "2 Lines",
        "3 Lines",
        "4 Lines",
        "5 Lines"
    };

    private int _linesPerSplitCount = 0;
    private string? _activeHymnNumber = null;
    private string? _activeItemRawText = null;
    private string? _activeItemRawRef = null;
    private List<string> _currentSplitChunks = new();
    private int _currentSplitChunkIndex = 0;

    partial void OnSelectedLinesPerSplitOptionChanged(string value)
    {
        if (int.TryParse(value.Split(' ')[0], out int count))
        {
            _linesPerSplitCount = count;
        }
        else
        {
            _linesPerSplitCount = 0; // Off
        }

        if (CurrentlyProjectedItem != null && !string.IsNullOrEmpty(_activeItemRawText))
        {
            ApplyLineSplitToActiveItem();
        }
    }

    private List<string> GetTextLines(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return new List<string>();

        // Clean leading stanza numbers (e.g. "4. So we..." -> "So we...")
        string cleanedRaw = System.Text.RegularExpressions.Regex.Replace(rawText.Trim(), @"^\d+[\.\s\-]+", "");

        // 1. Split raw text by explicit newlines (\r\n or \n) or punctuation clauses if single paragraph
        var initialLines = cleanedRaw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(l => System.Text.RegularExpressions.Regex.Replace(l.Trim(), @"^\d+[\.\s\-]+", ""))
                                     .Where(l => !string.IsNullOrEmpty(l))
                                     .ToList();

        if (initialLines.Count <= 1)
        {
            var clauses = System.Text.RegularExpressions.Regex.Split(cleanedRaw.Trim(), @"(?<=[.?!;:])\s+")
                                                              .Select(c => c.Trim())
                                                              .Where(c => !string.IsNullOrEmpty(c))
                                                              .ToList();
            if (clauses.Count > 1)
            {
                initialLines = clauses;
            }
        }

        // 2. Wrap any line exceeding ~30 characters on word boundaries to match display line width
        int maxCharsPerVisualLine = 30;
        List<string> visualLines = new();

        foreach (var line in initialLines)
        {
            var words = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) continue;

            string currentLine = "";
            foreach (var word in words)
            {
                if ((currentLine + " " + word).Trim().Length > maxCharsPerVisualLine && !string.IsNullOrEmpty(currentLine))
                {
                    visualLines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                }
            }
            if (!string.IsNullOrEmpty(currentLine))
            {
                visualLines.Add(currentLine);
            }
        }

        return visualLines.Count > 0 ? visualLines : new List<string> { cleanedRaw };
    }

    private void ApplyLineSplitToActiveItem()
    {
        if (string.IsNullOrEmpty(_activeItemRawText) || string.IsNullOrEmpty(_activeItemRawRef)) return;

        if (_linesPerSplitCount <= 0)
        {
            _currentSplitChunks.Clear();
            _currentSplitChunkIndex = 0;
            _projectionService.ProjectVerse(_activeItemRawRef, _activeItemRawText);
            return;
        }

        var lines = GetTextLines(_activeItemRawText);

        if (lines.Count <= _linesPerSplitCount)
        {
            _currentSplitChunks = new List<string> { _activeItemRawText };
        }
        else
        {
            _currentSplitChunks = new List<string>();
            for (int i = 0; i < lines.Count; i += _linesPerSplitCount)
            {
                var chunkLines = lines.Skip(i).Take(_linesPerSplitCount);
                _currentSplitChunks.Add(string.Join("\n", chunkLines));
            }
        }

        _currentSplitChunkIndex = 0;
        ProjectCurrentSplitChunk();
    }

    private void ProjectCurrentSplitChunk()
    {
        if (_currentSplitChunks.Count == 0 || string.IsNullOrEmpty(_activeItemRawRef)) return;

        string chunkText = _currentSplitChunks[_currentSplitChunkIndex];
        string displayRef = _activeItemRawRef;
        if (_currentSplitChunks.Count > 1)
        {
            displayRef = $"{_activeItemRawRef} ({_currentSplitChunkIndex + 1}/{_currentSplitChunks.Count})";
        }

        _projectionService.ProjectVerse(displayRef, chunkText);
    }

    private void ProjectItemWithSplitRules(ProjectedItem item)
    {
        if (item == null) return;
        _activeItemRawRef = item.Reference;
        _activeItemRawText = item.Text;

        var hymnMatch = System.Text.RegularExpressions.Regex.Match(item.Reference ?? "", @"\bHymn\s*#?\s*(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (hymnMatch.Success)
        {
            string newHymnNumber = hymnMatch.Groups[1].Value;
            if (_activeHymnNumber != newHymnNumber)
            {
                _activeHymnNumber = newHymnNumber;
                SelectedLinesPerSplitOption = "Off";
                _linesPerSplitCount = 0;
            }
        }
        else
        {
            _activeHymnNumber = null;
            SelectedLinesPerSplitOption = "Off";
            _linesPerSplitCount = 0;
        }

        ApplyLineSplitToActiveItem();
    }

    [RelayCommand]
    private void MoveLiveProjectionDown()
    {
        if (_linesPerSplitCount > 0 && _currentSplitChunks.Count > 1 && _currentSplitChunkIndex + 1 < _currentSplitChunks.Count)
        {
            _currentSplitChunkIndex++;
            ProjectCurrentSplitChunk();
            return;
        }

        if (DetailedContentItems.Count == 0) return;

        int index = -1;
        if (CurrentlyProjectedItem != null)
        {
            for (int i = 0; i < DetailedContentItems.Count; i++)
            {
                if (DetailedContentItems[i].Reference == CurrentlyProjectedItem.Reference ||
                    DetailedContentItems[i].Reference == _activeItemRawRef)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                string currentRefBase = CurrentlyProjectedItem.Reference.Split('(')[0].Trim();
                for (int i = 0; i < DetailedContentItems.Count; i++)
                {
                    string itemRefBase = DetailedContentItems[i].Reference.Split('(')[0].Trim();
                    if (itemRefBase == currentRefBase)
                    {
                        index = i;
                        break;
                    }
                }
            }
        }

        if (index >= 0 && index + 1 < DetailedContentItems.Count)
        {
            var nextItem = DetailedContentItems[index + 1];
            ProjectItemWithSplitRules(nextItem);
            SelectedDetailedItem = nextItem;
        }
        else if (DetailedContentItems.Count > 0)
        {
            var firstItem = DetailedContentItems[0];
            ProjectItemWithSplitRules(firstItem);
            SelectedDetailedItem = firstItem;
        }
    }

    [RelayCommand]
    private void MoveLiveProjectionUp()
    {
        if (_linesPerSplitCount > 0 && _currentSplitChunks.Count > 1 && _currentSplitChunkIndex > 0)
        {
            _currentSplitChunkIndex--;
            ProjectCurrentSplitChunk();
            return;
        }

        if (CurrentlyProjectedItem == null || DetailedContentItems.Count == 0) return;

        int index = -1;
        for (int i = 0; i < DetailedContentItems.Count; i++)
        {
            if (DetailedContentItems[i].Reference == CurrentlyProjectedItem.Reference ||
                DetailedContentItems[i].Reference == _activeItemRawRef)
            {
                index = i;
                break;
            }
        }

        if (index > 0)
        {
            var prevItem = DetailedContentItems[index - 1];
            ProjectItemWithSplitRules(prevItem);
            SelectedDetailedItem = prevItem;
        }
    }

    private async Task ReloadDetailedItemsInNewTranslationAsync(string newTranslation)
    {
        try
        {
            var itemsCopy = DetailedContentItems.ToList();
            if (itemsCopy.Count == 0) return;

            var firstItem = itemsCopy[0];
            if (firstItem.Reference.Contains("(Hymn")) return; // Hymn - no translation change

            var reference = _referenceParser.Parse(firstItem.Reference);
            if (reference != null)
            {
                int vNum = reference.Verse ?? 1;
                var mainVerses = await _bibleRepository.GetVerseInAllTranslationsAsync(reference.BookName, reference.Chapter, vNum);
                var mainMatch = mainVerses?.FirstOrDefault(v => v.Book?.Translation?.Abbreviation.Equals(newTranslation, StringComparison.OrdinalIgnoreCase) == true)
                                ?? mainVerses?.FirstOrDefault();

                if (mainMatch != null)
                {
                    var succeeding = await _bibleRepository.GetSucceedingVersesAsync(mainMatch.BookId, mainMatch.Chapter, mainMatch.VerseNumber, itemsCopy.Count - 1);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        DetailedContentItems.Clear();
                        DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                        {
                            Reference = mainMatch.DisplayReference,
                            Text = mainMatch.Text
                        });

                        if (succeeding != null)
                        {
                            foreach (var sv in succeeding)
                            {
                                DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                                {
                                    Reference = sv.DisplayReference,
                                    Text = sv.Text
                                });
                            }
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error re-translating detailed items: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ManualOverrideAsync()
    {
        if (string.IsNullOrWhiteSpace(EditableReference)) return;

        try
        {
            var reference = _referenceParser.Parse(EditableReference);
            if (reference != null)
            {
                int verseNumber = reference.Verse ?? 1;
                var verses = await _bibleRepository.GetVerseInAllTranslationsAsync(reference.BookName, reference.Chapter, verseNumber);
                if (verses != null && verses.Any())
                {
                    var defaultVerse = verses.FirstOrDefault(v => v.Book?.Translation?.Abbreviation == SelectedTranslationAbbreviation) 
                                       ?? verses.FirstOrDefault();
                    var succeeding = await _bibleRepository.GetSucceedingVersesAsync(defaultVerse.BookId, defaultVerse.Chapter, defaultVerse.VerseNumber, 50);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        AllTranslationsForPopup.Clear();
                        foreach (var v in verses)
                        {
                            AllTranslationsForPopup.Add(v);
                        }
                        SucceedingVerses.Clear();
                        DetailedContentItems.Clear();

                        DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                        {
                            Reference = defaultVerse.DisplayReference,
                            Text = defaultVerse.Text
                        });

                        if (succeeding != null)
                        {
                            foreach (var sv in succeeding)
                            {
                                SucceedingVerses.Add(sv);
                                DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                                {
                                    Reference = sv.DisplayReference,
                                    Text = sv.Text
                                });
                            }
                        }
                        ActiveDetectedVerse = defaultVerse;
                    });
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    private void DismissPopup()
    {
        AllTranslationsForPopup.Clear();
        ActiveDetectedVerse = null;
        EditableReference = string.Empty;
        _committedTranscript = string.Empty;
        LiveTranscript = "Welcome to ChurchXtra AI Control Room. Click the microphone to start listening...";
    }

    [RelayCommand]
    private void ProjectFromPopup()
    {
        var verse = ActiveDetectedVerse;
        if (verse != null)
        {
            var reference = $"{verse.Book?.Name} {verse.Chapter}:{verse.VerseNumber}";
            var translationName = verse.Book?.Translation?.Abbreviation ?? "";
            if (!string.IsNullOrEmpty(translationName))
            {
                reference += $" ({translationName})";
            }
            
            _projectionService.AddToQueue(reference, verse.Text);
            _projectionService.ProjectVerse(reference, verse.Text);
        }
    }

    [RelayCommand]
    private async Task ProjectSelectedAsync(ProjectedItem item)
    {
        if (item == null) return;

        _projectionService.ProjectVerse(item.Reference, item.Text);
        await PopulateDetailedContentItemsAsync(item);

        try
        {
            var reference = _referenceParser.Parse(item.Reference);
            if (reference != null)
            {
                string translation = SelectedTranslationAbbreviation;
                if (item.Reference.Contains("("))
                {
                    int openIdx = item.Reference.IndexOf('(');
                    int closeIdx = item.Reference.IndexOf(')', openIdx);
                    if (openIdx >= 0 && closeIdx > openIdx)
                    {
                        var tAbbr = item.Reference.Substring(openIdx + 1, closeIdx - openIdx - 1).Trim();
                        if (tAbbr.Equals("KJV", StringComparison.OrdinalIgnoreCase) ||
                            tAbbr.Equals("WEB", StringComparison.OrdinalIgnoreCase) ||
                            tAbbr.Equals("ASV", StringComparison.OrdinalIgnoreCase) ||
                            tAbbr.Equals("BBE", StringComparison.OrdinalIgnoreCase))
                        {
                            translation = tAbbr.ToUpperInvariant();
                        }
                    }
                }

                int verseNumber = reference.Verse ?? 1;
                var verses = await _bibleRepository.GetVerseInAllTranslationsAsync(reference.BookName, reference.Chapter, verseNumber);
                if (verses != null && verses.Any())
                {
                    var defaultVerse = verses.FirstOrDefault(v => v.Book?.Translation?.Abbreviation.Equals(translation, StringComparison.OrdinalIgnoreCase) == true) 
                                       ?? verses.FirstOrDefault();
                    var succeeding = await _bibleRepository.GetSucceedingVersesAsync(defaultVerse.BookId, defaultVerse.Chapter, defaultVerse.VerseNumber, 50);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        SelectedTranslationAbbreviation = translation;
                        EditableReference = $"{reference.BookName} {reference.Chapter}:{verseNumber}";
                        AllTranslationsForPopup.Clear();
                        foreach (var v in verses)
                        {
                            AllTranslationsForPopup.Add(v);
                        }
                        SucceedingVerses.Clear();
                        if (succeeding != null)
                        {
                            foreach (var sv in succeeding)
                            {
                                SucceedingVerses.Add(sv);
                            }
                        }
                        ActiveDetectedVerse = defaultVerse;
                    });
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    private void RemoveSelected(ProjectedItem item)
    {
        if (item != null)
        {
            _projectionService.RemoveFromQueue(item);
        }
    }

    [ObservableProperty]
    private bool _isTranslationTabsExpanded;

    public ObservableCollection<TranslationTabViewModel> TranslationTabs { get; } = new();

    public IEnumerable<TranslationTabViewModel> VisibleTranslationTabs
    {
        get
        {
            if (IsTranslationTabsExpanded)
            {
                return TranslationTabs;
            }
            return TranslationTabs.Take(4);
        }
    }

    public bool HasMoreTranslationTabs => TranslationTabs.Count > 4;

    partial void OnIsTranslationTabsExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(VisibleTranslationTabs));
    }

    [RelayCommand]
    private void ToggleTranslationTabsExpanded()
    {
        IsTranslationTabsExpanded = !IsTranslationTabsExpanded;
    }

    private async Task LoadAvailableTranslationsAsync()
    {
        try
        {
            var dbTranslations = await _bibleRepository.GetAvailableTranslationsAsync();
            var list = dbTranslations.ToList();
            if (list.Any())
            {
                SafeInvoke(() =>
                {
                    TranslationTabs.Clear();
                    foreach (var t in list)
                    {
                        TranslationTabs.Add(new TranslationTabViewModel
                        {
                            Abbreviation = t.Abbreviation,
                            Name = t.Name,
                            IsActive = string.Equals(t.Abbreviation, SelectedTranslationAbbreviation, StringComparison.OrdinalIgnoreCase)
                        });
                    }
                    OnPropertyChanged(nameof(VisibleTranslationTabs));
                    OnPropertyChanged(nameof(HasMoreTranslationTabs));
                });
            }
        }
        catch { }
    }

    public bool IsKjvActive => SelectedTranslationAbbreviation == "KJV";
    public bool IsWebActive => SelectedTranslationAbbreviation == "WEB";
    public bool IsAsvActive => SelectedTranslationAbbreviation == "ASV";
    public bool IsBbeActive => SelectedTranslationAbbreviation == "BBE";

    partial void OnSelectedTranslationAbbreviationChanged(string value)
    {
        var matchTab = TranslationTabs.FirstOrDefault(t => string.Equals(t.Abbreviation, value, StringComparison.OrdinalIgnoreCase));
        foreach (var tab in TranslationTabs)
        {
            tab.IsActive = string.Equals(tab.Abbreviation, value, StringComparison.OrdinalIgnoreCase);
        }

        if (matchTab != null)
        {
            int index = TranslationTabs.IndexOf(matchTab);
            if (index >= 4 && !IsTranslationTabsExpanded)
            {
                IsTranslationTabsExpanded = true;
            }
        }

        OnPropertyChanged(nameof(VisibleTranslationTabs));
        OnPropertyChanged(nameof(IsKjvActive));
        OnPropertyChanged(nameof(IsWebActive));
        OnPropertyChanged(nameof(IsAsvActive));
        OnPropertyChanged(nameof(IsBbeActive));
    }

    partial void OnActiveDetectedVerseChanged(ChurchAI.Core.Entities.Verse? value)
    {
        if (value != null)
        {
            PreviewHeader = value.DisplayReference;
            PreviewText = value.Text;

            if (value.Book?.Translation != null)
            {
                SelectedTranslationAbbreviation = value.Book.Translation.Abbreviation;
                SucceedingSearchReference = $"{value.Book.Name} {value.Chapter}:{value.VerseNumber}";
            }
        }
        else
        {
            PreviewHeader = "Preview - No Verse Selected";
            PreviewText = "Select a scripture to preview it here.";
            SucceedingVerses.Clear();
            SucceedingSearchReference = string.Empty;
        }
    }

    [RelayCommand]
    private async Task ProjectSucceedingSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SucceedingSearchReference)) return;

        try
        {
            var reference = _referenceParser.Parse(SucceedingSearchReference);
            if (reference != null)
            {
                int verseNumber = reference.Verse ?? 1;
                var verses = await _bibleRepository.GetVerseInAllTranslationsAsync(reference.BookName, reference.Chapter, verseNumber);
                if (verses != null && verses.Any())
                {
                    var defaultVerse = verses.FirstOrDefault(v => v.Book?.Translation?.Abbreviation == SelectedTranslationAbbreviation) 
                                       ?? verses.FirstOrDefault();
                    var succeeding = await _bibleRepository.GetSucceedingVersesAsync(defaultVerse.BookId, defaultVerse.Chapter, defaultVerse.VerseNumber, 50);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        AllTranslationsForPopup.Clear();
                        foreach (var v in verses)
                        {
                            AllTranslationsForPopup.Add(v);
                        }
                        SucceedingVerses.Clear();
                        DetailedContentItems.Clear();

                        DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                        {
                            Reference = defaultVerse.DisplayReference,
                            Text = defaultVerse.Text
                        });

                        if (succeeding != null)
                        {
                            foreach (var sv in succeeding)
                            {
                                SucceedingVerses.Add(sv);
                                DetailedContentItems.Add(new ChurchAI.Core.Models.ProjectedItem
                                {
                                    Reference = sv.DisplayReference,
                                    Text = sv.Text
                                });
                            }
                        }
                        
                        ActiveDetectedVerse = defaultVerse;
                        ProjectFromPopup();
                    });
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    private void ProjectVerse(ChurchAI.Core.Entities.Verse? verse)
    {
        var target = verse ?? ActiveDetectedVerse;
        if (target != null)
        {
            var reference = $"{target.Book?.Name} {target.Chapter}:{target.VerseNumber}";
            var translationName = target.Book?.Translation?.Abbreviation ?? "";
            if (!string.IsNullOrEmpty(translationName))
            {
                reference += $" ({translationName})";
            }
            _projectionService.AddToQueue(reference, target.Text);
            _projectionService.ProjectVerse(reference, target.Text);
        }
    }

    [RelayCommand]
    private void PreviewAnnouncement()
    {
        PreviewHeader = AnnouncementTitle;
        PreviewText = AnnouncementText;
        ActiveDetectedVerse = null;
    }

    [RelayCommand]
    private void ProjectAnnouncement()
    {
        if (string.IsNullOrWhiteSpace(AnnouncementText)) return;
        _projectionService.AddToQueue(AnnouncementTitle, AnnouncementText);
        _projectionService.ProjectVerse(AnnouncementTitle, AnnouncementText);
        PreviewAnnouncement();
    }

    [RelayCommand]
    private void AddAnnouncementToQueue()
    {
        if (string.IsNullOrWhiteSpace(AnnouncementText)) return;
        _projectionService.AddToQueue(AnnouncementTitle, AnnouncementText);
    }

    [RelayCommand]
    private void SaveAnnouncementTemplate()
    {
        if (string.IsNullOrWhiteSpace(AnnouncementText)) return;
        if (!SavedAnnouncements.Any(a => a.Reference == AnnouncementTitle && a.Text == AnnouncementText))
        {
            SavedAnnouncements.Add(new ChurchAI.Core.Models.ProjectedItem { Reference = AnnouncementTitle, Text = AnnouncementText });
        }
    }

    [RelayCommand]
    private void ProjectSavedAnnouncement(ChurchAI.Core.Models.ProjectedItem? item)
    {
        if (item != null)
        {
            _projectionService.AddToQueue(item.Reference, item.Text);
            _projectionService.ProjectVerse(item.Reference, item.Text);
            PreviewHeader = item.Reference;
            PreviewText = item.Text;
            ActiveDetectedVerse = null;
        }
    }

    [ObservableProperty]
    private int _selectedLeftCardSubTabIndex = 0;

    [ObservableProperty]
    private int _unreadCommunityMessageCount = 0;

    public string CommunityTabHeader => UnreadCommunityMessageCount > 0
        ? $"comm ({UnreadCommunityMessageCount})"
        : "comm";

    [ObservableProperty]
    private int _selectedVersesInnerSubTabIndex = 0;

    [RelayCommand]
    private void PerformScriptureSearch()
    {
        SelectedLeftCardSubTabIndex = 1; // Switch to Verses tab
        SelectedVersesInnerSubTabIndex = 0; // Switch to Search sub-tab
        BibleSearch.SearchCommand.Execute(null);
    }

    partial void OnSelectedLeftCardSubTabIndexChanged(int value)
    {
        if (value == 3)
        {
            UnreadCommunityMessageCount = 0;
            OnPropertyChanged(nameof(CommunityTabHeader));
        }
    }

    [RelayCommand]
    private void ProjectIncomingMessage()
    {
        _projectionService.ProjectVerse(string.Empty, CurrentMessageText);
        IsCommunityMessagePopupOpen = false;
    }

    [RelayCommand]
    private void AddIncomingMessageToSchedule()
    {
        _projectionService.AddToQueue("Community Alert", CurrentMessageText);
        IsCommunityMessagePopupOpen = false;
    }

    [RelayCommand]
    private void DiscardIncomingMessage()
    {
        IsCommunityMessagePopupOpen = false;
    }

    private void OnCommunityMessageReceived(object? sender, CommunityMessageEventArgs e)
    {
        SafeInvoke(() =>
        {
            CurrentMessageHeader = e.Message.Title;
            CurrentMessageText = e.Message.Text;
            CurrentMessageTagColor = e.Message.TagColor;
            CurrentMessageTagLabel = e.Message.TagLabel;
            IsCommunityMessagePopupOpen = false;

            if (SelectedLeftCardSubTabIndex != 3)
            {
                UnreadCommunityMessageCount++;
            }
            else
            {
                UnreadCommunityMessageCount = 0;
            }
            OnPropertyChanged(nameof(CommunityTabHeader));
        });
    }

    [RelayCommand]
    private void ClearAnnouncementFields()
    {
        AnnouncementTitle = "Announcement";
        AnnouncementText = string.Empty;
    }

    [RelayCommand]
    private void ShowCustomHymnPopup()
    {
        IsCustomHymnPopupOpen = true;
    }

    [RelayCommand]
    private void AddDroppedItem(object item)
    {
        if (item == null) return;

        if (item is Verse verse)
        {
            var reference = $"{verse.Book?.Name} {verse.Chapter}:{verse.VerseNumber}";
            var translationName = verse.Book?.Translation?.Abbreviation ?? "";
            if (!string.IsNullOrEmpty(translationName))
            {
                reference += $" ({translationName})";
            }
            _projectionService.AddToQueue(reference, verse.Text);
        }
        else if (item is Hymn hymn)
        {
            var title = $"Hymn {hymn.Number}: {hymn.Title} ({hymn.Book})";
            var stanzas = System.Text.RegularExpressions.Regex.Split(hymn.Lyrics, @"(?:\r?\n){2,}")
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (stanzas.Count > 0)
            {
                for (int i = 0; i < stanzas.Count; i++)
                {
                    var refStr = stanzas.Count > 1 ? $"{title} - Verse {i + 1}" : title;
                    _projectionService.AddToQueue(refStr, stanzas[i]);
                }
            }
        }
    }

    [RelayCommand]
    private void CloseCustomHymnPopup()
    {
        IsCustomHymnPopupOpen = false;
        CustomLyricsTitle = string.Empty;
        CustomLyricsText = string.Empty;
    }

    [RelayCommand]
    private void ProjectCustomLyrics()
    {
        if (string.IsNullOrWhiteSpace(CustomLyricsText)) return;

        var title = string.IsNullOrWhiteSpace(CustomLyricsTitle) ? "Custom Hymn" : CustomLyricsTitle.Trim();
        var cleanedText = CleanPastedLyrics(CustomLyricsText);

        // Split into verses/stanzas by empty lines (2 or more newlines)
        var stanzas = System.Text.RegularExpressions.Regex.Split(cleanedText, @"(?:\r?\n){2,}")
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (stanzas.Count == 0) return;

        DetailedContentItems.Clear();
        for (int i = 0; i < stanzas.Count; i++)
        {
            var refStr = stanzas.Count > 1 ? $"{title} - Verse {i + 1}" : title;
            var item = new ChurchAI.Core.Models.ProjectedItem
            {
                Reference = refStr,
                Text = stanzas[i]
            };
            DetailedContentItems.Add(item);
            _projectionService.AddToQueue(item.Reference, item.Text);
        }

        // Project the first stanza immediately (without showing the window, updating the live monitor card)
        var firstItem = DetailedContentItems[0];
        _projectionService.ProjectVerse(firstItem.Reference, firstItem.Text, showWindow: false);

        // Close the popup and clear
        IsCustomHymnPopupOpen = false;
        CustomLyricsTitle = string.Empty;
        CustomLyricsText = string.Empty;
    }

    private string CleanPastedLyrics(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // Strip HTML tags
        string cleaned = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);

        // Normalize carriage returns
        cleaned = cleaned.Replace("\r\n", "\n").Replace("\r", "\n");

        // Split, trim lines, and join
        var lines = cleaned.Split('\n')
            .Select(line => line.Trim())
            .ToArray();

        return string.Join(Environment.NewLine, lines);
    }

    [RelayCommand]
    private async Task SelectTranslation(string abbreviation)
    {
        SelectedTranslationAbbreviation = abbreviation;

        if (ActiveDetectedVerse != null)
        {
            var match = AllTranslationsForPopup.FirstOrDefault(v => v.Book?.Translation?.Abbreviation.Equals(abbreviation, StringComparison.OrdinalIgnoreCase) == true);
            if (match != null)
            {
                var succeeding = await _bibleRepository.GetSucceedingVersesAsync(match.BookId, match.Chapter, match.VerseNumber, 50);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    SucceedingVerses.Clear();
                    if (succeeding != null)
                    {
                        foreach (var sv in succeeding)
                        {
                            SucceedingVerses.Add(sv);
                        }
                    }
                    ActiveDetectedVerse = match;
                });
            }
        }

        // Update active Preview Monitor if it contains a bible reference
        if (!string.IsNullOrWhiteSpace(PreviewHeader))
        {
            var previewRef = _referenceParser.Parse(PreviewHeader);
            if (previewRef != null)
            {
                int vNum = previewRef.Verse ?? 1;
                var verses = await _bibleRepository.GetVerseInAllTranslationsAsync(previewRef.BookName, previewRef.Chapter, vNum);
                if (verses != null && verses.Any())
                {
                    var match = verses.FirstOrDefault(v => v.Book?.Translation?.Abbreviation.Equals(abbreviation, StringComparison.OrdinalIgnoreCase) == true)
                                ?? verses.FirstOrDefault();
                    if (match != null)
                    {
                        PreviewText = match.Text;
                    }
                }
            }
        }



        // Update the spoken scripture detection screen verses to the new translation
        await ReloadDetectedScripturesInNewTranslationAsync(abbreviation);
        await ReloadDetailedItemsInNewTranslationAsync(abbreviation);
    }

    private async Task ReloadDetectedScripturesInNewTranslationAsync(string newTranslation)
    {
        try
        {
            var itemsCopy = DetectedSpeechScriptures.ToList();
            for (int i = 0; i < itemsCopy.Count; i++)
            {
                var item = itemsCopy[i];
                var reference = _referenceParser.Parse(item.Reference);
                if (reference != null)
                {
                    int verseNum = reference.Verse ?? 1;
                    var verses = await _bibleRepository.GetVerseInAllTranslationsAsync(reference.BookName, reference.Chapter, verseNum);
                    if (verses != null && verses.Any())
                    {
                        var match = verses.FirstOrDefault(v => v.Book?.Translation?.Abbreviation.Equals(newTranslation, StringComparison.OrdinalIgnoreCase) == true)
                                    ?? verses.FirstOrDefault();
                        if (match != null)
                        {
                            var updatedText = match.Text;
                            var idx = i;
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (idx < DetectedSpeechScriptures.Count)
                                {
                                    DetectedSpeechScriptures[idx] = new ChurchAI.Core.Models.ProjectedItem
                                    {
                                        Reference = item.Reference,
                                        Text = updatedText
                                    };
                                }
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error re-translating detected scriptures: {ex.Message}");
        }
    }

    private void SafeInvoke(Action action)
    {
        if (System.Windows.Application.Current == null)
        {
            action();
            return;
        }

        var dispatcher = System.Windows.Application.Current.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        bool isTest = AppDomain.CurrentDomain.GetAssemblies().Any(a => 
            a.FullName?.Contains("test", StringComparison.OrdinalIgnoreCase) == true || 
            a.FullName?.Contains("xunit", StringComparison.OrdinalIgnoreCase) == true);

        if (isTest)
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }

    public void Dispose()
    {
        // Unsubscribe from service events to prevent memory leaks
        _projectionService.CurrentlyProjectedItemChanged -= OnCurrentlyProjectedItemChanged;
        _speechRecognitionService.SpeechRecognized -= OnSpeechRecognized;
        _speechRecognitionService.AudioLevelUpdated -= OnAudioLevelUpdated;
        if (_communityMessageService != null)
        {
            _communityMessageService.MessageReceived -= OnCommunityMessageReceived;
        }
    }

    [ObservableProperty]
    private bool _isSavedLowerThirdsPopupOpen;

    [RelayCommand]
    private void ToggleSavedLowerThirdsPopup()
    {
        _ = LoadSavedLowerThirdsAsync();
        IsSavedLowerThirdsPopupOpen = !IsSavedLowerThirdsPopupOpen;
    }

    [RelayCommand]
    private void ProjectPresetAndClosePopup(ChurchAI.Core.Entities.LowerThirdItem? item)
    {
        IsSavedLowerThirdsPopupOpen = false;
        ProjectSavedLowerThird(item);
    }

    [RelayCommand]
    private void OpenLowerThirdModal()
    {
        IsLowerThirdModalOpen = true;
        _ = LoadSavedLowerThirdsAsync();
    }

    [RelayCommand]
    private void CloseLowerThirdModal()
    {
        IsLowerThirdModalOpen = false;
    }

    [RelayCommand]
    private void OpenSpeakerLowerThirdStudio()
    {
        var item = new ChurchAI.Core.Entities.LowerThirdItem
        {
            TagText = LowerThirdTagText,
            Title = LowerThirdTitle,
            Subtitle = LowerThirdSubtitle,
            TagBgColorHex = LowerThirdTagBgColor,
            TagTextColorHex = LowerThirdTagTextColor,
            TitleBgColorHex = LowerThirdTitleBgColor,
            TitleTextColorHex = LowerThirdTitleTextColor,
            SubtitleBgColorHex = LowerThirdSubBgColor,
            SubtitleTextColorHex = LowerThirdSubTextColor,
            StylePreset = SelectedLowerThirdStyle,
            Position = SelectedLowerThirdPosition
        };

        OpenSpeakerLowerThirdEditorWindow(item);
    }

    [RelayCommand]
    private void SelectLowerThirdStyle(string styleName)
    {
        if (Enum.TryParse<ChurchAI.Core.Entities.LowerThirdStyle>(styleName, true, out var style))
        {
            SelectedLowerThirdStyle = style;

            var item = new ChurchAI.Core.Entities.LowerThirdItem
            {
                TagText = LowerThirdTagText,
                Title = LowerThirdTitle,
                Subtitle = LowerThirdSubtitle,
                TagBgColorHex = LowerThirdTagBgColor,
                TagTextColorHex = LowerThirdTagTextColor,
                TitleBgColorHex = LowerThirdTitleBgColor,
                TitleTextColorHex = LowerThirdTitleTextColor,
                SubtitleBgColorHex = LowerThirdSubBgColor,
                SubtitleTextColorHex = LowerThirdSubTextColor,
                StylePreset = style,
                Position = SelectedLowerThirdPosition
            };

            OpenSpeakerLowerThirdEditorWindow(item);
        }
    }

    [RelayCommand]
    private void SelectLowerThirdPosition(string posName)
    {
        if (Enum.TryParse<ChurchAI.Core.Entities.LowerThirdPosition>(posName, true, out var pos))
        {
            SelectedLowerThirdPosition = pos;
            if (ActiveLowerThird != null)
            {
                SendLowerThirdLive();
            }
        }
    }

    [RelayCommand]
    private void SendLowerThirdLive()
    {
        var item = new ChurchAI.Core.Entities.LowerThirdItem
        {
            TagText = LowerThirdTagText,
            Title = LowerThirdTitle,
            Subtitle = LowerThirdSubtitle,
            TagBgColorHex = LowerThirdTagBgColor,
            TagTextColorHex = LowerThirdTagTextColor,
            TitleBgColorHex = LowerThirdTitleBgColor,
            TitleTextColorHex = LowerThirdTitleTextColor,
            SubtitleBgColorHex = LowerThirdSubBgColor,
            SubtitleTextColorHex = LowerThirdSubTextColor,
            StylePreset = SelectedLowerThirdStyle,
            Position = SelectedLowerThirdPosition
        };
        _projectionService.ProjectLowerThird(item);
        SendLowerThirdButtonText = "✓ Sent Live!";
    }

    [RelayCommand]
    private void HideLowerThird()
    {
        _projectionService.HideLowerThird();
        SendLowerThirdButtonText = "🚀 Send / Update Live";
    }

    [RelayCommand]
    private async Task SaveLowerThirdPresetAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(LowerThirdTitle)) return;

            var preset = new ChurchAI.Core.Entities.LowerThirdItem
            {
                Id = 0, // Always 0 for new preset insertion
                Name = string.IsNullOrWhiteSpace(LowerThirdTagText) ? LowerThirdTitle : $"{LowerThirdTagText} - {LowerThirdTitle}",
                TagText = LowerThirdTagText,
                Title = LowerThirdTitle,
                Subtitle = LowerThirdSubtitle,
                TagBgColorHex = LowerThirdTagBgColor,
                TagTextColorHex = LowerThirdTagTextColor,
                TitleBgColorHex = LowerThirdTitleBgColor,
                TitleTextColorHex = LowerThirdTitleTextColor,
                SubtitleBgColorHex = LowerThirdSubBgColor,
                SubtitleTextColorHex = LowerThirdSubTextColor,
                StylePreset = SelectedLowerThirdStyle,
                Position = SelectedLowerThirdPosition,
                IsPreset = true,
                DateCreated = DateTime.UtcNow
            };

            var repo = _lowerThirdRepository ?? Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<ILowerThirdRepository>(((App)System.Windows.Application.Current).Host.Services);

            if (repo != null)
            {
                await repo.AddLowerThirdAsync(preset);
                await LoadSavedLowerThirdsAsync();
                SaveLowerThirdButtonText = "✓ Saved!";
            }
        }
        catch
        {
            SaveLowerThirdButtonText = "❌ Save Failed";
        }
    }

    [RelayCommand]
    private void ProjectSavedLowerThird(ChurchAI.Core.Entities.LowerThirdItem? item)
    {
        if (item == null) return;

        LowerThirdTagText = item.TagText;
        LowerThirdTitle = item.Title;
        LowerThirdSubtitle = item.Subtitle;
        if (!string.IsNullOrEmpty(item.TagBgColorHex)) LowerThirdTagBgColor = item.TagBgColorHex;
        if (!string.IsNullOrEmpty(item.TagTextColorHex)) LowerThirdTagTextColor = item.TagTextColorHex;
        if (!string.IsNullOrEmpty(item.TitleBgColorHex)) LowerThirdTitleBgColor = item.TitleBgColorHex;
        if (!string.IsNullOrEmpty(item.TitleTextColorHex)) LowerThirdTitleTextColor = item.TitleTextColorHex;
        if (!string.IsNullOrEmpty(item.SubtitleBgColorHex)) LowerThirdSubBgColor = item.SubtitleBgColorHex;
        if (!string.IsNullOrEmpty(item.SubtitleTextColorHex)) LowerThirdSubTextColor = item.SubtitleTextColorHex;
        SelectedLowerThirdStyle = item.StylePreset;
        SelectedLowerThirdPosition = item.Position;

        // Project preacher lower third live to screen/NDI/stage!
        _projectionService.ProjectLowerThird(item);
        SendLowerThirdButtonText = "🟢 LIVE ON SCREEN";
    }

    [RelayCommand]
    private void EditSavedLowerThird(ChurchAI.Core.Entities.LowerThirdItem? item)
    {
        if (item == null) return;
        OpenSpeakerLowerThirdEditorWindow(item);
    }

    [RelayCommand]
    private void ProjectCustomSpeakerTemplate(CustomTemplate? template)
    {
        if (template == null) return;
        _projectionService.SetActiveCustomTemplate(template);
        SendLowerThirdButtonText = "🟢 LIVE ON SCREEN";
    }

    [RelayCommand]
    private async Task EditCustomSpeakerTemplateAsync(CustomTemplate? template)
    {
        if (template == null) return;
        var customService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ICustomTemplateService>(((App)System.Windows.Application.Current).Host.Services);
        var aiService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IAITemplateExtractorService>(((App)System.Windows.Application.Current).Host.Services);
        var settingsService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ISettingsService>(((App)System.Windows.Application.Current).Host.Services);

        var vm = new CustomTemplateBuilderViewModel(customService, aiService, settingsService);
        vm.LoadTemplate(template);

        var window = new CustomTemplateBuilderWindow { DataContext = vm };
        window.ShowDialog();

        await RefreshCustomTemplatesAsync();
    }

    [RelayCommand]
    private async Task DeleteCustomSpeakerTemplateAsync(CustomTemplate? template)
    {
        if (template == null) return;
        var customService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ICustomTemplateService>(((App)System.Windows.Application.Current).Host.Services);
        await customService.DeleteTemplateAsync(template.Id);
        await RefreshCustomTemplatesAsync();
    }

    public void OpenSpeakerLowerThirdEditorWindow(ChurchAI.Core.Entities.LowerThirdItem item)
    {
        var vm = new SpeakerLowerThirdEditorViewModel(_projectionService, item, _lowerThirdRepository);
        var win = new Views.SpeakerLowerThirdEditorWindow { DataContext = vm };
        win.ShowDialog();
        _ = LoadSavedLowerThirdsAsync();
    }

    [RelayCommand]
    private async Task DeleteSavedLowerThirdAsync(ChurchAI.Core.Entities.LowerThirdItem? item)
    {
        if (item == null) return;
        if (_lowerThirdRepository != null)
        {
            await _lowerThirdRepository.DeleteLowerThirdAsync(item.Id);
            await LoadSavedLowerThirdsAsync();
        }
    }

    private async Task LoadSavedLowerThirdsAsync()
    {
        if (_lowerThirdRepository == null) return;

        var items = await _lowerThirdRepository.GetAllLowerThirdsAsync();
        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
        {
            SavedLowerThirds.Clear();
            foreach (var item in items)
            {
                SavedLowerThirds.Add(item);
            }
        });
    }
}

public class ScriptureTemplateOption
{
    public ChurchAI.Core.Entities.ScriptureLowerThirdTemplate Template { get; set; }
    public ChurchAI.Core.Entities.CustomTemplate? CustomTemplate { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public partial class LiveLineItem : ObservableObject
{
    public string LineText { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}

