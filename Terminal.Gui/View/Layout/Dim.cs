#nullable enable
using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     Specifies how <see cref="Dim.Auto"/> will compute the dimension.
/// </summary>
[Flags]
public enum DimAutoStyle
{
    /// <summary>
    ///     The dimension will be computed using both the view's <see cref="View.Text"/> and
    ///     <see cref="View.Subviews"/> (whichever is larger).
    /// </summary>
    Auto = Content | Text,

    /// <summary>
    ///     The dimensions will be computed based on the View's non-Text content.
    ///     <para>
    ///         If <see cref="View.ContentSize"/> is explicitly set (is not <see langword="null"/>) then
    ///         <see cref="View.ContentSize"/>
    ///         will be used to determine the dimension.
    ///     </para>
    ///     <para>
    ///         Otherwise, the Subview in <see cref="View.Subviews"/> with the largest corresponding position plus dimension
    ///         will determine the dimension.
    ///     </para>
    ///     <para>
    ///         The corresponding dimension of the view's <see cref="View.Text"/> will be ignored.
    ///     </para>
    /// </summary>
    Content = 0,

    /// <summary>
    ///     <para>
    ///         The corresponding dimension of the view's <see cref="View.Text"/>, formatted using the
    ///         <see cref="View.TextFormatter"/> settings,
    ///         will be used to determine the dimension.
    ///     </para>
    ///     <para>
    ///         The corresponding dimensions of the <see cref="View.Subviews"/> will be ignored.
    ///     </para>
    /// </summary>
    Text = 1
}

/// <summary>
///     Indicates the dimension for <see cref="Dim"/> operations.
/// </summary>
public enum Dimension
{
    /// <summary>
    ///     No dimension specified.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The height dimension.
    /// </summary>
    Height = 1,

    /// <summary>
    ///     The width dimension.
    /// </summary>
    Width = 2
}

/// <summary>
///     <para>
///         A Dim object describes the dimensions of a <see cref="View"/>. Dim is the type of the
///         <see cref="View.Width"/> and <see cref="View.Height"/> properties of <see cref="View"/>. Dim objects enable
///         Computed Layout (see <see cref="LayoutStyle.Computed"/>) to automatically manage the dimensions of a view.
///     </para>
///     <para>
///         Integer values are implicitly convertible to an absolute <see cref="Dim"/>. These objects are created using
///         the static methods described below. The <see cref="Dim"/> objects can be combined with the addition and
///         subtraction operators.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         <list type="table">
///             <listheader>
///                 <term>Dim Object</term> <description>Description</description>
///             </listheader>
///             <item>
///                 <term>
///                     <see cref="Dim.Auto"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that automatically sizes the view to fit
///                     the view's Text, SubViews, or ContentArea.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Func"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that computes the dimension by executing the provided
///                     function. The function will be called every time the dimension is needed.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Percent(float, bool)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that is a percentage of the width or height of the
///                     SuperView.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Fill(int)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that fills the dimension from the View's X position
///                     to the end of the super view's width, leaving the specified number of columns for a margin.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Width(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that tracks the Width of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Height(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that tracks the Height of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///         </list>
///     </para>
///     <para></para>
/// </remarks>
public abstract class Dim
{
    #region static Dim creation methods

