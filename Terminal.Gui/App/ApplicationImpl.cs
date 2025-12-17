namespace Terminal.Gui.App;

/// <summary>
///     Implementation of core <see cref="Application"/> methods using the modern
///     main loop architecture with component factories for different platforms.
/// </summary>
internal partial class ApplicationImpl : IApplication
{
    private readonly ITimeProvider _timeProvider;
    private readonly bool _testMode;
    private IInputInjector? _inputInjector;

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend and subscribes to Application configuration property
    ///     events.
    /// </summary>
    /// <param name="timeProvider">Time provider for timestamps and timing control.</param>
    /// <param name="testMode">If <see langword="true"/>, configures application for testing with TestInputSource.</param>
    internal ApplicationImpl (ITimeProvider timeProvider, bool testMode)
    {
        _timeProvider = timeProvider;
        _testMode = testMode;

        // Subscribe to Application static property change events
        Application.ForceDriverChanged += OnForceDriverChanged;
    }

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend for legacy static model.
    ///     Uses SystemTimeProvider and production mode by default.
    /// </summary>
    internal ApplicationImpl () : this (new SystemTimeProvider (), false) { }

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend.
    /// </summary>
    /// <param name="componentFactory"></param>
    internal ApplicationImpl (IComponentFactory componentFactory) : this () { _componentFactory = componentFactory; }

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend for testing.
    /// </summary>
    /// <param name="componentFactory">The component factory.</param>
    /// <param name="timeProvider">Time provider for timestamps and timing control.</param>
    /// <param name="testMode">If <see langword="true"/>, configures application for testing with TestInputSource.</param>
    internal ApplicationImpl (IComponentFactory componentFactory, ITimeProvider timeProvider, bool testMode) : this (timeProvider, testMode) 
    { 
        _componentFactory = componentFactory; 
    }

    /// <inheritdoc/>
    public ITimeProvider GetTimeProvider () { return _timeProvider; }

    /// <inheritdoc/>
    public IInputInjector GetInputInjector ()
    {
        if (_inputInjector is null)
        {
            if (Driver is null)
            {
                throw new InvalidOperationException ("Driver not initialized. Call Init() first.");
            }

            IInputProcessor processor = Driver.GetInputProcessor ();
            _inputInjector = new InputInjector (processor, _timeProvider);
        }

        return _inputInjector;
    }

    private string? _driverName;

    /// <inheritdoc/>
    public new string ToString () { return Driver?.ToString () ?? string.Empty; }

    #region Singleton - Legacy Static Support

    /// <summary>
    ///     Lock object for synchronizing access to ModelUsage and _instance.
    /// </summary>
    private static readonly object _modelUsageLock = new ();

    /// <summary>
    ///     Tracks which application model has been used in this process.
    /// </summary>
    public static ApplicationModelUsage ModelUsage { get; private set; } = ApplicationModelUsage.None;

    /// <summary>
    ///     Error message for when trying to use modern model after legacy static model.
    /// </summary>
    internal const string ERROR_MODERN_AFTER_LEGACY =
        "Cannot use modern instance-based model (Application.Create) after using legacy static Application model (Application.Init/ApplicationImpl.Instance). "
        + "Use only one model per process.";

    /// <summary>
    ///     Error message for when trying to use legacy static model after modern model.
    /// </summary>
    internal const string ERROR_LEGACY_AFTER_MODERN =
        "Cannot use legacy static Application model (Application.Init/ApplicationImpl.Instance) after using modern instance-based model (Application.Create). "
        + "Use only one model per process.";

    /// <summary>
    ///     Configures the singleton instance of <see cref="Application"/> to use the specified backend implementation.
    /// </summary>
    /// <param name="app"></param>
    public static void SetInstance (IApplication? app)
    {
        lock (_modelUsageLock)
        {
            ModelUsage = ApplicationModelUsage.LegacyStatic;
            _instance = app;
        }
    }

