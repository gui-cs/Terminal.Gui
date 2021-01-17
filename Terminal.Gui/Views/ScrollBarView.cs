//
// ScrollBarView.cs: ScrollBarView view.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

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
		bool showScrollIndicator;
		bool keepContentAlwaysInViewport = true;
		bool autoHideScrollBars = true;
		bool hosted;
		ScrollBarView otherScrollBarView;
		View contentBottomRightCorner;

		bool showBothScrollIndicator => OtherScrollBarView != null && OtherScrollBarView.showScrollIndicator && showScrollIndicator;

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

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="host">The view that will host this scrollbar.</param>
		/// <param name="isVertical">If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal.</param>
		/// <param name="showBothScrollIndicator">If set to <c>true (default)</c> will have the other scrollbar, otherwise will have only one.</param>
		public ScrollBarView (View host, bool isVertical, bool showBothScrollIndicator = true) : this (0, 0, isVertical)
		{
			if (host == null) {
				throw new ArgumentNullException ("The host parameter can't be null.");
			} else if (host.SuperView == null) {
				throw new ArgumentNullException ("The host SuperView parameter can't be null.");
			}
			hosted = true;
			ColorScheme = host.ColorScheme;
			X = isVertical ? Pos.Right (host) - 1 : Pos.Left (host);
			Y = isVertical ? Pos.Top (host) : Pos.Bottom (host) - 1;
			Host = host;
			Host.SuperView.Add (this);
			AutoHideScrollBars = true;
			if (showBothScrollIndicator) {
				OtherScrollBarView = new ScrollBarView (0, 0, !isVertical) {
					ColorScheme = host.ColorScheme,
					Host = host,
					OtherScrollBarView = this,
				};
				OtherScrollBarView.X = OtherScrollBarView.IsVertical ? Pos.Right (host) - 1 : Pos.Left (host);
				OtherScrollBarView.Y = OtherScrollBarView.IsVertical ? Pos.Top (host) : Pos.Bottom (host) - 1;
				OtherScrollBarView.Host.SuperView.Add (OtherScrollBarView);
				OtherScrollBarView.showScrollIndicator = true;
			}
			ShowScrollIndicator = true;
			contentBottomRightCorner = new View (" ");
			Host.SuperView.Add (contentBottomRightCorner);
			contentBottomRightCorner.X = Pos.Right (host) - 1;
			contentBottomRightCorner.Y = Pos.Bottom (host) - 1;
			contentBottomRightCorner.Width = 1;
			contentBottomRightCorner.Height = 1;
			contentBottomRightCorner.MouseClick += ContentBottomRightCorner_MouseClick;
		}

		private void ContentBottomRightCorner_MouseClick (MouseEventArgs me)
		{
			if (me.MouseEvent.Flags == MouseFlags.WheeledDown || me.MouseEvent.Flags == MouseFlags.WheeledUp
				|| me.MouseEvent.Flags == MouseFlags.WheeledRight || me.MouseEvent.Flags == MouseFlags.WheeledLeft) {
				me.Handled = true;
				MouseEvent (me.MouseEvent);
			} else if (me.MouseEvent.Flags == MouseFlags.Button1Clicked) {
				me.Handled = true;
				Host.SetFocus ();
			}
		}

		void Init (int size, int position, bool isVertical)
		{
			vertical = isVertical;
			this.position = position;
			this.size = size;
			WantContinuousButtonPressed = true;
		}

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
		/// part of a <see cref="View"/> the Size is set to the appropriate dimension of <see cref="Host"/>.</remarks>
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
		/// The position, relative to <see cref="Size"/>, to set the scrollbar at.
		/// </summary>
		/// <value>The position.</value>
		public int Position {
			get => position;
			set {
				if (position != value) {
					if (CanScroll (value - position, out int max, vertical) || max > 0) {
						if (max > 0 && max == value - position) {
							position = value;
						} else {
							position = Math.Max (position + max, 0);
						}
					} else if (max < 0) {
						position = Math.Max (position + max, 0);
					}
					OnChangedPosition ();
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Get or sets the view that host this <see cref="View"/>
		/// </summary>
		public View Host { get; internal set; }

		/// <summary>
		/// Represent a vertical or horizontal ScrollBarView other than this.
		/// </summary>
		public ScrollBarView OtherScrollBarView {
			get => otherScrollBarView;
			set {
				if (value != null && (value.IsVertical && vertical || !value.IsVertical && !vertical)) {
					throw new ArgumentException ($"There is already a {(vertical ? "vertical" : "horizontal")} ScrollBarView.");
				}
				otherScrollBarView = value;
			}
		}

		/// <summary>
		/// Gets or sets the visibility for the vertical or horizontal scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show vertical or horizontal scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowScrollIndicator {
			get => showScrollIndicator;
			set {
				if (value == showScrollIndicator) {
					return;
				}

				showScrollIndicator = value;
				SetNeedsLayout ();
				if (value) {
					Visible = true;
				} else {
					Visible = false;
					Position = 0;
				}
				SetWidthHeight ();
			}
		}

		void SetWidthHeight ()
		{
			if (showBothScrollIndicator) {
				Width = vertical ? 1 : Dim.Width (Host) - 1;
				Height = vertical ? Dim.Height (Host) - 1 : 1;
				otherScrollBarView.Width = otherScrollBarView.vertical ? 1 : Dim.Width (Host) - 1;
				otherScrollBarView.Height = otherScrollBarView.vertical ? Dim.Height (Host) - 1 : 1;
			} else if (showScrollIndicator) {
				Width = vertical ? 1 : Dim.Width (Host) - 0;
				Height = vertical ? Dim.Height (Host) - 0 : 1;
			} else if (otherScrollBarView != null && otherScrollBarView.showScrollIndicator) {
				otherScrollBarView.Width = otherScrollBarView.vertical ? 1 : Dim.Width (Host) - 0;
				otherScrollBarView.Height = otherScrollBarView.vertical ? Dim.Height (Host) - 0 : 1;
			}
		}

		/// <summary>
		/// Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollBarView"/>
		/// </summary>
		public bool KeepContentAlwaysInViewport {
			get { return keepContentAlwaysInViewport; }
			set {
				if (keepContentAlwaysInViewport != value) {
					keepContentAlwaysInViewport = value;
					int pos = 0;
					if (value && !vertical && position + Host.Bounds.Width > size) {
						pos = size - Host.Bounds.Width + (showBothScrollIndicator ? 1 : 0);
					}
					if (value && vertical && position + Host.Bounds.Height > size) {
						pos = size - Host.Bounds.Height + (showBothScrollIndicator ? 1 : 0);
					}
					if (pos != 0) {
						Position = pos;
					}
					if (OtherScrollBarView != null && OtherScrollBarView.keepContentAlwaysInViewport != value) {
						OtherScrollBarView.KeepContentAlwaysInViewport = value;
					}
				}
			}
		}

		/// <summary>
		/// If true the vertical/horizontal scroll bars won't be showed if it's not needed.
		/// </summary>
		public bool AutoHideScrollBars {
			get => autoHideScrollBars;
			set {
				if (autoHideScrollBars != value) {
					autoHideScrollBars = value;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Virtual method to invoke the <see cref="ChangedPosition"/> action event.
		/// </summary>
		public virtual void OnChangedPosition ()
		{
			ChangedPosition?.Invoke ();
		}

		/// <summary>
		/// Only used for a hosted view that will update and redraw the scrollbars.
		/// </summary>
		public virtual void Refresh ()
		{
			ShowHideScrollBars ();
		}

		void ShowHideScrollBars ()
		{
			if (!hosted || (hosted && !autoHideScrollBars)) {
				return;
			}

			var pending = CheckBothScrollBars (this);
			CheckBothScrollBars (otherScrollBarView, pending);

			SetWidthHeight ();
			SetRelativeLayout (Bounds);
			OtherScrollBarView.SetRelativeLayout (OtherScrollBarView.Bounds);

			if (showBothScrollIndicator) {
				contentBottomRightCorner.Visible = true;
			} else {
				contentBottomRightCorner.Visible = false;
			}
			if (showBothScrollIndicator) {
				Redraw (Bounds);
			}
			if (otherScrollBarView.showScrollIndicator) {
				otherScrollBarView.Redraw (otherScrollBarView.Bounds);
			}
		}

		bool CheckBothScrollBars (ScrollBarView scrollBarView, bool pending = false)
		{
			int barsize = scrollBarView.vertical ? scrollBarView.Bounds.Height : scrollBarView.Bounds.Width;

			if (barsize == 0 || barsize > scrollBarView.size) {
				if (scrollBarView.showScrollIndicator) {
					scrollBarView.ShowScrollIndicator = false;
				}
			} else if (barsize > 0 && barsize == scrollBarView.size && scrollBarView.OtherScrollBarView != null && pending) {
				if (scrollBarView.showScrollIndicator) {
					scrollBarView.ShowScrollIndicator = false;
				}
				if (scrollBarView.OtherScrollBarView != null && scrollBarView.showBothScrollIndicator) {
					scrollBarView.OtherScrollBarView.ShowScrollIndicator = false;
				}
			} else if (barsize > 0 && barsize == size && scrollBarView.OtherScrollBarView != null && !pending) {
				pending = true;
			} else {
				if (scrollBarView.OtherScrollBarView != null && pending) {
					if (!scrollBarView.showBothScrollIndicator) {
						scrollBarView.OtherScrollBarView.ShowScrollIndicator = true;
					}
				}
				if (!scrollBarView.showScrollIndicator) {
					scrollBarView.ShowScrollIndicator = true;
				}
			}

			return pending;
		}

		int posTopTee;
		int posLeftTee;
		int posBottomTee;
		int posRightTee;

		///<inheritdoc/>
		public override void Redraw (Rect region)
		{
			if (ColorScheme == null || Size == 0) {
				return;
			}

			Driver.SetAttribute (ColorScheme.Normal);

			if ((vertical && Bounds.Height == 0) || (!vertical && Bounds.Width == 0)) {
				return;
			}

			if (vertical) {
				if (region.Right < Bounds.Width - 1) {
					return;
				}

				var col = Bounds.Width - 1;
				var bh = Bounds.Height;
				Rune special;

				if (bh < 4) {
					var by1 = position * bh / Size;
					var by2 = (position + bh) * bh / Size;

					Move (col, 0);
					if (Bounds.Height == 1) {
						Driver.AddRune (Driver.Diamond);
					} else {
						Driver.AddRune (Driver.UpArrow);
					}
					if (Bounds.Height == 3) {
						Move (col, 1);
						Driver.AddRune (Driver.Diamond);
					}
					if (Bounds.Height > 1) {
						Move (col, Bounds.Height - 1);
						Driver.AddRune (Driver.DownArrow);
					}
				} else {
					bh -= 2;
					var by1 = position * bh / Size;
					var by2 = KeepContentAlwaysInViewport ? Math.Min (((position + bh) * bh / Size) + 1, bh - 1) : (position + bh) * bh / Size;
					if (KeepContentAlwaysInViewport && by1 == by2) {
						by1 = Math.Max (by1 - 1, 0);
					}

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
								} else if ((position == 0 && y == bh - 1 || y >= by2 || by2 == 0) && !hasBottomTee) {
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
				if (region.Bottom < Bounds.Height - 1) {
					return;
				}

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
					var bx2 = KeepContentAlwaysInViewport ? Math.Min (((position + bw) * bw / Size) + 1, bw - 1) : (position + bw) * bw / Size;
					if (KeepContentAlwaysInViewport && bx1 == bx2) {
						bx1 = Math.Max (bx1 - 1, 0);
					}

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
								} else if ((position == 0 && x == bw - 1 || x >= bx2 || bx2 == 0) && !hasRightTee) {
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
						Move (Bounds.Width - 2, row);
						Driver.AddRune (Driver.LeftTee);
					}

					Driver.AddRune (Driver.RightArrow);
				}
			}

			if (hosted && showBothScrollIndicator) {
				contentBottomRightCorner.Redraw (contentBottomRightCorner.Bounds);
			}
		}

		int lastLocation = -1;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags != MouseFlags.Button1Pressed && me.Flags != MouseFlags.Button1Clicked &&
				!me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) &&
				me.Flags != MouseFlags.Button1Released && me.Flags != MouseFlags.WheeledDown &&
				me.Flags != MouseFlags.WheeledUp && me.Flags != MouseFlags.WheeledRight &&
				me.Flags != MouseFlags.WheeledLeft) {
				return false;
			}

			if (Host != null && !Host.HasFocus) {
				Host.SetFocus ();
			}

			int location = vertical ? me.Y : me.X;
			int barsize = vertical ? Bounds.Height : Bounds.Width;
			int posTopLeftTee = vertical ? posTopTee + 1 : posLeftTee + 1;
			int posBottomRightTee = vertical ? posBottomTee + 1 : posRightTee + 1;
			barsize -= 2;
			var pos = Position;

			if ((me.Flags.HasFlag (MouseFlags.Button1Pressed) ||
				me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
				&& (Application.mouseGrabView == null || Application.mouseGrabView != this)) {
				Application.GrabMouse (this);
			} else if (me.Flags == MouseFlags.Button1Released && Application.mouseGrabView != null && Application.mouseGrabView == this) {
				Application.UngrabMouse ();
				return true;
			} else if (showScrollIndicator && (me.Flags == MouseFlags.WheeledDown || me.Flags == MouseFlags.WheeledUp ||
				me.Flags == MouseFlags.WheeledRight || me.Flags == MouseFlags.WheeledLeft)) {
				return Host.MouseEvent (me);
			}

			if (!me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
				lastLocation = -1;
			}
			if (location == 0) {
				if (pos > 0) {
					Position = pos - 1;
				}
			} else if (location == barsize + 1) {
				if (CanScroll (1, out _, vertical)) {
					Position = pos + 1;
				}
			} else if (location > 0 && location < barsize + 1) {
				var b1 = pos * (Size > 0 ? barsize / Size : 0);
				var b2 = Size > 0
					? (KeepContentAlwaysInViewport ? Math.Min (((pos + barsize) * barsize / Size) + 1, barsize - 1) : (pos + barsize) * barsize / Size)
					: 0;
				if (KeepContentAlwaysInViewport && b1 == b2) {
					b1 = Math.Max (b1 - 1, 0);
				}

				if (location > b1 && location <= b2 + 1) {
					if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1Clicked) {
						if (location == 1 && posTopLeftTee <= 2) {
							Position = 0;
						} else if (location == barsize) {
							CanScroll (Size - pos, out int nv, vertical);
							if (nv > 0) {
								Position = Math.Min (pos + nv, Size);
							}
						} else if (location < posTopLeftTee) {
							if (CanScroll (-barsize, out int nv, vertical)) {
								Position = pos + nv;
							}
						}
					} else if (me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
						var mb = (b2 - b1) / 2;
						var ml = mb + b1 + (mb == 0 ? 1 : 0);
						if ((location > 1 || (location == 1 && posTopLeftTee > 1)) && ((location >= b1 && location <= ml) || (location < lastLocation && lastLocation > -1))) {
							lastLocation = location;
							var np = location * Size / barsize;
							if (CanScroll (np - pos, out int nv, vertical)) {
								Position = pos + nv;
							}
						} else if (location != barsize && location > lastLocation) {
							var np = location * Size / barsize;
							if (CanScroll (np - pos, out int nv, vertical)) {
								Position = pos + nv;
							}
						} else if (location == 1 && posTopLeftTee <= 2) {
							Position = 0;
						} else if (location == barsize) {
							CanScroll (Size - pos, out int nv, vertical);
							if (nv > 0) {
								Position = Math.Min (pos + nv, Size);
							}
						}
					}
				} else {
					if (location >= b2 + 1 && location > posTopLeftTee && location > b1 && location > posBottomRightTee && posBottomRightTee > 0) {
						CanScroll (location, out int nv, vertical);
						if (nv > 0) {
							Position = Math.Min (pos + nv, Size);
						}
					} else if (location <= b1) {
						Position = Math.Max (pos - barsize - location, 0);
					}
				}
			}

			return true;
		}

		internal bool CanScroll (int n, out int max, bool isVertical = false)
		{
			if (Host == null) {
				max = 0;
				return false;
			}
			var s = isVertical ?
				(KeepContentAlwaysInViewport ? Host.Bounds.Height + (showBothScrollIndicator ? -2 : -1) : 0) :
				(KeepContentAlwaysInViewport ? Host.Bounds.Width + (showBothScrollIndicator ? -2 : -1) : 0);
			var newSize = Math.Min (size, position + n);
			max = size > s + newSize ? n : size - (s + position) - 1;
			if (size > s + newSize) {
				return true;
			}
			return false;
		}
	}
}
