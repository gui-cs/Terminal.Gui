using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui;

/// <summary>
///     <para>Indicates the LayoutStyle for the <see cref="View"/>.</para>
///     <para>
///         If Absolute, the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and
///         <see cref="View.Height"/> objects are all absolute values and are not relative. The position and size of the
///         view is described by <see cref="View.Frame"/>.
///     </para>
///     <para>
///         If Computed, one or more of the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, or
///         <see cref="View.Height"/> objects are relative to the <see cref="View.SuperView"/> and are computed at layout
///         time.
///     </para>
/// </summary>
[GenerateEnumExtensionMethods]
public enum LayoutStyle
{
    /// <summary>
    ///     Indicates the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and
    ///     <see cref="View.Height"/> objects are all absolute values and are not relative. The position and size of the view
    ///     is described by <see cref="View.Frame"/>.
    /// </summary>
    Absolute,

    /// <summary>
    ///     Indicates one or more of the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, or
    ///     <see cref="View.Height"/>
    ///     objects are relative to the <see cref="View.SuperView"/> and are computed at layout time.  The position and size of
    ///     the
    ///     view
    ///     will be computed based on these objects at layout time. <see cref="View.Frame"/> will provide the absolute computed
    ///     values.
    /// </summary>
    Computed
}