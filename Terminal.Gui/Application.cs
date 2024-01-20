using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
/// A static, singleton class representing the application. This class is the entry point for the application.
/// </summary>
/// <example>
/// <code>
/// // A simple Terminal.Gui app that creates a window with a frame and title with 
/// // 5 rows/columns of padding.
/// Application.Init();
/// var win = new Window ($"Example App ({Application.QuitKey} to quit)") {
///   X = 5,
///   Y = 5,
///   Width = Dim.Fill (5),
///   Height = Dim.Fill (5)
/// };
/// Application.Top.Add(win);
/// Application.Run();
/// Application.Shutdown();
/// </code>
/// </example>
/// <remarks>
/// TODO: Flush this out.
/// </remarks>
public static partial class Application {

	// IMPORTANT: Ensure all property/fields are reset here. See Init_ResetState_Resets_Properties unit test.
	// Encapsulate all setting of initial state for Application; Having
	// this in a function like this ensures we don't make mistakes in
	// guaranteeing that the state of this singleton is deterministic when Init
	// starts running and after Shutdown returns.
	internal static void ResetState ()
	{
		// Shutdown is the bookend for Init. As such it needs to clean up all resources
		// Init created. Apps that do any threading will need to code defensively for this.
		// e.g. see Issue #537
		foreach (var t in _topLevels) {
			t.Running = false;
			t.Dispose ();
		}
		_topLevels.Clear ();
		Current = null;
		Top?.Dispose ();
		Top = null;

		// MainLoop stuff
		MainLoop?.Dispose ();
		MainLoop = null;
		_mainThreadId = -1;
		Iteration = null;
		EndAfterFirstIteration = false;
		
		// Driver stuff
		if (Driver != null) {
			Driver.SizeChanged -= Driver_SizeChanged;
			Driver.KeyDown -= Driver_KeyDown;
			Driver.KeyUp -= Driver_KeyUp;
			Driver.MouseEvent -= Driver_MouseEvent;
			Driver?.End ();
			Driver = null;
		}
		// Don't reset ForceDriver; it needs to be set before Init is called.
		//ForceDriver = string.Empty;
		Force16Colors = false;
		_forceFakeConsole = false;
		
		// Run State stuff
		NotifyNewRunState = null;
		NotifyStopRunState = null;
		MouseGrabView = null;
		_initialized = false;

		// Mouse
		_mouseEnteredView = null;
		WantContinuousButtonPressedView = null;
		MouseEvent = null;
		GrabbedMouse = null;
		UnGrabbingMouse = null;
		GrabbedMouse = null;
		UnGrabbedMouse = null;

		// Keyboard
		AlternateBackwardKey = Key.Empty;
		AlternateForwardKey = Key.Empty;
		QuitKey = Key.Empty;
		KeyDown = null;
		KeyUp = null;
		SizeChanging = null;

		Colors.Reset ();

		// Reset synchronization context to allow the user to run async/await,
		// as the main loop has been ended, the synchronization context from 
		// gui.cs does no longer process any callbacks. See #1084 for more details:
		// (https://github.com/gui-cs/Terminal.Gui/issues/1084).
		SynchronizationContext.SetSynchronizationContext (syncContext: null);
	}

	/// <summary>
	/// Gets the <see cref="ConsoleDriver"/> that has been selected. See also <see cref="ForceDriver"/>.
	/// </summary>
	public static ConsoleDriver Driver { get; internal set; }

	/// <summary>
	/// Forces the use of the specified driver (one of "fake", "ansi", "curses", "net", or "windows"). If
	/// not specified, the driver is selected based on the platform.
	/// </summary>
	/// <remarks>
	/// Note, <see cref="Application.Init(ConsoleDriver, string)"/> will override this configuration setting if
	/// called with either `driver` or `driverName` specified.
	/// </remarks>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
	public static string ForceDriver { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether <see cref="Application.Driver"/> will be forced to output only the 16 colors defined in <see cref="ColorName"/>.
	/// The default is <see langword="false"/>, meaning 24-bit (TrueColor) colors will be output as long as the selected <see cref="ConsoleDriver"/>
	/// supports TrueColor.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
	public static bool Force16Colors { get; set; } = false;

	// For Unit testing - ignores UseSystemConsole
	internal static bool _forceFakeConsole;

	static List<CultureInfo> _cachedSupportedCultures;

	/// <summary>
	/// Gets all cultures supported by the application without the invariant language.
	/// </summary>
	public static List<CultureInfo> SupportedCultures => _cachedSupportedCultures;

	internal static List<CultureInfo> GetSupportedCultures ()
	{
		var culture = CultureInfo.GetCultures (CultureTypes.AllCultures);

		// Get the assembly
		var assembly = Assembly.GetExecutingAssembly ();

		//Find the location of the assembly
		string assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;

		// Find the resource file name of the assembly
		string resourceFilename = $"{Path.GetFileNameWithoutExtension (assembly.Location)}.resources.dll";

		// Return all culture for which satellite folder found with culture code.
		return culture.Where (cultureInfo =>
			Directory.Exists (Path.Combine (assemblyLocation, cultureInfo.Name)) &&
			File.Exists (Path.Combine (assemblyLocation, cultureInfo.Name, resourceFilename))
		).ToList ();
	}

	#region Initialization (Init/Shutdown)
	/// <summary>
	/// Initializes a new instance of <see cref="Terminal.Gui"/> Application. 
	/// </summary>
	/// <para>
	/// Call this method once per instance (or after <see cref="Shutdown"/> has been called).
	/// </para>
	/// <para>
	/// This function loads the right <see cref="ConsoleDriver"/> for the platform, 
	/// Creates a <see cref="Toplevel"/>. and assigns it to <see cref="Top"/>
	/// </para>
	/// <para>
	/// <see cref="Shutdown"/> must be called when the application is closing (typically after <see cref="Run(Func{Exception, bool})"/> has 
	/// returned) to ensure resources are cleaned up and terminal settings restored.
	/// </para>
	/// <para>
	/// The <see cref="Run{T}(Func{Exception, bool}, ConsoleDriver)"/> function 
	/// combines <see cref="Init(ConsoleDriver, string)"/> and <see cref="Run(Toplevel, Func{Exception, bool})"/>
	/// into a single call. An application cam use <see cref="Run{T}(Func{Exception, bool}, ConsoleDriver)"/> 
	/// without explicitly calling <see cref="Init(ConsoleDriver, string)"/>.
	/// </para>
	/// <param name="driver">The <see cref="ConsoleDriver"/> to use. If neither <paramref name="driver"/> or <paramref name="driverName"/> are specified the default driver for the platform will be used.</param>
	/// <param name="driverName">The short name (e.g. "net", "windows", "ansi", "fake", or "curses") of the <see cref="ConsoleDriver"/> to use. If neither <paramref name="driver"/> or <paramref name="driverName"/> are specified the default driver for the platform will be used.</param>
	public static void Init (ConsoleDriver driver = null, string driverName = null) => InternalInit (() => new Toplevel (), driver, driverName);

	internal static bool _initialized = false;
	internal static int _mainThreadId = -1;

	// INTERNAL function for initializing an app with a Toplevel factory object, driver, and mainloop.
	//
	// Called from:
	// 
	// Init() - When the user wants to use the default Toplevel. calledViaRunT will be false, causing all state to be reset.
	// Run<T>() - When the user wants to use a custom Toplevel. calledViaRunT will be true, enabling Run<T>() to be called without calling Init first.
	// Unit Tests - To initialize the app with a custom Toplevel, using the FakeDriver. calledViaRunT will be false, causing all state to be reset.
	// 
	// calledViaRunT: If false (default) all state will be reset. If true the state will not be reset.
	internal static void InternalInit (Func<Toplevel> topLevelFactory, ConsoleDriver driver = null, string driverName = null, bool calledViaRunT = false)
	{
		if (_initialized && driver == null) {
			return;
		}

		if (_initialized) {
			throw new InvalidOperationException ("Init has already been called and must be bracketed by Shutdown.");
		}

		if (!calledViaRunT) {
			// Reset all class variables (Application is a singleton).
			ResetState ();
		}

		// For UnitTests
		if (driver != null) {
			Driver = driver;
		}

		// Start the process of configuration management.
		// Note that we end up calling LoadConfigurationFromAllSources
		// multiple times. We need to do this because some settings are only
		// valid after a Driver is loaded. In this cases we need just 
		// `Settings` so we can determine which driver to use.
		Load (true);
		Apply ();

		// Ignore Configuration for ForceDriver if driverName is specified
		if (!string.IsNullOrEmpty (driverName)) {
			ForceDriver = driverName;
		}

		if (Driver == null) {
			var p = Environment.OSVersion.Platform;
			if (string.IsNullOrEmpty (ForceDriver)) {
				if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows) {
					Driver = new WindowsDriver ();
				} else {
					Driver = new CursesDriver ();
				}
			} else {
				var drivers = GetDriverTypes ();
				var driverType = drivers.FirstOrDefault (t => t.Name.ToLower () == ForceDriver.ToLower ());
				if (driverType != null) {
					Driver = (ConsoleDriver)Activator.CreateInstance (driverType);
				} else {
					throw new ArgumentException ($"Invalid driver name: {ForceDriver}. Valid names are {string.Join (", ", drivers.Select (t => t.Name))}");
				}
			}
		}

		try {
			MainLoop = Driver.Init ();
		} catch (InvalidOperationException ex) {
			// This is a case where the driver is unable to initialize the console.
			// This can happen if the console is already in use by another process or
			// if running in unit tests.
			// In this case, we want to throw a more specific exception.
			throw new InvalidOperationException ("Unable to initialize the console. This can happen if the console is already in use by another process or in unit tests.", ex);
		}

		Driver.SizeChanged += (s, args) => OnSizeChanging (args);
		Driver.KeyDown += (s, args) => OnKeyDown (args);
		Driver.KeyUp += (s, args) => OnKeyUp (args);
		Driver.MouseEvent += (s, args) => OnMouseEvent (args);

		SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext ());

