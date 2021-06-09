//
// Toplevel.cs: Toplevel views can be modally executed
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// Toplevel views can be modally executed.
	/// </summary>
	/// <remarks>
	///   <para>
	///     Toplevels can be modally executing views, started by calling <see cref="Application.Run(Toplevel, Func{Exception, bool})"/>. 
	///     They return control to the caller when <see cref="Application.RequestStop()"/> has 
	///     been called (which sets the <see cref="Toplevel.Running"/> property to false). 
	///   </para>
	///   <para>
	///     A Toplevel is created when an application initialzies Terminal.Gui by callling <see cref="Application.Init(ConsoleDriver, IMainLoopDriver)"/>.
	///     The application Toplevel can be accessed via <see cref="Application.Top"/>. Additional Toplevels can be created 
	///     and run (e.g. <see cref="Dialog"/>s. To run a Toplevel, create the <see cref="Toplevel"/> and 
	///     call <see cref="Application.Run(Toplevel, Func{Exception, bool})"/>.
	///   </para>
	///   <para>
	///     Toplevels can also opt-in to more sophisticated initialization
	///     by implementing <see cref="ISupportInitialize"/>. When they do
	///     so, the <see cref="ISupportInitialize.BeginInit"/> and
	///     <see cref="ISupportInitialize.EndInit"/> methods will be called
	///     before running the view.
	///     If first-run-only initialization is preferred, the <see cref="ISupportInitializeNotification"/>
	///     can be implemented too, in which case the <see cref="ISupportInitialize"/>
	///     methods will only be called if <see cref="ISupportInitializeNotification.IsInitialized"/>
	///     is <see langword="false"/>. This allows proper <see cref="View"/> inheritance hierarchies
	///     to override base class layout code optimally by doing so only on first run,
	///     instead of on every run.
	///   </para>
	/// </remarks>
	public class Toplevel : View {
		/// <summary>
		/// Gets or sets whether the <see cref="MainLoop"/> for this <see cref="Toplevel"/> is running or not. 
		/// </summary>
		/// <remarks>
		///    Setting this property directly is discouraged. Use <see cref="Application.RequestStop"/> instead. 
		/// </remarks>
		public bool Running { get; set; }

		/// <summary>
		/// Fired once the Toplevel's <see cref="Application.RunState"/> has begin loaded.
		/// A Loaded event handler is a good place to finalize initialization before calling `<see cref="Application.RunLoop(Application.RunState, bool)"/>.
		/// </summary>
		public event Action Loaded;

		/// <summary>
		/// Fired once the Toplevel's <see cref="MainLoop"/> has started it's first iteration.
		/// Subscribe to this event to perform tasks when the <see cref="Toplevel"/> has been laid out and focus has been set.
		/// changes. A Ready event handler is a good place to finalize initialization after calling `<see cref="Application.Run(Func{Exception, bool})"/>(topLevel)`.
		/// </summary>
		public event Action Ready;

		/// <summary>
		/// Fired once the Toplevel's <see cref="Application.RunState"/> has begin unloaded.
		/// A Unloaded event handler is a good place to disposing after calling `<see cref="Application.End(Application.RunState)"/>.
		/// </summary>
		public event Action Unloaded;

		/// <summary>
		/// Called from <see cref="Application.Begin(Toplevel)"/> before the <see cref="Toplevel"/> is redraws for the first time.
		/// </summary>
		internal virtual void OnLoaded ()
		{
			Loaded?.Invoke ();
		}

		/// <summary>
		/// Called from <see cref="Application.RunLoop"/> after the <see cref="Toplevel"/> has entered it's first iteration of the loop.
		/// </summary>
		internal virtual void OnReady ()
		{
			Ready?.Invoke ();
		}

		/// <summary>
		/// Called from <see cref="Application.End(Application.RunState)"/> before the <see cref="Toplevel"/> is disposed.
		/// </summary>
		internal virtual void OnUnloaded ()
		{
			Unloaded?.Invoke ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Toplevel"/> class with the specified absolute layout.
		/// </summary>
		/// <param name="frame">A superview-relative rectangle specifying the location and size for the new Toplevel</param>
		public Toplevel (Rect frame) : base (frame)
		{
			Initialize ();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Toplevel"/> class with <see cref="LayoutStyle.Computed"/> layout, defaulting to full screen.
		/// </summary>
		public Toplevel () : base ()
		{
			Initialize ();
			Width = Dim.Fill ();
			Height = Dim.Fill ();
		}

		void Initialize ()
		{
			ColorScheme = Colors.Base;
		}

		/// <summary>
		/// Convenience factory method that creates a new Toplevel with the current terminal dimensions.
		/// </summary>
		/// <returns>The create.</returns>
		public static Toplevel Create ()
		{
			return new Toplevel (new Rect (0, 0, Driver.Cols, Driver.Rows));
		}

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Toplevel"/> can focus.
		/// </summary>
		/// <value><c>true</c> if can focus; otherwise, <c>false</c>.</value>
		public override bool CanFocus {
			get => true;
		}

		/// <summary>
		/// Determines whether the <see cref="Toplevel"/> is modal or not.
		/// Causes <see cref="ProcessKey(KeyEvent)"/> to propagate keys upwards
		/// by default unless set to <see langword="true"/>.
		/// </summary>
		public bool Modal { get; set; }

		/// <summary>
		/// Gets or sets the menu for this Toplevel
		/// </summary>
		public MenuBar MenuBar { get; set; }

		/// <summary>
		/// Gets or sets the status bar for this Toplevel
		/// </summary>
		public StatusBar StatusBar { get; set; }

		///<inheritdoc/>
		public override bool OnKeyDown (KeyEvent keyEvent)
		{
			if (base.OnKeyDown (keyEvent)) {
				return true;
			}

			switch (keyEvent.Key) {
			case Key.AltMask:
			case Key.AltMask | Key.Space:
			case Key.CtrlMask | Key.Space:
				if (MenuBar != null && MenuBar.OnKeyDown (keyEvent)) {
					return true;
				}
				break;
			}

			return false;
		}

		///<inheritdoc/>
		public override bool OnKeyUp (KeyEvent keyEvent)
		{
			if (base.OnKeyUp (keyEvent)) {
				return true;
			}

			switch (keyEvent.Key) {
			case Key.AltMask:
			case Key.AltMask | Key.Space:
			case Key.CtrlMask | Key.Space:
				if (MenuBar != null && MenuBar.OnKeyUp (keyEvent)) {
					return true;
				}
				break;
			}

			return false;
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent keyEvent)
		{
			if (base.ProcessKey (keyEvent))
				return true;

			switch (ShortcutHelper.GetModifiersKey (keyEvent)) {
			case Key.Q | Key.CtrlMask:
				// FIXED: stop current execution of this container
				Application.RequestStop ();
				break;
			case Key.Z | Key.CtrlMask:
				Driver.Suspend ();
				return true;

#if false
			case Key.F5:
				Application.DebugDrawBounds = !Application.DebugDrawBounds;
				SetNeedsDisplay ();
				return true;
#endif
			case Key.Tab:
			case Key.CursorRight:
			case Key.CursorDown:
			case Key.I | Key.CtrlMask: // Unix
				var old = GetDeepestFocusedSubview (Focused);
				if (!FocusNext ())
					FocusNext ();
				if (old != Focused && old != Focused?.Focused) {
					old?.SetNeedsDisplay ();
					Focused?.SetNeedsDisplay ();
				} else {
					FocusNearestView (SuperView?.TabIndexes, Direction.Forward);
				}
				return true;
			case Key.BackTab | Key.ShiftMask:
			case Key.CursorLeft:
			case Key.CursorUp:
				old = GetDeepestFocusedSubview (Focused);
				if (!FocusPrev ())
					FocusPrev ();
				if (old != Focused && old != Focused?.Focused) {
					old?.SetNeedsDisplay ();
					Focused?.SetNeedsDisplay ();
				} else {
					FocusNearestView (SuperView?.TabIndexes?.Reverse(), Direction.Backward);
				}
				return true;
			case Key.Tab | Key.CtrlMask:
			case Key key when key == Application.AlternateForwardKey: // Needed on Unix
				Application.Top.FocusNext ();
				if (Application.Top.Focused == null) {
					Application.Top.FocusNext ();
				}
				return true;
			case Key.Tab | Key.ShiftMask | Key.CtrlMask:
			case Key key when key == Application.AlternateBackwardKey: // Needed on Unix
				Application.Top.FocusPrev ();
				if (Application.Top.Focused == null) {
					Application.Top.FocusPrev ();
				}
				return true;
			case Key.L | Key.CtrlMask:
				Application.Refresh ();
				return true;
			}
			return false;
		}

		///<inheritdoc/>
		public override bool ProcessColdKey (KeyEvent keyEvent)
		{
			if (base.ProcessColdKey (keyEvent)) {
				return true;
			}

			if (ShortcutHelper.FindAndOpenByShortcut(keyEvent, this)) {
				return true;
			}
			return false;
		}

		View GetDeepestFocusedSubview (View view)
		{
			if (view == null) {
				return null;
			}

			foreach (var v in view.Subviews) {
				if (v.HasFocus) {
					return GetDeepestFocusedSubview (v);
				}
			}
			return view;
		}

		void FocusNearestView (IEnumerable<View> views, Direction direction)
		{
			if (views == null) {
				return;
			}

			bool found = false;
			bool focusProcessed = false;
			int idx = 0;

			foreach (var v in views) {
				if (v == this) {
					found = true;
				}
				if (found && v != this) {
					if (direction == Direction.Forward) {
						SuperView?.FocusNext ();
					} else {
						SuperView?.FocusPrev ();
					}
					focusProcessed = true;
					if (SuperView.Focused != null && SuperView.Focused != this) {
						return;
					}
				} else if (found && !focusProcessed && idx == views.Count () - 1) {
					views.ToList () [0].SetFocus ();
				}
				idx++;
			}
		}

		///<inheritdoc/>
		public override void Add (View view)
		{
			if (this == Application.Top) {
				AddMenuStatusBar (view);
			}
			base.Add (view);
		}

		internal void AddMenuStatusBar (View view)
		{
			if (view is MenuBar) {
				MenuBar = view as MenuBar;
			}
			if (view is StatusBar) {
				StatusBar = view as StatusBar;
			}
		}

		///<inheritdoc/>
		public override void Remove (View view)
		{
			if (this is Toplevel toplevel && toplevel.MenuBar != null) {
				RemoveMenuStatusBar (view);
			}
			base.Remove (view);
		}

		///<inheritdoc/>
		public override void RemoveAll ()
		{
			if (this == Application.Top) {
				MenuBar?.Dispose ();
				MenuBar = null;
				StatusBar?.Dispose ();
				StatusBar = null;
			}
			base.RemoveAll ();
		}

		internal void RemoveMenuStatusBar (View view)
		{
			if (view is MenuBar) {
				MenuBar?.Dispose ();
				MenuBar = null;
			}
			if (view is StatusBar) {
				StatusBar?.Dispose ();
				StatusBar = null;
			}
		}

		internal void EnsureVisibleBounds (Toplevel top, int x, int y, out int nx, out int ny)
		{
			nx = Math.Max (x, 0);
			int l;
			if (SuperView == null || SuperView is Toplevel) {
				l = Driver.Cols;
			} else {
				l = SuperView.Frame.Width;
			}
			nx = nx + top.Frame.Width > l ? Math.Max (l - top.Frame.Width, 0) : nx;
			SetWidth (top.Frame.Width, out int rWidth);
			if (rWidth < 0 && nx >= top.Frame.X) {
				nx = Math.Max (top.Frame.Right - 2, 0);
			}
			//System.Diagnostics.Debug.WriteLine ($"nx:{nx}, rWidth:{rWidth}");
			bool m, s;
			if (SuperView == null || SuperView.GetType () != typeof (Toplevel)) {
				m = Application.Top.MenuBar != null;
			} else {
				m = ((Toplevel)SuperView).MenuBar != null;
			}
			if (SuperView == null || SuperView is Toplevel) {
				l = m ? 1 : 0;
			} else {
				l = 0;
			}
			ny = Math.Max (y, l);
			if (SuperView == null || SuperView.GetType () != typeof (Toplevel)) {
				s = Application.Top.StatusBar != null && Application.Top.StatusBar.Visible;
			} else {
				s = ((Toplevel)SuperView).StatusBar != null && ((Toplevel)SuperView).StatusBar.Visible;
			}
			if (SuperView == null || SuperView is Toplevel) {
				l = s ? Driver.Rows - 1 : Driver.Rows;
			} else {
				l = s ? SuperView.Frame.Height - 1 : SuperView.Frame.Height;
			}
			ny = Math.Min (ny, l);
			ny = ny + top.Frame.Height > l ? Math.Max (l - top.Frame.Height, m ? 1 : 0) : ny;
			SetHeight (top.Frame.Height, out int rHeight);
			if (rHeight < 0 && ny >= top.Frame.Y) {
				ny = Math.Max (top.Frame.Bottom - 2, 0);
			}
			//System.Diagnostics.Debug.WriteLine ($"ny:{ny}, rHeight:{rHeight}");
		}

		internal void PositionToplevels ()
		{
			PositionToplevel (this);
			foreach (var top in Subviews) {
				if (top is Toplevel) {
					PositionToplevel ((Toplevel)top);
				}
			}
		}

		private void PositionToplevel (Toplevel top)
		{
			EnsureVisibleBounds (top, top.Frame.X, top.Frame.Y, out int nx, out int ny);
			if ((nx != top.Frame.X || ny != top.Frame.Y) && top.LayoutStyle == LayoutStyle.Computed) {
				if ((top.X == null || top.X is Pos.PosAbsolute) && top.Bounds.X != nx) {
					top.X = nx;
				}
				if ((top.Y == null || top.Y is Pos.PosAbsolute) && top.Bounds.Y != ny) {
					top.Y = ny;
				}
			}
			if (top.StatusBar != null) {
				if (ny + top.Frame.Height > top.Frame.Height - (top.StatusBar.Visible ? 1 : 0)) {
					if (top.Height is Dim.DimFill)
						top.Height = Dim.Fill () - (top.StatusBar.Visible ? 1 : 0);
				}
				if (top.StatusBar.Frame.Y != top.Frame.Height - (top.StatusBar.Visible ? 1 : 0)) {
					top.StatusBar.Y = top.Frame.Height - (top.StatusBar.Visible ? 1 : 0);
					top.LayoutSubviews ();
				}
				top.BringSubviewToFront (top.StatusBar);
			}
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			if (IsCurrentTop || this == Application.Top) {
				if (!NeedDisplay.IsEmpty || LayoutNeeded) {
					Driver.SetAttribute (Colors.TopLevel.Normal);

					// This is the Application.Top. Clear just the region we're being asked to redraw 
					// (the bounds passed to us).
					Clear (bounds);
					Driver.SetAttribute (Colors.Base.Normal);
					PositionToplevels ();

					foreach (var view in Subviews) {
						if (view.Frame.IntersectsWith (bounds)) {
							view.SetNeedsLayout ();
							view.SetNeedsDisplay (view.Bounds);
						}
					}

					ClearLayoutNeeded ();
					ClearNeedsDisplay ();
				}
			}

			base.Redraw (base.Bounds);
		}

		/// <summary>
		/// Invoked by <see cref="Application.Begin"/> as part of the <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> after
		/// the views have been laid out, and before the views are drawn for the first time.
		/// </summary>
		public virtual void WillPresent ()
		{
			FocusFirst ();
		}
	}
}
