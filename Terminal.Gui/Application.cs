using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>A static, singleton class representing the application. This class is the entry point for the application.</summary>
/// <example>
///     <code>
///     Application.Init();
///     var win = new Window ($"Example App ({Application.QuitKey} to quit)");
///     Application.Run(win);
///     win.Dispose();
///     Application.Shutdown();
///     </code>
/// </example>
/// <remarks>TODO: Flush this out.</remarks>
public static partial class Application
{
    // For Unit testing - ignores UseSystemConsole
    internal static bool _forceFakeConsole;

    /// <summary>Gets the <see cref="ConsoleDriver"/> that has been selected. See also <see cref="ForceDriver"/>.</summary>
    public static ConsoleDriver Driver { get; internal set; }

    /// <summary>
    ///     Gets or sets whether <see cref="Application.Driver"/> will be forced to output only the 16 colors defined in
    ///     <see cref="ColorName"/>. The default is <see langword="false"/>, meaning 24-bit (TrueColor) colors will be output
    ///     as long as the selected <see cref="ConsoleDriver"/> supports TrueColor.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool Force16Colors { get; set; }

    /// <summary>
    ///     Forces the use of the specified driver (one of "fake", "ansi", "curses", "net", or "windows"). If not
    ///     specified, the driver is selected based on the platform.
    /// </summary>
    /// <remarks>
    ///     Note, <see cref="Application.Init(ConsoleDriver, string)"/> will override this configuration setting if called
    ///     with either `driver` or `driverName` specified.
    /// </remarks>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static string ForceDriver { get; set; } = string.Empty;

    /// <summary>Gets all cultures supported by the application without the invariant language.</summary>
    public static List<CultureInfo> SupportedCultures { get; private set; }

    internal static List<CultureInfo> GetSupportedCultures ()
    {
        CultureInfo [] culture = CultureInfo.GetCultures (CultureTypes.AllCultures);

        // Get the assembly
        var assembly = Assembly.GetExecutingAssembly ();

        //Find the location of the assembly
        string assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;

        // Find the resource file name of the assembly
        var resourceFilename = $"{Path.GetFileNameWithoutExtension (assembly.Location)}.resources.dll";

        // Return all culture for which satellite folder found with culture code.
        return culture.Where (
                              cultureInfo =>
                                  Directory.Exists (Path.Combine (assemblyLocation, cultureInfo.Name))
                                  && File.Exists (Path.Combine (assemblyLocation, cultureInfo.Name, resourceFilename))
                             )
                      .ToList ();
    }

    // When `End ()` is called, it is possible `RunState.Toplevel` is a different object than `Top`.
    // This variable is set in `End` in this case so that `Begin` correctly sets `Top`.
    private static Toplevel _cachedRunStateToplevel;

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
        foreach (Toplevel t in _topLevels)
        {
            t.Running = false;
#if DEBUG_IDISPOSABLE

            // Don't dispose the toplevels. It's up to caller dispose them
            Debug.Assert (t.WasDisposed);
#endif
        }

        _topLevels.Clear ();
        Current = null;
#if DEBUG_IDISPOSABLE

        // Don't dispose the Top. It's up to caller dispose it
        if (Top is { })
        {
            Debug.Assert (Top.WasDisposed);

            // If End wasn't called _cachedRunStateToplevel may be null
            if (_cachedRunStateToplevel is { })
            {
                Debug.Assert (_cachedRunStateToplevel.WasDisposed);
                Debug.Assert (_cachedRunStateToplevel == Top);
            }
        }
#endif
        Top = null;
        _cachedRunStateToplevel = null;

        // MainLoop stuff
        MainLoop?.Dispose ();
        MainLoop = null;
        _mainThreadId = -1;
        Iteration = null;
        EndAfterFirstIteration = false;

        // Driver stuff
        if (Driver is { })
        {
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
        SynchronizationContext.SetSynchronizationContext (null);
    }

    #region Initialization (Init/Shutdown)

    /// <summary>Initializes a new instance of <see cref="Terminal.Gui"/> Application.</summary>
    /// <para>Call this method once per instance (or after <see cref="Shutdown"/> has been called).</para>
    /// <para>
    ///     This function loads the right <see cref="ConsoleDriver"/> for the platform, Creates a <see cref="Toplevel"/>. and
    ///     assigns it to <see cref="Top"/>
    /// </para>
    /// <para>
    ///     <see cref="Shutdown"/> must be called when the application is closing (typically after
    ///     <see cref="Run(Func{Exception, bool}, ConsoleDriver)"/> has returned) to ensure resources are cleaned up and
    ///     terminal settings
    ///     restored.
    /// </para>
    /// <para>
    ///     The <see cref="Run{T}(Func{Exception, bool}, ConsoleDriver)"/> function combines
    ///     <see cref="Init(ConsoleDriver, string)"/> and <see cref="Run(Toplevel, Func{Exception, bool}, ConsoleDriver)"/>
    ///     into a single
    ///     call. An application cam use <see cref="Run{T}(Func{Exception, bool}, ConsoleDriver)"/> without explicitly calling
    ///     <see cref="Init(ConsoleDriver, string)"/>.
    /// </para>
    /// <param name="driver">
    ///     The <see cref="ConsoleDriver"/> to use. If neither <paramref name="driver"/> or
    ///     <paramref name="driverName"/> are specified the default driver for the platform will be used.
    /// </param>
    /// <param name="driverName">
    ///     The short name (e.g. "net", "windows", "ansi", "fake", or "curses") of the
    ///     <see cref="ConsoleDriver"/> to use. If neither <paramref name="driver"/> or <paramref name="driverName"/> are
    ///     specified the default driver for the platform will be used.
    /// </param>
    public static void Init (ConsoleDriver driver = null, string driverName = null) { InternalInit (driver, driverName); }

