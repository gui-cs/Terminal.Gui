namespace Terminal.Gui;

/// <summary>
/// Describes the highlight style of a view.
/// </summary>
[Flags]
public enum HighlightStyle
{
    /// <summary>
    /// No highlight.
    /// </summary>
    None = 0,

#if HOVER
    /// <summary>
    /// The mouse is hovering over the view.
    /// </summary>
    Hover = 1,
#endif

    /// <summary>
    /// The mouse is pressed within the <see cref="View.Viewport"/>.
    /// </summary>
    Pressed = 2,

    /// <summary>
    /// The mouse is pressed but moved outside the <see cref="View.Viewport"/>.
    /// </summary>
    PressedOutside = 4
}
