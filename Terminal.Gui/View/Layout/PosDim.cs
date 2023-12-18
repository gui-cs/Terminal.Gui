//
// PosDim.cs: Pos and Dim objects for view dimensions.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;

namespace Terminal.Gui; 

/// <summary>
/// Describes the position of a <see cref="View"/> which can be an absolute value, a percentage, centered, or 
/// relative to the ending dimension. Integer values are implicitly convertible to
/// an absolute <see cref="Pos"/>. These objects are created using the static methods Percent,
/// AnchorEnd, and Center. The <see cref="Pos"/> objects can be combined with the addition and 
/// subtraction operators.
/// </summary>
/// <remarks>
///   <para>
///     Use the <see cref="Pos"/> objects on the X or Y properties of a view to control the position.
///   </para>
///   <para>
///     These can be used to set the absolute position, when merely assigning an
///     integer value (via the implicit integer to <see cref="Pos"/> conversion), and they can be combined
///     to produce more useful layouts, like: Pos.Center - 3, which would shift the position
///     of the <see cref="View"/> 3 characters to the left after centering for example.
///   </para>
///   <para>
///     It is possible to reference coordinates of another view by using the methods
///     Left(View), Right(View), Bottom(View), Top(View). The X(View) and Y(View) are
///     aliases to Left(View) and Top(View) respectively.
///   </para>
/// </remarks>
public class Pos {
	internal virtual int Anchor (int width) => 0;

	// Helper class to provide dynamic value by the execution of a function that returns an integer.
	internal class PosFunc : Pos {
		Func<int> function;

		public PosFunc (Func<int> n) => function = n;

		internal override int Anchor (int width) => function ();

		public override string ToString () => $"PosFunc({function ()})";

		public override int GetHashCode () => function.GetHashCode ();

		public override bool Equals (object other) => other is PosFunc f && f.function () == function ();
	}

	/// <summary>
	/// Creates a "PosFunc" from the specified function.
	/// </summary>
	/// <param name="function">The function to be executed.</param>
	/// <returns>The <see cref="Pos"/> returned from the function.</returns>
	public static Pos Function (Func<int> function) => new PosFunc (function);

	internal class PosFactor : Pos {
		float factor;

		public PosFactor (float n) => factor = n;

		internal override int Anchor (int width) => (int)(width * factor);

		public override string ToString () => $"Factor({factor})";

		public override int GetHashCode () => factor.GetHashCode ();

		public override bool Equals (object other) => other is PosFactor f && f.factor == factor;
	}

	/// <summary>
	/// Creates a percentage <see cref="Pos"/> object
	/// </summary>
	/// <returns>The percent <see cref="Pos"/> object.</returns>
	/// <param name="n">A value between 0 and 100 representing the percentage.</param>
	/// <example>
	/// This creates a <see cref="TextField"/>that is centered horizontally, is 50% of the way down, 
	/// is 30% the height, and is 80% the width of the <see cref="View"/> it added to.
	/// <code>
	/// var textView = new TextView () {
	///	X = Pos.Center (),
	///	Y = Pos.Percent (50),
	///	Width = Dim.Percent (80),
	/// 	Height = Dim.Percent (30),
	/// };
	/// </code>
	/// </example>
	public static Pos Percent (float n)
	{
		if (n < 0 || n > 100) {
			throw new ArgumentException ("Percent value must be between 0 and 100");
		}

		return new PosFactor (n / 100);
	}

	internal class PosAnchorEnd : Pos {
		int n;

		public PosAnchorEnd (int n) => this.n = n;

		internal override int Anchor (int width) => width - n;

		public override string ToString () => $"AnchorEnd({n})";

		public override int GetHashCode () => n.GetHashCode ();

		public override bool Equals (object other) => other is PosAnchorEnd anchorEnd && anchorEnd.n == n;
	}

