using System.Text.Json.Serialization;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Describes the current state of the mouse in relation to a <see cref="View"/>.
/// </summary>
[JsonConverter (typeof (JsonStringEnumConverter<MouseState>))]
[Flags]
public enum MouseState
{
    /// <summary>
    ///     No mouse interaction with the view is occurring.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The mouse is over the <see cref="View.Viewport"/> (but not pressed). Set between the <see cref="View.MouseEnter"/>
    ///     and <see cref="View.MouseLeave"/> events.
    /// </summary>
    Over = 1,

    /// <summary>
    ///     The mouse is over the <see cref="View.Viewport"/> and is pressed.
    /// </summary>
    Pressed = 2,

    /// <summary>
    ///     The mouse is not over the <see cref="View.Viewport"/> and is pressed.
    /// </summary>
    PressedNotOver = 4
}
