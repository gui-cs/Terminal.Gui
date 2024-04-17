namespace Terminal.Gui;

/// <summary>
///     Describes the position of a <see cref="View"/> which can be an absolute value, a percentage, centered, or
///     relative to the ending dimension. Integer values are implicitly convertible to an absolute <see cref="Pos"/>. These
///     objects are created using the static methods Percent, AnchorEnd, and Center. The <see cref="Pos"/> objects can be
///     combined with the addition and subtraction operators.
/// </summary>
/// <remarks>
///     <para>Use the <see cref="Pos"/> objects on the X or Y properties of a view to control the position.</para>
///     <para>
///         These can be used to set the absolute position, when merely assigning an integer value (via the implicit
///         integer to <see cref="Pos"/> conversion), and they can be combined to produce more useful layouts, like:
///         Pos.Center - 3, which would shift the position of the <see cref="View"/> 3 characters to the left after
///         centering for example.
///     </para>
///     <para>
///         Reference coordinates of another view by using the methods Left(View), Right(View), Bottom(View), Top(View).
///         The X(View) and Y(View) are aliases to Left(View) and Top(View) respectively.
///     </para>
///     <para>
///         <list type="table">
///             <listheader>
///                 <term>Pos Object</term> <description>Description</description>
///             </listheader>
///             <item>
///                 <term>
///                     <see cref="Pos.Function(Func{int})"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that computes the position by executing the provided
///                     function. The function will be called every time the position is needed.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Percent(float)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that is a percentage of the width or height of the
///                     SuperView.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.AnchorEnd(int)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that is anchored to the end (right side or bottom) of
///                     the dimension, useful to flush the layout from the right or bottom.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Center"/>
///                 </term>
///                 <description>Creates a <see cref="Pos"/> object that can be used to center the <see cref="View"/>.</description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.At(int)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that is an absolute position based on the specified
///                     integer value.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Left"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.X(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Top(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Y(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified
///                     <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Right(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Right (X+Width) coordinate of the
///                     specified <see cref="View"/>.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.Bottom(View)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that tracks the Bottom (Y+Height) coordinate of the
///                     specified <see cref="View"/>
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public class Pos
{
    /// <summary>
    ///     Creates a <see cref="Pos"/> object that is anchored to the end (right side or
    ///     bottom) of the SuperView, minus the respective dimension of the View. This is equivalent to using
    ///     <see cref="Pos.AnchorEnd(int)"/>,
    ///     with an offset equivalent to the View's respective dimension.
    /// </summary>
    /// <returns>The <see cref="Pos"/> object anchored to the end (the bottom or the right side) minus the View's dimension.</returns>
    /// <example>
    ///     This sample shows how align a <see cref="Button"/> to the bottom-right the SuperView.
    ///     <code>
    /// anchorButton.X = Pos.AnchorEnd ();
    /// anchorButton.Y = Pos.AnchorEnd ();
    /// </code>
    /// </example>
    public static Pos AnchorEnd () { return new PosAnchorEnd (); }

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that is anchored to the end (right side or bottom) of the SuperView,
    ///     useful to flush the layout from the right or bottom.
    /// </summary>
    /// <returns>The <see cref="Pos"/> object anchored to the end (the bottom or the right side).</returns>
    /// <param name="offset">The view will be shifted left or up by the amount specified.</param>
    /// <example>
    ///     This sample shows how align a 10 column wide <see cref="Button"/> to the bottom-right the SuperView.
    ///     <code>
    /// anchorButton.X = Pos.AnchorEnd (10);
    /// anchorButton.Y = 1
    /// </code>
    /// </example>
    public static Pos AnchorEnd (int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentException (@"Must be positive", nameof (offset));
        }

        return new PosAnchorEnd (offset);
    }

    /// <summary>Creates a <see cref="Pos"/> object that is an absolute position based on the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Pos"/>.</returns>
    /// <param name="n">The value to convert to the <see cref="Pos"/>.</param>
    public static Pos At (int n) { return new PosAbsolute (n); }

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that tracks the Bottom (Y+Height) coordinate of the specified
    ///     <see cref="View"/>
    /// </summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Bottom (View view) { return new PosView (view, Side.Bottom); }

    /// <summary>Creates a <see cref="Pos"/> object that can be used to center the <see cref="View"/>.</summary>
    /// <returns>The center Pos.</returns>
    /// <example>
    ///     This creates a <see cref="TextView"/> centered horizontally, is 50% of the way down, is 30% the height, and
    ///     is 80% the width of the <see cref="View"/> it added to.
    ///     <code>
    ///  var textView = new TextView () {
    ///     X = Pos.Center (),
    ///     Y = Pos.Percent (50),
    ///     Width = Dim.Percent (80),
    ///     Height = Dim.Percent (30),
    ///  };
    ///  </code>
    /// </example>
    public static Pos Center () { return new PosCenter (); }

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="other">The object to compare with the current object. </param>
    /// <returns>
    ///     <see langword="true"/> if the specified object  is equal to the current object; otherwise,
    ///     <see langword="false"/>.
    /// </returns>
    public override bool Equals (object other) { return other is Pos abs && abs == this; }

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that computes the position by executing the provided function. The function
    ///     will be called every time the position is needed.
    /// </summary>
    /// <param name="function">The function to be executed.</param>
    /// <returns>The <see cref="Pos"/> returned from the function.</returns>
    public static Pos Function (Func<int> function) { return new PosFunc (function); }

    /// <summary>Serves as the default hash function. </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode () { return Anchor (0).GetHashCode (); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Left (View view) { return new PosView (view, Side.X); }

    /// <summary>Adds a <see cref="Terminal.Gui.Pos"/> to a <see cref="Terminal.Gui.Pos"/>, yielding a new <see cref="Pos"/>.</summary>
    /// <param name="left">The first <see cref="Terminal.Gui.Pos"/> to add.</param>
    /// <param name="right">The second <see cref="Terminal.Gui.Pos"/> to add.</param>
    /// <returns>The <see cref="Pos"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
    public static Pos operator + (Pos left, Pos right)
    {
        if (left is PosAbsolute && right is PosAbsolute)
        {
            return new PosAbsolute (left.Anchor (0) + right.Anchor (0));
        }

        var newPos = new PosCombine (true, left, right);

        if (left is PosView view)
        {
            view.Target.SetNeedsLayout ();
        }

        return newPos;
    }

    /// <summary>Creates an Absolute <see cref="Pos"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Pos"/>.</returns>
    /// <param name="n">The value to convert to the <see cref="Pos"/> .</param>
    public static implicit operator Pos (int n) { return new PosAbsolute (n); }

    /// <summary>
    ///     Subtracts a <see cref="Terminal.Gui.Pos"/> from a <see cref="Terminal.Gui.Pos"/>, yielding a new
    ///     <see cref="Pos"/>.
    /// </summary>
    /// <param name="left">The <see cref="Terminal.Gui.Pos"/> to subtract from (the minuend).</param>
    /// <param name="right">The <see cref="Terminal.Gui.Pos"/> to subtract (the subtrahend).</param>
    /// <returns>The <see cref="Pos"/> that is the <c>left</c> minus <c>right</c>.</returns>
    public static Pos operator - (Pos left, Pos right)
    {
        if (left is PosAbsolute && right is PosAbsolute)
        {
            return new PosAbsolute (left.Anchor (0) - right.Anchor (0));
        }

        var newPos = new PosCombine (false, left, right);

        if (left is PosView view)
        {
            view.Target.SetNeedsLayout ();
        }

        return newPos;
    }

    /// <summary>Creates a percentage <see cref="Pos"/> object</summary>
    /// <returns>The percent <see cref="Pos"/> object.</returns>
    /// <param name="percent">A value between 0 and 100 representing the percentage.</param>
    /// <example>
    ///     This creates a <see cref="TextField"/> centered horizontally, is 50% of the way down, is 30% the height, and
    ///     is 80% the width of the <see cref="View"/> it added to.
    ///     <code>
    ///  var textView = new TextField {
    ///      X = Pos.Center (),
    ///      Y = Pos.Percent (50),
    ///      Width = Dim.Percent (80),
    ///      Height = Dim.Percent (30),
    ///  };
    ///  </code>
    /// </example>
    public static Pos Percent (float percent)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentException ("Percent value must be between 0 and 100.");
        }

        return new PosFactor (percent / 100);
    }

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that tracks the Right (X+Width) coordinate of the specified
    ///     <see cref="View"/>.
    /// </summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Right (View view) { return new PosView (view, Side.Right); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Top (View view) { return new PosView (view, Side.Y); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos X (View view) { return new PosView (view, Side.X); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Y (View view) { return new PosView (view, Side.Y); }

    internal virtual int Anchor (int width) { return 0; }

    /// <summary>
    ///     Calculates and returns the position of a <see cref="View"/> object. It takes into account the dimension of the
    ///     superview and the dimension of the view itself.
    /// </summary>
    /// <param name="superviewDimension">
    ///     The dimension of the superview. This could be the width for x-coordinate calculation or the
    ///     height for y-coordinate calculation.
    /// </param>
    /// <param name="dim">The dimension of the View. It could be the current width or height.</param>
    /// <param name="autosize">Obsolete; to be deprecated.</param>
    /// <param name="autoSize">Obsolete; to be deprecated.</param>
    /// <returns>
    ///     The calculated position of the View. The way this position is calculated depends on the specific subclass of Pos that
    ///     is used.
    /// </returns>

    internal virtual int Calculate (int superviewDimension, Dim dim, int autosize, bool autoSize) { return Anchor (superviewDimension); }

    internal class PosAbsolute (int n) : Pos
    {
        private readonly int _n = n;
        public override bool Equals (object other) { return other is PosAbsolute abs && abs._n == _n; }
        public override int GetHashCode () { return _n.GetHashCode (); }
        public override string ToString () { return $"Absolute({_n})"; }
        internal override int Anchor (int width) { return _n; }
    }

    internal class PosAnchorEnd : Pos
    {
        private readonly int _offset;
        public PosAnchorEnd () { UseDimForOffset = true; }
        public PosAnchorEnd (int offset) { _offset = offset; }
        public override bool Equals (object other) { return other is PosAnchorEnd anchorEnd && anchorEnd._offset == _offset; }
        public override int GetHashCode () { return _offset.GetHashCode (); }

        /// <summary>
        ///     If true, the offset is the width of the view, if false, the offset is the offset value.
        /// </summary>
        internal bool UseDimForOffset { get; set; }

        public override string ToString () { return UseDimForOffset ? "AnchorEnd()" : $"AnchorEnd({_offset})"; }

        internal override int Anchor (int width)
        {
            if (UseDimForOffset)
            {
                return width;
            }

            return width - _offset;
        }

        internal override int Calculate (int superviewDimension, Dim dim, int autosize, bool autoSize)
        {
            int newLocation = Anchor (superviewDimension);

            if (UseDimForOffset)
            {
                newLocation -= dim.Anchor (superviewDimension);
            }

            return newLocation;
        }
    }

    internal class PosCenter : Pos
    {
        public override string ToString () { return "Center"; }
        internal override int Anchor (int width) { return width / 2; }

        internal override int Calculate (int superviewDimension, Dim dim, int autosize, bool autoSize)
        {
            int newDimension = Math.Max (dim.Calculate (0, superviewDimension, autosize, autoSize), 0);

            return Anchor (superviewDimension - newDimension);
        }
    }

    internal class PosCombine (bool add, Pos left, Pos right) : Pos
    {
        internal bool _add = add;
        internal Pos _left = left, _right = right;

        public override string ToString () { return $"Combine({_left}{(_add ? '+' : '-')}{_right})"; }

        internal override int Anchor (int width)
        {
            int la = _left.Anchor (width);
            int ra = _right.Anchor (width);

            if (_add)
            {
                return la + ra;
            }

            return la - ra;
        }

        internal override int Calculate (int superviewDimension, Dim dim, int autosize, bool autoSize)
        {
            int newDimension = dim.Calculate (0, superviewDimension, autosize, autoSize);
            int left = _left.Calculate (superviewDimension, dim, autosize, autoSize);
            int right = _right.Calculate (superviewDimension, dim, autosize, autoSize);

            if (_add)
            {
                return left + right;
            }

            return left - right;
        }
    }

    internal class PosFactor (float factor) : Pos
    {
        private readonly float _factor = factor;
        public override bool Equals (object other) { return other is PosFactor f && f._factor == _factor; }
        public override int GetHashCode () { return _factor.GetHashCode (); }
        public override string ToString () { return $"Factor({_factor})"; }
        internal override int Anchor (int width) { return (int)(width * _factor); }
    }

    // Helper class to provide dynamic value by the execution of a function that returns an integer.
    internal class PosFunc (Func<int> n) : Pos
    {
        private readonly Func<int> _function = n;
        public override bool Equals (object other) { return other is PosFunc f && f._function () == _function (); }
        public override int GetHashCode () { return _function.GetHashCode (); }
        public override string ToString () { return $"PosFunc({_function ()})"; }
        internal override int Anchor (int width) { return _function (); }
    }

    internal enum Side
    {
        X = 0,
        Y = 1,
        Right = 2,
        Bottom = 3
    }

    internal class PosView (View view, Side side) : Pos
    {
        public readonly View Target = view;

        public override bool Equals (object other) { return other is PosView abs && abs.Target == Target; }
        public override int GetHashCode () { return Target.GetHashCode (); }

        public override string ToString ()
        {
            string sideString = side switch
                                {
                                    Side.X => "x",
                                    Side.Y => "y",
                                    Side.Right => "right",
                                    Side.Bottom => "bottom",
                                    _ => "unknown"
                                };

            if (Target == null)
            {
                throw new NullReferenceException (nameof (Target));
            }

            return $"View(side={sideString},target={Target})";
        }

        internal override int Anchor (int width)
        {
            return side switch
                   {
                       Side.X => Target.Frame.X,
                       Side.Y => Target.Frame.Y,
                       Side.Right => Target.Frame.Right,
                       Side.Bottom => Target.Frame.Bottom,
                       _ => 0
                   };
        }
    }
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
///                     <see cref="Dim.Function(Func{int})"/>
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
public class Dim
{
    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="other">The object to compare with the current object. </param>
    /// <returns>
    ///     <see langword="true"/> if the specified object  is equal to the current object; otherwise,
    ///     <see langword="false"/>.
    /// </returns>
    public override bool Equals (object other) { return other is Dim abs && abs == this; }

