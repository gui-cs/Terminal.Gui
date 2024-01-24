using System;
using System.Linq;
using System.Text;

namespace Terminal.Gui;

public partial class View {
	ColorScheme _colorScheme;

	// The view-relative region that needs to be redrawn. Marked internal for unit tests.
	internal Rect _needsDisplayRect = Rect.Empty;

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
	/// Gets whether any Subviews need to be redrawn.
	/// </summary>
	public bool SubViewNeedsDisplay { get; private set; }

	/// <summary>
	/// The canvas that any line drawing that is to be shared by subviews of this view should add lines to.
	/// </summary>
	/// <remarks><see cref="Border"/> adds border lines to this LineCanvas.</remarks>
	public LineCanvas LineCanvas { get; } = new ();

	/// <summary>
	/// Gets or sets whether this View will use it's SuperView's <see cref="LineCanvas"/> for
	/// rendering any border lines. If <see langword="true"/> the rendering of any borders drawn
	/// by this Frame will be done by it's parent's SuperView. If <see langword="false"/> (the default)
	/// this View's <see cref="OnDrawAdornments"/> method will be called to render the borders.
	/// </summary>
	public virtual bool SuperViewRendersLineCanvas { get; set; } = false;

	/// <summary>
	/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
	/// </summary>
	/// <returns>
	/// <see cref="Terminal.Gui.ColorScheme.Normal"/> if <see cref="Enabled"/> is <see langword="true"/>
	/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
	/// If it's overridden can return other values.
	/// </returns>
	public virtual Attribute GetNormalColor ()
	{
		var cs = ColorScheme;
		if (ColorScheme == null) {
			cs = new ColorScheme ();
		}
		return Enabled ? cs.Normal : cs.Disabled;
	}

	/// <summary>
	/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
	/// </summary>
	/// <returns>
	/// <see cref="Terminal.Gui.ColorScheme.Focus"/> if <see cref="Enabled"/> is <see langword="true"/>
	/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
	/// If it's overridden can return other values.
	/// </returns>
	public virtual Attribute GetFocusColor ()
	{
		var cs = ColorScheme;
		if (ColorScheme == null) {
			cs = new ColorScheme ();
		}
		return Enabled ? cs.Focus : cs.Disabled;
	}

	/// <summary>
	/// Determines the current <see cref="ColorScheme"/> based on the <see cref="Enabled"/> value.
	/// </summary>
	/// <returns>
	/// <see cref="Terminal.Gui.ColorScheme.HotNormal"/> if <see cref="Enabled"/> is <see langword="true"/>
	/// or <see cref="Terminal.Gui.ColorScheme.Disabled"/> if <see cref="Enabled"/> is <see langword="false"/>.
	/// If it's overridden can return other values.
	/// </returns>
	public virtual Attribute GetHotNormalColor ()
	{
		var cs = ColorScheme;
		if (ColorScheme == null) {
			cs = new ColorScheme ();
		}
		return Enabled ? cs.HotNormal : cs.Disabled;
	}

	/// <summary>
	/// Displays the specified character in the specified column and row of the View.
	/// </summary>
	/// <param name="col">Column (view-relative).</param>
	/// <param name="row">Row (view-relative).</param>
	/// <param name="ch">Ch.</param>
	public void AddRune (int col, int row, Rune ch)
	{
		if (row < 0 || col < 0) {
			return;
		}
		if (row > _frame.Height - 1 || col > _frame.Width - 1) {
			return;
		}
		Move (col, row);
		Driver.AddRune (ch);
	}

	/// <summary>
	/// Clears <see cref="NeedsDisplay"/> and <see cref="SubViewNeedsDisplay"/>.
	/// </summary>
	protected void ClearNeedsDisplay ()
	{
		_needsDisplayRect = Rect.Empty;
		SubViewNeedsDisplay = false;
	}

	/// <summary>
	/// Sets the area of this view needing to be redrawn to <see cref="Bounds"/>.
	/// </summary>
	/// <remarks>
	/// If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>),
	/// this method does nothing.
	/// </remarks>
	public void SetNeedsDisplay ()
	{
		if (IsInitialized) {
			SetNeedsDisplay (Bounds);
		}
	}

