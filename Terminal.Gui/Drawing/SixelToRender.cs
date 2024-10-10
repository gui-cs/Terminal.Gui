namespace Terminal.Gui;

/// <summary>
///     Describes a request to render a given <see cref="SixelData"/> at a given <see cref="ScreenPosition"/>.
///     Requires that the terminal and <see cref="ConsoleDriver"/> both support sixel.
/// </summary>
public class SixelToRender
{
    /// <summary>
    ///     gets or sets the encoded sixel data. Use <see cref="SixelEncoder"/> to convert bitmaps
    ///     into encoded sixel data.
    /// </summary>
    public string SixelData { get; set; }

    /// <summary>
    ///     gets or sets where to move the cursor to before outputting the <see cref="SixelData"/>.
    /// </summary>
    public Point ScreenPosition { get; set; }
}