	/// <summary>
	/// Creates a <see cref="Pos"/> object that is anchored to the end (right side or bottom) of the dimension, 
	/// useful to flush the layout from the right or bottom.
	/// </summary>
	/// <returns>The <see cref="Pos"/> object anchored to the end (the bottom or the right side).</returns>
	/// <param name="margin">Optional margin to place to the right or below.</param>
	/// <example>
	/// This sample shows how align a <see cref="Button"/> to the bottom-right of a <see cref="View"/>.
	/// <code>
	/// // See Issue #502 
	/// anchorButton.X = Pos.AnchorEnd () - (Pos.Right (anchorButton) - Pos.Left (anchorButton));
	/// anchorButton.Y = Pos.AnchorEnd (1);
	/// </code>
	/// </example>
	public static Pos AnchorEnd (int margin = 0)
	{
		if (margin < 0) {
			throw new ArgumentException ("Margin must be positive");
		}

		return new PosAnchorEnd (margin);
	}

	internal class PosCenter : Pos {
		internal override int Anchor (int width) => width / 2;

		public override string ToString () => "Center";
	}

	/// <summary>
	/// Returns a <see cref="Pos"/> object that can be used to center the <see cref="View"/>
	/// </summary>
	/// <returns>The center Pos.</returns>
	/// <example>
	/// This creates a <see cref="TextField"/>that is centered horizontally, is 50% of the way down, 
	/// is 30% the height, and is 80% the width of the <see cref="View"/> it added to.
	/// <code>
	/// var textView = new TextView () {
	///	X = Pos.Center (),
	///	Y = Pos.Percent (50),
	///	Width = Dim.Percent (80),
	/// 	Height = Dim.Percent (30),
	/// };
	/// </code>
	/// </example>
	public static Pos Center () => new PosCenter ();

	internal class PosAbsolute : Pos {
		int n;
		public PosAbsolute (int n) => this.n = n;

		public override string ToString () => $"Absolute({n})";

		internal override int Anchor (int width) => n;

		public override int GetHashCode () => n.GetHashCode ();

		public override bool Equals (object other) => other is PosAbsolute abs && abs.n == n;
	}

	/// <summary>
	/// Creates an Absolute <see cref="Pos"/> from the specified integer value.
	/// </summary>
	/// <returns>The Absolute <see cref="Pos"/>.</returns>
	/// <param name="n">The value to convert to the <see cref="Pos"/> .</param>
	public static implicit operator Pos (int n) => new PosAbsolute (n);

	/// <summary>
	/// Creates an Absolute <see cref="Pos"/> from the specified integer value.
	/// </summary>
	/// <returns>The Absolute <see cref="Pos"/>.</returns>
	/// <param name="n">The value to convert to the <see cref="Pos"/>.</param>
	public static Pos At (int n) => new PosAbsolute (n);

	internal class PosCombine : Pos {
		internal Pos left, right;
		internal bool add;

		public PosCombine (bool add, Pos left, Pos right)
		{
			this.left = left;
			this.right = right;
			this.add = add;
		}

		internal override int Anchor (int width)
		{
			int la = left.Anchor (width);
			int ra = right.Anchor (width);
			if (add) {
				return la + ra;
			} else {
				return la - ra;
			}
		}

		public override string ToString () => $"Combine({left}{(add ? '+' : '-')}{right})";
	}

	/// <summary>
	/// Adds a <see cref="Terminal.Gui.Pos"/> to a <see cref="Terminal.Gui.Pos"/>, yielding a new <see cref="Pos"/>.
	/// </summary>
	/// <param name="left">The first <see cref="Terminal.Gui.Pos"/> to add.</param>
	/// <param name="right">The second <see cref="Terminal.Gui.Pos"/> to add.</param>
	/// <returns>The <see cref="Pos"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
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
	/// Subtracts a <see cref="Terminal.Gui.Pos"/> from a <see cref="Terminal.Gui.Pos"/>, yielding a new <see cref="Pos"/>.
	/// </summary>
	/// <param name="left">The <see cref="Terminal.Gui.Pos"/> to subtract from (the minuend).</param>
	/// <param name="right">The <see cref="Terminal.Gui.Pos"/> to subtract (the subtrahend).</param>
	/// <returns>The <see cref="Pos"/> that is the <c>left</c> minus <c>right</c>.</returns>
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

