namespace Terminal.Gui.ViewBase;

/// <summary>
///     Defines the contract for the <see cref="View"/>-level backing object of an adornment layer.
///     Implemented by <see cref="AdornmentView"/> (and its subclasses <c>BorderView</c>,
///     <c>MarginView</c>, <c>PaddingView</c>), as well as by the existing <see cref="Adornment"/> class
///     during the incremental migration.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="IAdornment.View"/> is typed as <see cref="IAdornmentView"/> rather than
///         the concrete <see cref="AdornmentView"/> to allow alternative implementations (e.g., test
///         doubles or custom renderers) without requiring inheritance from <see cref="AdornmentView"/>.
///     </para>
/// </remarks>
public interface IAdornmentView
{
    /// <summary>
    ///     The <see cref="View"/> this adornment layer surrounds.
    ///     Set by <see cref="AdornmentImpl.EnsureView"/> when the backing <see cref="View"/> is created,
    ///     using the <c>Parent</c> stored on <see cref="AdornmentImpl"/>.
    /// </summary>
    View? Parent { get; set; }

    /// <summary>
    ///     Back-reference to the lightweight <see cref="IAdornment"/> that owns this <see cref="View"/>.
    ///     <see cref="AdornmentView"/> delegates its <c>Thickness</c> property to this,
    ///     making <see cref="AdornmentImpl"/> the single authoritative owner of <see cref="IAdornment.Thickness"/>.
    ///     Set by <see cref="AdornmentImpl.EnsureView"/> when the backing <see cref="View"/> is created.
    /// </summary>
    IAdornment? Adornment { get; set; }
}