	/// <summary>
	/// Expands the area of this view needing to be redrawn to include <paramref name="region"/>.
	/// </summary>
	/// <remarks>
	/// If the view has not been initialized (<see cref="IsInitialized"/> is <see langword="false"/>),
	/// the area to be redrawn will be the <paramref name="region"/>.
	/// </remarks>
	/// <param name="region">The Bounds-relative region that needs to be redrawn.</param>
	public void SetNeedsDisplay (Rect region)
	{
		if (!IsInitialized) {
			_needsDisplayRect = region;
			return;
		}
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
	/// Indicates that any Subviews (in the <see cref="Subviews"/> list) need to be repainted.
	/// </summary>
	public void SetSubViewNeedsDisplay ()
	{
		SubViewNeedsDisplay = true;
		if (_superView != null && !_superView.SubViewNeedsDisplay) {
			_superView.SetSubViewNeedsDisplay ();
		}
	}

	/// <summary>
	/// Clears the <see cref="Bounds"/> with the normal background color.
	/// </summary>
	/// <remarks>
	///         <para>
	///         This clears the Bounds used by this view.
	///         </para>
	/// </remarks>
	public void Clear ()
	{
		if (IsInitialized) {
			Clear (BoundsToScreen (Bounds));
		}

	}

	// BUGBUG: This version of the Clear API should be removed. We should have a tenet that says 
	// "View APIs only deal with View-relative coords". This is only used by ComboBox which can
	// be refactored to use the View-relative version.
	/// <summary>
	/// Clears the specified screen-relative rectangle with the normal background.
	/// </summary>
	/// <remarks>
	/// </remarks>
	/// <param name="regionScreen">The screen-relative rectangle to clear.</param>
	public void Clear (Rect regionScreen)
	{
		if (Driver == null) {
			return;
		}
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
	/// <returns>
	/// The current screen-relative clip region, which can be then re-applied by setting
	/// <see cref="ConsoleDriver.Clip"/>.
	/// </returns>
	/// <remarks>
	///         <para>
	///         If <see cref="ConsoleDriver.Clip"/> and <see cref="Bounds"/> do not intersect, the clip region will be set to
	///         <see cref="Rect.Empty"/>.
	///         </para>
	/// </remarks>
	public Rect ClipToBounds ()
	{
		if (Driver == null) {
			return Rect.Empty;
		}
		var previous = Driver.Clip;
		Driver.Clip = Rect.Intersect (previous, BoundsToScreen (Bounds));
		return previous;
	}

	/// <summary>
	/// Utility function to draw strings that contain a hotkey.
	/// </summary>
	/// <param name="text">String to display, the hotkey specifier before a letter flags the next letter as the hotkey.</param>
	/// <param name="hotColor">Hot color.</param>
	/// <param name="normalColor">Normal color.</param>
	/// <remarks>
	///         <para>
	///         The hotkey is any character following the hotkey specifier, which is the underscore ('_') character by
	///         default.
	///         </para>
	///         <para>The hotkey specifier can be changed via <see cref="HotKeySpecifier"/></para>
	/// </remarks>
	public void DrawHotString (string text, Attribute hotColor, Attribute normalColor)
	{
		var hotkeySpec = HotKeySpecifier == (Rune)0xffff ? (Rune)'_' : HotKeySpecifier;
		Application.Driver.SetAttribute (normalColor);
		foreach (var rune in text.EnumerateRunes ()) {
			if (rune == new Rune(hotkeySpec.Value)) {
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
	/// <param name="focused">
	/// If set to <see langword="true"/> this uses the focused colors from the color scheme, otherwise
	/// the regular ones.
	/// </param>
	/// <param name="scheme">The color scheme to use.</param>
	public void DrawHotString (string text, bool focused, ColorScheme scheme)
	{
		if (focused) {
			DrawHotString (text, scheme.HotFocus, scheme.Focus);
		} else {
			DrawHotString (text, Enabled ? scheme.HotNormal : scheme.Disabled, Enabled ? scheme.Normal : scheme.Disabled);
		}
	}

	/// <summary>
	/// This moves the cursor to the specified column and row in the view.
	/// </summary>
	/// <returns>The move.</returns>
	/// <param name="col">The column to move to, in view-relative coordinates.</param>
	/// <param name="row">the row to move to, in view-relative coordinates.</param>
	public void Move (int col, int row)
	{
		if (Driver == null || Driver?.Rows == 0) {
			return;
		}

		BoundsToScreen (col, row, out var rCol, out var rRow, false);
		Driver?.Move (rCol, rRow);
	}

	// TODO: Make this cancelable
	/// <summary>
	/// Prepares <see cref="View.LineCanvas"/>. If <see cref="SuperViewRendersLineCanvas"/> is true, only the
	/// <see cref="LineCanvas"/> of
	/// this view's subviews will be rendered. If <see cref="SuperViewRendersLineCanvas"/> is false (the default), this
	/// method will cause the <see cref="LineCanvas"/> be prepared to be rendered.
	/// </summary>
	/// <returns></returns>
	public virtual bool OnDrawAdornments ()
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
	///         <para>
	///         Always use <see cref="Bounds"/> (view-relative) when calling <see cref="OnDrawContent(Rect)"/>, NOT
	///         <see cref="Frame"/> (superview-relative).
	///         </para>
	///         <para>
	///         Views should set the color that they want to use on entry, as otherwise this will inherit
	///         the last color that was set globally on the driver.
	///         </para>
	///         <para>
	///         Overrides of <see cref="OnDrawContent(Rect)"/> must ensure they do not set <c>Driver.Clip</c> to a clip region
	///         larger than the <ref name="Bounds"/> property, as this will cause the driver to clip the entire region.
	///         </para>
	/// </remarks>
	public void Draw ()
	{
		if (!CanBeVisible (this)) {
			return;
		}
		OnDrawAdornments ();

		var prevClip = ClipToBounds ();

		if (ColorScheme != null) {
			//Driver.SetAttribute (HasFocus ? GetFocusColor () : GetNormalColor ());
			Driver?.SetAttribute (GetNormalColor ());
		}

		// Invoke DrawContentEvent
		var dev = new DrawEventArgs (Bounds);
		DrawContent?.Invoke (this, dev);

		if (!dev.Cancel) {
			OnDrawContent (Bounds);
		}

		if (Driver != null) {
			Driver.Clip = prevClip;
		}

		OnRenderLineCanvas ();
		// Invoke DrawContentCompleteEvent
		OnDrawContentComplete (Bounds);

		// BUGBUG: v2 - We should be able to use View.SetClip here and not have to resort to knowing Driver details.
		ClearLayoutNeeded ();
		ClearNeedsDisplay ();
	}

	// TODO: Make this cancelable
	/// <summary>
	/// Renders <see cref="View.LineCanvas"/>. If <see cref="SuperViewRendersLineCanvas"/> is true, only the
	/// <see cref="LineCanvas"/> of
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
			foreach (var p in LineCanvas.GetCellMap ()) {
				// Get the entire map
				Driver.SetAttribute (p.Value.Attribute ?? ColorScheme.Normal);
				Driver.Move (p.Key.X, p.Key.Y);
				// TODO: #2616 - Support combining sequences that don't normalize
				Driver.AddRune (p.Value.Rune);
			}
			LineCanvas.Clear ();
		}

		if (Subviews.Any (s => s.SuperViewRendersLineCanvas)) {
			foreach (var subview in Subviews.Where (s => s.SuperViewRendersLineCanvas)) {
				// Combine the LineCanvas'
				LineCanvas.Merge (subview.LineCanvas);
				subview.LineCanvas.Clear ();
			}

			foreach (var p in LineCanvas.GetCellMap ()) {
				// Get the entire map
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
	///         <para>
	///         Will be invoked before any subviews added with <see cref="Add(View)"/> have been drawn.
	///         </para>
	///         <para>
	///         Rect provides the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>
	///         .
	///         </para>
	/// </remarks>
	public event EventHandler<DrawEventArgs> DrawContent;

	/// <summary>
	/// Enables overrides to draw infinitely scrolled content and/or a background behind added controls.
	/// </summary>
	/// <param name="contentArea">
	/// The view-relative rectangle describing the currently visible viewport into the
	/// <see cref="View"/>
	/// </param>
	/// <remarks>
	/// This method will be called before any subviews added with <see cref="Add(View)"/> have been drawn.
	/// </remarks>
	public virtual void OnDrawContent (Rect contentArea)
	{
		if (NeedsDisplay) {
			if (SuperView != null) {
				Clear (BoundsToScreen (contentArea));
			}

			if (!string.IsNullOrEmpty (TextFormatter.Text)) {
				if (TextFormatter != null) {
					TextFormatter.NeedsFormat = true;
				}
			}
			// This should NOT clear 
			TextFormatter?.Draw (BoundsToScreen (contentArea), HasFocus ? GetFocusColor () : GetNormalColor (),
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
	///         <para>
	///         Will be invoked after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
	///         </para>
	///         <para>
	///         Rect provides the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>
	///         .
	///         </para>
	/// </remarks>
	public event EventHandler<DrawEventArgs> DrawContentComplete;

	/// <summary>
	/// Enables overrides after completed drawing infinitely scrolled content and/or a background behind removed controls.
	/// </summary>
	/// <param name="contentArea">
	/// The view-relative rectangle describing the currently visible viewport into the
	/// <see cref="View"/>
	/// </param>
	/// <remarks>
	/// This method will be called after any subviews removed with <see cref="Remove(View)"/> have been completed drawing.
	/// </remarks>
	public virtual void OnDrawContentComplete (Rect contentArea) => DrawContentComplete?.Invoke (this, new DrawEventArgs (contentArea));
}