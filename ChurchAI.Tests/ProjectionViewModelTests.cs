using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.App.ViewModels;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using ChurchAI.Core.Models;
using ChurchAI.Core.Parsers;
using ChurchAI.Infrastructure.Data;
using ChurchAI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ChurchAI.Tests;

public class ProjectionViewModelTests : IDisposable
{
    private readonly Application _app;

    public ProjectionViewModelTests()
    {
        // Set up WPF Application context for Dispatcher.Invoke calls
        if (Application.Current == null)
        {
            _app = new Application();
        }
        else
        {
            _app = Application.Current;
        }
    }

    public void Dispose()
    {
        // Cleanup application context
    }

    [Fact]
    public async Task OnSpeechRecognized_ValidReference_PopulatesActiveDetectedVerse()
    {
        // 1. Arrange
        var optionsBuilder = new DbContextOptionsBuilder<BibleDbContext>();
        string dbPath = @"C:\Users\hp\AppData\Local\ChurchAI\churchai_bible.db";
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        using var context = new BibleDbContext(optionsBuilder.Options);
        var bibleRepository = new BibleRepository(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseSeeder>.Instance);

        var projectionService = new StubProjectionService();
        var parser = new SpokenReferenceParser();
        var speechService = new StubSpeechRecognitionService();
        var aiIntentService = new StubAIIntentService();
        var hymnRepository = new StubHymnRepository();

        var viewModel = new ProjectionViewModel(
            projectionService,
            bibleRepository,
            parser,
            speechService,
            aiIntentService,
            hymnRepository
        );

        // Set default translation to KJV
        viewModel.SelectedTranslationAbbreviation = "KJV";

        // 2. Act - Simulate Whisper transcribing John chapter 3 verse 16. (final result)
        speechService.RaiseSpeechRecognized("John chapter 3 verse 16.", true);

        // Since speech handler is async, let's wait a moment for the background Task to complete
        int retries = 50;
        while (retries-- > 0 && !viewModel.AllTranslationsForPopup.Any())
        {
            await Task.Delay(100);
        }

        // 3. Assert
        Assert.NotEmpty(viewModel.AllTranslationsForPopup);
        Assert.NotNull(viewModel.ActiveDetectedVerse);
        Assert.Equal("John", viewModel.ActiveDetectedVerse.Book?.Name);
        Assert.Equal(3, viewModel.ActiveDetectedVerse.Chapter);
        Assert.Equal(16, viewModel.ActiveDetectedVerse.VerseNumber);
        Assert.Equal("KJV", viewModel.ActiveDetectedVerse.Book?.Translation?.Abbreviation);

        // Wait for succeeding verses task to complete
        int retries2 = 50;
        while (retries2-- > 0 && !viewModel.SucceedingVerses.Any())
        {
            await Task.Delay(100);
        }

        Assert.NotEmpty(viewModel.SucceedingVerses);
        Assert.Equal(3, viewModel.SucceedingVerses.First().Chapter);
        Assert.Equal(17, viewModel.SucceedingVerses.First().VerseNumber);

        // Verify prefill of search reference
        Assert.Equal("John 3:16", viewModel.SucceedingSearchReference);

        // Simulate user changing reference to John 3:15 and submitting
        viewModel.SucceedingSearchReference = "John 3:15";
        await viewModel.ProjectSucceedingSearchCommand.ExecuteAsync(null);

        // Assert ActiveDetectedVerse is now John 3:15
        Assert.Equal("John", viewModel.ActiveDetectedVerse.Book?.Name);
        Assert.Equal(3, viewModel.ActiveDetectedVerse.Chapter);
        Assert.Equal(15, viewModel.ActiveDetectedVerse.VerseNumber);
        Assert.Equal("John 3:15", viewModel.SucceedingSearchReference);

        // Simulate user clicking on succeeding verse John 3:16 via the command
        var nextVerse = viewModel.SucceedingVerses.First(v => v.VerseNumber == 16);
        await viewModel.SelectSucceedingVerseCommand.ExecuteAsync(nextVerse);

        // Assert ActiveDetectedVerse is now John 3:16
        Assert.Equal("John", viewModel.ActiveDetectedVerse.Book?.Name);
        Assert.Equal(3, viewModel.ActiveDetectedVerse.Chapter);
        Assert.Equal(16, viewModel.ActiveDetectedVerse.VerseNumber);
        Assert.Equal("John 3:16", viewModel.SucceedingSearchReference);
    }

