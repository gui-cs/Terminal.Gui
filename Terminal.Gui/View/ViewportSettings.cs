namespace Terminal.Gui;

/// <summary>
///     Settings for how the <see cref="View.Viewport"/> behaves relative to the View's Content area.
/// </summary>
[Flags]
public enum ViewportSettings
{
    /// <summary>
    ///     No settings.
    /// </summary>
    None = 0,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.X</c> can be set to negative values enabling scrolling beyond the left of
    ///     the
    ///     content area.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.X</c> is constrained to positive values.
    ///     </para>
    /// </remarks>
    AllowNegativeX = 1,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Y</c> can be set to negative values enabling scrolling beyond the top of the
    ///     content area.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.Y</c> is constrained to positive values.
    ///     </para>
    /// </remarks>
    AllowNegativeY = 2,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Size</c> can be set to negative coordinates enabling scrolling beyond the
    ///     top-left of the
    ///     content area.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.Size</c> is constrained to positive coordinates.
    ///     </para>
    /// </remarks>
    AllowNegativeLocation = AllowNegativeX | AllowNegativeY,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.X</c> can be set values greater than <see cref="View.GetContentSize ()"/>
    ///     <c>.Width</c> enabling scrolling beyond the right
    ///     of the content area.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.X</c> is constrained to <see cref="View.GetContentSize ()"/>
    ///         <c>.Width - 1</c>.
    ///         This means the last column of the content will remain visible even if there is an attempt to scroll the
    ///         Viewport past the last column.
    ///     </para>
    ///     <para>
    ///         The practical effect of this is that the last column of the content will always be visible.
    ///     </para>
    /// </remarks>
    AllowXGreaterThanContentWidth = 4,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Y</c> can be set values greater than <see cref="View.GetContentSize ()"/>
    ///     <c>.Height</c> enabling scrolling beyond the right
    ///     of the content area.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/><c>.Y</c> is constrained to <see cref="View.GetContentSize ()"/>
    ///         <c>.Height - 1</c>.
    ///         This means the last row of the content will remain visible even if there is an attempt to scroll the Viewport
    ///         past the last row.
    ///     </para>
    ///     <para>
    ///         The practical effect of this is that the last row of the content will always be visible.
    ///     </para>
    /// </remarks>
    AllowYGreaterThanContentHeight = 8,

    /// <summary>
    ///     If set, <see cref="View.Viewport"/><c>.Size</c> can be set values greater than <see cref="View.GetContentSize ()"/>
    ///     enabling scrolling beyond the bottom-right
    ///     of the content area.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When not set, <see cref="View.Viewport"/> is constrained to <see cref="View.GetContentSize ()"/><c> -1</c>.
    ///         This means the last column and row of the content will remain visible even if there is an attempt to
    ///         scroll the Viewport past the last column or row.
    ///     </para>
    /// </remarks>
    AllowLocationGreaterThanContentSize = AllowXGreaterThanContentWidth | AllowYGreaterThanContentHeight,

    /// <summary>
    ///     By default, clipping is applied to the <see cref="View.Viewport"/>. Setting this flag will cause clipping to be
    ///     applied to the visible content area.
    /// </summary>
    ClipContentOnly = 16,

    /// <summary>
    ///     If set <see cref="View.Clear()"/> will clear only the portion of the content
    ///     area that is visible within the <see cref="View.Viewport"/>. This is useful for views that have a
    ///     content area larger than the Viewport and want the area outside the content to be visually distinct.
    /// </summary>
    /// <remarks>
    ///     <see cref="ClipContentOnly"/> must be set for this setting to work (clipping beyond the visible area must be
    ///     disabled).
    /// </remarks>
    ClearContentOnly = 32
}