    /// <summary>Creates an Absolute <see cref="Dim"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Dim"/>.</returns>
    /// <param name="size">The value to convert to the <see cref="Dim"/>.</param>
    public static Dim? Absolute (int size) { return new DimAbsolute (size); }

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that automatically sizes the view to fit all the view's Content, Subviews, and/or Text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="DimAutoStyle"/>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     This initializes a <see cref="View"/> with two SubViews. The view will be automatically sized to fit the two
    ///     SubViews.
    ///     <code>
    /// var button = new Button () { Text = "Click Me!", X = 1, Y = 1, Width = 10, Height = 1 };
    /// var textField = new TextField { Text = "Type here", X = 1, Y = 2, Width = 20, Height = 1 };
    /// var view = new Window () { Title = "MyWindow", X = 0, Y = 0, Width = Dim.Auto (), Height = Dim.Auto () };
    /// view.Add (button, textField);
    /// </code>
    /// </example>
    /// <returns>The <see cref="Dim"/> object.</returns>
    /// <param name="style">
    ///     Specifies how <see cref="Dim.Auto"/> will compute the dimension. The default is <see cref="DimAutoStyle.Auto"/>.
    /// </param>
    /// <param name="minimumContentDim">The minimum dimension the View's ContentSize will be constrained to.</param>
    /// <param name="maximumContentDim">The maximum dimension the View's ContentSize will be fit to. NOT CURRENTLY SUPPORTED.</param>
    public static Dim? Auto (DimAutoStyle style = DimAutoStyle.Auto, Dim? minimumContentDim = null, Dim? maximumContentDim = null)
    {
        //if (maximumContentDim != null)
        //{
        //    throw new NotImplementedException (@"maximumContentDim is not implemented");
        //}

        return new DimAuto (style, minimumContentDim, maximumContentDim);
    }

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension, leaving the specified margin.
    /// </summary>
    /// <returns>The Fill dimension.</returns>
    /// <param name="margin">Margin to use.</param>
    public static Dim? Fill (int margin = 0) { return new DimFill (margin); }

    /// <summary>
    ///     Creates a function <see cref="Dim"/> object that computes the dimension by executing the provided function.
    ///     The function will be called every time the dimension is needed.
    /// </summary>
    /// <param name="function">The function to be executed.</param>
    /// <returns>The <see cref="Dim"/> returned from the function.</returns>
    public static Dim Func (Func<int> function) { return new DimFunc (function); }

    /// <summary>Creates a <see cref="Dim"/> object that tracks the Height of the specified <see cref="View"/>.</summary>
    /// <returns>The height <see cref="Dim"/> of the other <see cref="View"/>.</returns>
    /// <param name="view">The view that will be tracked.</param>
    public static Dim Height (View view) { return new DimView (view, Dimension.Height); }