    internal static bool _initialized;
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
    internal static void InternalInit (
        ConsoleDriver driver = null,
        string driverName = null,
        bool calledViaRunT = false
    )
    {
        if (_initialized && driver is null)
        {
            return;
        }

        if (_initialized)
        {
            throw new InvalidOperationException ("Init has already been called and must be bracketed by Shutdown.");
        }

        if (!calledViaRunT)
        {
            // Reset all class variables (Application is a singleton).
            ResetState ();
        }

        // For UnitTests
        if (driver is { })
        {
            Driver = driver;
        }

        // Start the process of configuration management.
        // Note that we end up calling LoadConfigurationFromAllSources
        // multiple times. We need to do this because some settings are only
        // valid after a Driver is loaded. In this cases we need just 
        // `Settings` so we can determine which driver to use.
        // Don't reset, so we can inherit the theme from the previous run.
        Load ();
        Apply ();

        // Ignore Configuration for ForceDriver if driverName is specified
        if (!string.IsNullOrEmpty (driverName))
        {
            ForceDriver = driverName;
        }

        if (Driver is null)
        {
            PlatformID p = Environment.OSVersion.Platform;

            if (string.IsNullOrEmpty (ForceDriver))
            {
                if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
                {
                    Driver = new WindowsDriver ();
                }
                else
                {
                    Driver = new CursesDriver ();
                }
            }
            else
            {
                List<Type> drivers = GetDriverTypes ();
                Type driverType = drivers.FirstOrDefault (t => t.Name.Equals (ForceDriver, StringComparison.InvariantCultureIgnoreCase));

                if (driverType is { })
                {
                    Driver = (ConsoleDriver)Activator.CreateInstance (driverType);
                }
                else
                {
                    throw new ArgumentException (
                                                 $"Invalid driver name: {ForceDriver}. Valid names are {string.Join (", ", drivers.Select (t => t.Name))}"
                                                );
                }
            }
        }

        try
        {
            MainLoop = Driver.Init ();
        }
        catch (InvalidOperationException ex)
        {
            // This is a case where the driver is unable to initialize the console.
            // This can happen if the console is already in use by another process or
            // if running in unit tests.
            // In this case, we want to throw a more specific exception.
            throw new InvalidOperationException (
                                                 "Unable to initialize the console. This can happen if the console is already in use by another process or in unit tests.",
                                                 ex
                                                );
        }

        Driver.SizeChanged += (s, args) => OnSizeChanging (args);
        Driver.KeyDown += (s, args) => OnKeyDown (args);
        Driver.KeyUp += (s, args) => OnKeyUp (args);
        Driver.MouseEvent += (s, args) => OnMouseEvent (args);

        SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext ());

