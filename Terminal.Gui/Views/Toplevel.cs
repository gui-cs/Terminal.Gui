using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui;

/// <summary>
/// Toplevel views are used for both an application's main view (filling the entire screen and
/// for modal (pop-up) views such as <see cref="Dialog"/>, <see cref="MessageBox"/>, and
/// <see cref="Wizard"/>).
/// </summary>
/// <remarks>
///         <para>
///         Toplevels can run as modal (popup) views, started by calling
///         <see cref="Application.Run(Toplevel, Func{Exception,bool})"/>.
///         They return control to the caller when <see cref="Application.RequestStop(Toplevel)"/> has
///         been called (which sets the <see cref="Toplevel.Running"/> property to <c>false</c>).
///         </para>
///         <para>
///         A Toplevel is created when an application initializes Terminal.Gui by calling
///         <see cref="Application.Init"/>.
///         The application Toplevel can be accessed via <see cref="Application.Top"/>. Additional
///         Toplevels can be created
///         and run (e.g. <see cref="Dialog"/>s. To run a Toplevel, create the <see cref="Toplevel"/> and
///         call <see cref="Application.Run(Toplevel, Func{Exception,bool})"/>.
///         </para>
/// </remarks>
public partial class Toplevel : View {

	/// <summary>
	/// Initializes a new instance of the <see cref="Toplevel"/> class with the specified
	/// <see cref="LayoutStyle.Absolute"/> layout.
	/// </summary>
	/// <param name="frame">
	/// A Superview-relative rectangle specifying the location and size for the new
	/// Toplevel
	/// </param>
	public Toplevel (Rect frame) : base (frame) => SetInitialProperties ();

	/// <summary>
	/// Initializes a new instance of the <see cref="Toplevel"/> class with
	/// <see cref="LayoutStyle.Computed"/> layout, defaulting to full screen. The <see cref="View.Width"/> and
	/// <see cref="View.Height"/> properties
	/// will be set to the dimensions of the terminal using <see cref="Dim.Fill"/>.
	/// </summary>
	public Toplevel ()
	{
		SetInitialProperties ();
		Width = Dim.Fill ();
		Height = Dim.Fill ();
	}