    /// <summary>
    ///     Creates a <see cref="Dim"/> object that fills the dimension, leaving the specified number of columns for a
    ///     margin.
    /// </summary>
    /// <returns>The Fill dimension.</returns>
    /// <param name="margin">Margin to use.</param>
    public static Dim Fill (int margin = 0) { return new DimFill (margin); }

    /// <summary>
    ///     Creates a function <see cref="Dim"/> object that computes the dimension by executing the provided function.
    ///     The function will be called every time the dimension is needed.
    /// </summary>
    /// <param name="function">The function to be executed.</param>
    /// <returns>The <see cref="Dim"/> returned from the function.</returns>
    public static Dim Function (Func<int> function) { return new DimFunc (function); }

    /// <summary>Serves as the default hash function. </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode () { return Anchor (0).GetHashCode (); }

    /// <summary>Creates a <see cref="Dim"/> object that tracks the Height of the specified <see cref="View"/>.</summary>
    /// <returns>The height <see cref="Dim"/> of the other <see cref="View"/>.</returns>
    /// <param name="view">The view that will be tracked.</param>
    public static Dim Height (View view) { return new DimView (view, Side.Height); }

    /// <summary>Adds a <see cref="Dim"/> to a <see cref="Dim"/>, yielding a new <see cref="Dim"/>.</summary>
    /// <param name="left">The first <see cref="Dim"/> to add.</param>
    /// <param name="right">The second <see cref="Dim"/> to add.</param>
    /// <returns>The <see cref="Dim"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
    public static Dim operator + (Dim left, Dim right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.Anchor (0) + right.Anchor (0));
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
    public static Dim operator - (Dim left, Dim right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.Anchor (0) - right.Anchor (0));
        }

        var newDim = new DimCombine (false, left, right);
        (left as DimView)?.Target.SetNeedsLayout ();

        return newDim;
    }

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
    public static Dim Percent (float percent, bool usePosition = false)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentException ("Percent value must be between 0 and 100");
        }

        return new DimFactor (percent / 100, usePosition);
    }

    /// <summary>Creates an Absolute <see cref="Dim"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Dim"/>.</returns>
    /// <param name="n">The value to convert to the <see cref="Dim"/>.</param>
    public static Dim Sized (int n) { return new DimAbsolute (n); }

    /// <summary>Creates a <see cref="Dim"/> object that tracks the Width of the specified <see cref="View"/>.</summary>
    /// <returns>The width <see cref="Dim"/> of the other <see cref="View"/>.</returns>
    /// <param name="view">The view that will be tracked.</param>
    public static Dim Width (View view) { return new DimView (view, Side.Width); }

    internal virtual int Anchor (int width) { return 0; }

    /// <summary>
    ///     Calculates and returns the dimension of a <see cref="View"/> object. It takes into account the location of the
    ///     <see cref="View"/>, its current size, and whether it should automatically adjust its size based on its content.
    /// </summary>
    /// <param name="location">
    ///     The starting point from where the size calculation begins. It could be the left edge for width calculation or the
    ///     top edge for height calculation.
    /// </param>
    /// <param name="dimension">The current size of the View. It could be the current width or height.</param>
    /// <param name="autosize">Obsolete; To be deprecated.</param>
    /// <param name="autoSize">Obsolete; To be deprecated.</param>
    /// <returns>
    ///     The calculated size of the View. The way this size is calculated depends on the specific subclass of Dim that
    ///     is used.
    /// </returns>
    internal virtual int Calculate (int location, int dimension, int autosize, bool autoSize)
    {
        int newDimension = Math.Max (Anchor (dimension - location), 0);

        return autoSize && autosize > newDimension ? autosize : newDimension;
    }

    internal class DimAbsolute (int n) : Dim
    {
        private readonly int _n = n;
        public override bool Equals (object other) { return other is DimAbsolute abs && abs._n == _n; }
        public override int GetHashCode () { return _n.GetHashCode (); }
        public override string ToString () { return $"Absolute({_n})"; }
        internal override int Anchor (int width) { return _n; }

        internal override int Calculate (int location, int dimension, int autosize, bool autoSize)
        {
            // DimAbsolute.Anchor (int width) ignores width and returns n
            int newDimension = Math.Max (Anchor (0), 0);

            return autoSize && autosize > newDimension ? autosize : newDimension;
        }
    }

    internal class DimCombine (bool add, Dim left, Dim right) : Dim
    {
        internal bool _add = add;
        internal Dim _left = left, _right = right;

        public override string ToString () { return $"Combine({_left}{(_add ? '+' : '-')}{_right})"; }

        internal override int Anchor (int width)
        {
            int la = _left.Anchor (width);
            int ra = _right.Anchor (width);

            if (_add)
            {
                return la + ra;
            }

            return la - ra;
        }

        internal override int Calculate (int location, int dimension, int autosize, bool autoSize)
        {
            int leftNewDim = _left.Calculate (location, dimension, autosize, autoSize);
            int rightNewDim = _right.Calculate (location, dimension, autosize, autoSize);

            int newDimension;

            if (_add)
            {
                newDimension = leftNewDim + rightNewDim;
            }
            else
            {
                newDimension = Math.Max (0, leftNewDim - rightNewDim);
            }

            return autoSize && autosize > newDimension ? autosize : newDimension;
        }
    }

    internal class DimFactor (float factor, bool remaining = false) : Dim
    {
        private readonly float _factor = factor;
        private readonly bool _remaining = remaining;

        public override bool Equals (object other) { return other is DimFactor f && f._factor == _factor && f._remaining == _remaining; }
        public override int GetHashCode () { return _factor.GetHashCode (); }
        public bool IsFromRemaining () { return _remaining; }
        public override string ToString () { return $"Factor({_factor},{_remaining})"; }
        internal override int Anchor (int width) { return (int)(width * _factor); }

        internal override int Calculate (int location, int dimension, int autosize, bool autoSize)
        {
            int newDimension = _remaining ? Math.Max (Anchor (dimension - location), 0) : Anchor (dimension);

            return autoSize && autosize > newDimension ? autosize : newDimension;
        }
    }

    internal class DimFill (int margin) : Dim
    {
        private readonly int _margin = margin;
        public override bool Equals (object other) { return other is DimFill fill && fill._margin == _margin; }
        public override int GetHashCode () { return _margin.GetHashCode (); }
        public override string ToString () { return $"Fill({_margin})"; }
        internal override int Anchor (int width) { return width - _margin; }
    }

    // Helper class to provide dynamic value by the execution of a function that returns an integer.
    internal class DimFunc (Func<int> n) : Dim
    {
        private readonly Func<int> _function = n;
        public override bool Equals (object other) { return other is DimFunc f && f._function () == _function (); }
        public override int GetHashCode () { return _function.GetHashCode (); }
        public override string ToString () { return $"DimFunc({_function ()})"; }
        internal override int Anchor (int width) { return _function (); }
    }

    internal enum Side
    {
        Height = 0,
        Width = 1
    }

    internal class DimView : Dim
    {
        private readonly Side _side;

        internal DimView (View view, Side side)
        {
            Target = view;
            _side = side;
        }

        public View Target { get; init; }
        public override bool Equals (object other) { return other is DimView abs && abs.Target == Target; }
        public override int GetHashCode () { return Target.GetHashCode (); }

        public override string ToString ()
        {
            if (Target == null)
            {
                throw new NullReferenceException ();
            }

            string sideString = _side switch
                                {
                                    Side.Height => "Height",
                                    Side.Width => "Width",
                                    _ => "unknown"
                                };

            return $"View({sideString},{Target})";
        }

        internal override int Anchor (int width)
        {
            return _side switch
                   {
                       Side.Height => Target.Frame.Height,
                       Side.Width => Target.Frame.Width,
                       _ => 0
                   };
        }
    }
}