        SupportedCultures = GetSupportedCultures ();
        _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        _initialized = true;
    }

    private static void Driver_SizeChanged (object sender, SizeChangedEventArgs e) { OnSizeChanging (e); }
    private static void Driver_KeyDown (object sender, Key e) { OnKeyDown (e); }
    private static void Driver_KeyUp (object sender, Key e) { OnKeyUp (e); }
    private static void Driver_MouseEvent (object sender, MouseEventEventArgs e) { OnMouseEvent (e); }

    /// <summary>Gets of list of <see cref="ConsoleDriver"/> types that are available.</summary>
    /// <returns></returns>
    public static List<Type> GetDriverTypes ()
    {
        // use reflection to get the list of drivers
        List<Type> driverTypes = new ();

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ())
        {
            foreach (Type type in asm.GetTypes ())
            {
                if (type.IsSubclassOf (typeof (ConsoleDriver)) && !type.IsAbstract)
                {
                    driverTypes.Add (type);
                }
            }
        }

        return driverTypes;
    }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    /// <remarks>
    ///     Shutdown must be called for every call to <see cref="Init"/> or
    ///     <see cref="Application.Run(Toplevel, Func{Exception, bool}, ConsoleDriver)"/> to ensure all resources are cleaned
    ///     up (Disposed)
    ///     and terminal settings are restored.
    /// </remarks>
    public static void Shutdown ()
    {
        // TODO: Throw an exception if Init hasn't been called.
        ResetState ();
        PrintJsonErrors ();
    }

    #endregion Initialization (Init/Shutdown)

    #region Run (Begin, Run, End, Stop)

    /// <summary>
    ///     Notify that a new <see cref="RunState"/> was created (<see cref="Begin(Toplevel)"/> was called). The token is
    ///     created in <see cref="Begin(Toplevel)"/> and this event will be fired before that function exits.
    /// </summary>
    /// <remarks>
    ///     If <see cref="EndAfterFirstIteration"/> is <see langword="true"/> callers to <see cref="Begin(Toplevel)"/>
    ///     must also subscribe to <see cref="NotifyStopRunState"/> and manually dispose of the <see cref="RunState"/> token
    ///     when the application is done.
    /// </remarks>
    public static event EventHandler<RunStateEventArgs> NotifyNewRunState;

    /// <summary>Notify that a existent <see cref="RunState"/> is stopping (<see cref="End(RunState)"/> was called).</summary>
    /// <remarks>
    ///     If <see cref="EndAfterFirstIteration"/> is <see langword="true"/> callers to <see cref="Begin(Toplevel)"/>
    ///     must also subscribe to <see cref="NotifyStopRunState"/> and manually dispose of the <see cref="RunState"/> token
    ///     when the application is done.
    /// </remarks>
    public static event EventHandler<ToplevelEventArgs> NotifyStopRunState;

    /// <summary>Building block API: Prepares the provided <see cref="Toplevel"/> for execution.</summary>
    /// <returns>
    ///     The <see cref="RunState"/> handle that needs to be passed to the <see cref="End(RunState)"/> method upon
    ///     completion.
    /// </returns>
    /// <param name="toplevel">The <see cref="Toplevel"/> to prepare execution for.</param>
    /// <remarks>
    ///     This method prepares the provided <see cref="Toplevel"/> for running with the focus, it adds this to the list
    ///     of <see cref="Toplevel"/>s, lays out the Subviews, focuses the first element, and draws the <see cref="Toplevel"/>
    ///     in the screen. This is usually followed by executing the <see cref="RunLoop"/> method, and then the
    ///     <see cref="End(RunState)"/> method upon termination which will undo these changes.
    /// </remarks>
    public static RunState Begin (Toplevel toplevel)
    {
        ArgumentNullException.ThrowIfNull (toplevel);

#if DEBUG_IDISPOSABLE
        Debug.Assert (!toplevel.WasDisposed);

        if (_cachedRunStateToplevel is { } && _cachedRunStateToplevel != toplevel)
        {
            Debug.Assert (_cachedRunStateToplevel.WasDisposed);
        }
#endif

        if (toplevel.IsOverlappedContainer && OverlappedTop != toplevel && OverlappedTop is { })
        {
            throw new InvalidOperationException ("Only one Overlapped Container is allowed.");
        }

        // Ensure the mouse is ungrabbed.
        MouseGrabView = null;

        var rs = new RunState (toplevel);

        // View implements ISupportInitializeNotification which is derived from ISupportInitialize
        if (!toplevel.IsInitialized)
        {
            toplevel.BeginInit ();
            toplevel.EndInit ();
        }

#if DEBUG_IDISPOSABLE
        if (Top is { } && toplevel != Top && !_topLevels.Contains (Top))
        {
            // This assertion confirm if the Top was already disposed
            Debug.Assert (Top.WasDisposed);
            Debug.Assert (Top == _cachedRunStateToplevel);
        }
#endif

        lock (_topLevels)
        {
            if (Top is { } && toplevel != Top && !_topLevels.Contains (Top))
            {
                // If Top was already disposed and isn't on the Toplevels Stack,
                // clean it up here if is the same as _cachedRunStateToplevel
                if (Top == _cachedRunStateToplevel)
                {
                    Top = null;
                }
                else
                {
                    // Probably this will never hit
                    throw new ObjectDisposedException (Top.GetType ().FullName);
                }
            }
            else if (OverlappedTop is { } && toplevel != Top && _topLevels.Contains (Top))
            {
                Top.OnLeave (toplevel);
            }

            // BUGBUG: We should not depend on `Id` internally. 
            // BUGBUG: It is super unclear what this code does anyway.
            if (string.IsNullOrEmpty (toplevel.Id))
            {
                var count = 1;
                var id = (_topLevels.Count + count).ToString ();

                while (_topLevels.Count > 0 && _topLevels.FirstOrDefault (x => x.Id == id) is { })
                {
                    count++;
                    id = (_topLevels.Count + count).ToString ();
                }

                toplevel.Id = (_topLevels.Count + count).ToString ();

                _topLevels.Push (toplevel);
            }
            else
            {
                Toplevel dup = _topLevels.FirstOrDefault (x => x.Id == toplevel.Id);

                if (dup is null)
                {
                    _topLevels.Push (toplevel);
                }
            }

            if (_topLevels.FindDuplicates (new ToplevelEqualityComparer ()).Count > 0)
            {
                throw new ArgumentException ("There are duplicates Toplevel IDs");
            }
        }

        if (Top is null || toplevel.IsOverlappedContainer)
        {
            Top = toplevel;
        }

        var refreshDriver = true;

        if (OverlappedTop is null
            || toplevel.IsOverlappedContainer
            || (Current?.Modal == false && toplevel.Modal)
            || (Current?.Modal == false && !toplevel.Modal)
            || (Current?.Modal == true && toplevel.Modal))
        {
            if (toplevel.Visible)
            {
                Current?.OnDeactivate (toplevel);
                Toplevel previousCurrent = Current;
                Current = toplevel;
                Current.OnActivate (previousCurrent);

                SetCurrentOverlappedAsTop ();
            }
            else
            {
                refreshDriver = false;
            }
        }
        else if ((OverlappedTop != null
                  && toplevel != OverlappedTop
                  && Current?.Modal == true
                  && !_topLevels.Peek ().Modal)
                 || (OverlappedTop is { } && toplevel != OverlappedTop && Current?.Running == false))
        {
            refreshDriver = false;
            MoveCurrent (toplevel);
        }
        else
        {
            refreshDriver = false;
            MoveCurrent (Current);
        }

        toplevel.SetRelativeLayout (Driver.Bounds);

        // BUGBUG: This call is likely not needed.
        toplevel.LayoutSubviews ();
        toplevel.PositionToplevels ();
        toplevel.FocusFirst ();

        if (refreshDriver)
        {
            OverlappedTop?.OnChildLoaded (toplevel);
            toplevel.OnLoaded ();
            toplevel.SetNeedsDisplay ();
            toplevel.Draw ();
            toplevel.PositionCursor ();
            Driver.Refresh ();
        }

        NotifyNewRunState?.Invoke (toplevel, new (rs));

        return rs;
    }

    /// <summary>
    ///     Runs the application by creating a <see cref="Toplevel"/> object and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool}, ConsoleDriver)"/>.
    /// </summary>
    /// <remarks>
    ///     <para>Calling <see cref="Init"/> first is not needed as this function will initialize the application.</para>
    ///     <para>
    ///         <see cref="Shutdown"/> must be called when the application is closing (typically after Run> has returned) to
    ///         ensure resources are cleaned up and terminal settings restored.
    ///     </para>
    ///     <para>
    ///         The caller is responsible for disposing the object returned by this method.
    ///     </para>
    /// </remarks>
    /// <returns>The created <see cref="Toplevel"/> object. The caller is responsible for disposing this object.</returns>
    public static Toplevel Run (Func<Exception, bool> errorHandler = null, ConsoleDriver driver = null) { return Run<Toplevel> (errorHandler, driver); }

    /// <summary>
    ///     Runs the application by creating a <see cref="Toplevel"/>-derived object of type <c>T</c> and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool}, ConsoleDriver)"/>.
    /// </summary>
    /// <remarks>
    ///     <para>Calling <see cref="Init"/> first is not needed as this function will initialize the application.</para>
    ///     <para>
    ///         <see cref="Shutdown"/> must be called when the application is closing (typically after Run> has returned) to
    ///         ensure resources are cleaned up and terminal settings restored.
    ///     </para>
    ///     <para>
    ///         The caller is responsible for disposing the object returned by this method.
    ///     </para>
    /// </remarks>
    /// <param name="errorHandler"></param>
    /// <param name="driver">
    ///     The <see cref="ConsoleDriver"/> to use. If not specified the default driver for the platform will
    ///     be used ( <see cref="WindowsDriver"/>, <see cref="CursesDriver"/>, or <see cref="NetDriver"/>). Must be
    ///     <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>The created T object. The caller is responsible for disposing this object.</returns>
    public static T Run<T> (Func<Exception, bool> errorHandler = null, ConsoleDriver driver = null)
        where T : Toplevel, new()
    {
        var top = new T ();

        Run (top, errorHandler, driver);

        return top;
    }

    /// <summary>Runs the Application using the provided <see cref="Toplevel"/> view.</summary>
    /// <remarks>
    ///     <para>
    ///         This method is used to start processing events for the main application, but it is also used to run other
    ///         modal <see cref="View"/>s such as <see cref="Dialog"/> boxes.
    ///     </para>
    ///     <para>
    ///         To make a <see cref="Run(Toplevel, Func{Exception, bool}, ConsoleDriver)"/> stop execution, call
    ///         <see cref="Application.RequestStop"/>.
    ///     </para>
    ///     <para>
    ///         Calling <see cref="Run(Toplevel, Func{Exception, bool}, ConsoleDriver)"/> is equivalent to calling
    ///         <see cref="Begin(Toplevel)"/>, followed by <see cref="RunLoop(RunState)"/>, and then calling
    ///         <see cref="End(RunState)"/>.
    ///     </para>
    ///     <para>
    ///         Alternatively, to have a program control the main loop and process events manually, call
    ///         <see cref="Begin(Toplevel)"/> to set things up manually and then repeatedly call
    ///         <see cref="RunLoop(RunState)"/> with the wait parameter set to false. By doing this the
    ///         <see cref="RunLoop(RunState)"/> method will only process any pending events, timers, idle handlers and then
    ///         return control immediately.
    ///     </para>
    ///     <para>Calling <see cref="Init"/> first is not needed as this function will initialize the application.</para>
    ///     <para>
    ///         RELEASE builds only: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, if <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the <see cref="RunLoop(RunState)"/> will resume; otherwise this method will
    ///         exit.
    ///     </para>
    /// </remarks>
    /// <param name="view">The <see cref="Toplevel"/> to run as a modal.</param>
    /// <param name="errorHandler">
    ///     RELEASE builds only: Handler for any unhandled exceptions (resumes when returns true,
    ///     rethrows when null).
    /// </param>
    /// <param name="driver">
    ///     The <see cref="ConsoleDriver"/> to use. If not specified the default driver for the platform will
    ///     be used ( <see cref="WindowsDriver"/>, <see cref="CursesDriver"/>, or <see cref="NetDriver"/>). Must be
    ///     <see langword="null"/> if <see cref="Init"/> was called.
    /// </param>
    public static void Run (Toplevel view, Func<Exception, bool> errorHandler = null, ConsoleDriver driver = null)
    {
        ArgumentNullException.ThrowIfNull (view);
        
        if (_initialized)
        {
            if (Driver is null)
            {
                // Disposing before throwing
                view.Dispose ();

                // This code path should be impossible because Init(null, null) will select the platform default driver
                throw new InvalidOperationException (
                                                     "Init() completed without a driver being set (this should be impossible); Run<T>() cannot be called."
                                                    );
            }
        }
        else
        {
            // Init() has NOT been called.
            InternalInit (driver, null, true);
        }

        var resume = true;

        while (resume)
        {
#if !DEBUG
            try
            {
#endif
            resume = false;
            RunState runState = Begin (view);

            // If EndAfterFirstIteration is true then the user must dispose of the runToken
            // by using NotifyStopRunState event.
            RunLoop (runState);

            if (runState.Toplevel is null)
            {
#if DEBUG_IDISPOSABLE
                Debug.Assert (_topLevels.Count == 0);
#endif
                runState.Dispose ();

                return;
            }

            if (!EndAfterFirstIteration)
            {
                End (runState);
            }
#if !DEBUG
            }
            catch (Exception error)
            {
                if (errorHandler is null)
                {
                    throw;
                }

                resume = errorHandler (error);
            }
#endif
        }
    }

    /// <summary>Adds a timeout to the application.</summary>
    /// <remarks>
    ///     When time specified passes, the callback will be invoked. If the callback returns true, the timeout will be
    ///     reset, repeating the invocation. If it returns false, the timeout will stop and be removed. The returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="RemoveTimeout(object)"/>.
    /// </remarks>
    public static object AddTimeout (TimeSpan time, Func<bool> callback) { return MainLoop?.AddTimeout (time, callback); }

    /// <summary>Removes a previously scheduled timeout</summary>
    /// <remarks>The token parameter is the value returned by <see cref="AddTimeout"/>.</remarks>
    /// Returns
    /// <c>true</c>
    /// if the timeout is successfully removed; otherwise,
    /// <c>false</c>
    /// .
    /// This method also returns
    /// <c>false</c>
    /// if the timeout is not found.
    public static bool RemoveTimeout (object token) { return MainLoop?.RemoveTimeout (token) ?? false; }

    /// <summary>Runs <paramref name="action"/> on the thread that is processing events</summary>
    /// <param name="action">the action to be invoked on the main processing thread.</param>
    public static void Invoke (Action action)
    {
        MainLoop?.AddIdle (
                           () =>
                           {
                               action ();

                               return false;
                           }
                          );
    }

    // TODO: Determine if this is really needed. The only code that calls WakeUp I can find
    // is ProgressBarStyles and it's not clear it needs to.
    /// <summary>Wakes up the running application that might be waiting on input.</summary>
    public static void Wakeup () { MainLoop?.Wakeup (); }

    /// <summary>Triggers a refresh of the entire display.</summary>
    public static void Refresh ()
    {
        // TODO: Figure out how to remove this call to ClearContents. Refresh should just repaint damaged areas, not clear
        Driver.ClearContents ();
        View last = null;

        foreach (Toplevel v in _topLevels.Reverse ())
        {
            if (v.Visible)
            {
                v.SetNeedsDisplay ();
                v.SetSubViewNeedsDisplay ();
                v.Draw ();
            }

            last = v;
        }

        last?.PositionCursor ();
        Driver.Refresh ();
    }

    /// <summary>This event is raised on each iteration of the main loop.</summary>
    /// <remarks>See also <see cref="Timeout"/></remarks>
    public static event EventHandler<IterationEventArgs> Iteration;

    /// <summary>The <see cref="MainLoop"/> driver for the application</summary>
    /// <value>The main loop.</value>
    internal static MainLoop MainLoop { get; private set; }

    /// <summary>
    ///     Set to true to cause <see cref="End"/> to be called after the first iteration. Set to false (the default) to
    ///     cause the application to continue running until Application.RequestStop () is called.
    /// </summary>
    public static bool EndAfterFirstIteration { get; set; }

    /// <summary>Building block API: Runs the main loop for the created <see cref="Toplevel"/>.</summary>
    /// <param name="state">The state returned by the <see cref="Begin(Toplevel)"/> method.</param>
    public static void RunLoop (RunState state)
    {
        ArgumentNullException.ThrowIfNull (state);
        ObjectDisposedException.ThrowIf (state.Toplevel is null, "state");

        var firstIteration = true;

        for (state.Toplevel.Running = true; state.Toplevel?.Running == true;)
        {
            MainLoop.Running = true;

            if (EndAfterFirstIteration && !firstIteration)
            {
                return;
            }

            RunIteration (ref state, ref firstIteration);
        }

        MainLoop.Running = false;

        // Run one last iteration to consume any outstanding input events from Driver
        // This is important for remaining OnKeyUp events.
        RunIteration (ref state, ref firstIteration);
    }

    /// <summary>Run one application iteration.</summary>
    /// <param name="state">The state returned by <see cref="Begin(Toplevel)"/>.</param>
    /// <param name="firstIteration">
    ///     Set to <see langword="true"/> if this is the first run loop iteration. Upon return, it
    ///     will be set to <see langword="false"/> if at least one iteration happened.
    /// </param>
    public static void RunIteration (ref RunState state, ref bool firstIteration)
    {
        if (MainLoop.Running && MainLoop.EventsPending ())
        {
            // Notify Toplevel it's ready
            if (firstIteration)
            {
                state.Toplevel.OnReady ();
            }

            MainLoop.RunIteration ();
            Iteration?.Invoke (null, new ());
            EnsureModalOrVisibleAlwaysOnTop (state.Toplevel);

            if (state.Toplevel != Current)
            {
                OverlappedTop?.OnDeactivate (state.Toplevel);
                state.Toplevel = Current;
                OverlappedTop?.OnActivate (state.Toplevel);
                Top.SetSubViewNeedsDisplay ();
                Refresh ();
            }
        }

        firstIteration = false;

        if (Current == null)
        {
            return;
        }

        if (state.Toplevel != Top && (Top.NeedsDisplay || Top.SubViewNeedsDisplay || Top.LayoutNeeded))
        {
            state.Toplevel.SetNeedsDisplay (state.Toplevel.Frame);
            Top.Draw ();

            foreach (Toplevel top in _topLevels.Reverse ())
            {
                if (top != Top && top != state.Toplevel)
                {
                    top.SetNeedsDisplay ();
                    top.SetSubViewNeedsDisplay ();
                    top.Draw ();
                }
            }
        }

        if (_topLevels.Count == 1
            && state.Toplevel == Top
            && (Driver.Cols != state.Toplevel.Frame.Width
                || Driver.Rows != state.Toplevel.Frame.Height)
            && (state.Toplevel.NeedsDisplay
                || state.Toplevel.SubViewNeedsDisplay
                || state.Toplevel.LayoutNeeded))
        {
            Driver.ClearContents ();
        }

        if (state.Toplevel.NeedsDisplay || state.Toplevel.SubViewNeedsDisplay || state.Toplevel.LayoutNeeded || OverlappedChildNeedsDisplay ())
        {
            state.Toplevel.Draw ();
            state.Toplevel.PositionCursor ();
            Driver.Refresh ();
        }
        else
        {
            Driver.UpdateCursor ();
        }

        if (state.Toplevel != Top && !state.Toplevel.Modal && (Top.NeedsDisplay || Top.SubViewNeedsDisplay || Top.LayoutNeeded))
        {
            Top.Draw ();
        }
    }

    /// <summary>Stops the provided <see cref="Toplevel"/>, causing or the <paramref name="top"/> if provided.</summary>
    /// <param name="top">The <see cref="Toplevel"/> to stop.</param>
    /// <remarks>
    ///     <para>This will cause <see cref="Application.Run(Toplevel, Func{Exception, bool}, ConsoleDriver)"/> to return.</para>
    ///     <para>
    ///         Calling <see cref="Application.RequestStop"/> is equivalent to setting the <see cref="Toplevel.Running"/>
    ///         property on the currently running <see cref="Toplevel"/> to false.
    ///     </para>
    /// </remarks>
    public static void RequestStop (Toplevel top = null)
    {
        if (OverlappedTop is null || top is null || (OverlappedTop is null && top is { }))
        {
            top = Current;
        }

        if (OverlappedTop != null
            && top.IsOverlappedContainer
            && top?.Running == true
            && (Current?.Modal == false || (Current?.Modal == true && Current?.Running == false)))
        {
            OverlappedTop.RequestStop ();
        }
        else if (OverlappedTop != null
                 && top != Current
                 && Current?.Running == true
                 && Current?.Modal == true
                 && top.Modal
                 && top.Running)
        {
            var ev = new ToplevelClosingEventArgs (Current);
            Current.OnClosing (ev);

            if (ev.Cancel)
            {
                return;
            }

            ev = new (top);
            top.OnClosing (ev);

            if (ev.Cancel)
            {
                return;
            }

            Current.Running = false;
            OnNotifyStopRunState (Current);
            top.Running = false;
            OnNotifyStopRunState (top);
        }
        else if ((OverlappedTop != null
                  && top != OverlappedTop
                  && top != Current
                  && Current?.Modal == false
                  && Current?.Running == true
                  && !top.Running)
                 || (OverlappedTop != null
                     && top != OverlappedTop
                     && top != Current
                     && Current?.Modal == false
                     && Current?.Running == false
                     && !top.Running
                     && _topLevels.ToArray () [1].Running))
        {
            MoveCurrent (top);
        }
        else if (OverlappedTop != null
                 && Current != top
                 && Current?.Running == true
                 && !top.Running
                 && Current?.Modal == true
                 && top.Modal)
        {
            // The Current and the top are both modal so needed to set the Current.Running to false too.
            Current.Running = false;
            OnNotifyStopRunState (Current);
        }
        else if (OverlappedTop != null
                 && Current == top
                 && OverlappedTop?.Running == true
                 && Current?.Running == true
                 && top.Running
                 && Current?.Modal == true
                 && top.Modal)
        {
            // The OverlappedTop was requested to stop inside a modal Toplevel which is the Current and top,
            // both are the same, so needed to set the Current.Running to false too.
            Current.Running = false;
            OnNotifyStopRunState (Current);
        }
        else
        {
            Toplevel currentTop;

            if (top == Current || (Current?.Modal == true && !top.Modal))
            {
                currentTop = Current;
            }
            else
            {
                currentTop = top;
            }

            if (!currentTop.Running)
            {
                return;
            }

            var ev = new ToplevelClosingEventArgs (currentTop);
            currentTop.OnClosing (ev);

            if (ev.Cancel)
            {
                return;
            }

            currentTop.Running = false;
            OnNotifyStopRunState (currentTop);
        }
    }

    private static void OnNotifyStopRunState (Toplevel top)
    {
        if (EndAfterFirstIteration)
        {
            NotifyStopRunState?.Invoke (top, new (top));
        }
    }

    /// <summary>
    ///     Building block API: completes the execution of a <see cref="Toplevel"/> that was started with
    ///     <see cref="Begin(Toplevel)"/> .
    /// </summary>
    /// <param name="runState">The <see cref="RunState"/> returned by the <see cref="Begin(Toplevel)"/> method.</param>
    public static void End (RunState runState)
    {
        ArgumentNullException.ThrowIfNull (runState);

        if (OverlappedTop is { })
        {
            OverlappedTop.OnChildUnloaded (runState.Toplevel);
        }
        else
        {
            runState.Toplevel.OnUnloaded ();
        }

        // End the RunState.Toplevel 
        // First, take it off the Toplevel Stack
        if (_topLevels.Count > 0)
        {
            if (_topLevels.Peek () != runState.Toplevel)
            {
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
        if (OverlappedTop is { } && !runState.Toplevel.Modal && runState.Toplevel != OverlappedTop)
        {
            OverlappedTop.OnChildClosed (runState.Toplevel);
        }

        // Set Current and Top to the next TopLevel on the stack
        if (_topLevels.Count == 0)
        {
            Current = null;
        }
        else
        {
            Current = _topLevels.Peek ();

            if (_topLevels.Count == 1 && Current == OverlappedTop)
            {
                OverlappedTop.OnAllChildClosed ();
            }
            else
            {
                SetCurrentOverlappedAsTop ();
                runState.Toplevel.OnLeave (Current);
                Current.OnEnter (runState.Toplevel);
            }

            Refresh ();
        }

        // Don't dispose runState.Toplevel. It's up to caller dispose it
        // If it's not the same as the current in the RunIteration,
        // it will be fixed later in the next RunIteration.
        if (OverlappedTop is { } && !_topLevels.Contains (OverlappedTop))
        {
            _cachedRunStateToplevel = OverlappedTop;
        }
        else
        {
            _cachedRunStateToplevel = runState.Toplevel;
        }

        runState.Toplevel = null;
        runState.Dispose ();
    }

    #endregion Run (Begin, Run, End)

    #region Toplevel handling

    /// <summary>Holds the stack of TopLevel views.</summary>

    // BUGBUG: Techncally, this is not the full lst of TopLevels. THere be dragons hwre. E.g. see how Toplevel.Id is used. What
    // about TopLevels that are just a SubView of another View?
    internal static readonly Stack<Toplevel> _topLevels = new ();

    /// <summary>The <see cref="Toplevel"/> object used for the application on startup (<seealso cref="Application.Top"/>)</summary>
    /// <value>The top.</value>
    public static Toplevel Top { get; private set; }

    /// <summary>
    ///     The current <see cref="Toplevel"/> object. This is updated in <see cref="Application.Begin"/> enters and leaves to
    ///     point to the current
    ///     <see cref="Toplevel"/> .
    /// </summary>
    /// <remarks>
    ///     Only relevant in scenarios where <see cref="Toplevel.IsOverlappedContainer"/> is <see langword="true"/>.
    /// </remarks>
    /// <value>The current.</value>
    public static Toplevel Current { get; private set; }

    private static void EnsureModalOrVisibleAlwaysOnTop (Toplevel Toplevel)
    {
        if (!Toplevel.Running
            || (Toplevel == Current && Toplevel.Visible)
            || OverlappedTop == null
            || _topLevels.Peek ().Modal)
        {
            return;
        }

        foreach (Toplevel top in _topLevels.Reverse ())
        {
            if (top.Modal && top != Current)
            {
                MoveCurrent (top);

                return;
            }
        }

        if (!Toplevel.Visible && Toplevel == Current)
        {
            OverlappedMoveNext ();
        }
    }

#nullable enable
    private static Toplevel? FindDeepestTop (Toplevel start, int x, int y)
    {
        if (!start.Frame.Contains (x, y))
        {
            return null;
        }

        if (_topLevels is { Count: > 0 })
        {
            int rx = x - start.Frame.X;
            int ry = y - start.Frame.Y;

            foreach (Toplevel t in _topLevels)
            {
                if (t != Current)
                {
                    if (t != start && t.Visible && t.Frame.Contains (rx, ry))
                    {
                        start = t;

                        break;
                    }
                }
            }
        }

        return start;
    }
#nullable restore

    private static View FindTopFromView (View view)
    {
        View top = view?.SuperView is { } && view?.SuperView != Top
                       ? view.SuperView
                       : view;

        while (top?.SuperView is { } && top?.SuperView != Top)
        {
            top = top.SuperView;
        }

        return top;
    }

#nullable enable

    // Only return true if the Current has changed.
    private static bool MoveCurrent (Toplevel? top)
    {
        // The Current is modal and the top is not modal Toplevel then
        // the Current must be moved above the first not modal Toplevel.
        if (OverlappedTop is { }
            && top != OverlappedTop
            && top != Current
            && Current?.Modal == true
            && !_topLevels.Peek ().Modal)
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
            }

            var index = 0;
            Toplevel [] savedToplevels = _topLevels.ToArray ();

            foreach (Toplevel t in savedToplevels)
            {
                if (!t.Modal && t != Current && t != top && t != savedToplevels [index])
                {
                    lock (_topLevels)
                    {
                        _topLevels.MoveTo (top, index, new ToplevelEqualityComparer ());
                    }
                }

                index++;
            }

            return false;
        }

        // The Current and the top are both not running Toplevel then
        // the top must be moved above the first not running Toplevel.
        if (OverlappedTop is { }
            && top != OverlappedTop
            && top != Current
            && Current?.Running == false
            && top?.Running == false)
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
            }

            var index = 0;

            foreach (Toplevel t in _topLevels.ToArray ())
            {
                if (!t.Running && t != Current && index > 0)
                {
                    lock (_topLevels)
                    {
                        _topLevels.MoveTo (top, index - 1, new ToplevelEqualityComparer ());
                    }
                }

                index++;
            }

            return false;
        }

        if ((OverlappedTop is { } && top?.Modal == true && _topLevels.Peek () != top)
            || (OverlappedTop is { } && Current != OverlappedTop && Current?.Modal == false && top == OverlappedTop)
            || (OverlappedTop is { } && Current?.Modal == false && top != Current)
            || (OverlappedTop is { } && Current?.Modal == true && top == OverlappedTop))
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
                Current = top;
            }
        }

        return true;
    }
