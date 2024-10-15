namespace Terminal.Gui;

/// <summary>
///     Implementation of <see cref="ISixelSupportDetector"/> that assumes best
///     case scenario (full support including transparency with 10x20 resolution).
/// </summary>
public class AssumeSupportDetector : ISixelSupportDetector
{
    /// <inheritdoc/>
    public SixelSupportResult Detect ()
    {
        return new()
        {
            IsSupported = true,
            MaxPaletteColors = 256,
            Resolution = new (10, 20),
            SupportsTransparency = true
        };
    }
}