    /// <summary>Creates a percentage <see cref="Dim"/> object that is a percentage of the width or height of the SuperView.</summary>
    /// <returns>The percent <see cref="Dim"/> object.</returns>
    /// <param name="percent">A value between 0 and 100 representing the percentage.</param>
    /// <param name="usePosition">
    ///     If <see langword="true"/> the dimension is computed using the View's position (<see cref="View.X"/> or
    ///     <see cref="View.Y"/>).
    ///     If <see langword="false"/> the dimension is computed using the View's <see cref="View.ContentSize"/>.
    /// </param>
    /// <example>
    ///     This initializes a <see cref="TextField"/> that will be centered horizontally, is 50% of the way down, is 30% the
    ///     height,
    ///     and is 80% the width of the SuperView.
    ///     <code>
    ///  var textView = new TextField {
    ///     X = Pos.Center (),
    ///     Y = Pos.Percent (50),
    ///     Width = Dim.Percent (80),
    ///     Height = Dim.Percent (30),
    ///  };
    ///  </code>
    /// </example>
    public static Dim? Percent (float percent, bool usePosition = false)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentException ("Percent value must be between 0 and 100");
        }

        return new DimPercent (percent / 100, usePosition);
    }

    /// <summary>Creates a <see cref="Dim"/> object that tracks the Width of the specified <see cref="View"/>.</summary>
    /// <returns>The width <see cref="Dim"/> of the other <see cref="View"/>.</returns>
    /// <param name="view">The view that will be tracked.</param>
    public static Dim Width (View view) { return new DimView (view, Dimension.Width); }

    #endregion static Dim creation methods

    #region virtual methods

    /// <summary>
    ///     Gets a dimension that is anchored to a certain point in the layout.
    ///     This method is typically used internally by the layout system to determine the size of a View.
    /// </summary>
    /// <param name="size">The width of the area where the View is being sized (Superview.ContentSize).</param>
    /// <returns>
    ///     An integer representing the calculated dimension. The way this dimension is calculated depends on the specific
    ///     subclass of Dim that is used. For example, DimAbsolute returns a fixed dimension, DimFactor returns a
    ///     dimension that is a certain percentage of the super view's size, and so on.
    /// </returns>
    internal virtual int GetAnchor (int size) { return 0; }

    /// <summary>
    ///     Calculates and returns the dimension of a <see cref="View"/> object. It takes into account the location of the
    ///     <see cref="View"/>, it's SuperView's ContentSize, and whether it should automatically adjust its size based on its
    ///     content.
    /// </summary>
    /// <param name="location">
    ///     The starting point from where the size calculation begins. It could be the left edge for width calculation or the
    ///     top edge for height calculation.
    /// </param>
    /// <param name="superviewContentSize">The size of the SuperView's content. It could be width or height.</param>
    /// <param name="us">The View that holds this Pos object.</param>
    /// <param name="dimension">Width or Height</param>
    /// <returns>
    ///     The calculated size of the View. The way this size is calculated depends on the specific subclass of Dim that
    ///     is used.
    /// </returns>
    internal virtual int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        return Math.Max (GetAnchor (superviewContentSize - location), 0);
    }

    /// <summary>
    ///     Diagnostics API to determine if this Dim object references other views.
    /// </summary>
    /// <returns></returns>
    internal virtual bool ReferencesOtherViews () { return false; }

    #endregion virtual methods

    #region operators

    /// <summary>Adds a <see cref="Dim"/> to a <see cref="Dim"/>, yielding a new <see cref="Dim"/>.</summary>
    /// <param name="left">The first <see cref="Dim"/> to add.</param>
    /// <param name="right">The second <see cref="Dim"/> to add.</param>
    /// <returns>The <see cref="Dim"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
    public static Dim operator + (Dim? left, Dim? right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.GetAnchor (0) + right.GetAnchor (0));
        }

        var newDim = new DimCombine (true, left, right);
        (left as DimView)?.Target.SetNeedsLayout ();

        return newDim;
    }

    /// <summary>Creates an Absolute <see cref="Dim"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Dim"/>.</returns>
    /// <param name="n">The value to convert to the pos.</param>
    public static implicit operator Dim (int n) { return new DimAbsolute (n); }

    /// <summary>
    ///     Subtracts a <see cref="Dim"/> from a <see cref="Dim"/>, yielding a new
    ///     <see cref="Dim"/>.
    /// </summary>
    /// <param name="left">The <see cref="Dim"/> to subtract from (the minuend).</param>
    /// <param name="right">The <see cref="Dim"/> to subtract (the subtrahend).</param>
    /// <returns>The <see cref="Dim"/> that is the <c>left</c> minus <c>right</c>.</returns>
    public static Dim operator - (Dim? left, Dim? right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.GetAnchor (0) - right.GetAnchor (0));
        }

        var newDim = new DimCombine (false, left, right);
        (left as DimView)?.Target.SetNeedsLayout ();

        return newDim;
    }

    #endregion operators

}

/// <summary>
///     Represents a dimension that is a fixed size.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
///     </para>
/// </remarks>
/// <param name="size"></param>
public class DimAbsolute (int size) : Dim
{
    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is DimAbsolute abs && abs.Size == Size; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Size.GetHashCode (); }

    /// <summary>
    ///     Gets the size of the dimension.
    /// </summary>
    public int Size { get; } = size;

    /// <inheritdoc/>
    public override string ToString () { return $"Absolute({Size})"; }

    internal override int GetAnchor (int size) { return Size; }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        return Math.Max (GetAnchor (0), 0);
    }
}

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
/// <param name="style">
///     Specifies how <see cref="DimAuto"/> will compute the dimension. The default is <see cref="DimAutoStyle.Auto"/>.
/// </param>
/// <param name="minimumContentDim">The minimum dimension the View's ContentSize will be constrained to.</param>
/// <param name="maximumContentDim">The maximum dimension the View's ContentSize will be fit to. NOT CURRENTLY SUPPORTED.</param>
public class DimAuto (DimAutoStyle style, Dim? minimumContentDim, Dim? maximumContentDim) : Dim
{
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
    public override int GetHashCode () { return HashCode.Combine (base.GetHashCode (), MinimumContentDim, MaximumContentDim, Style); }

