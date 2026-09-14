using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ChurchAI.App.Services.Interfaces;
using ChurchAI.Core.Entities;
using ChurchAI.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ChurchAI.App.ViewModels;

public partial class SpeakerLowerThirdEditorViewModel : ViewModelBase
{
    private readonly IProjectionService _projectionService;
    private readonly ILowerThirdRepository? _lowerThirdRepository;

    public Action? RequestClose { get; set; }

    [ObservableProperty]
    private LowerThirdItem _item;

    [ObservableProperty]
    private string _tagText = "SPEAKER";

    [ObservableProperty]
    private string _title = "Pastor John Doe";

    [ObservableProperty]
    private string _subtitle = "Lead Pastor - Service Theme";

    [ObservableProperty]
    private string _tagBgColor = "#0038A8";

    [ObservableProperty]
    private string _tagTextColor = "#FFFFFF";

    [ObservableProperty]
    private string _titleBgColor = "#F1F5F9";

    [ObservableProperty]
    private string _titleTextColor = "#0F172A";

    [ObservableProperty]
    private string _subBgColor = "#0284C7";

    [ObservableProperty]
    private string _subTextColor = "#FFFFFF";

    [ObservableProperty]
    private string _selectedElementKey = "TitleBg";

    [ObservableProperty]
    private string _selectedElementName = "Preacher Name Container Background";

    [ObservableProperty]
    private string _activeColorHex = "#F1F5F9";

    [ObservableProperty]
    private string _statusMessage = "Click any container or text element on the lower third to change its color";

    public ObservableCollection<string> ColorSwatches { get; } = new()
    {
        "#0038A8", // Royal Blue
        "#DC2626", // Crimson Red
        "#10B981", // Emerald Green
        "#F59E0B", // Amber Gold
        "#8B5CF6", // Cyberpunk Purple
        "#0284C7", // Cyan Sky
        "#1E293B", // Dark Slate
        "#0F172A", // Midnight Black
        "#FFFFFF", // Pure White
        "#00E5FF", // Neon Cyan
        "#F1F5F9", // Crisp Off-White
        "#94A3B8"  // Muted Gray
    };

    [ObservableProperty]
    private LowerThirdStyle _selectedStylePreset = LowerThirdStyle.BlueAngular;

    [RelayCommand]
    public void SelectStylePreset(string styleStr)
    {
        if (Enum.TryParse<LowerThirdStyle>(styleStr, true, out var style))
        {
            SelectedStylePreset = style;
            Item.StylePreset = style;
            
            switch (style)
            {
                case LowerThirdStyle.BlueAngular:
                    TagBgColor = "#0038A8"; TagTextColor = "#FFFFFF";
                    TitleBgColor = "#F1F5F9"; TitleTextColor = "#0F172A";
                    SubBgColor = "#0284C7"; SubTextColor = "#FFFFFF";
                    break;
                case LowerThirdStyle.GoldBadge:
                    TagBgColor = "#F59E0B"; TagTextColor = "#0F172A";
                    TitleBgColor = "#1E293B"; TitleTextColor = "#FFFFFF";
                    SubBgColor = "#F59E0B"; SubTextColor = "#F59E0B";
                    break;
                case LowerThirdStyle.CyberpunkPurple:
                    TagBgColor = "#4C1D95"; TagTextColor = "#00E5FF";
                    TitleBgColor = "#5B21B6"; TitleTextColor = "#FFFFFF";
                    SubBgColor = "#0284C7"; SubTextColor = "#FFFFFF";
                    break;
                case LowerThirdStyle.ClassicNavy:
                    TagBgColor = "#00E5FF"; TagTextColor = "#00E5FF";
                    TitleBgColor = "#E60F172A"; TitleTextColor = "#FFFFFF";
                    SubBgColor = "#94A3B8"; SubTextColor = "#94A3B8";
                    break;
            }
        }
    }

    public SpeakerLowerThirdEditorViewModel(
        IProjectionService projectionService,
        LowerThirdItem item,
        ILowerThirdRepository? lowerThirdRepository = null)
    {
        _projectionService = projectionService;
        _lowerThirdRepository = lowerThirdRepository;
        _item = item;
        SelectedStylePreset = item.StylePreset;

        TagText = string.IsNullOrWhiteSpace(item.TagText) ? "SPEAKER" : item.TagText;
        Title = string.IsNullOrWhiteSpace(item.Title) ? "Pastor John Doe" : item.Title;
        Subtitle = string.IsNullOrWhiteSpace(item.Subtitle) ? "Lead Pastor - Service Theme" : item.Subtitle;

        TagBgColor = string.IsNullOrWhiteSpace(item.TagBgColorHex) ? "#0038A8" : item.TagBgColorHex;
        TagTextColor = string.IsNullOrWhiteSpace(item.TagTextColorHex) ? "#FFFFFF" : item.TagTextColorHex;
        TitleBgColor = string.IsNullOrWhiteSpace(item.TitleBgColorHex) ? "#F1F5F9" : item.TitleBgColorHex;
        TitleTextColor = string.IsNullOrWhiteSpace(item.TitleTextColorHex) ? "#0F172A" : item.TitleTextColorHex;
        SubBgColor = string.IsNullOrWhiteSpace(item.SubtitleBgColorHex) ? "#0284C7" : item.SubtitleBgColorHex;
        SubTextColor = string.IsNullOrWhiteSpace(item.SubtitleTextColorHex) ? "#FFFFFF" : item.SubtitleTextColorHex;

        SelectElement("TitleBg");
    }