	/// <summary>
	/// Gets or sets whether the main loop for this <see cref="Toplevel"/> is running or not.
	/// </summary>
	/// <remarks>
	/// Setting this property directly is discouraged. Use <see cref="Application.RequestStop"/>
	/// instead.
	/// </remarks>
	public bool Running { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="Toplevel"/> can focus.
	/// </summary>
	/// <value><c>true</c> if can focus; otherwise, <c>false</c>.</value>
	public override bool CanFocus => SuperView == null ? true : base.CanFocus;

	/// <summary>
	/// Determines whether the <see cref="Toplevel"/> is modal or not.
	/// If set to <c>false</c> (the default):
	/// <list type="bullet">
	///         <item>
	///                 <description><see cref="View.OnKeyDown"/> events will propagate keys upwards.</description>
	///         </item>
	///         <item>
	///                 <description>The Toplevel will act as an embedded view (not a modal/pop-up).</description>
	///         </item>
	/// </list>
	/// If set to <c>true</c>:
	/// <list type="bullet">
	///         <item>
	///                 <description><see cref="View.OnKeyDown"/> events will NOT propagate keys upwards.</description>
	///         </item>
	///         <item>
	///                 <description>
	///                 The Toplevel will and look like a modal (pop-up) (e.g. see
	///                 <see cref="Dialog"/>.
	///                 </description>
	///         </item>
	/// </list>
	/// </summary>
	public bool Modal { get; set; }

	/// <summary>
	/// Gets or sets the menu for this Toplevel.
	/// </summary>
	public virtual MenuBar MenuBar { get; set; }

	/// <summary>
	/// Gets or sets the status bar for this Toplevel.
	/// </summary>
	public virtual StatusBar StatusBar { get; set; }

	/// <summary>
	/// <see langword="true"/> if was already loaded by the <see cref="Application.Begin(Toplevel)"/>
	/// <see langword="false"/>, otherwise.
	/// </summary>
	public bool IsLoaded { get; private set; }

	/// <summary>
	/// Invoked when the <see cref="Toplevel"/> <see cref="RunState"/> has begun to be loaded.
	/// A Loaded event handler is a good place to finalize initialization before calling
	/// <see cref="Application.RunLoop(RunState)"/>.
	/// </summary>
	public event EventHandler Loaded;

	/// <summary>
	/// Invoked when the <see cref="Toplevel"/> main loop has started it's first iteration.
	/// Subscribe to this event to perform tasks when the <see cref="Toplevel"/> has been laid out and
	/// focus has been set.
	/// changes.
	/// <para>
	/// A Ready event handler is a good place to finalize initialization after calling
	/// <see cref="Application.Run(Func{Exception, bool})"/> on this <see cref="Toplevel"/>.
	/// </para>
	/// </summary>
	public event EventHandler Ready;

	/// <summary>
	/// Invoked when the Toplevel <see cref="RunState"/> has been unloaded.
	/// A Unloaded event handler is a good place to dispose objects after calling
	/// <see cref="Application.End(RunState)"/>.
	/// </summary>
	public event EventHandler Unloaded;

	/// <summary>
	/// Invoked when the Toplevel <see cref="RunState"/> becomes the <see cref="Application.Current"/>
	/// Toplevel.
	/// </summary>
	public event EventHandler<ToplevelEventArgs> Activate;

	/// <summary>
	/// Invoked when the Toplevel<see cref="RunState"/> ceases to be the <see cref="Application.Current"/>
	/// Toplevel.
	/// </summary>
	public event EventHandler<ToplevelEventArgs> Deactivate;

	/// <summary>
	/// Invoked when a child of the Toplevel <see cref="RunState"/> is closed by
	/// <see cref="Application.End(RunState)"/>.
	/// </summary>
	public event EventHandler<ToplevelEventArgs> ChildClosed;

	/// <summary>
	/// Invoked when the last child of the Toplevel <see cref="RunState"/> is closed from
	/// by <see cref="Application.End(RunState)"/>.
	/// </summary>
	public event EventHandler AllChildClosed;

	/// <summary>
	/// Invoked when the Toplevel's <see cref="RunState"/> is being closed by
	/// <see cref="Application.RequestStop(Toplevel)"/>.
	/// </summary>
	public event EventHandler<ToplevelClosingEventArgs> Closing;

	/// <summary>
	/// Invoked when the Toplevel's <see cref="RunState"/> is closed by
	/// <see cref="Application.End(RunState)"/>.
	/// </summary>
	public event EventHandler<ToplevelEventArgs> Closed;

	/// <summary>
	/// Invoked when a child Toplevel's <see cref="RunState"/> has been loaded.
	/// </summary>
	public event EventHandler<ToplevelEventArgs> ChildLoaded;

	/// <summary>
	/// Invoked when a cjhild Toplevel's <see cref="RunState"/> has been unloaded.
	/// </summary>
	public event EventHandler<ToplevelEventArgs> ChildUnloaded;

	/// <summary>
	/// Invoked when the terminal has been resized. The new <see cref="Size"/> of the terminal is provided.
	/// </summary>
	public event EventHandler<SizeChangedEventArgs> SizeChanging;

	// TODO: Make cancelable?
	internal virtual void OnSizeChanging (SizeChangedEventArgs size) => SizeChanging?.Invoke (this, size);

	internal virtual void OnChildUnloaded (Toplevel top) => ChildUnloaded?.Invoke (this, new ToplevelEventArgs (top));

	internal virtual void OnChildLoaded (Toplevel top) => ChildLoaded?.Invoke (this, new ToplevelEventArgs (top));

	internal virtual void OnClosed (Toplevel top) => Closed?.Invoke (this, new ToplevelEventArgs (top));

	internal virtual bool OnClosing (ToplevelClosingEventArgs ev)
	{
		Closing?.Invoke (this, ev);
		return ev.Cancel;
	}

	internal virtual void OnAllChildClosed () => AllChildClosed?.Invoke (this, EventArgs.Empty);

	internal virtual void OnChildClosed (Toplevel top)
	{
		if (IsOverlappedContainer) {
			SetSubViewNeedsDisplay ();
		}
		ChildClosed?.Invoke (this, new ToplevelEventArgs (top));
	}

	internal virtual void OnDeactivate (Toplevel activated) => Deactivate?.Invoke (this, new ToplevelEventArgs (activated));

	internal virtual void OnActivate (Toplevel deactivated) => Activate?.Invoke (this, new ToplevelEventArgs (deactivated));

	/// <summary>
	/// Called from <see cref="Application.Begin(Toplevel)"/> before the <see cref="Toplevel"/> redraws for
	/// the first time.
	/// </summary>
	public virtual void OnLoaded ()
	{
		IsLoaded = true;
		foreach (Toplevel tl in Subviews.Where (v => v is Toplevel)) {
			tl.OnLoaded ();
		}
		Loaded?.Invoke (this, EventArgs.Empty);
	}

	/// <summary>
	/// Called from <see cref="Application.RunLoop"/> after the <see cref="Toplevel"/> has entered the
	/// first iteration of the loop.
	/// </summary>
	internal virtual void OnReady ()
	{
		foreach (Toplevel tl in Subviews.Where (v => v is Toplevel)) {
			tl.OnReady ();
		}
		Ready?.Invoke (this, EventArgs.Empty);
	}

	/// <summary>
	/// Called from <see cref="Application.End(RunState)"/> before the <see cref="Toplevel"/> is disposed.
	/// </summary>
	internal virtual void OnUnloaded ()
	{
		foreach (Toplevel tl in Subviews.Where (v => v is Toplevel)) {
			tl.OnUnloaded ();
		}
		Unloaded?.Invoke (this, EventArgs.Empty);
	}

	void SetInitialProperties ()
	{
		ColorScheme = Colors.ColorSchemes ["TopLevel"];

		Application.GrabbingMouse += Application_GrabbingMouse;
		Application.UnGrabbingMouse += Application_UnGrabbingMouse;

		// TODO: v2 - ALL Views (Responders??!?!) should support the commands related to 
		//    - Focus
		//  Move the appropriate AddCommand calls to `Responder`

		// Things this view knows how to do
		AddCommand (Command.QuitToplevel, () => {
			QuitToplevel ();
			return true;
		});
		AddCommand (Command.Suspend, () => {
			Driver.Suspend ();
			;
			return true;
		});
		AddCommand (Command.NextView, () => {
			MoveNextView ();
			return true;
		});
		AddCommand (Command.PreviousView, () => {
			MovePreviousView ();
			return true;
		});
		AddCommand (Command.NextViewOrTop, () => {
			MoveNextViewOrTop ();
			return true;
		});
		AddCommand (Command.PreviousViewOrTop, () => {
			MovePreviousViewOrTop ();
			return true;
		});
		AddCommand (Command.Refresh, () => {
			Application.Refresh ();
			return true;
		});


		// Default keybindings for this view
		KeyBindings.Add (Application.QuitKey, Command.QuitToplevel);

		KeyBindings.Add (Key.CursorRight, Command.NextView);
		KeyBindings.Add (Key.CursorDown, Command.NextView);
		KeyBindings.Add (Key.CursorLeft, Command.PreviousView);
		KeyBindings.Add (Key.CursorUp, Command.PreviousView);

		KeyBindings.Add (Key.Tab, Command.NextView);
		KeyBindings.Add (Key.Tab.WithShift, Command.PreviousView);
		KeyBindings.Add (Key.Tab.WithCtrl, Command.NextViewOrTop);
		KeyBindings.Add (Key.Tab.WithShift.WithCtrl, Command.PreviousViewOrTop);

		KeyBindings.Add (Key.F5, Command.Refresh);
		KeyBindings.Add (Application.AlternateForwardKey, Command.NextViewOrTop); // Needed on Unix
		KeyBindings.Add (Application.AlternateBackwardKey, Command.PreviousViewOrTop); // Needed on Unix

#if UNIX_KEY_BINDINGS
			KeyBindings.Add (Key.Z.WithCtrl, Command.Suspend);
			KeyBindings.Add (Key.L.WithCtrl, Command.Refresh);// Unix
			KeyBindings.Add (Key.F.WithCtrl, Command.NextView);// Unix
			KeyBindings.Add (Key.I.WithCtrl, Command.NextView); // Unix
			KeyBindings.Add (Key.B.WithCtrl, Command.PreviousView);// Unix
#endif
	}

	/// <summary>
	/// Invoked when the <see cref="Application.AlternateForwardKey"/> is changed.
	/// </summary>
	public event EventHandler<KeyChangedEventArgs> AlternateForwardKeyChanged;

	/// <summary>
	/// Virtual method to invoke the <see cref="AlternateForwardKeyChanged"/> event.
	/// </summary>
	/// <param name="e"></param>
	public virtual void OnAlternateForwardKeyChanged (KeyChangedEventArgs e)
	{
		KeyBindings.Replace (e.OldKey, e.NewKey);
		AlternateForwardKeyChanged?.Invoke (this, e);
	}

	/// <summary>
	/// Invoked when the <see cref="Application.AlternateBackwardKey"/> is changed.
	/// </summary>
	public event EventHandler<KeyChangedEventArgs> AlternateBackwardKeyChanged;

	/// <summary>
	/// Virtual method to invoke the <see cref="AlternateBackwardKeyChanged"/> event.
	/// </summary>
	/// <param name="e"></param>
	public virtual void OnAlternateBackwardKeyChanged (KeyChangedEventArgs e)
	{
		KeyBindings.Replace (e.OldKey, e.NewKey);
		AlternateBackwardKeyChanged?.Invoke (this, e);
	}

	/// <summary>
	/// Invoked when the <see cref="Application.QuitKey"/> is changed.
	/// </summary>
	public event EventHandler<KeyChangedEventArgs> QuitKeyChanged;

	/// <summary>
	/// Virtual method to invoke the <see cref="QuitKeyChanged"/> event.
	/// </summary>
	/// <param name="e"></param>
	public virtual void OnQuitKeyChanged (KeyChangedEventArgs e)
	{
		KeyBindings.Replace (e.OldKey, e.NewKey);
		QuitKeyChanged?.Invoke (this, e);
	}

	void MovePreviousViewOrTop ()
	{
		if (Application.OverlappedTop == null) {
			var top = Modal ? this : Application.Top;
			top.FocusPrev ();
			if (top.Focused == null) {
				top.FocusPrev ();
			}
			top.SetNeedsDisplay ();
			Application.BringOverlappedTopToFront ();
		} else {
			Application.OverlappedMovePrevious ();
		}
	}

	void MoveNextViewOrTop ()
	{
		if (Application.OverlappedTop == null) {
			var top = Modal ? this : Application.Top;
			top.FocusNext ();
			if (top.Focused == null) {
				top.FocusNext ();
			}
			top.SetNeedsDisplay ();
			Application.BringOverlappedTopToFront ();
		} else {
			Application.OverlappedMoveNext ();
		}
	}

	void MovePreviousView ()
	{
		var old = GetDeepestFocusedSubview (Focused);
		if (!FocusPrev ()) {
			FocusPrev ();
		}
		if (old != Focused && old != Focused?.Focused) {
			old?.SetNeedsDisplay ();
			Focused?.SetNeedsDisplay ();
		} else {
			FocusNearestView (SuperView?.TabIndexes?.Reverse (), Direction.Backward);
		}
	}

	void MoveNextView ()
	{
		var old = GetDeepestFocusedSubview (Focused);
		if (!FocusNext ()) {
			FocusNext ();
		}
		if (old != Focused && old != Focused?.Focused) {
			old?.SetNeedsDisplay ();
			Focused?.SetNeedsDisplay ();
		} else {
			FocusNearestView (SuperView?.TabIndexes, Direction.Forward);
		}
	}

	void QuitToplevel ()
	{
		if (Application.OverlappedTop != null) {
			Application.OverlappedTop.RequestStop ();
		} else {
			Application.RequestStop ();
		}
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
		CanFocus = true;
		AddMenuStatusBar (view);
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
		if (this is Toplevel Toplevel && Toplevel.MenuBar != null) {
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

	/// <summary>
	///  Gets a new location of the <see cref="Toplevel"/> that is within the Bounds of the <paramref name="top"/>'s 
	///  <see cref="View.SuperView"/> (e.g. for dragging a Window).
	///  The `out` parameters are the new X and Y coordinates.
	/// </summary>
	/// <remarks>
	/// If <paramref name="top"/> does not have a <see cref="View.SuperView"/> or it's SuperView is not <see cref="Application.Top"/>
	/// the position will be bound by the <see cref="ConsoleDriver.Cols"/> and <see cref="ConsoleDriver.Rows"/>.
	/// </remarks>
	/// <param name="top">The Toplevel that is to be moved.</param>
	/// <param name="targetX">The target x location.</param>
	/// <param name="targetY">The target y location.</param>
	/// <param name="nx">The x location that will ensure <paramref name="top"/> will be visible.</param>
	/// <param name="ny">The y location that will ensure <paramref name="top"/> will be visible.</param>
	/// <param name="menuBar">The new top most menuBar</param>
	/// <param name="statusBar">The new top most statusBar</param>
	/// <returns>
	///  Either <see cref="Application.Top"/> (if <paramref name="top"/> does not have a Super View) or
	///  <paramref name="top"/>'s SuperView. This can be used to ensure LayoutSubviews is called on the correct View.
	///  </returns>
	internal View GetLocationThatFits (Toplevel top, int targetX, int targetY,
					out int nx, out int ny, out MenuBar menuBar, out StatusBar statusBar)
	{
		int maxWidth;
		View superView;
		if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
			maxWidth = Driver.Cols;
			superView = Application.Top;
		} else {
			// Use the SuperView's Bounds, not Frame
			maxWidth = top.SuperView.Bounds.Width;
			superView = top.SuperView;
		}
		if (superView.Margin != null && superView == top.SuperView) {
			maxWidth -= superView.GetAdornmentsThickness ().Left + superView.GetAdornmentsThickness ().Right;
		}
		if (top.Frame.Width <= maxWidth) {
			nx = Math.Max (targetX, 0);
			nx = nx + top.Frame.Width > maxWidth ? Math.Max (maxWidth - top.Frame.Width, 0) : nx;
			if (nx > top.Frame.X + top.Frame.Width) {
				nx = Math.Max (top.Frame.Right, 0);
			}
		} else {
			nx = targetX;
		}
		//System.Diagnostics.Debug.WriteLine ($"nx:{nx}, rWidth:{rWidth}");
		bool menuVisible, statusVisible;
		if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
			menuVisible = Application.Top.MenuBar?.Visible == true;
			menuBar = Application.Top.MenuBar;
		} else {
			var t = top.SuperView;
			while (t is not Toplevel) {
				t = t.SuperView;
			}
			menuVisible = ((Toplevel)t).MenuBar?.Visible == true;
			menuBar = ((Toplevel)t).MenuBar;
		}
		if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
			maxWidth = menuVisible ? 1 : 0;
		} else {
			maxWidth = 0;
		}
		ny = Math.Max (targetY, maxWidth);
		if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
			statusVisible = Application.Top.StatusBar?.Visible == true;
			statusBar = Application.Top.StatusBar;
		} else {
			var t = top.SuperView;
			while (t is not Toplevel) {
				t = t.SuperView;
			}
			statusVisible = ((Toplevel)t).StatusBar?.Visible == true;
			statusBar = ((Toplevel)t).StatusBar;
		}
		if (top?.SuperView == null || top == Application.Top || top?.SuperView == Application.Top) {
			maxWidth = statusVisible ? Driver.Rows - 1 : Driver.Rows;
		} else {
			maxWidth = statusVisible ? top.SuperView.Frame.Height - 1 : top.SuperView.Frame.Height;
		}
		if (superView.Margin != null && superView == top.SuperView) {
			maxWidth -= superView.GetAdornmentsThickness ().Top + superView.GetAdornmentsThickness ().Bottom;
		}
		ny = Math.Min (ny, maxWidth);
		if (top.Frame.Height <= maxWidth) {
			ny = ny + top.Frame.Height > maxWidth ? Math.Max (maxWidth - top.Frame.Height, menuVisible ? 1 : 0) : ny;
			if (ny > top.Frame.Y + top.Frame.Height) {
				ny = Math.Max (top.Frame.Bottom, 0);
			}
		}
		//System.Diagnostics.Debug.WriteLine ($"ny:{ny}, rHeight:{rHeight}");

