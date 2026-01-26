using System.Numerics;

namespace Terminal.Gui.ViewBase;

/// <summary>
///     <para>
///         A Dim object describes the dimensions of a <see cref="View"/>. Dim is the type of the
///         <see cref="View.Width"/> and <see cref="View.Height"/> properties of <see cref="View"/>.
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
///                     <see cref="Dim.Absolute"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> that is a fixed size.
///                 </description>
///             </item>
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
///                     <see cref="Dim.Percent(int, DimPercentMode)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Dim"/> object that is a percentage of the width or height of the
///                     SuperView.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Dim.Fill(Dim)"/>
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
public abstract record Dim : IEqualityOperators<Dim, Dim, bool>
{
    #region static Dim creation methods

    /// <summary>Creates an Absolute <see cref="Dim"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Dim"/>.</returns>
    /// <param name="size">The value to convert to the <see cref="Dim"/>.</param>
    public static Dim Absolute (int size) => new DimAbsolute (size);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that automatically sizes the view to fit all the view's Content, SubViews,
    ///     and/or Text.
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
    ///         SubViews that use <see cref="Dim.Fill()"/> do not contribute to the auto-sizing calculation unless
    ///         <see cref="DimFill.MinimumContentDim"/> is specified. Without it, a <see cref="DimFill"/> SubView will
    ///         receive a size of 0. Use <see cref="Dim.Fill(Dim, Dim?)"/> with a <c>minimumContentDim</c> to ensure
    ///         the SubView contributes a minimum size.
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
    /// <param name="maximumContentDim">The maximum dimension the View's ContentSize will be fit to.</param>
    public static Dim Auto (DimAutoStyle style = DimAutoStyle.Auto, Dim? minimumContentDim = null, Dim? maximumContentDim = null) =>
        new DimAuto (MinimumContentDim: minimumContentDim, MaximumContentDim: maximumContentDim, Style: style);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension, leaving no margin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The view will fill from its position to the end of the SuperView's content area.
    ///     </para>
    ///     <para>
    ///         If the SuperView uses <see cref="Dim.Auto"/>, a <see cref="DimFill"/> SubView does <b>not</b>
    ///         contribute to the auto-sizing calculation and will receive a size of 0. Use
    ///         <see cref="Fill(Dim, Dim?)"/> with a <c>minimumContentDim</c> to ensure the SubView contributes
    ///         a minimum size. See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for details.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var view = new View { X = 5, Y = 0, Width = Dim.Fill(), Height = 1 };
    /// // If SuperView width is 80, view width will be 75 (80 - 5)
    /// </code>
    /// </example>
    /// <returns>The Fill dimension.</returns>
    public static Dim Fill () => new DimFill (0);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension, leaving the specified margin.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the SuperView uses <see cref="Dim.Auto"/>, a <see cref="DimFill"/> SubView does <b>not</b>
    ///         contribute to the auto-sizing calculation and will receive a size of 0. Use
    ///         <see cref="Fill(Dim, Dim?)"/> with a <c>minimumContentDim</c> to ensure the SubView contributes
    ///         a minimum size. See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for details.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var view = new View { X = 0, Y = 0, Width = Dim.Fill(2), Height = 1 };
    /// // If SuperView width is 80, view width will be 78 (80 - 2)
    /// </code>
    /// </example>
    /// <returns>The Fill dimension.</returns>
    /// <param name="margin">Margin to use.</param>
    public static Dim Fill (Dim margin) => new DimFill (margin);

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension, leaving the specified margin and respecting
    ///     the specified minimum dimension.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When the SuperView uses <see cref="Dim.Auto"/>, a <see cref="DimFill"/> SubView does <b>not</b>
    ///         contribute to the auto-sizing calculation by default. The <paramref name="minimumContentDim"/> parameter
    ///         resolves this: it contributes a floor to the auto-sizing calculation, ensuring the SuperView is at least
    ///         large enough to accommodate the minimum. Without it, the SubView will receive a size of 0.
    ///     </para>
    ///     <para>
    ///         See the <a href="../docs/dimauto.md">Dim.Auto Deep Dive</a> for details.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Fill with minimum width of 40
    /// var view = new View { X = 0, Y = 0, Width = Dim.Fill(margin: 0, minimumContentDim: 40), Height = 1 };
    /// // If SuperView has Dim.Auto() width, it will be at least 40 wide
    /// // If SuperView is 80 wide, view will be 80 wide
    /// // If SuperView is 30 wide, view will still be 40 wide (minimum)
    /// </code>
    /// </example>
    /// <returns>The Fill dimension.</returns>
    /// <param name="margin">Margin to use.</param>
    /// <param name="minimumContentDim">
    ///     The minimum dimension. If <see langword="null"/>, no minimum is enforced.
    /// </param>
    public static Dim Fill (Dim margin, Dim? minimumContentDim) => new DimFill (margin, minimumContentDim);