		Top = topLevelFactory ();
		Current = Top;

		// Ensure Top's layout is up to date.
		Current.SetRelativeLayout (Driver.Bounds);

		_cachedSupportedCultures = GetSupportedCultures ();
		_mainThreadId = Thread.CurrentThread.ManagedThreadId;
		_initialized = true;
	}

	static void Driver_SizeChanged (object sender, SizeChangedEventArgs e) => OnSizeChanging (e);

	static void Driver_KeyDown (object sender, Key e) => OnKeyDown (e);

	static void Driver_KeyUp (object sender, Key e) => OnKeyUp (e);

	static void Driver_MouseEvent (object sender, MouseEventEventArgs e) => OnMouseEvent (e);

	/// <summary>
	/// Gets of list of <see cref="ConsoleDriver"/> types that are available.
	/// </summary>
	/// <returns></returns>
	public static List<Type> GetDriverTypes ()
	{
		// use reflection to get the list of drivers
		var driverTypes = new List<Type> ();
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies ()) {
			foreach (var type in asm.GetTypes ()) {
				if (type.IsSubclassOf (typeof (ConsoleDriver)) && !type.IsAbstract) {
					driverTypes.Add (type);
				}
			}
		}
		return driverTypes;
	}

	/// <summary>
	/// Shutdown an application initialized with <see cref="Init"/>.
	/// </summary>
	/// <remarks>
	/// Shutdown must be called for every call to <see cref="Init"/> or <see cref="Application.Run(Toplevel, Func{Exception, bool})"/>
	/// to ensure all resources are cleaned up (Disposed) and terminal settings are restored.
	/// </remarks>
	public static void Shutdown ()
	{
		ResetState ();
		PrintJsonErrors ();
	}
	#endregion Initialization (Init/Shutdown)

	#region Run (Begin, Run, End, Stop)
	/// <summary>
	/// Notify that a new <see cref="RunState"/> was created (<see cref="Begin(Toplevel)"/> was called). The token is created in 
	/// <see cref="Begin(Toplevel)"/> and this event will be fired before that function exits.
	/// </summary>
	/// <remarks>
	///	If <see cref="EndAfterFirstIteration"/> is <see langword="true"/> callers to
	///	<see cref="Begin(Toplevel)"/> must also subscribe to <see cref="NotifyStopRunState"/>
	///	and manually dispose of the <see cref="RunState"/> token when the application is done.
	/// </remarks>
	public static event EventHandler<RunStateEventArgs> NotifyNewRunState;

	/// <summary>
	/// Notify that a existent <see cref="RunState"/> is stopping (<see cref="End(RunState)"/> was called).
	/// </summary>
	/// <remarks>
	///	If <see cref="EndAfterFirstIteration"/> is <see langword="true"/> callers to
	///	<see cref="Begin(Toplevel)"/> must also subscribe to <see cref="NotifyStopRunState"/>
	///	and manually dispose of the <see cref="RunState"/> token when the application is done.
	/// </remarks>
	public static event EventHandler<ToplevelEventArgs> NotifyStopRunState;

	/// <summary>
	/// Building block API: Prepares the provided <see cref="Toplevel"/> for execution.
	/// </summary>
	/// <returns>The <see cref="RunState"/> handle that needs to be passed to the <see cref="End(RunState)"/> method upon completion.</returns>
	/// <param name="Toplevel">The <see cref="Toplevel"/> to prepare execution for.</param>
	/// <remarks>
	/// This method prepares the provided <see cref="Toplevel"/> for running with the focus,
	/// it adds this to the list of <see cref="Toplevel"/>s, lays out the Subviews, focuses the first element, and draws the
	/// <see cref="Toplevel"/> in the screen. This is usually followed by executing
	/// the <see cref="RunLoop"/> method, and then the <see cref="End(RunState)"/> method upon termination which will
	///  undo these changes.
	/// </remarks>
	public static RunState Begin (Toplevel Toplevel)
	{
		if (Toplevel == null) {
			throw new ArgumentNullException (nameof (Toplevel));
		} else if (Toplevel.IsOverlappedContainer && OverlappedTop != Toplevel && OverlappedTop != null) {
			throw new InvalidOperationException ("Only one Overlapped Container is allowed.");
		}

		// Ensure the mouse is ungrabed.
		MouseGrabView = null;

		var rs = new RunState (Toplevel);

		// View implements ISupportInitializeNotification which is derived from ISupportInitialize
		if (!Toplevel.IsInitialized) {
			Toplevel.BeginInit ();
			Toplevel.EndInit ();
		}

		lock (_topLevels) {
			// If Top was already initialized with Init, and Begin has never been called
			// Top was not added to the Toplevels Stack. It will thus never get disposed.
			// Clean it up here:
			if (Top != null && Toplevel != Top && !_topLevels.Contains (Top)) {
				Top.Dispose ();
				Top = null;
			} else if (Top != null && Toplevel != Top && _topLevels.Contains (Top)) {
				Top.OnLeave (Toplevel);
			}
			// BUGBUG: We should not depend on `Id` internally. 
			// BUGBUG: It is super unclear what this code does anyway.
			if (string.IsNullOrEmpty (Toplevel.Id)) {
				int count = 1;
				string id = (_topLevels.Count + count).ToString ();
				while (_topLevels.Count > 0 && _topLevels.FirstOrDefault (x => x.Id == id) != null) {
					count++;
					id = (_topLevels.Count + count).ToString ();
				}
				Toplevel.Id = (_topLevels.Count + count).ToString ();

				_topLevels.Push (Toplevel);
			} else {
				var dup = _topLevels.FirstOrDefault (x => x.Id == Toplevel.Id);
				if (dup == null) {
					_topLevels.Push (Toplevel);
				}
			}

			if (_topLevels.FindDuplicates (new ToplevelEqualityComparer ()).Count > 0) {
				throw new ArgumentException ("There are duplicates Toplevels Id's");
			}
		}
		if (Top == null || Toplevel.IsOverlappedContainer) {
			Top = Toplevel;
		}

		bool refreshDriver = true;
		if (OverlappedTop == null || Toplevel.IsOverlappedContainer || Current?.Modal == false && Toplevel.Modal
		|| Current?.Modal == false && !Toplevel.Modal || Current?.Modal == true && Toplevel.Modal) {

			if (Toplevel.Visible) {
				Current = Toplevel;
				SetCurrentOverlappedAsTop ();
			} else {
				refreshDriver = false;
			}
		} else if (OverlappedTop != null && Toplevel != OverlappedTop && Current?.Modal == true && !_topLevels.Peek ().Modal
			|| OverlappedTop != null && Toplevel != OverlappedTop && Current?.Running == false) {
			refreshDriver = false;
			MoveCurrent (Toplevel);
		} else {
			refreshDriver = false;
			MoveCurrent (Current);
		}

		//if (Toplevel.LayoutStyle == LayoutStyle.Computed) {
		Toplevel.SetRelativeLayout (Driver.Bounds);
		//}
		Toplevel.LayoutSubviews ();
		Toplevel.PositionToplevels ();
		Toplevel.FocusFirst ();
		if (refreshDriver) {
			OverlappedTop?.OnChildLoaded (Toplevel);
			Toplevel.OnLoaded ();
			Toplevel.SetNeedsDisplay ();
			Toplevel.Draw ();
			Toplevel.PositionCursor ();
			Driver.Refresh ();
		}

		NotifyNewRunState?.Invoke (Toplevel, new RunStateEventArgs (rs));
		return rs;
	}

	/// <summary>
	/// Runs the application by calling <see cref="Run(Toplevel, Func{Exception, bool})"/> with the value of <see cref="Top"/>.
	/// </summary>
	/// <remarks>
	/// See <see cref="Run(Toplevel, Func{Exception, bool})"/> for more details.
	/// </remarks>
	public static void Run (Func<Exception, bool> errorHandler = null) => Run (Top, errorHandler);

	/// <summary>
	/// Runs the application by calling <see cref="Run(Toplevel, Func{Exception, bool})"/> 
	/// with a new instance of the specified <see cref="Toplevel"/>-derived class.
	/// <para>
	/// Calling <see cref="Init"/> first is not needed as this function will initialize the application.
	/// </para>
	/// <para>
	/// <see cref="Shutdown"/> must be called when the application is closing (typically after Run> has 
	/// returned) to ensure resources are cleaned up and terminal settings restored.
	/// </para>
	/// </summary>
	/// <remarks>
	/// See <see cref="Run(Toplevel, Func{Exception, bool})"/> for more details.
	/// </remarks>
	/// <param name="errorHandler"></param>
	/// <param name="driver">The <see cref="ConsoleDriver"/> to use. If not specified the default driver for the
	/// platform will be used (<see cref="WindowsDriver"/>, <see cref="CursesDriver"/>, or <see cref="NetDriver"/>).
	/// Must be <see langword="null"/> if <see cref="Init"/> has already been called. 
	/// </param>
	public static void Run<T> (Func<Exception, bool> errorHandler = null, ConsoleDriver driver = null) where T : Toplevel, new()
	{
		if (_initialized) {
			if (Driver != null) {
				// Init() has been called and we have a driver, so just run the app.
				var top = new T ();
				var type = top.GetType ().BaseType;
				while (type != typeof (Toplevel) && type != typeof (object)) {
					type = type.BaseType;
				}
				if (type != typeof (Toplevel)) {
					throw new ArgumentException ($"{top.GetType ().Name} must be derived from TopLevel");
				}
				Run (top, errorHandler);
			} else {
				// This code path should be impossible because Init(null, null) will select the platform default driver
				throw new InvalidOperationException ("Init() completed without a driver being set (this should be impossible); Run<T>() cannot be called.");
			}
		} else {
			// Init() has NOT been called.
			InternalInit (() => new T (), driver, null, true);
			Run (Top, errorHandler);
		}
	}

	/// <summary>
	///  Runs the main loop on the given <see cref="Toplevel"/> container.
	/// </summary>
	/// <remarks>
	///  <para>
	///   This method is used to start processing events
	///   for the main application, but it is also used to
	///   run other modal <see cref="View"/>s such as <see cref="Dialog"/> boxes.
	///  </para>
	///  <para>
	///   To make a <see cref="Run(Toplevel, Func{Exception, bool})"/> stop execution, call <see cref="Application.RequestStop"/>.
	///  </para>
	///  <para>
	///   Calling <see cref="Run(Toplevel, Func{Exception, bool})"/> is equivalent to calling <see cref="Begin(Toplevel)"/>,
	///   followed by <see cref="RunLoop(RunState)"/>, and then calling <see cref="End(RunState)"/>.
	///  </para>
	///  <para>
	///   Alternatively, to have a program control the main loop and 
	///   process events manually, call <see cref="Begin(Toplevel)"/> to set things up manually and then
	///   repeatedly call <see cref="RunLoop(RunState)"/> with the wait parameter set to false. By doing this
	///   the <see cref="RunLoop(RunState)"/> method will only process any pending events, timers, idle handlers and
	///   then return control immediately.
	///  </para>
	///  <para>
	///   RELEASE builds only: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be rethrown. 
	///   Otherwise, if <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/> 
	///   returns <see langword="true"/> the <see cref="RunLoop(RunState)"/> will resume; otherwise 
	///   this method will exit.
	///  </para>
	/// </remarks>
	/// <param name="view">The <see cref="Toplevel"/> to run as a modal.</param>
	/// <param name="errorHandler">RELEASE builds only: Handler for any unhandled exceptions (resumes when returns true, rethrows when null).</param>
	public static void Run (Toplevel view, Func<Exception, bool> errorHandler = null)
	{
		bool resume = true;
		while (resume) {
#if !DEBUG
				try {
#endif
			resume = false;
			var runState = Begin (view);
			// If EndAfterFirstIteration is true then the user must dispose of the runToken
			// by using NotifyStopRunState event.
			RunLoop (runState);
			if (!EndAfterFirstIteration) {
				End (runState);
			}
#if !DEBUG
				}
				catch (Exception error)
				{
					if (errorHandler == null)
					{
						throw;
					}
					resume = errorHandler(error);
				}
#endif
		}
	}

	/// <summary>
	///   Adds a timeout to the application.
	/// </summary>
	/// <remarks>
	///   When time specified passes, the callback will be invoked.
	///   If the callback returns true, the timeout will be reset, repeating
	///   the invocation. If it returns false, the timeout will stop and be removed.
	///
	///   The returned value is a token that can be used to stop the timeout
	///   by calling <see cref="RemoveTimeout(object)"/>.
	/// </remarks>
	public static object AddTimeout (TimeSpan time, Func<bool> callback) => MainLoop?.AddTimeout (time, callback);

	/// <summary>
	///   Removes a previously scheduled timeout
	/// </summary>
	/// <remarks>
	///   The token parameter is the value returned by <see cref="AddTimeout"/>.
	/// </remarks>
	/// Returns <c>true</c>if the timeout is successfully removed; otherwise, <c>false</c>.
	/// This method also returns <c>false</c> if the timeout is not found.
	public static bool RemoveTimeout (object token) => MainLoop?.RemoveTimeout (token) ?? false;


	/// <summary>
	///   Runs <paramref name="action"/> on the thread that is processing events
	/// </summary>
	/// <param name="action">the action to be invoked on the main processing thread.</param>
	public static void Invoke (Action action) => MainLoop?.AddIdle (() => {
		action ();
		return false;
	});

	// TODO: Determine if this is really needed. The only code that calls WakeUp I can find
	// is ProgressBarStyles and it's not clear it needs to.
	/// <summary>
	/// Wakes up the running application that might be waiting on input.
	/// </summary>
	public static void Wakeup () => MainLoop?.Wakeup ();

	/// <summary>
	/// Triggers a refresh of the entire display.
	/// </summary>
	public static void Refresh ()
	{
		// TODO: Figure out how to remove this call to ClearContents. Refresh should just repaint damaged areas, not clear
		Driver.ClearContents ();
		View last = null;
		foreach (var v in _topLevels.Reverse ()) {
			if (v.Visible) {
				v.SetNeedsDisplay ();
				v.SetSubViewNeedsDisplay ();
				v.Draw ();
			}
			last = v;
		}
		last?.PositionCursor ();
		Driver.Refresh ();
	}

	/// <summary>
	///  This event is raised on each iteration of the main loop.
	/// </summary>
	/// <remarks>
	///  See also <see cref="Timeout"/>
	/// </remarks>
	public static event EventHandler<IterationEventArgs> Iteration;

	/// <summary>
	/// The <see cref="MainLoop"/> driver for the application
	/// </summary>
	/// <value>The main loop.</value>
	internal static MainLoop MainLoop { get; private set; }

	/// <summary>
	/// Set to true to cause <see cref="End"/> to be called after the first iteration.
	/// Set to false (the default) to cause the application to continue running until Application.RequestStop () is called.
	/// </summary>
	public static bool EndAfterFirstIteration { get; set; } = false;

	//
	// provides the sync context set while executing code in Terminal.Gui, to let
	// users use async/await on their code
	//
	class MainLoopSyncContext : SynchronizationContext {
		public override SynchronizationContext CreateCopy () => new MainLoopSyncContext ();

		public override void Post (SendOrPostCallback d, object state) => MainLoop.AddIdle (() => {
			d (state);
			return false;
		});

		//_mainLoop.Driver.Wakeup ();
		public override void Send (SendOrPostCallback d, object state)
		{
			if (Thread.CurrentThread.ManagedThreadId == _mainThreadId) {
				d (state);
			} else {
				bool wasExecuted = false;
				Invoke (() => {
					d (state);
					wasExecuted = true;
				});
				while (!wasExecuted) {
					Thread.Sleep (15);
				}
			}
		}
	}

	/// <summary>
	///  Building block API: Runs the main loop for the created <see cref="Toplevel"/>.
	/// </summary>
	/// <param name="state">The state returned by the <see cref="Begin(Toplevel)"/> method.</param>
	public static void RunLoop (RunState state)
	{
		if (state == null) {
			throw new ArgumentNullException (nameof (state));
		}
		if (state.Toplevel == null) {
			throw new ObjectDisposedException ("state");
		}

		bool firstIteration = true;
		for (state.Toplevel.Running = true; state.Toplevel.Running;) {
			MainLoop.Running = true;
			if (EndAfterFirstIteration && !firstIteration) {
				return;
			}
			RunIteration (ref state, ref firstIteration);
		}
		MainLoop.Running = false;
		// Run one last iteration to consume any outstanding input events from Driver
		// This is important for remaining OnKeyUp events.
		RunIteration (ref state, ref firstIteration);
	}

	/// <summary>
	/// Run one application iteration.
	/// </summary>
	/// <param name="state">The state returned by <see cref="Begin(Toplevel)"/>.</param>
	/// <param name="firstIteration">Set to <see langword="true"/> if this is the first run loop iteration. Upon return,
	/// it will be set to <see langword="false"/> if at least one iteration happened.</param>
	public static void RunIteration (ref RunState state, ref bool firstIteration)
	{
		if (MainLoop.Running && MainLoop.EventsPending ()) {
			// Notify Toplevel it's ready
			if (firstIteration) {
				state.Toplevel.OnReady ();
			}

			MainLoop.RunIteration ();
			Iteration?.Invoke (null, new IterationEventArgs ());
			EnsureModalOrVisibleAlwaysOnTop (state.Toplevel);
			if (state.Toplevel != Current) {
				OverlappedTop?.OnDeactivate (state.Toplevel);
				state.Toplevel = Current;
				OverlappedTop?.OnActivate (state.Toplevel);
				Top.SetSubViewNeedsDisplay ();
				Refresh ();
			}
		}

		firstIteration = false;

		if (state.Toplevel != Top &&
		(Top.NeedsDisplay || Top.SubViewNeedsDisplay || Top.LayoutNeeded)) {
			state.Toplevel.SetNeedsDisplay (state.Toplevel.Frame);
			Top.Draw ();
			foreach (var top in _topLevels.Reverse ()) {
				if (top != Top && top != state.Toplevel) {
					top.SetNeedsDisplay ();
					top.SetSubViewNeedsDisplay ();
					top.Draw ();
				}
			}
		}
		if (_topLevels.Count == 1 && state.Toplevel == Top
					&& (Driver.Cols != state.Toplevel.Frame.Width || Driver.Rows != state.Toplevel.Frame.Height)
					&& (state.Toplevel.NeedsDisplay || state.Toplevel.SubViewNeedsDisplay || state.Toplevel.LayoutNeeded)) {

			state.Toplevel.Clear (Driver.Bounds);
		}

		if (state.Toplevel.NeedsDisplay ||
		state.Toplevel.SubViewNeedsDisplay ||
		state.Toplevel.LayoutNeeded ||
		OverlappedChildNeedsDisplay ()) {
			state.Toplevel.Draw ();
			state.Toplevel.PositionCursor ();
			Driver.Refresh ();
		} else {
			Driver.UpdateCursor ();
		}
		if (state.Toplevel != Top &&
		!state.Toplevel.Modal &&
		(Top.NeedsDisplay || Top.SubViewNeedsDisplay || Top.LayoutNeeded)) {
			Top.Draw ();
		}
	}

	/// <summary>
	/// Stops running the most recent <see cref="Toplevel"/> or the <paramref name="top"/> if provided.
	/// </summary>
	/// <param name="top">The <see cref="Toplevel"/> to stop.</param>
	/// <remarks>
	///  <para>
	///  This will cause <see cref="Application.Run(Func{Exception, bool})"/> to return.
	///  </para>
	///  <para>
	///   Calling <see cref="Application.RequestStop"/> is equivalent to setting the <see cref="Toplevel.Running"/> property 
	///   on the currently running <see cref="Toplevel"/> to false.
	///  </para>
	/// </remarks>
	public static void RequestStop (Toplevel top = null)
	{
		if (OverlappedTop == null || top == null || OverlappedTop == null && top != null) {
			top = Current;
		}

		if (OverlappedTop != null && top.IsOverlappedContainer && top?.Running == true
		&& (Current?.Modal == false || Current?.Modal == true && Current?.Running == false)) {

			OverlappedTop.RequestStop ();
		} else if (OverlappedTop != null && top != Current && Current?.Running == true && Current?.Modal == true
			&& top.Modal && top.Running) {

			var ev = new ToplevelClosingEventArgs (Current);
			Current.OnClosing (ev);
			if (ev.Cancel) {
				return;
			}
			ev = new ToplevelClosingEventArgs (top);
			top.OnClosing (ev);
			if (ev.Cancel) {
				return;
			}
			Current.Running = false;
			OnNotifyStopRunState (Current);
			top.Running = false;
			OnNotifyStopRunState (top);
		} else if (OverlappedTop != null && top != OverlappedTop && top != Current && Current?.Modal == false
			&& Current?.Running == true && !top.Running
			|| OverlappedTop != null && top != OverlappedTop && top != Current && Current?.Modal == false
			&& Current?.Running == false && !top.Running && _topLevels.ToArray () [1].Running) {

			MoveCurrent (top);
		} else if (OverlappedTop != null && Current != top && Current?.Running == true && !top.Running
			&& Current?.Modal == true && top.Modal) {
			// The Current and the top are both modal so needed to set the Current.Running to false too.
			Current.Running = false;
			OnNotifyStopRunState (Current);
		} else if (OverlappedTop != null && Current == top && OverlappedTop?.Running == true && Current?.Running == true && top.Running
			&& Current?.Modal == true && top.Modal) {
			// The OverlappedTop was requested to stop inside a modal Toplevel which is the Current and top,
			// both are the same, so needed to set the Current.Running to false too.
			Current.Running = false;
			OnNotifyStopRunState (Current);
		} else {
			Toplevel currentTop;
			if (top == Current || Current?.Modal == true && !top.Modal) {
				currentTop = Current;
			} else {
				currentTop = top;
			}
			if (!currentTop.Running) {
				return;
			}
			var ev = new ToplevelClosingEventArgs (currentTop);
			currentTop.OnClosing (ev);
			if (ev.Cancel) {
				return;
			}
			currentTop.Running = false;
			OnNotifyStopRunState (currentTop);
		}
	}

	static void OnNotifyStopRunState (Toplevel top)
	{
		if (EndAfterFirstIteration) {
			NotifyStopRunState?.Invoke (top, new ToplevelEventArgs (top));
		}
	}

	/// <summary>
	/// Building block API: completes the execution of a <see cref="Toplevel"/> that was started with <see cref="Begin(Toplevel)"/> .
	/// </summary>
	/// <param name="runState">The <see cref="RunState"/> returned by the <see cref="Begin(Toplevel)"/> method.</param>
	public static void End (RunState runState)
	{
		if (runState == null) {
			throw new ArgumentNullException (nameof (runState));
		}

		if (OverlappedTop != null) {
			OverlappedTop.OnChildUnloaded (runState.Toplevel);
		} else {
			runState.Toplevel.OnUnloaded ();
		}

		// End the RunState.Toplevel 
		// First, take it off the Toplevel Stack
		if (_topLevels.Count > 0) {
			if (_topLevels.Peek () != runState.Toplevel) {
				// If there the top of the stack is not the RunState.Toplevel then
				// this call to End is not balanced with the call to Begin that started the RunState
				throw new ArgumentException ("End must be balanced with calls to Begin");
			}
			_topLevels.Pop ();
		}

		// Notify that it is closing
		runState.Toplevel?.OnClosed (runState.Toplevel);

		// If there is a OverlappedTop that is not the RunState.Toplevel then runstate.TopLevel 
		// is a child of MidTop and we should notify the OverlappedTop that it is closing
		if (OverlappedTop != null && !runState.Toplevel.Modal && runState.Toplevel != OverlappedTop) {
			OverlappedTop.OnChildClosed (runState.Toplevel);
		}

		// Set Current and Top to the next TopLevel on the stack
		if (_topLevels.Count == 0) {
			Current = null;
		} else {
			Current = _topLevels.Peek ();
			if (_topLevels.Count == 1 && Current == OverlappedTop) {
				OverlappedTop.OnAllChildClosed ();
			} else {
				SetCurrentOverlappedAsTop ();
				runState.Toplevel.OnLeave (Current);
				Current.OnEnter (runState.Toplevel);
			}
			Refresh ();
		}

		runState.Toplevel?.Dispose ();
		runState.Toplevel = null;
		runState.Dispose ();
	}
	#endregion Run (Begin, Run, End)

	#region Toplevel handling
	/// <summary>
	/// Holds the stack of TopLevel views.
	/// </summary>
	// BUGBUG: Techncally, this is not the full lst of TopLevels. THere be dragons hwre. E.g. see how Toplevel.Id is used. What
	// about TopLevels that are just a SubView of another View?
	internal static readonly Stack<Toplevel> _topLevels = new Stack<Toplevel> ();

	/// <summary>
	/// The <see cref="Toplevel"/> object used for the application on startup (<seealso cref="Application.Top"/>)
	/// </summary>
	/// <value>The top.</value>
	public static Toplevel Top { get; private set; }

	/// <summary>
	/// The current <see cref="Toplevel"/> object. This is updated when <see cref="Application.Run(Func{Exception, bool})"/> 
	/// enters and leaves to point to the current <see cref="Toplevel"/> .
	/// </summary>
	/// <value>The current.</value>
	public static Toplevel Current { get; private set; }

	static void EnsureModalOrVisibleAlwaysOnTop (Toplevel Toplevel)
	{
		if (!Toplevel.Running || Toplevel == Current && Toplevel.Visible || OverlappedTop == null || _topLevels.Peek ().Modal) {
			return;
		}

		foreach (var top in _topLevels.Reverse ()) {
			if (top.Modal && top != Current) {
				MoveCurrent (top);
				return;
			}
		}
		if (!Toplevel.Visible && Toplevel == Current) {
			OverlappedMoveNext ();
		}
	}

	static View FindDeepestTop (Toplevel start, int x, int y, out int resx, out int resy)
	{
		var startFrame = start.Frame;

		if (!startFrame.Contains (x, y)) {
			resx = 0;
			resy = 0;
			return null;
		}

		if (_topLevels != null) {
			int count = _topLevels.Count;
			if (count > 0) {
				int rx = x - startFrame.X;
				int ry = y - startFrame.Y;
				foreach (var t in _topLevels) {
					if (t != Current) {
						if (t != start && t.Visible && t.Frame.Contains (rx, ry)) {
							start = t;
							break;
						}
					}
				}
			}
		}
		resx = x - startFrame.X;
		resy = y - startFrame.Y;
		return start;
	}

	static View FindTopFromView (View view)
	{
		var top = view?.SuperView != null && view?.SuperView != Top
			? view.SuperView : view;

		while (top?.SuperView != null && top?.SuperView != Top) {
			top = top.SuperView;
		}
		return top;
	}

	// Only return true if the Current has changed.
	static bool MoveCurrent (Toplevel top)
	{
		// The Current is modal and the top is not modal Toplevel then
		// the Current must be moved above the first not modal Toplevel.
		if (OverlappedTop != null && top != OverlappedTop && top != Current && Current?.Modal == true && !_topLevels.Peek ().Modal) {
			lock (_topLevels) {
				_topLevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
			}
			int index = 0;
			var savedToplevels = _topLevels.ToArray ();
			foreach (var t in savedToplevels) {
				if (!t.Modal && t != Current && t != top && t != savedToplevels [index]) {
					lock (_topLevels) {
						_topLevels.MoveTo (top, index, new ToplevelEqualityComparer ());
					}
				}
				index++;
			}
			return false;
		}
		// The Current and the top are both not running Toplevel then
		// the top must be moved above the first not running Toplevel.
		if (OverlappedTop != null && top != OverlappedTop && top != Current && Current?.Running == false && !top.Running) {
			lock (_topLevels) {
				_topLevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
			}
			int index = 0;
			foreach (var t in _topLevels.ToArray ()) {
				if (!t.Running && t != Current && index > 0) {
					lock (_topLevels) {
						_topLevels.MoveTo (top, index - 1, new ToplevelEqualityComparer ());
					}
				}
				index++;
			}
			return false;
		}
		if (OverlappedTop != null && top?.Modal == true && _topLevels.Peek () != top
		|| OverlappedTop != null && Current != OverlappedTop && Current?.Modal == false && top == OverlappedTop
		|| OverlappedTop != null && Current?.Modal == false && top != Current
		|| OverlappedTop != null && Current?.Modal == true && top == OverlappedTop) {
			lock (_topLevels) {
				_topLevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
				Current = top;
			}
		}
		return true;
	}

	/// <summary>
	/// Invoked when the terminal's size changed. The new size of the terminal is provided.
	/// </summary>
	/// <remarks>
	/// Event handlers can set <see cref="SizeChangedEventArgs.Cancel"/> to <see langword="true"/>
	/// to prevent <see cref="Application"/> from changing it's size to match the new terminal size.
	/// </remarks>
	public static event EventHandler<SizeChangedEventArgs> SizeChanging;

	/// <summary>
	/// Called when the application's size changes. Sets the size of all <see cref="Toplevel"/>s and
	/// fires the <see cref="SizeChanging"/> event.
	/// </summary>
	/// <param name="args">The new size.</param>
	/// <returns><see lanword="true"/>if the size was changed.</returns>
	public static bool OnSizeChanging (SizeChangedEventArgs args)
	{
		SizeChanging?.Invoke (null, args);
		if (args.Cancel) {
			return false;
		}

		foreach (var t in _topLevels) {
			t.SetRelativeLayout (new Rect (0, 0, args.Size.Width, args.Size.Height));
			t.LayoutSubviews ();
			t.PositionToplevels ();
			t.OnSizeChanging (new SizeChangedEventArgs (args.Size));
		}
		Refresh ();
		return true;
	}
	#endregion Toplevel handling

	#region Mouse handling
	/// <summary>
	/// Disable or enable the mouse. The mouse is enabled by default.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
	public static bool IsMouseDisabled { get; set; }

	/// <summary>
	/// The current <see cref="View"/> object that wants continuous mouse button pressed events.
	/// </summary>
	public static View WantContinuousButtonPressedView { get; private set; }

	/// <summary>
	/// Gets the view that grabbed the mouse (e.g. for dragging). When this is set, all mouse events will be
	/// routed to this view until the view calls <see cref="UngrabMouse"/> or the mouse is released.
	/// </summary>
	public static View MouseGrabView { get; private set; }

	/// <summary>
	/// Invoked when a view wants to grab the mouse; can be canceled.
	/// </summary>
	public static event EventHandler<GrabMouseEventArgs> GrabbingMouse;

	/// <summary>
	/// Invoked when a view wants un-grab the mouse; can be canceled.
	/// </summary>
	public static event EventHandler<GrabMouseEventArgs> UnGrabbingMouse;

	/// <summary>
	/// Invoked after a view has grabbed the mouse.
	/// </summary>
	public static event EventHandler<ViewEventArgs> GrabbedMouse;

	/// <summary>
	/// Invoked after a view has un-grabbed the mouse.
	/// </summary>
	public static event EventHandler<ViewEventArgs> UnGrabbedMouse;

	/// <summary>
	/// Grabs the mouse, forcing all mouse events to be routed to the specified view until <see cref="UngrabMouse"/> is called.
	/// </summary>
	/// <param name="view">View that will receive all mouse events until <see cref="UngrabMouse"/> is invoked.</param>
	public static void GrabMouse (View view)
	{
		if (view == null) {
			return;
		}
		if (!OnGrabbingMouse (view)) {
			OnGrabbedMouse (view);
			MouseGrabView = view;
		}
	}

	/// <summary>
	/// Releases the mouse grab, so mouse events will be routed to the view on which the mouse is.
	/// </summary>
	public static void UngrabMouse ()
	{
		if (MouseGrabView == null) {
			return;
		}
		if (!OnUnGrabbingMouse (MouseGrabView)) {
			OnUnGrabbedMouse (MouseGrabView);
			MouseGrabView = null;
		}
	}

	static bool OnGrabbingMouse (View view)
	{
		if (view == null) {
			return false;
		}
		var evArgs = new GrabMouseEventArgs (view);
		GrabbingMouse?.Invoke (view, evArgs);
		return evArgs.Cancel;
	}

	static bool OnUnGrabbingMouse (View view)
	{
		if (view == null) {
			return false;
		}
		var evArgs = new GrabMouseEventArgs (view);
		UnGrabbingMouse?.Invoke (view, evArgs);
		return evArgs.Cancel;
	}

	static void OnGrabbedMouse (View view)
	{
		if (view == null) {
			return;
		}
		GrabbedMouse?.Invoke (view, new ViewEventArgs (view));
	}

	static void OnUnGrabbedMouse (View view)
	{
		if (view == null) {
			return;
		}
		UnGrabbedMouse?.Invoke (view, new ViewEventArgs (view));
	}

	// Used by OnMouseEvent to track the last view that was clicked on.
	internal static View _mouseEnteredView;

	/// <summary>
	/// Event fired when a mouse move or click occurs. Coordinates are screen relative.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Use this event to receive mouse events in screen coordinates. Use <see cref="Responder.MouseEvent"/> to receive
	/// mouse events relative to a <see cref="View"/>'s bounds.
	/// </para>
	/// <para>
	/// The <see cref="MouseEvent.View"/> will contain the <see cref="View"/> that contains the mouse coordinates.
	/// </para>
	/// </remarks>
	public static event EventHandler<MouseEventEventArgs> MouseEvent;

	/// <summary>
	/// Called when a mouse event occurs. Fires the <see cref="MouseEvent"/> event.
	/// </summary>
	/// <remarks>
	/// This method can be used to simulate a mouse event, e.g. in unit tests.
	/// </remarks>
	/// <param name="a">The mouse event with coordinates relative to the screen.</param>
	public static void OnMouseEvent (MouseEventEventArgs a)
	{
		static bool OutsideRect (Point p, Rect r) => p.X < 0 || p.X > r.Right || p.Y < 0 || p.Y > r.Bottom;

		if (IsMouseDisabled) {
			return;
		}

		var view = View.FindDeepestView (Current, a.MouseEvent.X, a.MouseEvent.Y, out int screenX, out int screenY);

		if (view != null && view.WantContinuousButtonPressed) {
			WantContinuousButtonPressedView = view;
		} else {
			WantContinuousButtonPressedView = null;
		}
		if (view != null) {
			a.MouseEvent.View = view;
		}
		MouseEvent?.Invoke (null, new MouseEventEventArgs (a.MouseEvent));

		if (a.MouseEvent.Handled) {
			return;
		}

		if (MouseGrabView != null) {
			// If the mouse is grabbed, send the event to the view that grabbed it.
			// The coordinates are relative to the Bounds of the view that grabbed the mouse.
			var newxy = MouseGrabView.ScreenToFrame (a.MouseEvent.X, a.MouseEvent.Y);
			var nme = new MouseEvent () {
				X = newxy.X,
				Y = newxy.Y,
				Flags = a.MouseEvent.Flags,
				OfX = a.MouseEvent.X - newxy.X,
				OfY = a.MouseEvent.Y - newxy.Y,
				View = view
			};
			if (OutsideRect (new Point (nme.X, nme.Y), MouseGrabView.Bounds)) {
				// The mouse has moved outside the bounds of the the view that
				// grabbed the mouse, so we tell the view that last got 
				// OnMouseEnter the mouse is leaving
				// BUGBUG: That sentence makes no sense. Either I'm missing something
				// or this logic is flawed.
				_mouseEnteredView?.OnMouseLeave (a.MouseEvent);
			}
			//System.Diagnostics.Debug.WriteLine ($"{nme.Flags};{nme.X};{nme.Y};{mouseGrabView}");
			if (MouseGrabView?.OnMouseEvent (nme) == true) {
				return;
			}
		}

		if ((view == null || view == OverlappedTop) &&
		Current is { Modal: false } && OverlappedTop != null &&
		a.MouseEvent.Flags != MouseFlags.ReportMousePosition &&
		a.MouseEvent.Flags != 0) {

			var top = FindDeepestTop (Top, a.MouseEvent.X, a.MouseEvent.Y, out _, out _);
			view = View.FindDeepestView (top, a.MouseEvent.X, a.MouseEvent.Y, out screenX, out screenY);

			if (view != null && view != OverlappedTop && top != Current) {
				MoveCurrent ((Toplevel)top);
			}
		}

		bool AdornmentHandledMouseEvent(Adornment frame)
		{
			if (frame?.Thickness.Contains (frame.FrameToScreen (), a.MouseEvent.X, a.MouseEvent.Y) ?? false) {
				var boundsPoint = frame.ScreenToBounds (a.MouseEvent.X, a.MouseEvent.Y);
				var me = new MouseEvent () {
					X = boundsPoint.X,
					Y = boundsPoint.Y,
					Flags = a.MouseEvent.Flags,
					OfX = boundsPoint.X,
					OfY = boundsPoint.Y,
					View = frame
				};
				frame.OnMouseEvent (me);
				return true;
			}
			return false;
		}

		if (view != null) {
			// Work inside-out (Padding, Border, Margin)
			// TODO: Debate whether inside-out or outside-in is the right strategy
			if (AdornmentHandledMouseEvent(view?.Padding)) {
				return;
			}
			if (AdornmentHandledMouseEvent(view?.Border)) {
				if (view is Toplevel) {
					// TODO: This is a temporary hack to work around the fact that 
					// drag handling is handled in Toplevel (See Issue #2537)

					var me = new MouseEvent () {
						X = screenX,
						Y = screenY,
						Flags = a.MouseEvent.Flags,
						OfX = screenX,
						OfY = screenY,
						View = view
					};

					if (_mouseEnteredView == null) {
						_mouseEnteredView = view;
						view.OnMouseEnter (me);
					} else if (_mouseEnteredView != view) {
						_mouseEnteredView.OnMouseLeave (me);
						view.OnMouseEnter (me);
						_mouseEnteredView = view;
					}

					if (!view.WantMousePositionReports && a.MouseEvent.Flags == MouseFlags.ReportMousePosition) {
						return;
					}

					WantContinuousButtonPressedView = view.WantContinuousButtonPressed ? view : null;

					if (view.OnMouseEvent (me)) {
						// Should we bubble up the event, if it is not handled?
						//return;
					}

					BringOverlappedTopToFront ();
				}
				return;
			}

			if (AdornmentHandledMouseEvent(view?.Margin)) {
				return;
			}

			var bounds = view.BoundsToScreen (view.Bounds);
			if (bounds.Contains (a.MouseEvent.X, a.MouseEvent.Y)) {
				var boundsPoint = view.ScreenToBounds (a.MouseEvent.X, a.MouseEvent.Y);
				var me = new MouseEvent () {
					X = boundsPoint.X,
					Y = boundsPoint.Y,
					Flags = a.MouseEvent.Flags,
					OfX = boundsPoint.X,
					OfY = boundsPoint.Y,
					View = view
				};

				if (_mouseEnteredView == null) {
					_mouseEnteredView = view;
					view.OnMouseEnter (me);
				} else if (_mouseEnteredView != view) {
					_mouseEnteredView.OnMouseLeave (me);
					view.OnMouseEnter (me);
					_mouseEnteredView = view;
				}

				if (!view.WantMousePositionReports && a.MouseEvent.Flags == MouseFlags.ReportMousePosition) {
					return;
				}

				WantContinuousButtonPressedView = view.WantContinuousButtonPressed ? view : null;

				if (view.OnMouseEvent (me)) {
					// Should we bubble up the event, if it is not handled?
					//return;
				}

				BringOverlappedTopToFront ();
			}
		}
	}
	#endregion Mouse handling

	#region Keyboard handling
	static Key _alternateForwardKey = Key.Empty; // Defined in config.json

	/// <summary>
	/// Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
	[JsonConverter (typeof (KeyJsonConverter))]
	public static Key AlternateForwardKey {
		get => _alternateForwardKey;
		set {
			if (_alternateForwardKey != value) {
				var oldKey = _alternateForwardKey;
				_alternateForwardKey = value;
				OnAlternateForwardKeyChanged (new KeyChangedEventArgs (oldKey, value));
			}
		}
	}

	static void OnAlternateForwardKeyChanged (KeyChangedEventArgs e)
	{
		foreach (var top in _topLevels.ToArray ()) {
			top.OnAlternateForwardKeyChanged (e);
		}
	}

	static Key _alternateBackwardKey = Key.Empty; // Defined in config.json

	/// <summary>
	/// Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
	[JsonConverter (typeof (KeyJsonConverter))]
	public static Key AlternateBackwardKey {
		get => _alternateBackwardKey;
		set {
			if (_alternateBackwardKey != value) {
				var oldKey = _alternateBackwardKey;
				_alternateBackwardKey = value;
				OnAlternateBackwardKeyChanged (new KeyChangedEventArgs (oldKey, value));
			}
		}
	}

	static void OnAlternateBackwardKeyChanged (KeyChangedEventArgs oldKey)
	{
		foreach (var top in _topLevels.ToArray ()) {
			top.OnAlternateBackwardKeyChanged (oldKey);
		}
	}

	static Key _quitKey = Key.Empty; // Defined in config.json

	/// <summary>
	/// Gets or sets the key to quit the application.
	/// </summary>
	[SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
	[JsonConverter (typeof (KeyJsonConverter))]
	public static Key QuitKey {
		get => _quitKey;
		set {
			if (_quitKey != value) {
				var oldKey = _quitKey;
				_quitKey = value;
				OnQuitKeyChanged (new KeyChangedEventArgs (oldKey, value));
			}
		}
	}

	static void OnQuitKeyChanged (KeyChangedEventArgs e)
	{
		// Duplicate the list so if it changes during enumeration we're safe
		foreach (var top in _topLevels.ToArray ()) {
			top.OnQuitKeyChanged (e);
		}
	}

	/// <summary>
	/// Event fired when the user presses a key. Fired by <see cref="OnKeyDown"/>. 
	/// <para>
	/// Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and
	/// to prevent additional processing.
	/// </para>
	/// </summary>
	/// <remarks>
	/// All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Curses)
	/// do not support firing the <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
	/// <para>
	/// Fired after <see cref="KeyDown"/> and before <see cref="KeyUp"/>.
	/// </para>
	/// </remarks>
	public static event EventHandler<Key> KeyDown;

	/// <summary>
	/// Called by the <see cref="ConsoleDriver"/> when the user presses a key.
	/// Fires the <see cref="KeyDown"/> event
	/// then calls <see cref="View.NewKeyDownEvent"/> on all top level views.
	/// Called after <see cref="OnKeyDown"/> and before <see cref="OnKeyUp"/>.
	/// </summary>
	/// <remarks>
	/// Can be used to simulate key press events.
	/// </remarks>
	/// <param name="keyEvent"></param>
	/// <returns><see langword="true"/> if the key was handled.</returns>
	public static bool OnKeyDown (Key keyEvent)
	{
		if (!_initialized) {
			return true;
		}

		KeyDown?.Invoke (null, keyEvent);
		if (keyEvent.Handled) {
			return true;
		}

		foreach (var topLevel in _topLevels.ToList ()) {
			if (topLevel.NewKeyDownEvent (keyEvent)) {
				return true;
			}
			if (topLevel.Modal) {
				break;
			}
		}

		// Invoke any Global KeyBindings
		foreach (var topLevel in _topLevels.ToList ()) {
			foreach (var view in topLevel.Subviews.Where (v => v.KeyBindings.TryGet (keyEvent.KeyCode, KeyBindingScope.Application, out var _))) {
				if (view.KeyBindings.TryGet (keyEvent.KeyCode, KeyBindingScope.Application, out var _)) {
					keyEvent.Scope = KeyBindingScope.Application;
					bool? handled = view.OnInvokingKeyBindings (keyEvent);
					if (handled != null && (bool)handled) {
						return true;
					}
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Event fired when the user releases a key. Fired by <see cref="OnKeyUp"/>.
	/// <para>
	/// Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and
	/// to prevent additional processing.
	/// </para>
	/// </summary>
	/// <remarks>
	/// All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Curses)
	/// do not support firing the <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
	/// <para>
	/// Fired after <see cref="KeyDown"/>.
	/// </para>
	/// </remarks>
	public static event EventHandler<Key> KeyUp;

	/// <summary>
	/// Called by the <see cref="ConsoleDriver"/> when the user releases a key.
	/// Fires the <see cref="KeyUp"/> event
	/// then calls <see cref="View.NewKeyUpEvent"/> on all top level views.
	/// Called after <see cref="OnKeyDown"/>.
	/// </summary>
	/// <remarks>
	/// Can be used to simulate key press events.
	/// </remarks>
	/// <param name="a"></param>
	/// <returns><see langword="true"/> if the key was handled.</returns>
	public static bool OnKeyUp (Key a)
	{
		if (!_initialized) {
			return true;
		}

		KeyUp?.Invoke (null, a);
		if (a.Handled) {
			return true;
		}
		foreach (var topLevel in _topLevels.ToList ()) {
			if (topLevel.NewKeyUpEvent (a)) {
				return true;
			}
			if (topLevel.Modal) {
				break;
			}
		}
		return false;
	}
	#endregion Keyboard handling
}
/// <summary>
/// Event arguments for the <see cref="Application.Iteration"/> event.
/// </summary>
public class IterationEventArgs {
}