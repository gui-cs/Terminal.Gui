//
// Core.cs: The core engine for gui.cs
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
using System.Threading;
using System.Linq;
using NStack;

namespace Terminal.Gui {

	public class Responder {
		public virtual bool CanFocus { get; set; }
		public virtual bool HasFocus { get; internal set; }

		// Key handling
		/// <summary>
		///   This method can be overwritten by view that
		///     want to provide accelerator functionality
		///     (Alt-key for example).
		/// </summary>
		/// <remarks>
		///   <para>
		///     Before keys are sent to the subview on the
		///     current view, all the views are
		///     processed and the key is passed to the widgets
		///     to allow some of them to process the keystroke
		///     as a hot-key. </para>
		///  <para>
		///     For example, if you implement a button that
		///     has a hotkey ok "o", you would catch the
		///     combination Alt-o here.  If the event is
		///     caught, you must return true to stop the
		///     keystroke from being dispatched to other
		///     views.
		///  </para>
		/// </remarks>

		public virtual bool ProcessHotKey (KeyEvent kb)
		{
			return false;
		}

		/// <summary>
		///   If the view is focused, gives the view a
		///   chance to process the keystroke. 
		/// </summary>
		/// <remarks>
		///   <para>
		///     Views can override this method if they are
		///     interested in processing the given keystroke.
		///     If they consume the keystroke, they must
		///     return true to stop the keystroke from being
		///     processed by other widgets or consumed by the
		///     widget engine.    If they return false, the
		///     keystroke will be passed using the ProcessColdKey
		///     method to other views to process.
		///   </para>
		///   <para>
		///     The View implementation does nothing but return false,
		///     so it is not necessary to call base.ProcessKey if you 
		///     derive directly from View, but you should if you derive
		///     other View subclasses.
		///   </para>
		/// </remarks>
		public virtual bool ProcessKey (KeyEvent kb)
		{
			return false;
		}

		/// <summary>
		///   This method can be overwritten by views that
		///     want to provide accelerator functionality
		///     (Alt-key for example), but without
		///     interefering with normal ProcessKey behavior.
		/// </summary>
		/// <remarks>
		///   <para>
		///     After keys are sent to the subviews on the
		///     current view, all the view are
		///     processed and the key is passed to the views
		///     to allow some of them to process the keystroke
		///     as a cold-key. </para>
		///  <para>
		///    This functionality is used, for example, by
		///    default buttons to act on the enter key.
		///    Processing this as a hot-key would prevent
		///    non-default buttons from consuming the enter
		///    keypress when they have the focus.
		///  </para>
		/// </remarks>
		public virtual bool ProcessColdKey (KeyEvent kb)
		{
			return false;
		}

		// Mouse events
		public virtual bool MouseEvent (MouseEvent me)
		{
			return false;
		}
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
	///    Views are created with a specified rectangle region (the frame) that is relative to the container
	///    that they are added into.   
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
	/// </remarks>
	public class View : Responder, IEnumerable {
		View container = null;
		View focused = null;

		/// <summary>
		/// Points to the current driver in use by the view, it is a convenience property
		/// for simplifying the development of new views.
		/// </summary>
		public static ConsoleDriver Driver = Application.Driver;

		static IList<View> empty = new List<View> (0).AsReadOnly ();
		List<View> subviews;

		/// <summary>
		/// This returns a list of the subviews contained by this view.
		/// </summary>
		/// <value>The subviews.</value>
		public IList<View> Subviews => subviews == null ? empty : subviews.AsReadOnly ();
		internal Rect NeedDisplay { get; private set; } = Rect.Empty;

		// The frame for the object
		Rect frame;

		/// <summary>
		/// Gets or sets an identifier for the view;
		/// </summary>
		/// <value>The identifier.</value>
		public ustring Id { get; set; } = "";

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.View"/> want mouse position reports.
		/// </summary>
		/// <value><c>true</c> if want mouse position reports; otherwise, <c>false</c>.</value>
		public virtual bool WantMousePositionReports { get; set; } = false;

		/// <summary>
		/// Gets or sets the frame for the view.
		/// </summary>
		/// <value>The frame.</value>
		/// <remarks>
		///    Altering the Frame of a view will trigger the redrawing of the 
		///    view as well as the redrawing of the affected regions in the superview.
		/// </remarks>
		public Rect Frame {
			get => frame;
			set {
				if (SuperView != null) {
					SuperView.SetNeedsDisplay (frame);
					SuperView.SetNeedsDisplay (value);
				}
				frame = value;

				SetNeedsDisplay (frame);
			}
		}

