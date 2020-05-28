//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Pending:
//   - Check for NeedDisplay on the hierarchy and repaint
//   - Layout support
//   - "Colors" type or "Attributes" type?
//   - What to surface as "BackgroundCOlor" when clearing a window, an attribute or colors?
//
// Optimziations
//   - Add rendering limitation to the exposed area
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NStack;

namespace Terminal.Gui {

	/// <summary>
	/// Determines the LayoutStyle for a view, if Absolute, during LayoutSubviews, the
	/// value from the Frame will be used, if the value is Computer, then the Frame
	/// will be updated from the X, Y Pos objects and the Width and Height Dim objects.
	/// </summary>
	public enum LayoutStyle {
		/// <summary>
		/// The position and size of the view are based on the Frame value.
		/// </summary>
		Absolute,

		/// <summary>
		/// The position and size of the view will be computed based on the
		/// X, Y, Width and Height properties and set on the Frame.
		/// </summary>
		Computed
	}

	/// <summary>
	/// View is the base class for all views on the screen and represents a visible element that can render itself and contains zero or more nested views.
	/// </summary>
	/// <remarks>
	/// <para>
	///    The View defines the base functionality for user interface elements in Terminal/gui.cs.  Views
	///    can contain one or more subviews, can respond to user input and render themselves on the screen.
	/// </para>
	/// <para>
	///    Views can either be created with an absolute position, by calling the constructor that takes a
	///    Rect parameter to specify the absolute position and size (the Frame of the View) or by setting the
	///    X, Y, Width and Height properties on the view.    Both approaches use coordinates that are relative
	///    to the container they are being added to.
	/// </para>
	/// <para>
	///    When you do not specify a Rect frame you can use the more flexible
	///    Dim and Pos objects that can dynamically update the position of a view.
	///    The X and Y properties are of type <see cref="Pos"/>
	///    and you can use either absolute positions, percentages or anchor
	///    points.   The Width and Height properties are of type
	///    <see cref="Dim"/> and can use absolute position,
	///    percentages and anchors.  These are useful as they will take
	///    care of repositioning your views if your view's frames are resized
	///    or if the terminal size changes.
	/// </para>
	/// <para>
	///    When you specify the Rect parameter to a view, you are setting the LayoutStyle to Absolute, and the
	///    view will always stay in the position that you placed it.   To change the position change the
	///    Frame property to the new position.
	/// </para>
	/// <para>
	///    Subviews can be added to a View by calling the Add method.   The container of a view is the
	///    Superview.
	/// </para>
	/// <para>
	///    Developers can call the SetNeedsDisplay method on the view to flag a region or the entire view
	///    as requiring to be redrawn.
	/// </para>
	/// <para>
	///    Views have a ColorScheme property that defines the default colors that subviews
	///    should use for rendering.   This ensures that the views fit in the context where
	///    they are being used, and allows for themes to be plugged in.   For example, the
	///    default colors for windows and toplevels uses a blue background, while it uses
	///    a white background for dialog boxes and a red background for errors.
	/// </para>
	/// <para>
	///    If a ColorScheme is not set on a view, the result of the ColorScheme is the
	///    value of the SuperView and the value might only be valid once a view has been
	///    added to a SuperView, so your subclasses should not rely on ColorScheme being
	///    set at construction time.
	/// </para>
	/// <para>
	///    Using ColorSchemes has the advantage that your application will work both
	///    in color as well as black and white displays.
	/// </para>
	/// <para>
	///    Views that are focusable should implement the PositionCursor to make sure that
	///    the cursor is placed in a location that makes sense.   Unix terminals do not have
	///    a way of hiding the cursor, so it can be distracting to have the cursor left at
	///    the last focused view.   So views should make sure that they place the cursor
	///    in a visually sensible place.
	/// </para>
	/// <para>
	///    The metnod LayoutSubviews is invoked when the size or layout of a view has
	///    changed.   The default processing system will keep the size and dimensions
	///    for views that use the LayoutKind.Absolute, and will recompute the
	///    frames for the vies that use LayoutKind.Computed.
	/// </para>
	/// </remarks>
	public class View : Responder, IEnumerable {
		internal enum Direction {
			Forward,
			Backward
		}

		View container = null;
		View focused = null;
		Direction focusDirection;

		/// <summary>
		/// Event fired when the view get focus.
		/// </summary>
		public event EventHandler Enter;

		/// <summary>
		/// Event fired when the view lost focus.
		/// </summary>
		public event EventHandler Leave;

