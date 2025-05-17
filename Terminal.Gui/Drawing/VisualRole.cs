namespace Terminal.Gui;

/// <summary>
///     Represents the semantic visual role of a visual element rendered by a <see cref="View"/> (e.g., Normal text, Focused item, Active selection).
/// </summary>
/// <remarks>
///     A single View may render as one or multiple elements. Each element can be associated with a different <see cref="VisualRole"/>.
/// </remarks>
public enum VisualRole
{
    /// <summary>
    ///     The default visual role for unfocused, unselected, enabled elements.
    /// </summary>
    Normal,

    /// <summary>
    ///     The visual role for hot elements (typically those with HotKey indicators) that are unfocused.
    /// </summary>
    HotNormal,

    /// <summary>
    ///     The visual role when the element is focused.
    /// </summary>
    Focus,

    /// <summary>
    ///     The visual role when the element is focused and has an active HotKey indicator.
    /// </summary>
    HotFocus,

    /// <summary>
    ///     The visual role for elements that are active or selected (e.g., selected item in a ListView).
    /// </summary>
    Active,

    /// <summary>
    ///     The visual role for elements that are active and have a HotKey indicator.
    /// </summary>
    HotActive,

    /// <summary>
    ///     The visual role for elements that are highlighted (e.g., when the mouse is hovering over a Button).
    /// </summary>
    Highlight,

    /// <summary>
    ///     The visual role for elements that are disabled and not interactable.
    /// </summary>
    Disabled,

    /// <summary>
    ///     The visual role for elements that are editible
    /// </summary>
    Editable,

    /// <summary>
    ///     The visual role for elements that are normally editable but currently read-only.
    /// </summary>
    ReadOnly
}
