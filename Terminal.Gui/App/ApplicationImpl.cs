using System.Collections.Concurrent;

namespace Terminal.Gui.App;

/// <summary>
///     Implementation of core <see cref="Application"/> methods using the modern
///     main loop architecture with component factories for different platforms.
/// </summary>
public partial class ApplicationImpl : IApplication
{
    /// <summary>
    ///     Creates a new instance of the Application backend.
    /// </summary>
    public ApplicationImpl () { }

    /// <summary>
    ///     INTERNAL: Creates a new instance of the Application backend.
    /// </summary>
    /// <param name="componentFactory"></param>
    internal ApplicationImpl (IComponentFactory componentFactory) { _componentFactory = componentFactory; }

    #region Singleton

    // Private static readonly Lazy instance of Application
    private static Lazy<IApplication> _lazyInstance = new (() => new ApplicationImpl ());

    /// <summary>
    ///     Change the singleton implementation, should not be called except before application
    ///     startup. This method lets you provide alternative implementations of core static gateway
    ///     methods of <see cref="Application"/>.
    /// </summary>
    /// <param name="newApplication"></param>
    public static void ChangeInstance (IApplication? newApplication) { _lazyInstance = new (newApplication!); }

    /// <summary>
    ///     Gets the currently configured backend implementation of <see cref="Application"/> gateway methods.
    ///     Change to your own implementation by using <see cref="ChangeInstance"/> (before init).
    /// </summary>
    public static IApplication Instance => _lazyInstance.Value;

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
                _mouse = new MouseImpl { Application = this };
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
                _keyboard = new KeyboardImpl { Application = this };
            }

            return _keyboard;
        }
        set => _keyboard = value ?? throw new ArgumentNullException (nameof (value));
    }

    #endregion Input

    #region View Management

    /// <inheritdoc/>
    public ApplicationPopover? Popover { get; set; }

    /// <inheritdoc/>
    public ApplicationNavigation? Navigation { get; set; }

    /// <inheritdoc/>
    public Toplevel? Current { get; set; }

    // BUGBUG: Technically, this is not the full lst of sessions. There be dragons here, e.g. see how Toplevel.Id is used. What

    /// <inheritdoc/>
    public ConcurrentStack<Toplevel> SessionStack { get; } = new ();

    /// <inheritdoc/>
    public Toplevel? CachedSessionTokenToplevel { get; set; }

    #endregion View Management
}
