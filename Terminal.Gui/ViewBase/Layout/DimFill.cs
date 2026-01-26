namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a dimension that fills the dimension, leaving the specified margin.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
///     </para>
///     <para>
///         When the SuperView uses <see cref="DimAuto"/>, a <see cref="DimFill"/> SubView does <b>not</b> contribute
///         to the auto-sizing calculation by default. Because <see cref="DimFill"/> derives its size from the
///         SuperView's ContentSize, and <see cref="DimAuto"/> computes the ContentSize from its SubViews, a circular
///         dependency arises: the <see cref="DimFill"/> SubView will be sized to 0 unless
///         <see cref="MinimumContentDim"/> is specified.
///     </para>
///     <para>
///         Set <see cref="MinimumContentDim"/> to ensure the SubView contributes a minimum size to the auto-sizing
///         calculation. See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for details.
///     </para>
/// </remarks>
/// <param name="Margin">The margin to not fill.</param>
/// <param name="MinimumContentDim">
///     The minimum dimension the filled view will be constrained to. When the SuperView uses <see cref="DimAuto"/>,
///     this minimum will contribute to the auto-sizing calculation, ensuring the SuperView is at least large enough
///     to accommodate the minimum.
/// </param>
public record DimFill (Dim Margin, Dim? MinimumContentDim = null) : Dim
{
    /// <inheritdoc/>
    public override string ToString ()
    {
        if (MinimumContentDim is { })
        {
            return $"Fill({Margin},min:{MinimumContentDim})";
        }

        return $"Fill({Margin})";
    }

    internal override int GetAnchor (int size) => size - Margin.GetAnchor (0);

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        int fillSize = base.Calculate (location, superviewContentSize, us, dimension);

        if (MinimumContentDim is null)
        {
            return fillSize;
        }
        int minSize = MinimumContentDim.Calculate (location, superviewContentSize, us, dimension);
        fillSize = int.Max (fillSize, minSize);

        return fillSize;
    }
}
