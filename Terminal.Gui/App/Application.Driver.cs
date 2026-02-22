

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public static partial class Application // Driver abstractions
{
    /// <inheritdoc cref="IApplication.Driver"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static IDriver? Driver
    {
        get => ApplicationImpl.Instance.Driver;
        internal set => ApplicationImpl.Instance.Driver = value;
    }

    // NOTE: ForceDriver is a configuration property (Application.ForceDriver).
    // NOTE: IApplication also has a ForceDriver property, which is an instance property
    // NOTE: set whenever this static property is set.
    private static string _forceDriver = string.Empty; // Resources/config.json overrides

    /// <inheritdoc cref="IApplication.ForceDriver"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static string ForceDriver
    {
        get => _forceDriver;
        set
        {
            string oldValue = _forceDriver;
            _forceDriver = value;
            ForceDriverChanged?.Invoke (null, new (oldValue, _forceDriver));
        }
    }

    /// <summary>Raised when <see cref="ForceDriver"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<string>>? ForceDriverChanged;

    /// <inheritdoc cref="IDriver.GetSixels"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static ConcurrentQueue<SixelToRender> GetSixels () => ApplicationImpl.Instance.Driver?.GetSixels ()!;

    /// <summary>
    ///     Gets the names of all registered drivers.
    /// </summary>
    /// <returns>Enumerable of driver names.</returns>
    public static IEnumerable<string> GetRegisteredDriverNames () => DriverRegistry.GetDriverNames ();

    /// <summary>
    ///     Gets all registered driver descriptors with metadata.
    /// </summary>
    /// <returns>Enumerable of driver descriptors containing name, display name, description, etc.</returns>
    public static IEnumerable<DriverRegistry.DriverDescriptor> GetRegisteredDrivers () => DriverRegistry.GetDrivers ();

    /// <summary>
    ///     Checks if a driver name is valid/registered (case-insensitive).
    /// </summary>
    /// <param name="driverName">The driver name to validate.</param>
    /// <returns>True if the driver is registered; false otherwise.</returns>
    public static bool IsDriverNameValid (string driverName) => DriverRegistry.IsRegistered (driverName);

    /// <summary>Gets a list of <see cref="IDriver"/> types and type names that are available.</summary>
    /// <returns></returns>
    [RequiresUnreferencedCode ("AOT")]
    [Obsolete ("Use GetRegisteredDriverNames() or GetRegisteredDrivers() instead. This method uses reflection and is not AOT-friendly.")]
    public static (List<Type?>, List<string?>) GetDriverTypes ()
    {
        // Keep for backward compatibility - return empty types list and names from registry
        return ([], DriverRegistry.GetDriverNames ().ToList ()!)!;
    }
}
