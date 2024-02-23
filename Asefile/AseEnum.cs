namespace Asefile;

public enum AnimationDirection
{
    Forward = 0,
    Reverse = 1,
    PingPong = 2,
    PingPongReverse = 3,
}

public enum BlendMode
{
    Normal         = 0,
    Multiply       = 1,
    Screen         = 2,
    Overlay        = 3,
    Darken         = 4,
    Lighten        = 5,
    ColorDodge    = 6,
    ColorBurn     = 7,
    HardLight     = 8,
    SoftLight     = 9,
    Difference     = 10,
    Exclusion      = 11,
    Hue            = 12,
    Saturation     = 13,
    Color          = 14,
    Luminosity     = 15,
    Addition       = 16,
    Subtract       = 17,
    Divide         = 18,
}

[Flags]
public enum LayerFlags
{
    Visible = 1,
    Editable = 2,
    LockMovement = 4,
    Background = 8,
    PreferLinkedCels = 16,
    DisplayGroupCollapsed = 32,
    ReferenceLayer = 64,
}

public enum ExternalFileType
{
    ExternPalette,
    ExternTileset,
    ExtensionNameForProperties,
    ExtensionNameForTileManagement
}

public enum ColorProfileType
{
    NoProfile = 0,
    UseSRGB = 1,
    UseEmbeddedICC = 2,
}

public enum CelType
{
    RawImageData = 0,
    LinkedCel = 1,
    CompressedImage = 2,
    CompressedTilemap = 3,
}