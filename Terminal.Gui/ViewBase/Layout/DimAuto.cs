namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a dimension that automatically sizes the view to fit all the view's Content, SubViews, and/or Text.
/// </summary>
/// <remarks>
///     <para>
///         See <see cref="DimAutoStyle"/>.
///     </para>
///     <para>
///         See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for comprehensive documentation including
///         non-trivial usage patterns.
///     </para>
///     <para>
///         SubViews that use <see cref="DimFill"/> do not contribute to the auto-sizing calculation unless
///         <see cref="DimFill.MinimumContentDim"/> or <see cref="DimFill.To"/> is specified. Without either, a
///         <see cref="DimFill"/> SubView will receive a size of 0 because the SuperView has no content-based size
///         to fill against. Use <see cref="Dim.Fill(Dim, Dim?)"/> with a <c>minimumContentDim</c> parameter or
///         <see cref="Dim.Fill(View)"/> with a <c>to</c> parameter to ensure the SubView contributes to the
///         auto-sizing calculation.
///     </para>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
///     </para>
/// </remarks>
/// <param name="MaximumContentDim">The maximum dimension the View's ContentSize will be fit to.</param>
/// <param name="MinimumContentDim">The minimum dimension the View's ContentSize will be constrained to.</param>
/// <param name="Style">The <see cref="DimAutoStyle"/> of the <see cref="DimAuto"/>.</param>
public record DimAuto (Dim? MaximumContentDim, Dim? MinimumContentDim, DimAutoStyle Style) : Dim
{
    /// <inheritdoc/>
    public override string ToString () => $"Auto({Style},{MinimumContentDim},{MaximumContentDim})";

    /// <inheritdoc/>
    internal override int GetAnchor (int size) => 0;

    /// <inheritdoc/>
    internal override bool IsFixed => true;

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        var textSize = 0;
        var maxCalculatedSize = 0;

        // 2048 x 2048 supports unit testing where no App is running.
        Size screenSize = us.App?.Screen.Size ?? new Size (2048, 2048);
        int autoMin = MinimumContentDim?.GetAnchor (superviewContentSize) ?? 0;
        int screenX4 = dimension == Dimension.Width ? screenSize.Width * 4 : screenSize.Height * 4;
        int autoMax = MaximumContentDim?.GetAnchor (superviewContentSize) ?? screenX4;

        //Debug.WriteLineIf (autoMin > autoMax, "MinimumContentDim must be less than or equal to MaximumContentDim.");

        if (Style.FastHasFlags (DimAutoStyle.Text))
        {
            if (dimension == Dimension.Width)
            {
                if (us.TextFormatter.ConstrainToWidth is null)
                {
                    // Set BOTH width and height (by setting Size). We do this because we will be called again, next
                    // for Dimension.Height. We need to know the width to calculate the height.
                    us.TextFormatter.ConstrainToSize = us.TextFormatter.FormatAndGetSize (new Size (int.Min (autoMax, screenX4), screenX4));
                }

                textSize = us.TextFormatter.ConstrainToWidth ?? 0;
            }
            else
            {
                // For height, we need to make sure width has been calculated.
                if (us.TextFormatter.ConstrainToHeight is null)
                {
                    int width = int.Min (MaximumContentDim?.GetAnchor (superviewContentSize) ?? screenX4, screenSize.Width * 4);

                    if (us.TextFormatter.ConstrainToWidth is null)
                    {
                        width = us.TextFormatter.FormatAndGetSize (new Size (us.Viewport.Width, screenX4)).Width;
                    }

                    textSize = us.TextFormatter.FormatAndGetSize (new Size (us.TextFormatter.ConstrainToWidth ?? width, screenX4)).Height;
                    us.TextFormatter.ConstrainToHeight = textSize;
                }
                else
                {
                    textSize = us.TextFormatter.ConstrainToHeight.Value;
                }
            }
        }

