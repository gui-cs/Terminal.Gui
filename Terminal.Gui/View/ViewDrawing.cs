﻿using System;
using System.Linq;
using System.Text;

namespace Terminal.Gui {
	public partial class View {
		/// <summary>
		/// Specifies the side to start when draw frame with 
		/// <see cref="DrawIncompleteFrame(ValueTuple{int, Side}, ValueTuple{int, Side}, Rect, LineStyle, Attribute?, bool)"/> method.
		/// </summary>
		public enum Side {
			/// <summary>
			/// Start on left.
			/// </summary>
			Left,
			/// <summary>
			/// Start on top.
			/// </summary>
			Top,
			/// <summary>
			/// Start on right.
			/// </summary>
			Right,
			/// <summary>
			/// Start on bottom.
			/// </summary>
			Bottom
		};

		ColorScheme _colorScheme;

		/// <summary>
		/// The color scheme for this view, if it is not defined, it returns the <see cref="SuperView"/>'s
		/// color scheme. 
		/// </summary>
		public virtual ColorScheme ColorScheme {
			get {
				if (_colorScheme == null) {
					return SuperView?.ColorScheme;
				}
				return _colorScheme;
			}
			set {
				if (_colorScheme != value) {
					_colorScheme = value;
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
		/// </summary>
		/// <returns><see cref="Terminal.Gui.ColorScheme.Normal"/> if <see cref="Enabled"/> is <see langword="true"/>
		/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
		/// If it's overridden can return other values.</returns>
		public virtual Attribute GetNormalColor ()
		{
			ColorScheme cs = ColorScheme;
			if (ColorScheme == null) {
				cs = new ColorScheme ();
			}
			return Enabled ? cs.Normal : cs.Disabled;
		}

		/// <summary>
		/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
		/// </summary>
		/// <returns><see cref="Terminal.Gui.ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/>
		/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
		/// If it's overridden can return other values.</returns>
		public virtual Attribute GetFocusColor ()
		{
			return Enabled ? ColorScheme.Focus : ColorScheme.Disabled;
		}

		/// <summary>
		/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
		/// </summary>
		/// <returns><see cref="Terminal.Gui.ColorScheme.HotNormal"/> if <see cref="Enabled"/> is <see langword="true"/>
		/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
		/// If it's overridden can return other values.</returns>
		public virtual Attribute GetHotNormalColor ()
		{
			return Enabled ? ColorScheme.HotNormal : ColorScheme.Disabled;
		}

		/// <summary>
		/// Displays the specified character in the specified column and row of the View.
		/// </summary>
		/// <param name="col">Column (view-relative).</param>
		/// <param name="row">Row (view-relative).</param>
		/// <param name="ch">Ch.</param>
		public void AddRune (int col, int row, Rune ch)
		{
			if (row < 0 || col < 0)
				return;
			if (row > _frame.Height - 1 || col > _frame.Width - 1)
				return;
			Move (col, row);
			Driver.AddRune (ch);
		}

		/// <summary>
		/// Clears <see cref="NeedsDisplay"/> and <see cref="SubViewNeedsDisplay"/>.
		/// </summary>
		protected void ClearNeedsDisplay ()
		{
			_needsDisplayRect = Rect.Empty;
			_subViewNeedsDisplay = false;
		}

		// The view-relative region that needs to be redrawn. Marked internal for unit tests.
		internal Rect _needsDisplayRect = Rect.Empty;

		/// <summary>
		/// Gets or sets whether the view needs to be redrawn.
		/// </summary>
		public bool NeedsDisplay {
			get => _needsDisplayRect != Rect.Empty;
			set {
				if (value) {
					SetNeedsDisplay ();
				} else {
					ClearNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// Sets the area of this view needing to be redrawn to <see cref="Bounds"/>.
		/// </summary>
		public void SetNeedsDisplay ()
		{
			if (!IsInitialized) {
				return;
			}
			SetNeedsDisplay (Bounds);
		}

		/// <summary>
		/// Expands the area of this view needing to be redrawn to include <paramref name="region"/>.
		/// </summary>
		/// <param name="region">The view-relative region that needs to be redrawn.</param>
		public void SetNeedsDisplay (Rect region)
		{
			if (_needsDisplayRect.IsEmpty) {
				_needsDisplayRect = region;
			} else {
				var x = Math.Min (_needsDisplayRect.X, region.X);
				var y = Math.Min (_needsDisplayRect.Y, region.Y);
				var w = Math.Max (_needsDisplayRect.Width, region.Width);
				var h = Math.Max (_needsDisplayRect.Height, region.Height);
				_needsDisplayRect = new Rect (x, y, w, h);
			}
			_superView?.SetSubViewNeedsDisplay ();

			if (_needsDisplayRect.X < Bounds.X ||
				_needsDisplayRect.Y < Bounds.Y ||
				_needsDisplayRect.Width > Bounds.Width ||
				_needsDisplayRect.Height > Bounds.Height) {
				Margin?.SetNeedsDisplay (Margin.Bounds);
				Border?.SetNeedsDisplay (Border.Bounds);
				Padding?.SetNeedsDisplay (Padding.Bounds);
			}

			if (_subviews == null) {
				return;
			}

			foreach (var subview in _subviews) {
				if (subview.Frame.IntersectsWith (region)) {
					var subviewRegion = Rect.Intersect (subview.Frame, region);
					subviewRegion.X -= subview.Frame.X;
					subviewRegion.Y -= subview.Frame.Y;
					subview.SetNeedsDisplay (subviewRegion);
				}
			}
		}

		/// <summary>
		/// Gets whether any Subviews need to be redrawn.
		/// </summary>
		public bool SubViewNeedsDisplay {
			get => _subViewNeedsDisplay;
		}

		bool _subViewNeedsDisplay;

		/// <summary>
		/// Indicates that any Subviews (in the <see cref="Subviews"/> list) need to be repainted.
		/// </summary>
		public void SetSubViewNeedsDisplay ()
		{
			_subViewNeedsDisplay = true;
			if (_superView != null && !_superView._subViewNeedsDisplay) {
				_superView.SetSubViewNeedsDisplay ();
			}
		}

		/// <summary>
		///   Clears the <see cref="Bounds"/> with the normal background color.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This clears the Bounds used by this view.
		///   </para>
		/// </remarks>
		public void Clear () => Clear (ViewToScreen (Bounds));

		// BUGBUG: This version of the Clear API should be removed. We should have a tenet that says 
		// "View APIs only deal with View-relative coords". This is only used by ComboBox which can
		// be refactored to use the View-relative version.
		/// <summary>
		///   Clears the specified screen-relative rectangle with the normal background. 
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="regionScreen">The screen-relative rectangle to clear.</param>
		public void Clear (Rect regionScreen)
		{
			var prev = Driver.SetAttribute (GetNormalColor ());
			Driver.FillRect (regionScreen);
			Driver.SetAttribute (prev);
		}

		// Clips a rectangle in screen coordinates to the dimensions currently available on the screen
		internal Rect ScreenClip (Rect regionScreen)
		{
			var x = regionScreen.X < 0 ? 0 : regionScreen.X;
			var y = regionScreen.Y < 0 ? 0 : regionScreen.Y;
			var w = regionScreen.X + regionScreen.Width >= Driver.Cols ? Driver.Cols - regionScreen.X : regionScreen.Width;
			var h = regionScreen.Y + regionScreen.Height >= Driver.Rows ? Driver.Rows - regionScreen.Y : regionScreen.Height;

			return new Rect (x, y, w, h);
		}

		/// <summary>
		/// Expands the <see cref="ConsoleDriver"/>'s clip region to include <see cref="Bounds"/>.
		/// </summary>
		/// <returns>The current screen-relative clip region, which can be then re-applied by setting <see cref="ConsoleDriver.Clip"/>.</returns>
		/// <remarks>
		/// <para>
		/// If <see cref="ConsoleDriver.Clip"/> and <see cref="Bounds"/> do not intersect, the clip region will be set to <see cref="Rect.Empty"/>.
		/// </para>
		/// </remarks>
		public Rect ClipToBounds ()
		{
			var previous = Driver.Clip;
			Driver.Clip = Rect.Intersect (previous, ViewToScreen (Bounds));
			return previous;
		}

		/// <summary>
		/// Utility function to draw strings that contain a hotkey.
		/// </summary>
		/// <param name="text">String to display, the hotkey specifier before a letter flags the next letter as the hotkey.</param>
		/// <param name="hotColor">Hot color.</param>
		/// <param name="normalColor">Normal color.</param>
		/// <remarks>
		/// <para>The hotkey is any character following the hotkey specifier, which is the underscore ('_') character by default.</para>
		/// <para>The hotkey specifier can be changed via <see cref="HotKeySpecifier"/></para>
		/// </remarks>
		public void DrawHotString (string text, Attribute hotColor, Attribute normalColor)
		{
			var hotkeySpec = HotKeySpecifier == (Rune)0xffff ? (Rune)'_' : HotKeySpecifier;
			Application.Driver.SetAttribute (normalColor);
			foreach (var rune in text) {
				if (rune == hotkeySpec.Value) {
					Application.Driver.SetAttribute (hotColor);
					continue;
				}
				Application.Driver.AddRune ((Rune)rune);
				Application.Driver.SetAttribute (normalColor);
			}
		}

		/// <summary>
		/// Utility function to draw strings that contains a hotkey using a <see cref="ColorScheme"/> and the "focused" state.
		/// </summary>
		/// <param name="text">String to display, the underscore before a letter flags the next letter as the hotkey.</param>
		/// <param name="focused">If set to <see langword="true"/> this uses the focused colors from the color scheme, otherwise the regular ones.</param>
		/// <param name="scheme">The color scheme to use.</param>
		public void DrawHotString (string text, bool focused, ColorScheme scheme)
		{
			if (focused)
				DrawHotString (text, scheme.HotFocus, scheme.Focus);
			else
				DrawHotString (text, Enabled ? scheme.HotNormal : scheme.Disabled, Enabled ? scheme.Normal : scheme.Disabled);
		}

		/// <summary>
		/// This moves the cursor to the specified column and row in the view.
		/// </summary>
		/// <returns>The move.</returns>
		/// <param name="col">The column to move to, in view-relative coordinates.</param>
		/// <param name="row">the row to move to, in view-relative coordinates.</param>
		/// <param name="clipped">Whether to clip the result of the ViewToScreen method,
		///  If  <see langword="true"/>, the <paramref name="col"/> and <paramref name="row"/> values are clamped to the screen (terminal) dimensions (0..TerminalDim-1).</param>
		public void Move (int col, int row, bool clipped = true)
		{
			if (Driver.Rows == 0) {
				return;
			}

			ViewToScreen (col, row, out var rCol, out var rRow, clipped);
			Driver.Move (rCol, rRow);
		}
		/// <summary>
		/// The canvas that any line drawing that is to be shared by subviews of this view should add lines to.
		/// </summary>
		/// <remarks><see cref="Border"/> adds border lines to this LineCanvas.</remarks>
		public LineCanvas LineCanvas { get; } = new LineCanvas ();

		/// <summary>
		/// Gets or sets whether this View will use it's SuperView's <see cref="LineCanvas"/> for
		/// rendering any border lines. If <see langword="true"/> the rendering of any borders drawn
		/// by this Frame will be done by it's parent's SuperView. If <see langword="false"/> (the default)
		/// this View's <see cref="OnDrawFrames()"/> method will be called to render the borders.
		/// </summary>
		public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

		// TODO: Make this cancelable
		/// <summary>
		/// Prepares <see cref="View.LineCanvas"/>. If <see cref="SuperViewRendersLineCanvas"/> is true, only the <see cref="LineCanvas"/> of 
		/// this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is false (the default), this 
		/// method will cause the <see cref="LineCanvas"/> be prepared to be rendered.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnDrawFrames ()
		{
			if (!IsInitialized) {
				return false;
			}

			// Each of these renders lines to either this View's LineCanvas 
			// Those lines will be finally rendered in OnRenderLineCanvas
			Margin?.OnDrawContent (Margin.Bounds);
			Border?.OnDrawContent (Border.Bounds);
			Padding?.OnDrawContent (Padding.Bounds);

			return true;
		}

		/// <summary>
		/// Draws the view. Causes the following virtual methods to be called (along with their related events): 
		/// <see cref="OnDrawContent"/>, <see cref="OnDrawContentComplete"/>.
		/// </summary>
		/// <remarks>
		/// <para>
		///    Always use <see cref="Bounds"/> (view-relative) when calling <see cref="OnDrawContent(Rect)"/>, NOT <see cref="Frame"/> (superview-relative).
		/// </para>
		/// <para>
		///    Views should set the color that they want to use on entry, as otherwise this will inherit
		///    the last color that was set globally on the driver.
		/// </para>
		/// <para>
		///    Overrides of <see cref="OnDrawContent(Rect)"/> must ensure they do not set <c>Driver.Clip</c> to a clip region
		///    larger than the <ref name="Bounds"/> property, as this will cause the driver to clip the entire region.
		/// </para>
		/// </remarks>
		public void Draw ()
		{
			if (!CanBeVisible (this)) {
				return;
			}
			OnDrawFrames ();

			var prevClip = ClipToBounds ();

			if (ColorScheme != null) {
				//Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
				Driver.SetAttribute (GetNormalColor ());
			}

			// Invoke DrawContentEvent
			var dev = new DrawEventArgs (Bounds);
			DrawContent?.Invoke (this, dev);

			if (!dev.Cancel) {
				OnDrawContent (Bounds);
			}

			Driver.Clip = prevClip;

			OnRenderLineCanvas ();
			// Invoke DrawContentCompleteEvent
			OnDrawContentComplete (Bounds);

			// BUGBUG: v2 - We should be able to use View.SetClip here and not have to resort to knowing Driver details.
			ClearLayoutNeeded ();
			ClearNeedsDisplay ();
		}

		// TODO: Make this cancelable
		/// <summary>
		/// Renders <see cref="View.LineCanvas"/>. If <see cref="SuperViewRendersLineCanvas"/> is true, only the <see cref="LineCanvas"/> of 
		/// this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is false (the default), this 
		/// method will cause the <see cref="LineCanvas"/> to be rendered.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnRenderLineCanvas ()
		{
			if (!IsInitialized) {
				return false;
			}

			// If we have a SuperView, it'll render our frames.
			if (!SuperViewRendersLineCanvas && LineCanvas.Bounds != Rect.Empty) {
				foreach (var p in LineCanvas.GetCellMap ()) { // Get the entire map
					Driver.SetAttribute (p.Value.Attribute ?? ColorScheme.Normal);
					Driver.Move (p.Key.X, p.Key.Y);
					// TODO: #2616 - Support combining sequences that don't normalize
					Driver.AddRune (p.Value.Rune);
				}
				LineCanvas.Clear ();
			}

			if (Subviews.Any (s => s.SuperViewRendersLineCanvas)) {
				foreach (var subview in Subviews.Where (s => s.SuperViewRendersLineCanvas == true)) {
					// Combine the LineCanvas'
					LineCanvas.Merge (subview.LineCanvas);
					subview.LineCanvas.Clear ();
				}

				foreach (var p in LineCanvas.GetCellMap ()) { // Get the entire map
					Driver.SetAttribute (p.Value.Attribute ?? ColorScheme.Normal);
					Driver.Move (p.Key.X, p.Key.Y);
					// TODO: #2616 - Support combining sequences that don't normalize
					Driver.AddRune (p.Value.Rune);
				}
				LineCanvas.Clear ();
			}

			return true;
		}

		/// <summary>
		/// Event invoked when the content area of the View is to be drawn.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.
		/// </para>
		/// <para>
		/// Rect provides the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.
		/// </para>
		/// </remarks>
		public event EventHandler<DrawEventArgs> DrawContent;

		/// <summary>
		/// Enables overrides to draw infinitely scrolled content and/or a background behind added controls. 
		/// </summary>
		/// <param name="contentArea">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called before any subviews added with <see cref="Add(View)"/> have been drawn. 
		/// </remarks>
		public virtual void OnDrawContent (Rect contentArea)
		{
			if (NeedsDisplay) {
				if (SuperView != null) {
					Clear (ViewToScreen (Bounds));
				}

				if (!string.IsNullOrEmpty (TextFormatter.Text)) {
					if (TextFormatter != null) {
						TextFormatter.NeedsFormat = true;
					}
				}
				// This should NOT clear 
				TextFormatter?.Draw (ViewToScreen (Bounds), HasFocus ? GetFocusColor () : GetNormalColor (),
					HasFocus ? ColorScheme.HotFocus : GetHotNormalColor (),
					Rect.Empty, false);
				SetSubViewNeedsDisplay ();
			}

			// Draw subviews
			// TODO: Implement OnDrawSubviews (cancelable);
			if (_subviews != null && SubViewNeedsDisplay) {
				var subviewsNeedingDraw = _subviews.Where (
					view => view.Visible &&
						(view.NeedsDisplay ||
						view.SubViewNeedsDisplay ||
						view.LayoutNeeded)
					);

				foreach (var view in subviewsNeedingDraw) {
					//view.Frame.IntersectsWith (bounds)) {
					// && (view.Frame.IntersectsWith (bounds) || bounds.X < 0 || bounds.Y < 0)) {
					if (view.LayoutNeeded) {
						view.LayoutSubviews ();
					}

					// Draw the subview
					// Use the view's bounds (view-relative; Location will always be (0,0)
					//if (view.Visible && view.Frame.Width > 0 && view.Frame.Height > 0) {
					view.Draw ();
					//}
				}
			}
		}

		/// <summary>
		/// Event invoked when the content area of the View is completed drawing.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Will be invoked after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
		/// </para>
		/// <para>
		/// Rect provides the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.
		/// </para>
		/// </remarks>
		public event EventHandler<DrawEventArgs> DrawContentComplete;

		/// <summary>
		/// Enables overrides after completed drawing infinitely scrolled content and/or a background behind removed controls.
		/// </summary>
		/// <param name="contentArea">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
		/// </remarks>
		public virtual void OnDrawContentComplete (Rect contentArea)
		{
			DrawContentComplete?.Invoke (this, new DrawEventArgs (contentArea));
		}

		/// <summary>
		/// Draws a rectangular frame. The frame will be merged (auto-joined) with any other lines drawn by this View 
		/// if <paramref name="mergeWithLineCanvas"/> is true, otherwise will be rendered immediately.
		/// </summary>
		/// <param name="rect">The view relative location and size of the frame.</param>
		/// <param name="lineStyle">The line style.</param>
		/// <param name="attribute">The colors to be used.</param>
		/// <param name="mergeWithLineCanvas">When drawing the frame, allow it to integrate (join) to other frames in other controls.
		/// Or false to simply draw the rect exactly with no side effects.</param>
		public void DrawFrame (Rect rect, LineStyle lineStyle, Attribute? attribute = null, bool mergeWithLineCanvas = true)
		{
			LineCanvas lc;
			if (mergeWithLineCanvas) {
				lc = new LineCanvas ();
			} else {
				lc = LineCanvas;
			}
			var vts = ViewToScreen (rect);
			lc.AddLine (new Point (vts.X, vts.Y), vts.Width,
				Orientation.Horizontal, lineStyle, attribute);
			lc.AddLine (new Point (vts.Right - 1, vts.Y), vts.Height,
				Orientation.Vertical, lineStyle, attribute);
			lc.AddLine (new Point (vts.Right - 1, vts.Bottom - 1), -vts.Width,
				Orientation.Horizontal, lineStyle, attribute);
			lc.AddLine (new Point (vts.X, vts.Bottom - 1), -vts.Height,
				Orientation.Vertical, lineStyle, attribute);

			if (mergeWithLineCanvas) {
				LineCanvas.Merge (lc);
			} else {
				OnRenderLineCanvas ();
			}
		}

		/// <summary>
		/// Draws an incomplete frame. The frame will be merged (auto-joined) with any other lines drawn by this View 
		/// if <paramref name="mergeWithLineCanvas"/> is true, otherwise will be rendered immediately.
		/// The frame is always drawn clockwise. For <see cref="Side.Top"/> and <see cref="Side.Right"/> the end position must
		/// be greater or equal to the start and for <see cref="Side.Left"/> and <see cref="Side.Bottom"/> the end position must
		/// be less or equal to the start.
		/// </summary>
		/// <param name="startPos">The start and side position screen relative.</param>
		/// <param name="endPos">The end and side position screen relative.</param>
		/// <param name="rect">The view relative location and size of the frame.</param>
		/// <param name="lineStyle">The line style.</param>
		/// <param name="attribute">The colors to be used.</param>
		/// <param name="mergeWithLineCanvas">When drawing the frame, allow it to integrate (join) to other frames in other controls.
		/// Or false to simply draw the rect exactly with no side effects.</param>
		public void DrawIncompleteFrame ((int start, Side side) startPos, (int end, Side side) endPos, Rect rect, LineStyle lineStyle, Attribute? attribute = null, bool mergeWithLineCanvas = true)
		{
			var vts = ViewToScreen (rect);
			LineCanvas lc;
			if (mergeWithLineCanvas) {
				lc = new LineCanvas ();
			} else {
				lc = LineCanvas;
			}
			var start = startPos.start;
			var end = endPos.end;
			switch (startPos.side) {
			case Side.Left:
				if (start == vts.Y) {
					lc.AddLine (new Point (vts.X, start), 1,
						Orientation.Vertical, lineStyle, attribute);
				} else {
					if (end <= start && startPos.side == endPos.side) {
						lc.AddLine (new Point (vts.X, start), end - start - 1,
							Orientation.Vertical, lineStyle, attribute);
						break;
					} else {
						lc.AddLine (new Point (vts.X, start), vts.Y - start - 1,
							Orientation.Vertical, lineStyle, attribute);
					}
				}
				switch (endPos.side) {
				case Side.Left:
					lc.AddLine (new Point (vts.X, vts.Y), vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.Right - 1, vts.Y), vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.Right - 1, vts.Bottom - 1), -vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					if (end <= vts.Bottom - 1 && startPos.side == endPos.side) {
						lc.AddLine (new Point (vts.X, vts.Bottom - 1), -(vts.Bottom - end),
							Orientation.Vertical, lineStyle, attribute);
					}
					break;
				case Side.Top:
					lc.AddLine (new Point (vts.X, vts.Y), end,
						Orientation.Horizontal, lineStyle, attribute);
					break;
				case Side.Right:
					lc.AddLine (new Point (vts.X, vts.Y), vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.Right - 1, vts.Y), end + 1,
						Orientation.Vertical, lineStyle, attribute);
					break;
				case Side.Bottom:
					lc.AddLine (new Point (vts.X, vts.Y), vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.Right - 1, vts.Y), vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.Right - 1, vts.Bottom - 1), -end,
						Orientation.Horizontal, lineStyle, attribute);
					break;
				}
				break;

			case Side.Top:
				if (start == vts.Width - 1) {
					lc.AddLine (new Point (vts.X + start, vts.Y), -1,
						Orientation.Horizontal, lineStyle, attribute);
				} else if (end >= start && startPos.side == endPos.side) {
					lc.AddLine (new Point (vts.X + start, vts.Y), end - start + 1,
						Orientation.Horizontal, lineStyle, attribute);
					break;
				} else if (vts.Width - start > 0) {
					lc.AddLine (new Point (vts.X + start, vts.Y), Math.Max (vts.Width - start, 0),
						Orientation.Horizontal, lineStyle, attribute);
				}
				switch (endPos.side) {
				case Side.Left:
					lc.AddLine (new Point (vts.Right - 1, vts.Y), vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.Right - 1, vts.Bottom - 1), -vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), -end,
						Orientation.Vertical, lineStyle, attribute);
					break;
				case Side.Top:
					lc.AddLine (new Point (vts.Right - 1, vts.Y), vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.Right - 1, vts.Bottom - 1), -vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), -vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					if (end >= 0 && startPos.side == endPos.side) {
						lc.AddLine (new Point (vts.X, vts.Y), end + 1,
							Orientation.Horizontal, lineStyle, attribute);
					}
					break;
				case Side.Right:
					lc.AddLine (new Point (vts.Right - 1, vts.Y), end,
						Orientation.Vertical, lineStyle, attribute);
					break;
				case Side.Bottom:
					lc.AddLine (new Point (vts.Right - 1, vts.Y), vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.Right - 1, vts.Bottom - 1), -(vts.Width - end),
						Orientation.Horizontal, lineStyle, attribute);
					break;
				}
				break;
			case Side.Right:
				if (start == vts.Bottom - 1) {
					lc.AddLine (new Point (vts.Width - 1, start), -1,
						Orientation.Vertical, lineStyle, attribute);
				} else {
					if (end >= start && startPos.side == endPos.side) {
						lc.AddLine (new Point (vts.Width - 1, start), end - start + 1,
							Orientation.Vertical, lineStyle, attribute);
						break;
					} else {
						lc.AddLine (new Point (vts.Width - 1, start), vts.Bottom - start,
							Orientation.Vertical, lineStyle, attribute);
					}
				}
				switch (endPos.side) {
				case Side.Left:
					lc.AddLine (new Point (vts.Width - 1, vts.Bottom - 1), -vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), -(vts.Bottom - end),
						Orientation.Vertical, lineStyle, attribute);
					break;
				case Side.Top:
					lc.AddLine (new Point (vts.Width - 1, vts.Bottom - 1), -vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), -vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Y), end,
						Orientation.Horizontal, lineStyle, attribute);
					break;
				case Side.Right:
					lc.AddLine (new Point (vts.Width - 1, vts.Bottom - 1), -vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), -vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Y), vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					if (end >= 0 && end < vts.Bottom - 1 && startPos.side == endPos.side) {
						lc.AddLine (new Point (vts.Width - 1, vts.Y), end + 1,
							Orientation.Vertical, lineStyle, attribute);
					}
					break;
				case Side.Bottom:
					lc.AddLine (new Point (vts.Width - 1, vts.Bottom - 1), -(vts.Width - end),
						Orientation.Horizontal, lineStyle, attribute);
					break;
				}
				break;
			case Side.Bottom:
				if (start == vts.X) {
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), 1,
						Orientation.Horizontal, lineStyle, attribute);
				} else if (end <= start && startPos.side == endPos.side) {
					lc.AddLine (new Point (vts.X + start, vts.Bottom - 1), -(start - end + 1),
						Orientation.Horizontal, lineStyle, attribute);
					break;
				} else {
					lc.AddLine (new Point (vts.X + start, vts.Bottom - 1), -(start + 1),
						Orientation.Horizontal, lineStyle, attribute);
				}
				switch (endPos.side) {
				case Side.Left:
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), -(vts.Bottom - end),
						Orientation.Vertical, lineStyle, attribute);
					break;
				case Side.Top:
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), -vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Y), end + 1,
						Orientation.Horizontal, lineStyle, attribute);
					break;
				case Side.Right:
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), -vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Y), vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.Width - 1, vts.Y), end,
						Orientation.Vertical, lineStyle, attribute);
					break;
				case Side.Bottom:
					lc.AddLine (new Point (vts.X, vts.Bottom - 1), -vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					lc.AddLine (new Point (vts.X, vts.Y), vts.Width,
						Orientation.Horizontal, lineStyle, attribute);
					lc.AddLine (new Point (vts.Width - 1, vts.Y), vts.Height,
						Orientation.Vertical, lineStyle, attribute);
					if (vts.Width - end > 0 && startPos.side == endPos.side) {
						lc.AddLine (new Point (vts.Width - 1, vts.Bottom - 1), -(vts.Width - end),
							Orientation.Horizontal, lineStyle, attribute);
					}
					break;
				}
				break;
			}

			if (mergeWithLineCanvas) {
				LineCanvas.Merge (lc);
			} else {
				OnRenderLineCanvas ();
			}
		}
	}
}