    [Fact]
    public async Task DetailedContentItems_And_LiveNavigation_CorrectlyOperates()
    {
        var optionsBuilder = new DbContextOptionsBuilder<BibleDbContext>();
        string dbPath = @"C:\Users\hp\AppData\Local\ChurchAI\churchai_bible.db";
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        using var context = new BibleDbContext(optionsBuilder.Options);
        var bibleRepository = new BibleRepository(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseSeeder>.Instance);

        var projectionService = new StubProjectionService();
        var parser = new SpokenReferenceParser();
        var speechService = new StubSpeechRecognitionService();
        var aiIntentService = new StubAIIntentService();
        var hymnRepository = new StubHymnRepository();

        var viewModel = new ProjectionViewModel(
            projectionService,
            bibleRepository,
            parser,
            speechService,
            aiIntentService,
            hymnRepository
        );

        viewModel.SelectedTranslationAbbreviation = "KJV";

        var inputItem = new ProjectedItem
        {
            Reference = "John 3:16",
            Text = "For God so loved..."
        };

        await viewModel.PreviewSpeechItemCommand.ExecuteAsync(inputItem);

        Assert.NotEmpty(viewModel.DetailedContentItems);
        var firstDetailed = viewModel.DetailedContentItems.First();
        Assert.Equal("John 3:16", firstDetailed.Reference);

        viewModel.ProjectSpeechItemCommand.Execute(firstDetailed);
        Assert.Equal("John 3:16", projectionService.CurrentlyProjectedItem?.Reference);

        viewModel.MoveLiveProjectionDownCommand.Execute(null);
        Assert.Equal("John 3:17", projectionService.CurrentlyProjectedItem?.Reference);

        viewModel.MoveLiveProjectionUpCommand.Execute(null);
        Assert.Equal("John 3:16", projectionService.CurrentlyProjectedItem?.Reference);
    }

    [Fact]
    public async Task SearchSelections_And_NavigationWindowBehavior_CorrectlyOperates()
    {
        var optionsBuilder = new DbContextOptionsBuilder<BibleDbContext>();
        string dbPath = @"C:\Users\hp\AppData\Local\ChurchAI\churchai_bible.db";
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        using var context = new BibleDbContext(optionsBuilder.Options);
        var bibleRepository = new BibleRepository(context, Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseSeeder>.Instance);

        var projectionService = new StubProjectionService();
        var parser = new SpokenReferenceParser();
        var speechService = new StubSpeechRecognitionService();
        var aiIntentService = new StubAIIntentService();
        var hymnRepository = new StubHymnRepository();

        var searchHistoryService = new StubSearchHistoryService();
        var bibleSearch = new BibleSearchViewModel(bibleRepository, searchHistoryService, projectionService, parser);

        var viewModel = new ProjectionViewModel(
            projectionService,
            bibleRepository,
            parser,
            speechService,
            aiIntentService,
            hymnRepository,
            settingsService: null,
            bibleSearch: bibleSearch
        );

        viewModel.SelectedTranslationAbbreviation = "KJV";

        // Create a mock verse
        var book = new Book { Name = "John", Translation = new Translation { Abbreviation = "KJV" } };
        var verse = new Verse { Book = book, BookId = 43, Chapter = 3, VerseNumber = 16, Text = "For God so loved the world..." };

        // Act - click the verse
        viewModel.BibleSearch.SelectVerseCommand.Execute(verse);

        // Since loading is async, let's wait a moment for the background Task to complete
        int retries = 50;
        while (retries-- > 0 && !viewModel.DetailedContentItems.Any())
        {
            await Task.Delay(100);
        }

        // Assert - detailed items should be loaded
        Assert.NotEmpty(viewModel.DetailedContentItems);
        var firstDetailed = viewModel.DetailedContentItems.First();
        Assert.Equal("John 3:16", firstDetailed.Reference);

        // Project the item live
        viewModel.ProjectSpeechItemCommand.Execute(firstDetailed);
        Assert.Equal("John 3:16", projectionService.CurrentlyProjectedItem?.Reference);

        // Move down - should update target verse, but showWindow should be false
        projectionService.LastProjectVerseShowWindow = null;
        viewModel.MoveLiveProjectionDownCommand.Execute(null);
        Assert.Equal("John 3:17", projectionService.CurrentlyProjectedItem?.Reference);
        Assert.False(projectionService.LastProjectVerseShowWindow);
    }

    private class StubProjectionService : IProjectionService
    {
        public ProjectedItem? CurrentlyProjectedItem { get; set; }
        public event EventHandler? CurrentlyProjectedItemChanged;
        public event EventHandler<ProjectedItem>? DeselectedLinesPushed;
        public event EventHandler? NextItemRequested;
        public ObservableCollection<ProjectedItem> ProjectionQueue { get; } = new();
        public bool? LastProjectVerseShowWindow { get; set; }

