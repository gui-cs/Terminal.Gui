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

namespace Terminal {

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
		public virtual void MouseEvent (Event.Mouse me) { }
	}

	public class View : Responder, IEnumerable {
		string id = "";
		View container = null;
		View focused = null;
		public static ConsoleDriver Driver = Application.Driver;
		public static IList<View> empty = new List<View> (0).AsReadOnly ();
		List<View> subviews;
		public IList<View> Subviews => subviews == null ? empty : subviews.AsReadOnly ();
		internal Rect NeedDisplay { get; private set; } = Rect.Empty;

		// The frame for the object
		Rect frame;

		public string Id {
			get => id;
			set {
				id = value;
			}
		}

		// The frame for this view
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

		public IEnumerator GetEnumerator ()
		{
			foreach (var v in subviews)
				yield return v;
		}

		public Rect Bounds {
			get => new Rect (Point.Empty, Frame.Size);
			set {
				Frame = new Rect (frame.Location, value.Size);
			}
		}

		public View SuperView => container;

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
					Driver.AddCh (' ');
			}
		}

		/// <summary>
		/// Converts the (col,row) position from the view into a screen (col,row).  The values are clamped to (0..ScreenDim-1)
		/// </summary>
		/// <param name="col">View-based column.</param>
		/// <param name="row">View-based row.</param>
		/// <param name="rcol">Absolute column, display relative.</param>
		/// <param name="rrow">Absolute row, display relative.</param>
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
		/// <param name="fill">If set to <c>true</c> it fill will the contents.</param>
		public void DrawFrame (Rect rect, bool fill = false)
		{
			var scrRect = RectToScreen (rect);
			var savedClip = Driver.Clip;
			Driver.Clip = ScreenClip (RectToScreen (Bounds));
			Driver.DrawFrame (scrRect, fill);
			Driver.Clip = savedClip;
		}

		/// <summary>
		/// Utility function to draw strings that contain a hotkey
		/// </summary>
		/// <param name="s">String to display, the underscoore before a letter flags the next letter as the hotkey.</param>
		/// <param name="hotColor">Hot color.</param>
		/// <param name="normalColor">Normal color.</param>
		public void DrawHotString (string text, Attribute hotColor, Attribute normalColor)
		{
			Driver.SetAttribute (normalColor);
			foreach (var c in text) {
				if (c == '_') {
					Driver.SetAttribute (hotColor);
					continue;
				}
				Driver.AddCh (c);
				Driver.SetAttribute (normalColor);
			}
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
		/// Displays the specified character in the specified column and row.
		/// </summary>
		/// <param name="col">Col.</param>
		/// <param name="row">Row.</param>
		/// <param name="ch">Ch.</param>
		public void AddCh (int col, int row, int ch)
		{
			if (row < 0 || col < 0)
				return;
			if (row > frame.Height - 1 || col > frame.Width - 1)
				return;
			Move (col, row);
			Driver.AddCh (ch);
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
			NeedDisplay = Rect.Empty;
			childNeedsDisplay = false;
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
				if (view.ProcessHotKey (kb))
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

		public virtual void LayoutSubviews ()
		{
		}

		public override string ToString ()
		{
			return $"{GetType ().Name}({id})({Frame})";
		}
	}

	/// <summary>
	/// Toplevel views can be modally executed.
	/// </summary>
	public class Toplevel : View {
		public bool Running;

		public Toplevel (Rect frame) : base (frame)
		{
		}

		public static Toplevel Create ()
		{
			return new Toplevel (new Rect (0, 0, Driver.Cols, Driver.Rows));
		}

		public override bool CanFocus {
			get => true;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			if (ProcessHotKey (kb))
				return true;

			if (base.ProcessKey (kb))
				return true;

			// Process the key normally
			if (ProcessColdKey (kb))
				return true;

			switch (kb.Key) {
			case Key.ControlC:
				// TODO: stop current execution of this container
				break;
			case Key.ControlZ:
				// TODO: should suspend
				// console_csharp_send_sigtstp ();
				break;
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
				SetNeedsDisplay();
				return true;
			}
			return false;
		}

#if false
        public override void Redraw ()
        {
            base.Redraw ();
            for (int i = 0; i < Driver.Cols; i++) {
                Driver.Move (0, i);
                Driver.AddStr ("Line: " + i);
            }
        }
#endif
	}

	/// <summary>
	/// A toplevel view that draws a frame around its region
	/// </summary>
	public class Window : Toplevel, IEnumerable {
		View contentView;
		string title;

		public string Title {
			get => title;
			set {
				title = value;
				SetNeedsDisplay ();
			}
		}

		class ContentView : View {
			public ContentView (Rect frame) : base (frame) { }
		}

		public Window (Rect frame, string title = null) : base (frame)
		{
			this.Title = title;
			frame.Inflate (-1, -1);
			contentView = new ContentView (frame);
			base.Add (contentView);
		}

		public new IEnumerator GetEnumerator ()
		{
			return contentView.GetEnumerator ();
		}

		void DrawFrame ()
		{
			DrawFrame (new Rect (0, 0, Frame.Width, Frame.Height), true);
		}

		public override void Add (View view)
		{
			contentView.Add (view);
		}

		public override void Redraw (Rect bounds)
		{
			if (!NeedDisplay.IsEmpty) {
				Driver.SetAttribute (Colors.Base.Normal);
				DrawFrame ();
				if (HasFocus)
					Driver.SetAttribute (Colors.Dialog.Normal);
				var width = Frame.Width;
				if (Title != null && width > 4) {
					Move (1, 0);
					Driver.AddCh (' ');
					var str = Title.Length > width ? Title.Substring (0, width - 4) : Title;
					Driver.AddStr (str);
					Driver.AddCh (' ');
				}
				Driver.SetAttribute (Colors.Dialog.Normal);
			}
			contentView.Redraw (contentView.Bounds);
		}
	}

	public class Application {
		public static ConsoleDriver Driver = new CursesDriver ();
		public static Toplevel Top { get; private set; }
		public static Mono.Terminal.MainLoop MainLoop { get; private set; }

		static Stack<View> toplevels = new Stack<View> ();
		static Responder focus;

		/// <summary>
		///   This event is raised on each iteration of the
		///   main loop. 
		/// </summary>
		/// <remarks>
		///   See also <see cref="Timeout"/>
		/// </remarks>
		static public event EventHandler Iteration;

		public static void MakeFirstResponder (Responder newResponder)
		{
			if (newResponder == null)
				throw new ArgumentNullException ();

			throw new NotImplementedException ();
		}

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
			focus = Top;
		}

		public class RunState : IDisposable {
			internal RunState (Toplevel view)
			{
				Toplevel = view;
			}
			internal Toplevel Toplevel;

			public void Dispose ()
			{
				Dispose (true);
				GC.SuppressFinalize (this);
			}

			public virtual void Dispose (bool disposing)
			{
				if (Toplevel != null) {
					Application.End (Toplevel);
					Toplevel = null;
				}
			}
		}

		static void KeyEvent (Key key)
		{
		}

		static public RunState Begin (Toplevel toplevel)
		{
			if (toplevel == null)
				throw new ArgumentNullException (nameof (toplevel));
			var rs = new RunState (toplevel);

			Init ();
			toplevels.Push (toplevel);
			Driver.PrepareToRun (MainLoop, toplevel);
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

		public static void Refresh ()
		{
			Driver.RedrawTop ();
			View last = null;
			foreach (var v in toplevels) {
				v.Redraw (v.Bounds);
				last = v;
			}
			if (last != null)
				last.PositionCursor ();
			Driver.Refresh ();
		}

		internal static void End (View view)
		{
			if (toplevels.Peek () != view)
				throw new ArgumentException ("The view that you end with must be balanced");
			toplevels.Pop ();
			if (toplevels.Count == 0)
				Shutdown ();
			else
				Refresh ();
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
					state.Toplevel.PositionCursor ();
					Driver.Refresh ();
				}
			}
		}

		public static void Run ()
		{
			Run (Top);
		}

		/// <summary>
		///   Runs the main loop on the given container.
		/// </summary>
		/// <remarks>
		///   This method is used to start processing events
		///   for the main application, but it is also used to
		///   run modal dialog boxes.
		/// </remarks>
		public static void Run (Toplevel view)
		{
			var runToken = Begin (view);
			RunLoop (runToken);
			End (runToken);
		}

		static void TerminalResized ()
		{
			foreach (var t in toplevels) {
				t.Frame = new Rect (0, 0, Driver.Cols, Driver.Rows);
			}
		}
	}
}