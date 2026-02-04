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
                // Single-pass categorization of subviews
                CategorizedViews categorized = CategorizeSubViews (us.InternalSubViews, dimension);

                foreach (View notDependentSubView in categorized.NotDependent)
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

                foreach (View v in categorized.Centered)
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

                // Need to convert to list for PosAlign.CalculateMinDimension which expects IReadOnlyCollection
                List<View> subViewsList = us.InternalSubViews as List<View> ?? [.. us.InternalSubViews];

                foreach (int groupId in categorized.AlignGroupIds)
                {
                    maxAlign = PosAlign.CalculateMinDimension (groupId, subViewsList, dimension);
                }

                maxCalculatedSize = int.Max (maxCalculatedSize, maxAlign);

                #endregion Aligned

                var maxAnchorEnd = 0;

                foreach (View anchoredSubView in categorized.Anchored)
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

                // Process PosView, DimView, and DimAuto using the unified GetMaxSize helper
                Func<View, int, Dimension, int> calculateViewSize = (v, max, dim) => dim == Dimension.Width
                                                                                         ? v.Frame.X + v.Width.Calculate (0, max, v, dim)
                                                                                         : v.Frame.Y + v.Height.Calculate (0, max, v, dim);

                maxCalculatedSize = GetMaxSize (maxCalculatedSize, dimension, categorized.PosViewBased, calculateViewSize);
                maxCalculatedSize = GetMaxSize (maxCalculatedSize, dimension, categorized.DimViewBased, calculateViewSize);
                maxCalculatedSize = GetMaxSize (maxCalculatedSize, dimension, categorized.DimAutoBased, calculateViewSize);

                // DimFill subviews contribute to auto-sizing only if they have MinimumContentDim or To set
                // Process DimFill views that can contribute
                foreach (View dimFillSubView in categorized.DimFillBased)
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

    /// <summary>
    ///     Holds subviews categorized by their Pos/Dim types for single-pass iteration.
    /// </summary>
    private readonly struct CategorizedViews
    {
        public List<View> NotDependent { get; init; }
        public List<View> Centered { get; init; }
        public List<View> Anchored { get; init; }
        public List<View> PosViewBased { get; init; }
        public List<View> DimViewBased { get; init; }
        public List<View> DimAutoBased { get; init; }
        public List<View> DimFillBased { get; init; }
        public HashSet<int> AlignGroupIds { get; init; }
    }

    /// <summary>
    ///     Categorizes subviews by their Pos/Dim types in a single pass to avoid multiple iterations.
    /// </summary>
    private static CategorizedViews CategorizeSubViews (IList<View> subViews, Dimension dimension)
    {
        List<View> notDependent = [];
        List<View> centered = [];
        List<View> anchored = [];
        List<View> posViewBased = [];
        List<View> dimViewBased = [];
        List<View> dimAutoBased = [];
        List<View> dimFillBased = [];
        HashSet<int> alignGroupIds = [];

        foreach (View v in subViews)
        {
            Pos pos = dimension == Dimension.Width ? v.X : v.Y;
            Dim dim = dimension == Dimension.Width ? v.Width : v.Height;

            // Check position types (mutually exclusive for categorization purposes)
            if (pos.Has<PosCenter> (out _))
            {
                centered.Add (v);
            }
            else if (pos.Has<PosAnchorEnd> (out _))
            {
                anchored.Add (v);
            }
            else if (pos.Has<PosView> (out _))
            {
                posViewBased.Add (v);
            }

            // Check for PosAlign (can coexist with other categories)
            if (pos.Has (out PosAlign posAlign))
            {
                alignGroupIds.Add (posAlign.GroupId);
            }

            // Check dimension types (mutually exclusive for categorization purposes)
            if (dim.Has<DimView> (out _))
            {
                dimViewBased.Add (v);
            }
            else if (dim.Has<DimAuto> (out _))
            {
                dimAutoBased.Add (v);
            }
            else if (dim.Has<DimFill> (out _))
            {
                dimFillBased.Add (v);
            }

            // Check not-dependent (can coexist with other categories)
            bool isFixed = pos.IsFixed || dim.IsFixed;
            bool dependsOnSuper = pos.DependsOnSuperViewContentSize || dim.DependsOnSuperViewContentSize;

            if (isFixed && !dependsOnSuper)
            {
                notDependent.Add (v);
            }
        }

        return new CategorizedViews
        {
            NotDependent = notDependent,
            Centered = centered,
            Anchored = anchored,
            PosViewBased = posViewBased,
            DimViewBased = dimViewBased,
            DimAutoBased = dimAutoBased,
            DimFillBased = dimFillBased,
            AlignGroupIds = alignGroupIds
        };
    }

    private static int GetMaxSize (int max, Dimension dimension, List<View> views, Func<View, int, Dimension, int> calculateSize)
    {
        foreach (View v in views)
        {
            int newMax = calculateSize (v, max, dimension);

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