        public void ShowProjection() {}
        public void HideProjection() {}
        public void ProjectVerse(string reference, string text, bool showWindow = false)
        {
            CurrentlyProjectedItem = new ProjectedItem { Reference = reference, Text = text };
            CurrentlyProjectedItemChanged?.Invoke(this, EventArgs.Empty);
            LastProjectVerseShowWindow = showWindow;
        }
        public void ClearScreen() {}
        public void ShowLogo() {}
        public void SetLowerThirds(bool isLowerThirds) {}
        public void SetScriptureLowerThirdTemplate(ChurchAI.Core.Entities.ScriptureLowerThirdTemplate template) { ActiveScriptureTemplate = template; }
        public void SetTextAlignment(string alignment) {}
        public ChurchAI.Core.Entities.ScriptureLowerThirdTemplate ActiveScriptureTemplate { get; set; } = ChurchAI.Core.Entities.ScriptureLowerThirdTemplate.PillCapsuleSand;
        public void SetActiveCustomTemplate(ChurchAI.Core.Entities.CustomTemplate? template) { ActiveCustomTemplate = template; }
        public ChurchAI.Core.Entities.CustomTemplate? ActiveCustomTemplate { get; set; }
        public ChurchAI.Core.Entities.LowerThirdItem? ActiveLowerThird { get; set; }
        public event EventHandler<ChurchAI.Core.Entities.LowerThirdItem?>? ActiveLowerThirdChanged;
        public object? CurrentProjectionWindow { get; set; }
        public event EventHandler? ProjectionWindowChanged;

        public void ProjectLowerThird(ChurchAI.Core.Entities.LowerThirdItem lowerThird) {}
        public void UpdateActiveLowerThirdText(string title, string subtitle, string tagText) {}
        public void UpdateActiveLowerThirdColors(string tagBg, string tagTxt, string titleBg, string titleTxt, string subBg, string subTxt) {}
        public void HideLowerThird() {}
        public void SetBackgroundMedia(ChurchAI.Core.Entities.MediaItem? mediaItem) {}
        public void SetBackgroundColorHex(string hexColor) {}
        public void ProjectStandaloneMedia(ChurchAI.Core.Entities.MediaItem mediaItem) {}
        public void AddToQueue(string reference, string text) {}
        public void RemoveFromQueue(ProjectedItem item) {}
        public void SplitQueueItem(ProjectedItem item, string firstPart, string secondPart) {}
        public void ProjectNext() {}
        public void ProjectPrevious() {}
    }

    private class StubSpeechRecognitionService : ISpeechRecognitionService
    {
        public event EventHandler<SpeechRecognizedEventArgs>? SpeechRecognized;
        public event EventHandler<AudioLevelUpdatedEventArgs>? AudioLevelUpdated;

        public bool IsListening { get; set; } = true;

        public Task StartListeningAsync() => Task.CompletedTask;
        public Task StopListeningAsync() => Task.CompletedTask;

        public void RaiseSpeechRecognized(string text, bool isFinal)
        {
            SpeechRecognized?.Invoke(this, new SpeechRecognizedEventArgs(text, isFinal));
        }
    }

    private class StubAIIntentService : IAIIntentService
    {
        public Task<string?> ExtractBibleReferenceIntentAsync(string spokenText)
        {
            return Task.FromResult<string?>(null);
        }
    }

    private class StubHymnRepository : IHymnRepository
    {
        public Task<Hymn?> GetHymnByNumberAsync(int number, string? bookName = null) => Task.FromResult<Hymn?>(null);
        public Task<Hymn?> MatchHymnByLyricsAsync(string spokenText, string? bookName = null) => Task.FromResult<Hymn?>(null);
        public Task<List<Hymn>> GetAllHymnsAsync(string? bookName = null) => Task.FromResult(new List<Hymn>());
        public Task<List<string>> GetAvailableHymnBooksAsync() => Task.FromResult(new List<string> { "Default" });
        public Task SaveHymnBookAsync(string bookName, List<Hymn> hymns) => Task.CompletedTask;
    }

    private class StubSearchHistoryService : ChurchAI.App.Services.Interfaces.ISearchHistoryService
    {
        public Task<IEnumerable<string>> GetSearchHistoryAsync() => Task.FromResult<IEnumerable<string>>(new List<string>());
        public Task AddSearchAsync(string query) => Task.CompletedTask;
        public Task ClearHistoryAsync() => Task.CompletedTask;
    }
}
