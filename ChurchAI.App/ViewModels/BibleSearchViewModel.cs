using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChurchAI.App.ViewModels;

public partial class BibleSearchViewModel : ViewModelBase
{
    private readonly IBibleRepository _bibleRepository;
    private readonly ISearchHistoryService _searchHistoryService;
    private readonly IProjectionService _projectionService;
    private readonly IBibleReferenceParser _referenceParser;
    private const int DefaultTranslationId = 1; // E.g. KJV

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    private System.Threading.CancellationTokenSource? _searchCts;

    partial void OnSearchQueryChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new System.Threading.CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(200, token);
                if (token.IsCancellationRequested) return;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (token.IsCancellationRequested) return;
                    await ExecuteSearchInternalAsync(value, isRealTime: true);
                });
            }
            catch (TaskCanceledException) { }
        });
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasNoResults;

    [ObservableProperty]
    private Verse? _selectedResult;

    public ObservableCollection<Verse> SearchResults { get; } = new();
    public ObservableCollection<string> SearchHistory { get; } = new();

    public BibleSearchViewModel(IBibleRepository bibleRepository, ISearchHistoryService searchHistoryService, IProjectionService projectionService, IBibleReferenceParser referenceParser)
    {
        _bibleRepository = bibleRepository;
        _searchHistoryService = searchHistoryService;
        _projectionService = projectionService;
        _referenceParser = referenceParser;
        
        LoadHistoryAsync();
    }

    private async void LoadHistoryAsync()
    {
        var history = await _searchHistoryService.GetSearchHistoryAsync();
        foreach (var item in history)
        {
            SearchHistory.Add(item);
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await ExecuteSearchInternalAsync(SearchQuery, isRealTime: false);
    }

    [RelayCommand]
    private void SelectFirstResult()
    {
        if (SearchResults.Count > 0)
        {
            SelectVerse(SearchResults[0]);
        }
    }

    private async Task ExecuteSearchInternalAsync(string rawQuery, bool isRealTime)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            SearchResults.Clear();
            HasNoResults = false;
            return;
        }

        IsLoading = true;
        HasNoResults = false;
        SearchResults.Clear();
        SelectedResult = null;

        var query = rawQuery.Trim();
        
        if (!isRealTime)
        {
            // Update history only on explicit submit/enter
            await _searchHistoryService.AddSearchAsync(query);
            if (!SearchHistory.Contains(query))
            {
                SearchHistory.Insert(0, query);
            }
            else
            {
                SearchHistory.Remove(query);
                SearchHistory.Insert(0, query);
            }
        }

        try
        {
            // Parse query using our custom natural language parser
            var reference = _referenceParser.Parse(query);

            if (reference != null)
            {
                var bookName = reference.BookName;
                var chapter = reference.Chapter;
                var verseNum = reference.Verse;

                if (verseNum.HasValue)
                {
                    var verses = await _bibleRepository.GetVerseInAllTranslationsAsync(bookName, chapter, verseNum.Value);
                    foreach (var v in verses) SearchResults.Add(v);
                }
                else
                {
                    var verses = await _bibleRepository.GetChapterAsync(DefaultTranslationId, bookName, chapter);
                    foreach (var v in verses) SearchResults.Add(v);
                }
            }
            else
            {
                // Fallback to text search across all translations
                var verses = await _bibleRepository.SearchAllTranslationsAsync(query);
                foreach (var v in verses) SearchResults.Add(v);
            }

            if (SearchResults.Count == 0)
            {
                HasNoResults = true;
            }
            else
            {
                SelectedResult = SearchResults[0];
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectHistory(string historyQuery)
    {
        SearchQuery = historyQuery;
        SearchCommand.Execute(null);
    }

    [RelayCommand]
    private void SelectVerse(Verse? verse)
    {
        if (verse != null)
        {
            SelectedResult = null;
            SelectedResult = verse;
        }
    }

    [RelayCommand]
    private void ProjectVerse(Verse? verse)
    {
        var target = verse ?? SelectedResult;
        if (target != null)
        {
            SelectedResult = target;
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
    private void Clear()
    {
        _projectionService.ClearScreen();
    }
}
