using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.VisualBasic;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Views;

namespace Terminal.Gui.App;

public static partial class Application // Lifecycle (Init/Shutdown)
{

    /// <summary>Initializes a new instance of a Terminal.Gui Application. <see cref="Shutdown"/> must be called when the application is closing.</summary>
    /// <para>Call this method once per instance (or after <see cref="Shutdown"/> has been called).</para>
    /// <para>
    ///     This function loads the right <see cref="IDriver"/> for the platform, Creates a <see cref="Toplevel"/>. and
    ///     assigns it to <see cref="Current"/>
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

    /// <summary>
    ///     Gets or sets the main thread ID for the application.
    /// </summary>
    public static int? MainThreadId
    {
        get => ((ApplicationImpl)ApplicationImpl.Instance).MainThreadId;
        set => ((ApplicationImpl)ApplicationImpl.Instance).MainThreadId = value;
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

    /// <inheritdoc cref="IApplication.InitializedChanged"/>
    public static event EventHandler<EventArgs<bool>>? InitializedChanged
    {
        add => ApplicationImpl.Instance.InitializedChanged += value;
        remove => ApplicationImpl.Instance.InitializedChanged -= value;
    }

    // IMPORTANT: Ensure all property/fields are reset here. See Init_ResetState_Resets_Properties unit test.
    // Encapsulate all setting of initial state for Application; Having
    // this in a function like this ensures we don't make mistakes in
    // guaranteeing that the state of this singleton is deterministic when Init
    // starts running and after Shutdown returns.
    internal static void ResetState (bool ignoreDisposed = false) => ApplicationImpl.Instance?.ResetState (ignoreDisposed);
}
