namespace Terminal.Gui.Drivers;

/// <summary>
///     Holds global driver settings.
/// </summary>
public sealed class Driver
{
    private static bool _force16Colors = false; // Resources/config.json overrides

    // NOTE: Force16Colors is a configuration property (Driver.Force16Colors).
    // NOTE: IDriver also has a Force16Colors property, which is an instance property
    // NOTE: set whenever this static property is set.
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
