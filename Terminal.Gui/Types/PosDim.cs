//
// PosDim.cs: Pos and Dim objects for view dimensions.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
namespace Terminal.Gui {
	/// <summary>
	/// Describes a position which can be an absolute value, a percentage, centered, or 
	/// relative to the ending dimension.   Integer values are implicitly convertible to
	/// an absolute Pos.    These objects are created using the static methods Percent,
	/// AnchorEnd and Center.   The Pos objects can be combined with the addition and 
	/// subtraction operators.
	/// </summary>
	/// <remarks>
	///   <para>
	///     Use the Pos objects on the X or Y properties of a view to control the position.
	///   </para>
	///   <para>
	///     These can be used to set the absolute position, when merely assigning an
	///     integer value (via the implicit integer to Pos conversion), and they can be combined
	///     to produce more useful layouts, like: Pos.Center - 3, which would shift the postion
	///     of the view 3 characters to the left after centering for example.
	///   </para>
	///   <para>
	///     It is possible to reference coordinates of another view by using the methods
	///     Left(View), Right(View), Bottom(View), Top(View).   The X(View) and Y(View) are
	///     aliases to Left(View) and Top(View) respectively.
	///   </para>
	/// </remarks>
	public class Pos {
		internal virtual int Anchor (int width)
		{
			return 0;
		}

		class PosFactor : Pos {
			float factor;

			public PosFactor (float n)
			{
				this.factor = n;
			}

			internal override int Anchor (int width)
			{
				return (int)(width * factor);
			}

			public override string ToString ()
			{
				return $"Pos.Factor({factor})";
			}
		}

		/// <summary>
		/// Creates a percentage Pos object
		/// </summary>
		/// <returns>The percent Pos object.</returns>
		/// <param name="n">A value between 0 and 100 representing the percentage.</param>
		public static Pos Percent (float n)
		{
			if (n < 0 || n > 100)
				throw new ArgumentException ("Percent value must be between 0 and 100");

			return new PosFactor (n / 100);
		}

		static PosAnchorEnd endNoMargin;

		class PosAnchorEnd : Pos {
			int n;

			public PosAnchorEnd (int n)
			{
				this.n = n;
			}

			internal override int Anchor (int width)
			{
				return width - n;
			}

			public override string ToString ()
			{
				return $"Pos.AnchorEnd(margin={n})";
			}
		}

		/// <summary>
		/// Creates a Pos object that is anchored to the end of the dimension, useful to flush 
		/// the layout from the end.
		/// </summary>
		/// <returns>The Pos object anchored to the end (the bottom or the right side).</returns>
		/// <param name="margin">Optional margin to set aside.</param>
		public static Pos AnchorEnd (int margin = 0)
		{
			if (margin < 0)
				throw new ArgumentException ("Margin must be positive");

			if (margin == 0) {
				if (endNoMargin == null)
					endNoMargin = new PosAnchorEnd (0);
				return endNoMargin;
			}
			return new PosAnchorEnd (margin);
		}

		internal class PosCenter : Pos {
			internal override int Anchor (int width)
			{
				return width / 2;
			}

			public override string ToString ()
			{
				return "Pos.Center";
			}
		}

		static PosCenter pCenter;

		/// <summary>
		/// Returns a Pos object that can be used to center the views.
		/// </summary>
		/// <returns>The center Pos.</returns>
		public static Pos Center ()
		{
			if (pCenter == null)
				pCenter = new PosCenter ();
			return pCenter;
		}

		class PosAbsolute : Pos {
			int n;
			public PosAbsolute (int n) { this.n = n; }

			public override string ToString ()
			{
				return $"Pos.Absolute({n})";
			}

			internal override int Anchor (int width)
			{
				return n;
			}
		}

		/// <summary>
		/// Creates an Absolute Pos from the specified integer value.
		/// </summary>
		/// <returns>The Absolute Pos.</returns>
		/// <param name="n">The value to convert to the pos.</param>
		public static implicit operator Pos (int n)
		{
			return new PosAbsolute (n);
		}

		class PosCombine : Pos {
			Pos left, right;
			bool add;
			public PosCombine (bool add, Pos left, Pos right)
			{
				this.left = left;
				this.right = right;
				this.add = add;
			}

