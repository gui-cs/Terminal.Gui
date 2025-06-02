

namespace Terminal.Gui.ViewBase;

/// <summary>
///     Indicates the side for <see cref="Pos"/> operations.
/// </summary>
///
public enum Side
{
    /// <summary>
    ///     The left (X) side of the view.
    /// </summary>
    Left = 0,

    /// <summary>
    ///     The top (Y) side of the view.
    /// </summary>
    Top = 1,

    /// <summary>
    ///     The right (X + Width) side of the view.
    /// </summary>
    Right = 2,

    /// <summary>
    ///     The bottom (Y + Height) side of the view.
    /// </summary>
    Bottom = 3
}