    /// <summary>
    ///     Gets the maximum dimension the View's ContentSize will be fit to. NOT CURRENTLY SUPPORTED.
    /// </summary>
    public Dim? MaximumContentDim { get; } = maximumContentDim;

    /// <summary>
    ///     Gets the minimum dimension the View's ContentSize will be constrained to.
    /// </summary>
    public Dim? MinimumContentDim { get; } = minimumContentDim;

    /// <summary>
    ///     Gets the style of the DimAuto.
    /// </summary>
    public DimAutoStyle Style { get; } = style;

    /// <inheritdoc/>
    public override string ToString () { return $"Auto({Style},{MinimumContentDim},{MaximumContentDim})"; }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        if (us == null)
        {
            return MaximumContentDim?.GetAnchor (0) ?? 0;
        }

        var textSize = 0;
        var subviewsSize = 0;

        int autoMin = MinimumContentDim?.GetAnchor (superviewContentSize) ?? 0;

        if (superviewContentSize < autoMin)
        {
            Debug.WriteLine ($"WARNING: DimAuto specifies a min size ({autoMin}), but the SuperView's bounds are smaller ({superviewContentSize}).");

            //return superviewContentSize;
        }

        if (Style.HasFlag (DimAutoStyle.Text))
        {
            textSize = int.Max (autoMin, dimension == Dimension.Width ? us.TextFormatter.Size.Width : us.TextFormatter.Size.Height);
        }

        if (Style.HasFlag (DimAutoStyle.Content))
        {
            if (us._contentSize is { })
            {
                subviewsSize = dimension == Dimension.Width ? us.ContentSize.Width : us.ContentSize.Height;
            }
            else
            {
                // TODO: AnchorEnd needs work
                // TODO: If _min > 0 we can SetRelativeLayout for the subviews?
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
        // We take the larger of text and content.
        int max = int.Max (textSize, subviewsSize);

        // And, if min: is set, it wins if larger
        max = int.Max (max, autoMin);

        // Factor in adornments
        Thickness thickness = us.GetAdornmentsThickness ();

        if (dimension == Dimension.Width)
        {
            max += thickness.Horizontal;
        }
        else
        {
            max += thickness.Vertical;
        }

        // If max: is set, clamp the return - BUGBUG: Not tested
        return int.Min (max, MaximumContentDim?.GetAnchor (superviewContentSize) ?? max);
    }

    internal override bool ReferencesOtherViews ()
    {
        // BUGBUG: This is not correct. _contentSize may be null.
        return false; //_style.HasFlag (DimAutoStyle.Content);
    }
}

/// <summary>
///     Represents a dimension that is a combination of two other dimensions.
/// </summary>
/// <param name="add">
///     Indicates whether the two dimensions are added or subtracted. If <see langword="true"/>, the dimensions are added,
///     otherwise they are subtracted.
/// </param>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="left">The left dimension.</param>
/// <param name="right">The right dimension.</param>
public class DimCombine (bool add, Dim? left, Dim? right) : Dim
{
    /// <summary>
    ///     Gets whether the two dimensions are added or subtracted.
    /// </summary>
    public bool Add { get; } = add;

    /// <summary>
    ///     Gets the left dimension.
    /// </summary>
    public Dim? Left { get; } = left;

    /// <summary>
    ///     Gets the right dimension.
    /// </summary>
    public Dim? Right { get; } = right;

    /// <inheritdoc/>
    public override string ToString () { return $"Combine({Left}{(Add ? '+' : '-')}{Right})"; }

    internal override int GetAnchor (int size)
    {
        int la = Left!.GetAnchor (size);
        int ra = Right!.GetAnchor (size);

        if (Add)
        {
            return la + ra;
        }

        return la - ra;
    }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        int leftNewDim = Left!.Calculate (location, superviewContentSize, us, dimension);
        int rightNewDim = Right!.Calculate (location, superviewContentSize, us, dimension);

        int newDimension;

        if (Add)
        {
            newDimension = leftNewDim + rightNewDim;
        }
        else
        {
            newDimension = Math.Max (0, leftNewDim - rightNewDim);
        }

