using System.Collections.Concurrent;

namespace Terminal.Gui.App;

/// <summary>
///     Implementation of core <see cref="Application"/> methods using the modern
///     main loop architecture with component factories for different platforms.
/// </summary>
public partial class ApplicationImpl : IApplication
{
    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend.
    /// </summary>
    internal ApplicationImpl () { }

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend.
    /// </summary>
    /// <param name="componentFactory"></param>
    internal ApplicationImpl (IComponentFactory componentFactory) { _componentFactory = componentFactory; }

    #region Singleton

    /// <summary>
    ///     Tracks which application model has been used in this process.
    /// </summary>
    private static ApplicationModelUsage _modelUsage = ApplicationModelUsage.None;

    /// <summary>
    ///     Configures the singleton instance of <see cref="Application"/> to use the specified backend implementation.
    /// </summary>
    /// <param name="app"></param>
    public static void SetInstance (IApplication? app) { _instance = app; }

    // Private static readonly Lazy instance of Application
    private static IApplication? _instance;

    /// <summary>
    ///     Gets the currently configured backend implementation of <see cref="Application"/> gateway methods.
    /// </summary>
    public static IApplication Instance
    {
        get
        {
            // If an instance already exists, return it without fence checking
            // This allows for cleanup/reset operations
            if (_instance is { })
            {
                return _instance;
            }

            // Only check the fence when creating a new instance
            if (_modelUsage == ApplicationModelUsage.InstanceBased)
            {
                throw new InvalidOperationException (
                    "Cannot use legacy static Application model (Application.Init/ApplicationImpl.Instance) after using modern instance-based model (Application.Create). " +
                    "Use only one model per process.");
            }

            _modelUsage = ApplicationModelUsage.LegacyStatic;

            return _instance = new ApplicationImpl ();
        }
    }

    /// <summary>
    ///     INTERNAL: Marks that the instance-based model has been used. Called by Application.Create().
    /// </summary>
    internal static void MarkInstanceBasedModelUsed ()
    {
        if (_modelUsage == ApplicationModelUsage.LegacyStatic)
        {
            throw new InvalidOperationException (
                "Cannot use modern instance-based model (Application.Create) after using legacy static Application model (Application.Init/ApplicationImpl.Instance). " +
                "Use only one model per process.");
        }

        _modelUsage = ApplicationModelUsage.InstanceBased;
    }

    /// <summary>
    ///     INTERNAL: Resets the model usage tracking. Only for testing purposes.
    /// </summary>
    internal static void ResetModelUsageTracking ()
    {
        _modelUsage = ApplicationModelUsage.None;
        _instance = null;
    }

    #endregion Singleton

    /// <summary>
    ///     Defines the different application usage models.
    /// </summary>
    private enum ApplicationModelUsage
    {
        /// <summary>No model has been used yet.</summary>
        None,

        /// <summary>Legacy static model (Application.Init/ApplicationImpl.Instance).</summary>
        LegacyStatic,

        /// <summary>Modern instance-based model (Application.Create).</summary>
        InstanceBased
    }

    private string? _driverName;

    #region Input

    private IMouse? _mouse;

    /// <summary>
    ///     Handles mouse event state and processing.
    /// </summary>
    public IMouse Mouse
    {
        get
        {
            _mouse ??= new MouseImpl { App = this };

            return _mouse;
        }
        set => _mouse = value ?? throw new ArgumentNullException (nameof (value));
    }

    private IKeyboard? _keyboard;

    /// <summary>
    ///     Handles keyboard input and key bindings at the Application level
    /// </summary>
    public IKeyboard Keyboard
    {
        get
        {
            _keyboard ??= new KeyboardImpl { App = this };

            return _keyboard;
        }
        set => _keyboard = value ?? throw new ArgumentNullException (nameof (value));
    }

    #endregion Input

    #region View Management

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

    private Toplevel? _topRunnable;

    /// <inheritdoc/>
    public Toplevel? TopRunnable
    {
        get => _topRunnable;
        set
        {
            _topRunnable = value;

            if (_topRunnable is { })
            {
                _topRunnable.App = this;
            }
        }
    }

    // BUGBUG: Technically, this is not the full lst of sessions. There be dragons here, e.g. see how Toplevel.Id is used. What

    /// <inheritdoc/>
    public ConcurrentStack<Toplevel> SessionStack { get; } = new ();

    /// <inheritdoc/>
    public Toplevel? CachedSessionTokenToplevel { get; set; }

    /// <inheritdoc/>
    public ConcurrentStack<RunnableSessionToken>? RunnableSessionStack { get; } = new ();

    /// <inheritdoc/>
    public IRunnable? FrameworkOwnedRunnable { get; set; }

    #endregion View Management

    /// <inheritdoc/>
    public new string ToString () => Driver?.ToString () ?? string.Empty;
}
