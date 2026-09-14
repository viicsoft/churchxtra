using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChurchAI.App.ViewModels;
using ChurchAI.Core.Entities;

namespace ChurchAI.App.Views;

public partial class SpeakerLowerThirdEditorWindow : Window
{
    private SpeakerLowerThirdEditorViewModel? ViewModel => DataContext as SpeakerLowerThirdEditorViewModel;

    public SpeakerLowerThirdEditorWindow()
    {
        InitializeComponent();
        DataContextChanged += SpeakerLowerThirdEditorWindow_DataContextChanged;
    }

    private void SpeakerLowerThirdEditorWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is SpeakerLowerThirdEditorViewModel oldVm)
        {
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        }

        if (e.NewValue is SpeakerLowerThirdEditorViewModel newVm)
        {
            newVm.PropertyChanged += ViewModel_PropertyChanged;
            newVm.RequestClose = Close;
            UpdatePreviewGraphics();
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdatePreviewGraphics();
    }

    private void UpdatePreviewGraphics()
    {
        if (ViewModel == null) return;

        // Hide all template style grids first
        LtStyleBlueAngular.Visibility = Visibility.Collapsed;
        LtStyleGoldBadge.Visibility = Visibility.Collapsed;
        LtStyleCyberpunkPurple.Visibility = Visibility.Collapsed;
        LtStyleClassicNavy.Visibility = Visibility.Collapsed;

        var tagBg = ParseColor(ViewModel.TagBgColor, Color.FromRgb(0, 56, 168));
        var tagText = ParseColor(ViewModel.TagTextColor, Colors.White);
        var titleBg = ParseColor(ViewModel.TitleBgColor, Color.FromRgb(241, 245, 249));
        var titleText = ParseColor(ViewModel.TitleTextColor, Color.FromRgb(15, 23, 42));
        var subBg = ParseColor(ViewModel.SubBgColor, Color.FromRgb(2, 132, 199));
        var subText = ParseColor(ViewModel.SubTextColor, Colors.White);

        switch (ViewModel.SelectedStylePreset)
        {
            case LowerThirdStyle.BlueAngular:
                LtStyleBlueAngular.Visibility = Visibility.Visible;
                TagBorder_Blue.Background = tagBg;
                TagTextBlock_Blue.Foreground = tagText;
                TitleBorder_Blue.Background = titleBg;
                TitleTextBlock_Blue.Foreground = titleText;
                SubtitleBorder_Blue.Background = subBg;
                SubtitleTextBlock_Blue.Foreground = subText;
                ApplyHighlight(TagBorder_Blue, TagTextBlock_Blue, TitleBorder_Blue, TitleTextBlock_Blue, SubtitleBorder_Blue, SubtitleTextBlock_Blue);
                break;

            case LowerThirdStyle.GoldBadge:
                LtStyleGoldBadge.Visibility = Visibility.Visible;
                TagEllipse_Gold.Fill = tagBg;
                TagTextBlock_Gold.Foreground = tagText;
                TitleBorder_Gold.Background = titleBg;
                TitleTextBlock_Gold.Foreground = titleText;
                SubtitleTextBlock_Gold.Foreground = subBg;
                ApplyHighlight(null, TagTextBlock_Gold, TitleBorder_Gold, TitleTextBlock_Gold, null, SubtitleTextBlock_Gold);
                break;

            case LowerThirdStyle.CyberpunkPurple:
                LtStyleCyberpunkPurple.Visibility = Visibility.Visible;
                TagBorder_Purple.Background = tagBg;
                TagTextBlock_Purple.Foreground = tagText;
                TitleBorder_Purple.Background = titleBg;
                TitleTextBlock_Purple.Foreground = titleText;
                SubtitleBorder_Purple.Background = subBg;
                SubtitleTextBlock_Purple.Foreground = subText;
                ApplyHighlight(TagBorder_Purple, TagTextBlock_Purple, TitleBorder_Purple, TitleTextBlock_Purple, SubtitleBorder_Purple, SubtitleTextBlock_Purple);
                break;

            case LowerThirdStyle.ClassicNavy:
                LtStyleClassicNavy.Visibility = Visibility.Visible;
                TitleBorder_Navy.Background = titleBg;
                TitleBorder_Navy.BorderBrush = tagBg;
                TagTextBlock_Navy.Foreground = tagText;
                TitleTextBlock_Navy.Foreground = titleText;
                SubtitleTextBlock_Navy.Foreground = subText;
                ApplyHighlight(TitleBorder_Navy, TagTextBlock_Navy, TitleBorder_Navy, TitleTextBlock_Navy, TitleBorder_Navy, SubtitleTextBlock_Navy);
                break;
        }
    }

    private void ApplyHighlight(Border? tagBorder, TextBlock? tagText, Border? titleBorder, TextBlock? titleText, Border? subBorder, TextBlock? subText)
    {
        if (ViewModel == null) return;

        if (tagBorder != null) tagBorder.BorderThickness = new Thickness(0);
        if (titleBorder != null) titleBorder.BorderThickness = new Thickness(0);
        if (subBorder != null) subBorder.BorderThickness = new Thickness(0);

        if (tagText != null) tagText.TextDecorations = null;
        if (titleText != null) titleText.TextDecorations = null;
        if (subText != null) subText.TextDecorations = null;

        var highlightBrush = new SolidColorBrush(Color.FromRgb(0, 229, 255));

        switch (ViewModel.SelectedElementKey)
        {
            case "TagBg":
                if (tagBorder != null)
                {
                    tagBorder.BorderBrush = highlightBrush;
                    tagBorder.BorderThickness = new Thickness(3);
                }
                break;
            case "TagText":
                if (tagText != null) tagText.TextDecorations = TextDecorations.Underline;
                break;
            case "TitleBg":
                if (titleBorder != null)
                {
                    titleBorder.BorderBrush = highlightBrush;
                    titleBorder.BorderThickness = new Thickness(3);
                }
                break;
            case "TitleText":
                if (titleText != null) titleText.TextDecorations = TextDecorations.Underline;
                break;
            case "SubtitleBg":
                if (subBorder != null)
                {
                    subBorder.BorderBrush = highlightBrush;
                    subBorder.BorderThickness = new Thickness(3);
                }
                break;
            case "SubtitleText":
                if (subText != null) subText.TextDecorations = TextDecorations.Underline;
                break;
        }
    }

    private void TagBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.SelectElement("TagBg");
        e.Handled = true;
    }

    private void TagText_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.SelectElement("TagText");
        e.Handled = true;
    }

    private void TitleBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.SelectElement("TitleBg");
        e.Handled = true;
    }

    private void TitleText_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.SelectElement("TitleText");
        e.Handled = true;
    }

    private void SubtitleBorder_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.SelectElement("SubtitleBg");
        e.Handled = true;
    }

    private void SubtitleText_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel?.SelectElement("SubtitleText");
        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private static SolidColorBrush ParseColor(string hex, Color fallback)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                var converted = ColorConverter.ConvertFromString(hex);
                if (converted is Color c) return new SolidColorBrush(c);
            }
        }
        catch { }
        return new SolidColorBrush(fallback);
    }
}
