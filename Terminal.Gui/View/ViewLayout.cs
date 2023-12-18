using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Terminal.Gui {
	/// <summary>
	/// Determines the LayoutStyle for a <see cref="View"/>, if Absolute, during <see cref="View.LayoutSubviews"/>, the
	/// value from the <see cref="View.Frame"/> will be used, if the value is Computed, then <see cref="View.Frame"/>
	/// will be updated from the X, Y <see cref="Pos"/> objects and the Width and Height <see cref="Dim"/> objects.
	/// </summary>
	public enum LayoutStyle {
		/// <summary>
		/// The position and size of the view are based <see cref="View.Frame"/>. 
		/// </summary>
		Absolute,

		/// <summary>
		/// The position and size of the view will be computed based on 
		/// <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/>. <see cref="View.Frame"/> will
		/// provide the absolute computed values.
		/// </summary>
		Computed
	}

	public partial class View {

		// The frame for the object. Relative to the SuperView's Bounds.
		Rect _frame;

		/// <summary>
		/// Gets or sets the frame for the view. The frame is relative to the <see cref="SuperView"/>'s <see cref="Bounds"/>.
		/// </summary>
		/// <value>The frame.</value>
		/// <remarks>
		/// <para>
		///    Change the Frame when using the <see cref="Terminal.Gui.LayoutStyle.Absolute"/> layout style to move or resize views. 
		/// </para>
		/// <para>
		///    Altering the Frame of a view will trigger the redrawing of the
		///    view as well as the redrawing of the affected regions of the <see cref="SuperView"/>.
		/// </para>
		/// </remarks>
		public virtual Rect Frame {
			get => _frame;
			set {
				_frame = new Rect (value.X, value.Y, Math.Max (value.Width, 0), Math.Max (value.Height, 0));
				if (IsInitialized || LayoutStyle == LayoutStyle.Absolute) {
					LayoutFrames ();
					TextFormatter.Size = GetTextFormatterSizeNeededForTextAndHotKey ();
					SetNeedsLayout ();
					SetNeedsDisplay ();
				}
			}
		}

		/// <summary>
		/// The frame (specified as a <see cref="Thickness"/>) that separates a View from other SubViews of the same SuperView. 
		/// The margin offsets the <see cref="Bounds"/> from the <see cref="Frame"/>. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// The frames (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the View's content
		/// and are not clipped by the View's Clip Area.
		/// </para>
		/// <para>
		/// Changing the size of a frame (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>)
		/// will change the size of the <see cref="Frame"/> and trigger <see cref="LayoutSubviews"/> to update the layout of the
		/// <see cref="SuperView"/> and its <see cref="Subviews"/>.
		/// </para>
		/// </remarks>
		public Frame Margin { get; private set; }

		/// <summary>
		/// The frame (specified as a <see cref="Thickness"/>) inside of the view that offsets the <see cref="Bounds"/> from the <see cref="Margin"/>. 
		///  The Border provides the space for a visual border (drawn using line-drawing glyphs) and the Title. 
		///  The Border expands inward; in other words if `Border.Thickness.Top == 2` the border and 
		///  title will take up the first row and the second row will be filled with spaces. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// <see cref="BorderStyle"/> provides a simple helper for turning a simple border frame on or off.
		/// </para>
		/// <para>
		/// The frames (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the View's content
		/// and are not clipped by the View's Clip Area.
		/// </para>
		/// <para>
		/// Changing the size of a frame (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>)
		/// will change the size of the <see cref="Frame"/> and trigger <see cref="LayoutSubviews"/> to update the layout of the
		/// <see cref="SuperView"/> and its <see cref="Subviews"/>.
		/// </para>
		/// </remarks>
		public Frame Border { get; private set; }

		/// <summary>
		/// Gets or sets whether the view has a one row/col thick border.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is a helper for manipulating the view's <see cref="Border"/>. Setting this property to any value other than
		/// <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s <see cref="Frame.Thickness"/> 
		/// to `1` and <see cref="BorderStyle"/> to the value. 
		/// </para>
		/// <para>
		/// Setting this property to <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s <see cref="Frame.Thickness"/> 
		/// to `0` and <see cref="BorderStyle"/> to <see cref="LineStyle.None"/>. 
		/// </para>
		/// <para>
		/// For more advanced customization of the view's border, manipulate see <see cref="Border"/> directly.
		/// </para>
		/// </remarks>
		public LineStyle BorderStyle {
			get {
				return Border?.BorderStyle ?? LineStyle.None;
			}
			set {
				if (Border == null) {
					throw new InvalidOperationException ("Border is null; this is likely a bug.");
				}
				if (value != LineStyle.None) {
					Border.Thickness = new Thickness (1);
				} else {
					Border.Thickness = new Thickness (0);
				}
				Border.BorderStyle = value;
				LayoutFrames ();
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// The frame (specified as a <see cref="Thickness"/>) inside of the view that offsets the <see cref="Bounds"/> from the <see cref="Border"/>. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// The frames (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the View's content
		/// and are not clipped by the View's Clip Area.
		/// </para>
		/// <para>
		/// Changing the size of a frame (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>)
		/// will change the size of the <see cref="Frame"/> and trigger <see cref="LayoutSubviews"/> to update the layout of the
		/// <see cref="SuperView"/> and its <see cref="Subviews"/>.
		/// </para>
		/// </remarks>
		public Frame Padding { get; private set; }

		/// <summary>
		/// Helper to get the total thickness of the <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>. 
		/// </summary>
		/// <returns>A thickness that describes the sum of the Frames' thicknesses.</returns>
		public Thickness GetFramesThickness ()
		{
			var left = Margin.Thickness.Left + Border.Thickness.Left + Padding.Thickness.Left;
			var top = Margin.Thickness.Top + Border.Thickness.Top + Padding.Thickness.Top;
			var right = Margin.Thickness.Right + Border.Thickness.Right + Padding.Thickness.Right;
			var bottom = Margin.Thickness.Bottom + Border.Thickness.Bottom + Padding.Thickness.Bottom;
			return new Thickness (left, top, right, bottom);
		}

		/// <summary>
		/// Helper to get the X and Y offset of the Bounds from the Frame. This is the sum of the Left and Top properties of
		/// <see cref="Margin"/>, <see cref="Border"/> and <see cref="Padding"/>.
		/// </summary>
		public Point GetBoundsOffset () => new Point (Padding?.Thickness.GetInside (Padding.Frame).X ?? 0, Padding?.Thickness.GetInside (Padding.Frame).Y ?? 0);

		/// <summary>
		/// Creates the view's <see cref="Frame"/> objects. This internal method is overridden by Frame to do nothing
		/// to prevent recursion during View construction.
		/// </summary>
		internal virtual void CreateFrames ()
		{
			void ThicknessChangedHandler (object sender, EventArgs e)
			{
				LayoutFrames ();
				SetNeedsLayout ();
				SetNeedsDisplay ();
			}

			if (Margin != null) {
				Margin.ThicknessChanged -= ThicknessChangedHandler;
				Margin.Dispose ();
			}
			Margin = new Frame () { Id = "Margin", Thickness = new Thickness (0) };
			Margin.ThicknessChanged += ThicknessChangedHandler;
			Margin.Parent = this;

			if (Border != null) {
				Border.ThicknessChanged -= ThicknessChangedHandler;
				Border.Dispose ();
			}
			Border = new Frame () { Id = "Border", Thickness = new Thickness (0) };
			Border.ThicknessChanged += ThicknessChangedHandler;
			Border.Parent = this;

			// TODO: Create View.AddAdornment

			if (Padding != null) {
				Padding.ThicknessChanged -= ThicknessChangedHandler;
				Padding.Dispose ();
			}
			Padding = new Frame () { Id = "Padding", Thickness = new Thickness (0) };
			Padding.ThicknessChanged += ThicknessChangedHandler;
			Padding.Parent = this;
		}

		LayoutStyle _layoutStyle;

		/// <summary>
		/// Controls how the View's <see cref="Frame"/> is computed during <see cref="LayoutSubviews"/>. If the style is set to
		/// <see cref="LayoutStyle.Absolute"/>, 
		/// LayoutSubviews does not change the <see cref="Frame"/>. If the style is <see cref="LayoutStyle.Computed"/>
		/// the <see cref="Frame"/> is updated using
		/// the <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> properties.
		/// </summary>
		/// <value>The layout style.</value>
		public LayoutStyle LayoutStyle {
			get => _layoutStyle;
			set {
				_layoutStyle = value;
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// The view's content area.
		/// <para>
		/// SubViews are positioned relative to Bounds.
		/// </para>
		/// <para>
		/// Drawing is clipped to Bounds (<see cref="Draw()"/> clips drawing to Bounds.<see cref="Rect.Size">Size</see>).
		/// </para>
		/// <para>
		/// Mouse events are reported relative to Bounds.
		/// </para>
		/// </summary>
		/// <value>The view's content area.</value>
		/// <remarks>
		/// <para>
		/// The <see cref="Rect.Location"/> of Bounds is always (0, 0). To obtain the offset of the Bounds from the Frame use 
		/// <see cref="GetBoundsOffset"/>.
		/// </para>
		/// <para>
		/// When using <see cref="LayoutStyle.Computed"/>, Bounds is not valid until after the view has been initialized (after <see cref="EndInit"/> has been called and <see cref="Initialized"/>
		/// has fired). Accessing this property before the view is initialized is considered an error./>
		/// </para>
		/// </remarks>
		public virtual Rect Bounds {
			get {
#if DEBUG
				if (LayoutStyle == LayoutStyle.Computed && !IsInitialized) {
					Debug.WriteLine ($"WARNING: Bounds is being accessed before the View has been initialized. This is likely a bug. View: {this}");
					Debug.WriteLine ($"The Frame is set before the View has been initialized. So it isn't a bug.Is by design.");
				}
#endif // DEBUG
				//var frameRelativeBounds = Padding?.Thickness.GetInside (Padding.Frame) ?? new Rect (default, Frame.Size);
				var frameRelativeBounds = FrameGetInsideBounds ();
				return new Rect (default, frameRelativeBounds.Size);
			}
			set {
				// BUGBUG: Margin etc.. can be null (if typeof(Frame))
				Frame = new Rect (Frame.Location,
					new Size (
						value.Size.Width + Margin.Thickness.Horizontal + Border.Thickness.Horizontal + Padding.Thickness.Horizontal,
						value.Size.Height + Margin.Thickness.Vertical + Border.Thickness.Vertical + Padding.Thickness.Vertical
						)
					);
			}
		}

		private Rect FrameGetInsideBounds ()
		{
			if (Margin == null || Border == null || Padding == null) {
				return new Rect (default, Frame.Size);
			}
			var width = Math.Max (0, Frame.Size.Width - Margin.Thickness.Horizontal - Border.Thickness.Horizontal - Padding.Thickness.Horizontal);
			var height = Math.Max (0, Frame.Size.Height - Margin.Thickness.Vertical - Border.Thickness.Vertical - Padding.Thickness.Vertical);
			return new Rect (Point.Empty, new Size (width, height));
		}

		// Diagnostics to highlight when X or Y is read before the view has been initialized
		Pos VerifyIsInitialized (Pos pos)
		{
#if DEBUG
			if (LayoutStyle == LayoutStyle.Computed && (!IsInitialized)) {
				Debug.WriteLine ($"WARNING: \"{this}\" has not been initialized; position is indeterminate {pos}. This is likely a bug.");
			}
#endif // DEBUG
			return pos;
		}

		// Diagnostics to highlight when Width or Height is read before the view has been initialized
		Dim VerifyIsInitialized (Dim dim)
		{
#if DEBUG
			if (LayoutStyle == LayoutStyle.Computed && (!IsInitialized)) {
				Debug.WriteLine ($"WARNING: \"{this}\" has not been initialized; dimension is indeterminate: {dim}. This is likely a bug.");
			}
#endif // DEBUG		
			return dim;
		}

		Pos _x, _y;

		/// <summary>
		/// Gets or sets the X position for the view (the column). Only used if the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The X Position.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Pos X {
			get => VerifyIsInitialized (_x);
			set {
				if (ForceValidatePosDim && !ValidatePosDim (_x, value)) {
					throw new ArgumentException ();
				}

				_x = value;

				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets the Y position for the view (the row). Only used if the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The y position (line).</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Pos Y {
			get => VerifyIsInitialized (_y);
			set {
				if (ForceValidatePosDim && !ValidatePosDim (_y, value)) {
					throw new ArgumentException ();
				}

				_y = value;

				OnResizeNeeded ();
			}
		}
		Dim _width, _height;

		/// <summary>
		/// Gets or sets the width of the view. Only used the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The width.</value>
		/// <remarks>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		/// </remarks>
		public Dim Width {
			get => VerifyIsInitialized (_width);
			set {
				if (ForceValidatePosDim && !ValidatePosDim (_width, value)) {
					throw new ArgumentException ("ForceValidatePosDim is enabled", nameof (Width));
				}

				_width = value;

				if (ForceValidatePosDim) {
					var isValidNewAutSize = AutoSize && IsValidAutoSizeWidth (_width);

					if (IsAdded && AutoSize && !isValidNewAutSize) {
						throw new InvalidOperationException ("Must set AutoSize to false before set the Width.");
					}
				}
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Gets or sets the height of the view. Only used the <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Computed"/>.
		/// </summary>
		/// <value>The height.</value>
		/// If <see cref="LayoutStyle"/> is <see cref="Terminal.Gui.LayoutStyle.Absolute"/> changing this property has no effect and its value is indeterminate. 
		public Dim Height {
			get => VerifyIsInitialized (_height);
			set {
				if (ForceValidatePosDim && !ValidatePosDim (_height, value)) {
					throw new ArgumentException ("ForceValidatePosDim is enabled", nameof (Height));
				}

				_height = value;

				if (ForceValidatePosDim) {
					var isValidNewAutSize = AutoSize && IsValidAutoSizeHeight (_height);

					if (IsAdded && AutoSize && !isValidNewAutSize) {
						throw new InvalidOperationException ("Must set AutoSize to false before set the Height.");
					}
				}
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// Forces validation with <see cref="Terminal.Gui.LayoutStyle.Computed"/> layout
		///  to avoid breaking the <see cref="Pos"/> and <see cref="Dim"/> settings.
		/// </summary>
		public bool ForceValidatePosDim { get; set; }

		bool ValidatePosDim (object oldValue, object newValue)
		{
			if (!IsInitialized || _layoutStyle == LayoutStyle.Absolute || oldValue == null || oldValue.GetType () == newValue.GetType () || this is Toplevel) {
				return true;
			}
			if (_layoutStyle == LayoutStyle.Computed) {
				if (oldValue.GetType () != newValue.GetType () && !(newValue is Pos.PosAbsolute || newValue is Dim.DimAbsolute)) {
					return true;
				}
			}
			return false;
		}

		// BUGBUG: This API is broken - should not assume Frame.Height == Bounds.Height
		// BUGBUG: this function does not belong in ViewLayout.cs - it should be in ViewText.cs
		/// <summary>
		/// Gets the minimum dimensions required to fit the View's <see cref="Text"/>, factoring in <see cref="TextDirection"/>.
		/// </summary>
		/// <param name="size">The minimum dimensions required.</param>
		/// <returns><see langword="true"/> if the dimensions fit within the View's <see cref="Bounds"/>, <see langword="false"/> otherwise.</returns>
		/// <remarks>
		/// Always returns <see langword="false"/> if <see cref="AutoSize"/> is <see langword="true"/> or
		/// if <see cref="Height"/> (Horizontal) or <see cref="Width"/> (Vertical) are not not set or zero.
		/// Does not take into account word wrapping.
		/// </remarks>
		bool GetMinimumBoundsForFrame (out Size size)
		{
			if (!IsInitialized) {
				size = new Size (0, 0);
				return false;
			}
			size = Bounds.Size;

			if (!AutoSize && !string.IsNullOrEmpty (TextFormatter.Text)) {
				switch (TextFormatter.IsVerticalDirection (TextDirection)) {
				case true:
					var colWidth = TextFormatter.GetSumMaxCharWidth (new List<string> { TextFormatter.Text }, 0, 1);
					// TODO: v2 - This uses frame.Width; it should only use Bounds
					if (_frame.Width < colWidth &&
						(Width == null ||
							(Bounds.Width >= 0 &&
								Width is Dim.DimAbsolute &&
								Width.Anchor (0) >= 0 &&
								Width.Anchor (0) < colWidth))) {
						size = new Size (colWidth, Bounds.Height);
						return true;
					}
					break;
				default:
					if (_frame.Height < 1 &&
						(Height == null ||
							(Height is Dim.DimAbsolute &&
								Height.Anchor (0) == 0))) {
						size = new Size (Bounds.Width, 1);
						return true;
					}
					break;
				}
			}
			return false;
		}

		// BUGBUG: this function does not belong in ViewLayout.cs - it should be in ViewText.cs
		/// <summary>
		/// Sets the size of the View to the minimum width or height required to fit <see cref="Text"/> (see <see cref="GetMinimumBoundsForFrame"/>.
		/// </summary>
		/// <returns><see langword="true"/> if the size was changed, <see langword="false"/> if <see cref="Text"/>
		/// will not fit.</returns>
		bool SetBoundsToFitFrame ()
		{
			if (GetMinimumBoundsForFrame (out Size size)) {
				_frame = new Rect (_frame.Location, size);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Called whenever the view needs to be resized. Sets <see cref="Frame"/> and
		/// triggers a <see cref="LayoutSubviews()"/> call.
		/// </summary>
		/// <remarks>
		/// Can be overridden if the view resize behavior is different than the default.
		/// </remarks>
		protected virtual void OnResizeNeeded ()
		{
			var actX = _x is Pos.PosAbsolute ? _x.Anchor (0) : _frame.X;
			var actY = _y is Pos.PosAbsolute ? _y.Anchor (0) : _frame.Y;

			if (AutoSize) {
				//if (TextAlignment == TextAlignment.Justified) {
				//	throw new InvalidOperationException ("TextAlignment.Justified cannot be used with AutoSize");
				//}
				var s = GetAutoSize ();
				var w = _width is Dim.DimAbsolute && _width.Anchor (0) > s.Width ? _width.Anchor (0) : s.Width;
				var h = _height is Dim.DimAbsolute && _height.Anchor (0) > s.Height ? _height.Anchor (0) : s.Height;
				_frame = new Rect (new Point (actX, actY), new Size (w, h)); // Set frame, not Frame!
			} else {
				var w = _width is Dim.DimAbsolute ? _width.Anchor (0) : _frame.Width;
				var h = _height is Dim.DimAbsolute ? _height.Anchor (0) : _frame.Height;
				// BUGBUG: v2 - ? - If layoutstyle is absolute, this overwrites the current frame h/w with 0. Hmmm...
				// This is needed for DimAbsolute values by setting the frame before LayoutSubViews.
				_frame = new Rect (new Point (actX, actY), new Size (w, h)); // Set frame, not Frame!
			}
			//// BUGBUG: I think these calls are redundant or should be moved into just the AutoSize case
			if (IsInitialized || LayoutStyle == LayoutStyle.Absolute) {
				SetBoundsToFitFrame ();
				LayoutFrames ();
				TextFormatter.Size = GetTextFormatterSizeNeededForTextAndHotKey ();
				SetNeedsLayout ();
				SetNeedsDisplay ();
			}
		}

		internal bool LayoutNeeded { get; private set; } = true;

		internal void SetNeedsLayout ()
		{
			if (LayoutNeeded) {
				return;
			}
			LayoutNeeded = true;
			foreach (var view in Subviews) {
				view.SetNeedsLayout ();
			}
			TextFormatter.NeedsFormat = true;
			SuperView?.SetNeedsLayout ();
		}

		/// <summary>
		/// Indicates that the view does not need to be laid out.
		/// </summary>
		protected void ClearLayoutNeeded ()
		{
			LayoutNeeded = false;
		}

		/// <summary>
		/// Converts a screen-relative coordinate to a Frame-relative coordinate. Frame-relative means
		/// relative to the View's <see cref="SuperView"/>'s <see cref="Bounds"/>.
		/// </summary>
		/// <returns>The coordinate relative to the <see cref="SuperView"/>'s <see cref="Bounds"/>.</returns>
		/// <param name="x">Screen-relative column.</param>
		/// <param name="y">Screen-relative row.</param>
		public Point ScreenToFrame (int x, int y)
		{
			Point superViewBoundsOffset = SuperView?.GetBoundsOffset () ?? Point.Empty;
			var ret = new Point (x - Frame.X - superViewBoundsOffset.X, y - Frame.Y - superViewBoundsOffset.Y);
			if (SuperView != null) {
				var superFrame = SuperView.ScreenToFrame (x - superViewBoundsOffset.X, y - superViewBoundsOffset.Y);
				ret = new Point (superFrame.X - Frame.X, superFrame.Y - Frame.Y);
			}
			return ret;
		}

		/// <summary>
		/// Converts a screen-relative coordinate to a bounds-relative coordinate. 
		/// </summary>
		/// <returns>The coordinate relative to this view's <see cref="Bounds"/>.</returns>
		/// <param name="x">Screen-relative column.</param>
		/// <param name="y">Screen-relative row.</param>
		public Point ScreenToBounds (int x, int y)
		{
			var screen = ScreenToFrame (x, y);
			var boundsOffset = GetBoundsOffset ();
			return new Point (screen.X - boundsOffset.X, screen.Y - boundsOffset.Y);
		}

		/// <summary>
		/// Converts a <see cref="Bounds"/>-relative coordinate to a screen-relative coordinate. The output is optionally clamped to the screen dimensions.
		/// </summary>
		/// <param name="x"><see cref="Bounds"/>-relative column.</param>
		/// <param name="y"><see cref="Bounds"/>-relative row.</param>
		/// <param name="rx">Absolute column; screen-relative.</param>
		/// <param name="ry">Absolute row; screen-relative.</param>
		/// <param name="clamped">If <see langword="true"/>, <paramref name="rx"/> and <paramref name="ry"/> will be clamped to the 
		/// screen dimensions (will never be negative and will always be less than <see cref="ConsoleDriver.Cols"/> and
		/// <see cref="ConsoleDriver.Rows"/>, respectively.</param>
		public virtual void BoundsToScreen (int x, int y, out int rx, out int ry, bool clamped = true)
		{
			var boundsOffset = GetBoundsOffset ();
			rx = x + Frame.X + boundsOffset.X;
			ry = y + Frame.Y + boundsOffset.Y;

			var super = SuperView;
			while (super != null) {
				boundsOffset = super.GetBoundsOffset ();
				rx += super.Frame.X + boundsOffset.X;
				ry += super.Frame.Y + boundsOffset.Y;
				super = super.SuperView;
			}

			// The following ensures that the cursor is always in the screen boundaries.
			if (clamped) {
				ry = Math.Min (ry, Driver.Rows - 1);
				rx = Math.Min (rx, Driver.Cols - 1);
			}
		}

		/// <summary>
		/// Converts a <see cref="Bounds"/>-relative region to a screen-relative region. 
		/// </summary>
		public Rect BoundsToScreen (Rect region)
		{
			BoundsToScreen (region.X, region.Y, out var x, out var y, clamped: false);
			return new Rect (x, y, region.Width, region.Height);
		}

		/// <summary>
		/// Gets the <see cref="Frame"/> with a screen-relative location. 
		/// </summary>
		/// <returns>The location and size of the view in screen-relative coordinates.</returns>
		public virtual Rect FrameToScreen ()
		{
			var ret = Frame;
			var super = SuperView;
			while (super != null) {
				var boundsOffset = super.GetBoundsOffset ();
				ret.X += super.Frame.X + boundsOffset.X;
				ret.Y += super.Frame.Y + boundsOffset.Y;
				super = super.SuperView;
			}
			return ret;
		}

		/// <summary>
		/// Sets the View's <see cref="Frame"/> to the frame-relative coordinates if its container. The
		/// container size and location are specified by <paramref name="superviewFrame"/> and are relative to the
		/// View's superview.
		/// </summary>
		/// <param name="superviewFrame">The SuperView-relative rectangle describing View's container (nominally the 
		/// same as <c>this.SuperView.Frame</c>).</param>
		internal void SetRelativeLayout (Rect superviewFrame)
		{
			int newX, newW, newY, newH;
			var autosize = Size.Empty;

			if (AutoSize) {
				// Note this is global to this function and used as such within the local functions defined
				// below. In v2 AutoSize will be re-factored to not need to be dealt with in this function.
				autosize = GetAutoSize ();
			}

			// Returns the new dimension (width or height) and location (x or y) for the View given
			//   the superview's Frame.X or Frame.Y
			//   the superview's width or height
			//   the current Pos (View.X or View.Y)
			//   the current Dim (View.Width or View.Height)
			(int newLocation, int newDimension) GetNewLocationAndDimension (bool horiz, int superviewLocation, int superviewDimension, Pos pos, Dim dim, int autosizeDimension)
			{
				int newDimension, newLocation;

				switch (pos) {
				case Pos.PosCenter:
					if (dim == null) {
						newDimension = AutoSize ? autosizeDimension : superviewDimension;
					} else {
						newDimension = dim.Anchor (superviewDimension);
						newDimension = AutoSize && autosizeDimension > newDimension ? autosizeDimension : newDimension;
					}
					newLocation = pos.Anchor (superviewDimension - newDimension);
					break;

				case Pos.PosCombine combine:
					int left, right;
					(left, newDimension) = GetNewLocationAndDimension (horiz, superviewLocation, superviewDimension, combine.left, dim, autosizeDimension);
					(right, newDimension) = GetNewLocationAndDimension (horiz, superviewLocation, superviewDimension, combine.right, dim, autosizeDimension);
					if (combine.add) {
						newLocation = left + right;
					} else {
						newLocation = left - right;
					}
					newDimension = Math.Max (CalculateNewDimension (horiz, dim, newLocation, superviewDimension, autosizeDimension), 0);
					break;

				case Pos.PosAbsolute:
				case Pos.PosAnchorEnd:
				case Pos.PosFactor:
				case Pos.PosFunc:
				case Pos.PosView:
				default:
					newLocation = pos?.Anchor (superviewDimension) ?? 0;
					newDimension = Math.Max (CalculateNewDimension (horiz, dim, newLocation, superviewDimension, autosizeDimension), 0);
					break;
				}
				return (newLocation, newDimension);
			}

			// Recursively calculates the new dimension (width or height) of the given Dim given:
			//   the current location (x or y)
			//   the current dimension (width or height)
			int CalculateNewDimension (bool horiz, Dim d, int location, int dimension, int autosize)
			{
				int newDimension;
				switch (d) {
				case null:
					newDimension = AutoSize ? autosize : dimension;
					break;
				case Dim.DimCombine combine:
					int leftNewDim = CalculateNewDimension (horiz, combine._left, location, dimension, autosize);
					int rightNewDim = CalculateNewDimension (horiz, combine._right, location, dimension, autosize);
					if (combine._add) {
						newDimension = leftNewDim + rightNewDim;
					} else {
						newDimension = leftNewDim - rightNewDim;
					}
					newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;
					break;
					
				case Dim.DimFactor factor when !factor.IsFromRemaining ():
					newDimension = d.Anchor (dimension);
					newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;
					break;
					
				case Dim.DimAutoSize:
					var thickness = GetFramesThickness ();
					if (horiz) {
						var furthestLeft = Subviews.Where (v => v.Frame.X >= 0).Min (v => v.Frame.X);
						var furthestRight = Subviews.Where (v => v.Frame.X >= 0).Max (v => v.Frame.X + v.Frame.Width);
						newDimension = furthestLeft + furthestRight + thickness.Left + thickness.Right;
					} else {
						var furthestTop = Subviews.Where (v => v.Frame.Y >= 0).Min (v => v.Frame.Y);
						var furthestBottom = Subviews.Where (v => v.Frame.Y >= 0).Max (v => v.Frame.Y + v.Frame.Height);
						newDimension = furthestTop + furthestBottom + thickness.Top + thickness.Bottom;
					}
					break;

				case Dim.DimFill:
				default:
					newDimension = Math.Max (d.Anchor (dimension - location), 0);
					newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;
					break;
				}

				return newDimension;
			}

			// horizontal
			(newX, newW) = GetNewLocationAndDimension (true, superviewFrame.X, superviewFrame.Width, _x, _width, autosize.Width);

			// vertical
			(newY, newH) = GetNewLocationAndDimension (false, superviewFrame.Y, superviewFrame.Height, _y, _height, autosize.Height);

			var r = new Rect (newX, newY, newW, newH);
			if (Frame != r) {
				Frame = r;
				// BUGBUG: Why is this AFTER setting Frame? Seems duplicative.
				if (!SetBoundsToFitFrame ()) {
					TextFormatter.Size = GetTextFormatterSizeNeededForTextAndHotKey ();
				}
			}
		}

		/// <summary>
		/// Fired after the View's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public event EventHandler<LayoutEventArgs> LayoutStarted;

		/// <summary>
		/// Raises the <see cref="LayoutStarted"/> event. Called from  <see cref="LayoutSubviews"/> before any subviews have been laid out.
		/// </summary>
		internal virtual void OnLayoutStarted (LayoutEventArgs args)
		{
			LayoutStarted?.Invoke (this, args);
		}

		/// <summary>
		/// Fired after the View's <see cref="LayoutSubviews"/> method has completed. 
		/// </summary>
		/// <remarks>
		/// Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has otherwise changed.
		/// </remarks>
		public event EventHandler<LayoutEventArgs> LayoutComplete;

		/// <summary>
		/// Event called only once when the <see cref="View"/> is being initialized for the first time.
		/// Allows configurations and assignments to be performed before the <see cref="View"/> being shown.
		/// This derived from <see cref="ISupportInitializeNotification"/> to allow notify all the views that are being initialized.
		/// </summary>
		public event EventHandler Initialized;

		/// <summary>
		/// Raises the <see cref="LayoutComplete"/> event. Called from  <see cref="LayoutSubviews"/> before all sub-views have been laid out.
		/// </summary>
		internal virtual void OnLayoutComplete (LayoutEventArgs args)
		{
			LayoutComplete?.Invoke (this, args);
		}

		internal void CollectPos (Pos pos, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
		{
			switch (pos) {
			case Pos.PosView pv:
				// See #2461
				//if (!from.InternalSubviews.Contains (pv.Target)) {
				//	throw new InvalidOperationException ($"View {pv.Target} is not a subview of {from}");
				//}
				if (pv.Target != this) {
					nEdges.Add ((pv.Target, from));
				}
				return;
			case Pos.PosCombine pc:
				CollectPos (pc.left, from, ref nNodes, ref nEdges);
				CollectPos (pc.right, from, ref nNodes, ref nEdges);
				break;
			}
		}

		internal void CollectDim (Dim dim, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
		{
			switch (dim) {
			case Dim.DimView dv:
				// See #2461
				//if (!from.InternalSubviews.Contains (dv.Target)) {
				//	throw new InvalidOperationException ($"View {dv.Target} is not a subview of {from}");
				//}
				if (dv.Target != this) {
					nEdges.Add ((dv.Target, from));
				}
				return;
			case Dim.DimCombine dc:
				CollectDim (dc._left, from, ref nNodes, ref nEdges);
				CollectDim (dc._right, from, ref nNodes, ref nEdges);
				break;
			}
		}

		internal void CollectAll (View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
		{
			foreach (var v in from.InternalSubviews) {
				nNodes.Add (v);
				if (v._layoutStyle != LayoutStyle.Computed) {
					continue;
				}
				CollectPos (v.X, v, ref nNodes, ref nEdges);
				CollectPos (v.Y, v, ref nNodes, ref nEdges);
				CollectDim (v.Width, v, ref nNodes, ref nEdges);
				CollectDim (v.Height, v, ref nNodes, ref nEdges);
			}
		}

		// https://en.wikipedia.org/wiki/Topological_sorting
		internal static List<View> TopologicalSort (View superView, IEnumerable<View> nodes, ICollection<(View From, View To)> edges)
		{
			var result = new List<View> ();

			// Set of all nodes with no incoming edges
			var noEdgeNodes = new HashSet<View> (nodes.Where (n => edges.All (e => !e.To.Equals (n))));

			while (noEdgeNodes.Any ()) {
				//  remove a node n from S
				var n = noEdgeNodes.First ();
				noEdgeNodes.Remove (n);

				// add n to tail of L
				if (n != superView)
					result.Add (n);

				// for each node m with an edge e from n to m do
				foreach (var e in edges.Where (e => e.From.Equals (n)).ToArray ()) {
					var m = e.To;

					// remove edge e from the graph
					edges.Remove (e);

					// if m has no other incoming edges then
					if (edges.All (me => !me.To.Equals (m)) && m != superView) {
						// insert m into S
						noEdgeNodes.Add (m);
					}
				}
			}

			if (edges.Any ()) {
				foreach ((var from, var to) in edges) {
					if (from == to) {
						// if not yet added to the result, add it and remove from edge
						if (result.Find (v => v == from) == null) {
							result.Add (from);
						}
						edges.Remove ((from, to));
					} else if (from.SuperView == to.SuperView) {
						// if 'from' is not yet added to the result, add it
						if (result.Find (v => v == from) == null) {
							result.Add (from);
						}
						// if 'to' is not yet added to the result, add it
						if (result.Find (v => v == to) == null) {
							result.Add (to);
						}
						// remove from edge
						edges.Remove ((from, to));
					} else if (from != superView?.GetTopSuperView (to, from) && !ReferenceEquals (from, to)) {
						if (ReferenceEquals (from.SuperView, to)) {
							throw new InvalidOperationException ($"ComputedLayout for \"{superView}\": \"{to}\" references a SubView (\"{from}\").");
						} else {
							throw new InvalidOperationException ($"ComputedLayout for \"{superView}\": \"{from}\" linked with \"{to}\" was not found. Did you forget to add it to {superView}?");
						}
					}
				}
			}
			// return L (a topologically sorted order)
			return result;
		} // TopologicalSort

		/// <summary>
		/// Overriden by <see cref="Frame"/> to do nothing, as the <see cref="Frame"/> does not have frames.
		/// </summary>
		internal virtual void LayoutFrames ()
		{
			if (Margin == null) return; // CreateFrames() has not been called yet

			if (Margin.Frame.Size != Frame.Size) {
				Margin._frame = new Rect (Point.Empty, Frame.Size);
				Margin.X = 0;
				Margin.Y = 0;
				Margin.Width = Frame.Size.Width;
				Margin.Height = Frame.Size.Height;
				Margin.SetNeedsLayout ();
				Margin.LayoutSubviews ();
				Margin.SetNeedsDisplay ();
			}

			var border = Margin.Thickness.GetInside (Margin.Frame);
			if (border != Border.Frame) {
				Border._frame = new Rect (new Point (border.Location.X, border.Location.Y), border.Size);
				Border.X = border.Location.X;
				Border.Y = border.Location.Y;
				Border.Width = border.Size.Width;
				Border.Height = border.Size.Height;
				Border.SetNeedsLayout ();
				Border.LayoutSubviews ();
				Border.SetNeedsDisplay ();
			}

			var padding = Border.Thickness.GetInside (Border.Frame);
			if (padding != Padding.Frame) {
				Padding._frame = new Rect (new Point (padding.Location.X, padding.Location.Y), padding.Size);
				Padding.X = padding.Location.X;
				Padding.Y = padding.Location.Y;
				Padding.Width = padding.Size.Width;
				Padding.Height = padding.Size.Height;
				Padding.SetNeedsLayout ();
				Padding.LayoutSubviews ();
				Padding.SetNeedsDisplay ();
			}
		}

		/// <summary>
		/// Invoked when a view starts executing or when the dimensions of the view have changed, for example in
		/// response to the container view or terminal resizing.
		/// </summary>
		/// <remarks>
		/// Raises the <see cref="LayoutComplete"/> event) before it returns.
		/// </remarks>
		public virtual void LayoutSubviews ()
		{
			if (!LayoutNeeded) {
				return;
			}

			LayoutFrames ();

			var oldBounds = Bounds;
			OnLayoutStarted (new LayoutEventArgs () { OldBounds = oldBounds });

			TextFormatter.Size = GetTextFormatterSizeNeededForTextAndHotKey ();

			// Sort out the dependencies of the X, Y, Width, Height properties
			var nodes = new HashSet<View> ();
			var edges = new HashSet<(View, View)> ();
			CollectAll (this, ref nodes, ref edges);
			var ordered = View.TopologicalSort (SuperView, nodes, edges);
			foreach (var v in ordered) {
				LayoutSubview (v, new Rect (GetBoundsOffset (), Bounds.Size));
			}

			// If the 'to' is rooted to 'from' and the layoutstyle is Computed it's a special-case.
			// Use LayoutSubview with the Frame of the 'from' 
			if (SuperView != null && GetTopSuperView () != null && LayoutNeeded && edges.Count > 0) {
				foreach ((var from, var to) in edges) {
					LayoutSubview (to, from.Frame);
				}
			}

			LayoutNeeded = false;

			OnLayoutComplete (new LayoutEventArgs () { OldBounds = oldBounds });
		}

		private void LayoutSubview (View v, Rect contentArea)
		{
			if (v.LayoutStyle == LayoutStyle.Computed) {
				v.SetRelativeLayout (contentArea);
			}

			v.LayoutSubviews ();
			v.LayoutNeeded = false;
		}

		bool _autoSize;

		/// <summary>
		/// Gets or sets a flag that determines whether the View will be automatically resized to fit the <see cref="Text"/> 
		/// within <see cref="Bounds"/>
		/// <para>
		/// The default is <see langword="false"/>. Set to <see langword="true"/> to turn on AutoSize. If <see langword="true"/> then
		/// <see cref="Width"/> and <see cref="Height"/> will be used if <see cref="Text"/> can fit; 
		/// if <see cref="Text"/> won't fit the view will be resized as needed.
		/// </para>
		/// <para>
		/// In addition, if <see cref="ForceValidatePosDim"/> is <see langword="true"/> the new values of <see cref="Width"/> and
		/// <see cref="Height"/> must be of the same types of the existing one to avoid breaking the <see cref="Dim"/> settings.
		/// </para>
		/// </summary>
		public virtual bool AutoSize {
			get => _autoSize;
			set {
				var v = ResizeView (value);
				TextFormatter.AutoSize = v;
				if (_autoSize != v) {
					_autoSize = v;
					TextFormatter.NeedsFormat = true;
					UpdateTextFormatterText ();
					OnResizeNeeded ();
				}
			}
		}

		bool ResizeView (bool autoSize)
		{
			if (!autoSize) {
				return false;
			}

			var boundsChanged = true;
			var newFrameSize = GetAutoSize ();
			if (IsInitialized && newFrameSize != Frame.Size) {
				if (ForceValidatePosDim) {
					// BUGBUG: This ain't right, obviously.  We need to figure out how to handle this.
					boundsChanged = ResizeBoundsToFit (newFrameSize);
				} else {
					Height = newFrameSize.Height;
					Width = newFrameSize.Width; 
				}
			}
			// BUGBUG: This call may be redundant
			TextFormatter.Size = GetTextFormatterSizeNeededForTextAndHotKey ();
			return boundsChanged;
		}

		/// <summary>
		/// Resizes the View to fit the specified size. Factors in the HotKey.
		/// </summary>
		/// <param name="size"></param>
		/// <returns>whether the Bounds was changed or not</returns>
		bool ResizeBoundsToFit (Size size)
		{
			var boundsChanged = false;
			var canSizeW = TrySetWidth (size.Width - GetHotKeySpecifierLength (), out var rW);
			var canSizeH = TrySetHeight (size.Height - GetHotKeySpecifierLength (false), out var rH);
			if (canSizeW) {
				boundsChanged = true;
				_width = rW;
			}
			if (canSizeH) {
				boundsChanged = true;
				_height = rH;
			}
			if (boundsChanged) {
				Bounds = new Rect (Bounds.X, Bounds.Y, canSizeW ? rW : Bounds.Width, canSizeH ? rH : Bounds.Height);
			}

			return boundsChanged;
		}

		/// <summary>
		/// Gets the Frame dimensions required to fit <see cref="Text"/> within <see cref="Bounds"/> using the text <see cref="Direction"/> specified by the
		/// <see cref="TextFormatter"/> property and accounting for any <see cref="HotKeySpecifier"/> characters.
		/// </summary>
		/// <returns>The <see cref="Size"/> of the view required to fit the text.</returns>
		public Size GetAutoSize ()
		{
			int x = 0;
			int y = 0;
			if (IsInitialized) {
				x = Bounds.X;
				y = Bounds.Y; 
			}
			var rect = TextFormatter.CalcRect (x, y,TextFormatter.Text, TextFormatter.Direction);
			var newWidth = rect.Size.Width - GetHotKeySpecifierLength () + Margin.Thickness.Horizontal + Border.Thickness.Horizontal + Padding.Thickness.Horizontal;
			var newHeight = rect.Size.Height - GetHotKeySpecifierLength (false) + Margin.Thickness.Vertical + Border.Thickness.Vertical + Padding.Thickness.Vertical;
			return new Size (newWidth, newHeight);
		}

		bool IsValidAutoSize (out Size autoSize)
		{
			var rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
			autoSize = new Size (rect.Size.Width - GetHotKeySpecifierLength (),
			    rect.Size.Height - GetHotKeySpecifierLength (false));
			return !(ForceValidatePosDim && (!(Width is Dim.DimAbsolute) || !(Height is Dim.DimAbsolute))
			    || _frame.Size.Width != rect.Size.Width - GetHotKeySpecifierLength ()
			    || _frame.Size.Height != rect.Size.Height - GetHotKeySpecifierLength (false));
		}

		bool IsValidAutoSizeWidth (Dim width)
		{
			var rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
			var dimValue = width.Anchor (0);
			return !(ForceValidatePosDim && (!(width is Dim.DimAbsolute)) || dimValue != rect.Size.Width
			    - GetHotKeySpecifierLength ());
		}

		bool IsValidAutoSizeHeight (Dim height)
		{
			var rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
			var dimValue = height.Anchor (0);
			return !(ForceValidatePosDim && (!(height is Dim.DimAbsolute)) || dimValue != rect.Size.Height
			    - GetHotKeySpecifierLength (false));
		}

		/// <summary>
		/// Determines if the View's <see cref="Width"/> can be set to a new value.
		/// </summary>
		/// <param name="desiredWidth"></param>
		/// <param name="resultWidth">Contains the width that would result if <see cref="Width"/> were set to <paramref name="desiredWidth"/>"/> </param>
		/// <returns><see langword="true"/> if the View's <see cref="Width"/> can be changed to the specified value. False otherwise.</returns>
		internal bool TrySetWidth (int desiredWidth, out int resultWidth)
		{
			var w = desiredWidth;
			bool canSetWidth;
			switch (Width) {
			case Dim.DimCombine _:
			case Dim.DimView _:
			case Dim.DimFill _:
				// It's a Dim.DimCombine and so can't be assigned. Let it have it's Width anchored.
				w = Width.Anchor (w);
				canSetWidth = !ForceValidatePosDim;
				break;
			case Dim.DimFactor factor:
				// Tries to get the SuperView Width otherwise the view Width.
				var sw = SuperView != null ? SuperView.Frame.Width : w;
				if (factor.IsFromRemaining ()) {
					sw -= Frame.X;
				}
				w = Width.Anchor (sw);
				canSetWidth = !ForceValidatePosDim;
				break;
			default:
				canSetWidth = true;
				break;
			}
			resultWidth = w;

			return canSetWidth;
		}

		/// <summary>
		/// Determines if the View's <see cref="Height"/> can be set to a new value.
		/// </summary>
		/// <param name="desiredHeight"></param>
		/// <param name="resultHeight">Contains the width that would result if <see cref="Height"/> were set to <paramref name="desiredHeight"/>"/> </param>
		/// <returns><see langword="true"/> if the View's <see cref="Height"/> can be changed to the specified value. False otherwise.</returns>
		internal bool TrySetHeight (int desiredHeight, out int resultHeight)
		{
			var h = desiredHeight;
			bool canSetHeight;
			switch (Height) {
			case Dim.DimCombine _:
			case Dim.DimView _:
			case Dim.DimFill _:
				// It's a Dim.DimCombine and so can't be assigned. Let it have it's height anchored.
				h = Height.Anchor (h);
				canSetHeight = !ForceValidatePosDim;
				break;
			case Dim.DimFactor factor:
				// Tries to get the SuperView height otherwise the view height.
				var sh = SuperView != null ? SuperView.Frame.Height : h;
				if (factor.IsFromRemaining ()) {
					sh -= Frame.Y;
				}
				h = Height.Anchor (sh);
				canSetHeight = !ForceValidatePosDim;
				break;
			default:
				canSetHeight = true;
				break;
			}
			resultHeight = h;

			return canSetHeight;
		}

		/// <summary>
		/// Finds which view that belong to the <paramref name="start"/> superview at the provided location.
		/// </summary>
		/// <param name="start">The superview where to look for.</param>
		/// <param name="x">The column location in the superview.</param>
		/// <param name="y">The row location in the superview.</param>
		/// <param name="resx">The found view screen relative column location.</param>
		/// <param name="resy">The found view screen relative row location.</param>
		/// <returns>
		///  The view that was found at the <praramref name="x"/> and <praramref name="y"/> coordinates.
		///  <see langword="null"/> if no view was found.
		/// </returns>
		public static View FindDeepestView (View start, int x, int y, out int resx, out int resy)
		{
			resy = resx = 0;
			if (start == null || !start.Frame.Contains (x, y)) {
				return null;
			}
			
			var startFrame = start.Frame;
			if (start.InternalSubviews != null) {
				int count = start.InternalSubviews.Count;
				if (count > 0) {
					var boundsOffset = start.GetBoundsOffset ();
					var rx = x - (startFrame.X + boundsOffset.X);
					var ry = y - (startFrame.Y + boundsOffset.Y);
					for (int i = count - 1; i >= 0; i--) {
						View v = start.InternalSubviews [i];
						if (v.Visible && v.Frame.Contains (rx, ry)) {
							var deep = FindDeepestView (v, rx, ry, out resx, out resy);
							if (deep == null)
								return v;
							return deep;
						}
					}
				}
			}
			resx = x - startFrame.X;
			resy = y - startFrame.Y;
			return start;
		}
	}
}