	internal class PosView : Pos {
		public View Target;
		int side;

		public PosView (View view, int side)
		{
			Target = view;
			this.side = side;
		}

		internal override int Anchor (int width)
		{
			switch (side) {
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
			switch (side) {
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
			// Note: We do not checkt `Target` for null here to intentionally throw if so
			return $"View({tside},{Target.ToString ()})";
		}

		public override int GetHashCode () => Target.GetHashCode ();

		public override bool Equals (object other) => other is PosView abs && abs.Target == Target;
	}

	/// <summary>
	/// Returns a <see cref="Pos"/> object tracks the Left (X) position of the specified <see cref="View"/>.
	/// </summary>
	/// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View"/>  that will be tracked.</param>
	public static Pos Left (View view) => new PosCombine (true, new PosView (view, 0), new PosAbsolute (0));

	/// <summary>
	/// Returns a <see cref="Pos"/> object tracks the Left (X) position of the specified <see cref="View"/>.
	/// </summary>
	/// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View"/>  that will be tracked.</param>
	public static Pos X (View view) => new PosCombine (true, new PosView (view, 0), new PosAbsolute (0));

	/// <summary>
	/// Returns a <see cref="Pos"/> object tracks the Top (Y) position of the specified <see cref="View"/>.
	/// </summary>
	/// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View"/>  that will be tracked.</param>
	public static Pos Top (View view) => new PosCombine (true, new PosView (view, 1), new PosAbsolute (0));

	/// <summary>
	/// Returns a <see cref="Pos"/> object tracks the Top (Y) position of the specified <see cref="View"/>.
	/// </summary>
	/// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View"/>  that will be tracked.</param>
	public static Pos Y (View view) => new PosCombine (true, new PosView (view, 1), new PosAbsolute (0));

	/// <summary>
	/// Returns a <see cref="Pos"/> object tracks the Right (X+Width) coordinate of the specified <see cref="View"/>.
	/// </summary>
	/// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View"/>  that will be tracked.</param>
	public static Pos Right (View view) => new PosCombine (true, new PosView (view, 2), new PosAbsolute (0));

	/// <summary>
	/// Returns a <see cref="Pos"/> object tracks the Bottom (Y+Height) coordinate of the specified <see cref="View"/> 
	/// </summary>
	/// <returns>The <see cref="Pos"/> that depends on the other view.</returns>
	/// <param name="view">The <see cref="View"/>  that will be tracked.</param>
	public static Pos Bottom (View view) => new PosCombine (true, new PosView (view, 3), new PosAbsolute (0));

	/// <summary>Serves as the default hash function. </summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode () => Anchor (0).GetHashCode ();

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="other">The object to compare with the current object. </param>
	/// <returns>
	///     <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
	public override bool Equals (object other) => other is Pos abs && abs == this;
}

/// <summary>
/// Dim properties of a <see cref="View"/> to control the dimensions.
/// </summary>
/// <remarks>
///   <para>
///     Use the Dim objects on the Width or Height properties of a <see cref="View"/> to control the dimensions.
///   </para>
///   <para>
///   </para>
/// </remarks>
public class Dim {
	internal virtual int Anchor (int width) => 0;

	// Helper class to provide dynamic value by the execution of a function that returns an integer.
	internal class DimFunc : Dim {
		Func<int> function;

		public DimFunc (Func<int> n) => function = n;

		internal override int Anchor (int width) => function ();

		public override string ToString () => $"DimFunc({function ()})";

