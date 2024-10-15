namespace Terminal.Gui;

/// <summary>
///     Interface for detecting sixel support. Either through
///     ansi requests to terminal or config file etc.
/// </summary>
public interface ISixelSupportDetector
{
    /// <summary>
    ///     Gets the supported sixel state e.g. by sending Ansi escape sequences
    ///     or from a config file etc.
    /// </summary>
    /// <returns>Description of sixel support.</returns>
    public SixelSupportResult Detect ();
}
