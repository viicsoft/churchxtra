using System;

namespace ChurchAI.Core.Entities;

public enum LowerThirdStyle
{
    BlueAngular,
    GoldBadge,
    CyberpunkPurple,
    ClassicNavy
}

public enum LowerThirdPosition
{
    BottomLeft,
    BottomCenter,
    BottomRight,
    TopLeft
}

public enum ScriptureLowerThirdTemplate
{
    PillCapsuleSand = 0,             // Image 1: Cream/sand rounded pill with dark text + floating gold reference badge
    DualStackedCoralOffWhite = 1,    // Image 2: Dusty rose category banner + off-white text box + blue accent
    DualMintGlassmorphism = 2,       // Image 3: Floating mint reference capsule + rounded translucent mint verse card
    PastelSageRosePill = 3,          // Image 4: Dual connected pill with coral reference badge on left + sage green verse box
    CrimsonVousBroadcast = 4         // Image 5: Crimson/black reference header + crisp off-white uppercase box + V logo
}

public class LowerThirdItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TagText { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string TagBgColorHex { get; set; } = string.Empty;
    public string TagTextColorHex { get; set; } = string.Empty;
    public string TitleBgColorHex { get; set; } = string.Empty;
    public string TitleTextColorHex { get; set; } = string.Empty;
    public string SubtitleBgColorHex { get; set; } = string.Empty;
    public string SubtitleTextColorHex { get; set; } = string.Empty;
    public LowerThirdStyle StylePreset { get; set; } = LowerThirdStyle.BlueAngular;
    public LowerThirdPosition Position { get; set; } = LowerThirdPosition.BottomLeft;
    public bool IsPreset { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
