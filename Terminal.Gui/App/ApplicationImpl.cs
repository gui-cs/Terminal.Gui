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
    ///     Configures the singleton instance of <see cref="Application"/> to use the specified backend implementation.
    /// </summary>
    /// <param name="app"></param>
    public static void SetInstance (IApplication? app)
    {
        _instance = app;
    }

    // Private static readonly Lazy instance of Application
    private static IApplication? _instance;

    /// <summary>
    ///     Gets the currently configured backend implementation of <see cref="Application"/> gateway methods.
    /// </summary>
    public static IApplication Instance => _instance ??= new ApplicationImpl ();

    #endregion Singleton

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
            if (_mouse is null)
            {
                _mouse = new MouseImpl { App = this };
            }

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
            if (_keyboard is null)
            {
                _keyboard = new KeyboardImpl { App = this };
            }

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
            if (_popover is null)
            {
                _popover = new () { App = this };
            }

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
            if (_navigation is null)
            {
                _navigation = new () { App = this };
            }

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

    #endregion View Management

    /// <inheritdoc/>
    public new string ToString () => Driver?.ToString () ?? string.Empty;
}
