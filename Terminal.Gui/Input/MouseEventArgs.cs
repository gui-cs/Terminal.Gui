#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Specifies the event arguments for <see cref="Terminal.Gui.MouseEventArgs"/>. This is a higher-level construct than
///     the wrapped <see cref="Terminal.Gui.MouseEventArgs"/> class and is used for the events defined on <see cref="View"/> and subclasses
///     of View (e.g. <see cref="View.MouseEnter"/> and <see cref="View.MouseClick"/>).
/// </summary>
public class MouseEventArgs : HandledEventArgs
{
    /// <summary>
    ///     Flags indicating the state of the mouse buttons and the type of event that occurred.
    /// </summary>
    public MouseFlags Flags { get; set; }

    /// <summary>
    ///     The screen-relative mouse position.
    /// </summary>
    public Point ScreenPosition { get; set; }

    /// <summary>The deepest View who's <see cref="View.Frame"/> contains <see cref="ScreenPosition"/>.</summary>
    public View? View { get; set; }

    /// <summary>The position of the mouse in <see cref="View"/>'s Viewport-relative coordinates. Only valid if <see cref="View"/> is set.</summary>
    public Point Position { get; set; }

    /// <summary>Returns a <see cref="T:System.String"/> that represents the current <see cref="Terminal.Gui.MouseEventArgs"/>.</summary>
    /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="Terminal.Gui.MouseEventArgs"/>.</returns>
    public override string ToString () { return $"({ScreenPosition}):{Flags}:{View?.Id}:{Position}"; }
}
