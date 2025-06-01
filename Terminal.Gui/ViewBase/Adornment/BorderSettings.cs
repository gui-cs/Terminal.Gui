

namespace Terminal.Gui.ViewBase;

/// <summary>
/// Determines the settings for <see cref="Border"/>.
/// </summary>
[Flags]

public enum BorderSettings
{
    /// <summary>
    /// No settings.
    /// </summary>
    None = 0,

    /// <summary>
    /// Show the title.
    /// </summary>
    Title = 1,

    /// <summary>
    /// Use <see cref="GradientFill"/> to draw the border.
    /// </summary>
    Gradient = 2,
}