    [RelayCommand]
    public void SelectElement(string elementKey)
    {
        SelectedElementKey = elementKey;
        switch (elementKey)
        {
            case "TagBg":
                SelectedElementName = "🏷️ Tag Container Background";
                ActiveColorHex = TagBgColor;
                break;
            case "TagText":
                SelectedElementName = "🏷️ Tag Text Color";
                ActiveColorHex = TagTextColor;
                break;
            case "TitleBg":
                SelectedElementName = "👤 Preacher Name Container Background";
                ActiveColorHex = TitleBgColor;
                break;
            case "TitleText":
                SelectedElementName = "👤 Preacher Name Text Color";
                ActiveColorHex = TitleTextColor;
                break;
            case "SubtitleBg":
                SelectedElementName = "📜 Subtitle Container Background";
                ActiveColorHex = SubBgColor;
                break;
            case "SubtitleText":
                SelectedElementName = "📜 Subtitle Text Color";
                ActiveColorHex = SubTextColor;
                break;
        }
        StatusMessage = $"Selected: {SelectedElementName}. Pick a color from palette or type hex code.";
    }

    [RelayCommand]
    public void SelectSwatchColor(string hexColor)
    {
        ActiveColorHex = hexColor;
        ApplyActiveColor();
    }

    partial void OnActiveColorHexChanged(string value)
    {
        ApplyActiveColor();
    }

    private void ApplyActiveColor()
    {
        if (string.IsNullOrWhiteSpace(ActiveColorHex)) return;

        switch (SelectedElementKey)
        {
            case "TagBg":
                TagBgColor = ActiveColorHex;
                break;
            case "TagText":
                TagTextColor = ActiveColorHex;
                break;
            case "TitleBg":
                TitleBgColor = ActiveColorHex;
                break;
            case "TitleText":
                TitleTextColor = ActiveColorHex;
                break;
            case "SubtitleBg":
                SubBgColor = ActiveColorHex;
                break;
            case "SubtitleText":
                SubTextColor = ActiveColorHex;
                break;
        }

        UpdateItemState();
    }

    partial void OnTagTextChanged(string value) => UpdateItemState();
    partial void OnTitleChanged(string value) => UpdateItemState();
    partial void OnSubtitleChanged(string value) => UpdateItemState();
    partial void OnTagBgColorChanged(string value) => UpdateItemState();
    partial void OnTagTextColorChanged(string value) => UpdateItemState();
    partial void OnTitleBgColorChanged(string value) => UpdateItemState();
    partial void OnTitleTextColorChanged(string value) => UpdateItemState();
    partial void OnSubBgColorChanged(string value) => UpdateItemState();
    partial void OnSubTextColorChanged(string value) => UpdateItemState();

    private void UpdateItemState()
    {
        Item.StylePreset = SelectedStylePreset;
        Item.TagText = TagText;
        Item.Title = Title;
        Item.Subtitle = Subtitle;
        Item.TagBgColorHex = TagBgColor;
        Item.TagTextColorHex = TagTextColor;
        Item.TitleBgColorHex = TitleBgColor;
        Item.TitleTextColorHex = TitleTextColor;
        Item.SubtitleBgColorHex = SubBgColor;
        Item.SubtitleTextColorHex = SubTextColor;
    }

    [RelayCommand]
    public void SendLive()
    {
        UpdateItemState();
        _projectionService.ProjectLowerThird(Item);
        StatusMessage = "🚀 Preacher lower third sent live to projector, Full View stage & NDI stream!";
    }

    [RelayCommand]
    public async Task SavePresetAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                StatusMessage = "⚠️ Please enter a preacher name before saving.";
                return;
            }

            UpdateItemState();

            var repo = _lowerThirdRepository ?? Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<ILowerThirdRepository>(((App)System.Windows.Application.Current).Host.Services);

            if (repo != null)
            {
                var newItem = new LowerThirdItem
                {
                    Id = 0, // Always 0 for new preset insertion
                    Name = string.IsNullOrWhiteSpace(TagText) ? Title : $"{TagText} - {Title}",
                    TagText = TagText,
                    Title = Title,
                    Subtitle = Subtitle,
                    TagBgColorHex = TagBgColor,
                    TagTextColorHex = TagTextColor,
                    TitleBgColorHex = TitleBgColor,
                    TitleTextColorHex = TitleTextColor,
                    SubtitleBgColorHex = SubBgColor,
                    SubtitleTextColorHex = SubTextColor,
                    StylePreset = SelectedStylePreset,
                    Position = Item.Position,
                    IsPreset = true,
                    DateCreated = DateTime.UtcNow
                };

                await repo.AddLowerThirdAsync(newItem);
                StatusMessage = $"💾 Saved preset '{newItem.Name}' to library!";
            }
            else
            {
                StatusMessage = "❌ Database repository service unavailable.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Error saving preset: {ex.Message}";
        }
    }
}
