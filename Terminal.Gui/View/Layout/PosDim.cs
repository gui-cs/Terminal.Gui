namespace Terminal.Gui;

/// <summary>
///         Describes the position of a <see cref="View" /> which can be an absolute value, a percentage, centered, or
///         relative to the ending dimension. Integer values are implicitly convertible to
///         an absolute <see cref="Pos" />. These objects are created using the static methods Percent,
///         AnchorEnd, and Center. The <see cref="Pos" /> objects can be combined with the addition and
///         subtraction operators.
/// </summary>
/// <remarks>
///         <para>
///                 Use the <see cref="Pos" /> objects on the X or Y properties of a view to control the position.
///         </para>
///         <para>
///                 These can be used to set the absolute position, when merely assigning an
///                 integer value (via the implicit integer to <see cref="Pos" /> conversion), and they can be combined
///                 to produce more useful layouts, like: Pos.Center - 3, which would shift the position
///                 of the <see cref="View" /> 3 characters to the left after centering for example.
///         </para>
///         <para>
///                 Reference coordinates of another view by using the methods Left(View), Right(View), Bottom(View),
///                 Top(View).
///                 The X(View) and Y(View) are
///                 aliases to Left(View) and Top(View) respectively.
///         </para>
///         <para>
///                 <list type="table">
///                         <listheader>
///                                 <term>Pos Object</term>
///                                 <description>Description</description>
///                         </listheader>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.Function(Func{int})" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that computes the position by executing the
///                                         provided
///                                         function. The function will be called every time the position is needed.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.Percent(float)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that is a percentage of the width or height
///                                         of the
///                                         SuperView.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.Anchor(int)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that is anchored to the end (right side or
///                                         bottom)
///                                         of the dimension,
///                                         useful to flush the layout from the right or bottom.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.Center" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that can be used to center the
///                                         <see cref="View" />.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.At(int)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that is an absolute position based on the
///                                         specified
///                                         integer value.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.Left" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that tracks the Left (X) position of the
///                                         specified
///                                         <see cref="View" />.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.X(View)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that tracks the Left (X) position of the
///                                         specified
///                                         <see cref="View" />.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.Top(View)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that tracks the Top (Y) position of the
///                                         specified
///                                         <see cref="View" />.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.Y(View)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that tracks the Top (Y) position of the
///                                         specified
///                                         <see cref="View" />.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.Right(View)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that tracks the Right (X+Width) coordinate
///                                         of the
///                                         specified <see cref="View" />.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Pos.Bottom(View)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Pos" /> object that tracks the Bottom (Y+Height)
///                                         coordinate of the
///                                         specified <see cref="View" />
///                                 </description>
///                         </item>
///                 </list>
///         </para>
/// </remarks>
public class Pos {
	internal virtual int Anchor (int width) => 0;

	/// <summary>
	///         Creates a <see cref="Pos" /> object that computes the position by executing the provided function. The function
	///         will be
	///         called every time the position is needed.
	/// </summary>
	/// <param name="function">The function to be executed.</param>
	/// <returns>The <see cref="Pos" /> returned from the function.</returns>
	public static Pos Function (Func<int> function) => new PosFunc (function);

	/// <summary>
	///         Creates a percentage <see cref="Pos" /> object
	/// </summary>
	/// <returns>The percent <see cref="Pos" /> object.</returns>
	/// <param name="n">A value between 0 and 100 representing the percentage.</param>
	/// <example>
	///         This creates a <see cref="TextField" />that is centered horizontally, is 50% of the way down,
	///         is 30% the height, and is 80% the width of the <see cref="View" /> it added to.
	///         <code>
	///  var textView = new TextView {
	/// 	X = Pos.Center (),
	/// 	Y = Pos.Percent (50),
	/// 	Width = Dim.Percent (80),
	///  	Height = Dim.Percent (30),
	///  };
	///  </code>
	/// </example>
	public static Pos Percent (float n)
	{
		if (n is < 0 or > 100) {
			throw new ArgumentException ("Percent value must be between 0 and 100");
		}

		return new PosFactor (n / 100);
	}