		return superView;
	}

	// TODO: v2 - Not sure this is needed anymore.
	internal void PositionToplevels ()
	{
		PositionToplevel (this);
		foreach (var top in Subviews) {
			if (top is Toplevel) {
				PositionToplevel ((Toplevel)top);
			}
		}
	}

	/// <summary>
	/// Adjusts the location and size of <paramref name="top"/> within this Toplevel.
	/// Virtual method enabling implementation of specific positions for inherited <see cref="Toplevel"/> views.
	/// </summary>
	/// <param name="top">The Toplevel to adjust.</param>
	public virtual void PositionToplevel (Toplevel top)
	{
		var superView = GetLocationThatFits (top, top.Frame.X, top.Frame.Y,
			out int nx, out int ny, out _, out var sb);
		bool layoutSubviews = false;
		int maxWidth = 0;
		if (superView.Margin != null && superView == top.SuperView) {
			maxWidth -= superView.GetAdornmentsThickness ().Left + superView.GetAdornmentsThickness ().Right;
		}
		if ((superView != top || top?.SuperView != null || top != Application.Top && top.Modal || top?.SuperView == null && top.IsOverlapped)
		    // BUGBUG: Prevously PositionToplevel required LayotuStyle.Computed
		    &&
		    (top.Frame.X + top.Frame.Width > maxWidth || ny > top.Frame.Y) /*&& top.LayoutStyle == LayoutStyle.Computed*/) {

			if ((top.X == null || top.X is Pos.PosAbsolute) && top.Frame.X != nx) {
				top.X = nx;
				layoutSubviews = true;
			}
			if ((top.Y == null || top.Y is Pos.PosAbsolute) && top.Frame.Y != ny) {
				top.Y = ny;
				layoutSubviews = true;
			}
		}

		// TODO: v2 - This is a hack to get the StatusBar to be positioned correctly.
		if (sb != null && !top.Subviews.Contains (sb) && ny + top.Frame.Height != superView.Frame.Height - (sb.Visible ? 1 : 0) && top.Height is Dim.DimFill && -top.Height.Anchor (0) < 1) {

			top.Height = Dim.Fill (sb.Visible ? 1 : 0);
			layoutSubviews = true;
		}

		if (superView.LayoutNeeded || layoutSubviews) {
			superView.LayoutSubviews ();
		}
		if (LayoutNeeded) {
			LayoutSubviews ();
		}
	}

	///<inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		if (!Visible) {
			return;
		}

		if (NeedsDisplay || SubViewNeedsDisplay || LayoutNeeded) {
			//Driver.SetAttribute (GetNormalColor ());
			// TODO: It's bad practice for views to always clear. Defeats the purpose of clipping etc...
			Clear ();
			LayoutSubviews ();
			PositionToplevels ();

			if (this == Application.OverlappedTop) {
				foreach (var top in Application.OverlappedChildren.AsEnumerable ().Reverse ()) {
					if (top.Frame.IntersectsWith (Bounds)) {
						if (top != this && !top.IsCurrentTop && !OutsideTopFrame (top) && top.Visible) {
							top.SetNeedsLayout ();
							top.SetNeedsDisplay (top.Bounds);
							top.Draw ();
							top.OnRenderLineCanvas ();
						}
					}
				}
			}

			// This should not be here, but in base
			foreach (var view in Subviews) {
				if (view.Frame.IntersectsWith (Bounds) && !OutsideTopFrame (this)) {
					//view.SetNeedsLayout ();
					view.SetNeedsDisplay (view.Bounds);
					view.SetSubViewNeedsDisplay ();
				}
			}

			base.OnDrawContent (contentArea);

			// This is causing the menus drawn incorrectly if UseSubMenusSingleFrame is true
			//if (this.MenuBar != null && this.MenuBar.IsMenuOpen && this.MenuBar.openMenu != null) {
			//	// TODO: Hack until we can get compositing working right.
			//	this.MenuBar.openMenu.Redraw (this.MenuBar.openMenu.Bounds);
			//}
		}
	}

	bool OutsideTopFrame (Toplevel top)
	{
		if (top.Frame.X > Driver.Cols || top.Frame.Y > Driver.Rows) {
			return true;
		}
		return false;
	}

	internal static Point? _dragPosition;
	Point _startGrabPoint;

	void Application_UnGrabbingMouse (object sender, GrabMouseEventArgs e)
	{
		if (Application.MouseGrabView == this && _dragPosition.HasValue) {
			e.Cancel = true;
		}
	}

	void Application_GrabbingMouse (object sender, GrabMouseEventArgs e)
	{
		if (Application.MouseGrabView == this && _dragPosition.HasValue) {
			e.Cancel = true;
		}
	}

	///<inheritdoc/>
	public override bool MouseEvent (MouseEvent mouseEvent)
	{
		if (!CanFocus) {
			return true;
		}

		//System.Diagnostics.Debug.WriteLine ($"dragPosition before: {dragPosition.HasValue}");

		int nx, ny;
		if (!_dragPosition.HasValue && (mouseEvent.Flags == MouseFlags.Button1Pressed
						|| mouseEvent.Flags == MouseFlags.Button2Pressed
						|| mouseEvent.Flags == MouseFlags.Button3Pressed)) {

			SetFocus ();
			Application.BringOverlappedTopToFront ();

			// Only start grabbing if the user clicks on the title bar.
			// BUGBUG: Assumes Frame == Border and Title is always at Y == 0
			if (mouseEvent.Y == 0 && mouseEvent.Flags == MouseFlags.Button1Pressed) {
				_startGrabPoint = new Point (mouseEvent.X, mouseEvent.Y);
				_dragPosition = new Point ();
				nx = mouseEvent.X - mouseEvent.OfX;
				ny = mouseEvent.Y - mouseEvent.OfY;
				_dragPosition = new Point (nx, ny);
				Application.GrabMouse (this);
			}

			//System.Diagnostics.Debug.WriteLine ($"Starting at {dragPosition}");
			return true;
		} else if (mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) ||
			mouseEvent.Flags == MouseFlags.Button3Pressed) {
			if (_dragPosition.HasValue) {
				if (SuperView == null) {
					// Redraw the entire app window using just our Frame. Since we are 
					// Application.Top, and our Frame always == our Bounds (Location is always (0,0))
					// our Frame is actually view-relative (which is what Redraw takes).
					// We need to pass all the view bounds because since the windows was 
					// moved around, we don't know exactly what was the affected region.
					Application.Top.SetNeedsDisplay ();
				} else {
					SuperView.SetNeedsDisplay ();
				}
				// BUGBUG: Assumes Frame == Border?
				GetLocationThatFits (this, mouseEvent.X + (SuperView == null ? mouseEvent.OfX - _startGrabPoint.X : Frame.X - _startGrabPoint.X),
					mouseEvent.Y + (SuperView == null ? mouseEvent.OfY - _startGrabPoint.Y : Frame.Y - _startGrabPoint.Y),
					out nx, out ny, out _, out _);

				_dragPosition = new Point (nx, ny);
				X = nx;
				Y = ny;
				//System.Diagnostics.Debug.WriteLine ($"Drag: nx:{nx},ny:{ny}");

				SetNeedsDisplay ();
				return true;
			}
		}

		if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && _dragPosition.HasValue) {
			_dragPosition = null;
			Application.UngrabMouse ();
		}

		//System.Diagnostics.Debug.WriteLine ($"dragPosition after: {dragPosition.HasValue}");
		//System.Diagnostics.Debug.WriteLine ($"Toplevel: {mouseEvent}");
		return false;
	}

	/// <summary>
	/// Stops and closes this <see cref="Toplevel"/>. If this Toplevel is the top-most Toplevel, 
	/// <see cref="Application.RequestStop(Toplevel)"/> will be called, causing the application to exit.
	/// </summary>
	public virtual void RequestStop ()
	{
		if (IsOverlappedContainer && Running
					&& (Application.Current == this
					|| Application.Current?.Modal == false
					|| Application.Current?.Modal == true && Application.Current?.Running == false)) {

			foreach (var child in Application.OverlappedChildren) {
				var ev = new ToplevelClosingEventArgs (this);
				if (child.OnClosing (ev)) {
					return;
				}
				child.Running = false;
				Application.RequestStop (child);
			}
			Running = false;
			Application.RequestStop (this);
		} else if (IsOverlappedContainer && Running && Application.Current?.Modal == true && Application.Current?.Running == true) {
			var ev = new ToplevelClosingEventArgs (Application.Current);
			if (OnClosing (ev)) {
				return;
			}
			Application.RequestStop (Application.Current);
		} else if (!IsOverlappedContainer && Running && (!Modal || Modal && Application.Current != this)) {
			var ev = new ToplevelClosingEventArgs (this);
			if (OnClosing (ev)) {
				return;
			}
			Running = false;
			Application.RequestStop (this);
		} else {
			Application.RequestStop (Application.Current);
		}
	}

	/// <summary>
	/// Stops and closes the <see cref="Toplevel"/> specified by <paramref name="top"/>. If <paramref name="top"/> is the top-most Toplevel, 
	/// <see cref="Application.RequestStop(Toplevel)"/> will be called, causing the application to exit.
	/// </summary>
	/// <param name="top">The Toplevel to request stop.</param>
	public virtual void RequestStop (Toplevel top) => top.RequestStop ();

	///<inheritdoc/>
	public override void PositionCursor ()
	{
		if (!IsOverlappedContainer) {
			base.PositionCursor ();
			if (Focused == null) {
				EnsureFocus ();
				if (Focused == null) {
					Driver.SetCursorVisibility (CursorVisibility.Invisible);
				}
			}
			return;
		}

		if (Focused == null) {
			foreach (var top in Application.OverlappedChildren) {
				if (top != this && top.Visible) {
					top.SetFocus ();
					return;
				}
			}
		}
		base.PositionCursor ();
		if (Focused == null) {
			Driver.SetCursorVisibility (CursorVisibility.Invisible);
		}
	}

	///<inheritdoc/>
	public override bool OnEnter (View view) => MostFocused?.OnEnter (view) ?? base.OnEnter (view);

	///<inheritdoc/>
	public override bool OnLeave (View view) => MostFocused?.OnLeave (view) ?? base.OnLeave (view);

	///<inheritdoc/>
	protected override void Dispose (bool disposing)
	{
		Application.GrabbingMouse -= Application_GrabbingMouse;
		Application.UnGrabbingMouse -= Application_UnGrabbingMouse;

		_dragPosition = null;
		base.Dispose (disposing);
	}
}
/// <summary>
/// Implements the <see cref="IEqualityComparer{T}"/> for comparing two <see cref="Toplevel"/>s
/// used by <see cref="StackExtensions"/>.
/// </summary>
public class ToplevelEqualityComparer : IEqualityComparer<Toplevel> {
	/// <summary>Determines whether the specified objects are equal.</summary>
	/// <param name="x">The first object of type <see cref="Toplevel" /> to compare.</param>
	/// <param name="y">The second object of type <see cref="Toplevel" /> to compare.</param>
	/// <returns>
	///     <see langword="true" /> if the specified objects are equal; otherwise, <see langword="false" />.</returns>
	public bool Equals (Toplevel x, Toplevel y)
	{
		if (y == null && x == null) {
			return true;
		} else if (x == null || y == null) {
			return false;
		} else if (x.Id == y.Id) {
			return true;
		}
		return false;
	}

