
namespace Terminal.Gui.App;

public static partial class Application // Navigation stuff
{
    /// <summary>
    ///     Gets the <see cref="ApplicationNavigation"/> instance for the current <see cref="Application"/>.
    /// </summary>
    [Obsolete ("The legacy static Application object is going away.")]
    public static ApplicationNavigation? Navigation
    {
        get => ApplicationImpl.Instance.Navigation;
        internal set => ApplicationImpl.Instance.Navigation = value;
    }

    private static Key _nextTabGroupKey = Key.F6; // Resources/config.json overrides

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    [Obsolete ("The legacy static Application object is going away.")]
    public static Key NextTabGroupKey
    {
        get => _nextTabGroupKey;
        set
        {
            Key oldValue = _nextTabGroupKey;
            _nextTabGroupKey = value;
            NextTabGroupKeyChanged?.Invoke (null, new ValueChangedEventArgs<Key> (oldValue, _nextTabGroupKey));
        }
    }

    /// <summary>Raised when <see cref="NextTabGroupKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? NextTabGroupKeyChanged;

    private static Key _nextTabKey = Key.Tab; // Resources/config.json overrides

    /// <summary>Alternative key to navigate forwards through views. Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key NextTabKey
    {
        get => _nextTabKey;
        set
        {
            Key oldValue = _nextTabKey;
            _nextTabKey = value;
            NextTabKeyChanged?.Invoke (null, new ValueChangedEventArgs<Key> (oldValue, _nextTabKey));
        }
    }

    /// <summary>Raised when <see cref="NextTabKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? NextTabKeyChanged;

    /// <summary>
    ///     Raised when the user releases a key.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Unix) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/>.</para>
    /// </remarks>
    [Obsolete ("The legacy static Application object is going away.")]
    public static event EventHandler<Key>? KeyUp
    {
        add => ApplicationImpl.Instance.Keyboard.KeyUp += value;
        remove => ApplicationImpl.Instance.Keyboard.KeyUp -= value;
    }

    private static Key _prevTabGroupKey = Key.F6.WithShift; // Resources/config.json overrides

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabGroupKey
    {
        get => _prevTabGroupKey;
        set
        {
            Key oldValue = _prevTabGroupKey;
            _prevTabGroupKey = value;
            PrevTabGroupKeyChanged?.Invoke (null, new ValueChangedEventArgs<Key> (oldValue, _prevTabGroupKey));
        }
    }

    /// <summary>Raised when <see cref="PrevTabGroupKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? PrevTabGroupKeyChanged;

    private static Key _prevTabKey = Key.Tab.WithShift; // Resources/config.json overrides

    /// <summary>Alternative key to navigate backwards through views. Shift+Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabKey
    {
        get => _prevTabKey;
        set
        {
            Key oldValue = _prevTabKey;
            _prevTabKey = value;
            PrevTabKeyChanged?.Invoke (null, new ValueChangedEventArgs<Key> (oldValue, _prevTabKey));
        }
    }

    /// <summary>Raised when <see cref="PrevTabKey"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<Key>>? PrevTabKeyChanged;
}
