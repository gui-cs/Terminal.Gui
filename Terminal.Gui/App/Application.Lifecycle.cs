#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Terminal.Gui.App;

public static partial class Application // Lifecycle (Init/Shutdown)
{

    /// <summary>Initializes a new instance of a Terminal.Gui Application. <see cref="Shutdown"/> must be called when the application is closing.</summary>
    /// <para>Call this method once per instance (or after <see cref="Shutdown"/> has been called).</para>
    /// <para>
    ///     This function loads the right <see cref="IDriver"/> for the platform, Creates a <see cref="Toplevel"/>. and
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
    ///     <see cref="Init(IDriver,string)"/> and <see cref="Run(Toplevel, Func{Exception, bool})"/>
    ///     into a single
    ///     call. An application can use <see cref="Run{T}"/> without explicitly calling
    ///     <see cref="Init(IDriver,string)"/>.
    /// </para>
    /// <param name="driver">
    ///     The <see cref="IDriver"/> to use. If neither <paramref name="driver"/> or
    ///     <paramref name="driverName"/> are specified the default driver for the platform will be used.
    /// </param>
    /// <param name="driverName">
    ///     The short name (e.g. "dotnet", "windows", "unix", or "fake") of the
    ///     <see cref="IDriver"/> to use. If neither <paramref name="driver"/> or <paramref name="driverName"/> are
    ///     specified the default driver for the platform will be used.
    /// </param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Init (IDriver? driver = null, string? driverName = null)
    {
        ApplicationImpl.Instance.Init (driver, driverName ?? ForceDriver);
    }

    internal static int MainThreadId
    {
        get => ((ApplicationImpl)ApplicationImpl.Instance).MainThreadId;
        set => ((ApplicationImpl)ApplicationImpl.Instance).MainThreadId = value;
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

    private static void Driver_SizeChanged (object? sender, SizeChangedEventArgs e)
    {
        RaiseScreenChangedEvent (new Rectangle (new (0, 0), e.Size!.Value));
    }
    private static void Driver_KeyDown (object? sender, Key e) { RaiseKeyDownEvent (e); }
    private static void Driver_KeyUp (object? sender, Key e) { RaiseKeyUpEvent (e); }
    private static void Driver_MouseEvent (object? sender, MouseEventArgs e) { RaiseMouseEvent (e); }

    /// <summary>Gets a list of <see cref="IDriver"/> types and type names that are available.</summary>
    /// <returns></returns>
    [RequiresUnreferencedCode ("AOT")]
    public static (List<Type?>, List<string?>) GetDriverTypes ()
    {
        // use reflection to get the list of drivers
        List<Type?> driverTypes = new ();

        // Only inspect the IConsoleDriver assembly
        var asm = typeof (IDriver).Assembly;

        foreach (Type? type in asm.GetTypes ())
        {
            if (typeof (IDriver).IsAssignableFrom (type) &&
                type is { IsAbstract: false, IsClass: true })
            {
                driverTypes.Add (type);
            }
        }

        List<string?> driverTypeNames = driverTypes
                                        .Where (d => !typeof (IDriver).IsAssignableFrom (d))
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
    public static bool Initialized
    {
        get => ApplicationImpl.Instance.Initialized;
        internal set => ApplicationImpl.Instance.Initialized = value;
    }

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
