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

        List<View> viewsNeedingLayout = [];

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
                // TODO: All the below is a naive implementation. It may be possible to optimize this.

                List<View> includedSubViews = us.InternalSubViews.ToList ();

                // If [x] it can cause `us.ContentSize` to change.
                // If [ ] it doesn't need special processing for us to determine `us.ContentSize`.

                // -------------------- Pos types that are dependent on `us.SubViews`
                // [ ] PosAlign     - Position is dependent on other views with `GroupId` AND `us.ContentSize`
                // [x] PosView      - Position is dependent on `subview.Target` - it can cause a change in `us.ContentSize`
                // [x] PosCombine   - Position is dependent if `Pos.Has [one of the above]` - it can cause a change in `us.ContentSize`

                // -------------------- Pos types that are dependent on `us.ContentSize`
                // [ ] PosAlign     - Position is dependent on other views with `GroupId` AND `us.ContentSize`
                // [x] PosAnchorEnd - Position is dependent on `us.ContentSize` AND `subview.Frame` - it can cause a change in `us.ContentSize`
                // [ ] PosCenter    - Position is dependent `us.ContentSize` AND `subview.Frame` - 
                // [ ] PosPercent   - Position is dependent `us.ContentSize` - Will always be 0 if there is no other content that makes the superview have a size.
                // [x] PosCombine   - Position is dependent if `Pos.Has [one of the above]` - it can cause a change in `us.ContentSize`

                // -------------------- Pos types that are not dependent on either `us.SubViews` or `us.ContentSize`
                // [ ] PosAbsolute  - Position is fixed.
                // [ ] PosFunc      - Position is internally calculated.

                // -------------------- Dim types that are dependent on `us.SubViews`
                // [x] DimView      - Dimension is dependent on `subview.Target`
                // [x] DimCombine   - Dimension is dependent if `Dim.Has [one of the above]` - it can cause a change in `us.ContentSize`

                // -------------------- Dim types that are dependent on `us.ContentSize`
                // [ ] DimFill      - Dimension is dependent on `us.ContentSize` - Will always be 0 if there is no other content that makes the superview have a size.
                //                    Exception: If DimFill.To is set, it's dependent on another view's position and contributes to auto-sizing.
                // [ ] DimPercent   - Dimension is dependent on `us.ContentSize` - Will always be 0 if there is no other content that makes the superview have a size.
                // [ ] DimCombine   - Dimension is dependent if `Dim.Has [one of the above]`

                // -------------------- Dim types that are not dependent on either `us.SubViews` or `us.ContentSize`
                // [ ] DimAuto      - Dimension is internally calculated
                // [ ] DimAbsolute  - Dimension is fixed
                // [ ] DimFunc      - Dimension is internally calculated

                // ======================================================
                // Do the easy stuff first - subviews whose position and size are not dependent on other views or content size
                // ======================================================
                // [ ] PosAbsolute  - Position is fixed.
                // [ ] PosFunc      - Position is internally calculated
                // [ ] DimAuto      - Dimension is internally calculated
                // [ ] DimAbsolute  - Dimension is fixed
                // [ ] DimFunc      - Dimension is internally calculated
                List<View> notDependentSubViews;

                if (dimension == Dimension.Width)
                {
                    notDependentSubViews = includedSubViews
                                           .Where (v =>
                                                       (v.X is PosAbsolute or PosFunc
                                                        || v.Width is DimAuto or DimAbsolute or DimFunc) // BUGBUG: We should use v.X.Has and v.Width.Has?
                                                       && !v.X.DependsOnSuperViewContentSize
                                                       && !v.Width.DependsOnSuperViewContentSize)
                                           .ToList ();
                }
                else
                {
                    notDependentSubViews = includedSubViews
                                           .Where (v =>
                                                       (v.Y is PosAbsolute or PosFunc
                                                        || v.Height is DimAuto or DimAbsolute or DimFunc) // BUGBUG: We should use v.Y.Has and v.Height.Has?
                                                       && !v.Y.DependsOnSuperViewContentSize
                                                       && !v.Height.DependsOnSuperViewContentSize)
                                           .ToList ();
                }

                foreach (View notDependentSubView in notDependentSubViews)
                {
                    notDependentSubView.SetRelativeLayout (us.GetContentSize ());
                }

                for (var i = 0; i < notDependentSubViews.Count; i++)
                {
                    View v = notDependentSubViews [i];

                    var size = 0;

                    if (dimension == Dimension.Width)
                    {
                        int width = v.Width.Calculate (0, superviewContentSize, v, dimension);
                        size = v.X.GetAnchor (0) + width;
                    }
                    else
                    {
                        int height = v.Height.Calculate (0, superviewContentSize, v, dimension);
                        size = v.Y.GetAnchor (0) + height;
                    }

                    if (size > maxCalculatedSize)
                    {
                        maxCalculatedSize = size;
                    }
                }

                // ************** We now have some idea of `us.ContentSize` ***************

                #region Centered

                // [ ] PosCenter    - Position is dependent `us.ContentSize` AND `subview.Frame`
                List<View> centeredSubViews;

                if (dimension == Dimension.Width)
                {
                    centeredSubViews = us.InternalSubViews.Where (v => v.X.Has<PosCenter> (out _)).ToList ();
                }
                else
                {
                    centeredSubViews = us.InternalSubViews.Where (v => v.Y.Has<PosCenter> (out _)).ToList ();
                }

                viewsNeedingLayout.AddRange (centeredSubViews);

                var maxCentered = 0;

                for (var i = 0; i < centeredSubViews.Count; i++)
                {
                    View v = centeredSubViews [i];

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

                #region Percent

                // [ ] DimPercent   - Dimension is dependent on `us.ContentSize`
                // No need to do anything.

                #endregion Percent

                #region Aligned

                // [ ] PosAlign     - Position is dependent on other views with `GroupId` AND `us.ContentSize`
                var maxAlign = 0;

                // Use Linq to get a list of distinct GroupIds from the subviews
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
                    // PERF: If this proves a perf issue, consider caching a ref to this list in each item
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

                #region Anchored

                // [x] PosAnchorEnd - Position is dependent on `us.ContentSize` AND `subview.Frame` 
                List<View> anchoredSubViews;

                if (dimension == Dimension.Width)
                {
                    anchoredSubViews = includedSubViews.Where (v => v.X.Has<PosAnchorEnd> (out _)).ToList ();
                }
                else
                {
                    anchoredSubViews = includedSubViews.Where (v => v.Y.Has<PosAnchorEnd> (out _)).ToList ();
                }

                viewsNeedingLayout.AddRange (anchoredSubViews);

                var maxAnchorEnd = 0;

                for (var i = 0; i < anchoredSubViews.Count; i++)
                {
                    View anchoredSubView = anchoredSubViews [i];

                    // Need to set the relative layout for PosAnchorEnd subviews to calculate the size
                    // TODO: Figure out a way to not have to calculate change the state of subviews (calling SRL).
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

                #endregion Anchored

                #region PosView

                // [x] PosView      - Position is dependent on `subview.Target` - it can cause a change in `us.ContentSize`
                List<View> posViewSubViews;

                if (dimension == Dimension.Width)
                {
                    posViewSubViews = includedSubViews.Where (v => v.X.Has<PosView> (out _)).ToList ();
                }
                else
                {
                    posViewSubViews = includedSubViews.Where (v => v.Y.Has<PosView> (out _)).ToList ();
                }

                for (var i = 0; i < posViewSubViews.Count; i++)
                {
                    View v = posViewSubViews [i];

                    // BUGBUG: The order may not be correct. May need to call TopologicalSort?
                    // TODO: Figure out a way to not have to Calculate change the state of subviews (calling SRL).
                    int maxPosView = dimension == Dimension.Width
                                         ? v.Frame.X + v.Width.Calculate (0, maxCalculatedSize, v, dimension)
                                         : v.Frame.Y + v.Height.Calculate (0, maxCalculatedSize, v, dimension);

                    if (maxPosView > maxCalculatedSize)
                    {
                        maxCalculatedSize = maxPosView;
                    }
                }

                #endregion PosView

                // [x] PosCombine   - Position is dependent if `Pos.Has ([one of the above]` - it can cause a change in `us.ContentSize`

                #region DimView

                // [x] DimView      - Dimension is dependent on `subview.Target` - it can cause a change in `us.ContentSize`
                List<View> dimViewSubViews;

                if (dimension == Dimension.Width)
                {
                    dimViewSubViews = includedSubViews.Where (v => v.Width.Has<DimView> (out _)).ToList ();
                }
                else
                {
                    dimViewSubViews = includedSubViews.Where (v => v.Height.Has<DimView> (out _)).ToList ();
                }

                for (var i = 0; i < dimViewSubViews.Count; i++)
                {
                    View v = dimViewSubViews [i];

                    // BUGBUG: The order may not be correct. May need to call TopologicalSort?
                    // TODO: Figure out a way to not have to Calculate change the state of subviews (calling SRL).
                    int maxDimView = dimension == Dimension.Width
                                         ? v.Frame.X + v.Width.Calculate (0, maxCalculatedSize, v, dimension)
                                         : v.Frame.Y + v.Height.Calculate (0, maxCalculatedSize, v, dimension);

                    if (maxDimView > maxCalculatedSize)
                    {
                        maxCalculatedSize = maxDimView;
                    }
                }

                #endregion DimView

                #region DimAuto

                // [ ] DimAuto      - Dimension is internally calculated

                List<View> dimAutoSubViews;

                if (dimension == Dimension.Width)
                {
                    dimAutoSubViews = includedSubViews.Where (v => v.Width.Has<DimAuto> (out _)).ToList ();
                }
                else
                {
                    dimAutoSubViews = includedSubViews.Where (v => v.Height.Has<DimAuto> (out _)).ToList ();
                }

                for (var i = 0; i < dimAutoSubViews.Count; i++)
                {
                    View v = dimAutoSubViews [i];

                    int maxDimAuto = dimension == Dimension.Width
                                         ? v.Frame.X + v.Width.Calculate (0, maxCalculatedSize, v, dimension)
                                         : v.Frame.Y + v.Height.Calculate (0, maxCalculatedSize, v, dimension);

                    if (maxDimAuto > maxCalculatedSize)
                    {
                        maxCalculatedSize = maxDimAuto;
                    }
                }

                #endregion

                #region DimFill

                // DimFill subviews contribute to auto-sizing only if they have MinimumContentDim or To set
                List<View> contributingDimFillSubViews;

                if (dimension == Dimension.Width)
                {
                    contributingDimFillSubViews = us.InternalSubViews
                                                    .Where (v => v.Width.Has<DimFill> (out _) && v.Width.CanContributeToAutoSizing)
                                                    .ToList ();
                }
                else
                {
                    contributingDimFillSubViews = us.InternalSubViews
                                                    .Where (v => v.Height.Has<DimFill> (out _) && v.Height.CanContributeToAutoSizing)
                                                    .ToList ();
                }

                // Process DimFill views with MinimumContentDim or To
                for (var i = 0; i < contributingDimFillSubViews.Count; i++)
                {
                    View dimFillSubView = contributingDimFillSubViews [i];
                    DimFill? dimFill = dimension == Dimension.Width ? dimFillSubView.Width as DimFill : dimFillSubView.Height as DimFill;

                    if (dimFill?.MinimumContentDim is { })
                    {
                        // This DimFill has a minimum - it contributes to auto-sizing
                        int minSize = dimFill.MinimumContentDim.Calculate (0, maxCalculatedSize, dimFillSubView, dimension);
                        int positionOffset = dimension == Dimension.Width ? dimFillSubView.Frame.X : dimFillSubView.Frame.Y;
                        int totalSize = positionOffset + minSize;

                        if (totalSize > maxCalculatedSize)
                        {
                            maxCalculatedSize = totalSize;
                        }
                    }

                    if (dimFill?.To is { })
                    {
                        // This DimFill has a To view - it contributes to auto-sizing
                        // The SuperView needs to be large enough to contain both the dimFillSubView and the To view
                        int dimFillPos = dimension == Dimension.Width ? dimFillSubView.Frame.X : dimFillSubView.Frame.Y;
                        int toViewPos = dimension == Dimension.Width ? dimFill.To.Frame.X : dimFill.To.Frame.Y;
                        int toViewSize = dimension == Dimension.Width ? dimFill.To.Frame.Width : dimFill.To.Frame.Height;
                        int totalSize = int.Max (dimFillPos, toViewPos + toViewSize);

                        if (totalSize > maxCalculatedSize)
                        {
                            maxCalculatedSize = totalSize;
                        }
                    }
                }

                #endregion
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