		/// <summary>
		/// Gets an enumerator that enumerates the subviews in this view.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public IEnumerator GetEnumerator ()
		{
			foreach (var v in subviews)
				yield return v;
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

		/// <summary>
		/// Returns the container for this view, or null if this view has not been added to a container.
		/// </summary>
		/// <value>The super view.</value>
		public View SuperView => container;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.View"/> class with the specified frame.   This is the default constructor.
		/// </summary>
		/// <param name="frame">The region covered by this view.</param>
		public View (Rect frame)
		{
			this.Frame = frame;
			CanFocus = false;
		}

		/// <summary>
		/// Invoke to flag that this view needs to be redisplayed, by any code
		/// that alters the state of the view.
		/// </summary>
		public void SetNeedsDisplay ()
		{
			SetNeedsDisplay (Frame);
		}

		/// <summary>
		/// Flags the specified rectangle region on this view as needing to be repainted.
		/// </summary>
		/// <param name="region">The region that must be flagged for repaint.</param>
		public void SetNeedsDisplay (Rect region)
		{
			if (NeedDisplay.IsEmpty)
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
					view.SetNeedsDisplay (Rect.Intersect (view.Frame, region));
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
				var view = subviews [0];
				Remove (view);
				subviews.RemoveAt (0);
			}
		}

