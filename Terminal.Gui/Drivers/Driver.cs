namespace Terminal.Gui.Drivers;

/// <summary>
///     Provides driver-wide configuration settings.
/// </summary>
public static class Driver
{
    private static bool _force16Colors = false; // Resources/config.json overrides

    /// <summary>
    ///     Gets or sets whether drivers should use 16 colors instead of the default TrueColors.
    ///     This is a process-wide setting that is read by each driver instance at construction time.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This setting is read by driver instances when they are created. Changing this value after
    ///         a driver has been initialized will not affect existing driver instances.
    ///     </para>
    ///     <para>
    ///         Individual drivers may override this if they do not support TrueColor output.
    ///     </para>
    /// </remarks>
    [ConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = false)]
    public static bool Force16Colors
    {
        get => _force16Colors;
        set => _force16Colors = value;
    }
}
