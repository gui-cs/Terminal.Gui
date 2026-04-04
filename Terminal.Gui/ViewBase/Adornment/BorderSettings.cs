namespace Terminal.Gui.ViewBase;

/// <summary>
///     Determines the settings for <see cref="Border"/>.
/// </summary>
[Flags]
public enum BorderSettings
{
    /// <summary>
    ///     No settings.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Show the title.
    /// </summary>
    Title = 1,

    /// <summary>
    ///     Use <see cref="GradientFill"/> to draw the border.
    /// </summary>
    Gradient = 2,

    /// <summary>
    ///     Draw a Tab on one side of the border. The <see cref="View.Title"/> will be displayed in the Tab. Configure with
    ///     <see cref="Border.TabSide"/>, <see cref="Border.TabOffset"/>,
    ///     <see cref="Border.TabLength"/>.
    /// </summary>
    Tab = 4
}