        if (Style.FastHasFlags (DimAutoStyle.Content))
        {
            maxCalculatedSize = textSize;

            if (us is { ContentSizeTracksViewport: false, InternalSubViews.Count: 0 })
            {
                // ContentSize was explicitly set. Use `us.ContentSize` to determine size.
                maxCalculatedSize = dimension == Dimension.Width ? us.GetContentSize ().Width : us.GetContentSize ().Height;
            }
            else
            {
                List<View> includedSubViews = us.InternalSubViews.ToList ();

                foreach (View notDependentSubView in GetViewsThatMatch (us.InternalSubViews,
                                                                        v => dimension == Dimension.Width
                                                                                 ? (v.X.IsFixed || v.Width.IsFixed)
                                                                                   && !v.X.DependsOnSuperViewContentSize
                                                                                   && !v.Width.DependsOnSuperViewContentSize
                                                                                 : (v.Y.IsFixed || v.Height.IsFixed)
                                                                                   && !v.Y.DependsOnSuperViewContentSize
                                                                                   && !v.Height.DependsOnSuperViewContentSize))
                {
                    notDependentSubView.SetRelativeLayout (us.GetContentSize ());
                    int size;

                    if (dimension == Dimension.Width)
                    {
                        int width = notDependentSubView.Width.Calculate (0, superviewContentSize, notDependentSubView, dimension);
                        size = notDependentSubView.X.GetAnchor (0) + width;
                    }
                    else
                    {
                        int height = notDependentSubView.Height.Calculate (0, superviewContentSize, notDependentSubView, dimension);
                        size = notDependentSubView.Y.GetAnchor (0) + height;
                    }

                    if (size > maxCalculatedSize)
                    {
                        maxCalculatedSize = size;
                    }
                }

                #region Centered

                var maxCentered = 0;

                foreach (View v in GetViewsThatHavePos<PosCenter> (dimension, us.InternalSubViews))
                {
                    if (dimension == Dimension.Width)
                    {
                        int width = v.Width.Calculate (0, screenX4, v, dimension);
                        maxCentered = v.X.GetAnchor (0) + width;
                    }
                    else
                    {
                        int height = v.Height.Calculate (0, screenX4, v, dimension);
                        maxCentered = v.Y.GetAnchor (0) + height;
                    }
                }

                maxCalculatedSize = int.Max (maxCalculatedSize, maxCentered);

                #endregion Centered

                #region Aligned

                var maxAlign = 0;

                // Get distinct GroupIds from the subviews
                List<int> groupIds = includedSubViews.Select (v =>
                                                              {
                                                                  return dimension switch
                                                                         {
                                                                             Dimension.Width when v.X.Has (out PosAlign posAlign) => posAlign.GroupId,
                                                                             Dimension.Height when v.Y.Has (out PosAlign posAlign) => posAlign.GroupId,
                                                                             _ => -1
                                                                         };
                                                              })
                                                     .Distinct ()
                                                     .ToList ();

                foreach (int groupId in groupIds.Where (g => g != -1))
                {
                    List<PosAlign?> posAlignsInGroup = includedSubViews.Where (v => PosAlign.HasGroupId (v, dimension, groupId))
                                                                       .Select (v => dimension == Dimension.Width ? v.X as PosAlign : v.Y as PosAlign)
                                                                       .ToList ();

                    if (posAlignsInGroup.Count == 0)
                    {
                        continue;
                    }

                    maxAlign = PosAlign.CalculateMinDimension (groupId, includedSubViews, dimension);
                }

                maxCalculatedSize = int.Max (maxCalculatedSize, maxAlign);

                #endregion Aligned

                var maxAnchorEnd = 0;

                foreach (View anchoredSubView in GetViewsThatHavePos<PosAnchorEnd> (dimension, us.InternalSubViews))
                {
                    // Need to set the relative layout for PosAnchorEnd subviews to calculate the size
                    if (dimension == Dimension.Width)
                    {
                        anchoredSubView.SetRelativeLayout (new Size (maxCalculatedSize, screenX4));
                    }
                    else
                    {
                        anchoredSubView.SetRelativeLayout (new Size (screenX4, maxCalculatedSize));
                    }

                    maxAnchorEnd = dimension == Dimension.Width
                                       ? anchoredSubView.X.GetAnchor (maxCalculatedSize + anchoredSubView.Frame.Width)
                                       : anchoredSubView.Y.GetAnchor (maxCalculatedSize + anchoredSubView.Frame.Height);
                }

                maxCalculatedSize = Math.Max (maxCalculatedSize, maxAnchorEnd);

                maxCalculatedSize = GetMaxSizePos<PosView> (maxCalculatedSize, dimension, us.InternalSubViews);
                maxCalculatedSize = GetMaxSizeDim<DimView> (maxCalculatedSize, dimension, us.InternalSubViews);
                maxCalculatedSize = GetMaxSizeDim<DimAuto> (maxCalculatedSize, dimension, us.InternalSubViews);

                // DimFill subviews contribute to auto-sizing only if they have MinimumContentDim or To set
                // Process DimFill views that can contribute
                foreach (View dimFillSubView in GetViewsThatHaveDim<DimFill> (dimension, us.InternalSubViews))
                {
                    Dim dimFill = dimension == Dimension.Width ? dimFillSubView.Width : dimFillSubView.Height;

                    // Get the minimum contribution from the Dim itself
                    int minContribution = dimFill.GetMinimumContribution (0, maxCalculatedSize, dimFillSubView, dimension);

                    if (minContribution > 0)
                    {
                        // Add position offset to get total size needed
                        int positionOffset = dimension == Dimension.Width ? dimFillSubView.Frame.X : dimFillSubView.Frame.Y;
                        int totalSize = positionOffset + minContribution;

                        if (totalSize > maxCalculatedSize)
                        {
                            maxCalculatedSize = totalSize;
                        }
                    }

                    // Handle special case for DimFill with To (still needs type-specific logic)
                    if (dimFill is not DimFill dimFillTyped || dimFillTyped.To is null)
                    {
                        continue;
                    }

                    {
                        // The SuperView needs to be large enough to contain both the dimFillSubView and the To view
                        int dimFillPos = dimension == Dimension.Width ? dimFillSubView.Frame.X : dimFillSubView.Frame.Y;
                        int toViewPos = dimension == Dimension.Width ? dimFillTyped.To.Frame.X : dimFillTyped.To.Frame.Y;
                        int toViewSize = dimension == Dimension.Width ? dimFillTyped.To.Frame.Width : dimFillTyped.To.Frame.Height;
                        int totalSize = int.Max (dimFillPos, toViewPos + toViewSize);

                        if (totalSize > maxCalculatedSize)
                        {
                            maxCalculatedSize = totalSize;
                        }
                    }
                }
            }
        }