    /// <summary>
    ///     Creates a function <see cref="Dim"/> object that computes the dimension based on the passed view and by executing
    ///     the provided function.
    ///     The function will be called every time the dimension is needed.
    /// </summary>
    /// <param name="function">The function to be executed.</param>
    /// <param name="view">The view where the data will be retrieved.</param>
    /// <returns>The <see cref="Dim"/> returned from the function based on the passed view.</returns>
    public static Dim Func (Func<View?, int> function, View? view = null) => new DimFunc (function, view);

    /// <summary>Creates a <see cref="Dim"/> object that tracks the Height of the specified <see cref="View"/>.</summary>
    /// <returns>The height <see cref="Dim"/> of the other <see cref="View"/>.</returns>
    /// <param name="view">The view that will be tracked.</param>
    public static Dim Height (View? view) => new DimView (view, Dimension.Height);

    /// <summary>Creates a percentage <see cref="Dim"/> object that is a percentage of the width or height of the SuperView.</summary>
    /// <returns>The percent <see cref="Dim"/> object.</returns>
    /// <param name="percent">A value between 0 and 100 representing the percentage.</param>
    /// <param name="mode">the mode. Defaults to <see cref="DimPercentMode.ContentSize"/>.</param>
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
    public static Dim Percent (int percent, DimPercentMode mode = DimPercentMode.ContentSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative (percent);

        return new DimPercent (percent, mode);
    }

    /// <summary>Creates a <see cref="Dim"/> object that tracks the Width of the specified <see cref="View"/>.</summary>
    /// <returns>The width <see cref="Dim"/> of the other <see cref="View"/>.</returns>
    /// <param name="view">The view that will be tracked.</param>
    public static Dim Width (View? view) => new DimView (view, Dimension.Width);

    #endregion static Dim creation methods

    /// <summary>
    ///     Indicates whether the specified type <typeparamref name="TDim"/> is in the hierarchy of this Dim object.
    /// </summary>
    /// <param name="dim">A reference to this <see cref="Dim"/> instance.</param>
    /// <returns></returns>
    public bool Has<TDim> (out TDim dim) where TDim : Dim
    {
        dim = (this as TDim)!;

        return this switch
               {
                   DimCombine combine => combine.Left.Has (out dim) || combine.Right.Has (out dim),
                   TDim => true,
                   _ => false
               };
    }

    #region virtual methods

    /// <summary>
    ///     Gets a dimension that is anchored to a certain point in the layout.
    ///     This method is typically used internally by the layout system to determine the size of a View.
    /// </summary>
    /// <param name="size">The width of the area where the View is being sized (Superview.GetContentSize ()).</param>
    /// <returns>
    ///     An integer representing the calculated dimension. The way this dimension is calculated depends on the specific
    ///     subclass of Dim that is used. For example, DimAbsolute returns a fixed dimension, DimFactor returns a
    ///     dimension that is a certain percentage of the super view's size, and so on.
    /// </returns>
    internal abstract int GetAnchor (int size);

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
    internal virtual int Calculate (int location, int superviewContentSize, View us, Dimension dimension) =>
        Math.Clamp (GetAnchor (superviewContentSize - location), 0, short.MaxValue);

    /// <summary>
    ///     Diagnostics API to determine if this Dim object references other views.
    /// </summary>
    /// <returns></returns>
    internal virtual bool ReferencesOtherViews () => false;

    #endregion virtual methods

    #region operators

    /// <summary>Adds a <see cref="Dim"/> to a <see cref="Dim"/>, yielding a new <see cref="Dim"/>.</summary>
    /// <param name="left">The first <see cref="Dim"/> to add.</param>
    /// <param name="right">The second <see cref="Dim"/> to add.</param>
    /// <returns>The <see cref="Dim"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
    public static Dim operator + (Dim left, Dim right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.GetAnchor (0) + right.GetAnchor (0));
        }

        var newDim = new DimCombine (AddOrSubtract.Add, left, right);
        (left as DimView)?.Target?.SetNeedsLayout ();

        return newDim;
    }

    /// <summary>Creates an Absolute <see cref="Dim"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Dim"/>.</returns>
    /// <param name="n">The value to convert to the pos.</param>
    public static implicit operator Dim (int n) => new DimAbsolute (n);

    /// <summary>
    ///     Subtracts a <see cref="Dim"/> from a <see cref="Dim"/>, yielding a new
    ///     <see cref="Dim"/>.
    /// </summary>
    /// <param name="left">The <see cref="Dim"/> to subtract from (the minuend).</param>
    /// <param name="right">The <see cref="Dim"/> to subtract (the subtrahend).</param>
    /// <returns>The <see cref="Dim"/> that is the <c>left</c> minus <c>right</c>.</returns>
    public static Dim operator - (Dim left, Dim right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.GetAnchor (0) - right.GetAnchor (0));
        }

        var newDim = new DimCombine (AddOrSubtract.Subtract, left, right);
        (left as DimView)?.Target?.SetNeedsLayout ();

        return newDim;
    }

    #endregion operators
}