	/// <summary>
	///         Creates a <see cref="Pos" /> object that is anchored to the end (right side or bottom) of the dimension,
	///         useful to flush the layout from the right or bottom.
	/// </summary>
	/// <returns>The <see cref="Pos" /> object anchored to the end (the bottom or the right side).</returns>
	/// <param name="offset">The view will be shifted left or up by the amount specified.</param>
	/// <example>
	///         This sample shows how align a <see cref="Button" /> to the bottom-right of a <see cref="View" />.
	///         <code>
	/// // See Issue #502 
	/// anchorButton.X = Pos.AnchorEnd () - (Pos.Right (anchorButton) - Pos.Left (anchorButton));
	/// anchorButton.Y = Pos.AnchorEnd (1);
	/// </code>
	/// </example>
	public static Pos AnchorEnd (int offset = 0)
	{
		if (offset < 0) {
			throw new ArgumentException (@"Must be positive", nameof (offset));
		}

		return new PosAnchorEnd (offset);
	}

	/// <summary>
	///         Creates a <see cref="Pos" /> object that can be used to center the <see cref="View" />.
	/// </summary>
	/// <returns>The center Pos.</returns>
	/// <example>
	///         This creates a <see cref="TextField" />that is centered horizontally, is 50% of the way down,
	///         is 30% the height, and is 80% the width of the <see cref="View" /> it added to.
	///         <code>
	///  var textView = new TextView {
	/// 	X = Pos.Center (),
	/// 	Y = Pos.Percent (50),
	/// 	Width = Dim.Percent (80),
	///  	Height = Dim.Percent (30),
	///  };
	///  </code>
	/// </example>
	public static Pos Center () => new PosCenter ();

	/// <summary>
	///         Creates a <see cref="Pos" /> object that is an absolute position based on the specified integer value.
	/// </summary>
	/// <returns>The Absolute <see cref="Pos" />.</returns>
	/// <param name="n">The value to convert to the <see cref="Pos" />.</param>
	public static Pos At (int n) => new PosAbsolute (n);

	/// <summary>
	///         Creates an Absolute <see cref="Pos" /> from the specified integer value.
	/// </summary>
	/// <returns>The Absolute <see cref="Pos" />.</returns>
	/// <param name="n">The value to convert to the <see cref="Pos" /> .</param>
	public static implicit operator Pos (int n) => new PosAbsolute (n);

	/// <summary>
	///         Adds a <see cref="Terminal.Gui.Pos" /> to a <see cref="Terminal.Gui.Pos" />, yielding a new <see cref="Pos" />.
	/// </summary>
	/// <param name="left">The first <see cref="Terminal.Gui.Pos" /> to add.</param>
	/// <param name="right">The second <see cref="Terminal.Gui.Pos" /> to add.</param>
	/// <returns>The <see cref="Pos" /> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
	public static Pos operator + (Pos left, Pos right)
	{
		if (left is PosAbsolute && right is PosAbsolute) {
			return new PosAbsolute (left.Anchor (0) + right.Anchor (0));
		}

		var newPos = new PosCombine (true, left, right);
		SetPosCombine (left, newPos);
		return newPos;
	}

	/// <summary>
	///         Subtracts a <see cref="Terminal.Gui.Pos" /> from a <see cref="Terminal.Gui.Pos" />, yielding a new
	///         <see cref="Pos" />.
	/// </summary>
	/// <param name="left">The <see cref="Terminal.Gui.Pos" /> to subtract from (the minuend).</param>
	/// <param name="right">The <see cref="Terminal.Gui.Pos" /> to subtract (the subtrahend).</param>
	/// <returns>The <see cref="Pos" /> that is the <c>left</c> minus <c>right</c>.</returns>
	public static Pos operator - (Pos left, Pos right)
	{
		if (left is PosAbsolute && right is PosAbsolute) {
			return new PosAbsolute (left.Anchor (0) - right.Anchor (0));
		}

		var newPos = new PosCombine (false, left, right);
		SetPosCombine (left, newPos);
		return newPos;
	}

