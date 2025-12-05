using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Holds global driver settings.
/// </summary>
public sealed class Driver
{
    private static bool _force16Colors = false; // Resources/config.json overrides

    /// <summary>
    ///     Determines if driver instances should use 16 colors instead of the default TrueColors.
    /// </summary>
    /// <seealso cref="IDriver.Force16Colors"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool Force16Colors
    {
        get => _force16Colors;
        set
        {
            bool oldValue = _force16Colors;
            _force16Colors = value;
            Force16ColorsChanged?.Invoke (null, new ValueChangedEventArgs<bool> (oldValue, _force16Colors));
        }
    }

    /// <summary>Raised when <see cref="Force16Colors"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<bool>>? Force16ColorsChanged;

}
