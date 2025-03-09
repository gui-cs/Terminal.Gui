namespace Terminal.Gui;

/// <summary>
///     Settings for how the <see cref="View.Viewport"/> behaves.
/// </summary>
/// <remarks>
///     See the Layout Deep Dive for more information:
///     <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/layout.html"/>
/// </remarks>
[Flags]
public enum ViewportSettings
{
    /// <summary>
    ///     No settings.
    /// </summary>
    None = 0b_0000,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.X</c> can be set to negative values enabling scrolling beyond the left of
    ///     the
    ///     content area.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.X</c> is constrained to positive values.
    ///     </para>
    /// </summary>
    AllowNegativeX = 0b_0001,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Y</c> can be set to negative values enabling scrolling beyond the top of the
    ///     content area.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.Y</c> is constrained to positive values.
    ///     </para>
    /// </summary>
    AllowNegativeY = 0b_0010,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Size</c> can be set to negative coordinates enabling scrolling beyond the
    ///     top-left of the
    ///     content area.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.Size</c> is constrained to positive coordinates.
    ///     </para>
    /// </summary>
    AllowNegativeLocation = AllowNegativeX | AllowNegativeY,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.X</c> can be set values greater than <see cref="View.GetContentSize ()"/>
    ///     <c>.Width</c> enabling scrolling beyond the right
    ///     of the content area.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.X</c> is constrained to <see cref="View.GetContentSize ()"/>
    ///         <c>.Width - 1</c>.
    ///         This means the last column of the content will remain visible even if there is an attempt to scroll the
    ///         Viewport past the last column.
    ///     </para>
    ///     <para>
    ///         The practical effect of this is that the last column of the content will always be visible.
    ///     </para>
    /// </summary>
    AllowXGreaterThanContentWidth = 0b_0100,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Y</c> can be set values greater than <see cref="View.GetContentSize ()"/>
    ///     <c>.Height</c> enabling scrolling beyond the right
    ///     of the content area.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.Y</c> is constrained to <see cref="View.GetContentSize ()"/>
    ///         <c>.Height - 1</c>.
    ///         This means the last row of the content will remain visible even if there is an attempt to scroll the Viewport
    ///         past the last row.
    ///     </para>
    ///     <para>
    ///         The practical effect of this is that the last row of the content will always be visible.
    ///     </para>
    /// </summary>
    AllowYGreaterThanContentHeight = 0b_1000,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Location</c> can be set values greater than
    ///     <see cref="View.GetContentSize ()"/>
    ///     enabling scrolling beyond the bottom-right
    ///     of the content area.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/> is constrained to <see cref="View.GetContentSize ()"/><c> -1</c>.
    ///         This means the last column and row of the content will remain visible even if there is an attempt to
    ///         scroll the Viewport past the last column or row.
    ///     </para>
    /// </summary>
    AllowLocationGreaterThanContentSize = AllowXGreaterThanContentWidth | AllowYGreaterThanContentHeight,

    /// <summary>
    ///     If set and <see cref="View.Viewport"/><c>.Width</c> is greater than <see cref="View.GetContentSize ()"/>
    ///     <c>.Width</c> <see cref="View.Viewport"/><c>.X</c> can be negative.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.X</c> will be constrained to non-negative values when
    ///         <see cref="View.Viewport"/><c>.Width</c> is greater than <see cref="View.GetContentSize ()"/>
    ///         <c>.Width</c>, preventing
    ///         scrolling beyond the left of the Viewport.
    ///     </para>
    ///     <para>
    ///         This can be useful in infinite scrolling scenarios.
    ///     </para>
    /// </summary>
    AllowNegativeXWhenWidthGreaterThanContentWidth = 0b_0001_0000,

    /// <summary>
    ///     If set and <see cref="View.Viewport"/><c>.Height</c> is greater than <see cref="View.GetContentSize ()"/>
    ///     <c>.Height</c> <see cref="View.Viewport"/><c>.Y</c> can be negative.
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.Y</c> will be constrained to non-negative values when
    ///         <see cref="View.Viewport"/><c>.Height</c> is greater than <see cref="View.GetContentSize ()"/>
    ///         <c>.Height</c>, preventing
    ///         scrolling above the top of the Viewport.
    ///     </para>
    ///     <para>
    ///         This can be useful in infinite scrolling scenarios.
    ///     </para>
    /// </summary>
    AllowNegativeYWhenHeightGreaterThanContentHeight = 0b_0010_0000,

    /// <summary>
    ///     The combination of <see cref="AllowNegativeXWhenWidthGreaterThanContentWidth"/> and
    ///     <see cref="AllowNegativeYWhenHeightGreaterThanContentHeight"/>.
    /// </summary>
    AllowNegativeLocationWhenSizeGreaterThanContentSize = AllowNegativeXWhenWidthGreaterThanContentWidth | AllowNegativeYWhenHeightGreaterThanContentHeight,

    /// <summary>
    ///     By default, clipping is applied to the <see cref="View.Viewport"/>. Setting this flag will cause clipping to be
    ///     applied to the visible content area.
    /// </summary>
    ClipContentOnly = 0b_0100_0000,

    /// <summary>
    ///     If set <see cref="View.ClearViewport"/> will clear only the portion of the content
    ///     area that is visible within the <see cref="View.Viewport"/>. This is useful for views that have a
    ///     content area larger than the Viewport and want the area outside the content to be visually distinct.
    ///     <see cref="ClipContentOnly"/> must be set for this setting to work (clipping beyond the visible area must be
    ///     disabled).
    /// </summary>
    ClearContentOnly = 0b_1000_0000,

    /// <summary>
    ///     If set the View will be transparent: The <see cref="View.Viewport"/> will not be cleared when the View is drawn and the clip region
    ///     will be set to clip the View's <see cref="View.Text"/> and <see cref="View.Subviews"/>.
    ///     <para>
    ///         Only the topmost View in a Subview Hierarchy can be transparent. Any subviews of the topmost transparent view
    ///         will have indeterminate draw behavior.
    ///     </para>
    ///     <para>
    ///         Combine this with <see cref="TransparentMouse"/> to get a view that is both visually transparent and transparent to the mouse.
    ///     </para>
    /// </summary>
    Transparent = 0b_0001_0000_0000,

    /// <summary>
    ///     If set the View will be transparent to mouse events: Any mouse event that occurs over the View (and it's Subviews) will be passed to the
    ///     Views below it.
    ///     <para>
    ///         Combine this with <see cref="Transparent"/> to get a view that is both visually transparent and transparent to the mouse.
    ///     </para>
    /// </summary>
    TransparentMouse = 0b_0010_0000_0000,
}
