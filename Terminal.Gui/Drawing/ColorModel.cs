#nullable enable

namespace Terminal.Gui;

/// <summary>
/// Describes away of modelling color e.g. Hue
/// Saturation Lightness.
/// </summary>
public enum ColorModel
{
    /// <summary>
    /// Color modelled by storing Red, Green and Blue as (0-255) ints
    /// </summary>
    RGB,

    /// <summary>
    /// Color modelled by storing Hue (360 degrees), Saturation (100%) and Value (100%)
    /// </summary>
    HSV,

    /// <summary>
    /// Color modelled by storing Hue (360 degrees), Saturation (100%) and Lightness (100%)
    /// </summary>
    HSL
}
