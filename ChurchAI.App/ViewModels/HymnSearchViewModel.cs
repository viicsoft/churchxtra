using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChurchAI.App.ViewModels;

public partial class HymnSearchViewModel : ViewModelBase
{
    private readonly IHymnRepository _hymnRepository;
    private readonly ISettingsService _settingsService;
    private readonly IProjectionService _projectionService;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        _ = SearchAsync();
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasNoResults;

    [ObservableProperty]
    private Hymn? _selectedResult;

    [ObservableProperty]
    private bool _hasMultipleHymnBooks;

    [ObservableProperty]
    private string _selectedHymnBook = "Default";

    public ObservableCollection<Hymn> SearchResults { get; } = new();
    public ObservableCollection<HymnBookTabViewModel> HymnBookTabs { get; } = new();

    public HymnSearchViewModel(IHymnRepository hymnRepository, ISettingsService settingsService, IProjectionService projectionService)
    {
        _hymnRepository = hymnRepository;
        _settingsService = settingsService;
        _projectionService = projectionService;

        if (!string.IsNullOrEmpty(_settingsService.ActiveHymnBook))
        {
            _selectedHymnBook = _settingsService.ActiveHymnBook;
        }

        _ = LoadAvailableHymnBooksAsync();
    }

    public async Task LoadAvailableHymnBooksAsync()
    {
        try
        {
            var books = await _hymnRepository.GetAvailableHymnBooksAsync();
            if (books == null || books.Count == 0)
            {
                books = new System.Collections.Generic.List<string> { "Default" };
            }

            HasMultipleHymnBooks = books.Count > 1;

            if (!books.Contains(SelectedHymnBook, StringComparer.OrdinalIgnoreCase))
            {
                SelectedHymnBook = books[0];
            }

            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                HymnBookTabs.Clear();
                foreach (var b in books)
                {
                    HymnBookTabs.Add(new HymnBookTabViewModel
                    {
                        Name = b,
                        IsActive = string.Equals(b, SelectedHymnBook, StringComparison.OrdinalIgnoreCase)
                    });
                }
            });

            await SearchAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadAvailableHymnBooks error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SelectHymnBookAsync(string bookName)
    {
        if (string.IsNullOrWhiteSpace(bookName)) return;

        SelectedHymnBook = bookName;
        _settingsService.ActiveHymnBook = bookName;

        foreach (var tab in HymnBookTabs)
        {
            tab.IsActive = string.Equals(tab.Name, bookName, StringComparison.OrdinalIgnoreCase);
        }

        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        IsLoading = true;
        HasNoResults = false;
        SearchResults.Clear();
        SelectedResult = null;

        var query = SearchQuery.Trim();
        var activeHymnBook = SelectedHymnBook;

        try
        {
            var allHymns = await _hymnRepository.GetAllHymnsAsync(activeHymnBook);

            if (string.IsNullOrEmpty(query))
            {
                foreach (var h in allHymns.OrderBy(h => h.Number))
                {
                    SearchResults.Add(h);
                }
            }
            else
            {
                bool isPureNumber = int.TryParse(query, out int exactNumber);

                var matched = allHymns.Where(h =>
                    (isPureNumber && h.Number == exactNumber) ||
                    h.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    h.Lyrics.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(h => (isPureNumber && h.Number == exactNumber) ? 0 :
                                  h.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 1 :
                                  h.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ? 2 : 3)
                    .ThenBy(h => h.Number)
                    .ToList();

                foreach (var h in matched)
                {
                    SearchResults.Add(h);
                }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Hymn search error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectFirstResult()
    {
        if (SearchResults.Count > 0)
        {
            SelectHymn(SearchResults[0]);
        }
    }

    [RelayCommand]
    private void SelectHymn(Hymn? hymn)
    {
        if (hymn != null)
        {
            SelectedResult = null;
            SelectedResult = hymn;
        }
    }

    [RelayCommand]
    private void ProjectHymn(Hymn? hymn)
    {
        var target = hymn ?? SelectedResult;
        if (target != null)
        {
            SelectedResult = target;
            var reference = $"Hymn {target.Number}: {target.Title}";
            _projectionService.AddToQueue(reference, target.Lyrics);
            _projectionService.ProjectVerse(reference, target.Lyrics);
        }
    }
}
