﻿using System;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <remarks>
	/// <para>
	///    Views have a <see cref="ColorScheme"/> property that defines the default colors that subviews
	///    should use for rendering. This ensures that the views fit in the context where
	///    they are being used, and allows for themes to be plugged in. For example, the
	///    default colors for windows and toplevels uses a blue background, while it uses
	///    a white background for dialog boxes and a red background for errors.
	/// </para>
	/// <para>
	///    Subclasses should not rely on <see cref="ColorScheme"/> being
	///    set at construction time. If a <see cref="ColorScheme"/> is not set on a view, the view will inherit the
	///    value from its <see cref="SuperView"/> and the value might only be valid once a view has been
	///    added to a SuperView. 
	/// </para>
	/// <para>
	///    By using  <see cref="ColorScheme"/> applications will work both
	///    in color as well as black and white displays.
	/// </para>
	/// </remarks>
	public partial class View {

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
			return Enabled ? ColorScheme.Normal : ColorScheme.Disabled;
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
		/// Removes the <see cref="_needsDisplay"/> and the <see cref="_childNeedsDisplay"/> setting on this view.
		/// </summary>
		protected void ClearNeedsDisplay ()
		{
			_needsDisplay = Rect.Empty;
			_childNeedsDisplay = false;
		}

		// The view-relative region that needs to be redrawn
		internal Rect _needsDisplay { get; private set; } = Rect.Empty;

		/// <summary>
		/// Sets a flag indicating this view needs to be redisplayed because its state has changed.
		/// </summary>
		public void SetNeedsDisplay ()
		{
			if (!IsInitialized) return;
			SetNeedsDisplay (Bounds);
		}

		/// <summary>
		/// Flags the view-relative region on this View as needing to be redrawn.
		/// </summary>
		/// <param name="region">The view-relative region that needs to be redrawn.</param>
		public void SetNeedsDisplay (Rect region)
		{
			if (_needsDisplay.IsEmpty)
				_needsDisplay = region;
			else {
				var x = Math.Min (_needsDisplay.X, region.X);
				var y = Math.Min (_needsDisplay.Y, region.Y);
				var w = Math.Max (_needsDisplay.Width, region.Width);
				var h = Math.Max (_needsDisplay.Height, region.Height);
				_needsDisplay = new Rect (x, y, w, h);
			}
			_superView?.SetSubViewNeedsDisplay ();

			if (_subviews == null)
				return;

			foreach (var view in _subviews)
				if (view.Frame.IntersectsWith (region)) {
					var childRegion = Rect.Intersect (view.Frame, region);
					childRegion.X -= view.Frame.X;
					childRegion.Y -= view.Frame.Y;
					view.SetNeedsDisplay (childRegion);
				}
		}

		internal bool _childNeedsDisplay { get; private set; }

		/// <summary>
		/// Indicates that any Subviews (in the <see cref="Subviews"/> list) need to be repainted.
		/// </summary>
		public void SetSubViewNeedsDisplay ()
		{
			if (_childNeedsDisplay) {
				return;
			}
			_childNeedsDisplay = true;
			if (_superView != null && !_superView._childNeedsDisplay)
				_superView.SetSubViewNeedsDisplay ();
		}

		/// <summary>
		///   Clears the view region with the current color.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This clears the entire region used by this view.
		///   </para>
		/// </remarks>
		public void Clear ()
		{
			var h = Frame.Height;
			var w = Frame.Width;
			for (var line = 0; line < h; line++) {
				Move (0, line);
				for (var col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
		}

		// BUGBUG: Stupid that this takes screen-relative. We should have a tenet that says 
		// "View APIs only deal with View-relative coords". 
		/// <summary>
		///   Clears the specified region with the current color. 
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="regionScreen">The screen-relative region to clear.</param>
		public void Clear (Rect regionScreen)
		{
			var h = regionScreen.Height;
			var w = regionScreen.Width;
			for (var line = regionScreen.Y; line < regionScreen.Y + h; line++) {
				Driver.Move (regionScreen.X, line);
				for (var col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
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
		/// Sets the <see cref="ConsoleDriver"/>'s clip region to <see cref="Bounds"/>.
		/// </summary>
		/// <returns>The current screen-relative clip region, which can be then re-applied by setting <see cref="ConsoleDriver.Clip"/>.</returns>
		/// <remarks>
		/// <para>
		/// <see cref="Bounds"/> is View-relative.
		/// </para>
		/// <para>
		/// If <see cref="ConsoleDriver.Clip"/> and <see cref="Bounds"/> do not intersect, the clip region will be set to <see cref="Rect.Empty"/>.
		/// </para>
		/// </remarks>
		public Rect ClipToBounds ()
		{
			var clip = Bounds;

			return SetClip (clip);
		}

		// BUGBUG: v2 - SetClip should return VIEW-relative so that it can be used to reset it; using Driver.Clip directly should not be necessary. 
		/// <summary>
		/// Sets the clip region to the specified view-relative region.
		/// </summary>
		/// <returns>The current screen-relative clip region, which can be then re-applied by setting <see cref="ConsoleDriver.Clip"/>.</returns>
		/// <param name="region">View-relative clip region.</param>
		/// <remarks>
		/// If <see cref="ConsoleDriver.Clip"/> and <paramref name="region"/> do not intersect, the clip region will be set to <see cref="Rect.Empty"/>.
		/// </remarks>
		public Rect SetClip (Rect region)
		{
			var previous = Driver.Clip;
			Driver.Clip = Rect.Intersect (previous, ViewToScreen (region));
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
		public void DrawHotString (ustring text, Attribute hotColor, Attribute normalColor)
		{
			var hotkeySpec = HotKeySpecifier == (Rune)0xffff ? (Rune)'_' : HotKeySpecifier;
			Application.Driver.SetAttribute (normalColor);
			foreach (var rune in text) {
				if (rune == hotkeySpec) {
					Application.Driver.SetAttribute (hotColor);
					continue;
				}
				Application.Driver.AddRune (rune);
				Application.Driver.SetAttribute (normalColor);
			}
		}

		/// <summary>
		/// Utility function to draw strings that contains a hotkey using a <see cref="ColorScheme"/> and the "focused" state.
		/// </summary>
		/// <param name="text">String to display, the underscore before a letter flags the next letter as the hotkey.</param>
		/// <param name="focused">If set to <see langword="true"/> this uses the focused colors from the color scheme, otherwise the regular ones.</param>
		/// <param name="scheme">The color scheme to use.</param>
		public void DrawHotString (ustring text, bool focused, ColorScheme scheme)
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
		public virtual LineCanvas LineCanvas { get; set; } = new LineCanvas ();

		/// <summary>
		/// Gets or sets whether this View will use it's SuperView's <see cref="LineCanvas"/> for
		/// rendering any border lines. If <see langword="true"/> the rendering of any borders drawn
		/// by this Frame will be done by it's parent's SuperView. If <see langword="false"/> (the default)
		/// this View's <see cref="OnDrawFrames()"/> method will be called to render the borders.
		/// </summary>
		public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

		// TODO: Make this cancelable
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public virtual bool OnDrawFrames ()
		{
			if (!IsInitialized) {
				return false;
			}

			var prevClip = Driver.Clip;
			Driver.Clip = ViewToScreen (Frame);

			// TODO: Figure out what we should do if we have no superview
			//if (SuperView != null) {
			// TODO: Clipping is disabled for now to ensure we see errors
			Driver.Clip = new Rect (0, 0, Driver.Cols, Driver.Rows);// screenBounds;// SuperView.ClipToBounds ();
										//}

			// Each of these renders lines to either this View's LineCanvas 
			// Those lines will be finally rendered in OnRenderLineCanvas
			Margin?.Redraw (Margin.Frame);
			Border?.Redraw (Border.Frame);
			Padding?.Redraw (Padding.Frame);

			Driver.Clip = prevClip;

			return true;
		}

		/// <summary>
		/// Redraws this view and its subviews; only redraws the views that have been flagged for a re-display.
		/// </summary>
		/// <param name="bounds">The bounds (view-relative region) to redraw.</param>
		/// <remarks>
		/// <para>
		///    Always use <see cref="Bounds"/> (view-relative) when calling <see cref="Redraw(Rect)"/>, NOT <see cref="Frame"/> (superview-relative).
		/// </para>
		/// <para>
		///    Views should set the color that they want to use on entry, as otherwise this will inherit
		///    the last color that was set globally on the driver.
		/// </para>
		/// <para>
		///    Overrides of <see cref="Redraw"/> must ensure they do not set <c>Driver.Clip</c> to a clip region
		///    larger than the <ref name="bounds"/> parameter, as this will cause the driver to clip the entire region.
		/// </para>
		/// </remarks>
		public virtual void Redraw (Rect bounds)
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

			if (SuperView != null) {
				Clear (ViewToScreen (bounds));
			}

			// Invoke DrawContentEvent
			OnDrawContent (bounds);

			// Draw subviews
			// TODO: Implement OnDrawSubviews (cancelable);
			if (_subviews != null) {
				foreach (var view in _subviews) {
					if (view.Visible) { //!view._needsDisplay.IsEmpty || view._childNeedsDisplay || view.LayoutNeeded) {
						if (true) { //view.Frame.IntersectsWith (bounds)) { // && (view.Frame.IntersectsWith (bounds) || bounds.X < 0 || bounds.Y < 0)) {
							if (view.LayoutNeeded) {
								view.LayoutSubviews ();
							}

							// Draw the subview
							// Use the view's bounds (view-relative; Location will always be (0,0)
							//if (view.Visible && view.Frame.Width > 0 && view.Frame.Height > 0) {
							view.Redraw (view.Bounds);
							//}
						}
						view.ClearNeedsDisplay ();
					}
				}
			}

			// Invoke DrawContentCompleteEvent
			OnDrawContentComplete (bounds);
			Driver.Clip = prevClip;

			OnRenderLineCanvas ();

			// BUGBUG: v2 - We should be able to use View.SetClip here and not have to resort to knowing Driver details.
			ClearLayoutNeeded ();
			ClearNeedsDisplay ();
		}

		internal void OnRenderLineCanvas ()
		{
			//Driver.SetAttribute (new Attribute(Color.White, Color.Black));

			// If we have a SuperView, it'll render our frames.
			if (!SuperViewRendersLineCanvas && LineCanvas.Bounds != Rect.Empty) {
				foreach (var p in LineCanvas.GetCellMap ()) { // Get the entire map
					Driver.SetAttribute (p.Value.Attribute?.Value ?? ColorScheme.Normal);
					Driver.Move (p.Key.X, p.Key.Y);
					Driver.AddRune (p.Value.Rune.Value);
				}
				LineCanvas.Clear ();
			}

			if (Subviews.Select (s => s.SuperViewRendersLineCanvas).Count () > 0) {
				foreach (var subview in Subviews.Where (s => s.SuperViewRendersLineCanvas)) {
					// Combine the LineCavas'
					LineCanvas.Merge (subview.LineCanvas);
					subview.LineCanvas.Clear ();
				}

				foreach (var p in LineCanvas.GetCellMap ()) { // Get the entire map
					Driver.SetAttribute (p.Value.Attribute?.Value ?? ColorScheme.Normal);
					Driver.Move (p.Key.X, p.Key.Y);
					Driver.AddRune (p.Value.Rune.Value);
				}
				LineCanvas.Clear ();
			}
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
			// TODO: Make DrawContent a cancelable event
			// if (!DrawContent?.Invoke(this, new DrawEventArgs (viewport)) {
			DrawContent?.Invoke (this, new DrawEventArgs (contentArea));

			if (!ustring.IsNullOrEmpty (TextFormatter.Text)) {
				if (TextFormatter != null) {
					TextFormatter.NeedsFormat = true;
				}
				// This should NOT clear 
				TextFormatter?.Draw (ViewToScreen (contentArea), HasFocus ? GetFocusColor () : GetNormalColor (),
				    HasFocus ? ColorScheme.HotFocus : GetHotNormalColor (),
				    Rect.Empty, false);
				SetSubViewNeedsDisplay ();
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
		/// <param name="viewport">The view-relative rectangle describing the currently visible viewport into the <see cref="View"/></param>
		/// <remarks>
		/// This method will be called after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
		/// </remarks>
		public virtual void OnDrawContentComplete (Rect viewport)
		{
			DrawContentComplete?.Invoke (this, new DrawEventArgs (viewport));
		}

	}
}