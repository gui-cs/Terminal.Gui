using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Describes the highlight style of a view when the mouse is over it.
/// </summary>
[JsonConverter (typeof (JsonStringEnumConverter<HighlightStyle>))]
[Flags]
public enum HighlightStyle
{
    /// <summary>
    ///     No highlight.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The mouse is hovering over the view (but not pressed). See <see cref="View.MouseEnter"/>.
    /// </summary>
    Hover = 1,

    /// <summary>
    ///     The mouse is pressed within the <see cref="View.Viewport"/>.
    /// </summary>
    Pressed = 2,

    /// <summary>
    ///     The mouse is pressed but moved outside the <see cref="View.Viewport"/>.
    /// </summary>
    PressedOutside = 4
}
