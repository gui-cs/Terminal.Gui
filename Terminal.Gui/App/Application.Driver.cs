
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

    private static bool _force16Colors = false; // Resources/config.json overrides

    /// <inheritdoc cref="IApplication.Force16Colors"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    [Obsolete ("The legacy static Application object is going away.")]
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

    private static string _forceDriver = string.Empty; // Resources/config.json overrides

    /// <inheritdoc cref="IApplication.ForceDriver"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    [Obsolete ("The legacy static Application object is going away.")]
    public static string ForceDriver
    {
        get => ApplicationImpl.Instance.ForceDriver;
        set
        {
            string oldValue = _forceDriver;
            _forceDriver = ApplicationImpl.Instance.ForceDriver = value;
            ForceDriverChanged?.Invoke (null, new ValueChangedEventArgs<string> (oldValue, _forceDriver));
        }
    }

    /// <summary>Raised when <see cref="ForceDriver"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<string>>? ForceDriverChanged;

    /// <inheritdoc cref="IDriver.Sixel"/>
    [Obsolete ("The legacy static Application object is going away.")]
    public static List<SixelToRender> Sixel => ApplicationImpl.Instance.Driver?.Sixel!;

    /// <summary>Gets a list of <see cref="IDriver"/> types and type names that are available.</summary>
    /// <returns></returns>
    [RequiresUnreferencedCode ("AOT")]
    [Obsolete ("The legacy static Application object is going away.")]
    public static (List<Type?>, List<string?>) GetDriverTypes ()
    {
        // use reflection to get the list of drivers
        List<Type?> driverTypes = new ();

        // Only inspect the IDriver assembly
        var asm = typeof (IDriver).Assembly;

        foreach (Type? type in asm.GetTypes ())
        {
            if (typeof (IDriver).IsAssignableFrom (type) && type is { IsAbstract: false, IsClass: true })
            {
                driverTypes.Add (type);
            }
        }

        List<string?> driverTypeNames = driverTypes
                                        .Where (d => !typeof (IDriver).IsAssignableFrom (d))
                                        .Select (d => d!.Name)
                                        .Union (["dotnet", "windows", "unix", "fake"])
                                        .ToList ()!;

        return (driverTypes, driverTypeNames);
    }
}
