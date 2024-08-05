﻿namespace Terminal.Gui;

/// <summary>
///     Describes what user actions are enabled for arranging a <see cref="View"/> within it's <see cref="View.SuperView"/>
///     .
///     See <see cref="View.Arrangement"/>.
/// </summary>
/// <remarks>
///     <para>
///         Sizing or moving a view is only possible if the <see cref="View"/> is part of a <see cref="View.SuperView"/>
///         and
///         the relevant position and dimensions of the <see cref="View"/> are independent of other SubViews
///     </para>
/// </remarks>
[Flags]
public enum ViewArrangement
{
    /// <summary>
    ///     The view can neither be moved nor resized.
    /// </summary>
    Fixed = 0,

    /// <summary>
    ///     The view can be moved.
    /// </summary>
    Movable = 1,

    /// <summary>
    ///     The left edge of the view can be resized.
    /// </summary>
    LeftResizable = 2,

    /// <summary>
    ///     The right edge of the view can be resized.
    /// </summary>
    RightResizable = 4,

    /// <summary>
    ///     The top edge of the view can be resized.
    /// </summary>
    /// <remarks>
    ///     This flag is mutually exclusive with <see cref="Movable"/>. If both are set, <see cref="Movable"/> takes
    ///     precedence.
    /// </remarks>
    TopResizable = 8,

    /// <summary>
    ///     The bottom edge of the view can be resized.
    /// </summary>
    BottomResizable = 16,

    /// <summary>
    ///     The view can be resized in any direction.
    /// </summary>
    /// <remarks>
    ///     If <see cref="Movable"/> is also set, the top will not be resizable.
    /// </remarks>
    Resizable = LeftResizable | RightResizable | TopResizable | BottomResizable,

    /// <summary>
    ///     The view overlap other views.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set, Tab and Shift-Tab will be constrained to the subviews of the view (normally, they will navigate to
    ///         the next/prev view in the next/prev Tabindex).
    ///         Use Ctrl-Tab (Ctrl-PageDown) / Ctrl-Shift-Tab (Ctrl-PageUp) to move between overlapped views.
    ///     </para>
    /// </remarks>
    Overlapped = 32,
}
