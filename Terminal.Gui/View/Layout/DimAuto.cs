#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a dimension that automatically sizes the view to fit all the view's Content, SubViews, and/or Text.
/// </summary>
/// <remarks>
///     <para>
///         See <see cref="DimAutoStyle"/>.
///     </para>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
///     </para>
/// </remarks>
public class DimAuto () : Dim
{
    private readonly Dim? _maximumContentDim;

    /// <summary>
    ///     Gets the maximum dimension the View's ContentSize will be fit to. NOT CURRENTLY SUPPORTED.
    /// </summary>
    // ReSharper disable once ConvertToAutoProperty
    public required Dim? MaximumContentDim
    {
        get => _maximumContentDim;
        init => _maximumContentDim = value;
    }

    private readonly Dim? _minimumContentDim;

    /// <summary>
    ///     Gets the minimum dimension the View's ContentSize will be constrained to.
    /// </summary>
    // ReSharper disable once ConvertToAutoProperty
    public required Dim? MinimumContentDim
    {
        get => _minimumContentDim;
        init => _minimumContentDim = value;
    }

    private readonly DimAutoStyle _style;

    /// <summary>
    ///     Gets the style of the DimAuto.
    /// </summary>
    // ReSharper disable once ConvertToAutoProperty
    public required DimAutoStyle Style
    {
        get => _style;
        init => _style = value;
    }

    /// <inheritdoc/>
    public override string ToString () { return $"Auto({Style},{MinimumContentDim},{MaximumContentDim})"; }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        var textSize = 0;
        var subviewsSize = 0;

        int autoMin = MinimumContentDim?.GetAnchor (superviewContentSize) ?? 0;
        
        if (Style.FastHasFlags (DimAutoStyle.Text))
        {
            textSize = int.Max (autoMin, dimension == Dimension.Width ? us.TextFormatter.Size.Width : us.TextFormatter.Size.Height);
        }

        if (Style.FastHasFlags (DimAutoStyle.Content))
        {
            if (us._contentSize is { })
            {
                subviewsSize = dimension == Dimension.Width ? us.ContentSize.Width : us.ContentSize.Height;
            }
            else
            {
                // TODO: This whole body of code is a WIP (for https://github.com/gui-cs/Terminal.Gui/pull/3451).
                subviewsSize = 0;

                List<View> subviews;

                if (dimension == Dimension.Width)
                {
                    subviews = us.Subviews.Where (v => v.X is not PosAnchorEnd && v.Width is not DimFill).ToList ();
                }
                else
                {
                    subviews = us.Subviews.Where (v => v.Y is not PosAnchorEnd && v.Height is not DimFill).ToList ();
                }

                for (var i = 0; i < subviews.Count; i++)
                {
                    View v = subviews [i];

                    int size = dimension == Dimension.Width ? v.Frame.X + v.Frame.Width : v.Frame.Y + v.Frame.Height;

                    if (size > subviewsSize)
                    {
                        subviewsSize = size;
                    }
                }

                if (dimension == Dimension.Width)
                {
                    subviews = us.Subviews.Where (v => v.X is PosAnchorEnd).ToList ();
                }
                else
                {
                    subviews = us.Subviews.Where (v => v.Y is PosAnchorEnd).ToList ();
                }

                int maxAnchorEnd = 0;
                for (var i = 0; i < subviews.Count; i++)
                {
                    View v = subviews [i];
                    maxAnchorEnd = dimension == Dimension.Width ? v.Frame.Width : v.Frame.Height;
                }

                subviewsSize += maxAnchorEnd;


                if (dimension == Dimension.Width)
                {
                    subviews = us.Subviews.Where (v => v.Width is DimFill).ToList ();
                }
                else
                {
                    subviews = us.Subviews.Where (v => v.Height is DimFill).ToList ();
                }

                for (var i = 0; i < subviews.Count; i++)
                {
                    View v = subviews [i];

                    if (dimension == Dimension.Width)
                    {
                        v.SetRelativeLayout (new Size (autoMin - subviewsSize, 0));
                    }
                    else
                    {
                        v.SetRelativeLayout (new Size (0, autoMin - subviewsSize));
                    }
                }

            }
        }

        // All sizes here are content-relative; ignoring adornments.
        // We take the largest of text and content.
        int max = int.Max (textSize, subviewsSize);

        // And, if min: is set, it wins if larger
        max = int.Max (max, autoMin);

        // Factor in adornments
        Thickness thickness = us.GetAdornmentsThickness ();

        max += dimension switch
               {
                   Dimension.Width => thickness.Horizontal,
                   Dimension.Height => thickness.Vertical,
                   Dimension.None => 0,
                   _ => throw new ArgumentOutOfRangeException (nameof (dimension), dimension, null)
               };

        return int.Min (max, MaximumContentDim?.GetAnchor (superviewContentSize) ?? max);
    }

    internal override bool ReferencesOtherViews ()
    {
        // BUGBUG: This is not correct. _contentSize may be null.
        return false; //_style.HasFlag (DimAutoStyle.Content);
    }

    /// <inheritdoc/>
    public override bool Equals (object? other)
    {
        if (other is not DimAuto auto)
        {
            return false;
        }

        return auto.MinimumContentDim == MinimumContentDim &&
               auto.MaximumContentDim == MaximumContentDim &&
               auto.Style == Style;
    }

    /// <inheritdoc/>
    public override int GetHashCode ()
    {
        return HashCode.Combine (MinimumContentDim, MaximumContentDim, Style);
    }

}