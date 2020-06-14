//
// ScrollView.cs: ScrollView and ScrollBarView views.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
//
// TODO:
// - focus in scrollview
// - focus handling in scrollview to auto scroll to focused view
// - Raise events
// - Perhaps allow an option to not display the scrollbar arrow indicators?

using System;
using System.Reflection;

namespace Terminal.Gui {
	/// <summary>
	/// ScrollBarViews are views that display a 1-character scrollbar, either horizontal or vertical
	/// </summary>
	/// <remarks>
	/// <para>
	///   The scrollbar is drawn to be a representation of the Size, assuming that the 
	///   scroll position is set at Position.
	/// </para>
	/// <para>
	///   If the region to display the scrollbar is larger than three characters, 
	///   arrow indicators are drawn.
	/// </para>
	/// </remarks>
	public class ScrollBarView : View {
		bool vertical = false;
		int size = 0, position = 0;

		/// <summary>
		/// If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal.
		/// </summary>
		public bool IsVertical {
			get => vertical;
			set {
				vertical = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// The size of content the scrollbar represents. 
		/// </summary>
		/// <value>The size.</value>
		/// <remarks>The <see cref="Size"/> is typically the size of the virtual content. E.g. when a Scrollbar is
		/// part of a <see cref="ScrollView"/> the Size is set to the appropriate dimension of <see cref="ScrollView.ContentSize"/>.</remarks>
		public int Size {
			get => size;
			set {
				size = value;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// This event is raised when the position on the scrollbar has changed.
		/// </summary>
		public Action ChangedPosition;

		/// <summary>
		/// The position, relative to <see cref="Size"/>, to set the scrollbar at.
		/// </summary>
		/// <value>The position.</value>
		public int Position {
			get => position;
			set {
				position = value;
				SetNeedsDisplay ();
			}
		}

		void SetPosition (int newPos)
		{
			Position = newPos;
			ChangedPosition?.Invoke ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <param name="rect">Frame for the scrollbar.</param>
		public ScrollBarView (Rect rect) : this (rect, 0, 0, false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <param name="rect">Frame for the scrollbar.</param>
		/// <param name="size">The size that this scrollbar represents. Sets the <see cref="Size"/> property.</param>
		/// <param name="position">The position within this scrollbar. Sets the <see cref="Position"/> property.</param>
		/// <param name="isVertical">If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal. Sets the <see cref="IsVertical"/> property.</param>
		public ScrollBarView (Rect rect, int size, int position, bool isVertical) : base (rect)
		{
			Init (size, position, isVertical);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public ScrollBarView () : this (0, 0, false) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="size">The size that this scrollbar represents.</param>
		/// <param name="position">The position within this scrollbar.</param>
		/// <param name="isVertical">If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal.</param>
		public ScrollBarView (int size, int position, bool isVertical) : base ()
		{
			Init (size, position, isVertical);
		}

		void Init (int size, int position, bool isVertical)
		{
			vertical = isVertical;
			this.position = position;
			this.size = size;
			WantContinuousButtonPressed = true;
		}

		int posTopTee;
		int posLeftTee;
		int posBottomTee;
		int posRightTee;

		///<inheritdoc/>
		public override void Redraw (Rect region)
		{
			if (ColorScheme == null || Size == 0)
				return;

			Driver.SetAttribute (ColorScheme.Normal);

			if (vertical) {
				if (region.Right < Bounds.Width - 1)
					return;

				var col = Bounds.Width - 1;
				var bh = Bounds.Height;
				Rune special;

				if (bh < 4) {
					var by1 = position * bh / Size;
					var by2 = (position + bh) * bh / Size;

					Move (col, 0);
					Driver.AddRune (Driver.UpArrow);
					Move (col, Bounds.Height - 1);
					Driver.AddRune (Driver.DownArrow);
				} else {
					bh -= 2;
					var by1 = position * bh / Size;
					var by2 = (position + bh) * bh / Size;

					Move (col, 0);
					Driver.AddRune (Driver.UpArrow);
					Move (col, Bounds.Height - 1);
					Driver.AddRune (Driver.DownArrow);

					bool hasTopTee = false;
					bool hasDiamond = false;
					bool hasBottomTee = false;
					for (int y = 0; y < bh; y++) {
						Move (col, y + 1);
						if ((y < by1 || y > by2) && ((position > 0 && !hasTopTee) || (hasTopTee && hasBottomTee))) {
							special = Driver.Stipple;
						} else {
							if (y != by2 && y > 1 && by2 - by1 == 0 && by1 < bh - 1 && hasTopTee && !hasDiamond) {
								hasDiamond = true;
								special = Driver.Diamond;
							} else {
								if (y == by1 && !hasTopTee) {
									hasTopTee = true;
									posTopTee = y;
									special = Driver.TopTee;
								} else if ((y >= by2 || by2 == 0) && !hasBottomTee) {
									hasBottomTee = true;
									posBottomTee = y;
									special = Driver.BottomTee;
								} else {
									special = Driver.VLine;
								}
							}
						}
						Driver.AddRune (special);
					}
					if (!hasTopTee) {
						Move (col, Bounds.Height - 2);
						Driver.AddRune (Driver.TopTee);
					}
				}
			} else {
				if (region.Bottom < Bounds.Height - 1)
					return;

				var row = Bounds.Height - 1;
				var bw = Bounds.Width;
				Rune special;

				if (bw < 4) {
					var bx1 = position * bw / Size;
					var bx2 = (position + bw) * bw / Size;

					Move (0, row);
					Driver.AddRune (Driver.LeftArrow);
					Driver.AddRune (Driver.RightArrow);
				} else {
					bw -= 2;
					var bx1 = position * bw / Size;
					var bx2 = (position + bw) * bw / Size;

					Move (0, row);
					Driver.AddRune (Driver.LeftArrow);

					bool hasLeftTee = false;
					bool hasDiamond = false;
					bool hasRightTee = false;
					for (int x = 0; x < bw; x++) {
						if ((x < bx1 || x >= bx2 + 1) && ((position > 0 && !hasLeftTee) || (hasLeftTee && hasRightTee))) {
							special = Driver.Stipple;
						} else {
							if (x != bx2 && x > 1 && bx2 - bx1 == 0 && bx1 < bw - 1 && hasLeftTee && !hasDiamond) {
								hasDiamond = true;
								special = Driver.Diamond;
							} else {
								if (x == bx1 && !hasLeftTee) {
									hasLeftTee = true;
									posLeftTee = x;
									special = Driver.LeftTee;
								} else if ((x >= bx2 || bx2 == 0) && !hasRightTee) {
									hasRightTee = true;
									posRightTee = x;
									special = Driver.RightTee;
								} else {
									special = Driver.HLine;
								}
							}
						}
						Driver.AddRune (special);
					}
					if (!hasLeftTee) {
						Move (Bounds.Width -2, row);
						Driver.AddRune (Driver.LeftTee);
					}

					Driver.AddRune (Driver.RightArrow);
				}
			}
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags != MouseFlags.Button1Pressed && me.Flags != MouseFlags.Button1Clicked &&
				!me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
				return false;

			int location = vertical ? me.Y : me.X;
			int barsize = vertical ? Bounds.Height : Bounds.Width;
			int posTopLeftTee = vertical ? posTopTee : posLeftTee;
			int posBottomRightTee = vertical ? posBottomTee : posRightTee;

			barsize -= 2;
			var pos = Position;
			if (location == 0) {
				if (pos > 0)
					SetPosition (pos - 1);
			} else if (location == barsize + 1) {
				if (pos + 1 < Size)
					SetPosition (pos + 1);
			} else {
				var b1 = pos * barsize / Size;
				var b2 = (pos + barsize) * barsize / Size;

				if (b1 == 0 && location == 1 && pos == 0 || (location >= posTopLeftTee + 1 && location <= posBottomRightTee + 1 && (pos != 0 || pos != Size - 1) && location != 1 && location != barsize) ||
					(b2 == barsize + (b2 - b1 - 1) && location == barsize && pos == Size - 1)) {
					return true;
				} else if (location <= barsize) {
					if (location > 1 && location > posTopLeftTee && location > posBottomRightTee)
						SetPosition (Math.Min (pos + (Size / location), Size - 1));
					else if (location <= b2 && pos > 0 || pos > 0)
						SetPosition (Math.Max (pos - (Size / barsize), 0));
				}
			}

			return true;
		}
	}

	/// <summary>
	/// Scrollviews are views that present a window into a virtual space where subviews are added.  Similar to the iOS UIScrollView.
	/// </summary>
	/// <remarks>
	/// <para>
	///   The subviews that are added to this <see cref="Gui.ScrollView"/> are offset by the
	///   <see cref="ContentOffset"/> property.  The view itself is a window into the 
	///   space represented by the <see cref="ContentSize"/>.
	/// </para>
	/// <para>
	///   Use the 
	/// </para>
	/// </remarks>
	public class ScrollView : View {
		View contentView = null;
		ScrollBarView vertical, horizontal;

		/// <summary>
		///  Initializes a new instance of the <see cref="Gui.ScrollView"/> class using <see cref="LayoutStyle.Absolute"/> positioning.
		/// </summary>
		/// <param name="frame"></param>
		public ScrollView (Rect frame) : base (frame)
		{
			Init (frame);
		}


		/// <summary>
		///  Initializes a new instance of the <see cref="Gui.ScrollView"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public ScrollView () : base ()
		{
			Init (new Rect (0, 0, 0, 0));
		}

		void Init (Rect frame)
		{
			contentView = new View (frame);
			vertical = new ScrollBarView (1, 0, isVertical: true) {
				X = Pos.AnchorEnd (1),
				Y = 0,
				Width = 1,
				Height = Dim.Fill (showHorizontalScrollIndicator ? 1 : 0)
			};
			vertical.ChangedPosition += delegate {
				ContentOffset = new Point (ContentOffset.X, vertical.Position);
			};
			horizontal = new ScrollBarView (1, 0, isVertical: false) {
				X = 0,
				Y = Pos.AnchorEnd (1),
				Width = Dim.Fill (showVerticalScrollIndicator ? 1 : 0),
				Height = 1
			};
			horizontal.ChangedPosition += delegate {
				ContentOffset = new Point (horizontal.Position, ContentOffset.Y);
			};
			base.Add (contentView);
			CanFocus = true;

			MouseEnter += View_MouseEnter;
			MouseLeave += View_MouseLeave;
		}

		Size contentSize;
		Point contentOffset;
		bool showHorizontalScrollIndicator;
		bool showVerticalScrollIndicator;

		/// <summary>
		/// Represents the contents of the data shown inside the scrolview
		/// </summary>
		/// <value>The size of the content.</value>
		public Size ContentSize {
			get {
				return contentSize;
			}
			set {
				contentSize = value;
				contentView.Frame = new Rect (contentOffset, value);
				vertical.Size = contentSize.Height;
				horizontal.Size = contentSize.Width;
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Represents the top left corner coordinate that is displayed by the scrollview
		/// </summary>
		/// <value>The content offset.</value>
		public Point ContentOffset {
			get {
				return contentOffset;
			}
			set {
				contentOffset = new Point (-Math.Abs (value.X), -Math.Abs (value.Y));
				contentView.Frame = new Rect (contentOffset, contentSize);
				vertical.Position = Math.Max (0, -contentOffset.Y);
				horizontal.Position = Math.Max (0, -contentOffset.X);
				SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Adds the view to the scrollview.
		/// </summary>
		/// <param name="view">The view to add to the scrollview.</param>
		public override void Add (View view)
		{
			if (!IsOverridden (view)) {
				view.MouseEnter += View_MouseEnter;
				view.MouseLeave += View_MouseLeave;
			}
			contentView.Add (view);
			SetNeedsLayout ();
		}

		void View_MouseLeave (MouseEventArgs e)
		{
			Application.UngrabMouse ();
		}

		void View_MouseEnter (MouseEventArgs e)
		{
			Application.GrabMouse (this);
		}

		bool IsOverridden (View view)
		{
			Type t = view.GetType ();
			MethodInfo m = t.GetMethod ("MouseEvent");

			return m.DeclaringType == t && m.GetBaseDefinition ().DeclaringType == typeof (Responder);
		}

		/// <summary>
		/// Gets or sets the visibility for the horizontal scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show vertical scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowHorizontalScrollIndicator {
			get => showHorizontalScrollIndicator;
			set {
				if (value == showHorizontalScrollIndicator)
					return;

				showHorizontalScrollIndicator = value;
				SetNeedsLayout ();
				if (value) {
					base.Add (horizontal);
					horizontal.MouseEnter += View_MouseEnter;
					horizontal.MouseLeave += View_MouseLeave;
				} else {
					Remove (horizontal);
					horizontal.MouseEnter -= View_MouseEnter;
					horizontal.MouseLeave -= View_MouseLeave;
				}
				vertical.Height = Dim.Fill (showHorizontalScrollIndicator ? 1 : 0);
			}
		}

		/// <summary>
		///   Removes all widgets from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		/// <summary>
		/// /// Gets or sets the visibility for the vertical scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show vertical scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowVerticalScrollIndicator {
			get => showVerticalScrollIndicator;
			set {
				if (value == showVerticalScrollIndicator)
					return;

				showVerticalScrollIndicator = value;
				SetNeedsLayout ();
				if (value) {
					base.Add (vertical);
					vertical.MouseEnter += View_MouseEnter;
					vertical.MouseLeave += View_MouseLeave;
				} else {
					Remove (vertical);
					vertical.MouseEnter -= View_MouseEnter;
					vertical.MouseLeave -= View_MouseLeave;
				}
				horizontal.Width = Dim.Fill (showVerticalScrollIndicator ? 1 : 0);
			}
		}

		/// <inheritdoc/>
		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (ColorScheme.Normal);
			SetViewsNeedsDisplay ();
			Clear ();

			var savedClip = ClipToBounds ();
			OnDrawContent (new Rect (ContentOffset,
				new Size (Bounds.Width - (ShowVerticalScrollIndicator ? 1 : 0),
					Bounds.Height - (ShowHorizontalScrollIndicator ? 1 : 0))));
			contentView.Redraw (contentView.Frame);
			Driver.Clip = savedClip;

			if (ShowVerticalScrollIndicator) {
				vertical.Redraw (vertical.Bounds);
			}

			if (ShowHorizontalScrollIndicator) {
				horizontal.Redraw (horizontal.Bounds);
			}

			// Fill in the bottom left corner
			if (ShowVerticalScrollIndicator && ShowHorizontalScrollIndicator) {
				AddRune (Bounds.Width - 1, Bounds.Height - 1, ' ');
			}
			Driver.SetAttribute (ColorScheme.Normal);
		}

		void SetViewsNeedsDisplay ()
		{
			foreach (View view in contentView) {
				view.SetNeedsDisplay ();
			}
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			if (InternalSubviews.Count == 0)
				Move (0, 0);
			else
				base.PositionCursor ();
		}

		/// <summary>
		/// Scrolls the view up.
		/// </summary>
		/// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
		/// <param name="lines">Number of lines to scroll.</param>
		public bool ScrollUp (int lines)
		{
			if (contentOffset.Y < 0) {
				ContentOffset = new Point (contentOffset.X, Math.Min (contentOffset.Y + lines, 0));
				return true;
			}
			return false;
		}

		/// <summary>
		/// Scrolls the view to the left
		/// </summary>
		/// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
		/// <param name="cols">Number of columns to scroll by.</param>
		public bool ScrollLeft (int cols)
		{
			if (contentOffset.X < 0) {
				ContentOffset = new Point (Math.Min (contentOffset.X + cols, 0), contentOffset.Y);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Scrolls the view down.
		/// </summary>
		/// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
		/// <param name="lines">Number of lines to scroll.</param>
		public bool ScrollDown (int lines)
		{
			var ny = Math.Max (-contentSize.Height, contentOffset.Y - lines);
			if (ny == contentOffset.Y)
				return false;
			ContentOffset = new Point (contentOffset.X, ny);
			return true;
		}

		/// <summary>
		/// Scrolls the view to the right.
		/// </summary>
		/// <returns><c>true</c>, if right was scrolled, <c>false</c> otherwise.</returns>
		/// <param name="cols">Number of columns to scroll by.</param>
		public bool ScrollRight (int cols)
		{
			var nx = Math.Max (-contentSize.Width, contentOffset.X - cols);
			if (nx == contentOffset.X)
				return false;

			ContentOffset = new Point (nx, contentOffset.Y);
			return true;
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (base.ProcessKey (kb))
				return true;

			switch (kb.Key) {
			case Key.CursorUp:
				return ScrollUp (1);
			case (Key)'v' | Key.AltMask:
			case Key.PageUp:
				return ScrollUp (Bounds.Height);

			case Key.ControlV:
			case Key.PageDown:
				return ScrollDown (Bounds.Height);

			case Key.CursorDown:
				return ScrollDown (1);

			case Key.CursorLeft:
				return ScrollLeft (1);

			case Key.CursorRight:
				return ScrollRight (1);

			case Key.Home:
				return ScrollUp (contentSize.Height);

			case Key.End:
				return ScrollDown (contentSize.Height);

			}
			return false;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags != MouseFlags.WheeledDown && me.Flags != MouseFlags.WheeledUp &&
				me.Flags != MouseFlags.Button1Pressed && me.Flags != MouseFlags.Button1Clicked &&
				!me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
				return false;

			if (me.Flags == MouseFlags.WheeledDown)
				ScrollDown (1);
			else if (me.Flags == MouseFlags.WheeledUp)
				ScrollUp (1);
			else if (me.X == vertical.Frame.X)
				vertical.MouseEvent (me);
			else if (me.Y == horizontal.Frame.Y)
				horizontal.MouseEvent (me);
			else if (IsOverridden (me.View)) {
				Application.UngrabMouse ();
				return false;
			}
			return true;
		}
	}
}
