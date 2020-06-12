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
using System.ComponentModel;

namespace Terminal.Gui {

	/// <summary>
	/// A static, singelton class provding the main application driver for Terminal.Gui apps. 
	/// </summary>
	/// <example>
	/// <code>
	/// // A simple Terminal.Gui app that creates a window with a frame and title with 
	/// // 5 rows/columns of padding.
	/// Application.Init();
	/// var win = new Window ("Hello World - CTRL-Q to quit") {
	///     X = 5,
	///     Y = 5,
	///     Width = Dim.Fill (5),
	///     Height = Dim.Fill (5)
	/// };
	/// Application.Top.Add(win);
	/// Application.Run();
	/// </code>
	/// </example>
	/// <remarks>
	///   <para>
	///     Creates a instance of <see cref="Terminal.Gui.MainLoop"/> to process input events, handle timers and
	///     other sources of data. It is accessible via the <see cref="MainLoop"/> property.
	///   </para>
	///   <para>
	///     You can hook up to the <see cref="Iteration"/> event to have your method
	///     invoked on each iteration of the <see cref="Terminal.Gui.MainLoop"/>.
	///   </para>
	///   <para>
	///     When invoked sets the SynchronizationContext to one that is tied
	///     to the mainloop, allowing user code to use async/await.
	///   </para>
	/// </remarks>
	public static class Application {
		/// <summary>
		/// The current <see cref="ConsoleDriver"/> in use.
		/// </summary>
		public static ConsoleDriver Driver;

		/// <summary>
		/// The <see cref="Toplevel"/> object used for the application on startup (<seealso cref="Application.Top"/>)
		/// </summary>
		/// <value>The top.</value>
		public static Toplevel Top { get; private set; }

		/// <summary>
		/// The current <see cref="Toplevel"/> object. This is updated when <see cref="Application.Run()"/> enters and leaves to point to the current <see cref="Toplevel"/> .
		/// </summary>
		/// <value>The current.</value>
		public static Toplevel Current { get; private set; }

		/// <summary>
		/// TThe current <see cref="View"/> object being redrawn.
		/// </summary>
		/// /// <value>The current.</value>
		public static View CurrentView { get; set; }

		/// <summary>
		/// The <see cref="MainLoop"/>  driver for the applicaiton
		/// </summary>
		/// <value>The main loop.</value>
		public static MainLoop MainLoop { get; private set; }

		static Stack<Toplevel> toplevels = new Stack<Toplevel> ();

		/// <summary>
		///   This event is raised on each iteration of the <see cref="MainLoop"/> 
		/// </summary>
		/// <remarks>
		///   See also <see cref="Timeout"/>
		/// </remarks>
		public static Action Iteration;

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
		// provides the sync context set while executing code in Terminal.Gui, to let
		// users use async/await on their code
		//
		class MainLoopSyncContext : SynchronizationContext {
			MainLoop mainLoop;

			public MainLoopSyncContext (MainLoop mainLoop)
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
				//mainLoop.Driver.Wakeup ();
			}

			public override void Send (SendOrPostCallback d, object state)
			{
				mainLoop.Invoke (() => {
					d (state);
				});
			}
		}

		/// <summary>
		/// If set, it forces the use of the System.Console-based driver.
		/// </summary>
		public static bool UseSystemConsole;

		/// <summary>
		/// Initializes a new instance of <see cref="Terminal.Gui"/> Application. 
		/// </summary>
		/// <remarks>
		/// <para>
		/// Call this method once per instance (or after <see cref="Shutdown"/> has been called).
		/// </para>
		/// <para>
		/// Loads the right <see cref="ConsoleDriver"/> for the platform.
		/// </para>
		/// <para>
		/// Creates a <see cref="Toplevel"/> and assigns it to <see cref="Top"/> and <see cref="CurrentView"/>
		/// </para>
		/// </remarks>
		public static void Init (ConsoleDriver driver = null, IMainLoopDriver mainLoopDriver = null) => Init (() => Toplevel.Create (), driver, mainLoopDriver);

		internal static bool _initialized = false;