    // Private static readonly Lazy instance of Application
    private static IApplication? _instance;

    /// <summary>
    ///     Gets the currently configured backend implementation of <see cref="Application"/> gateway methods.
    /// </summary>
    internal static IApplication Instance
    {
        get
        {
            //Debug.Fail ("ApplicationImpl.Instance accessed - parallelizable tests should not use legacy static Application model");

            // Thread-safe: Use lock to make check-and-create atomic
            lock (_modelUsageLock)
            {
                // If an instance already exists, return it without fence checking
                // This allows for cleanup/reset operations
                if (_instance is { })
                {
                    return _instance;
                }

                // Check if the instance-based model has already been used
                if (ModelUsage == ApplicationModelUsage.InstanceBased)
                {
                    throw new InvalidOperationException (ERROR_LEGACY_AFTER_MODERN);
                }

                // Mark the usage and create the instance
                ModelUsage = ApplicationModelUsage.LegacyStatic;

                return _instance = new ApplicationImpl ();
            }
        }
    }

    /// <summary>
    ///     INTERNAL: Marks that the instance-based model has been used. Called by Application.Create().
    /// </summary>
    internal static void MarkInstanceBasedModelUsed ()
    {
        lock (_modelUsageLock)
        {
            // Check if the legacy static model has already been initialized
            if (ModelUsage == ApplicationModelUsage.LegacyStatic && _instance?.Initialized == true)
            {
                throw new InvalidOperationException (ERROR_MODERN_AFTER_LEGACY);
            }

            ModelUsage = ApplicationModelUsage.InstanceBased;
        }
    }

    /// <summary>
    ///     INTERNAL: Resets the model usage tracking. Only for testing purposes.
    /// </summary>
    internal static void ResetModelUsageTracking ()
    {
        lock (_modelUsageLock)
        {
            ModelUsage = ApplicationModelUsage.None;
            _instance = null;
        }
    }

    /// <summary>
    ///     INTERNAL: Resets state without going through the fence-checked Instance property.
    ///     Used by Application.ResetState() to allow cleanup regardless of which model was used.
    /// </summary>
    internal static void ResetStateStatic (bool ignoreDisposed = false)
    {
        // If an instance exists, reset it
        _instance?.ResetState (ignoreDisposed);

        // Always reset the model tracking to allow tests to use either model after reset
        ResetModelUsageTracking ();
    }

    #endregion Singleton - Legacy Static Support

    #region Screen and Driver

    /// <inheritdoc/>
    public IClipboard? Clipboard => Driver?.Clipboard;

    #endregion Screen and Driver

    #region Keyboard

    private IKeyboard? _keyboard;

    /// <inheritdoc/>
    public IKeyboard Keyboard
    {
        get
        {
            _keyboard ??= new KeyboardImpl { App = this };

            return _keyboard;
        }
        set => _keyboard = value ?? throw new ArgumentNullException (nameof (value));
    }

    #endregion Keyboard

    #region Mouse

    private IMouse? _mouse;

    /// <inheritdoc/>
    public IMouse Mouse
    {
        get
        {
            _mouse ??= new MouseImpl { App = this };

            return _mouse;
        }
        set => _mouse = value ?? throw new ArgumentNullException (nameof (value));
    }

    #endregion Mouse

    #region Navigation and Popover

    private ApplicationNavigation? _navigation;

    /// <inheritdoc/>
    public ApplicationNavigation? Navigation
    {
        get
        {
            _navigation ??= new () { App = this };

            return _navigation;
        }
        set => _navigation = value ?? throw new ArgumentNullException (nameof (value));
    }

    private ApplicationPopover? _popover;

    /// <inheritdoc/>
    public ApplicationPopover? Popover
    {
        get
        {
            _popover ??= new () { App = this };

            return _popover;
        }
        set => _popover = value;
    }

    #endregion Navigation and Popover
}
