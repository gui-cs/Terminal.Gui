namespace Terminal.Gui;

/// <summary>
///     Describes what user actions are enabled for arranging a <see cref="View"/> within it's <see cref="View.SuperView"/>.
/// </summary>
[Flags]
public enum ViewArrangement
{
    /// <summary>
    ///     The view can neither be moved nor resized.
    /// </summary>
    Fixed = 0,

    /// <summary>
    ///     The view can be moved within it's <see cref="SuperView"/>.
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
    Resizable = LeftResizable | RightResizable | TopResizable | BottomResizable
}
public partial class View
{
    /// <summary>
    ///    Gets or sets the user actions that are enabled for the view within it's <see cref="SuperView"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Sizing or moving a view is only possible if the <see cref="View"/> is part of a <see cref="SuperView"/> and
    ///     the relevant position and dimensions of the <see cref="View"/> are independent of other SubViews
    /// </para>
    /// </remarks>
    public ViewArrangement Arrangement { get; set; }
}