			internal override int Anchor (int width)
			{
				var la = left.Anchor (width);
				var ra = right.Anchor (width);
				if (add)
					return la + ra;
				else
					return la - ra;
			}
		}

		/// <summary>
		/// Adds a <see cref="Terminal.Gui.Pos"/> to a <see cref="Terminal.Gui.Pos"/>, yielding a new <see cref="T:Terminal.Gui.Pos"/>.
		/// </summary>
		/// <param name="left">The first <see cref="Terminal.Gui.Pos"/> to add.</param>
		/// <param name="right">The second <see cref="Terminal.Gui.Pos"/> to add.</param>
		/// <returns>The <see cref="T:Terminal.Gui.Pos"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
		public static Pos operator + (Pos left, Pos right)
		{
			return new PosCombine (true, left, right);
		}

		/// <summary>
		/// Subtracts a <see cref="Terminal.Gui.Pos"/> from a <see cref="Terminal.Gui.Pos"/>, yielding a new <see cref="T:Terminal.Gui.Pos"/>.
		/// </summary>
		/// <param name="left">The <see cref="Terminal.Gui.Pos"/> to subtract from (the minuend).</param>
		/// <param name="right">The <see cref="Terminal.Gui.Pos"/> to subtract (the subtrahend).</param>
		/// <returns>The <see cref="T:Terminal.Gui.Pos"/> that is the <c>left</c> minus <c>right</c>.</returns>
		public static Pos operator - (Pos left, Pos right)
		{
			return new PosCombine (false, left, right);
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
		}

		/// <summary>
		/// Returns a Pos object tracks the Left (X) position of the specified view.
		/// </summary>
		/// <returns>The Position that depends on the other view.</returns>
		/// <param name="view">The view that will be tracked.</param>
		public static Pos Left (View view) => new PosView (view, 0);

		/// <summary>
		/// Returns a Pos object tracks the Left (X) position of the specified view.
		/// </summary>
		/// <returns>The Position that depends on the other view.</returns>
		/// <param name="view">The view that will be tracked.</param>
		public static Pos X (View view) => new PosView (view, 0);

		/// <summary>
		/// Returns a Pos object tracks the Top (Y) position of the specified view.
		/// </summary>
		/// <returns>The Position that depends on the other view.</returns>
		/// <param name="view">The view that will be tracked.</param>
		public static Pos Top (View view) => new PosView (view, 1);

		/// <summary>
		/// Returns a Pos object tracks the Top (Y) position of the specified view.
		/// </summary>
		/// <returns>The Position that depends on the other view.</returns>
		/// <param name="view">The view that will be tracked.</param>
		public static Pos Y (View view) => new PosView (view, 1);

		/// <summary>
		/// Returns a Pos object tracks the Right (X+Width) coordinate of the specified view.
		/// </summary>
		/// <returns>The Position that depends on the other view.</returns>
		/// <param name="view">The view that will be tracked.</param>
		public static Pos Right (View view) => new PosView (view, 2);

		/// <summary>
		/// Returns a Pos object tracks the Bottom (Y+Height) coordinate of the specified view.
		/// </summary>
		/// <returns>The Position that depends on the other view.</returns>
		/// <param name="view">The view that will be tracked.</param>
		public static Pos Bottom (View view) => new PosView (view, 3);
	}

	/// <summary>
	/// </summary>
	/// <remarks>
	///   <para>
	///     Use the Dim objects on the Width or Height properties of a view to control the position.
	///   </para>
	///   <para>
	///     These can be used to set the absolute position, when merely assigning an
	///     integer value (via the implicit integer to Pos conversion), and they can be combined
	///     to produce more useful layouts, like: Pos.Center - 3, which would shift the postion
	///     of the view 3 characters to the left after centering for example.
	///   </para>
	/// </remarks>
	public class Dim {
		internal virtual int Anchor (int width)
		{
			return 0;
		}

		class DimFactor : Dim {
			float factor;

			public DimFactor (float n)
			{
				this.factor = n;
			}

			internal override int Anchor (int width)
			{
				return (int)(width * factor);
			}

			public override string ToString ()
			{
				return $"Dim.Factor({factor})";
			}
		}

