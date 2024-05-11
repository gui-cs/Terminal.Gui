using System.ComponentModel;
using System.Diagnostics;
using static Terminal.Gui.Dim;

namespace Terminal.Gui;

/// <summary>
///     Describes the horizontal or vertical position of a <see cref="View"/>. The position can be an absolute value,
///     a percentage, centered, a function, relative to the ending dimension, relative to another View, or aligned with a
///     set of views.
///     Integer values are implicitly convertible to an absolute <see cref="Pos"/>. <see cref="Pos"/>
///     objects are created using the static methods provided (e.g. <see cref="Percent"/> and <see cref="Center"/>).
///     The <see cref="Pos"/> objects can be combined with the addition and subtraction operators.
/// </summary>
/// <remarks>
///     <para>Use the <see cref="Pos"/> objects on the X or Y properties of a <see cref="View"/> to control the position.</para>
///     <para>
///         These can be used to set the absolute position, when merely assigning an integer value (via the implicit
///         integer to <see cref="Pos"/> conversion), and they can be combined to produce more useful layouts, like:
///         <c>Pos.Center - 3</c>, which would shift the position of the <see cref="View"/> 3 characters to the left after
///         centering for example.
///     </para>
///     <para>
///         <list type="table">
///             <listheader>
///                 <term>Pos Object</term> <description>Description</description>
///             </listheader>
///             <item>
///                 <term>
///                     <see cref="Pos.Align"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that aligns a set of views.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.AnchorEnd()"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that is anchored to the end (right side or bottom) of
///                     the dimension, useful to flush the layout from the right or bottom.
///                 </description>
///             </item>
///             <item>
///                 <term>
///                     <see cref="Pos.At(int)"/>
///                 </term>
///                 <description>
///                     Creates a <see cref="Pos"/> object that is an absolute position based on the specified
///                     integer.
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
///                     <see cref="Pos.Left(View)"/>
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
    #region Static Methods - Create Pos objects

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
    ///     useful to flush the layout from the right or bottom. See also <see cref="Pos.AnchorEnd()"/>, which uses the view
    ///     dimension to ensure the view is fully visible.
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

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that computes the position by executing the provided function. The function
    ///     will be called every time the position is needed.
    /// </summary>
    /// <param name="function">The function to be executed.</param>
    /// <returns>The <see cref="Pos"/> returned from the function.</returns>
    public static Pos Function (Func<int> function) { return new PosFunc (function); }

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that aligns a set of views according to the specified alignment setting.
    /// </summary>
    /// <param name="alignment"></param>
    /// <param name="groupId">
    ///     The optional, unique identifier for the set of views to align according to
    ///     <paramref name="alignment"/>.
    /// </param>
    /// <returns></returns>
    public static Pos Align (Alignment alignment, int groupId = 0) { return new PosAlign (alignment, groupId); }

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

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Top (View view) { return new PosView (view, Side.Top); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Top (Y) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Y (View view) { return new PosView (view, Side.Top); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Left (View view) { return new PosView (view, Side.Left); }

    /// <summary>Creates a <see cref="Pos"/> object that tracks the Left (X) position of the specified <see cref="View"/>.</summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos X (View view) { return new PosView (view, Side.Left); }

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that tracks the Bottom (Y+Height) coordinate of the specified
    ///     <see cref="View"/>
    /// </summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Bottom (View view) { return new PosView (view, Side.Bottom); }

    /// <summary>
    ///     Creates a <see cref="Pos"/> object that tracks the Right (X+Width) coordinate of the specified
    ///     <see cref="View"/>.
    /// </summary>
    /// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
    /// <param name="view">The <see cref="View"/>  that will be tracked.</param>
    public static Pos Right (View view) { return new PosView (view, Side.Right); }

    #endregion Static Methods - Create Pos objects

    #region Methods

    /// <summary>
    ///     Gets a position that is anchored to a certain point in the layout. This method is used
    ///     internally by the layout system to determine where a View should be positioned.
    /// </summary>
    /// <param name="width">The width of the area where the View is being positioned (Superview.ContentSize).</param>
    /// <returns>
    ///     An integer representing the calculated position. The way this position is calculated depends on the specific
    ///     subclass of Pos that is used. For example, PosAbsolute returns a fixed position, PosAnchorEnd returns a
    ///     position that is anchored to the end of the layout, and so on.
    /// </returns>
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
    /// <param name="us">The View that holds this Pos object.</param>
    /// <param name="dimension">Width or Height</param>
    /// <returns>
    ///     The calculated position of the View. The way this position is calculated depends on the specific subclass of Pos
    ///     that
    ///     is used.
    /// </returns>
    internal virtual int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension) { return Anchor (superviewDimension); }

    /// <summary>
    ///     Diagnostics API to determine if this Pos object references other views.
    /// </summary>
    /// <returns></returns>
    internal virtual bool ReferencesOtherViews () { return false; }

    #endregion Methods

    #region Operators

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

    #endregion

    #region Overrides

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="other">The object to compare with the current object. </param>
    /// <returns>
    ///     <see langword="true"/> if the specified object  is equal to the current object; otherwise,
    ///     <see langword="false"/>.
    /// </returns>
    public override bool Equals (object other) { return other is Pos abs && abs == this; }

    /// <summary>Serves as the default hash function. </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode () { return Anchor (0).GetHashCode (); }

    #endregion Overrides

    #region Pos Types
    // TODO: These should not be nested classes - extract them.
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

        internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
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

        internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
        {
            int newDimension = Math.Max (dim.Calculate (0, superviewDimension, us, dimension), 0);

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

        internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
        {
            int newDimension = dim.Calculate (0, superviewDimension, us, dimension);
            int left = _left.Calculate (superviewDimension, dim, us, dimension);
            int right = _right.Calculate (superviewDimension, dim, us, dimension);

            if (_add)
            {
                return left + right;
            }

            return left - right;
        }

        /// <summary>
        ///     Diagnostics API to determine if this Pos object references other views.
        /// </summary>
        /// <returns></returns>
        internal override bool ReferencesOtherViews ()
        {
            if (_left.ReferencesOtherViews ())
            {
                return true;
            }

            if (_right.ReferencesOtherViews ())
            {
                return true;
            }

            return false;
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

    /// <summary>
    ///     Enables alignment of a set of views.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The Group ID is used to identify a set of views that should be alignment together. When only a single
    ///         set of views is aligned, setting the Group ID is not needed because it defaults to 0.
    ///     </para>
    ///     <para>
    ///         The first view added to the Superview with a given Group ID is used to determine the alignment of the group.
    ///         The alignment is applied to all views with the same Group ID.
    ///     </para>
    /// </remarks>
    public class PosAlign : Pos
    {
        // BUGBUG: PosAlign should be internal like all other Pos classes. It is public because the PosAlign Scenario uses it. Refactor that Scenario.
        /// <summary>
        ///     The cached location. Used to store the calculated location to avoid recalculating it.
        /// </summary>
        private int? _location;

        /// <summary>
        ///     Gets the identifier of a set of views that should be aligned together. When only a single
        ///     set of views is aligned, setting the <see cref="_groupId"/> is not needed because it defaults to 0.
        /// </summary>
        private readonly int _groupId;

        /// <summary>
        ///     Gets the alignment settings.
        /// </summary>
        public Aligner Aligner { get; } = new ();

        /// <summary>
        ///     Aligns the views in <paramref name="views"/> that have the same group ID as <paramref name="groupId"/>.
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="views"></param>
        /// <param name="dimension"></param>
        /// <param name="size"></param>
        private static void AlignGroup (int groupId, IList<View> views, Dimension dimension, int size)
        {
            if (views is null)
            {
                return;
            }

            Aligner firstInGroup = null;
            List<int> dimensionsList = new ();

            List<View> viewsInGroup = views.Where (
                                                   v =>
                                                   {
                                                       if (dimension == Dimension.Width && v.X is PosAlign alignX)
                                                       {
                                                           return alignX._groupId == groupId;
                                                       }

                                                       if (dimension == Dimension.Height && v.Y is PosAlign alignY)
                                                       {
                                                           return alignY._groupId == groupId;
                                                       }

                                                       return false;
                                                   })
                                           .ToList ();

            if (viewsInGroup.Count == 0)
            {
                return;
            }

            foreach (View view in viewsInGroup)
            {
                PosAlign posAlign = dimension == Dimension.Width ? view.X as PosAlign : view.Y as PosAlign;

                if (posAlign is { })
                {
                    if (firstInGroup is null)
                    {
                        firstInGroup = posAlign.Aligner;
                    }

                    dimensionsList.Add (dimension == Dimension.Width ? view.Frame.Width : view.Frame.Height);
                }
            }

            if (firstInGroup is null)
            {
                return;
            }

            firstInGroup.ContainerSize = size;
            int [] locations = firstInGroup.Align (dimensionsList.ToArray ());

            for (var index = 0; index < viewsInGroup.Count; index++)
            {
                View view = viewsInGroup [index];
                PosAlign align = dimension == Dimension.Width ? view.X as PosAlign : view.Y as PosAlign;

                if (align is { })
                {
                    align._location = locations [index];
                }
            }
        }

        /// <summary>
        ///     Enables alignment of a set of views.
        /// </summary>
        /// <param name="alignment"></param>
        /// <param name="groupId">The unique identifier for the set of views to align according to <paramref name="alignment"/>.</param>
        public PosAlign (Alignment alignment, int groupId = 0)
        {
            Aligner.SpaceBetweenItems = true;
            Aligner.Alignment = alignment;
            _groupId = groupId;
            Aligner.PropertyChanged += Aligner_PropertyChanged;
        }

        private void Aligner_PropertyChanged (object sender, PropertyChangedEventArgs e) { _location = null; }

        /// <inheritdoc/>
        public override bool Equals (object other)
        {
            return other is PosAlign align && _groupId == align._groupId && _location == align._location && align.Aligner.Alignment == Aligner.Alignment;
        }

        /// <inheritdoc/>
        public override int GetHashCode () { return Aligner.GetHashCode () ^ _groupId.GetHashCode (); }

        /// <inheritdoc/>
        public override string ToString () { return $"Align(groupId={_groupId}, alignment={Aligner.Alignment})"; }

        internal override int Anchor (int width) { return _location ?? 0 - width; }

        internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
        {
            if (_location.HasValue && Aligner.ContainerSize == superviewDimension)
            {
                return _location.Value;
            }

            if (us?.SuperView is null)
            {
                return 0;
            }

            AlignGroup (_groupId, us.SuperView.Subviews, dimension, superviewDimension);

            if (_location.HasValue)
            {
                return _location.Value;
            }

            return 0;
        }
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

    // TODO: This should not be a nested enum. Extract it.
    /// <summary>
    ///     Describes which side of the view to use for the position.
    /// </summary>
    public enum Side
    {
        /// <summary>
        ///     The left (X) side of the view.
        /// </summary>
        Left = 0,

        /// <summary>
        ///     The top (Y) side of the view.
        /// </summary>
        Top = 1,

        /// <summary>
        ///     The right (X + Width) side of the view.
        /// </summary>
        Right = 2,

        /// <summary>
        ///     The bottom (Y + Height) side of the view.
        /// </summary>
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
                Side.Left => "left",
                Side.Top => "top",
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
                Side.Left => Target.Frame.X,
                Side.Top => Target.Frame.Y,
                Side.Right => Target.Frame.Right,
                Side.Bottom => Target.Frame.Bottom,
                _ => 0
            };
        }

        /// <summary>
        ///     Diagnostics API to determine if this Pos object references other views.
        /// </summary>
        /// <returns></returns>
        internal override bool ReferencesOtherViews () { return true; }
    }
    #endregion Pos Types
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
    /// <summary>
    ///     Specifies how <see cref="DimAuto"/> will compute the dimension.
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
        Content = 1,

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
        Text = 2
    }

    /// <summary>
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
    ///     Creates a <see cref="Dim"/> object that automatically sizes the view to fit all the view's SubViews and/or Text.
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
    ///     Specifies how <see cref="DimAuto"/> will compute the dimension. The default is <see cref="DimAutoStyle.Auto"/>.
    /// </param>
    /// <param name="min">Specifies the minimum dimension that view will be automatically sized to.</param>
    /// <param name="max">Specifies the maximum dimension that view will be automatically sized to. NOT CURRENTLY SUPPORTED.</param>
    public static Dim Auto (DimAutoStyle style = DimAutoStyle.Auto, Dim min = null, Dim max = null)
    {
        if (max != null)
        {
            throw new NotImplementedException (@"max is not implemented");
        }

        return new DimAuto (style, min, max);
    }

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
    public static Dim Height (View view) { return new DimView (view, Dimension.Height); }

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
    public static Dim Width (View view) { return new DimView (view, Dimension.Width); }

    /// <summary>
    ///     Gets a dimension that is anchored to a certain point in the layout.
    ///     This method is typically used internally by the layout system to determine the size of a View.
    /// </summary>
    /// <param name="width">The width of the area where the View is being sized (Superview.ContentSize).</param>
    /// <returns>
    ///     An integer representing the calculated dimension. The way this dimension is calculated depends on the specific
    ///     subclass of Dim that is used. For example, DimAbsolute returns a fixed dimension, DimFactor returns a
    ///     dimension that is a certain percentage of the super view's size, and so on.
    /// </returns>
    internal virtual int Anchor (int width) { return 0; }

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
        return Math.Max (Anchor (superviewContentSize - location), 0);
    }

    /// <summary>
    ///     Diagnostics API to determine if this Dim object references other views.
    /// </summary>
    /// <returns></returns>
    internal virtual bool ReferencesOtherViews () { return false; }

    internal class DimAbsolute (int n) : Dim
    {
        private readonly int _n = n;
        public override bool Equals (object other) { return other is DimAbsolute abs && abs._n == _n; }
        public override int GetHashCode () { return _n.GetHashCode (); }
        public override string ToString () { return $"Absolute({_n})"; }
        internal override int Anchor (int width) { return _n; }

        internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
        {
            // DimAbsolute.Anchor (int width) ignores width and returns n
            return Math.Max (Anchor (0), 0);
        }
    }

    /// <summary>
    ///     A <see cref="Dim"/> object that automatically sizes the view to fit all the view's SubViews and/or Text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         See <see cref="Dim.DimAutoStyle"/>.
    ///     </para>
    /// </remarks>
    /// <param name="style">
    ///     Specifies how <see cref="Dim.DimAuto"/> will compute the dimension. The default is
    ///     <see cref="Dim.DimAutoStyle.Auto"/>.
    /// </param>
    /// <param name="min">Specifies the minimum dimension that view will be automatically sized to.</param>
    /// <param name="max">Specifies the maximum dimension that view will be automatically sized to. NOT CURRENTLY SUPPORTED.</param>
    public class DimAuto (DimAutoStyle style, Dim min, Dim max) : Dim
    {
        internal readonly Dim _max = max;
        internal readonly Dim _min = min;
        internal readonly DimAutoStyle _style = style;
        internal int _size;

        /// <inheritdoc/>
        public override bool Equals (object other) { return other is DimAuto auto && auto._min == _min && auto._max == _max && auto._style == _style; }

        /// <inheritdoc/>
        public override int GetHashCode () { return HashCode.Combine (base.GetHashCode (), _min, _max, _style); }

        /// <inheritdoc/>
        public override string ToString () { return $"Auto({_style},{_min},{_max})"; }

        internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
        {
            if (us == null)
            {
                return _max?.Anchor (0) ?? 0;
            }

            var textSize = 0;
            var subviewsSize = 0;

            int autoMin = _min?.Anchor (superviewContentSize) ?? 0;

            if (superviewContentSize < autoMin)
            {
                Debug.WriteLine ($"WARNING: DimAuto specifies a min size ({autoMin}), but the SuperView's bounds are smaller ({superviewContentSize}).");

                return superviewContentSize;
            }

            if (_style.HasFlag (DimAutoStyle.Text))
            {
                textSize = int.Max (autoMin, dimension == Dimension.Width ? us.TextFormatter.Size.Width : us.TextFormatter.Size.Height);
            }

            if (_style.HasFlag (DimAutoStyle.Content))
            {
                if (us._contentSize is { })
                {
                    subviewsSize = dimension == Dimension.Width ? us.ContentSize!.Value.Width : us.ContentSize!.Value.Height;
                }
                else
                {
                    // TODO: AnchorEnd needs work
                    // TODO: If _min > 0 we can SetRelativeLayout for the subviews?
                    subviewsSize = 0;

                    if (us.Subviews.Count > 0)
                    {
                        for (var i = 0; i < us.Subviews.Count; i++)
                        {
                            View v = us.Subviews [i];
                            bool isNotPosAnchorEnd = dimension == Dimension.Width ? v.X is not Pos.PosAnchorEnd : v.Y is not Pos.PosAnchorEnd;

                            //if (!isNotPosAnchorEnd)
                            //{
                            //    v.SetRelativeLayout(dimension == Dim.Dimension.Width ? (new Size (autoMin, 0)) : new Size (0, autoMin));
                            //}

                            if (isNotPosAnchorEnd)
                            {
                                int size = dimension == Dimension.Width ? v.Frame.X + v.Frame.Width : v.Frame.Y + v.Frame.Height;

                                if (size > subviewsSize)
                                {
                                    subviewsSize = size;
                                }
                            }
                        }
                    }
                }
            }

            int max = int.Max (textSize, subviewsSize);

            Thickness thickness = us.GetAdornmentsThickness ();

            if (dimension == Dimension.Width)
            {
                max += thickness.Horizontal;
            }
            else
            {
                max += thickness.Vertical;
            }

            max = int.Max (max, autoMin);

            return int.Min (max, _max?.Anchor (superviewContentSize) ?? superviewContentSize);
        }

        /// <summary>
        ///     Diagnostics API to determine if this Dim object references other views.
        /// </summary>
        /// <returns></returns>
        internal override bool ReferencesOtherViews ()
        {
            // BUGBUG: This is not correct. _contentSize may be null.
            return _style.HasFlag (DimAutoStyle.Content);
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

        internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
        {
            int leftNewDim = _left.Calculate (location, superviewContentSize, us, dimension);
            int rightNewDim = _right.Calculate (location, superviewContentSize, us, dimension);

            int newDimension;

            if (_add)
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
            if (_left.ReferencesOtherViews ())
            {
                return true;
            }

            if (_right.ReferencesOtherViews ())
            {
                return true;
            }

            return false;
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

        internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
        {
            return _remaining ? Math.Max (Anchor (superviewContentSize - location), 0) : Anchor (superviewContentSize);
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

    internal class DimView : Dim
    {
        private readonly Dimension _side;

        internal DimView (View view, Dimension side)
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
                Dimension.Height => "Height",
                Dimension.Width => "Width",
                _ => "unknown"
            };

            return $"View({sideString},{Target})";
        }

        internal override int Anchor (int width)
        {
            return _side switch
            {
                Dimension.Height => Target.Frame.Height,
                Dimension.Width => Target.Frame.Width,
                _ => 0
            };
        }

        internal override bool ReferencesOtherViews () { return true; }
    }
}