		/// <summary>
		///   Removes a widget from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public virtual void Remove (View view)
		{
			if (view == null)
				return;

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
		Rect RectToScreen (Rect rect)
		{
			ViewToScreen (rect.X, rect.Y, out var x, out var y, clipped: false);
			return new Rect (x, y, rect.Width, rect.Height);
		}

		// Clips a rectangle in screen coordinates to the dimensions currently available on the screen
		Rect ScreenClip (Rect rect)
		{
			var x = rect.X < 0 ? 0 : rect.X;
			var y = rect.Y < 0 ? 0 : rect.Y;
			var w = rect.X + rect.Width >= Driver.Cols ? Driver.Cols - rect.X : rect.Width;
			var h = rect.Y + rect.Height >= Driver.Rows ? Driver.Rows - rect.Y : rect.Height;

			return new Rect (x, y, w, h);
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

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:Terminal.View"/> has focus.
		/// </summary>
		/// <value><c>true</c> if has focus; otherwise, <c>false</c>.</value>
		public override bool HasFocus {
			get {
				return base.HasFocus;
			}
			internal set {
				if (base.HasFocus != value)
					SetNeedsDisplay ();
				base.HasFocus = value;
			}
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
		/// <remarks>
		/// The region argument is relative to the view itself.
		/// </remarks>
		public virtual void Redraw (Rect region)
		{
			var clipRect = new Rect (Point.Empty, frame.Size);

			if (subviews != null) {
				foreach (var view in subviews) {
					if (!view.NeedDisplay.IsEmpty || view.childNeedsDisplay) {
						if (view.Frame.IntersectsWith (clipRect) && view.Frame.IntersectsWith (region)) {

							// TODO: optimize this by computing the intersection of region and view.Bounds
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
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			if (Focused?.ProcessKey (kb) == true)
				return true;

			return false;
		}

		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.ProcessHotKey (kb))
					return true;
			return false;
		}

		public override bool ProcessColdKey (KeyEvent kb)
		{
			if (subviews == null || subviews.Count == 0)
				return false;
			foreach (var view in subviews)
				if (view.ProcessColdKey (kb))
					return true;
			return false;
		}

		/// <summary>
		/// Finds the first view in the hierarchy that wants to get the focus if nothing is currently focused, otherwise, it does nothing.
		/// </summary>
		public void EnsureFocus ()
		{
			if (focused == null)
				FocusFirst ();
		}

		/// <summary>
		/// Focuses the first focusable subview if one exists.
		/// </summary>
		public void FocusFirst ()
		{
			if (subviews == null) {
				SuperView.SetFocus (this);
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
			if (subviews == null)
				return;

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
			if (subviews == null || subviews.Count == 0)
				return false;

			if (focused == null) {
				FocusLast ();
				return true;
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

					if (w.CanFocus)
						w.FocusLast ();

					SetFocus (w);
					return true;
				}
			}
			if (focused_idx != -1) {
				FocusLast ();
				return true;
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
		/// This virtual method is invoked when a view starts executing or 
		/// when the dimensions of the view have changed, for example in 
		/// response to the container view or terminal resizing.
		/// </summary>
		public virtual void LayoutSubviews ()
		{
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:Terminal.View"/>.
		/// </summary>
		/// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:Terminal.View"/>.</returns>
		public override string ToString ()
		{
			return $"{GetType ().Name}({Id})({Frame})";
		}
	}

	/// <summary>
	/// Toplevel views can be modally executed.
	/// </summary>
	/// <remarks>
	///   <para>
	///     Toplevels can be modally executing views, and they return control
	///     to the caller when the "Running" property is set to false.
	///   </para>
	/// </remarks>
	public class Toplevel : View {
		/// <summary>
		/// This flag is checked on each iteration of the mainloop and it continues
		/// running until this flag is set to false.   
		/// </summary>
		public bool Running;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Toplevel"/> class.
		/// </summary>
		/// <param name="frame">Frame.</param>
		public Toplevel (Rect frame) : base (frame)
		{
			ColorScheme = Colors.Base;
		}

		/// <summary>
		/// Convenience factory method that creates a new toplevel with the current terminal dimensions.
		/// </summary>
		/// <returns>The create.</returns>
		public static Toplevel Create ()
		{
			return new Toplevel (new Rect (0, 0, Driver.Cols, Driver.Rows));
		}

		public override bool CanFocus {
			get => true;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			if (base.ProcessKey (kb))
				return true;

			switch (kb.Key) {
			case Key.ControlC:
				// TODO: stop current execution of this container
				break;
			case Key.ControlZ:
				Driver.Suspend ();
				return true;

#if false
			case Key.F5:
				Application.DebugDrawBounds = !Application.DebugDrawBounds;
				SetNeedsDisplay ();
				return true;
#endif
			case Key.Tab:
				var old = Focused;
				if (!FocusNext ())
					FocusNext ();
				if (old != Focused) {
					old?.SetNeedsDisplay ();
					Focused?.SetNeedsDisplay ();
				}
				return true;
			case Key.BackTab:
				old = Focused;
				if (!FocusPrev ())
					FocusPrev ();
				if (old != Focused) {
					old?.SetNeedsDisplay ();
					Focused?.SetNeedsDisplay ();
				}
				return true;
			case Key.ControlL:
				Application.Refresh ();
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// A toplevel view that draws a frame around its region and has a "ContentView" subview where the contents are added.
	/// </summary>
	public class Window : Toplevel, IEnumerable {
		View contentView;
		ustring title;

		/// <summary>
		/// The title to be displayed for this window.
		/// </summary>
		/// <value>The title.</value>
		public ustring Title {
			get => title;
			set {
				title = value;
				SetNeedsDisplay ();
			}
		}

		class ContentView : View {
			public ContentView (Rect frame) : base (frame) { }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Window"/> class with an optioanl title
		/// </summary>
		/// <param name="frame">Frame.</param>
		/// <param name="title">Title.</param>
		public Window (Rect frame, ustring title = null) : this (frame, title, padding: 0)
		{
		}

		int padding;
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Window"/> with
		/// the specified frame for its location, with the specified border 
		/// an optional title.
		/// </summary>
		/// <param name="frame">Frame.</param>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="title">Title.</param>
		public Window (Rect frame, ustring title = null, int padding = 0) : base (frame)
		{
			this.Title = title;
			int wb = 2 * (1 + padding);
			this.padding = padding;
			var cFrame = new Rect (1 + padding, 1 + padding, frame.Width - wb, frame.Height - wb);
			contentView = new ContentView (cFrame);
			base.Add (contentView);
		}

		/// <summary>
		/// Enumerates the various views in the ContentView.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public new IEnumerator GetEnumerator ()
		{
			return contentView.GetEnumerator ();
		}

		void DrawFrame ()
		{
			DrawFrame (new Rect (0, 0, Frame.Width, Frame.Height), padding, fill: true);
		}

		/// <summary>
		/// Add the specified view to the ContentView.
		/// </summary>
		/// <param name="view">View to add to the window.</param>
		public override void Add (View view)
		{
			contentView.Add (view);
		}

		public override void Redraw (Rect bounds)
		{
			if (!NeedDisplay.IsEmpty) {
				Driver.SetAttribute (ColorScheme.Normal);
				DrawFrame ();
				if (HasFocus)
					Driver.SetAttribute (ColorScheme.Normal);
				var width = Frame.Width;
				if (Title != null && width > 4) {
					Move (1+padding, padding);
					Driver.AddRune (' ');
					var str = Title.Length > width ? Title [0, width-4] : Title;
					Driver.AddStr (str);
					Driver.AddRune (' ');
				}
				Driver.SetAttribute (ColorScheme.Normal);
			}
			contentView.Redraw (contentView.Bounds);
			ClearNeedsDisplay ();
		}

#if false
		// 
		// It does not look like the event is raised on clicked-drag
		// need to figure that out.
		//
		Point? dragPosition;
		public override bool MouseEvent(MouseEvent me)
		{
			if (me.Flags == MouseFlags.Button1Pressed){
				if (dragPosition.HasValue) {
					var dx = me.X - dragPosition.Value.X;
					var dy = me.Y - dragPosition.Value.Y;

					var nx = Frame.X + dx;
					var ny = Frame.Y + dy;
					if (nx < 0)
						nx = 0;
					if (ny < 0)
						ny = 0;

					Demo.ml2.Text = $"{dx},{dy}";
					dragPosition = new Point (me.X, me.Y);

					// TODO: optimize, only SetNeedsDisplay on the before/after regions.
					if (SuperView == null)
						Application.Refresh ();
					else
						SuperView.SetNeedsDisplay ();
					Frame = new Rect (nx, ny, Frame.Width, Frame.Height);
					SetNeedsDisplay ();
					return true;
				} else {
					dragPosition = new Point (me.X, me.Y);
					Application.GrabMouse (this);

					Demo.ml2.Text = $"Starting at {dragPosition}";
					return true;
				}


			}

			if (me.Flags == MouseFlags.Button1Released) {
				Application.UngrabMouse ();
				dragPosition = null;
				//Driver.StopReportingMouseMoves ();
			}

			Demo.ml.Text = me.ToString ();
			return false;
		}
#endif
	}

	/// <summary>
	/// The application driver for gui.cs
	/// </summary>
	/// <remarks>
	///   <para>
	///     You can hook up to the Iteration event to have your method 
	///     invoked on each iteration of the mainloop.
	///   </para>
	///   <para>
	///     Creates a mainloop to process input events, handle timers and
	///     other sources of data.   It is accessible via the MainLoop property.
	///   </para>
	///   <para>
	///     When invoked sets the SynchronizationContext to one that is tied
	///     to the mainloop, allowing user code to use async/await.
	///   </para>
	/// </remarks>
	public class Application {
		/// <summary>
		/// The current Console Driver in use.
		/// </summary>
		public static ConsoleDriver Driver = new CursesDriver ();

		/// <summary>
		/// The Toplevel object used for the application on startup.
		/// </summary>
		/// <value>The top.</value>
		public static Toplevel Top { get; private set; }

		/// <summary>
		/// The current toplevel object.   This is updated when Application.Run enters and leaves and points to the current toplevel.
		/// </summary>
		/// <value>The current.</value>
		public static Toplevel Current { get; private set; }

		/// <summary>
		/// The mainloop driver for the applicaiton
		/// </summary>
		/// <value>The main loop.</value>
		public static Mono.Terminal.MainLoop MainLoop { get; private set; }

		static Stack<Toplevel> toplevels = new Stack<Toplevel> ();

		/// <summary>
		///   This event is raised on each iteration of the
		///   main loop. 
		/// </summary>
		/// <remarks>
		///   See also <see cref="Timeout"/>
		/// </remarks>
		static public event EventHandler Iteration;

		/// <summary>
		/// Returns a rectangle that is centered in the screen for the provided size.
		/// </summary>
		/// <returns>The centered rect.</returns>
		/// <param name="size">Size for the rectangle.</param>
		public static Rect MakeCenteredRect (Size size)
		{
			return new Rect (new Point ((Driver.Cols - size.Width) / 2, (Driver.Rows - size.Height) / 2), size);
		}

		//
		// provides the sync context set while executing code in gui.cs, to let
		// users use async/await on their code
		//
		class MainLoopSyncContext : SynchronizationContext {
			Mono.Terminal.MainLoop mainLoop;

			public MainLoopSyncContext (Mono.Terminal.MainLoop mainLoop)
			{
				this.mainLoop = mainLoop;
			}

			public override SynchronizationContext CreateCopy ()
			{
				return new MainLoopSyncContext (MainLoop);
			}

			public override void Post (SendOrPostCallback d, object state)
			{
				mainLoop.AddIdle (() => { 
					d (state);
					return false;
				});
			}

			public override void Send (SendOrPostCallback d, object state)
			{
				mainLoop.Invoke (() => {
					d (state);
				});
			}
		}

		/// <summary>
		/// Initializes the Application
		/// </summary>
		public static void Init ()
		{
			if (Top != null)
				return;

			Driver.Init (TerminalResized);
			MainLoop = new Mono.Terminal.MainLoop ();
			SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext (MainLoop));
			Top = Toplevel.Create ();
			Current = Top;
		}

		/// <summary>
		/// Captures the execution state for the provided TopLevel view.
		/// </summary>
		public class RunState : IDisposable {
			internal RunState (Toplevel view)
			{
				Toplevel = view;
			}
			internal Toplevel Toplevel;

			/// <summary>
			/// Releases all resource used by the <see cref="T:Terminal.Application.RunState"/> object.
			/// </summary>
			/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:Terminal.Application.RunState"/>. The
			/// <see cref="Dispose"/> method leaves the <see cref="T:Terminal.Application.RunState"/> in an unusable state. After
			/// calling <see cref="Dispose"/>, you must release all references to the
			/// <see cref="T:Terminal.Application.RunState"/> so the garbage collector can reclaim the memory that the
			/// <see cref="T:Terminal.Application.RunState"/> was occupying.</remarks>
			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}

			/// <summary>
			/// Dispose the specified disposing.
			/// </summary>
			/// <returns>The dispose.</returns>
			/// <param name="disposing">If set to <c>true</c> disposing.</param>
			public virtual void Dispose (bool disposing)
			{
				if (Toplevel != null) {
					Application.End (Toplevel);
					Toplevel = null;
				}
			}
		}

		static void ProcessKeyEvent (KeyEvent ke)
		{
			if (Current.ProcessHotKey (ke))
				return;

			if (Current.ProcessKey (ke))
				return;
			
			// Process the key normally
			if (Current.ProcessColdKey (ke))
				return;
		}

		static View FindDeepestView (View start, int x, int y, out int resx, out int resy)
		{
			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}

			if (start.Subviews != null){
				int count = start.Subviews.Count;
				if (count > 0) {
					var rx = x - startFrame.X;
					var ry = y - startFrame.Y;
					for (int i = count - 1; i >= 0; i--) {
						View v = start.Subviews [i];
						if (v.Frame.Contains (rx, ry)) {
							var deep = FindDeepestView (v, rx, ry, out resx, out resy);
							if (deep == null)
								return v;
							return deep;
						}
					}
				}
			}
			resx = x-startFrame.X;
			resy = y-startFrame.Y;
			return start;
		}

		static View mouseGrabView;

		/// <summary>
		/// Grabs the mouse, forcing all mouse events to be routed to the specified view until UngrabMouse is called.
		/// </summary>
		/// <returns>The grab.</returns>
		/// <param name="view">View that will receive all mouse events until UngrabMouse is invoked.</param>
		public static void GrabMouse (View view)
		{
			if (view == null)
				return;
			mouseGrabView = view;
		}

		/// <summary>
		/// Releases the mouse grab, so mouse events will be routed to the view on which the mouse is.
		/// </summary>
		public static void UngrabMouse ()
		{
			mouseGrabView = null;
		}

		/// <summary>
		/// Merely a debugging aid to see the raw mouse events
		/// </summary>
		static public Action<MouseEvent> RootMouseEvent;

		static void ProcessMouseEvent (MouseEvent me)
		{
			RootMouseEvent?.Invoke (me);
			if (mouseGrabView != null) {
				var newxy = mouseGrabView.ScreenToView (me.X, me.Y);
				var nme = new MouseEvent () {
					X = newxy.X,
					Y = newxy.Y,
					Flags = me.Flags
				};
				mouseGrabView.MouseEvent (me);
				return;
			}

			int rx, ry;
			var view = FindDeepestView (Current, me.X, me.Y, out rx, out ry);
			if (view != null) {
				if (!view.WantMousePositionReports && me.Flags == MouseFlags.ReportMousePosition)
					return;
				
				var nme = new MouseEvent () {
					X = rx,
					Y = ry,
					Flags = me.Flags
				};
				// Should we bubbled up the event, if it is not handled?
				view.MouseEvent (nme);
			}
		}

		static public RunState Begin (Toplevel toplevel)
		{
			if (toplevel == null)
				throw new ArgumentNullException (nameof (toplevel));
			var rs = new RunState (toplevel);

			Init ();
			toplevels.Push (toplevel);
			Current = toplevel;
			Driver.PrepareToRun (MainLoop, ProcessKeyEvent, ProcessMouseEvent);
			toplevel.LayoutSubviews ();
			toplevel.FocusFirst ();
			Redraw (toplevel);
			toplevel.PositionCursor ();
			Driver.Refresh ();

			return rs;
		}

		static public void End (RunState rs)
		{
			if (rs == null)
				throw new ArgumentNullException (nameof (rs));

			rs.Dispose ();
		}

		static void Shutdown ()
		{
			Driver.End ();
		}

		static void Redraw (View view)
		{
			view.Redraw (view.Bounds);
			Driver.Refresh ();
		}

		static void Refresh (View view)
		{
			view.Redraw (view.Bounds);
			Driver.Refresh ();
		}

		/// <summary>
		/// Triggers a refresh of the entire display.
		/// </summary>
		public static void Refresh ()
		{
			Driver.RedrawTop ();
			View last = null;
			foreach (var v in toplevels.Reverse ()) {
				v.SetNeedsDisplay ();
				v.Redraw (v.Bounds);
				last = v;
			}
			last?.PositionCursor ();
			Driver.Refresh ();
		}

		internal static void End (View view)
		{
			if (toplevels.Peek () != view)
				throw new ArgumentException ("The view that you end with must be balanced");
			toplevels.Pop ();
			if (toplevels.Count == 0)
				Shutdown ();
			else {
				Current = toplevels.Peek () as Toplevel;
				Refresh ();
			}
		}

		/// <summary>
		///   Runs the main loop for the created dialog
		/// </summary>
		/// <remarks>
		///   Use the wait parameter to control whether this is a
		///   blocking or non-blocking call.
		/// </remarks>
		public static void RunLoop (RunState state, bool wait = true)
		{
			if (state == null)
				throw new ArgumentNullException (nameof (state));
			if (state.Toplevel == null)
				throw new ObjectDisposedException ("state");

			for (state.Toplevel.Running = true; state.Toplevel.Running;) {
				if (MainLoop.EventsPending (wait)) {
					MainLoop.MainIteration ();
					if (Iteration != null)
						Iteration (null, EventArgs.Empty);
				} else if (wait == false)
					return;
				if (!state.Toplevel.NeedDisplay.IsEmpty || state.Toplevel.childNeedsDisplay) {
					state.Toplevel.Redraw (state.Toplevel.Bounds);
					if (DebugDrawBounds)
						DrawBounds (state.Toplevel);
					state.Toplevel.PositionCursor ();
					Driver.Refresh ();
				}
			}
		}

		internal static bool DebugDrawBounds;

		// Need to look into why this does not work properly.
		static void DrawBounds (View v)
		{
			v.DrawFrame (v.Frame, padding: 0, fill: false);
			if (v.Subviews != null && v.Subviews.Count > 0)
				foreach (var sub in v.Subviews)
					DrawBounds (sub);
		}

		/// <summary>
		/// Runs the application with the built-in toplevel view
		/// </summary>
		public static void Run ()
		{
			Run (Top);
		}

		/// <summary>
		///   Runs the main loop on the given container.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This method is used to start processing events
		///     for the main application, but it is also used to
		///     run modal dialog boxes.
		///   </para>
		///   <para>
		///     To make a toplevel stop execution, set the "Running"
		///     property to false.
		///   </para>
		/// </remarks>
		public static void Run (Toplevel view)
		{
			var runToken = Begin (view);
			RunLoop (runToken);
			End (runToken);
		}

		/// <summary>
		/// Stops running the most recent toplevel
		/// </summary>
		public static void RequestStop ()
		{
			var ct = Current as Toplevel;

			Current.Running = false;
		}

		static void TerminalResized ()
		{
			foreach (var t in toplevels) {
				t.Frame = new Rect (0, 0, Driver.Cols, Driver.Rows);
			}
		}
	}
}