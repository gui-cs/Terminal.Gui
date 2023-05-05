﻿//
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

		bool showBothScrollIndicator => OtherScrollBarView?.showScrollIndicator == true && showScrollIndicator;

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
			SetInitialProperties (size, position, isVertical);
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
			SetInitialProperties (size, position, isVertical);
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
			CanFocus = false;
			Enabled = host.Enabled;
			Visible = host.Visible;
			//Host.CanFocusChanged += Host_CanFocusChanged;
			Host.EnabledChanged += Host_EnabledChanged;
			Host.VisibleChanged += Host_VisibleChanged;
			Host.SuperView.Add (this);
			AutoHideScrollBars = true;
			if (showBothScrollIndicator) {
				OtherScrollBarView = new ScrollBarView (0, 0, !isVertical) {
					Id = "OtherScrollBarView",
					ColorScheme = host.ColorScheme,
					Host = host,
					CanFocus = false,
					Enabled = host.Enabled,
					Visible = host.Visible,
					OtherScrollBarView = this
				};
				OtherScrollBarView.hosted = true;
				// BUGBUG: v2 - Host may be superview and thus this may be bogus
				OtherScrollBarView.X = OtherScrollBarView.IsVertical ? Pos.Right (host) - 1 : Pos.Left (host);
				OtherScrollBarView.Y = OtherScrollBarView.IsVertical ? Pos.Top (host) : Pos.Bottom (host) - 1;
				OtherScrollBarView.Host.SuperView.Add (OtherScrollBarView);
				OtherScrollBarView.ShowScrollIndicator = true;
			}
			ShowScrollIndicator = true;
			CreateBottomRightCorner ();
			ClearOnVisibleFalse = false;
		}

		private void CreateBottomRightCorner ()
		{
			if (Host != null && (contentBottomRightCorner == null && OtherScrollBarView == null
				|| (contentBottomRightCorner == null && OtherScrollBarView != null && OtherScrollBarView.contentBottomRightCorner == null))) {

				contentBottomRightCorner = new View (" ") {
					Id = "contentBottomRightCorner",
					Visible = Host.Visible,
					ClearOnVisibleFalse = false
				};
				Host.Add (contentBottomRightCorner);
				contentBottomRightCorner.X = Pos.Right (Host) - 1;
				contentBottomRightCorner.Y = Pos.Bottom (Host) - 1;
				contentBottomRightCorner.Width = 1;
				contentBottomRightCorner.Height = 1;
				contentBottomRightCorner.MouseClick += ContentBottomRightCorner_MouseClick;
			}
		}

		private void Host_VisibleChanged (object sender, EventArgs e)
		{
			if (!Host.Visible) {
				Visible = Host.Visible;
				if (otherScrollBarView != null) {
					otherScrollBarView.Visible = Visible;
				}
				contentBottomRightCorner.Visible = Visible;
			} else {
				ShowHideScrollBars ();
			}
		}

		private void Host_EnabledChanged (object sender, EventArgs e)
		{
			Enabled = Host.Enabled;
			if (otherScrollBarView != null) {
				otherScrollBarView.Enabled = Enabled;
			}
			contentBottomRightCorner.Enabled = Enabled;
		}

		//private void Host_CanFocusChanged ()
		//{
		//	CanFocus = Host.CanFocus;
		//	if (otherScrollBarView != null) {
		//		otherScrollBarView.CanFocus = CanFocus;
		//	}
		//}

		void ContentBottomRightCorner_MouseClick (object sender, MouseEventEventArgs me)
		{
			if (me.MouseEvent.Flags == MouseFlags.WheeledDown || me.MouseEvent.Flags == MouseFlags.WheeledUp
			    || me.MouseEvent.Flags == MouseFlags.WheeledRight || me.MouseEvent.Flags == MouseFlags.WheeledLeft) {

				MouseEvent (me.MouseEvent);
			} else if (me.MouseEvent.Flags == MouseFlags.Button1Clicked) {
				Host.SetFocus ();
			}

			me.Handled = true;
		}

		void SetInitialProperties (int size, int position, bool isVertical)
		{
			Id = "ScrollBarView";
			vertical = isVertical;
			this.position = position;
			this.size = size;
			WantContinuousButtonPressed = true;
			ClearOnVisibleFalse = false;

			Added += (s, e) => CreateBottomRightCorner ();

			Initialized += (s, e) => {
				SetWidthHeight ();
				SetRelativeLayout (SuperView?.Frame ?? Host?.Frame ?? Frame);
				if (Id == "OtherScrollBarView" || OtherScrollBarView == null) {
					// Only do this once if both scrollbars are enabled
					ShowHideScrollBars ();
				}
				SetPosition (position);
			};
		}

		/// <summary>
		/// If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal.
		/// </summary>
		public bool IsVertical {
			get => vertical;
			set {
				vertical = value;
				if (IsInitialized) {
					SetWidthHeight ();
				}
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
				if (IsInitialized) {
					SetRelativeLayout (SuperView?.Frame ?? Host.Frame);
					ShowHideScrollBars (false);
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// This event is raised when the position on the scrollbar has changed.
		/// </summary>
		public event EventHandler ChangedPosition;

		/// <summary>
		/// The position, relative to <see cref="Size"/>, to set the scrollbar at.
		/// </summary>
		/// <value>The position.</value>
		public int Position {
			get => position;
			set {
				position = value;
				if (IsInitialized) {
					// We're not initialized so we can't do anything fancy. Just cache value.
					SetPosition (value);
				}
			}
		}

		// Helper to assist Initialized event handler
		private void SetPosition (int newPosition)
		{
			if (CanScroll (newPosition - position, out int max, vertical)) {
				if (max == newPosition - position) {
					position = newPosition;
				} else {
					position = Math.Max (position + max, 0);
				}
			} else if (max < 0) {
				position = Math.Max (position + max, 0);
			} else {
				position = Math.Max (newPosition, 0);
			}
			OnChangedPosition ();
			SetNeedsDisplay ();
		}

		// BUGBUG: v2 - for consistency this should be named "Parent" not "Host"
		/// <summary>
		/// Get or sets the view that host this <see cref="ScrollBarView"/>
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

		// BUGBUG: v2 - Why can't we get rid of this and just use Visible?
		/// <summary>
		/// Gets or sets the visibility for the vertical or horizontal scroll indicator.
		/// </summary>
		/// <value><c>true</c> if show vertical or horizontal scroll indicator; otherwise, <c>false</c>.</value>
		public bool ShowScrollIndicator {
			get => showScrollIndicator;
			set {
				//if (value == showScrollIndicator) {
				//	return;
				//}

				showScrollIndicator = value;
				if (IsInitialized) {
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
					if (pos == 0) {
						Refresh ();
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
			ChangedPosition?.Invoke (this, EventArgs.Empty);
		}

		/// <summary>
		/// Only used for a hosted view that will update and redraw the scrollbars.
		/// </summary>
		public virtual void Refresh ()
		{
			ShowHideScrollBars ();
		}

		void ShowHideScrollBars (bool redraw = true)
		{
			if (!hosted || (hosted && !autoHideScrollBars)) {
				if (contentBottomRightCorner != null && contentBottomRightCorner.Visible) {
					contentBottomRightCorner.Visible = false;
				} else if (otherScrollBarView != null && otherScrollBarView.contentBottomRightCorner != null && otherScrollBarView.contentBottomRightCorner.Visible) {
					otherScrollBarView.contentBottomRightCorner.Visible = false;
				}
				return;
			}

			var pending = CheckBothScrollBars (this);
			if (otherScrollBarView != null) {
				CheckBothScrollBars (otherScrollBarView, pending);
			}

			SetWidthHeight ();
			SetRelativeLayout (SuperView?.Frame ?? Host.Frame);
			if (otherScrollBarView != null) {
				OtherScrollBarView.SetRelativeLayout (SuperView?.Frame ?? Host.Frame);
			}

			if (showBothScrollIndicator) {
				if (contentBottomRightCorner != null) {
					contentBottomRightCorner.Visible = true;
				} else if (otherScrollBarView != null && otherScrollBarView.contentBottomRightCorner != null) {
					otherScrollBarView.contentBottomRightCorner.Visible = true;
				}
			} else if (!showScrollIndicator) {
				if (contentBottomRightCorner != null) {
					contentBottomRightCorner.Visible = false;
				} else if (otherScrollBarView != null && otherScrollBarView.contentBottomRightCorner != null) {
					otherScrollBarView.contentBottomRightCorner.Visible = false;
				}
				if (Application.MouseGrabView != null && Application.MouseGrabView == this) {
					Application.UngrabMouse ();
				}
			} else if (contentBottomRightCorner != null) {
				contentBottomRightCorner.Visible = false;
			} else if (otherScrollBarView != null && otherScrollBarView.contentBottomRightCorner != null) {
				otherScrollBarView.contentBottomRightCorner.Visible = false;
			}
			if (Host?.Visible == true && showScrollIndicator && !Visible) {
				Visible = true;
			}
			if (Host?.Visible == true && otherScrollBarView?.showScrollIndicator == true && !otherScrollBarView.Visible) {
				otherScrollBarView.Visible = true;
			}

			if (!redraw) {
				return;
			}

			if (showScrollIndicator) {
				Draw ();
			}
			if (otherScrollBarView != null && otherScrollBarView.showScrollIndicator) {
				otherScrollBarView.Draw ();
			}
		}

		bool CheckBothScrollBars (ScrollBarView scrollBarView, bool pending = false)
		{
			int barsize = scrollBarView.vertical ? scrollBarView.Bounds.Height : scrollBarView.Bounds.Width;

			if (barsize == 0 || barsize >= scrollBarView.size) {
				if (scrollBarView.showScrollIndicator) {
					scrollBarView.ShowScrollIndicator = false;
				}
				if (scrollBarView.Visible) {
					scrollBarView.Visible = false;
				}
			} else if (barsize > 0 && barsize == scrollBarView.size && scrollBarView.OtherScrollBarView != null && pending) {
				if (scrollBarView.showScrollIndicator) {
					scrollBarView.ShowScrollIndicator = false;
				}
				if (scrollBarView.Visible) {
					scrollBarView.Visible = false;
				}
				if (scrollBarView.OtherScrollBarView != null && scrollBarView.showBothScrollIndicator) {
					scrollBarView.OtherScrollBarView.ShowScrollIndicator = false;
				}
				if (scrollBarView.OtherScrollBarView.Visible) {
					scrollBarView.OtherScrollBarView.Visible = false;
				}
			} else if (barsize > 0 && barsize == size && scrollBarView.OtherScrollBarView != null && !pending) {
				pending = true;
			} else {
				if (scrollBarView.OtherScrollBarView != null && pending) {
					if (!scrollBarView.showBothScrollIndicator) {
						scrollBarView.OtherScrollBarView.ShowScrollIndicator = true;
					}
					if (!scrollBarView.OtherScrollBarView.Visible) {
						scrollBarView.OtherScrollBarView.Visible = true;
					}
				}
				if (!scrollBarView.showScrollIndicator) {
					scrollBarView.ShowScrollIndicator = true;
				}
				if (!scrollBarView.Visible) {
					scrollBarView.Visible = true;
				}
			}

			return pending;
		}

		// BUGBUG: v2 - rationalize this with View.SetMinWidthHeight
		void SetWidthHeight ()
		{
			// BUGBUG: v2 - If Host is also the ScrollBarView's superview, this is all bogus because it's not
			// supported that a view can reference it's superview's Dims. This code also assumes the host does 
			//  not have a margin/borderframe/padding.
			if (!IsInitialized) {
				return;
			}

			if (showBothScrollIndicator) {
				Width = vertical ? 1 : Host != SuperView ? Dim.Width (Host) - 1 : Dim.Fill () - 1;
				Height = vertical ? Host != SuperView ? Dim.Height (Host) - 1 : Dim.Fill () - 1 : 1;

				otherScrollBarView.Width = otherScrollBarView.vertical ? 1 : Host != SuperView ? Dim.Width (Host) - 1 : Dim.Fill () - 1;
				otherScrollBarView.Height = otherScrollBarView.vertical ? Host != SuperView ? Dim.Height (Host) - 1 : Dim.Fill () - 1 : 1;
			} else if (showScrollIndicator) {
				Width = vertical ? 1 : Host != SuperView ? Dim.Width (Host) : Dim.Fill ();
				Height = vertical ? Host != SuperView ? Dim.Height (Host) : Dim.Fill () : 1;
			} else if (otherScrollBarView?.showScrollIndicator == true) {
				otherScrollBarView.Width = otherScrollBarView.vertical ? 1 : Host != SuperView ? Dim.Width (Host) : Dim.Fill () - 0;
				otherScrollBarView.Height = otherScrollBarView.vertical ? Host != SuperView ? Dim.Height (Host) : Dim.Fill () - 0 : 1;
			}
		}

		int posTopTee;
		int posLeftTee;
		int posBottomTee;
		int posRightTee;

		///<inheritdoc/>
		public override void OnDrawContent (Rect contentArea)
		{
			if (ColorScheme == null || ((!showScrollIndicator || Size == 0) && AutoHideScrollBars && Visible)) {
				if ((!showScrollIndicator || Size == 0) && AutoHideScrollBars && Visible) {
					ShowHideScrollBars (false);
				}
				return;
			}

			if (Size == 0 || (vertical && Bounds.Height == 0) || (!vertical && Bounds.Width == 0)) {
				return;
			}

			Driver.SetAttribute (Host.HasFocus ? ColorScheme.Focus : GetNormalColor ());

			if (vertical) {
				if (Bounds.Right < Bounds.Width - 1) {
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
						Driver.AddRune (Glyphs.Diamond);
					} else {
						Driver.AddRune (Glyphs.UpArrow);
					}
					if (Bounds.Height == 3) {
						Move (col, 1);
						Driver.AddRune (Glyphs.Diamond);
					}
					if (Bounds.Height > 1) {
						Move (col, Bounds.Height - 1);
						Driver.AddRune (Glyphs.DownArrow);
					}
				} else {
					bh -= 2;
					var by1 = KeepContentAlwaysInViewport ? position * bh / Size : position * bh / (Size + bh);
					var by2 = KeepContentAlwaysInViewport ? Math.Min (((position + bh) * bh / Size) + 1, bh - 1) : (position + bh) * bh / (Size + bh);
					if (KeepContentAlwaysInViewport && by1 == by2) {
						by1 = Math.Max (by1 - 1, 0);
					}

					Move (col, 0);
					Driver.AddRune (Glyphs.UpArrow);

					bool hasTopTee = false;
					bool hasDiamond = false;
					bool hasBottomTee = false;
					for (int y = 0; y < bh; y++) {
						Move (col, y + 1);
						if ((y < by1 || y > by2) && ((position > 0 && !hasTopTee) || (hasTopTee && hasBottomTee))) {
							special = Glyphs.Stipple;
						} else {
							if (y != by2 && y > 1 && by2 - by1 == 0 && by1 < bh - 1 && hasTopTee && !hasDiamond) {
								hasDiamond = true;
								special = Glyphs.Diamond;
							} else {
								if (y == by1 && !hasTopTee) {
									hasTopTee = true;
									posTopTee = y;
									special = Glyphs.TopTee;
								} else if ((position == 0 && y == bh - 1 || y >= by2 || by2 == 0) && !hasBottomTee) {
									hasBottomTee = true;
									posBottomTee = y;
									special = Glyphs.BottomTee;
								} else {
									special = Glyphs.VLine;
								}
							}
						}
						Driver.AddRune (special);
					}
					if (!hasTopTee) {
						Move (col, Bounds.Height - 2);
						Driver.AddRune (Glyphs.TopTee);
					}
					Move (col, Bounds.Height - 1);
					Driver.AddRune (Glyphs.DownArrow);
				}
			} else {
				if (Bounds.Bottom < Bounds.Height - 1) {
					return;
				}

				var row = Bounds.Height - 1;
				var bw = Bounds.Width;
				Rune special;

				if (bw < 4) {
					var bx1 = position * bw / Size;
					var bx2 = (position + bw) * bw / Size;

					Move (0, row);
					Driver.AddRune (Glyphs.LeftArrow);
					Driver.AddRune (Glyphs.RightArrow);
				} else {
					bw -= 2;
					var bx1 = KeepContentAlwaysInViewport ? position * bw / Size : position * bw / (Size + bw);
					var bx2 = KeepContentAlwaysInViewport ? Math.Min (((position + bw) * bw / Size) + 1, bw - 1) : (position + bw) * bw / (Size + bw);
					if (KeepContentAlwaysInViewport && bx1 == bx2) {
						bx1 = Math.Max (bx1 - 1, 0);
					}

					Move (0, row);
					Driver.AddRune (Glyphs.LeftArrow);

					bool hasLeftTee = false;
					bool hasDiamond = false;
					bool hasRightTee = false;
					for (int x = 0; x < bw; x++) {
						if ((x < bx1 || x >= bx2 + 1) && ((position > 0 && !hasLeftTee) || (hasLeftTee && hasRightTee))) {
							special = Glyphs.Stipple;
						} else {
							if (x != bx2 && x > 1 && bx2 - bx1 == 0 && bx1 < bw - 1 && hasLeftTee && !hasDiamond) {
								hasDiamond = true;
								special = Glyphs.Diamond;
							} else {
								if (x == bx1 && !hasLeftTee) {
									hasLeftTee = true;
									posLeftTee = x;
									special = Glyphs.LeftTee;
								} else if ((position == 0 && x == bw - 1 || x >= bx2 || bx2 == 0) && !hasRightTee) {
									hasRightTee = true;
									posRightTee = x;
									special = Glyphs.RightTee;
								} else {
									special = Glyphs.HLine;
								}
							}
						}
						Driver.AddRune (special);
					}
					if (!hasLeftTee) {
						Move (Bounds.Width - 2, row);
						Driver.AddRune (Glyphs.LeftTee);
					}

					Driver.AddRune (Glyphs.RightArrow);
				}
			}

			// BUGBUG: v2 - contentBottomRightCorner is a view. it will be drawn by Host.Superview.OnDraw; no 
			// need to draw it here.
			//if (contentBottomRightCorner != null && hosted && showBothScrollIndicator) {
			//	contentBottomRightCorner.Redraw (contentBottomRightCorner.Bounds);
			//} else if (otherScrollBarView != null && otherScrollBarView.contentBottomRightCorner != null && otherScrollBarView.hosted && otherScrollBarView.showBothScrollIndicator) {
			//	otherScrollBarView.contentBottomRightCorner.Redraw (otherScrollBarView.contentBottomRightCorner.Bounds);
			//}
		}

		int lastLocation = -1;
		int posBarOffset;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent mouseEvent)
		{
			if (mouseEvent.Flags != MouseFlags.Button1Pressed && mouseEvent.Flags != MouseFlags.Button1DoubleClicked &&
				!mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) &&
				mouseEvent.Flags != MouseFlags.Button1Released && mouseEvent.Flags != MouseFlags.WheeledDown &&
				mouseEvent.Flags != MouseFlags.WheeledUp && mouseEvent.Flags != MouseFlags.WheeledRight &&
				mouseEvent.Flags != MouseFlags.WheeledLeft && mouseEvent.Flags != MouseFlags.Button1TripleClicked) {

				return false;
			}

			if (!Host.CanFocus) {
				return true;
			}
			if (Host?.HasFocus == false) {
				Host.SetFocus ();
			}

			int location = vertical ? mouseEvent.Y : mouseEvent.X;
			int barsize = vertical ? Bounds.Height : Bounds.Width;
			int posTopLeftTee = vertical ? posTopTee + 1 : posLeftTee + 1;
			int posBottomRightTee = vertical ? posBottomTee + 1 : posRightTee + 1;
			barsize -= 2;
			var pos = Position;

			if (mouseEvent.Flags != MouseFlags.Button1Released
				&& (Application.MouseGrabView == null || Application.MouseGrabView != this)) {

				Application.GrabMouse (this);
			} else if (mouseEvent.Flags == MouseFlags.Button1Released && Application.MouseGrabView != null && Application.MouseGrabView == this) {
				lastLocation = -1;
				Application.UngrabMouse ();
				return true;
			}
			if (showScrollIndicator && (mouseEvent.Flags == MouseFlags.WheeledDown || mouseEvent.Flags == MouseFlags.WheeledUp ||
				mouseEvent.Flags == MouseFlags.WheeledRight || mouseEvent.Flags == MouseFlags.WheeledLeft)) {

				return Host.MouseEvent (mouseEvent);
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
				//var b1 = pos * (Size > 0 ? barsize / Size : 0);
				//var b2 = Size > 0
				//	? (KeepContentAlwaysInViewport ? Math.Min (((pos + barsize) * barsize / Size) + 1, barsize - 1) : (pos + barsize) * barsize / Size)
				//	: 0;
				//if (KeepContentAlwaysInViewport && b1 == b2) {
				//	b1 = Math.Max (b1 - 1, 0);
				//}

				if (lastLocation > -1 || (location >= posTopLeftTee && location <= posBottomRightTee
				&& mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))) {
					if (lastLocation == -1) {
						lastLocation = location;
						posBarOffset = keepContentAlwaysInViewport ? Math.Max (location - posTopLeftTee, 1) : 0;
						return true;
					}

					if (location > lastLocation) {
						if (location - posBarOffset < barsize) {
							var np = ((location - posBarOffset) * Size / barsize) + (Size / barsize);
							if (CanScroll (np - pos, out int nv, vertical)) {
								Position = pos + nv;
							}
						} else if (CanScroll (Size - pos, out int nv, vertical)) {
							Position = Math.Min (pos + nv, Size);
						}
					} else if (location < lastLocation) {
						if (location - posBarOffset > 0) {
							var np = ((location - posBarOffset) * Size / barsize) - (Size / barsize);
							if (CanScroll (np - pos, out int nv, vertical)) {
								Position = pos + nv;
							}
						} else {
							Position = 0;
						}
					} else if (location - posBarOffset >= barsize && posBottomRightTee - posTopLeftTee >= 3 && CanScroll (Size - pos, out int nv, vertical)) {
						Position = Math.Min (pos + nv, Size);
					} else if (location - posBarOffset >= barsize - 1 && posBottomRightTee - posTopLeftTee <= 3 && CanScroll (Size - pos, out nv, vertical)) {
						Position = Math.Min (pos + nv, Size);
					} else if (location - posBarOffset <= 0 && posBottomRightTee - posTopLeftTee <= 3) {
						Position = 0;
					}
				} else if (location > posBottomRightTee) {
					if (CanScroll (barsize, out int nv, vertical)) {
						Position = pos + nv;
					}
				} else if (location < posTopLeftTee) {
					if (CanScroll (-barsize, out int nv, vertical)) {
						Position = pos + nv;
					}
				} else if (location == 1 && posTopLeftTee <= 3) {
					Position = 0;
				} else if (location == barsize) {
					if (CanScroll (Size - pos, out int nv, vertical)) {
						Position = Math.Min (pos + nv, Size);
					}
				}
			}

			return true;
		}

		internal bool CanScroll (int n, out int max, bool isVertical = false)
		{
			if (Host?.Bounds.IsEmpty != false) {
				max = 0;
				return false;
			}
			int s = GetBarsize (isVertical);
			var newSize = Math.Max (Math.Min (size - s, position + n), 0);
			max = size > s + newSize ? (newSize == 0 ? -position : n) : size - (s + position) - 1;
			if (size >= s + newSize && max != 0) {
				return true;
			}
			return false;
		}

		int GetBarsize (bool isVertical)
		{
			if (Host?.Bounds.IsEmpty != false) {
				return 0;
			}
			return isVertical ?
				(KeepContentAlwaysInViewport ? Host.Bounds.Height + (showBothScrollIndicator ? -2 : -1) : 0) :
				(KeepContentAlwaysInViewport ? Host.Bounds.Width + (showBothScrollIndicator ? -2 : -1) : 0);
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}
}
