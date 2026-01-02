namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a dimension that fills the dimension, leaving the specified margin.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="Margin">The margin to not fill.</param>
/// <param name="MinimumContentDim">
///     The minimum dimension the filled view will be constrained to. When the SuperView uses <see cref="DimAuto"/>,
///     this minimum will contribute to the auto-sizing calculation.
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

    internal override int GetAnchor (int size) { return size - Margin.GetAnchor (0); }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        int fillSize = base.Calculate (location, superviewContentSize, us, dimension);

        if (MinimumContentDim is { })
        {
            int minSize = MinimumContentDim.Calculate (location, superviewContentSize, us, dimension);
            fillSize = int.Max (fillSize, minSize);
        }

        return fillSize;
    }
}