	static void SetPosCombine (Pos left, PosCombine newPos)
	{
		var view = left as PosView;
		if (view != null) {
			view.Target.SetNeedsLayout ();
		}
	}

	/// <summary>
	///         Creates a <see cref="Pos" /> object that tracks the Left (X) position of the specified <see cref="View" />.
	/// </summary>
	/// <returns>The <see cref="Pos" /> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View" />  that will be tracked.</param>
	public static Pos Left (View view) => new PosView (view, 0);

	/// <summary>
	///         Creates a <see cref="Pos" /> object that tracks the Left (X) position of the specified <see cref="View" />.
	/// </summary>
	/// <returns>The <see cref="Pos" /> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View" />  that will be tracked.</param>
	public static Pos X (View view) => new PosView (view, 0);

	/// <summary>
	///         Creates a <see cref="Pos" /> object that tracks the Top (Y) position of the specified <see cref="View" />.
	/// </summary>
	/// <returns>The <see cref="Pos" /> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View" />  that will be tracked.</param>
	public static Pos Top (View view) => new PosView (view, 1);

	/// <summary>
	///         Creates a <see cref="Pos" /> object that tracks the Top (Y) position of the specified <see cref="View" />.
	/// </summary>
	/// <returns>The <see cref="Pos" /> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View" />  that will be tracked.</param>
	public static Pos Y (View view) => new PosView (view, 1);

	/// <summary>
	///         Creates a <see cref="Pos" /> object that tracks the Right (X+Width) coordinate of the specified
	///         <see cref="View" />.
	/// </summary>
	/// <returns>The <see cref="Pos" /> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View" />  that will be tracked.</param>
	public static Pos Right (View view) => new PosView (view, 2);

	/// <summary>
	///         Creates a <see cref="Pos" /> object that tracks the Bottom (Y+Height) coordinate of the specified
	///         <see cref="View" />
	/// </summary>
	/// <returns>The <see cref="Pos" /> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View" />  that will be tracked.</param>
	public static Pos Bottom (View view) => new PosView (view, 3);

	/// <summary>Serves as the default hash function. </summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode () => Anchor (0).GetHashCode ();

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="other">The object to compare with the current object. </param>
	/// <returns>
	///         <see langword="true" /> if the specified object  is equal to the current object; otherwise,
	///         <see langword="false" />.
	/// </returns>
	public override bool Equals (object other) => other is Pos abs && abs == this;

	internal class PosFactor : Pos {
		readonly float _factor;

		public PosFactor (float n) => _factor = n;

		internal override int Anchor (int width) => (int)(width * _factor);

		public override string ToString () => $"Factor({_factor})";

		public override int GetHashCode () => _factor.GetHashCode ();

		public override bool Equals (object other) => other is PosFactor f && f._factor == _factor;
	}

	// Helper class to provide dynamic value by the execution of a function that returns an integer.
	internal class PosFunc : Pos {
		readonly Func<int> _function;

		public PosFunc (Func<int> n) => _function = n;

		internal override int Anchor (int width) => _function ();

		public override string ToString () => $"PosFunc({_function ()})";

		public override int GetHashCode () => _function.GetHashCode ();

		public override bool Equals (object other) => other is PosFunc f && f._function () == _function ();
	}

	internal class PosAnchorEnd : Pos {
		readonly int _offset;

		public PosAnchorEnd (int offset) => _offset = offset;

		internal override int Anchor (int width) => width - _offset;

		public override string ToString () => $"AnchorEnd({_offset})";

		public override int GetHashCode () => _offset.GetHashCode ();

		public override bool Equals (object other) =>
			other is PosAnchorEnd anchorEnd && anchorEnd._offset == _offset;
	}

	internal class PosAbsolute : Pos {
		readonly int _n;
		public PosAbsolute (int n) => _n = n;

		public override string ToString () => $"Absolute({_n})";

