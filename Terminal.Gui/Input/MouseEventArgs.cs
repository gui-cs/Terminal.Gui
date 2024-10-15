#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Specifies the event arguments for <see cref="Terminal.Gui.MouseEventArgs"/>. This is a higher-level construct than
///     the wrapped <see cref="Terminal.Gui.MouseEventArgs"/> class and is used for the events defined on
///     <see cref="View"/> and subclasses
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

    /// <summary>
    ///     The position of the mouse in <see cref="View"/>'s Viewport-relative coordinates. Only valid if <see cref="View"/>
    ///     is set.
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    ///     Gets whether <see cref="Flags"/> contains any of the button pressed related flags.
    /// </summary>
    public bool IsPressed => Flags.HasFlag (MouseFlags.Button1Pressed)
                             || Flags.HasFlag (MouseFlags.Button2Pressed)
                             || Flags.HasFlag (MouseFlags.Button3Pressed)
                             || Flags.HasFlag (MouseFlags.Button4Pressed);

    /// <summary>
    ///     Gets whether <see cref="Flags"/> contains any of the button released related flags.
    /// </summary>
    public bool IsReleased => Flags.HasFlag (MouseFlags.Button1Released)
                              || Flags.HasFlag (MouseFlags.Button2Released)
                              || Flags.HasFlag (MouseFlags.Button3Released)
                              || Flags.HasFlag (MouseFlags.Button4Released);

    /// <summary>
    ///     Gets whether <see cref="Flags"/> contains any of the single-clicked related flags.
    /// </summary>
    public bool IsSingleClicked => Flags.HasFlag (MouseFlags.Button1Clicked)
                                   || Flags.HasFlag (MouseFlags.Button2Clicked)
                                   || Flags.HasFlag (MouseFlags.Button3Clicked)
                                   || Flags.HasFlag (MouseFlags.Button4Clicked);

    /// <summary>
    ///     Gets whether <see cref="Flags"/> contains any of the double-clicked related flags.
    /// </summary>
    public bool IsDoubleClicked => Flags.HasFlag (MouseFlags.Button1DoubleClicked)
                                   || Flags.HasFlag (MouseFlags.Button2DoubleClicked)
                                   || Flags.HasFlag (MouseFlags.Button3DoubleClicked)
                                   || Flags.HasFlag (MouseFlags.Button4DoubleClicked);

    /// <summary>
    ///     Gets whether <see cref="Flags"/> contains any of the triple-clicked related flags.
    /// </summary>
    public bool IsTripleClicked => Flags.HasFlag (MouseFlags.Button1TripleClicked)
                                   || Flags.HasFlag (MouseFlags.Button2TripleClicked)
                                   || Flags.HasFlag (MouseFlags.Button3TripleClicked)
                                   || Flags.HasFlag (MouseFlags.Button4TripleClicked);

    /// <summary>
    ///     Gets whether <see cref="Flags"/> contains any of the mouse button clicked related flags.
    /// </summary>
    public bool IsSingleDoubleOrTripleClicked =>
        Flags.HasFlag (MouseFlags.Button1Clicked)
        || Flags.HasFlag (MouseFlags.Button2Clicked)
        || Flags.HasFlag (MouseFlags.Button3Clicked)
        || Flags.HasFlag (MouseFlags.Button4Clicked)
        || Flags.HasFlag (MouseFlags.Button1DoubleClicked)
        || Flags.HasFlag (MouseFlags.Button2DoubleClicked)
        || Flags.HasFlag (MouseFlags.Button3DoubleClicked)
        || Flags.HasFlag (MouseFlags.Button4DoubleClicked)
        || Flags.HasFlag (MouseFlags.Button1TripleClicked)
        || Flags.HasFlag (MouseFlags.Button2TripleClicked)
        || Flags.HasFlag (MouseFlags.Button3TripleClicked)
        || Flags.HasFlag (MouseFlags.Button4TripleClicked);

    /// <summary>
    ///     Gets whether <see cref="Flags"/> contains any of the mouse wheel related flags.
    /// </summary>
    public bool IsWheel => Flags.HasFlag (MouseFlags.WheeledDown)
                           || Flags.HasFlag (MouseFlags.WheeledUp)
                           || Flags.HasFlag (MouseFlags.WheeledLeft)
                           || Flags.HasFlag (MouseFlags.WheeledRight);

    /// <summary>Returns a <see cref="T:System.String"/> that represents the current <see cref="Terminal.Gui.MouseEventArgs"/>.</summary>
    /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="Terminal.Gui.MouseEventArgs"/>.</returns>
    public override string ToString () { return $"({ScreenPosition}):{Flags}:{View?.Id}:{Position}"; }
}
