#nullable enable
namespace Terminal.Gui;

public static partial class Application // Navigation stuff
{
    /// <summary>
    ///     Gets the <see cref="ApplicationNavigation"/> instance for the current <see cref="Application"/>.
    /// </summary>
    public static ApplicationNavigation? Navigation { get; internal set; }

    private static Key _nextTabGroupKey = Key.F6; // Resources/config.json overrides
    private static Key _nextTabKey = Key.Tab; // Resources/config.json overrides
    private static Key _prevTabGroupKey = Key.F6.WithShift; // Resources/config.json overrides
    private static Key _prevTabKey = Key.Tab.WithShift; // Resources/config.json overrides

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key NextTabGroupKey
    {
        get => _nextTabGroupKey;
        set
        {
            if (_nextTabGroupKey != value)
            {
                ReplaceKey (_nextTabGroupKey, value);
                _nextTabGroupKey = value;
            }
        }
    }

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key NextTabKey
    {
        get => _nextTabKey;
        set
        {
            if (_nextTabKey != value)
            {
                ReplaceKey (_nextTabKey, value);
                _nextTabKey = value;
            }
        }
    }


    /// <summary>
    ///     Raised when the user releases a key.
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
    public static event EventHandler<Key>? KeyUp;
    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabGroupKey
    {
        get => _prevTabGroupKey;
        set
        {
            if (_prevTabGroupKey != value)
            {
                ReplaceKey (_prevTabGroupKey, value);
                _prevTabGroupKey = value;
            }
        }
    }

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabKey
    {
        get => _prevTabKey;
        set
        {
            if (_prevTabKey != value)
            {
                ReplaceKey (_prevTabKey, value);
                _prevTabKey = value;
            }
        }
    }
}
