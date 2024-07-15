#nullable enable
using System.Diagnostics;
using System.Drawing;

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
        var maxCalculatedSize = 0;

        int autoMin = MinimumContentDim?.GetAnchor (superviewContentSize) ?? 0;
        int screen = dimension == Dimension.Width ? Application.Screen.Width * 4 : Application.Screen.Height * 4;
        int autoMax = MaximumContentDim?.GetAnchor (superviewContentSize) ?? screen;

        if (Style.FastHasFlags (DimAutoStyle.Text))
        {
            if (dimension == Dimension.Width)
            {
                us.TextFormatter.Size = new (autoMax, 2048);
                textSize = us.TextFormatter.FormatAndGetSize ().Width;
                us.TextFormatter.Size = new Size (textSize, 2048);
            }
            else
            {
                if (us.TextFormatter.Size.Width == 0)
                {
                    us.TextFormatter.Size = us.TextFormatter.GetAutoSize ();
                }
                textSize = us.TextFormatter.FormatAndGetSize ().Height;
                us.TextFormatter.Size = us.TextFormatter.Size with { Height = textSize };
            }
        }

        if (Style.FastHasFlags (DimAutoStyle.Content))
        {
            if (!us.ContentSizeTracksViewport)
            {
                // ContentSize was explicitly set. Use `us.ContentSize` to determine size.
                maxCalculatedSize = dimension == Dimension.Width ? us.GetContentSize ().Width : us.GetContentSize ().Height;
            }
            else
            {
                maxCalculatedSize = textSize;
                // ContentSize was NOT explicitly set. Use `us.Subviews` to determine size.

                List<View> includedSubviews = us.Subviews.ToList ();

                // If [x] it can cause `us.ContentSize` to change.
                // If [ ] it doesn't need special processing for us to determine `us.ContentSize`.

                // -------------------- Pos types that are dependent on `us.Subviews`
                // [ ] PosAlign     - Position is dependent on other views with `GroupId` AND `us.ContentSize`
                // [x] PosView      - Position is dependent on `subview.Target` - it can cause a change in `us.ContentSize`
                // [x] PosCombine   - Position is dependent if `Pos.Has ([one of the above]` - it can cause a change in `us.ContentSize`

                // -------------------- Pos types that are dependent on `us.ContentSize`
                // [ ] PosAlign     - Position is dependent on other views with `GroupId` AND `us.ContentSize`
                // [x] PosAnchorEnd - Position is dependent on `us.ContentSize` AND `subview.Frame` - it can cause a change in `us.ContentSize`
                // [ ] PosCenter    - Position is dependent `us.ContentSize` AND `subview.Frame`
                // [ ] PosPercent   - Position is dependent `us.ContentSize`
                // [x] PosCombine   - Position is dependent if `Pos.Has ([one of the above]` - it can cause a change in `us.ContentSize`

                // -------------------- Pos types that are not dependent on either `us.Subviews` or `us.ContentSize`
                // [ ] PosAbsolute  - Position is fixed.
                // [ ] PosFunc      - Position is internally calculated.

                // -------------------- Dim types that are dependent on `us.Subviews`
                // [x] DimView      - Dimension is dependent on `subview.Target`
                // [x] DimCombine   - Dimension is dependent if `Dim.Has ([one of the above]` - it can cause a change in `us.ContentSize`

                // -------------------- Dim types that are dependent on `us.ContentSize`
                // [ ] DimFill      - Dimension is dependent on `us.ContentSize`
                // [ ] DimPercent   - Dimension is dependent on `us.ContentSize`
                // [ ] DimCombine   - Dimension is dependent if `Dim.Has ([one of the above]`

                // -------------------- Dim types that are not dependent on either `us.Subviews` or `us.ContentSize`
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
                    notDependentSubViews = includedSubviews.Where (v => v.Width is { } &&
                                                                        (v.X is PosAbsolute or PosFunc || v.Width is DimAuto or DimAbsolute or DimFunc) &&
                                                                        !v.X.Has (typeof (PosAnchorEnd), out _) &&
                                                                        !v.X.Has (typeof (PosAlign), out _) &&
                                                                        !v.X.Has (typeof (PosView), out _) &&
                                                                        !v.Width.Has (typeof (DimView), out _) &&
                                                                        !v.X.Has (typeof (PosCenter), out _)).ToList ();
                }
                else
                {
                    notDependentSubViews = includedSubviews.Where (v => v.Height is { } &&
                                                                        (v.Y is PosAbsolute or PosFunc || v.Height is DimAuto or DimAbsolute or DimFunc) &&
                                                                        !v.Y.Has (typeof (PosAnchorEnd), out _) &&
                                                                        !v.Y.Has (typeof (PosAlign), out _) &&
                                                                        !v.Y.Has (typeof (PosView), out _) &&
                                                                        !v.Height.Has (typeof (DimView), out _) &&
                                                                        !v.Y.Has (typeof (PosCenter), out _)).ToList ();
                }

                for (var i = 0; i < notDependentSubViews.Count; i++)
                {
                    View v = notDependentSubViews [i];

                    int size = 0;

                    if (dimension == Dimension.Width)
                    {
                        int width = v.Width!.Calculate (0, 0, v, dimension);
                        size = v.X.GetAnchor (0) + width;
                    }
                    else
                    {
                        int height = v.Height!.Calculate (0, 0, v, dimension);
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
                    centeredSubViews = us.Subviews.Where (v => v.X.Has (typeof (PosCenter), out _)).ToList ();
                }
                else
                {
                    centeredSubViews = us.Subviews.Where (v => v.Y.Has (typeof (PosCenter), out _)).ToList ();
                }

                int maxCentered = 0;

                for (var i = 0; i < centeredSubViews.Count; i++)
                {
                    View v = centeredSubViews [i];

                    if (dimension == Dimension.Width)
                    {
                        int width = v.Width!.Calculate (0, 0, v, dimension);
                        maxCentered = (v.X.GetAnchor (0) + width) * 2;
                    }
                    else
                    {
                        int height = v.Height!.Calculate (0, 0, v, dimension);
                        maxCentered = (v.Y.GetAnchor (0) + height) * 2;
                    }
                }
                maxCalculatedSize = int.Max (maxCalculatedSize, maxCentered);
                #endregion Centered

                #region Percent
                // [ ] DimPercent   - Dimension is dependent on `us.ContentSize`
                List<View> percentSubViews;
                if (dimension == Dimension.Width)
                {
                    percentSubViews = us.Subviews.Where (v => v.Width.Has (typeof (DimPercent), out _)).ToList ();
                }
                else
                {
                    percentSubViews = us.Subviews.Where (v => v.Height.Has (typeof (DimPercent), out _)).ToList ();
                }

                int maxPercent = 0;

                for (var i = 0; i < percentSubViews.Count; i++)
                {
                    View v = percentSubViews [i];

                    if (dimension == Dimension.Width)
                    {
                        int width = v.Width!.Calculate (0, 0, v, dimension);
                        maxPercent = (v.X.GetAnchor (0) + width);
                    }
                    else
                    {
                        int height = v.Height!.Calculate (0, 0, v, dimension);
                        maxPercent = (v.Y.GetAnchor (0) + height);
                    }
                }
                maxCalculatedSize = int.Max (maxCalculatedSize, maxPercent);
                #endregion Percent


                #region Aligned
                // [ ] PosAlign     - Position is dependent on other views with `GroupId` AND `us.ContentSize`
                int maxAlign = 0;
                // Use Linq to get a list of distinct GroupIds from the subviews
                List<int> groupIds = includedSubviews.Select (
                                                              v =>
                                                              {
                                                                  if (dimension == Dimension.Width)
                                                                  {
                                                                      if (v.X.Has (typeof (PosAlign), out Pos posAlign))
                                                                      {
                                                                          return ((PosAlign)posAlign).GroupId;
                                                                      }
                                                                  }
                                                                  else
                                                                  {
                                                                      if (v.Y.Has (typeof (PosAlign), out Pos posAlign))
                                                                      {
                                                                          return ((PosAlign)posAlign).GroupId;
                                                                      }
                                                                  }
                                                                  return -1;
                                                              }).Distinct ().ToList ();

                foreach (var groupId in groupIds.Where (g => g != -1))
                {
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

                    maxAlign = PosAlign.CalculateMinDimension (groupId, includedSubviews, dimension);
                }

                maxCalculatedSize = int.Max (maxCalculatedSize, maxAlign);
                #endregion Aligned



                #region Anchored
                // [x] PosAnchorEnd - Position is dependent on `us.ContentSize` AND `subview.Frame` 
                List<View> anchoredSubViews;
                if (dimension == Dimension.Width)
                {
                    anchoredSubViews = includedSubviews.Where (v => v.X.Has (typeof (PosAnchorEnd), out _)).ToList ();
                }
                else
                {
                    anchoredSubViews = includedSubviews.Where (v => v.Y.Has (typeof (PosAnchorEnd), out _)).ToList ();
                }

                int maxAnchorEnd = 0;
                for (var i = 0; i < anchoredSubViews.Count; i++)
                {
                    View v = anchoredSubViews [i];

                    // Need to set the relative layout for PosAnchorEnd subviews to calculate the size
                    if (dimension == Dimension.Width)
                    {
                        v.SetRelativeLayout (new Size (maxCalculatedSize, 0));
                    }
                    else
                    {
                        v.SetRelativeLayout (new Size (0, maxCalculatedSize));
                    }
                    maxAnchorEnd = dimension == Dimension.Width ? v.X.GetAnchor (maxCalculatedSize) + v.Frame.Width : v.Y.GetAnchor (maxCalculatedSize) + v.Frame.Height;
                }

                maxCalculatedSize = Math.Max (maxCalculatedSize, maxAnchorEnd);
                #endregion Anchored

                #region PosView
                // [x] PosView      - Position is dependent on `subview.Target` - it can cause a change in `us.ContentSize`
                List<View> posViewSubViews;
                if (dimension == Dimension.Width)
                {
                    posViewSubViews = includedSubviews.Where (v => v.X.Has (typeof (PosView), out _)).ToList ();
                }
                else
                {
                    posViewSubViews = includedSubviews.Where (v => v.Y.Has (typeof (PosView), out _)).ToList ();
                }

                for (var i = 0; i < posViewSubViews.Count; i++)
                {
                    View v = posViewSubViews [i];

                    // BUGBUG: The order may not be correct. May need to call TopologicalSort?
                    if (dimension == Dimension.Width)
                    {
                        v.SetRelativeLayout (new Size (maxCalculatedSize, 0));
                    }
                    else
                    {
                        v.SetRelativeLayout (new Size (0, maxCalculatedSize));
                    }
                    int maxPosView = dimension == Dimension.Width ? v.Frame.X + v.Frame.Width : v.Frame.Y + v.Frame.Height;

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
                    dimViewSubViews = includedSubviews.Where (v => v.Width is { } && v.Width.Has (typeof (DimView), out _)).ToList ();
                }
                else
                {
                    dimViewSubViews = includedSubviews.Where (v => v.Height is { } && v.Height.Has (typeof (DimView), out _)).ToList ();
                }

                for (var i = 0; i < dimViewSubViews.Count; i++)
                {
                    View v = dimViewSubViews [i];

                    // BUGBUG: The order may not be correct. May need to call TopologicalSort?
                    if (dimension == Dimension.Width)
                    {
                        v.SetRelativeLayout (new Size (maxCalculatedSize, 0));
                    }
                    else
                    {
                        v.SetRelativeLayout (new Size (0, maxCalculatedSize));
                    }

                    int maxDimView = dimension == Dimension.Width ? v.Frame.X + v.Frame.Width : v.Frame.Y + v.Frame.Height;

                    if (maxDimView > maxCalculatedSize)
                    {
                        maxCalculatedSize = maxDimView;
                    }
                }
                #endregion DimView


                // [x] DimCombine   - Dimension is dependent if `Dim.Has ([one of the above]` - it can cause a change in `us.ContentSize`







                //        // ======================================================
                //        // Now do PosAlign - It's dependent on other views with `GroupId` AND `us.ContentSize`
                //        // ======================================================
                //        // [ ] PosAlign     - Position is dependent on other views with `GroupId` AND `us.ContentSize`
                //        #region Aligned

                //        int maxAlign = 0;
                //        if (dimension == Dimension.Width)
                //        {
                //            // Use Linq to get a list of distinct GroupIds from the subviews
                //            List<int> groupIds = includedSubviews.Select (v => v.X is PosAlign posAlign ? posAlign.GroupId : -1).Distinct ().ToList ();

                //            foreach (var groupId in groupIds)
                //            {
                //                List<int> dimensionsList = new ();

                //                // PERF: If this proves a perf issue, consider caching a ref to this list in each item
                //                List<PosAlign?> posAlignsInGroup = includedSubviews.Where (
                //                    v =>
                //                    {
                //                        return dimension switch
                //                        {
                //                            Dimension.Width when v.X is PosAlign alignX => alignX.GroupId == groupId,
                //                            Dimension.Height when v.Y is PosAlign alignY => alignY.GroupId == groupId,
                //                            _ => false
                //                        };
                //                    })
                //                    .Select (v => dimension == Dimension.Width ? v.X as PosAlign : v.Y as PosAlign)
                //                    .ToList ();

                //                if (posAlignsInGroup.Count == 0)
                //                {
                //                    continue;
                //                }
                //                // BUGBUG: ignores adornments

                //                maxAlign = PosAlign.CalculateMinDimension (groupId, includedSubviews, dimension);
                //            }
                //        }
                //        else
                //        {

                //            // BUGBUG: Incompletge
                //            subviews = includedSubviews.Where (v => v.Y is PosAlign).ToList ();
                //        }

                //        maxCalculatedSize = int.Max (maxCalculatedSize, maxAlign);
                //        #endregion Aligned

                //        // TODO: This whole body of code is a WIP (forhttps://github.com/gui-cs/Terminal.Gui/issues/3499).


                //        List<View> subviews;

                //        #region Not Anchored and Are Not Dependent
                //        // Start with subviews that are not anchored to the end, aligned, or dependent on content size
                //        // [x] PosAnchorEnd
                //        // [x] PosAlign
                //        // [ ] PosCenter
                //        // [ ] PosPercent
                //        // [ ] PosView
                //        // [ ] PosFunc
                //        // [x] DimFill
                //        // [ ] DimPercent
                //        // [ ] DimFunc
                //        // [ ] DimView
                //        if (dimension == Dimension.Width)
                //        {
                //            subviews = includedSubviews.Where (v => v.X is not PosAnchorEnd
                //                                                   && v.X is not PosAlign
                //                                                    // && v.X is not PosCenter
                //                                                    && v.Width is not DimAuto
                //                                                   && v.Width is not DimFill).ToList ();
                //        }
                //        else
                //        {
                //            subviews = includedSubviews.Where (v => v.Y is not PosAnchorEnd
                //                                                   && v.Y is not PosAlign
                //                                                    // && v.Y is not PosCenter
                //                                                    && v.Height is not DimAuto
                //                                                   && v.Height is not DimFill).ToList ();
                //        }

                //        for (var i = 0; i < subviews.Count; i++)
                //        {
                //            View v = subviews [i];

                //            int size = dimension == Dimension.Width ? v.Frame.X + v.Frame.Width : v.Frame.Y + v.Frame.Height;

                //            if (size > maxCalculatedSize)
                //            {
                //                // BUGBUG: Should we break here? Or choose min/max?
                //                maxCalculatedSize = size;
                //            }
                //        }
                //        #endregion Not Anchored and Are Not Dependent



                //        #region Auto



                //        #endregion Auto

                //        //#region Center
                //        //// Now, handle subviews that are Centered
                //        //if (dimension == Dimension.Width)
                //        //{
                //        //    subviews = us.Subviews.Where (v => v.X is PosCenter).ToList ();
                //        //}
                //        //else
                //        //{
                //        //    subviews = us.Subviews.Where (v => v.Y is PosCenter).ToList ();
                //        //}

                //        //int maxCenter = 0;
                //        //for (var i = 0; i < subviews.Count; i++)
                //        //{
                //        //    View v = subviews [i];
                //        //    maxCenter = dimension == Dimension.Width ? v.Frame.Width : v.Frame.Height;
                //        //}

                //        //subviewsSize += maxCenter;
                //        //#endregion Center

                //        #region Are Dependent
                //        // Now, go back to those that are dependent on content size


                //        // Set relative layout for all DimAuto subviews
                //        List<View> dimAutoSubViews;
                //        int maxAuto = 0;
                //        if (dimension == Dimension.Width)
                //        {
                //            dimAutoSubViews = includedSubviews.Where (v => v.Width is DimAuto).ToList ();
                //        }
                //        else
                //        {
                //            dimAutoSubViews = includedSubviews.Where (v => v.Height is DimAuto).ToList ();
                //        }
                //        for (var i = 0; i < dimAutoSubViews.Count; i++)
                //        {
                //            View v = dimAutoSubViews [i];

                //            if (dimension == Dimension.Width)
                //            {
                //                // BUGBUG: ignores adornments

                //                v.SetRelativeLayout (new Size (autoMax - maxCalculatedSize, 0));
                //            }
                //            else
                //            {
                //                // BUGBUG: ignores adornments

                //                v.SetRelativeLayout (new Size (0, autoMax - maxCalculatedSize));
                //            }

                //            maxAuto = dimension == Dimension.Width ? v.Frame.X + v.Frame.Width : v.Frame.Y + v.Frame.Height;

                //            if (maxAuto > maxCalculatedSize)
                //            {
                //                // BUGBUG: Should we break here? Or choose min/max?
                //                maxCalculatedSize = maxAuto;
                //            }
                //        }

                //        // [x] DimFill
                //        // [ ] DimPercent
                //        if (dimension == Dimension.Width)
                //        {
                //            subviews = includedSubviews.Where (v => v.Width is DimFill).ToList ();
                //        }
                //        else
                //        {
                //            subviews = includedSubviews.Where (v => v.Height is DimFill).ToList ();
                //        }

                //        int maxFill = 0;
                //        for (var i = 0; i < subviews.Count; i++)
                //        {
                //            View v = subviews [i];

                //            if (autoMax == int.MaxValue)
                //            {
                //                autoMax = superviewContentSize;
                //            }
                //            if (dimension == Dimension.Width)
                //            {
                //                // BUGBUG: ignores adornments
                //                v.SetRelativeLayout (new Size (autoMax - maxCalculatedSize, 0));
                //            }
                //            else
                //            {
                //                // BUGBUG: ignores adornments
                //                v.SetRelativeLayout (new Size (0, autoMax - maxCalculatedSize));
                //            }
                //            maxFill = dimension == Dimension.Width ? v.Frame.Width : v.Frame.Height;
                //        }

                //        maxCalculatedSize += maxFill;
                //        #endregion Are Dependent
            }
        }

        // All sizes here are content-relative; ignoring adornments.
        // We take the largest of text and content.
        int max = int.Max (textSize, maxCalculatedSize);

        // And, if min: is set, it wins if larger
        max = int.Max (max, autoMin);

        // And, if max: is set, it wins if smaller
        max = int.Min (max, autoMax);


        // ************** We now definitively know `us.ContentSize` ***************

        int oppositeScreen = dimension == Dimension.Width ? Application.Screen.Height * 4 : Application.Screen.Width * 4 ;
        foreach (var v in us.Subviews)
        {
            if (dimension == Dimension.Width)
            {
                v.SetRelativeLayout (new Size (max, oppositeScreen));
            }
            else
            {
                v.SetRelativeLayout (new Size (oppositeScreen, max));
            }
        }

        // Factor in adornments
        Thickness thickness = us.GetAdornmentsThickness ();
        var adornmentThickness = dimension switch
        {
            Dimension.Width => thickness.Horizontal,
            Dimension.Height => thickness.Vertical,
            Dimension.None => 0,
            _ => throw new ArgumentOutOfRangeException (nameof (dimension), dimension, null)
        };

        max += adornmentThickness;

        return max;
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