        return newDimension;
    }

    /// <summary>
    ///     Diagnostics API to determine if this Dim object references other views.
    /// </summary>
    /// <returns></returns>
    internal override bool ReferencesOtherViews ()
    {
        if (Left!.ReferencesOtherViews ())
        {
            return true;
        }

        if (Right!.ReferencesOtherViews ())
        {
            return true;
        }

        return false;
    }
}

/// <summary>
///     Represents a dimension that is a percentage of the width or height of the SuperView.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="percent">The percentage.</param>
/// <param name="usePosition">
///     If <see langword="true"/> the dimension is computed using the View's position (<see cref="View.X"/> or
///     <see cref="View.Y"/>).
///     If <see langword="false"/> the dimension is computed using the View's <see cref="View.ContentSize"/>.
/// </param>
public class DimPercent (float percent, bool usePosition = false) : Dim
{
    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is DimPercent f && f.Percent == Percent && f.UsePosition == UsePosition; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Percent.GetHashCode (); }

    /// <summary>
    ///     Gets the percentage.
    /// </summary>
    public new float Percent { get; } = percent;

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string ToString () { return $"Percent({Percent},{UsePosition})"; }

    /// <summary>
    ///     Gets whether the dimension is computed using the View's position or ContentSize.
    /// </summary>
    public bool UsePosition { get; } = usePosition;

    internal override int GetAnchor (int size) { return (int)(size * Percent); }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        return UsePosition ? Math.Max (GetAnchor (superviewContentSize - location), 0) : GetAnchor (superviewContentSize);
    }
}

/// <summary>
///     Represents a dimension that fills the dimension, leaving the specified margin.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="margin">The margin to not fill.</param>
public class DimFill (int margin) : Dim
{
    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is DimFill fill && fill.Margin == Margin; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Margin.GetHashCode (); }

    /// <summary>
    ///     Gets the margin to not fill.
    /// </summary>
    public int Margin { get; } = margin;

    /// <inheritdoc/>
    public override string ToString () { return $"Fill({Margin})"; }

    internal override int GetAnchor (int size) { return size - Margin; }
}

/// <summary>
///     Represents a function <see cref="Dim"/> object that computes the dimension by executing the provided function.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="dim"></param>
public class DimFunc (Func<int> dim) : Dim
{
    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is DimFunc f && f.Func () == Func (); }

    /// <summary>
    ///     Gets the function that computes the dimension.
    /// </summary>
    public new Func<int> Func { get; } = dim;

    /// <inheritdoc/>
    public override int GetHashCode () { return Func.GetHashCode (); }

    /// <inheritdoc/>
    public override string ToString () { return $"DimFunc({Func ()})"; }

    internal override int GetAnchor (int size) { return Func (); }
}

/// <summary>
///     Represents a dimension that tracks the Height or Width of the specified View.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
public class DimView : Dim
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DimView"/> class.
    /// </summary>
    /// <param name="view">The view the dimension is anchored to.</param>
    /// <param name="dimension">Indicates which dimension is tracked.</param>
    public DimView (View view, Dimension dimension)
    {
        Target = view;
        Dimension = dimension;
    }

    /// <summary>
    ///     Gets the indicated dimension of the View.
    /// </summary>
    public Dimension Dimension { get; }

    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is DimView abs && abs.Target == Target && abs.Dimension == Dimension; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Target.GetHashCode (); }

    /// <summary>
    ///     Gets the View the dimension is anchored to.
    /// </summary>
    public View Target { get; init; }

    /// <inheritdoc/>
    public override string ToString ()
    {
        if (Target == null)
        {
            throw new NullReferenceException ();
        }

        string dimString = Dimension switch
        {
            Dimension.Height => "Height",
            Dimension.Width => "Width",
            _ => "unknown"
        };

        return $"View({dimString},{Target})";
    }

    internal override int GetAnchor (int size)
    {
        return Dimension switch
        {
            Dimension.Height => Target.Frame.Height,
            Dimension.Width => Target.Frame.Width,
            _ => 0
        };
    }

    internal override bool ReferencesOtherViews () { return true; }
}
