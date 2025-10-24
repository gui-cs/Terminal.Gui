#nullable enable

namespace Terminal.Gui.App;

public static partial class Application // Navigation stuff
{
    /// <summary>
    ///     Gets the <see cref="ApplicationNavigation"/> instance for the current <see cref="Application"/>.
    /// </summary>
    public static ApplicationNavigation? Navigation { get; internal set; }

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key NextTabGroupKey
    {
        get => Keyboard.NextTabGroupKey;
        set => Keyboard.NextTabGroupKey = value;
    }

    /// <summary>Alternative key to navigate forwards through views. Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key NextTabKey
    {
        get => Keyboard.NextTabKey;
        set => Keyboard.NextTabKey = value;
    }

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
    public static event EventHandler<Key>? KeyUp
    {
        add => Keyboard.KeyUp += value;
        remove => Keyboard.KeyUp -= value;
    }

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabGroupKey
    {
        get => Keyboard.PrevTabGroupKey;
        set => Keyboard.PrevTabGroupKey = value;
    }

    /// <summary>Alternative key to navigate backwards through views. Shift+Tab is the primary key.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabKey
    {
        get => Keyboard.PrevTabKey;
        set => Keyboard.PrevTabKey = value;
    }
}