#nullable restore

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    /// <remarks>
    ///     Event handlers can set <see cref="SizeChangedEventArgs.Cancel"/> to <see langword="true"/> to prevent
    ///     <see cref="Application"/> from changing it's size to match the new terminal size.
    /// </remarks>
    public static event EventHandler<SizeChangedEventArgs> SizeChanging;

    /// <summary>
    ///     Called when the application's size changes. Sets the size of all <see cref="Toplevel"/>s and fires the
    ///     <see cref="SizeChanging"/> event.
    /// </summary>
    /// <param name="args">The new size.</param>
    /// <returns><see lanword="true"/>if the size was changed.</returns>
    public static bool OnSizeChanging (SizeChangedEventArgs args)
    {
        SizeChanging?.Invoke (null, args);

        if (args.Cancel)
        {
            return false;
        }

        foreach (Toplevel t in _topLevels)
        {
            t.SetRelativeLayout (Rectangle.Empty with { Size = args.Size });
            t.LayoutSubviews ();
            t.PositionToplevels ();
            t.OnSizeChanging (new (args.Size));
        }

        Refresh ();

        return true;
    }

    #endregion Toplevel handling

    #region Mouse handling

    /// <summary>Disable or enable the mouse. The mouse is enabled by default.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool IsMouseDisabled { get; set; }

    /// <summary>The current <see cref="View"/> object that wants continuous mouse button pressed events.</summary>
    public static View WantContinuousButtonPressedView { get; private set; }

    /// <summary>
    ///     Gets the view that grabbed the mouse (e.g. for dragging). When this is set, all mouse events will be routed to
    ///     this view until the view calls <see cref="UngrabMouse"/> or the mouse is released.
    /// </summary>
    public static View MouseGrabView { get; private set; }

    /// <summary>Invoked when a view wants to grab the mouse; can be canceled.</summary>
    public static event EventHandler<GrabMouseEventArgs> GrabbingMouse;

    /// <summary>Invoked when a view wants un-grab the mouse; can be canceled.</summary>
    public static event EventHandler<GrabMouseEventArgs> UnGrabbingMouse;

    /// <summary>Invoked after a view has grabbed the mouse.</summary>
    public static event EventHandler<ViewEventArgs> GrabbedMouse;

    /// <summary>Invoked after a view has un-grabbed the mouse.</summary>
    public static event EventHandler<ViewEventArgs> UnGrabbedMouse;

    /// <summary>
    ///     Grabs the mouse, forcing all mouse events to be routed to the specified view until <see cref="UngrabMouse"/>
    ///     is called.
    /// </summary>
    /// <param name="view">View that will receive all mouse events until <see cref="UngrabMouse"/> is invoked.</param>
    public static void GrabMouse (View view)
    {
        if (view is null)
        {
            return;
        }

        if (!OnGrabbingMouse (view))
        {
            OnGrabbedMouse (view);
            MouseGrabView = view;
        }
    }

    /// <summary>Releases the mouse grab, so mouse events will be routed to the view on which the mouse is.</summary>
    public static void UngrabMouse ()
    {
        if (MouseGrabView is null)
        {
            return;
        }

        if (!OnUnGrabbingMouse (MouseGrabView))
        {
            View view = MouseGrabView;
            MouseGrabView = null;
            OnUnGrabbedMouse (view);
        }
    }

    private static bool OnGrabbingMouse (View view)
    {
        if (view is null)
        {
            return false;
        }

        var evArgs = new GrabMouseEventArgs (view);
        GrabbingMouse?.Invoke (view, evArgs);

        return evArgs.Cancel;
    }

    private static bool OnUnGrabbingMouse (View view)
    {
        if (view is null)
        {
            return false;
        }

        var evArgs = new GrabMouseEventArgs (view);
        UnGrabbingMouse?.Invoke (view, evArgs);

        return evArgs.Cancel;
    }

    private static void OnGrabbedMouse (View view)
    {
        if (view is null)
        {
            return;
        }

        GrabbedMouse?.Invoke (view, new (view));
    }

    private static void OnUnGrabbedMouse (View view)
    {
        if (view is null)
        {
            return;
        }

        UnGrabbedMouse?.Invoke (view, new (view));
    }