	/// <summary>Returns a hash code for the specified object.</summary>
	/// <param name="obj">The <see cref="Toplevel" /> for which a hash code is to be returned.</param>
	/// <returns>A hash code for the specified object.</returns>
	/// <exception cref="ArgumentNullException">The type of <paramref name="obj" /> 
	/// is a reference type and <paramref name="obj" /> is <see langword="null" />.</exception>
	public int GetHashCode (Toplevel obj)
	{
		if (obj == null) {
			throw new ArgumentNullException ();
		}

		int hCode = 0;
		if (int.TryParse (obj.Id, out int result)) {
			hCode = result;
		}
		return hCode.GetHashCode ();
	}
}
/// <summary>
/// Implements the <see cref="IComparer{T}"/> to sort the <see cref="Toplevel"/> 
/// from the <see cref="Application.OverlappedChildren"/> if needed.
/// </summary>
public sealed class ToplevelComparer : IComparer<Toplevel> {
	/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.</summary>
	/// <param name="x">The first object to compare.</param>
	/// <param name="y">The second object to compare.</param>
	/// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero
	///             <paramref name="x" /> is less than <paramref name="y" />.Zero
	///             <paramref name="x" /> equals <paramref name="y" />.Greater than zero
	///             <paramref name="x" /> is greater than <paramref name="y" />.</returns>
	public int Compare (Toplevel x, Toplevel y)
	{
		if (ReferenceEquals (x, y)) {
			return 0;
		}
		if (x == null) {
			return -1;
		}
		if (y == null) {
			return 1;
		}
		return string.Compare (x.Id, y.Id);
	}
}