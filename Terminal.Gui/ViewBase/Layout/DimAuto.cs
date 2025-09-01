#nullable enable
using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

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
/// <param name="MaximumContentDim">The maximum dimension the View's ContentSize will be fit to.</param>
/// <param name="MinimumContentDim">The minimum dimension the View's ContentSize will be constrained to.</param>
/// <param name="Style">The <see cref="DimAutoStyle"/> of the <see cref="DimAuto"/>.</param>
public record DimAuto (Dim? MaximumContentDim, Dim? MinimumContentDim, DimAutoStyle Style) : Dim
{
    /// <inheritdoc/>
    public override string ToString () { return $"Auto({Style},{MinimumContentDim},{MaximumContentDim})"; }

    /// <inheritdoc />
    internal override int GetAnchor (int size) => 0;

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        var textSize = 0;
        var maxCalculatedSize = 0;

        int autoMin = MinimumContentDim?.GetAnchor (superviewContentSize) ?? 0;
        int screenX4 = dimension == Dimension.Width ? Application.Screen.Width * 4 : Application.Screen.Height * 4;
        int autoMax = MaximumContentDim?.GetAnchor (superviewContentSize) ?? screenX4;

        Debug.WriteLineIf (autoMin > autoMax, "MinimumContentDim must be less than or equal to MaximumContentDim.");

        if (Style.FastHasFlags (DimAutoStyle.Text))
        {
            if (dimension == Dimension.Width)
            {
                if (us.TextFormatter.ConstrainToWidth is null)
                {
                    // Set BOTH width and height (by setting Size). We do this because we will be called again, next
                    // for Dimension.Height. We need to know the width to calculate the height.
                    us.TextFormatter.ConstrainToSize = us.TextFormatter.FormatAndGetSize (new (int.Min (autoMax, screenX4), screenX4));
                }

                textSize = us.TextFormatter.ConstrainToWidth ?? 0;
            }
            else
            {
                if (us.TextFormatter.ConstrainToHeight is null)
                {
                    // Set just the height. It is assumed that the width has already been set.
                    // TODO: There may be cases where the width is not set. We may need to set it here.
                    textSize = us.TextFormatter.FormatAndGetSize (new (us.TextFormatter.ConstrainToWidth ?? screenX4, int.Min (autoMax, screenX4))).Height;
                    us.TextFormatter.ConstrainToHeight = textSize;
                }
                else
                {
                    textSize = us.TextFormatter.ConstrainToHeight.Value;
                }
            }
        }

        List<View> viewsNeedingLayout = new ();

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
                // TOOD: All the below is a naive implementation. It may be possible to optimize this.

