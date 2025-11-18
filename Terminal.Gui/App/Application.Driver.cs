
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

    /// <inheritdoc cref="IApplication.Force16Colors"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    [Obsolete ("The legacy static Application object is going away.")]
    public static bool Force16Colors
    {
        get => ApplicationImpl.Instance.Force16Colors;
        set => ApplicationImpl.Instance.Force16Colors = value;
    }

    /// <inheritdoc cref="IApplication.ForceDriver"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    [Obsolete ("The legacy static Application object is going away.")]
    public static string ForceDriver
    {
        get => ApplicationImpl.Instance.ForceDriver;
        set => ApplicationImpl.Instance.ForceDriver = value;
    }

    /// <inheritdoc cref="IApplication.Sixel"/>
    [Obsolete ("The legacy static Application object is going away.")] 
    public static List<SixelToRender> Sixel => ApplicationImpl.Instance.Sixel;

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