		internal override int Anchor (int width) => _n;

		public override int GetHashCode () => _n.GetHashCode ();

		public override bool Equals (object other) => other is PosAbsolute abs && abs._n == _n;
	}

	internal class PosCenter : Pos {
		internal override int Anchor (int width) => width / 2;

		public override string ToString () => "Center";
	}

	internal class PosCombine : Pos {
		internal bool _add;
		internal Pos _left, _right;

		public PosCombine (bool add, Pos left, Pos right)
		{
			_left = left;
			_right = right;
			_add = add;
		}

		internal override int Anchor (int width)
		{
			var la = _left.Anchor (width);
			var ra = _right.Anchor (width);
			if (_add) {
				return la + ra;
			}

			return la - ra;
		}

		public override string ToString () => $"Combine({_left}{(_add ? '+' : '-')}{_right})";
	}

	internal class PosView : Pos {
		readonly int _side;
		public readonly View Target;

		public PosView (View view, int side)
		{
			Target = view;
			_side = side;
		}

		internal override int Anchor (int width)
		{
			switch (_side) {
			case 0: return Target.Frame.X;
			case 1: return Target.Frame.Y;
			case 2: return Target.Frame.Right;
			case 3: return Target.Frame.Bottom;
			default:
				return 0;
			}
		}

		public override string ToString ()
		{
			string tside;
			switch (_side) {
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

			if (Target == null) {
				throw new NullReferenceException (nameof (Target));
			}

			return $"View(side={tside},target={Target})";
		}

		public override int GetHashCode () => Target.GetHashCode ();

		public override bool Equals (object other) => other is PosView abs && abs.Target == Target;
	}
}

/// <summary>
///         <para>
///                 A Dim object describes the dimensions of a <see cref="View" />. Dim is the type of the
///                 <see cref="View.Width" />
///                 and
///                 <see cref="View.Height" /> properties of <see cref="View" />. Dim objects enable Computed Layout (see
///                 <see cref="LayoutStyle.Computed" />)
///                 to automatically manage the dimensions of a view.
///         </para>
///         <para>
///                 Integer values are implicitly convertible to an absolute <see cref="Dim" />. These objects are created
///                 using the
///                 static methods described below.
///                 The <see cref="Dim" /> objects can be combined with the addition and subtraction operators.
///         </para>
/// </summary>
/// <remarks>
///         <para>
///                 <list type="table">
///                         <listheader>
///                                 <term>Dim Object</term>
///                                 <description>Description</description>
///                         </listheader>
///                         <item>
///                                 <term>
///                                         <see cref="Dim.Function(Func{int})" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Dim" /> object that computes the dimension by executing
///                                         the
///                                         provided function. The function will be called every time the dimension is
///                                         needed.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Dim.Percent(float, bool)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Dim" /> object that is a percentage of the width or height
///                                         of the
///                                         SuperView.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Dim.Fill(int)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Dim" /> object that fills the dimension, leaving the
///                                         specified
///                                         number of columns for a margin.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Dim.Width(View)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Dim" /> object that tracks the Width of the specified
///                                         <see cref="View" />.
///                                 </description>
///                         </item>
///                         <item>
///                                 <term>
///                                         <see cref="Dim.Height(View)" />
///                                 </term>
///                                 <description>
///                                         Creates a <see cref="Dim" /> object that tracks the Height of the specified
///                                         <see cref="View" />.
///                                 </description>
///                         </item>
///                 </list>
///         </para>
///         <para>
///         </para>
/// </remarks>
public class Dim {
	internal virtual int Anchor (int width) => 0;

	/// <summary>
	///         Creates a function <see cref="Dim" /> object that computes the dimension by executing the provided function.
	///         The function will be called every time the dimension is needed.
	/// </summary>
	/// <param name="function">The function to be executed.</param>
	/// <returns>The <see cref="Dim" /> returned from the function.</returns>
	public static Dim Function (Func<int> function) => new DimFunc (function);

