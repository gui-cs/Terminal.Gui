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
		Dim originalHostWidth, originalHostHeight;
		bool hosted;
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
		public ScrollBarView (View host, bool isVertical) : this (0, 0, isVertical)
		{
			if (host == null) {
				throw new ArgumentNullException ("The host parameter can't be null.");
			} else if (host.SuperView == null) {
				throw new ArgumentNullException ("The host SuperView parameter can't be null.");
			}
			hosted = true;
			originalHostWidth = host.Width;
			originalHostHeight = host.Height;
			X = isVertical ? Pos.Right(host) : Pos.Left (host);
			Y = isVertical ? Pos.Top (host) : Pos.Bottom (host);
			Host = host;
			Host.SuperView.Add (this);
			ShowScrollIndicator = true;
			AutoHideScrollBars = true;
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
				ShowHideScrollBars ();
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
		public ScrollBarView OtherScrollBarView { get; set; }

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
				}
				Width = vertical ? 1 : Dim.Width (Host);
				Height = vertical ? Dim.Height (Host) : 1;
				if (vertical) {
					Host.Width = showScrollIndicator ? originalHostWidth - 1 : originalHostWidth;
				} else {
					Host.Height = showScrollIndicator ? originalHostHeight - 1 : originalHostHeight;
				}
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

		void SetPosition (int newPos)
		{
			Position = newPos;
			OnChangedPosition ();
		}

		/// <summary>
		/// Virtual method to invoke the <see cref="ChangedPosition"/> action event.
		/// </summary>
		public virtual void OnChangedPosition ()
		{
			ChangedPosition?.Invoke ();
		}

		internal bool pending;

		void ShowHideScrollBars ()
		{
			if (!hosted || !autoHideScrollBars) {
				return;
			}

			int barsize = vertical ? Bounds.Height : Bounds.Width;

			if (barsize == 0 || barsize > size) {
				if (showScrollIndicator) {
					ShowScrollIndicator = false;
				}
			} else if (barsize > 0 && barsize == size && OtherScrollBarView != null && OtherScrollBarView.pending) {
				if (showScrollIndicator) {
					ShowScrollIndicator = false;
				}
				if (OtherScrollBarView != null && showBothScrollIndicator) {
					OtherScrollBarView.ShowScrollIndicator = false;
				}
			} else if (barsize > 0 && barsize == size && OtherScrollBarView != null && !OtherScrollBarView.pending) {
				pending = true;
				OtherScrollBarView.Redraw (OtherScrollBarView.Bounds);
			} else {
				if (OtherScrollBarView != null && OtherScrollBarView.pending) {
					if (!showBothScrollIndicator) {
						OtherScrollBarView.ShowScrollIndicator = true;
						OtherScrollBarView.Redraw (OtherScrollBarView.Bounds);
					}
				}
				if (!showScrollIndicator) {
					ShowScrollIndicator = true;
				}
			}
			if (OtherScrollBarView != null) {
				OtherScrollBarView.pending = false;
			}
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
		}

		int lastLocation = -1;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags != MouseFlags.Button1Pressed && me.Flags != MouseFlags.Button1Clicked &&
				!me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
				return false;
			}

			if (!me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
				lastLocation = -1;
			}

			int location = vertical ? me.Y : me.X;
			int barsize = vertical ? Bounds.Height : Bounds.Width;
			int posTopLeftTee = vertical ? posTopTee : posLeftTee;
			int posBottomRightTee = vertical ? posBottomTee : posRightTee;

			barsize -= 2;
			var pos = Position;
			if (location == 0) {
				if (pos > 0) {
					SetPosition (pos - 1);
				}
			} else if (location == barsize + 1) {
				if (CanScroll (1, out _, vertical)) {
					SetPosition (pos + 1);
				}
			} else if (location > 0 && location < barsize + 1) {
				var b1 = pos * barsize / Size;
				var b2 = KeepContentAlwaysInViewport ? Math.Min (((pos + barsize) * barsize / Size) + 1, barsize - 1) : (pos + barsize) * barsize / Size;
				if (KeepContentAlwaysInViewport && b1 == b2) {
					b1 = Math.Max (b1 - 1, 0);
				}

				if (location > b1 && location <= b2 + 1) {
					if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1Clicked) {
						if (location == 1) {
							SetPosition (0);
						} else if (location == barsize) {
							CanScroll (Size - pos, out int nv, vertical);
							if (nv > 0) {
								SetPosition (Math.Min (pos + nv, Size));
							}
						}
					} else if (me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
						var mb = (b2 - b1) / 2;
						var ml = mb + b1 + (mb == 0 ? 1 : 0);
						if ((location >= b1 && location <= ml) || (location < lastLocation && lastLocation > -1)) {
							lastLocation = location;
							var np = b1 * Size / barsize;
							SetPosition (np);
						} else if (location > lastLocation) {
							var np = location * Size / barsize;
							CanScroll (np - pos, out int nv, vertical);
							if (nv > 0) {
								SetPosition (pos + nv);
							}
						}
					}
				} else {
					if (location >= b2 + 1 && location > posTopLeftTee && location > b1 && location > posBottomRightTee && posBottomRightTee > 0) {
						CanScroll (location, out int nv, vertical);
						if (nv > 0) {
							SetPosition (Math.Min (pos + nv, Size));
						}
					} else if (location <= b1) {
						SetPosition (Math.Max (pos - barsize - location, 0));
					}
				}
			}

			return true;
		}

		internal bool CanScroll (int n, out int max, bool isVertical = false)
		{
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