#nullable enable

    // Used by OnMouseEvent to track the last view that was clicked on.
    internal static View? _mouseEnteredView;

    /// <summary>Event fired when a mouse move or click occurs. Coordinates are screen relative.</summary>
    /// <remarks>
    ///     <para>
    ///         Use this event to receive mouse events in screen coordinates. Use <see cref="MouseEvent"/> to
    ///         receive mouse events relative to a <see cref="View"/>'s bounds.
    ///     </para>
    ///     <para>The <see cref="MouseEvent.View"/> will contain the <see cref="View"/> that contains the mouse coordinates.</para>
    /// </remarks>
    public static event EventHandler<MouseEventEventArgs> MouseEvent;

    /// <summary>Called when a mouse event occurs. Raises the <see cref="MouseEvent"/> event.</summary>
    /// <remarks>This method can be used to simulate a mouse event, e.g. in unit tests.</remarks>
    /// <param name="a">The mouse event with coordinates relative to the screen.</param>
    internal static void OnMouseEvent (MouseEventEventArgs a)
    {
        if (IsMouseDisabled)
        {
            return;
        }

        // TODO: In PR #3273, FindDeepestView will return adornments. Update logic below to fix adornment mouse handling
        var view = View.FindDeepestView (Current, a.MouseEvent.X, a.MouseEvent.Y);

        if (view is { WantContinuousButtonPressed: true })
        {
            WantContinuousButtonPressedView = view;
        }
        else
        {
            WantContinuousButtonPressedView = null;
        }

        if (view is { })
        {
            a.MouseEvent.View = view;
        }

        MouseEvent?.Invoke (null, new (a.MouseEvent));

        if (a.MouseEvent.Handled)
        {
            return;
        }

        if (MouseGrabView is { })
        {
            // If the mouse is grabbed, send the event to the view that grabbed it.
            // The coordinates are relative to the Bounds of the view that grabbed the mouse.
            Point frameLoc = MouseGrabView.ScreenToFrame (a.MouseEvent.X, a.MouseEvent.Y);

            var viewRelativeMouseEvent = new MouseEvent
            {
                X = frameLoc.X,
                Y = frameLoc.Y,
                Flags = a.MouseEvent.Flags,
                ScreenPosition = new (a.MouseEvent.X, a.MouseEvent.Y),
                View = view
            };

            if (MouseGrabView.Bounds.Contains (viewRelativeMouseEvent.X, viewRelativeMouseEvent.Y) is false)
            {
                // The mouse has moved outside the bounds of the view that
                // grabbed the mouse, so we tell the view that last got 
                // OnMouseEnter the mouse is leaving
                // BUGBUG: That sentence makes no sense. Either I'm missing something or this logic is flawed.
                _mouseEnteredView?.OnMouseLeave (a.MouseEvent);
            }

            //System.Diagnostics.Debug.WriteLine ($"{nme.Flags};{nme.X};{nme.Y};{mouseGrabView}");
            if (MouseGrabView?.OnMouseEvent (viewRelativeMouseEvent) == true)
            {
                return;
            }
        }

        if (view is not Adornment)
        {
            if ((view is null || view == OverlappedTop)
                && Current is { Modal: false }
                && OverlappedTop != null
                && a.MouseEvent.Flags != MouseFlags.ReportMousePosition
                && a.MouseEvent.Flags != 0)
            {
                // This occurs when there are multiple overlapped "tops"
                // E.g. "Mdi" - in the Background Worker Scenario
                View? top = FindDeepestTop (Top, a.MouseEvent.X, a.MouseEvent.Y);
                view = View.FindDeepestView (top, a.MouseEvent.X, a.MouseEvent.Y);

                if (view is { } && view != OverlappedTop && top != Current)
                {
                    MoveCurrent ((Toplevel)top);
                }
            }
        }

        if (view is null)
        {
            return;
        }

        MouseEvent? me = null;

        if (view is Adornment adornment)
        {
            Point frameLoc = adornment.ScreenToFrame (a.MouseEvent.X, a.MouseEvent.Y);

            me = new ()
            {
                X = frameLoc.X,
                Y = frameLoc.Y,
                Flags = a.MouseEvent.Flags,
                ScreenPosition = new (a.MouseEvent.X, a.MouseEvent.Y),
                View = view
            };
        }
        else if (view.BoundsToScreen (view.Bounds).Contains (a.MouseEvent.X, a.MouseEvent.Y))
        {
            Point boundsPoint = view.ScreenToBounds (a.MouseEvent.X, a.MouseEvent.Y);

            me = new ()
            {
                X = boundsPoint.X,
                Y = boundsPoint.Y,
                Flags = a.MouseEvent.Flags,
                ScreenPosition = new (a.MouseEvent.X, a.MouseEvent.Y),
                View = view
            };
        }

        if (me is null)
        {
            return;
        }

        if (_mouseEnteredView is null)
        {
            _mouseEnteredView = view;
            view.OnMouseEnter (me);
        }
        else if (_mouseEnteredView != view)
        {
            _mouseEnteredView.OnMouseLeave (me);
            view.OnMouseEnter (me);
            _mouseEnteredView = view;
        }

        if (!view.WantMousePositionReports && a.MouseEvent.Flags == MouseFlags.ReportMousePosition)
        {
            return;
        }

        WantContinuousButtonPressedView = view.WantContinuousButtonPressed ? view : null;

        //Debug.WriteLine ($"OnMouseEvent: ({a.MouseEvent.X},{a.MouseEvent.Y}) - {a.MouseEvent.Flags}");

        if (view.OnMouseEvent (me))
        {
            // Should we bubble up the event, if it is not handled?
            //return;
        }

        BringOverlappedTopToFront ();
    }
