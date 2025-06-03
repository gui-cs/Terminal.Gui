namespace Terminal.Gui.Drawing;

/// <summary>
///     Represents the semantic visual role of a visual element rendered by a <see cref="View"/>. Each VisualRole maps to
///     a property of <see cref="Scheme"/> (e.g., <see cref="Scheme.Normal"/>).
/// </summary>
/// <remarks>
///     A single View may render as one or multiple elements. Each element can be associated with a different
///     <see cref="VisualRole"/>.
/// </remarks>
public enum VisualRole
{
    /// <summary>
    ///     The default visual role for unfocused, unselected, enabled elements.
    /// </summary>
    Normal,

    /// <summary>
    ///     The visual role for <see cref="Normal"/> elements with a <see cref="View.HotKey"/> indicator.
    /// </summary>
    HotNormal,

    /// <summary>
    ///     The visual role when the element is focused.
    /// </summary>
    Focus,

    /// <summary>
    ///     The visual role for <see cref="Focus"/> elements with a <see cref="View.HotKey"/> indicator.
    /// </summary>
    HotFocus,

    /// <summary>
    ///     The visual role for elements that are active or selected (e.g., selected item in a <see cref="ListView"/>). Also
    ///     used
    ///     for headers in, <see cref="HexView"/>, <see cref="CharMap"/> and  <see cref="TabView"/>.
    /// </summary>
    Active,

    /// <summary>
    ///     The visual role for <see cref="Active"/> elements with a <see cref="View.HotKey"/> indicator.
    /// </summary>
    HotActive,

    /// <summary>
    ///     The visual role for elements that are highlighted (e.g., when the mouse is inside over a <see cref="Button"/>).
    /// </summary>
    Highlight,

    /// <summary>
    ///     The visual role for elements that are disabled and not interactable.
    /// </summary>
    Disabled,

    /// <summary>
    ///     The visual role for elements that are editable (e.g., <see cref="TextField"/> and <see cref="TextView"/>).
    /// </summary>
    Editable,

    /// <summary>
    ///     The visual role for elements that are normally editable but currently read-only.
    /// </summary>
    ReadOnly
}