		public override int GetHashCode () => function.GetHashCode ();

		public override bool Equals (object other) => other is DimFunc f && f.function () == function ();
	}

	/// <summary>
	/// Creates a "DimFunc" from the specified function.
	/// </summary>
	/// <param name="function">The function to be executed.</param>
	/// <returns>The <see cref="Dim"/> returned from the function.</returns>
	public static Dim Function (Func<int> function) => new DimFunc (function);

	internal class DimFactor : Dim {
		float factor;
		bool remaining;

		public DimFactor (float n, bool r = false)
		{
			factor = n;
			remaining = r;
		}

		internal override int Anchor (int width) => (int)(width * factor);

		public bool IsFromRemaining () => remaining;

		public override string ToString () => $"Factor({factor},{remaining})";

		public override int GetHashCode () => factor.GetHashCode ();

		public override bool Equals (object other) => other is DimFactor f && f.factor == factor && f.remaining == remaining;
	}

	/// <summary>
	/// Creates a percentage <see cref="Dim"/> object that is a percentage of the width or height of the SuperView.
	/// </summary>
	/// <returns>The percent <see cref="Dim"/> object.</returns>
	/// <param name="n">A value between 0 and 100 representing the percentage.</param>
	/// <param name="r">If <c>true</c> the Percent is computed based on the remaining space after the X/Y anchor positions.
	/// If <c>false</c> is computed based on the whole original space.</param>
	/// <example>
	/// This initializes a <see cref="TextField"/>that is centered horizontally, is 50% of the way down, 
	/// is 30% the height, and is 80% the width of the <see cref="View"/> it added to.
	/// <code>
	/// var textView = new TextView () {
	///	X = Pos.Center (),
	///	Y = Pos.Percent (50),
	///	Width = Dim.Percent (80),
	/// 	Height = Dim.Percent (30),
	/// };
	/// </code>
	/// </example>
	public static Dim Percent (float n, bool r = false)
	{
		if (n is < 0 or > 100) {
			throw new ArgumentException ("Percent value must be between 0 and 100");
		}

		return new DimFactor (n / 100, r);
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

	/// <summary>
	/// Initializes a new instance of the <see cref="Dim"/> class that fills the dimension, but leaves the specified number of columns for a margin.
	/// </summary>
	/// <returns>The Fill dimension.</returns>
	/// <param name="margin">Margin to use.</param>
	public static Dim Fill (int margin = 0) => new DimFill (margin);

	internal class DimAutoSize : Dim {
		readonly int _margin;
		public DimAutoSize (int margin)
		{
			_margin = margin;
		}

		public override string ToString () => $"AutoSize({_margin})";

		internal override int Anchor (int width)
		{
			return width - _margin;
		}

		public override int GetHashCode () => _margin.GetHashCode ();

		public override bool Equals (object other) => other is DimAutoSize autoSize && autoSize._margin == _margin;
	}

	/// <summary>
	/// Creates an AutoSize <see cref="Dim"/> object that is the size required to fit all of the view's SubViews.
	/// </summary>
	/// <returns>The AutoSize <see cref="Dim"/> object.</returns>
	/// <param name="margin">Margin to use.</param>
	/// <example>
	/// This initializes a <see cref="View"/> with two SubViews. The view will be automatically sized to fit the two SubViews.
	/// <code>
	/// var button = new Button () { Text = "Click Me!"; X = 1, Y = 1, Width = 10, Height = 1 };
	/// var textField = new TextField { Text = "Type here", X = 1, Y = 2, Width = 20, Height = 1 };
	/// var view = new Window () { Title = "MyWindow", X = 0, Y = 0, Width = Dim.AutoSize (), Height = Dim.AutoSize () };
	/// view.Add (button, textField);
	/// </code>
	/// </example>
	public static Dim AutoSize (int margin = 0)
	{
		return new DimAutoSize (margin);
	}

	/// <summary>
	/// Creates an Absolute <see cref="Dim"/> from the specified integer value.
	/// </summary>
	/// <returns>The Absolute <see cref="Dim"/>.</returns>
	/// <param name="n">The value to convert to the pos.</param>
	public static implicit operator Dim (int n) => new DimAbsolute (n);

	/// <summary>
	/// Creates an Absolute <see cref="Dim"/> from the specified integer value.
	/// </summary>
	/// <returns>The Absolute <see cref="Dim"/>.</returns>
	/// <param name="n">The value to convert to the <see cref="Dim"/>.</param>
	public static Dim Sized (int n) => new DimAbsolute (n);

	internal class DimCombine : Dim {
		internal Dim _left, _right;
		internal bool _add;

		public DimCombine (bool add, Dim left, Dim right)
		{
			_left = left;
			_right = right;
			_add = add;
		}

		internal override int Anchor (int width)
		{
			int la = _left.Anchor (width);
			int ra = _right.Anchor (width);
			if (_add) {
				return la + ra;
			} else {
				return la - ra;
			}
		}

		public override string ToString () => $"Combine({_left}{(_add ? '+' : '-')}{_right})";
	}

	/// <summary>
	/// Adds a <see cref="Terminal.Gui.Dim"/> to a <see cref="Terminal.Gui.Dim"/>, yielding a new <see cref="Dim"/>.
	/// </summary>
	/// <param name="left">The first <see cref="Terminal.Gui.Dim"/> to add.</param>
	/// <param name="right">The second <see cref="Terminal.Gui.Dim"/> to add.</param>
	/// <returns>The <see cref="Dim"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
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
	/// Subtracts a <see cref="Terminal.Gui.Dim"/> from a <see cref="Terminal.Gui.Dim"/>, yielding a new <see cref="Dim"/>.
	/// </summary>
	/// <param name="left">The <see cref="Terminal.Gui.Dim"/> to subtract from (the minuend).</param>
	/// <param name="right">The <see cref="Terminal.Gui.Dim"/> to subtract (the subtrahend).</param>
	/// <returns>The <see cref="Dim"/> that is the <c>left</c> minus <c>right</c>.</returns>
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

	internal class DimView : Dim {
		public View Target { get; init; }
		readonly int _side;

		public DimView (View view, int side)
		{
			Target = view;
			_side = side;
		}

		internal override int Anchor (int width) => _side switch {
			0 => Target.Frame.Height,
			1 => Target.Frame.Width,
			_ => 0
		};

		public override string ToString ()
		{
			string tside = _side switch {
				0 => "Height",
				1 => "Width",
				_ => "unknown"
			};
			return $"View({tside},{Target})";
		}

		public override int GetHashCode () => Target.GetHashCode ();

		public override bool Equals (object other) => other is DimView abs && abs.Target == Target;
	}

	/// <summary>
	/// Returns a <see cref="Dim"/> object tracks the Width of the specified <see cref="View"/>.
	/// </summary>
	/// <returns>The <see cref="Dim"/> of the other <see cref="View"/>.</returns>
	/// <param name="view">The view that will be tracked.</param>
	public static Dim Width (View view) => new DimView (view, 1);

	/// <summary>
	/// Returns a <see cref="Dim"/> object tracks the Height of the specified <see cref="View"/>.
	/// </summary>
	/// <returns>The <see cref="Dim"/> of the other <see cref="View"/>.</returns>
	/// <param name="view">The view that will be tracked.</param>
	public static Dim Height (View view) => new DimView (view, 0);

	/// <summary>Serves as the default hash function. </summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode () => Anchor (0).GetHashCode ();

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="other">The object to compare with the current object. </param>
	/// <returns>
	///     <see langword="true" /> if the specified object  is equal to the current object; otherwise, <see langword="false" />.</returns>
	public override bool Equals (object other) => other is Dim abs && abs == this;
}