#nullable restore

    #endregion Mouse handling

    #region Keyboard handling

    private static Key _alternateForwardKey = Key.Empty; // Defined in config.json

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    [JsonConverter (typeof (KeyJsonConverter))]
    public static Key AlternateForwardKey
    {
        get => _alternateForwardKey;
        set
        {
            if (_alternateForwardKey != value)
            {
                Key oldKey = _alternateForwardKey;
                _alternateForwardKey = value;
                OnAlternateForwardKeyChanged (new (oldKey, value));
            }
        }
    }

    private static void OnAlternateForwardKeyChanged (KeyChangedEventArgs e)
    {
        foreach (Toplevel top in _topLevels.ToArray ())
        {
            top.OnAlternateForwardKeyChanged (e);
        }
    }

    private static Key _alternateBackwardKey = Key.Empty; // Defined in config.json

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    [JsonConverter (typeof (KeyJsonConverter))]
    public static Key AlternateBackwardKey
    {
        get => _alternateBackwardKey;
        set
        {
            if (_alternateBackwardKey != value)
            {
                Key oldKey = _alternateBackwardKey;
                _alternateBackwardKey = value;
                OnAlternateBackwardKeyChanged (new (oldKey, value));
            }
        }
    }

    private static void OnAlternateBackwardKeyChanged (KeyChangedEventArgs oldKey)
    {
        foreach (Toplevel top in _topLevels.ToArray ())
        {
            top.OnAlternateBackwardKeyChanged (oldKey);
        }
    }

    private static Key _quitKey = Key.Empty; // Defined in config.json

    /// <summary>Gets or sets the key to quit the application.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    [JsonConverter (typeof (KeyJsonConverter))]
    public static Key QuitKey
    {
        get => _quitKey;
        set
        {
            if (_quitKey != value)
            {
                Key oldKey = _quitKey;
                _quitKey = value;
                OnQuitKeyChanged (new (oldKey, value));
            }
        }
    }

    private static void OnQuitKeyChanged (KeyChangedEventArgs e)
    {
        // Duplicate the list so if it changes during enumeration we're safe
        foreach (Toplevel top in _topLevels.ToArray ())
        {
            top.OnQuitKeyChanged (e);
        }
    }

    /// <summary>
    ///     Event fired when the user presses a key. Fired by <see cref="OnKeyDown"/>.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Curses) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/> and before <see cref="KeyUp"/>.</para>
    /// </remarks>
    public static event EventHandler<Key> KeyDown;

    /// <summary>
    ///     Called by the <see cref="ConsoleDriver"/> when the user presses a key. Fires the <see cref="KeyDown"/> event
    ///     then calls <see cref="View.NewKeyDownEvent"/> on all top level views. Called after <see cref="OnKeyDown"/> and
    ///     before <see cref="OnKeyUp"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="keyEvent"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool OnKeyDown (Key keyEvent)
    {
        if (!_initialized)
        {
            return true;
        }

        KeyDown?.Invoke (null, keyEvent);

        if (keyEvent.Handled)
        {
            return true;
        }

        foreach (Toplevel topLevel in _topLevels.ToList ())
        {
            if (topLevel.NewKeyDownEvent (keyEvent))
            {
                return true;
            }

            if (topLevel.Modal)
            {
                break;
            }
        }

        // Invoke any Global KeyBindings
        foreach (Toplevel topLevel in _topLevels.ToList ())
        {
            foreach (View view in topLevel.Subviews.Where (
                                                           v => v.KeyBindings.TryGet (
                                                                                      keyEvent,
                                                                                      KeyBindingScope.Application,
                                                                                      out KeyBinding _
                                                                                     )
                                                          ))
            {
                if (view.KeyBindings.TryGet (keyEvent.KeyCode, KeyBindingScope.Application, out KeyBinding _))
                {
                    bool? handled = view.OnInvokingKeyBindings (keyEvent);

                    if (handled is { } && (bool)handled)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Event fired when the user releases a key. Fired by <see cref="OnKeyUp"/>.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Curses) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/>.</para>
    /// </remarks>
    public static event EventHandler<Key> KeyUp;

    /// <summary>
    ///     Called by the <see cref="ConsoleDriver"/> when the user releases a key. Fires the <see cref="KeyUp"/> event
    ///     then calls <see cref="View.NewKeyUpEvent"/> on all top level views. Called after <see cref="OnKeyDown"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="a"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool OnKeyUp (Key a)
    {
        if (!_initialized)
        {
            return true;
        }

        KeyUp?.Invoke (null, a);

        if (a.Handled)
        {
            return true;
        }

        foreach (Toplevel topLevel in _topLevels.ToList ())
        {
            if (topLevel.NewKeyUpEvent (a))
            {
                return true;
            }

            if (topLevel.Modal)
            {
                break;
            }
        }

        return false;
    }

    #endregion Keyboard handling
}
