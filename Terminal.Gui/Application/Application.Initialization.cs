#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Terminal.Gui;

public static partial class Application // Initialization (Init/Shutdown)
{
    /// <summary>Initializes a new instance of <see cref="Terminal.Gui"/> Application.</summary>
    /// <para>Call this method once per instance (or after <see cref="Shutdown"/> has been called).</para>
    /// <para>
    ///     This function loads the right <see cref="ConsoleDriver"/> for the platform, Creates a <see cref="Toplevel"/>. and
    ///     assigns it to <see cref="Top"/>
    /// </para>
    /// <para>
    ///     <see cref="Shutdown"/> must be called when the application is closing (typically after
    ///     <see cref="Run{T}"/> has returned) to ensure resources are cleaned up and
    ///     terminal settings
    ///     restored.
    /// </para>
    /// <para>
    ///     The <see cref="Run{T}"/> function combines
    ///     <see cref="Init(Terminal.Gui.ConsoleDriver,string)"/> and <see cref="Run(Toplevel, Func{Exception, bool})"/>
    ///     into a single
    ///     call. An application cam use <see cref="Run{T}"/> without explicitly calling
    ///     <see cref="Init(Terminal.Gui.ConsoleDriver,string)"/>.
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
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Init (ConsoleDriver? driver = null, string? driverName = null) { InternalInit (driver, driverName); }

    internal static bool IsInitialized { get; set; }
    internal static int MainThreadId { get; set; } = -1;

    // INTERNAL function for initializing an app with a Toplevel factory object, driver, and mainloop.
    //
    // Called from:
    //
    // Init() - When the user wants to use the default Toplevel. calledViaRunT will be false, causing all state to be reset.
    // Run<T>() - When the user wants to use a custom Toplevel. calledViaRunT will be true, enabling Run<T>() to be called without calling Init first.
    // Unit Tests - To initialize the app with a custom Toplevel, using the FakeDriver. calledViaRunT will be false, causing all state to be reset.
    //
    // calledViaRunT: If false (default) all state will be reset. If true the state will not be reset.
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal static void InternalInit (
        ConsoleDriver? driver = null,
        string? driverName = null,
        bool calledViaRunT = false
    )
    {
        if (IsInitialized && driver is null)
        {
            return;
        }

        if (IsInitialized)
        {
            throw new InvalidOperationException ("Init has already been called and must be bracketed by Shutdown.");
        }

        if (!calledViaRunT)
        {
            // Reset all class variables (Application is a singleton).
            ResetState (ignoreDisposed: true);
        }

        Navigation = new ();

        // For UnitTests
        if (driver is { })
        {
            Driver = driver;

            if (driver is FakeDriver)
            {
                // We're running unit tests. Disable loading config files other than default
                if (Locations == ConfigLocations.All)
                {
                    Locations = ConfigLocations.DefaultOnly;
                    Reset ();
                }
            }
        }

        // Start the process of configuration management.
        // Note that we end up calling LoadConfigurationFromAllSources
        // multiple times. We need to do this because some settings are only
        // valid after a Driver is loaded. In this case we need just
        // `Settings` so we can determine which driver to use.
        // Don't reset, so we can inherit the theme from the previous run.
        string previousTheme = Themes?.Theme ?? string.Empty;
        Load ();
        if (Themes is { } && !string.IsNullOrEmpty (previousTheme) && previousTheme != "Default")
        {
            ThemeManager.SelectedTheme = previousTheme;
        }
        Apply ();

        AddApplicationKeyBindings ();

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
                List<Type?> drivers = GetDriverTypes ();
                Type? driverType = drivers.FirstOrDefault (t => t!.Name.Equals (ForceDriver, StringComparison.InvariantCultureIgnoreCase));

                if (driverType is { })
                {
                    Driver = (ConsoleDriver)Activator.CreateInstance (driverType)!;
                }
                else
                {
                    throw new ArgumentException (
                                                 $"Invalid driver name: {ForceDriver}. Valid names are {string.Join (", ", drivers.Select (t => t!.Name))}"
                                                );
                }
            }
        }

        try
        {
            MainLoop = Driver!.Init ();
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

        Driver.SizeChanged += Driver_SizeChanged;
        Driver.KeyDown += Driver_KeyDown;
        Driver.KeyUp += Driver_KeyUp;
        Driver.MouseEvent += Driver_MouseEvent;

        SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext ());

        SupportedCultures = GetSupportedCultures ();
        MainThreadId = Thread.CurrentThread.ManagedThreadId;
        bool init = IsInitialized = true;
        InitializedChanged?.Invoke (null, new (init));
    }

    private static void Driver_SizeChanged (object? sender, SizeChangedEventArgs e) { OnSizeChanging (e); }
    private static void Driver_KeyDown (object? sender, Key e) { RaiseKeyDownEvent (e); }
    private static void Driver_KeyUp (object? sender, Key e) { RaiseKeyUpEvent (e); }
    private static void Driver_MouseEvent (object? sender, MouseEventArgs e) { RaiseMouseEvent (e); }

    /// <summary>Gets of list of <see cref="ConsoleDriver"/> types that are available.</summary>
    /// <returns></returns>
    [RequiresUnreferencedCode ("AOT")]
    public static List<Type?> GetDriverTypes ()
    {
        // use reflection to get the list of drivers
        List<Type?> driverTypes = new ();

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies ())
        {
            foreach (Type? type in asm.GetTypes ())
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
    ///     <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> to ensure all resources are cleaned
    ///     up (Disposed)
    ///     and terminal settings are restored.
    /// </remarks>
    public static void Shutdown ()
    {
        // TODO: Throw an exception if Init hasn't been called.

        bool wasInitialized = IsInitialized;
        ResetState ();
        PrintJsonErrors ();

        if (wasInitialized)
        {
            bool init = IsInitialized;
            InitializedChanged?.Invoke (null, new (in init));
        }
    }

    /// <summary>
    ///     This event is raised after the <see cref="Init"/> and <see cref="Shutdown"/> methods have been called.
    /// </summary>
    /// <remarks>
    ///     Intended to support unit tests that need to know when the application has been initialized.
    /// </remarks>
    public static event EventHandler<EventArgs<bool>>? InitializedChanged;
}
