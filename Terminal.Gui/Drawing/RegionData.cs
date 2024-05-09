#nullable enable
namespace Terminal.Gui;

/// <summary>
/// Encapsulates the data that makes up a <see cref="Region"/> object. This class cannot be inherited.
/// </summary>
public sealed  class RegionData
{
    internal RegionData (Rune [] data) { Data = data; }

    /// <summary>
    /// Gets or sets an array of bytes that specify the <see cref="Region"/> object.
    /// </summary>
    public Rune [] Data { get; set; }
}
