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
///                     <see cref="Pos.Anchor(int)"/>
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
    ///     Creates a <see cref="Pos"/> object that has its end (right side or bottom) anchored to the end (right side or
    ///     bottom)
    ///     of the SuperView, useful to flush the layout from the right or bottom.
    /// </summary>
    /// <returns>The <see cref="Pos"/> object anchored to the end (the bottom or the right side).</returns>
    /// <example>
    ///     This sample shows how align a <see cref="Button"/> to the bottom-right the SuperView.
    /// <code>
    /// anchorButton.X = Pos.AnchorEnd (0);
    /// anchorButton.Y = Pos.AnchorEnd (0);
    /// </code>
    /// </example>
    public static Pos AnchorEnd ()
    {
        return new PosAnchorEnd ();
    }

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that is anchored to the end (right side or bottom) of the SuperView,
    ///     useful to flush the layout from the right or bottom.
    /// </summary>
    /// <returns>The <see cref="Pos"/> object anchored to the end (the bottom or the right side).</returns>
    /// <param name="offset">The view will be shifted left or up by the amount specified.</param>
    /// <example>
    ///     This sample shows how align a <see cref="Button"/> such that its left side is offset 10 columns from
    ///     the right edge of the SuperView.
    /// <code>
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
    public static Pos Bottom (View view) { return new PosView (view, 3); }

    /// <summary>Creates a <see cref="Pos"/> object that can be used to center the <see cref="View"/>.</summary>
    /// <returns>The center Pos.</returns>
    /// <example>
    ///     This creates a <see cref="TextField"/>that is centered horizontally, is 50% of the way down, is 30% the height, and
    ///     is 80% the width of the <see cref="View"/> it added to.
    ///     <code>
    ///  var textView = new TextView () {
    /// 	X = Pos.Center (),
    /// 	Y = Pos.Percent (50),
    /// 	Width = Dim.Percent (80),
    ///  	Height = Dim.Percent (30),
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
    public static Pos Left (View view) { return new PosView (view, 0); }

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
        SetPosCombine (left, newPos);

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
        SetPosCombine (left, newPos);

        return newPos;
    }

    /// <summary>Creates a percentage <see cref="Pos"/> object</summary>
    /// <returns>The percent <see cref="Pos"/> object.</returns>
    /// <param name="n">A value between 0 and 100 representing the percentage.</param>
    /// <example>
    ///     This creates a <see cref="TextField"/>that is centered horizontally, is 50% of the way down, is 30% the height, and
    ///     is 80% the width of the <see cref="View"/> it added to.
    ///     <code>
    ///  var textView = new TextView () {
    /// 	X = Pos.Center (),
    /// 	Y = Pos.Percent (50),
    /// 	Width = Dim.Percent (80),
    ///  	Height = Dim.Percent (30),
    ///  };
    ///  </code>
    /// </example>
    public static Pos Percent (float n)
    {
        if (n is < 0 or > 100)
        {
            throw new ArgumentException ("Percent value must be between 0 and 100");
        }

        return new PosFactor (n / 100);
    }

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that tracks the Right (X+Width) coordinate of the specified
    ///     <see cref="View"/>.
    /// </summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Right (View view) { return new PosView (view, 2); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Top (View view) { return new PosView (view, 1); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos X (View view) { return new PosView (view, 0); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Y (View view) { return new PosView (view, 1); }

    internal virtual int Anchor (int width) { return 0; }

    private static void SetPosCombine (Pos left, PosCombine newPos)
    {
        var view = left as PosView;

        if (view is { })
        {
            view.Target.SetNeedsLayout ();
        }
    }

    internal class PosAbsolute : Pos
    {
        private readonly int _n;
        public PosAbsolute (int n) { _n = n; }
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

        public bool UseDimForOffset { get; set; }

        public override string ToString ()
        {
            if (UseDimForOffset)
            {
                return "AnchorEnd()";
            }
            return $"AnchorEnd({_offset})";
        }

        internal override int Anchor (int width)
        {
            if (UseDimForOffset)
            {
                return width;
            }
            return width - _offset;
        }
    }

    internal class PosCenter : Pos
    {
        public override string ToString () { return "Center"; }
        internal override int Anchor (int width) { return width / 2; }
    }

    internal class PosCombine : Pos
    {
        internal bool _add;
        internal Pos _left, _right;

        public PosCombine (bool add, Pos left, Pos right)
        {
            _left = left;
            _right = right;
            _add = add;
        }

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
    }

    internal class PosFactor : Pos
    {
        private readonly float _factor;
        public PosFactor (float n) { _factor = n; }
        public override bool Equals (object other) { return other is PosFactor f && f._factor == _factor; }
        public override int GetHashCode () { return _factor.GetHashCode (); }
        public override string ToString () { return $"Factor({_factor})"; }
        internal override int Anchor (int width) { return (int)(width * _factor); }
    }

    // Helper class to provide dynamic value by the execution of a function that returns an integer.
    internal class PosFunc : Pos
    {
        private readonly Func<int> _function;
        public PosFunc (Func<int> n) { _function = n; }
        public override bool Equals (object other) { return other is PosFunc f && f._function () == _function (); }
        public override int GetHashCode () { return _function.GetHashCode (); }
        public override string ToString () { return $"PosFunc({_function ()})"; }
        internal override int Anchor (int width) { return _function (); }
    }

    internal class PosView : Pos
    {
        public readonly View Target;

        private readonly int side;

        public PosView (View view, int side)
        {
            Target = view;
            this.side = side;
        }

        public override bool Equals (object other) { return other is PosView abs && abs.Target == Target; }
        public override int GetHashCode () { return Target.GetHashCode (); }

        public override string ToString ()
        {
            string tside;

            switch (side)
            {
                case 0:
                    tside = "x";

                    break;
                case 1:
                    tside = "y";

                    break;
                case 2:
                    tside = "right";

                    break;
                case 3:
                    tside = "bottom";

                    break;
                default:
                    tside = "unknown";

                    break;
            }

            if (Target is null)
            {
                throw new NullReferenceException (nameof (Target));
            }

            return $"View(side={tside},target={Target})";
        }

        internal override int Anchor (int width)
        {
            switch (side)
            {
                case 0: return Target.Frame.X;
                case 1: return Target.Frame.Y;
                case 2: return Target.Frame.Right;
                case 3: return Target.Frame.Bottom;
                default:
                    return 0;
            }
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
///                     Creates a <see cref="Dim"/> object that fills the dimension, leaving the specified number
///                     of columns for a margin.
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
    public static Dim Height (View view) { return new DimView (view, 0); }

    /// <summary>Adds a <see cref="Terminal.Gui.Dim"/> to a <see cref="Terminal.Gui.Dim"/>, yielding a new <see cref="Dim"/>.</summary>
    /// <param name="left">The first <see cref="Terminal.Gui.Dim"/> to add.</param>
    /// <param name="right">The second <see cref="Terminal.Gui.Dim"/> to add.</param>
    /// <returns>The <see cref="Dim"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
    public static Dim operator + (Dim left, Dim right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.Anchor (0) + right.Anchor (0));
        }

        var newDim = new DimCombine (true, left, right);
        SetDimCombine (left, newDim);

        return newDim;
    }

    /// <summary>Creates an Absolute <see cref="Dim"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Dim"/>.</returns>
    /// <param name="n">The value to convert to the pos.</param>
    public static implicit operator Dim (int n) { return new DimAbsolute (n); }

    /// <summary>
    ///     Subtracts a <see cref="Terminal.Gui.Dim"/> from a <see cref="Terminal.Gui.Dim"/>, yielding a new
    ///     <see cref="Dim"/>.
    /// </summary>
    /// <param name="left">The <see cref="Terminal.Gui.Dim"/> to subtract from (the minuend).</param>
    /// <param name="right">The <see cref="Terminal.Gui.Dim"/> to subtract (the subtrahend).</param>
    /// <returns>The <see cref="Dim"/> that is the <c>left</c> minus <c>right</c>.</returns>
    public static Dim operator - (Dim left, Dim right)
    {
        if (left is DimAbsolute && right is DimAbsolute)
        {
            return new DimAbsolute (left.Anchor (0) - right.Anchor (0));
        }

        var newDim = new DimCombine (false, left, right);
        SetDimCombine (left, newDim);

        return newDim;
    }

    /// <summary>Creates a percentage <see cref="Dim"/> object that is a percentage of the width or height of the SuperView.</summary>
    /// <returns>The percent <see cref="Dim"/> object.</returns>
    /// <param name="n">A value between 0 and 100 representing the percentage.</param>
    /// <param name="r">
    ///     If <c>true</c> the Percent is computed based on the remaining space after the X/Y anchor positions. If
    ///     <c>false</c> is computed based on the whole original space.
    /// </param>
    /// <example>
    ///     This initializes a <see cref="TextField"/>that is centered horizontally, is 50% of the way down, is 30% the height,
    ///     and is 80% the width of the <see cref="View"/> it added to.
    ///     <code>
    ///  var textView = new TextView () {
    /// 	X = Pos.Center (),
    /// 	Y = Pos.Percent (50),
    /// 	Width = Dim.Percent (80),
    ///  	Height = Dim.Percent (30),
    ///  };
    ///  </code>
    /// </example>
    public static Dim Percent (float n, bool r = false)
    {
        if (n is < 0 or > 100)
        {
            throw new ArgumentException ("Percent value must be between 0 and 100");
        }

        return new DimFactor (n / 100, r);
    }

    /// <summary>Creates an Absolute <see cref="Dim"/> from the specified integer value.</summary>
    /// <returns>The Absolute <see cref="Dim"/>.</returns>
    /// <param name="n">The value to convert to the <see cref="Dim"/>.</param>
    public static Dim Sized (int n) { return new DimAbsolute (n); }

    /// <summary>Creates a <see cref="Dim"/> object that tracks the Width of the specified <see cref="View"/>.</summary>
    /// <returns>The width <see cref="Dim"/> of the other <see cref="View"/>.</returns>
    /// <param name="view">The view that will be tracked.</param>
    public static Dim Width (View view) { return new DimView (view, 1); }

    internal virtual int Anchor (int width) { return 0; }

    // BUGBUG: newPos is never used.
    private static void SetDimCombine (Dim left, DimCombine newPos) { (left as DimView)?.Target.SetNeedsLayout (); }

    internal class DimAbsolute : Dim
    {
        private readonly int _n;
        public DimAbsolute (int n) { _n = n; }
        public override bool Equals (object other) { return other is DimAbsolute abs && abs._n == _n; }
        public override int GetHashCode () { return _n.GetHashCode (); }
        public override string ToString () { return $"Absolute({_n})"; }
        internal override int Anchor (int width) { return _n; }
    }

    internal class DimCombine : Dim
    {
        internal bool _add;
        internal Dim _left, _right;

        public DimCombine (bool add, Dim left, Dim right)
        {
            _left = left;
            _right = right;
            _add = add;
        }

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
    }

    internal class DimFactor : Dim
    {
        private readonly float _factor;
        private readonly bool _remaining;

        public DimFactor (float n, bool r = false)
        {
            _factor = n;
            _remaining = r;
        }

        public override bool Equals (object other) { return other is DimFactor f && f._factor == _factor && f._remaining == _remaining; }
        public override int GetHashCode () { return _factor.GetHashCode (); }
        public bool IsFromRemaining () { return _remaining; }
        public override string ToString () { return $"Factor({_factor},{_remaining})"; }
        internal override int Anchor (int width) { return (int)(width * _factor); }
    }

    internal class DimFill : Dim
    {
        private readonly int _margin;
        public DimFill (int margin) { _margin = margin; }
        public override bool Equals (object other) { return other is DimFill fill && fill._margin == _margin; }
        public override int GetHashCode () { return _margin.GetHashCode (); }
        public override string ToString () { return $"Fill({_margin})"; }
        internal override int Anchor (int width) { return width - _margin; }
    }

    // Helper class to provide dynamic value by the execution of a function that returns an integer.
    internal class DimFunc : Dim
    {
        private readonly Func<int> _function;
        public DimFunc (Func<int> n) { _function = n; }
        public override bool Equals (object other) { return other is DimFunc f && f._function () == _function (); }
        public override int GetHashCode () { return _function.GetHashCode (); }
        public override string ToString () { return $"DimFunc({_function ()})"; }
        internal override int Anchor (int width) { return _function (); }
    }

    internal class DimView : Dim
    {
        private readonly int _side;

        public DimView (View view, int side)
        {
            Target = view;
            _side = side;
        }

        public View Target { get; init; }
        public override bool Equals (object other) { return other is DimView abs && abs.Target == Target; }
        public override int GetHashCode () { return Target.GetHashCode (); }

        public override string ToString ()
        {
            if (Target is null)
            {
                throw new NullReferenceException ();
            }

            string tside = _side switch
                           {
                               0 => "Height",
                               1 => "Width",
                               _ => "unknown"
                           };

            return $"View({tside},{Target})";
        }

        internal override int Anchor (int width)
        {
            return _side switch
                   {
                       0 => Target.Frame.Height,
                       1 => Target.Frame.Width,
                       _ => 0
                   };
        }
    }
}