                List<View> includedSubViews = us.SubViews.Snapshot ().ToList ();

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
                    notDependentSubViews = includedSubViews.Where (
                                                                   v => v.Width is { }
                                                                        && (v.X is PosAbsolute or PosFunc
                                                                            || v.Width is DimAuto
                                                                                          or DimAbsolute
                                                                                          or DimFunc) // BUGBUG: We should use v.X.Has and v.Width.Has?
                                                                        && !v.X.Has<PosAnchorEnd> (out _)
                                                                        && !v.X.Has<PosAlign> (out _)
                                                                        && !v.X.Has<PosCenter> (out _)
                                                                        && !v.Width.Has<DimFill> (out _)
                                                                        && !v.Width.Has<DimPercent> (out _)
                                                                  )
                                                           .ToList ();
                }
                else
                {
                    notDependentSubViews = includedSubViews.Where (
                                                                   v => v.Height is { }
                                                                        && (v.Y is PosAbsolute or PosFunc
                                                                            || v.Height is DimAuto
                                                                                           or DimAbsolute
                                                                                           or DimFunc) // BUGBUG: We should use v.Y.Has and v.Height.Has?
                                                                        && !v.Y.Has<PosAnchorEnd> (out _)
                                                                        && !v.Y.Has<PosAlign> (out _)
                                                                        && !v.Y.Has<PosCenter> (out _)
                                                                        && !v.Height.Has<DimFill> (out _)
                                                                        && !v.Height.Has<DimPercent> (out _)
                                                                  )
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
                        int width = v.Width!.Calculate (0, superviewContentSize, v, dimension);
                        size = v.X.GetAnchor (0) + width;

                    }
                    else
                    {
                        int height = v.Height!.Calculate (0, superviewContentSize, v, dimension);
                        size = v.Y!.GetAnchor (0) + height;
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
                        int width = v.Width!.Calculate (0, screenX4, v, dimension);
                        maxCentered = v.X.GetAnchor (0) + width;
                    }
                    else
                    {
                        int height = v.Height!.Calculate (0, screenX4, v, dimension);
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
                List<int> groupIds = includedSubViews.Select (
                                                              v =>
                                                              {
                                                                  return dimension switch
                                                                  {
                                                                      Dimension.Width when v.X.Has<PosAlign> (out PosAlign posAlign) =>
                                                                              ((PosAlign)posAlign).GroupId,
                                                                      Dimension.Height when v.Y.Has<PosAlign> (out PosAlign posAlign) =>
                                                                              ((PosAlign)posAlign).GroupId,
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
                    View v = anchoredSubViews [i];

                    // Need to set the relative layout for PosAnchorEnd subviews to calculate the size
                    // TODO: Figure out a way to not have Calculate change the state of subviews (calling SRL).
                    if (dimension == Dimension.Width)
                    {
                        v.SetRelativeLayout (new (maxCalculatedSize, screenX4));
                    }
                    else
                    {
                        v.SetRelativeLayout (new (screenX4, maxCalculatedSize));
                    }

                    maxAnchorEnd = dimension == Dimension.Width
                                       ? v.X.GetAnchor (maxCalculatedSize + v.Frame.Width)
                                       : v.Y.GetAnchor (maxCalculatedSize + v.Frame.Height);
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
                    // TODO: Figure out a way to not have Calculate change the state of subviews (calling SRL).
                    int maxPosView = dimension == Dimension.Width
                                         ? v.Frame.X + v.Width!.Calculate (0, maxCalculatedSize, v, dimension)
                                         : v.Frame.Y + v.Height!.Calculate (0, maxCalculatedSize, v, dimension);

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
                    dimViewSubViews = includedSubViews.Where (v => v.Width is { } && v.Width.Has<DimView> (out _)).ToList ();
                }
                else
                {
                    dimViewSubViews = includedSubViews.Where (v => v.Height is { } && v.Height.Has<DimView> (out _)).ToList ();
                }

                for (var i = 0; i < dimViewSubViews.Count; i++)
                {
                    View v = dimViewSubViews [i];

                    // BUGBUG: The order may not be correct. May need to call TopologicalSort?
                    // TODO: Figure out a way to not have Calculate change the state of subviews (calling SRL).
                    int maxDimView = dimension == Dimension.Width
                                         ? v.Frame.X + v.Width!.Calculate (0, maxCalculatedSize, v, dimension)
                                         : v.Frame.Y + v.Height!.Calculate (0, maxCalculatedSize, v, dimension);

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
                    dimAutoSubViews = includedSubViews.Where (v => v.Width is { } && v.Width.Has<DimAuto> (out _)).ToList ();
                }
                else
                {
                    dimAutoSubViews = includedSubViews.Where (v => v.Height is { } && v.Height.Has<DimAuto> (out _)).ToList ();
                }

                for (var i = 0; i < dimAutoSubViews.Count; i++)
                {
                    View v = dimAutoSubViews [i];

                    int maxDimAuto = dimension == Dimension.Width
                                         ? v.Frame.X + v.Width!.Calculate (0, maxCalculatedSize, v, dimension)
                                         : v.Frame.Y + v.Height!.Calculate (0, maxCalculatedSize, v, dimension);

                    if (maxDimAuto > maxCalculatedSize)
                    {
                        maxCalculatedSize = maxDimAuto;
                    }
                }

                #endregion


                #region DimFill

                //// [ ] DimFill      - Dimension is internally calculated

                //List<View> DimFillSubViews;

                //if (dimension == Dimension.Width)
                //{
                //    DimFillSubViews = includedSubViews.Where (v => v.Width is { } && v.Width.Has<DimFill> (out _)).ToList ();
                //}
                //else
                //{
                //    DimFillSubViews = includedSubViews.Where (v => v.Height is { } && v.Height.Has<DimFill> (out _)).ToList ();
                //}

                //for (var i = 0; i < DimFillSubViews.Count; i++)
                //{
                //    View v = DimFillSubViews [i];

                //    if (dimension == Dimension.Width)
                //    {
                //        v.SetRelativeLayout (new (maxCalculatedSize, 0));
                //    }
                //    else
                //    {
                //        v.SetRelativeLayout (new (0, maxCalculatedSize));
                //    }

                //    int maxDimFill = dimension == Dimension.Width ? v.Frame.X + v.Frame.Width : v.Frame.Y + v.Frame.Height;

                //    if (maxDimFill > maxCalculatedSize)
                //    {
                //        maxCalculatedSize = maxDimFill;
                //    }
                //}

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
}
