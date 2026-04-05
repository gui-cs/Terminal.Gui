namespace Terminal.Gui.ViewBase;

/// <summary>
///     Parameters passed from <see cref="BorderView"/> to an <see cref="ITitleView"/> during layout.
///     Captures the border/tab state that the title view cannot derive from its own stored properties.
/// </summary>
public readonly record struct TabLayoutContext
{
    /// <summary>Gets the content border rectangle in screen coordinates.</summary>
    public required Rectangle BorderBounds { get; init; }

    /// <summary>Gets the tab offset along the tab side (in cells from the content border origin).</summary>
    public required int TabOffset { get; init; }

    /// <summary>
    ///     Gets an explicit tab length override, or <see langword="null"/> to auto-size from the title text.
    ///     When set, the title view uses this length instead of auto-sizing.
    /// </summary>
    public int? TabLengthOverride { get; init; }

    /// <summary>Gets whether this tab is focused or is the last tab (open-gap state).</summary>
    public required bool HasFocus { get; init; }

    /// <summary>Gets the <see cref="LineStyle"/> to apply to the title view's border.</summary>
    public required LineStyle? LineStyle { get; init; }

    /// <summary>Gets the title text to display in the header.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the screen origin of the owning <see cref="BorderView"/>'s viewport, used for coordinate conversion.</summary>
    public required Point ScreenOrigin { get; init; }
}

/// <summary>
///     Defines the contract for a replaceable tab header view used by <see cref="BorderView"/>.
///     Implementations must also derive from <see cref="View"/> so they can be added as SubViews.
/// </summary>
public interface ITitleView : IOrientation
{
    /// <summary>
    ///     Gets or sets the tab depth — the number of rows (or columns) the tab header occupies
    ///     on its <see cref="TabSide"/>.
    /// </summary>
    int TabDepth { get; set; }

    /// <summary>
    ///     Gets the measured tab length (in cells) after <see cref="UpdateLayout"/> has auto-sized the title view.
    ///     Returns 0 if <see cref="UpdateLayout"/> has not yet been called.
    /// </summary>
    int MeasuredTabLength { get; set; }

    /// <summary>Gets or sets which side of the content border the tab header sits on.</summary>
    Side TabSide { get; set; }

    /// <summary>
    ///     Updates this title view's frame, border thickness, text, orientation, padding,
    ///     and visibility based on the tab layout context provided by the owning <see cref="BorderView"/>.
    /// </summary>
    void UpdateLayout (in TabLayoutContext context);
}
