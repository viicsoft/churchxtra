using System.Collections.ObjectModel;
using ChurchAI.Core.Models;

namespace ChurchAI.App.Services.Interfaces;

public interface IProjectionService
{
    void ShowProjection();
    void HideProjection();
    void ProjectVerse(string reference, string text, bool showWindow = false);
    void ClearScreen();
    void ShowLogo();
    void SetLowerThirds(bool isLowerThirds);
    void SetScriptureLowerThirdTemplate(ChurchAI.Core.Entities.ScriptureLowerThirdTemplate template);
    void SetTextAlignment(string alignment);
    ChurchAI.Core.Entities.ScriptureLowerThirdTemplate ActiveScriptureTemplate { get; }
    void SetActiveCustomTemplate(ChurchAI.Core.Entities.CustomTemplate? template);
    ChurchAI.Core.Entities.CustomTemplate? ActiveCustomTemplate { get; }
    void SetBackgroundMedia(ChurchAI.Core.Entities.MediaItem? mediaItem);
    void SetBackgroundColorHex(string hexColor);
    void ProjectStandaloneMedia(ChurchAI.Core.Entities.MediaItem mediaItem);
    
    void ProjectLowerThird(ChurchAI.Core.Entities.LowerThirdItem lowerThird);
    void UpdateActiveLowerThirdText(string title, string subtitle, string tagText);
    void UpdateActiveLowerThirdColors(string tagBg, string tagTxt, string titleBg, string titleTxt, string subBg, string subTxt);
    void HideLowerThird();

    ChurchAI.Core.Entities.LowerThirdItem? ActiveLowerThird { get; }
    event EventHandler<ChurchAI.Core.Entities.LowerThirdItem?>? ActiveLowerThirdChanged;

    object? CurrentProjectionWindow { get; }
    event EventHandler? ProjectionWindowChanged;

    ProjectedItem? CurrentlyProjectedItem { get; }
    event EventHandler? CurrentlyProjectedItemChanged;
    event EventHandler<ProjectedItem>? DeselectedLinesPushed;
    event EventHandler? NextItemRequested;
    
    ObservableCollection<ProjectedItem> ProjectionQueue { get; }
    void AddToQueue(string reference, string text);
    void RemoveFromQueue(ProjectedItem item);
    void SplitQueueItem(ProjectedItem item, string firstPart, string secondPart);
    void ProjectNext();
    void ProjectPrevious();
}