		/// <summary>
		/// Creates a percentage Dim object
		/// </summary>
		/// <returns>The percent Dim object.</returns>
		/// <param name="n">A value between 0 and 100 representing the percentage.</param>
		public static Dim Percent (float n)
		{
			if (n < 0 || n > 100)
				throw new ArgumentException ("Percent value must be between 0 and 100");

			return new DimFactor (n / 100);
		}

		class DimAbsolute : Dim {
			int n;
			public DimAbsolute (int n) { this.n = n; }

			public override string ToString ()
			{
				return $"Dim.Absolute({n})";
			}

			internal override int Anchor (int width)
			{
				return n;
			}
		}

		class DimFill : Dim {
			int margin;
			public DimFill (int margin) { this.margin = margin; }

			public override string ToString ()
			{
				return $"Dim.Fill(margin={margin})";
			}

			internal override int Anchor (int width)
			{
				return width-margin;
			}			
		}

		static DimFill zeroMargin;

		/// <summary>
		/// Creates a Dim object that fills the dimension, but leaves the specified number of colums for a margin.
		/// </summary>
		/// <returns>The Fill dimension.</returns>
		/// <param name="margin">Margin to use.</param>
		public static Dim Fill (int margin = 0)
		{
			if (margin == 0) {
				if (zeroMargin == null)
					zeroMargin = new DimFill (0);
				return zeroMargin;
			}
			return new DimFill (margin);
		}

		/// <summary>
		/// Creates an Absolute Pos from the specified integer value.
		/// </summary>
		/// <returns>The Absolute Pos.</returns>
		/// <param name="n">The value to convert to the pos.</param>
		public static implicit operator Dim (int n)
		{
			return new DimAbsolute (n);
		}

		class DimCombine : Dim {
			Dim left, right;
			bool add;
			public DimCombine (bool add, Dim left, Dim right)
			{
				this.left = left;
				this.right = right;
				this.add = add;
			}

			internal override int Anchor (int width)
			{
				var la = left.Anchor (width);
				var ra = right.Anchor (width);
				if (add)
					return la + ra;
				else
					return la - ra;
			}
		}

		/// <summary>
		/// Adds a <see cref="Terminal.Gui.Pos"/> to a <see cref="Terminal.Gui.Pos"/>, yielding a new <see cref="T:Terminal.Gui.Pos"/>.
		/// </summary>
		/// <param name="left">The first <see cref="Terminal.Gui.Pos"/> to add.</param>
		/// <param name="right">The second <see cref="Terminal.Gui.Pos"/> to add.</param>
		/// <returns>The <see cref="T:Terminal.Gui.Pos"/> that is the sum of the values of <c>left</c> and <c>right</c>.</returns>
		public static Dim operator + (Dim left, Dim right)
		{
			return new DimCombine (true, left, right);
		}

		/// <summary>
		/// Subtracts a <see cref="Terminal.Gui.Pos"/> from a <see cref="Terminal.Gui.Pos"/>, yielding a new <see cref="T:Terminal.Gui.Pos"/>.
		/// </summary>
		/// <param name="left">The <see cref="Terminal.Gui.Pos"/> to subtract from (the minuend).</param>
		/// <param name="right">The <see cref="Terminal.Gui.Pos"/> to subtract (the subtrahend).</param>
		/// <returns>The <see cref="T:Terminal.Gui.Pos"/> that is the <c>left</c> minus <c>right</c>.</returns>
		public static Dim operator - (Dim left, Dim right)
		{
			return new DimCombine (false, left, right);
		}

		internal class DimView : Dim {
			public View Target;
			int side;
			public DimView (View view, int side)
			{
				Target = view;
				this.side = side;
			}

			internal override int Anchor (int width)
			{
				switch (side) {
				case 0: return Target.Frame.Height;
				case 1: return Target.Frame.Width;
				default:
					return 0;
				}
			}
		}
		/// <summary>
		/// Returns a Dim object tracks the Width of the specified view.
		/// </summary>
		/// <returns>The dimension of the other view.</returns>
		/// <param name="view">The view that will be tracked.</param>
		public static Dim Width (View view) => new DimView (view, 1);

		/// <summary>
		/// Returns a Dim object tracks the Height of the specified view.
		/// </summary>
		/// <returns>The dimension of the other view.</returns>
		/// <param name="view">The view that will be tracked.</param>
		public static Dim Height (View view) => new DimView (view, 0);
	}
}
