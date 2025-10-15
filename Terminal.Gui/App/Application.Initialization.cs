#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Terminal.Gui.App;

public static partial class Application // Initialization (Init/Shutdown)
{

    /// <summary>Initializes a new instance of a Terminal.Gui Application. <see cref="Shutdown"/> must be called when the application is closing.</summary>
    /// <para>Call this method once per instance (or after <see cref="Shutdown"/> has been called).</para>
    /// <para>
    ///     This function loads the right <see cref="IConsoleDriver"/> for the platform, Creates a <see cref="Toplevel"/>. and
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
    ///     <see cref="Init(IConsoleDriver,string)"/> and <see cref="Run(Toplevel, Func{Exception, bool})"/>
    ///     into a single
    ///     call. An application cam use <see cref="Run{T}"/> without explicitly calling
    ///     <see cref="Init(IConsoleDriver,string)"/>.
    /// </para>
    /// <param name="driver">
    ///     The <see cref="IConsoleDriver"/> to use. If neither <paramref name="driver"/> or
    ///     <paramref name="driverName"/> are specified the default driver for the platform will be used.
    /// </param>
    /// <param name="driverName">
    ///     The short name (e.g. "dotnet", "windows", "unix", or "fake") of the
    ///     <see cref="IConsoleDriver"/> to use. If neither <paramref name="driver"/> or <paramref name="driverName"/> are
    ///     specified the default driver for the platform will be used.
    /// </param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Init (IConsoleDriver? driver = null, string? driverName = null)
    {
        // Check if this is a request for a legacy driver (like FakeDriver)
        // that isn't supported by the modern application architecture
        if (driver is null)
        {
            var driverNameToCheck = string.IsNullOrWhiteSpace (driverName) ? ForceDriver : driverName;
            if (!string.IsNullOrEmpty (driverNameToCheck))
            {
                (List<Type?> drivers, List<string?> driverTypeNames) = GetDriverTypes ();
                Type? driverType = drivers.FirstOrDefault (t => t!.Name.Equals (driverNameToCheck, StringComparison.InvariantCultureIgnoreCase));
                
                // If it's a legacy IConsoleDriver (not a Facade), use InternalInit which supports legacy drivers
                if (driverType is { } && !typeof (IConsoleDriverFacade).IsAssignableFrom (driverType))
                {
                    InternalInit (driver, driverName);
                    return;
                }
            }
        }
        
        // Otherwise delegate to the ApplicationImpl instance (which uses the modern architecture)
        ApplicationImpl.Instance.Init (driver, driverName ?? ForceDriver);
    }

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
        IConsoleDriver? driver = null,
        string? driverName = null,
        bool calledViaRunT = false
    )
    {
        if (Initialized && driver is null)
        {
            return;
        }

        if (Initialized)
        {
            throw new InvalidOperationException ("Init has already been called and must be bracketed by Shutdown.");
        }

        if (!calledViaRunT)
        {
            // Reset all class variables (Application is a singleton).
            ResetState (ignoreDisposed: true);
        }

        // For UnitTests
        if (driver is { })
        {
            Driver = driver;
        }

        // Ignore Configuration for ForceDriver if driverName is specified
        if (!string.IsNullOrEmpty (driverName))
        {
            ForceDriver = driverName;
        }

        // Check if we need to use a legacy driver (like FakeDriver)
        // or go through the modern application architecture
        if (Driver is null)
        {
            //// Try to find a legacy IConsoleDriver type that matches the driver name
            //bool useLegacyDriver = false;
            //if (!string.IsNullOrEmpty (ForceDriver))
            //{
            //    (List<Type?> drivers, List<string?> driverTypeNames) = GetDriverTypes ();
            //    Type? driverType = drivers.FirstOrDefault (t => t!.Name.Equals (ForceDriver, StringComparison.InvariantCultureIgnoreCase));
                
            //    if (driverType is { } && !typeof (IConsoleDriverFacade).IsAssignableFrom (driverType))
            //    {
            //        // This is a legacy driver (not a ConsoleDriverFacade)
            //        Driver = (IConsoleDriver)Activator.CreateInstance (driverType)!;
            //        useLegacyDriver = true;
            //    }
            //}
            
            //// Use the modern application architecture
            //if (!useLegacyDriver)
            {
                ApplicationImpl.Instance.Init (driver, driverName);
                Debug.Assert (Driver is { });
                return;
            }
        }

        Debug.Assert (Navigation is null);
        Navigation = new ();

        Debug.Assert (Popover is null);
        Popover = new ();

        AddKeyBindings ();

        try
        {
            MainLoop = Driver!.Init ();
            SubscribeDriverEvents ();
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

        SynchronizationContext.SetSynchronizationContext (new MainLoopSyncContext ());

        // TODO: This is probably not needed
        if (Popover.GetActivePopover () is View popover)
        {
            popover.Visible = false;
        }

        MainThreadId = Thread.CurrentThread.ManagedThreadId;
        bool init = Initialized = true;
        InitializedChanged?.Invoke (null, new (init));
    }

    internal static void SubscribeDriverEvents ()
    {
        ArgumentNullException.ThrowIfNull (Driver);

        Driver.SizeChanged += Driver_SizeChanged;
        Driver.KeyDown += Driver_KeyDown;
        Driver.KeyUp += Driver_KeyUp;
        Driver.MouseEvent += Driver_MouseEvent;
    }

    internal static void UnsubscribeDriverEvents ()
    {
        ArgumentNullException.ThrowIfNull (Driver);

        Driver.SizeChanged -= Driver_SizeChanged;
        Driver.KeyDown -= Driver_KeyDown;
        Driver.KeyUp -= Driver_KeyUp;
        Driver.MouseEvent -= Driver_MouseEvent;
    }

    private static void Driver_SizeChanged (object? sender, SizeChangedEventArgs e) { OnSizeChanging (e); }
    private static void Driver_KeyDown (object? sender, Key e) { RaiseKeyDownEvent (e); }
    private static void Driver_KeyUp (object? sender, Key e) { RaiseKeyUpEvent (e); }
    private static void Driver_MouseEvent (object? sender, MouseEventArgs e) { RaiseMouseEvent (e); }

    /// <summary>Gets of list of <see cref="IConsoleDriver"/> types and type names that are available.</summary>
    /// <returns></returns>
    [RequiresUnreferencedCode ("AOT")]
    public static (List<Type?>, List<string?>) GetDriverTypes ()
    {
        // use reflection to get the list of drivers
        List<Type?> driverTypes = new ();

        // Only inspect the IConsoleDriver assembly
        var asm = typeof (IConsoleDriver).Assembly;

        foreach (Type? type in asm.GetTypes ())
        {
            if (typeof (IConsoleDriver).IsAssignableFrom (type) &&
                type is { IsAbstract: false, IsClass: true })
            {
                driverTypes.Add (type);
            }
        }

        List<string?> driverTypeNames = driverTypes
                                        .Where (d => !typeof (IConsoleDriverFacade).IsAssignableFrom (d))
                                        .Select (d => d!.Name)
                                        .Union (["dotnet", "windows", "unix", "fake"])
                                        .ToList ()!;



        return (driverTypes, driverTypeNames);
    }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    /// <remarks>
    ///     Shutdown must be called for every call to <see cref="Init"/> or
    ///     <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> to ensure all resources are cleaned
    ///     up (Disposed)
    ///     and terminal settings are restored.
    /// </remarks>
    public static void Shutdown () => ApplicationImpl.Instance.Shutdown ();

    /// <summary>
    ///     Gets whether the application has been initialized with <see cref="Init"/> and not yet shutdown with <see cref="Shutdown"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     The <see cref="InitializedChanged"/> event is raised after the <see cref="Init"/> and <see cref="Shutdown"/> methods have been called.
    /// </para>
    /// </remarks>
    public static bool Initialized { get; internal set; }

    /// <summary>
    ///     This event is raised after the <see cref="Init"/> and <see cref="Shutdown"/> methods have been called.
    /// </summary>
    /// <remarks>
    ///     Intended to support unit tests that need to know when the application has been initialized.
    /// </remarks>
    public static event EventHandler<EventArgs<bool>>? InitializedChanged;

    /// <summary>
    ///  Raises the <see cref="InitializedChanged"/> event.
    /// </summary>
    internal static void OnInitializedChanged (object sender, EventArgs<bool> e)
    {
        Application.InitializedChanged?.Invoke (sender, e);
    }
}
