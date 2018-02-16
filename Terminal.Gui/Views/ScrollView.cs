//
// ScrollView.cs: ScrollView and ScrollBarView views.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
//
// TODO:
// - Mouse handling in scrollbarview
// - focus in scrollview
// - keyboard handling in scrollview to scroll
// - focus handling in scrollview to auto scroll to focused view
// - Raise events
// - Perhaps allow an option to not display the scrollbar arrow indicators?

using System;
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
		bool vertical;
		int size, position;

		/// <summary>
		/// The size that this scrollbar represents
		/// </summary>
		/// <value>The size.</value>
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
		public event Action ChangedPosition;

		/// <summary>
		/// The position to show the scrollbar at.
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
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Gui.ScrollBarView"/> class.
		/// </summary>
		/// <param name="rect">Frame for the scrollbar.</param>
		/// <param name="size">The size that this scrollbar represents.</param>
		/// <param name="position">The position within this scrollbar.</param>
		/// <param name="isVertical">If set to <c>true</c> this is a vertical scrollbar, otherwize, the scrollbar is horizontal.</param>
		public ScrollBarView (Rect rect, int size, int position, bool isVertical) : base (rect)
		{
			vertical = isVertical;
			this.position = position;
			this.size = size;
		}

		/// <summary>
		/// Redraw the scrollbar
		/// </summary>
		/// <param name="region">Region to be redrawn.</param>
		public override void Redraw(Rect region)
		{
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

					for (int y = 0; y < bh; y++) {
						Move (col, y);
						if (y < by1 || y > by2)
							special = Driver.Stipple;
						else
							special = Driver.Diamond;
						Driver.AddRune(special);
					}
				} else {
					bh -= 2;
					var by1 = position * bh / Size;
					var by2 = (position + bh) * bh / Size;

					
					Move (col, 0);
					Driver.AddRune ('^');
					Move (col, Bounds.Height - 1);
					Driver.AddRune ('v');
					for (int y = 0; y < bh; y++) {
						Move (col, y+1);

						if (y < by1 || y > by2)
							special = Driver.Stipple;
						else {
							if (by2 - by1 == 0)
								special = Driver.Diamond;
							else {
								if (y == by1)
									special = Driver.TopTee;
								else if (y == by2)
									special = Driver.BottomTee;
								else
									special = Driver.VLine;
							}
						}
						Driver.AddRune (special);
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

					for (int x = 0; x < bw; x++) {
						Move (0, x);
						if (x < bx1 || x > bx2)
							special = Driver.Stipple;
						else
							special = Driver.Diamond;
						Driver.AddRune (special);
					}
				} else {
					bw -= 2;
					var bx1 = position * bw / Size;
					var bx2 = (position + bw) * bw / Size;

					Move (0, row);
					Driver.AddRune ('<');

					for (int x = 0; x < bw; x++) {

						if (x < bx1 || x > bx2) {
							special = Driver.Stipple;
						} else {
							if (bx2 - bx1 == 0)
								special = Driver.Diamond;
							else {
								if (x == bx1)
									special = Driver.LeftTee;
								else if (x == bx2)
									special = Driver.RightTee;
								else
									special = Driver.HLine;
							}
						}
						Driver.AddRune (special);
					}
					Driver.AddRune ('>');
				}
			}
		}

		public override bool MouseEvent(MouseEvent me)
		{
			if (me.Flags != MouseFlags.Button1Clicked)
				return false;

			int location = vertical ? me.Y : me.X;
			int barsize = vertical ? Bounds.Height : Bounds.Width;

			if (barsize < 4) {
				// Handle scrollbars with no buttons
				Console.WriteLine ("TODO at ScrollBarView2");
			} else {
				barsize -= 2;
				// Handle scrollbars with arrow buttons
				var pos = Position;
				if (location == 0) {
					if (pos > 0)
						SetPosition (pos - 1);
				} else if (location == Bounds.Width - 1){
					if (pos + 1 + barsize < Size)
						SetPosition (pos + 1);
				} else {
					Console.WriteLine ("TODO at ScrollBarView");
				}
			}

			return true;
		}
	}

	/// <summary>
	/// Scrollviews are views that present a window into a virtual space where children views are added.  Similar to the iOS UIScrollView.
	/// </summary>
	/// <remarks>
	/// <para>
	///   The subviews that are added to this scrollview are offset by the
	///   ContentOffset property.   The view itself is a window into the 
	///   space represented by the ContentSize.
	/// </para>
	/// <para>
	///   
	/// </para>
	/// </remarks>
	public class ScrollView : View {
		View contentView;
		ScrollBarView vertical, horizontal;

		public ScrollView (Rect frame) : base (frame)
		{
			contentView = new View (frame);
			vertical = new ScrollBarView (new Rect (frame.Width - 1, 0, 1, frame.Height), frame.Height, 0, isVertical: true);
			vertical.ChangedPosition += delegate {
				ContentOffset = new Point (ContentOffset.X, vertical.Position);
			};
			horizontal = new ScrollBarView (new Rect (0, frame.Height-1, frame.Width-1, 1), frame.Width-1, 0, isVertical: false);
			horizontal.ChangedPosition += delegate {
				ContentOffset = new Point (horizontal.Position, ContentOffset.Y);
			};
			base.Add (contentView);
			CanFocus = true;
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
				contentOffset = new Point (-Math.Abs (value.X), -Math.Abs(value.Y));
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
			contentView.Add (view);
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
				SetNeedsDisplay ();
				if (value)
					base.Add (horizontal);
				else
					Remove (horizontal);
			}
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
				SetNeedsDisplay ();
				if (value)
					base.Add (vertical);
				else
					Remove (vertical);
			}
		}

		/// <summary>
		/// This event is raised when the contents have scrolled
		/// </summary>
		public event Action<ScrollView> Scrolled;

		public override void Redraw(Rect region)
		{
			var oldClip = ClipToBounds ();
			Driver.SetAttribute (ColorScheme.Normal);
			Clear ();
			base.Redraw(region);
			Driver.Clip = oldClip;
			Driver.SetAttribute (ColorScheme.Normal);
		}

		public override void PositionCursor()
		{
			if (Subviews.Count == 0)
				Driver.Move (0, 0);
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

		public override bool ProcessKey(KeyEvent kb)
		{
			if (base.ProcessKey (kb))
				return true;

			switch (kb.Key) {
			case Key.CursorUp:
				return ScrollUp (1);
			case (Key) 'v' | Key.AltMask:
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

			}
			return false;
		}
	}
}
