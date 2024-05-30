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
        int autoMax = MaximumContentDim?.GetAnchor (superviewContentSize) ?? int.MaxValue;

        if (Style.FastHasFlags (DimAutoStyle.Text))
        {
            textSize = int.Max (autoMin, dimension == Dimension.Width ? us.TextFormatter.Size.Width : us.TextFormatter.Size.Height);
        }

        if (Style.FastHasFlags (DimAutoStyle.Content))
        {
            if (!us.ContentSizeTracksViewport)
            {
                // ContentSize was explicitly set. Ignore subviews.
                subviewsSize = dimension == Dimension.Width ? us.GetContentSize ().Width : us.GetContentSize ().Height;
            }
            else
            {
                // ContentSize was NOT explicitly set. Use subviews to determine size.

                // TODO: This whole body of code is a WIP (for https://github.com/gui-cs/Terminal.Gui/pull/3451).
                subviewsSize = 0;

                List<View> includedSubviews = us.Subviews.ToList ();//.Where (v => !v.ExcludeFromLayout).ToList ();
                List<View> subviews;

                #region Not Anchored and Are Not Dependent
                // Start with subviews that are not anchored to the end, aligned, or dependent on content size
                // [x] PosAnchorEnd
                // [x] PosAlign
                // [ ] PosCenter
                // [ ] PosPercent
                // [ ] PosView
                // [ ] PosFunc
                // [x] DimFill
                // [ ] DimPercent
                // [ ] DimFunc
                // [ ] DimView
                if (dimension == Dimension.Width)
                {
                    subviews = includedSubviews.Where (v => v.X is not PosAnchorEnd
                                                           && v.X is not PosAlign
                                                            // && v.X is not PosCenter
                                                            && v.Width is not DimAuto
                                                           && v.Width is not DimFill).ToList ();
                }
                else
                {
                    subviews = includedSubviews.Where (v => v.Y is not PosAnchorEnd
                                                           && v.Y is not PosAlign
                                                            // && v.Y is not PosCenter
                                                            && v.Height is not DimAuto
                                                           && v.Height is not DimFill).ToList ();
                }

                for (var i = 0; i < subviews.Count; i++)
                {
                    View v = subviews [i];

                    int size = dimension == Dimension.Width ? v.Frame.X + v.Frame.Width : v.Frame.Y + v.Frame.Height;

                    if (size > subviewsSize)
                    {
                        // BUGBUG: Should we break here? Or choose min/max?
                        subviewsSize = size;
                    }
                }
                #endregion Not Anchored and Are Not Dependent

                #region Anchored
                // Now, handle subviews that are anchored to the end
                // [x] PosAnchorEnd
                if (dimension == Dimension.Width)
                {
                    subviews = includedSubviews.Where (v => v.X is PosAnchorEnd).ToList ();
                }
                else
                {
                    subviews = includedSubviews.Where (v => v.Y is PosAnchorEnd).ToList ();
                }

                int maxAnchorEnd = 0;
                for (var i = 0; i < subviews.Count; i++)
                {
                    View v = subviews [i];
                    maxAnchorEnd = dimension == Dimension.Width ? v.Frame.Width : v.Frame.Height;
                }

                subviewsSize += maxAnchorEnd;
                #endregion Anchored

                #region Aligned

                // Now, handle subviews that are anchored to the end
                // [x] PosAnchorEnd
                int maxAlign = 0;
                if (dimension == Dimension.Width)
                {
                    // Use Linq to get a list of distinct GroupIds from the subviews
                    List<int> groupIds = includedSubviews.Select (v => v.X is PosAlign posAlign ? posAlign.GroupId : -1).Distinct ().ToList ();

                    foreach (var groupId in groupIds)
                    {
                        List<int> dimensionsList = new ();

                        // PERF: If this proves a perf issue, consider caching a ref to this list in each item
                        List<PosAlign?> posAlignsInGroup = includedSubviews.Where (
                            v =>
                            {
                                return dimension switch
                                {
                                    Dimension.Width when v.X is PosAlign alignX => alignX.GroupId == groupId,
                                    Dimension.Height when v.Y is PosAlign alignY => alignY.GroupId == groupId,
                                    _ => false
                                };
                            })
                            .Select (v => dimension == Dimension.Width ? v.X as PosAlign : v.Y as PosAlign)
                            .ToList ();

                        if (posAlignsInGroup.Count == 0)
                        {
                            continue;
                        }

                        maxAlign = posAlignsInGroup [0].CalculateMinDimension (groupId, includedSubviews, dimension);
                    }
                }
                else
                {
                    subviews = includedSubviews.Where (v => v.Y is PosAlign).ToList ();
                }

                subviewsSize = int.Max (subviewsSize, maxAlign);
                #endregion Aligned


                #region Auto

                if (dimension == Dimension.Width)
                {
                    subviews = includedSubviews.Where (v => v.Width is DimAuto).ToList ();
                }
                else
                {
                    subviews = includedSubviews.Where (v => v.Height is DimAuto).ToList ();
                }

                int maxAuto = 0;
                for (var i = 0; i < subviews.Count; i++)
                {
                    View v = subviews [i];

                    if (dimension == Dimension.Width)
                    {
                        v.SetRelativeLayout (new Size (autoMax - subviewsSize, 0));
                    }
                    else
                    {
                        v.SetRelativeLayout (new Size (0, autoMax - subviewsSize));
                    }
                    maxAuto = dimension == Dimension.Width ? v.Frame.Width : v.Frame.Height;
                }

                subviewsSize += maxAuto;

                #endregion Auto

                //#region Center
                //// Now, handle subviews that are Centered
                //if (dimension == Dimension.Width)
                //{
                //    subviews = us.Subviews.Where (v => v.X is PosCenter).ToList ();
                //}
                //else
                //{
                //    subviews = us.Subviews.Where (v => v.Y is PosCenter).ToList ();
                //}

                //int maxCenter = 0;
                //for (var i = 0; i < subviews.Count; i++)
                //{
                //    View v = subviews [i];
                //    maxCenter = dimension == Dimension.Width ? v.Frame.Width : v.Frame.Height;
                //}

                //subviewsSize += maxCenter;
                //#endregion Center

                #region Are Dependent
                // Now, go back to those that are dependent on content size
                // [x] DimFill
                // [ ] DimPercent
                if (dimension == Dimension.Width)
                {
                    subviews = includedSubviews.Where (v => v.Width is DimFill).ToList ();
                }
                else
                {
                    subviews = includedSubviews.Where (v => v.Height is DimFill).ToList ();
                }

                int maxFill = 0;
                for (var i = 0; i < subviews.Count; i++)
                {
                    View v = subviews [i];

                    if (dimension == Dimension.Width)
                    {
                        v.SetRelativeLayout (new Size (autoMax - subviewsSize, 0));
                    }
                    else
                    {
                        v.SetRelativeLayout (new Size (0, autoMax - subviewsSize));
                    }
                    maxFill = dimension == Dimension.Width ? v.Frame.Width : v.Frame.Height;
                }

                subviewsSize += maxFill;
                #endregion Are Dependent
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

        return int.Min (max, autoMax);
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