	/// <summary>
	///         Creates a percentage <see cref="Dim" /> object that is a percentage of the width or height of the SuperView.
	/// </summary>
	/// <returns>The percent <see cref="Dim" /> object.</returns>
	/// <param name="n">A value between 0 and 100 representing the percentage.</param>
	/// <param name="r">
	///         If <c>true</c> the Percent is computed based on the remaining space after the X/Y anchor positions.
	///         If <c>false</c> is computed based on the whole original space.
	/// </param>
	/// <example>
	///         This initializes a <see cref="TextView" />that is centered horizontally, is 50% of the way down,
	///         is 30% the height, and is 80% the width of the <see cref="View" /> it added to.
	///         <code>
	///  var textView = new TextView {
	/// 	X = Pos.Center (),
	/// 	Y = Pos.Percent (50),
	/// 	Width = Dim.Percent (80),
	///  	Height = Dim.Percent (30),
	///  };
	///  </code>
	/// </example>
	public static Dim Percent (float n, bool r = false)
	{
		if (n is < 0 or > 100) {
			throw new ArgumentException ("Percent value must be between 0 and 100");
		}

		return new DimFactor (n / 100, r);
	}

	/// <summary>
	///         Creates a <see cref="Dim" /> object that fills the dimension, leaving the specified number of columns for a
	///         margin.
	/// </summary>
	/// <returns>The Fill dimension.</returns>
	/// <param name="margin">Margin to use.</param>
	public static Dim Fill (int margin = 0) => new DimFill (margin);

	/// <summary>
	///         Creates an Absolute <see cref="Dim" /> from the specified integer value.
	/// </summary>
	/// <returns>The Absolute <see cref="Dim" />.</returns>
	/// <param name="n">The value to convert to the pos.</param>
	public static implicit operator Dim (int n) => new DimAbsolute (n);

	/// <summary>
	///         Creates an Absolute <see cref="Dim" /> from the specified integer value.
	/// </summary>
	/// <returns>The Absolute <see cref="Dim" />.</returns>
	/// <param name="n">The value to convert to the <see cref="Dim" />.</param>
	public static Dim Sized (int n) => new DimAbsolute (n);

	/// <summary>
	///         Adds a <see cref="Terminal.Gui.Dim" /> to a <see cref="Terminal.Gui.Dim" />, yielding a new <see cref="Dim" />.
	/// </summary>
	/// <param name="left">The first <see cref="Terminal.Gui.Dim" /> to add.</param>
	/// <param name="right">The second <see cref="Terminal.Gui.Dim" /> to add.</param>
	/// <returns>The <see cref="Dim" /> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
	public static Dim operator + (Dim left, Dim right)
	{
		if (left is DimAbsolute && right is DimAbsolute) {
			return new DimAbsolute (left.Anchor (0) + right.Anchor (0));
		}

		var newDim = new DimCombine (true, left, right);
		SetDimCombine (left, newDim);
		return newDim;
	}

	/// <summary>
	///         Subtracts a <see cref="Terminal.Gui.Dim" /> from a <see cref="Terminal.Gui.Dim" />, yielding a new
	///         <see cref="Dim" />.
	/// </summary>
	/// <param name="left">The <see cref="Terminal.Gui.Dim" /> to subtract from (the minuend).</param>
	/// <param name="right">The <see cref="Terminal.Gui.Dim" /> to subtract (the subtrahend).</param>
	/// <returns>The <see cref="Dim" /> that is the <c>left</c> minus <c>right</c>.</returns>
	public static Dim operator - (Dim left, Dim right)
	{
		if (left is DimAbsolute && right is DimAbsolute) {
			return new DimAbsolute (left.Anchor (0) - right.Anchor (0));
		}

		var newDim = new DimCombine (false, left, right);
		SetDimCombine (left, newDim);
		return newDim;
	}

	// BUGBUG: newPos is never used.
	static void SetDimCombine (Dim left, DimCombine newPos) => (left as DimView)?.Target.SetNeedsLayout ();

