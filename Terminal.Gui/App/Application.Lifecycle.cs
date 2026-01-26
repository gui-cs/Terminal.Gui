using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public static partial class Application // Lifecycle (Init/Shutdown)
{
    #region Modern Instance-Based Model Events (Thread-Local)

    // Thread-local backing fields for events - each thread has its own subscribers
    private static readonly ThreadLocal<EventHandler<EventArgs<IApplication>>?> _instanceCreated = new ();
    private static readonly ThreadLocal<EventHandler<EventArgs<IApplication>>?> _instanceInitialized = new ();
    private static readonly ThreadLocal<EventHandler<EventArgs<IApplication>>?> _instanceDisposed = new ();

    /// <summary>
    ///     Raised when an <see cref="IApplication"/> instance is created via <see cref="Create"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is for the modern instance-based model only. It fires immediately after
    ///         <see cref="Create"/> creates a new instance, before <see cref="IApplication.Init"/> is called.
    ///     </para>
    ///     <para>
    ///         This event is thread-local, meaning each thread has its own set of subscribers.
    ///         This enables parallel test execution where each test thread can independently
    ///         monitor application instances created on that thread.
    ///     </para>
    /// </remarks>
    public static event EventHandler<EventArgs<IApplication>>? InstanceCreated
    {
        add => _instanceCreated.Value += value;
        remove => _instanceCreated.Value -= value;
    }

    /// <summary>
    ///     Raised when an <see cref="IApplication"/> instance completes initialization.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is for the modern instance-based model only. It fires after
    ///         <see cref="IApplication.Init"/> completes successfully.
    ///     </para>
    ///     <para>
    ///         This event is thread-local, meaning each thread has its own set of subscribers.
    ///         This enables parallel test execution where each test thread can independently
    ///         monitor application instances initialized on that thread.
    ///     </para>
    /// </remarks>
    public static event EventHandler<EventArgs<IApplication>>? InstanceInitialized
    {
        add => _instanceInitialized.Value += value;
        remove => _instanceInitialized.Value -= value;
    }

    /// <summary>
    ///     Raised when an <see cref="IApplication"/> instance is disposed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event is for the modern instance-based model only. It fires when
    ///         <see cref="IDisposable.Dispose"/> is called on an instance.
    ///     </para>
    ///     <para>
    ///         This event is thread-local, meaning each thread has its own set of subscribers.
    ///         This enables parallel test execution where each test thread can independently
    ///         monitor application instances disposed on that thread.
    ///     </para>
    /// </remarks>
    public static event EventHandler<EventArgs<IApplication>>? InstanceDisposed
    {
        add => _instanceDisposed.Value += value;
        remove => _instanceDisposed.Value -= value;
    }

    /// <summary>
    ///     Raises the <see cref="InstanceCreated"/> event on the current thread.
    /// </summary>
    internal static void RaiseInstanceCreated (IApplication app) { _instanceCreated.Value?.Invoke (null, new (app)); }

    /// <summary>
    ///     Raises the <see cref="InstanceInitialized"/> event on the current thread.
    /// </summary>
    internal static void RaiseInstanceInitialized (IApplication app) { _instanceInitialized.Value?.Invoke (null, new (app)); }

    /// <summary>
    ///     Raises the <see cref="InstanceDisposed"/> event on the current thread.
    /// </summary>
    internal static void RaiseInstanceDisposed (IApplication app) { _instanceDisposed.Value?.Invoke (null, new (app)); }

    #endregion Modern Instance-Based Model Events (Thread-Local)

    /// <summary>
    ///     Gets the singleton <see cref="IApplication"/> instance used by the legacy static Application model.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For new code, prefer using <see cref="Create"/> to get an instance-based application.
    ///         This property is provided for backward compatibility and internal use.
    ///     </para>
    ///     <para>
    ///         This property returns the same singleton instance used by the legacy static <see cref="Application"/>
    ///         methods like <see cref="Init"/> and <see cref="Run(IRunnable, Func{Exception, bool}?)"/>.
    ///     </para>
    /// </remarks>
    [Obsolete ("The legacy static Application object is going away. Use Application.Create() for new code.")]
    public static IApplication Instance => ApplicationImpl.Instance;

    /// <summary>
    ///     Creates a new <see cref="IApplication"/> instance.
    /// </summary>
    /// <param name="timeProvider">
    ///     Optional time provider for controlling time in tests. If <see langword="null"/>, defaults to
    ///     <see cref="SystemTimeProvider"/>.
    ///     For production use, omit this parameter or pass <see langword="null"/>. For testing, pass a
    ///     <see cref="VirtualTimeProvider"/>.
    /// </param>
    /// <remarks>
    ///     The recommended pattern is for developers to call <c>Application.Create()</c> and then use the returned
    ///     <see cref="IApplication"/> instance for all subsequent application operations.
    /// </remarks>
    /// <returns>A new <see cref="IApplication"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if the legacy static Application model has already been used in this process.
    /// </exception>
    public static IApplication Create (ITimeProvider? timeProvider = null)
    {
        //Debug.Fail ("Application.Create() called");
        ApplicationImpl.MarkInstanceBasedModelUsed ();

        IApplication app = new ApplicationImpl (timeProvider ?? new SystemTimeProvider ());
        RaiseInstanceCreated (app);

        return app;
    }

    /// <inheritdoc cref="IApplication.Init"/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Init (string? driverName = null)
    {
        //Debug.Fail ("Application.Init() called - parallelizable tests should not use legacy static Application model");
        ApplicationImpl.Instance.Init (driverName ?? ForceDriver);
    }

    /// <summary>
    ///     Gets or sets the main thread ID for the application.
    /// </summary>
    [Obsolete ("The legacy static Application object is going away.")]
    public static int? MainThreadId
    {
        get => ApplicationImpl.Instance.MainThreadId;
        internal set => ApplicationImpl.Instance.MainThreadId = value;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static void Shutdown () { ApplicationImpl.Instance.Dispose (); }

    /// <inheritdoc cref="IApplication.Initialized"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool Initialized
    {
        get => ApplicationImpl.Instance.Initialized;
        internal set => ApplicationImpl.Instance.Initialized = value;
    }

    /// <inheritdoc cref="IApplication.InitializedChanged"/>
    [Obsolete ("The legacy static Application object is going away.")]
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
    [Obsolete ("The legacy static Application object is going away.")]
    internal static void ResetState (bool ignoreDisposed = false)
    {
        // Use the static reset method to bypass the fence check
        ApplicationImpl.ResetStateStatic (ignoreDisposed);
    }
}