		/// <summary>
		/// Initializes the Terminal.Gui application
		/// </summary>
		static void Init (Func<Toplevel> topLevelFactory, ConsoleDriver driver = null, IMainLoopDriver mainLoopDriver = null)
		{
			if (_initialized) return;

			// This supports Unit Tests and the passing of a mock driver/loopdriver
			if (driver != null) {
				if (mainLoopDriver == null) {
					throw new ArgumentNullException ("mainLoopDriver cannot be null if driver is provided.");
				}
				Driver = driver;
				Driver.Init (TerminalResized);
				MainLoop = new MainLoop (mainLoopDriver);
				SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext (MainLoop));
			}

			if (Driver == null) {
				var p = Environment.OSVersion.Platform;
				if (UseSystemConsole) {
					mainLoopDriver = new NetMainLoop (() => Console.ReadKey (true));
					Driver = new NetDriver ();
				} else if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows) {
					var windowsDriver = new WindowsDriver ();
					mainLoopDriver = windowsDriver;
					Driver = windowsDriver;
				} else {
					mainLoopDriver = new UnixMainLoop ();
					Driver = new CursesDriver ();
				}
				Driver.Init (TerminalResized);
				MainLoop = new MainLoop (mainLoopDriver);
				SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext (MainLoop));
			}
			Top = topLevelFactory ();
			Current = Top;
			CurrentView = Top;
			_initialized = true;
		}

		/// <summary>
		/// Captures the execution state for the provided <see cref="Toplevel"/>  view.
		/// </summary>
		public class RunState : IDisposable {
			internal bool closeDriver = true;

			/// <summary>
			/// Initializes a new <see cref="RunState"/> class.
			/// </summary>
			/// <param name="view"></param>
			public RunState (Toplevel view)
			{
				Toplevel = view;
			}
			internal Toplevel Toplevel;

			/// <summary>
			/// Releases alTop = l resource used by the <see cref="Application.RunState"/> object.
			/// </summary>
			/// <remarks>Call <see cref="Dispose()"/> when you are finished using the <see cref="Application.RunState"/>. The
			/// <see cref="Dispose()"/> method leaves the <see cref="Application.RunState"/> in an unusable state. After
			/// calling <see cref="Dispose()"/>, you must release all references to the
			/// <see cref="Application.RunState"/> so the garbage collector can reclaim the memory that the
			/// <see cref="Application.RunState"/> was occupying.</remarks>
			public void Dispose ()
			{
				Dispose (closeDriver);
				GC.SuppressFinalize (this);
			}

			/// <summary>
			/// Dispose the specified disposing.
			/// </summary>
			/// <returns>The dispose.</returns>
			/// <param name="disposing">If set to <c>true</c> disposing.</param>
			protected virtual void Dispose (bool disposing)
			{
				if (Toplevel != null) {
					End (Toplevel, disposing);
					Toplevel = null;
				}
			}
		}

		static void ProcessKeyEvent (KeyEvent ke)
		{

			var chain = toplevels.ToList ();
			foreach (var topLevel in chain) {
				if (topLevel.ProcessHotKey (ke))
					return;
				if (topLevel.Modal)
					break;
			}

			foreach (var topLevel in chain) {
				if (topLevel.ProcessKey (ke))
					return;
				if (topLevel.Modal)
					break;
			}

			foreach (var topLevel in chain) {
				// Process the key normally
				if (topLevel.ProcessColdKey (ke))
					return;
				if (topLevel.Modal)
					break;
			}
		}

		static void ProcessKeyDownEvent (KeyEvent ke)
		{
			var chain = toplevels.ToList ();
			foreach (var topLevel in chain) {
				if (topLevel.OnKeyDown (ke))
					return;
				if (topLevel.Modal)
					break;
			}
		}


		static void ProcessKeyUpEvent (KeyEvent ke)
		{
			var chain = toplevels.ToList ();
			foreach (var topLevel in chain) {
				if (topLevel.OnKeyUp (ke))
					return;
				if (topLevel.Modal)
					break;
			}
		}

		static View FindDeepestView (View start, int x, int y, out int resx, out int resy)
		{
			var startFrame = start.Frame;

			if (!startFrame.Contains (x, y)) {
				resx = 0;
				resy = 0;
				return null;
			}

			if (start.InternalSubviews != null) {
				int count = start.InternalSubviews.Count;
				if (count > 0) {
					var rx = x - startFrame.X;
					var ry = y - startFrame.Y;
					for (int i = count - 1; i >= 0; i--) {
						View v = start.InternalSubviews [i];
						if (v.Frame.Contains (rx, ry)) {
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

		internal static View mouseGrabView;

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
			Driver.UncookMouse ();
		}

		/// <summary>
		/// Releases the mouse grab, so mouse events will be routed to the view on which the mouse is.
		/// </summary>
		public static void UngrabMouse ()
		{
			mouseGrabView = null;
			Driver.CookMouse ();
		}

		/// <summary>
		/// Merely a debugging aid to see the raw mouse events
		/// </summary>
		public static Action<MouseEvent> RootMouseEvent;

		internal static View wantContinuousButtonPressedView;
		static View lastMouseOwnerView;

		static void ProcessMouseEvent (MouseEvent me)
		{
			var view = FindDeepestView (Current, me.X, me.Y, out int rx, out int ry);

			if (view != null && view.WantContinuousButtonPressed)
				wantContinuousButtonPressedView = view;
			else
				wantContinuousButtonPressedView = null;

			RootMouseEvent?.Invoke (me);
			if (mouseGrabView != null) {
				var newxy = mouseGrabView.ScreenToView (me.X, me.Y);
				var nme = new MouseEvent () {
					X = newxy.X,
					Y = newxy.Y,
					Flags = me.Flags,
					OfX = me.X - newxy.X,
					OfY = me.Y - newxy.Y,
					View = view
				};
				if (OutsideFrame (new Point (nme.X, nme.Y), mouseGrabView.Frame)) {
					lastMouseOwnerView?.OnMouseLeave (me);
				}
				if (mouseGrabView != null) {
					mouseGrabView.OnMouseEvent (nme);
					return;
				}
			}

			if (view != null) {
				var nme = new MouseEvent () {
					X = rx,
					Y = ry,
					Flags = me.Flags,
					OfX = rx,
					OfY = ry,
					View = view
				};

				if (lastMouseOwnerView == null) {
					lastMouseOwnerView = view;
					view.OnMouseEnter (nme);
				} else if (lastMouseOwnerView != view) {
					lastMouseOwnerView.OnMouseLeave (nme);
					view.OnMouseEnter (nme);
					lastMouseOwnerView = view;
				}

				if (!view.WantMousePositionReports && me.Flags == MouseFlags.ReportMousePosition)
					return;

				if (view.WantContinuousButtonPressed)
					wantContinuousButtonPressedView = view;
				else
					wantContinuousButtonPressedView = null;

				// Should we bubbled up the event, if it is not handled?
				view.OnMouseEvent (nme);
			}
		}

		static bool OutsideFrame (Point p, Rect r)
		{
			return p.X < 0 || p.X > r.Width - 1 || p.Y < 0 || p.Y > r.Height - 1;
		}

		/// <summary>
		/// This event is fired once when the application is first loaded. The dimensions of the
		/// terminal are provided.
		/// </summary>
		public static Action<ResizedEventArgs> Loaded;

		/// <summary>
		/// Building block API: Prepares the provided <see cref="Toplevel"/>  for execution.
		/// </summary>
		/// <returns>The runstate handle that needs to be passed to the <see cref="End(RunState, bool)"/> method upon completion.</returns>
		/// <param name="toplevel">Toplevel to prepare execution for.</param>
		/// <remarks>
		///  This method prepares the provided toplevel for running with the focus,
		///  it adds this to the list of toplevels, sets up the mainloop to process the
		///  event, lays out the subviews, focuses the first element, and draws the
		///  toplevel in the screen. This is usually followed by executing
		///  the <see cref="RunLoop"/> method, and then the <see cref="End(RunState, bool)"/> method upon termination which will
		///   undo these changes.
		/// </remarks>
		public static RunState Begin (Toplevel toplevel)
		{
			if (toplevel == null)
				throw new ArgumentNullException (nameof (toplevel));
			var rs = new RunState (toplevel);

			Init ();
			if (toplevel is ISupportInitializeNotification initializableNotification &&
			    !initializableNotification.IsInitialized) {
				initializableNotification.BeginInit ();
				initializableNotification.EndInit ();
			} else if (toplevel is ISupportInitialize initializable) {
				initializable.BeginInit ();
				initializable.EndInit ();
			}
			toplevels.Push (toplevel);
			Current = toplevel;
			Driver.PrepareToRun (MainLoop, ProcessKeyEvent, ProcessKeyDownEvent, ProcessKeyUpEvent, ProcessMouseEvent);
			if (toplevel.LayoutStyle == LayoutStyle.Computed)
				toplevel.SetRelativeLayout (new Rect (0, 0, Driver.Cols, Driver.Rows));
			toplevel.LayoutSubviews ();
			Loaded?.Invoke (new ResizedEventArgs () { Rows = Driver.Rows, Cols = Driver.Cols });
			toplevel.WillPresent ();
			Redraw (toplevel);
			toplevel.PositionCursor ();
			Driver.Refresh ();

			return rs;
		}

		/// <summary>
		/// Building block API: completes the execution of a <see cref="Toplevel"/>  that was started with <see cref="Begin(Toplevel)"/> .
		/// </summary>
		/// <param name="runState">The runstate returned by the <see cref="Begin(Toplevel)"/> method.</param>
		/// <param name="closeDriver">If <c>true</c>, closes the application. If <c>false</c> closes the toplevels only.</param>
		public static void End (RunState runState, bool closeDriver = true)
		{
			if (runState == null)
				throw new ArgumentNullException (nameof (runState));

			runState.closeDriver = closeDriver;
			runState.Dispose ();
		}

		/// <summary>
		/// Shutdown an application initialized with <see cref="Init(ConsoleDriver, IMainLoopDriver)"/>
		/// </summary>
		/// <param name="closeDriver"><c>true</c>Closes the application.<c>false</c>Closes toplevels only.</param>
		public static void Shutdown (bool closeDriver = true)
		{
			// Shutdown is the bookend for Init. As such it needs to clean up all resources
			// Init created. Apps that do any threading will need to code defensively for this.
			// e.g. see Issue #537
			// TODO: Some of this state is actually related to Begin/End (not Init/Shutdown) and should be moved to `RunState` (#520)
			foreach (var t in toplevels) {
				t.Running = false;
			}
			toplevels.Clear ();
			Current = null;
			CurrentView = null;
			Top = null;

			// Closes the application if it's true.
			if (closeDriver) {
				MainLoop = null;
				Driver?.End ();
				Driver = null;
			}

			_initialized = false;
		}

		static void Redraw (View view)
		{
			Application.CurrentView = view;

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
			Driver.UpdateScreen ();
			View last = null;
			foreach (var v in toplevels.Reverse ()) {
				v.SetNeedsDisplay ();
				v.Redraw (v.Bounds);
				last = v;
			}
			last?.PositionCursor ();
			Driver.Refresh ();
		}

		internal static void End (View view, bool closeDriver = true)
		{
			if (toplevels.Peek () != view)
				throw new ArgumentException ("The view that you end with must be balanced");
			toplevels.Pop ();
			if (toplevels.Count == 0)
				Shutdown (closeDriver);
			else {
				Current = toplevels.Peek ();
				Refresh ();
			}
		}

		/// <summary>
		///   Building block API: Runs the main loop for the created dialog
		/// </summary>
		/// <remarks>
		///   Use the wait parameter to control whether this is a
		///   blocking or non-blocking call.
		/// </remarks>
		/// <param name="state">The state returned by the Begin method.</param>
		/// <param name="wait">By default this is true which will execute the runloop waiting for events, if you pass false, you can use this method to run a single iteration of the events.</param>
		public static void RunLoop (RunState state, bool wait = true)
		{
			if (state == null)
				throw new ArgumentNullException (nameof (state));
			if (state.Toplevel == null)
				throw new ObjectDisposedException ("state");

			bool firstIteration = true;
			for (state.Toplevel.Running = true; state.Toplevel.Running;) {
				if (MainLoop.EventsPending (wait)) {
					// Notify Toplevel it's ready
					if (firstIteration) {
						state.Toplevel.OnReady ();
					}
					firstIteration = false;

					MainLoop.MainIteration ();
					Iteration?.Invoke ();
				} else if (wait == false)
					return;
				if (state.Toplevel.NeedDisplay != null && (!state.Toplevel.NeedDisplay.IsEmpty || state.Toplevel.childNeedsDisplay)) {
					state.Toplevel.Redraw (state.Toplevel.Bounds);
					if (DebugDrawBounds)
						DrawBounds (state.Toplevel);
					state.Toplevel.PositionCursor ();
					Driver.Refresh ();
				} else
					Driver.UpdateCursor ();
			}
		}

		internal static bool DebugDrawBounds = false;

		// Need to look into why this does not work properly.
		static void DrawBounds (View v)
		{
			v.DrawFrame (v.Frame, padding: 0, fill: false);
			if (v.InternalSubviews != null && v.InternalSubviews.Count > 0)
				foreach (var sub in v.InternalSubviews)
					DrawBounds (sub);
		}

		/// <summary>
		/// Runs the application by calling <see cref="Run(Toplevel, bool)"/> with the value of <see cref="Top"/>
		/// </summary>
		public static void Run ()
		{
			Run (Top);
		}

		/// <summary>
		/// Runs the application by calling <see cref="Run(Toplevel, bool)"/> with a new instance of the specified <see cref="Toplevel"/>-derived class
		/// </summary>
		public static void Run<T> () where T : Toplevel, new()
		{
			Init (() => new T ());
			Run (Top);
		}

		/// <summary>
		///   Runs the main loop on the given <see cref="Toplevel"/> container.
		/// </summary>
		/// <remarks>
		///   <para>
		///     This method is used to start processing events
		///     for the main application, but it is also used to
		///     run other modal <see cref="View"/>s such as <see cref="Dialog"/> boxes.
		///   </para>
		///   <para>
		///     To make a <see cref="Run(Toplevel, bool)"/> stop execution, call <see cref="Application.RequestStop"/>.
		///   </para>
		///   <para>
		///     Calling <see cref="Run(Toplevel, bool)"/> is equivalent to calling <see cref="Begin(Toplevel)"/>, followed by <see cref="RunLoop(RunState, bool)"/>,
		///     and then calling <see cref="End(RunState, bool)"/>.
		///   </para>
		///   <para>
		///     Alternatively, to have a program control the main loop and 
		///     process events manually, call <see cref="Begin(Toplevel)"/> to set things up manually and then
		///     repeatedly call <see cref="RunLoop(RunState, bool)"/> with the wait parameter set to false.   By doing this
		///     the <see cref="RunLoop(RunState, bool)"/> method will only process any pending events, timers, idle handlers and
		///     then return control immediately.
		///   </para>
		/// </remarks>
		/// <param name="view">The <see cref="Toplevel"/> tu run modally.</param>
		/// <param name="closeDriver">Set to <true/> to cause the MainLoop to end when <see cref="End(RunState, bool)"/> is called, clsing the toplevels only.</param>
		public static void Run (Toplevel view, bool closeDriver = true)
		{
			var runToken = Begin (view);
			RunLoop (runToken);
			End (runToken, closeDriver);
		}

		/// <summary>
		/// Stops running the most recent <see cref="Toplevel"/>. 
		/// </summary>
		/// <remarks>
		///   <para>
		///   This will cause <see cref="Application.Run()"/> to return.
		///   </para>
		///   <para>
		///     Calling <see cref="Application.RequestStop"/> is equivalent to setting the <see cref="Toplevel.Running"/> property on the curently running <see cref="Toplevel"/> to false.
		///   </para>
		/// </remarks>
		public static void RequestStop ()
		{
			Current.Running = false;
		}

		/// <summary>
		/// Event arguments for the <see cref="Application.Resized"/> event.
		/// </summary>
		public class ResizedEventArgs : EventArgs {
			/// <summary>
			/// The number of rows in the resized terminal.
			/// </summary>
			public int Rows { get; set; }
			/// <summary>
			/// The number of columns in the resized terminal.
			/// </summary>
			public int Cols { get; set; }
		}

		/// <summary>
		/// Invoked when the terminal was resized. The new size of the terminal is provided.
		/// </summary>
		public static Action<ResizedEventArgs> Resized;

		static void TerminalResized ()
		{
			var full = new Rect (0, 0, Driver.Cols, Driver.Rows);
			Resized?.Invoke (new ResizedEventArgs () { Cols = full.Width, Rows = full.Height });
			Driver.Clip = full;
			foreach (var t in toplevels) {
				t.PositionToplevels ();
				t.SetRelativeLayout (full);
				t.LayoutSubviews ();
			}
			Refresh ();
		}
	}
}