	/// <summary>
	///         Creates a <see cref="Dim" /> object that tracks the Width of the specified <see cref="View" />.
	/// </summary>
	/// <returns>The width <see cref="Dim" /> of the other <see cref="View" />.</returns>
	/// <param name="view">The view that will be tracked.</param>
	public static Dim Width (View view) => new DimView (view, 1);

	/// <summary>
	///         Creates a <see cref="Dim" /> object that tracks the Height of the specified <see cref="View" />.
	/// </summary>
	/// <returns>The height <see cref="Dim" /> of the other <see cref="View" />.</returns>
	/// <param name="view">The view that will be tracked.</param>
	public static Dim Height (View view) => new DimView (view, 0);

	/// <summary>Serves as the default hash function. </summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode () => Anchor (0).GetHashCode ();

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="other">The object to compare with the current object. </param>
	/// <returns>
	///         <see langword="true" /> if the specified object  is equal to the current object; otherwise,
	///         <see langword="false" />.
	/// </returns>
	public override bool Equals (object other) => other is Dim abs && abs == this;

	// Helper class to provide dynamic value by the execution of a function that returns an integer.
	internal class DimFunc : Dim {
		readonly Func<int> _function;

		public DimFunc (Func<int> n) => _function = n;

		internal override int Anchor (int width) => _function ();

		public override string ToString () => $"DimFunc({_function ()})";

		public override int GetHashCode () => _function.GetHashCode ();

		public override bool Equals (object other) => other is DimFunc f && f._function () == _function ();
	}

	internal class DimFactor : Dim {
		readonly float _factor;
		readonly bool _remaining;

		public DimFactor (float n, bool r = false)
		{
			_factor = n;
			_remaining = r;
		}

		internal override int Anchor (int width) => (int)(width * _factor);

		public bool IsFromRemaining () => _remaining;

		public override string ToString () => $"Factor({_factor},{_remaining})";

		public override int GetHashCode () => _factor.GetHashCode ();

		public override bool Equals (object other) =>
			other is DimFactor f && f._factor == _factor && f._remaining == _remaining;
	}


	internal class DimAbsolute : Dim {
		readonly int _n;
		public DimAbsolute (int n) => _n = n;

		public override string ToString () => $"Absolute({_n})";

		internal override int Anchor (int width) => _n;

		public override int GetHashCode () => _n.GetHashCode ();

		public override bool Equals (object other) => other is DimAbsolute abs && abs._n == _n;
	}

	internal class DimFill : Dim {
		readonly int _margin;
		public DimFill (int margin) => _margin = margin;

		public override string ToString () => $"Fill({_margin})";

		internal override int Anchor (int width) => width - _margin;

		public override int GetHashCode () => _margin.GetHashCode ();

		public override bool Equals (object other) => other is DimFill fill && fill._margin == _margin;
	}

	internal class DimCombine : Dim {
		internal bool _add;
		internal Dim _left, _right;

		public DimCombine (bool add, Dim left, Dim right)
		{
			_left = left;
			_right = right;
			_add = add;
		}

		internal override int Anchor (int width)
		{
			var la = _left.Anchor (width);
			var ra = _right.Anchor (width);
			if (_add) {
				return la + ra;
			}

			return la - ra;
		}

		public override string ToString () => $"Combine({_left}{(_add ? '+' : '-')}{_right})";
	}

	internal class DimView : Dim {
		readonly int _side;

		public DimView (View view, int side)
		{
			Target = view;
			_side = side;
		}

		public View Target { get; init; }

		internal override int Anchor (int width) => _side switch {
			0 => Target.Frame.Height,
			1 => Target.Frame.Width,
			_ => 0
		};

		public override string ToString ()
		{
			if (Target == null) {
				throw new NullReferenceException ();
			}

			var tside = _side switch {
				0 => "Height",
				1 => "Width",
				_ => "unknown"
			};
			return $"View({tside},{Target})";
		}

		public override int GetHashCode () => Target.GetHashCode ();

		public override bool Equals (object other) => other is DimView abs && abs.Target == Target;
	}
}