        // All sizes here are content-relative; ignoring adornments.
        // We take the largest of text and content.
        int max = int.Max (textSize, maxCalculatedSize);

        // And, if min: is set, it wins if larger
        max = int.Max (max, autoMin);

        // And, if max: is set, it wins if smaller
        max = int.Min (max, autoMax);

        Thickness thickness = us.GetAdornmentsThickness ();

        int adornmentThickness = dimension switch
                                 {
                                     Dimension.Width => thickness.Horizontal,
                                     Dimension.Height => thickness.Vertical,
                                     Dimension.None => 0,
                                     _ => throw new ArgumentOutOfRangeException (nameof (dimension), dimension, null)
                                 };

        max += adornmentThickness;

        return max;
    }

    private static List<View> GetViewsThatMatch (IList<View> subViews, Func<View, bool> predicate) => subViews.Where (predicate).ToList ();

    private List<View> GetViewsThatHavePos<TPos> (Dimension dimension, IList<View> subViews) where TPos : Pos =>
        dimension switch
        {
            Dimension.Width => subViews.Where (v => v.X.Has<TPos> (out _)).ToList (),
            _ => subViews.Where (v => v.Y.Has<TPos> (out _)).ToList ()
        };

    private List<View> GetViewsThatHaveDim<TDim> (Dimension dimension, IList<View> subViews) where TDim : Dim =>
        dimension switch
        {
            Dimension.Width => subViews.Where (v => v.Width.Has<TDim> (out _)).ToList (),
            _ => subViews.Where (v => v.Height.Has<TDim> (out _)).ToList ()
        };

    private int GetMaxSizePos<TPos> (int max, Dimension dimension, IList<View> views) where TPos : Pos
    {
        foreach (View v in GetViewsThatHavePos<TPos> (dimension, views))
        {
            int newMax = dimension == Dimension.Width
                             ? v.Frame.X + v.Width.Calculate (0, max, v, dimension)
                             : v.Frame.Y + v.Height.Calculate (0, max, v, dimension);

            if (newMax > max)
            {
                max = newMax;
            }
        }

        return max;
    }

    private int GetMaxSizeDim<TDim> (int max, Dimension dimension, IList<View> views) where TDim : Dim
    {
        foreach (View v in GetViewsThatHaveDim<TDim> (dimension, views))
        {
            int newMax = dimension == Dimension.Width
                             ? v.Frame.X + v.Width.Calculate (0, max, v, dimension)
                             : v.Frame.Y + v.Height.Calculate (0, max, v, dimension);

            if (newMax > max)
            {
                max = newMax;
            }
        }

        return max;
    }

    /// <inheritdoc/>
    protected override bool HasInner<TDim> (out TDim dim)
    {
        if (MinimumContentDim is { } && MinimumContentDim.Has (out dim))
        {
            return true;
        }

        if (MaximumContentDim is { } && MaximumContentDim.Has (out dim))
        {
            return true;
        }

        dim = null!;

        return false;
    }
}