		/// <summary>
		/// Event fired when the view receives the mouse event for the first time.
		/// </summary>
		public event EventHandler<MouseEvent> MouseEnter;

		/// <summary>
		/// Event fired when the view loses mouse event for the last time.
		/// </summary>
		public event EventHandler<MouseEvent> MouseLeave;

		internal Direction FocusDirection {
			get => SuperView?.FocusDirection ?? focusDirection;
			set {
				if (SuperView != null)
					SuperView.FocusDirection = value;
				else
					focusDirection = value;
			}
		}

		/// <summary>
		/// Points to the current driver in use by the view, it is a convenience property
		/// for simplifying the development of new views.
		/// </summary>
		public static ConsoleDriver Driver { get { return Application.Driver; } }

		static IList<View> empty = new List<View> (0).AsReadOnly ();

		// This is null, and allocated on demand.
		List<View> subviews;

		/// <summary>
		/// This returns a list of the subviews contained by this view.
		/// </summary>
		/// <value>The subviews.</value>
		public IList<View> Subviews => subviews == null ? empty : subviews.AsReadOnly ();

		// Internally, we use InternalSubviews rather than subviews, as we do not expect us
		// to make the same mistakes our users make when they poke at the Subviews.
		internal IList<View> InternalSubviews => subviews ?? empty;

		internal Rect NeedDisplay { get; private set; } = Rect.Empty;

		// The frame for the object
		Rect frame;

		/// <summary>
		/// Gets or sets an identifier for the view;
		/// </summary>
		/// <value>The identifier.</value>
		public ustring Id { get; set; } = "";

		/// <summary>
		/// Returns a value indicating if this View is currently on Top (Active)
		/// </summary>
		public bool IsCurrentTop {
			get {
				return Application.Current == this;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> want mouse position reports.
		/// </summary>
		/// <value><c>true</c> if want mouse position reports; otherwise, <c>false</c>.</value>
		public virtual bool WantMousePositionReports { get; set; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="View"/> want continuous button pressed event.
		/// </summary>
		public virtual bool WantContinuousButtonPressed { get; set; } = false;
		/// <summary>
		/// Gets or sets the frame for the view.
		/// </summary>
		/// <value>The frame.</value>
		/// <remarks>
		///    Altering the Frame of a view will trigger the redrawing of the
		///    view as well as the redrawing of the affected regions in the superview.
		/// </remarks>
		public virtual Rect Frame {
			get => frame;
			set {
				if (SuperView != null) {
					SuperView.SetNeedsDisplay (frame);
					SuperView.SetNeedsDisplay (value);
				}
				frame = value;

				SetNeedsLayout ();
				SetNeedsDisplay (frame);
			}
		}

		/// <summary>
		/// Gets an enumerator that enumerates the subviews in this view.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator GetEnumerator ()
		{
			foreach (var v in InternalSubviews)
				yield return v;
		}

		LayoutStyle layoutStyle;

		/// <summary>
		/// Controls how the view's Frame is computed during the LayoutSubviews method, if Absolute, then
		/// LayoutSubviews does not change the Frame properties, otherwise the Frame is updated from the
		/// values in X, Y, Width and Height properties.
		/// </summary>
		/// <value>The layout style.</value>
		public LayoutStyle LayoutStyle {
			get => layoutStyle;
			set {
				layoutStyle = value;
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// The bounds represent the View-relative rectangle used for this view.   Updates to the Bounds update the Frame, and has the same side effects as updating the frame.
		/// </summary>
		/// <value>The bounds.</value>
		public Rect Bounds {
			get => new Rect (Point.Empty, Frame.Size);
			set {
				Frame = new Rect (frame.Location, value.Size);
			}
		}

		Pos x, y;
		/// <summary>
		/// Gets or sets the X position for the view (the column).  This is only used when the LayoutStyle is Computed, if the
		/// LayoutStyle is set to Absolute, this value is ignored.
		/// </summary>
		/// <value>The X Position.</value>
		public Pos X {
			get => x;
			set {
				x = value;
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// Gets or sets the Y position for the view (line).  This is only used when the LayoutStyle is Computed, if the
		/// LayoutStyle is set to Absolute, this value is ignored.
		/// </summary>
		/// <value>The y position (line).</value>
		public Pos Y {
			get => y;
			set {
				y = value;
				SetNeedsLayout ();
			}
		}

		Dim width, height;

		/// <summary>
		/// Gets or sets the width for the view. This is only used when the LayoutStyle is Computed, if the
		/// LayoutStyle is set to Absolute, this value is ignored.
		/// </summary>
		/// <value>The width.</value>
		public Dim Width {
			get => width;
			set {
				width = value;
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// Gets or sets the height for the view. This is only used when the LayoutStyle is Computed, if the
		/// LayoutStyle is set to Absolute, this value is ignored.
		/// </summary>
		/// <value>The height.</value>
		public Dim Height {
			get => height;
			set {
				height = value;
				SetNeedsLayout ();
			}
		}

		/// <summary>
		/// Returns the container for this view, or null if this view has not been added to a container.
		/// </summary>
		/// <value>The super view.</value>
		public View SuperView => container;

		/// <summary>
		/// Initializes a new instance of the <see cref="View"/> class with the absolute
		/// dimensions specified in the frame.   If you want to have Views that can be positioned with
		/// Pos and Dim properties on X, Y, Width and Height, use the empty constructor.
		/// </summary>
		/// <param name="frame">The region covered by this view.</param>
		public View (Rect frame)
		{
			this.Frame = frame;
			CanFocus = false;
			LayoutStyle = LayoutStyle.Absolute;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="View"/> class and sets the
		/// view up for Computed layout, which will use the values in X, Y, Width and Height to
		/// compute the View's Frame.
		/// </summary>
		public View ()
		{
			CanFocus = false;
			LayoutStyle = LayoutStyle.Computed;
		}

		/// <summary>
		/// Invoke to flag that this view needs to be redisplayed, by any code
		/// that alters the state of the view.
		/// </summary>
		public void SetNeedsDisplay ()
		{
			SetNeedsDisplay (Bounds);
		}

		internal bool layoutNeeded = true;

		internal void SetNeedsLayout ()
		{
			if (layoutNeeded)
				return;
			layoutNeeded = true;
			if (SuperView == null)
				return;
			SuperView.SetNeedsLayout ();
		}

		/// <summary>
		/// Flags the specified rectangle region on this view as needing to be repainted.
		/// </summary>
		/// <param name="region">The region that must be flagged for repaint.</param>
		public void SetNeedsDisplay (Rect region)
		{
			if (NeedDisplay == null || NeedDisplay.IsEmpty)
				NeedDisplay = region;
			else {
				var x = Math.Min (NeedDisplay.X, region.X);
				var y = Math.Min (NeedDisplay.Y, region.Y);
				var w = Math.Max (NeedDisplay.Width, region.Width);
				var h = Math.Max (NeedDisplay.Height, region.Height);
				NeedDisplay = new Rect (x, y, w, h);
			}
			if (container != null)
				container.ChildNeedsDisplay ();
			if (subviews == null)
				return;
			foreach (var view in subviews)
				if (view.Frame.IntersectsWith (region)) {
					var childRegion = Rect.Intersect (view.Frame, region);
					childRegion.X -= view.Frame.X;
					childRegion.Y -= view.Frame.Y;
					view.SetNeedsDisplay (childRegion);
				}
		}

		internal bool childNeedsDisplay;

		/// <summary>
		/// Flags this view for requiring the children views to be repainted.
		/// </summary>
		public void ChildNeedsDisplay ()
		{
			childNeedsDisplay = true;
			if (container != null)
				container.ChildNeedsDisplay ();
		}

		/// <summary>
		///   Adds a subview to this view.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Add (View view)
		{
			if (view == null)
				return;
			if (subviews == null)
				subviews = new List<View> ();
			subviews.Add (view);
			view.container = this;
			if (view.CanFocus)
				CanFocus = true;
			SetNeedsLayout ();
			SetNeedsDisplay ();
		}

		/// <summary>
		/// Adds the specified views to the view.
		/// </summary>
		/// <param name="views">Array of one or more views (can be optional parameter).</param>
		public void Add (params View [] views)
		{
			if (views == null)
				return;
			foreach (var view in views)
				Add (view);
		}

		/// <summary>
		///   Removes all the widgets from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void RemoveAll ()
		{
			if (subviews == null)
				return;

			while (subviews.Count > 0) {
				Remove (subviews [0]);
			}
		}

		/// <summary>
		///   Removes a widget from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Remove (View view)
		{
			if (view == null || subviews == null)
				return;

			SetNeedsLayout ();
			SetNeedsDisplay ();
			var touched = view.Frame;
			subviews.Remove (view);
			view.container = null;

			if (subviews.Count < 1)
				this.CanFocus = false;

			foreach (var v in subviews) {
				if (v.Frame.IntersectsWith (touched))
					view.SetNeedsDisplay ();
			}
		}

		void PerformActionForSubview (View subview, Action<View> action)
		{
			if (subviews.Contains (subview)) {
				action (subview);
			}

			SetNeedsDisplay ();
			subview.SetNeedsDisplay ();
		}

		/// <summary>
		/// Brings the specified subview to the front so it is drawn on top of any other views.
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="SendSubviewToBack"/>.
		/// </remarks>
		public void BringSubviewToFront (View subview)
		{
			PerformActionForSubview (subview, x => {
				subviews.Remove (x);
				subviews.Add (x);
			});
		}

		/// <summary>
		/// Sends the specified subview to the front so it is the first view drawn
		/// </summary>
		/// <param name="subview">The subview to send to the front</param>
		/// <remarks>
		///   <seealso cref="BringSubviewToFront(View)"/>.
		/// </remarks>
		public void SendSubviewToBack (View subview)
		{
			PerformActionForSubview (subview, x => {
				subviews.Remove (x);
				subviews.Insert (0, subview);
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void SendSubviewBackwards (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = subviews.IndexOf (x);
				if (idx > 0) {
					subviews.Remove (x);
					subviews.Insert (idx - 1, x);
				}
			});
		}

		/// <summary>
		/// Moves the subview backwards in the hierarchy, only one step
		/// </summary>
		/// <param name="subview">The subview to send backwards</param>
		/// <remarks>
		/// If you want to send the view all the way to the back use SendSubviewToBack.
		/// </remarks>
		public void BringSubviewForward (View subview)
		{
			PerformActionForSubview (subview, x => {
				var idx = subviews.IndexOf (x);
				if (idx + 1 < subviews.Count) {
					subviews.Remove (x);
					subviews.Insert (idx + 1, x);
				}
			});
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
			for (int line = 0; line < h; line++) {
				Move (0, line);
				for (int col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
		}

		/// <summary>
		///   Clears the specified rectangular region with the current color
		/// </summary>
		public void Clear (Rect r)
		{
			var h = r.Height;
			var w = r.Width;
			for (int line = r.Y; line < r.Y + h; line++) {
				Driver.Move (r.X, line);
				for (int col = 0; col < w; col++)
					Driver.AddRune (' ');
			}
		}

		/// <summary>
		/// Converts the (col,row) position from the view into a screen (col,row).  The values are clamped to (0..ScreenDim-1)
		/// </summary>
		/// <param name="col">View-based column.</param>
		/// <param name="row">View-based row.</param>
		/// <param name="rcol">Absolute column, display relative.</param>
		/// <param name="rrow">Absolute row, display relative.</param>
		/// <param name="clipped">Whether to clip the result of the ViewToScreen method, if set to true, the rcol, rrow values are clamped to the screen dimensions.</param>
		internal void ViewToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true)
		{
			// Computes the real row, col relative to the screen.
			rrow = row + frame.Y;
			rcol = col + frame.X;
			var ccontainer = container;
			while (ccontainer != null) {
				rrow += ccontainer.frame.Y;
				rcol += ccontainer.frame.X;
				ccontainer = ccontainer.container;
			}

			// The following ensures that the cursor is always in the screen boundaries.
			if (clipped) {
				rrow = Math.Max (0, Math.Min (rrow, Driver.Rows - 1));
				rcol = Math.Max (0, Math.Min (rcol, Driver.Cols - 1));
			}
		}

		/// <summary>
		/// Converts a point from screen coordinates into the view coordinate space.
		/// </summary>
		/// <returns>The mapped point.</returns>
		/// <param name="x">X screen-coordinate point.</param>
		/// <param name="y">Y screen-coordinate point.</param>
		public Point ScreenToView (int x, int y)
		{
			if (SuperView == null) {
				return new Point (x - Frame.X, y - frame.Y);
			} else {
				var parent = SuperView.ScreenToView (x, y);
				return new Point (parent.X - frame.X, parent.Y - frame.Y);
			}
		}

		// Converts a rectangle in view coordinates to screen coordinates.
		internal Rect RectToScreen (Rect rect)
		{
			ViewToScreen (rect.X, rect.Y, out var x, out var y, clipped: false);
			return new Rect (x, y, rect.Width, rect.Height);
		}

		// Clips a rectangle in screen coordinates to the dimensions currently available on the screen
		internal Rect ScreenClip (Rect rect)
		{
			var x = rect.X < 0 ? 0 : rect.X;
			var y = rect.Y < 0 ? 0 : rect.Y;
			var w = rect.X + rect.Width >= Driver.Cols ? Driver.Cols - rect.X : rect.Width;
			var h = rect.Y + rect.Height >= Driver.Rows ? Driver.Rows - rect.Y : rect.Height;

			return new Rect (x, y, w, h);
		}

		/// <summary>
		/// Sets the Console driver's clip region to the current View's Bounds.
		/// </summary>
		/// <returns>The existing driver's Clip region, which can be then set by setting the Driver.Clip property.</returns>
		public Rect ClipToBounds ()
		{
			return SetClip (Bounds);
		}

		/// <summary>
		/// Sets the clipping region to the specified region, the region is view-relative
		/// </summary>
		/// <returns>The previous clip region.</returns>
		/// <param name="rect">Rectangle region to clip into, the region is view-relative.</param>
		public Rect SetClip (Rect rect)
		{
			var bscreen = RectToScreen (rect);
			var previous = Driver.Clip;
			Driver.Clip = ScreenClip (RectToScreen (Bounds));
			return previous;
		}

		/// <summary>
		/// Draws a frame in the current view, clipped by the boundary of this view
		/// </summary>
		/// <param name="rect">Rectangular region for the frame to be drawn.</param>
		/// <param name="padding">The padding to add to the drawn frame.</param>
		/// <param name="fill">If set to <c>true</c> it fill will the contents.</param>
		public void DrawFrame (Rect rect, int padding = 0, bool fill = false)
		{
			var scrRect = RectToScreen (rect);
			var savedClip = Driver.Clip;
			Driver.Clip = ScreenClip (RectToScreen (Bounds));
			Driver.DrawFrame (scrRect, padding, fill);
			Driver.Clip = savedClip;
		}

		/// <summary>
		/// Utility function to draw strings that contain a hotkey
		/// </summary>
		/// <param name="text">String to display, the underscoore before a letter flags the next letter as the hotkey.</param>
		/// <param name="hotColor">Hot color.</param>
		/// <param name="normalColor">Normal color.</param>
		public void DrawHotString (ustring text, Attribute hotColor, Attribute normalColor)
		{
			Driver.SetAttribute (normalColor);
			foreach (var rune in text) {
				if (rune == '_') {
					Driver.SetAttribute (hotColor);
					continue;
				}
				Driver.AddRune (rune);
				Driver.SetAttribute (normalColor);
			}
		}

		/// <summary>
		/// Utility function to draw strings that contains a hotkey using a colorscheme and the "focused" state.
		/// </summary>
		/// <param name="text">String to display, the underscoore before a letter flags the next letter as the hotkey.</param>
		/// <param name="focused">If set to <c>true</c> this uses the focused colors from the color scheme, otherwise the regular ones.</param>
		/// <param name="scheme">The color scheme to use.</param>
		public void DrawHotString (ustring text, bool focused, ColorScheme scheme)
		{
			if (focused)
				DrawHotString (text, scheme.HotFocus, scheme.Focus);
			else
				DrawHotString (text, scheme.HotNormal, scheme.Normal);
		}

		/// <summary>
		/// This moves the cursor to the specified column and row in the view.
		/// </summary>
		/// <returns>The move.</returns>
		/// <param name="col">Col.</param>
		/// <param name="row">Row.</param>
		public void Move (int col, int row)
		{
			ViewToScreen (col, row, out var rcol, out var rrow);
			Driver.Move (rcol, rrow);
		}

		/// <summary>
		///   Positions the cursor in the right position based on the currently focused view in the chain.
		/// </summary>
		public virtual void PositionCursor ()
		{
			if (focused != null)
				focused.PositionCursor ();
			else
				Move (frame.X, frame.Y);
		}

		/// <inheritdoc cref="HasFocus"/>
		public override bool HasFocus {
			get {
				return base.HasFocus;
			}
			internal set {
				if (base.HasFocus != value)
					if (value)
						OnEnter ();
					else
						OnLeave ();
				SetNeedsDisplay ();
				base.HasFocus = value;

				// Remove focus down the chain of subviews if focus is removed
				if (!value && focused != null) {
					focused.OnLeave ();
					focused.HasFocus = false;
					focused = null;
				}
			}
		}

		/// <inheritdoc cref="OnEnter"/>
		public override bool OnEnter ()
		{
			Enter?.Invoke (this, new EventArgs ());
			return base.OnEnter ();
		}

		/// <inheritdoc cref="OnLeave"/>
		public override bool OnLeave ()
		{
			Leave?.Invoke (this, new EventArgs ());
			return base.OnLeave ();
		}

		/// <summary>
		/// Returns the currently focused view inside this view, or null if nothing is focused.
		/// </summary>
		/// <value>The focused.</value>
		public View Focused => focused;

		/// <summary>
		/// Returns the most focused view in the chain of subviews (the leaf view that has the focus).
		/// </summary>
		/// <value>The most focused.</value>
		public View MostFocused {
			get {
				if (Focused == null)
					return null;
				var most = Focused.MostFocused;
				if (most != null)
					return most;
				return Focused;
			}
		}

		/// <summary>
		/// The color scheme for this view, if it is not defined, it returns the parent's
		/// color scheme.
		/// </summary>
		public ColorScheme ColorScheme {
			get {
				if (colorScheme == null)
					return SuperView?.ColorScheme;
				return colorScheme;
			}
			set {
				colorScheme = value;
			}
		}

		ColorScheme colorScheme;

		/// <summary>
		/// Displays the specified character in the specified column and row.
		/// </summary>
		/// <param name="col">Col.</param>
		/// <param name="row">Row.</param>
		/// <param name="ch">Ch.</param>
		public void AddRune (int col, int row, Rune ch)
		{
			if (row < 0 || col < 0)
				return;
			if (row > frame.Height - 1 || col > frame.Width - 1)
				return;
			Move (col, row);
			Driver.AddRune (ch);
		}

		/// <summary>
		/// Removes the SetNeedsDisplay and the ChildNeedsDisplay setting on this view.
		/// </summary>
		protected void ClearNeedsDisplay ()
		{
			NeedDisplay = Rect.Empty;
			childNeedsDisplay = false;
		}

		/// <summary>
		/// Performs a redraw of this view and its subviews, only redraws the views that have been flagged for a re-display.
		/// </summary>
		/// <param name="region">The region to redraw, this is relative to the view itself.</param>
		/// <remarks>
		/// <para>
		///    Views should set the color that they want to use on entry, as otherwise this will inherit
		///    the last color that was set globaly on the driver.
		/// </para>
		/// </remarks>
		public virtual void Redraw (Rect region)
		{
			var clipRect = new Rect (Point.Empty, frame.Size);

			if (subviews != null) {
				foreach (var view in subviews) {
					if (view.NeedDisplay != null && (!view.NeedDisplay.IsEmpty || view.childNeedsDisplay)) {
						if (view.Frame.IntersectsWith (clipRect) && view.Frame.IntersectsWith (region)) {

							// FIXED: optimize this by computing the intersection of region and view.Bounds
							if (view.layoutNeeded)
								view.LayoutSubviews ();
							Application.CurrentView = view;
							view.Redraw (view.Bounds);
						}
						view.NeedDisplay = Rect.Empty;
						view.childNeedsDisplay = false;
					}
				}
			}
			ClearNeedsDisplay ();
		}

		/// <summary>
		/// Focuses the specified sub-view.
		/// </summary>
		/// <param name="view">View.</param>
		public void SetFocus (View view)
		{
			if (view == null)
				return;
			//Console.WriteLine ($"Request to focus {view}");
			if (!view.CanFocus)
				return;
			if (focused == view)
				return;

			// Make sure that this view is a subview
			View c;
			for (c = view.container; c != null; c = c.container)
				if (c == this)
					break;
			if (c == null)
				throw new ArgumentException ("the specified view is not part of the hierarchy of this view");

			if (focused != null)
				focused.HasFocus = false;

			focused = view;
			focused.HasFocus = true;
			focused.EnsureFocus ();

			// Send focus upwards
			SuperView?.SetFocus (this);
		}

		/// <summary>
		/// Specifies the event arguments for <see cref="KeyEvent"/>
		/// </summary>
		public class KeyEventEventArgs : EventArgs {
			/// <summary>
			/// Constructs.
			/// </summary>
			/// <param name="ke"></param>
			public KeyEventEventArgs (KeyEvent ke) => KeyEvent = ke;
			/// <summary>
			/// The <see cref="KeyEvent"/> for the event.
			/// </summary>
			public KeyEvent KeyEvent { get; set; }
			/// <summary>
			/// Indicates if the current Key event has already been processed and the driver should stop notifying any other event subscriber.
			/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
			/// </summary>
			public bool Handled { get; set; } = false;
		}

		/// <summary>
		/// Invoked when a character key is pressed and occurs after the key up event.
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyPress;

		/// <inheritdoc cref="ProcessKey"/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{

			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (Focused?.ProcessKey (keyEvent) == true)
				return true;

			return false;
		}

		/// <inheritdoc cref="ProcessHotKey"/>
		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.ProcessHotKey (keyEvent))
					return true;
			return false;
		}

		/// <inheritdoc cref="ProcessColdKey"/>
		public override bool ProcessColdKey (KeyEvent keyEvent)
		{
			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyPress?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.ProcessColdKey (keyEvent))
					return true;
			return false;
		}

		/// <summary>
		/// Invoked when a key is pressed
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyDown;

		/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyDown?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.OnKeyDown (keyEvent))
					return true;

			return false;
		}

		/// <summary>
		/// Invoked when a key is released
		/// </summary>
		public event EventHandler<KeyEventEventArgs> KeyUp;

		/// <param name="keyEvent">Contains the details about the key that produced the event.</param>
		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			KeyEventEventArgs args = new KeyEventEventArgs (keyEvent);
			KeyUp?.Invoke (this, args);
			if (args.Handled)
				return true;
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.OnKeyUp (keyEvent))
					return true;

			return false;
		}

		/// <summary>
		/// Finds the first view in the hierarchy that wants to get the focus if nothing is currently focused, otherwise, it does nothing.
		/// </summary>
		public void EnsureFocus ()
		{
			if (focused == null)
				if (FocusDirection == Direction.Forward)
					FocusFirst ();
				else
					FocusLast ();
		}

		/// <summary>
		/// Focuses the first focusable subview if one exists.
		/// </summary>
		public void FocusFirst ()
		{
			if (subviews == null) {
				SuperView?.SetFocus (this);
				return;
			}

			foreach (var view in subviews) {
				if (view.CanFocus) {
					SetFocus (view);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the last focusable subview if one exists.
		/// </summary>
		public void FocusLast ()
		{
			if (subviews == null) {
				SuperView?.SetFocus (this);
				return;
			}

			for (int i = subviews.Count; i > 0;) {
				i--;

				View v = subviews [i];
				if (v.CanFocus) {
					SetFocus (v);
					return;
				}
			}
		}

		/// <summary>
		/// Focuses the previous view.
		/// </summary>
		/// <returns><c>true</c>, if previous was focused, <c>false</c> otherwise.</returns>
		public bool FocusPrev ()
		{
			FocusDirection = Direction.Backward;
			if (subviews == null || subviews.Count == 0)
				return false;

			if (focused == null) {
				FocusLast ();
				return focused != null;
			}
			int focused_idx = -1;
			for (int i = subviews.Count; i > 0;) {
				i--;
				View w = subviews [i];

				if (w.HasFocus) {
					if (w.FocusPrev ())
						return true;
					focused_idx = i;
					continue;
				}
				if (w.CanFocus && focused_idx != -1) {
					focused.HasFocus = false;

					if (w != null && w.CanFocus)
						w.FocusLast ();

					SetFocus (w);
					return true;
				}
			}
			if (focused != null) {
				focused.HasFocus = false;
				focused = null;
			}
			return false;
		}

		/// <summary>
		/// Focuses the next view.
		/// </summary>
		/// <returns><c>true</c>, if next was focused, <c>false</c> otherwise.</returns>
		public bool FocusNext ()
		{
			FocusDirection = Direction.Forward;
			if (subviews == null || subviews.Count == 0)
				return false;

			if (focused == null) {
				FocusFirst ();
				return focused != null;
			}
			int n = subviews.Count;
			int focused_idx = -1;
			for (int i = 0; i < n; i++) {
				View w = subviews [i];

				if (w.HasFocus) {
					if (w.FocusNext ())
						return true;
					focused_idx = i;
					continue;
				}
				if (w.CanFocus && focused_idx != -1) {
					focused.HasFocus = false;

					if (w != null && w.CanFocus)
						w.FocusFirst ();

					SetFocus (w);
					return true;
				}
			}
			if (focused != null) {
				focused.HasFocus = false;
				focused = null;
			}
			return false;
		}

		/// <summary>
		/// Computes the RelativeLayout for the view, given the frame for its container.
		/// </summary>
		/// <param name="hostFrame">The Frame for the host.</param>
		internal void RelativeLayout (Rect hostFrame)
		{
			int w, h, _x, _y;

			if (x is Pos.PosCenter) {
				if (width == null)
					w = hostFrame.Width;
				else
					w = width.Anchor (hostFrame.Width);
				_x = x.Anchor (hostFrame.Width - w);
			} else {
				if (x == null)
					_x = 0;
				else
					_x = x.Anchor (hostFrame.Width);
				if (width == null)
					w = hostFrame.Width;
				else
					w = width.Anchor (hostFrame.Width - _x);
			}

			if (y is Pos.PosCenter) {
				if (height == null)
					h = hostFrame.Height;
				else
					h = height.Anchor (hostFrame.Height);
				_y = y.Anchor (hostFrame.Height - h);
			} else {
				if (y == null)
					_y = 0;
				else
					_y = y.Anchor (hostFrame.Height);
				if (height == null)
					h = hostFrame.Height;
				else
					h = height.Anchor (hostFrame.Height - _y);
			}
			Frame = new Rect (_x, _y, w, h);
			// layoutNeeded = false;
		}

		// https://en.wikipedia.org/wiki/Topological_sorting
		List<View> TopologicalSort (HashSet<View> nodes, HashSet<(View From, View To)> edges)
		{
			var result = new List<View> ();

			// Set of all nodes with no incoming edges
			var S = new HashSet<View> (nodes.Where (n => edges.All (e => e.To.Equals (n) == false)));

			while (S.Any ()) {
				//  remove a node n from S
				var n = S.First ();
				S.Remove (n);

				// add n to tail of L
				if (n != this?.SuperView)
					result.Add (n);

				// for each node m with an edge e from n to m do
				foreach (var e in edges.Where (e => e.From.Equals (n)).ToArray ()) {
					var m = e.To;

					// remove edge e from the graph
					edges.Remove (e);

					// if m has no other incoming edges then
					if (edges.All (me => me.To.Equals (m) == false) && m != this?.SuperView) {
						// insert m into S
						S.Add (m);
					}
				}
			}

			// if graph has edges then
			if (edges.Any ()) {
				// return error (graph has at least one cycle)
				return null;
			} else {
				// return L (a topologically sorted order)
				return result;
			}
		}

		/// <summary>
		/// This virtual method is invoked when a view starts executing or
		/// when the dimensions of the view have changed, for example in
		/// response to the container view or terminal resizing.
		/// </summary>
		public virtual void LayoutSubviews ()
		{
			if (!layoutNeeded)
				return;

			// Sort out the dependencies of the X, Y, Width, Height properties
			var nodes = new HashSet<View> ();
			var edges = new HashSet<(View, View)> ();

			foreach (var v in InternalSubviews) {
				nodes.Add (v);
				if (v.LayoutStyle == LayoutStyle.Computed) {
					if (v.X is Pos.PosView vX)
						edges.Add ((vX.Target, v));
					if (v.Y is Pos.PosView vY)
						edges.Add ((vY.Target, v));
					if (v.Width is Dim.DimView vWidth)
						edges.Add ((vWidth.Target, v));
					if (v.Height is Dim.DimView vHeight)
						edges.Add ((vHeight.Target, v));
				}
			}

			var ordered = TopologicalSort (nodes, edges);
			if (ordered == null)
				throw new Exception ("There is a recursive cycle in the relative Pos/Dim in the views of " + this);

			foreach (var v in ordered) {
				if (v.LayoutStyle == LayoutStyle.Computed)
					v.RelativeLayout (Frame);

				v.LayoutSubviews ();
				v.layoutNeeded = false;

			}

			if (SuperView == Application.Top && layoutNeeded && ordered.Count == 0 && LayoutStyle == LayoutStyle.Computed) {
				RelativeLayout (Frame);
			}

			layoutNeeded = false;
		}

		/// <inheritdoc cref="ToString"/>
		public override string ToString ()
		{
			return $"{GetType ().Name}({Id})({Frame})";
		}

		/// <inheritdoc cref="OnMouseEnter(Gui.MouseEvent)"/>
		public override bool OnMouseEnter (MouseEvent mouseEvent)
		{
			if (!base.OnMouseEnter (mouseEvent)) {
				MouseEnter?.Invoke (this, mouseEvent);
				return false;
			}
			return true;
		}

		/// <inheritdoc cref="OnMouseLeave(Gui.MouseEvent)"/>
		public override bool OnMouseLeave (MouseEvent mouseEvent)
		{
			if (!base.OnMouseLeave (mouseEvent)) {
				MouseLeave?.Invoke (this, mouseEvent);
				return false;
			}
			return true;
		}
	}
}
