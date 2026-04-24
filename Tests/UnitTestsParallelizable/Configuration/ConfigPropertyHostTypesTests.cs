// Claude - Opus 4.7
using System.Reflection;

namespace ConfigurationTests;

public class ConfigPropertyHostTypesTests
{
    /// <summary>
    ///     Guard-rail test: the hard-coded list in <c>ConfigPropertyHostTypes</c> must exhaustively cover every type in
    ///     the Terminal.Gui assembly that declares a <see cref="ConfigurationPropertyAttribute"/> property. Drift between
    ///     the list and the attribute usage would silently reintroduce the trim/AOT failure fixed by
    ///     <see href="https://github.com/gui-cs/Terminal.Gui/issues/5069"/>.
    /// </summary>
    [Fact]
    public void ConfigPropertyHostTypes_GetTypes_Matches_Reflected_Hosts_In_TerminalGui_Assembly ()
    {
        // Arrange
        Assembly terminalGuiAssembly = typeof (ConfigurationManager).Assembly;

        HashSet<Type> reflected = terminalGuiAssembly
                                  .GetTypes ()
                                  .Where (HasConfigurationProperty)
                                  .ToHashSet ();

        // Act: invoke via reflection because ConfigPropertyHostTypes is internal.
        Type hostTypesType = terminalGuiAssembly.GetType ("Terminal.Gui.Configuration.ConfigPropertyHostTypes")!;
        MethodInfo getTypes = hostTypesType.GetMethod ("GetTypes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        HashSet<Type> registered = ((Type [])getTypes.Invoke (null, null)!).ToHashSet ();

        // Assert
        Type [] missing = reflected.Except (registered).ToArray ();
        Type [] extra = registered.Except (reflected).ToArray ();

        Assert.True (
                     missing.Length == 0 && extra.Length == 0,
                     $"ConfigPropertyHostTypes drift detected.\nMissing (add to list): {string.Join (", ", missing.Select (t => t.FullName))}\nExtra (remove from list): {string.Join (", ", extra.Select (t => t.FullName))}");
    }

    private static bool HasConfigurationProperty (Type type)
    {
        PropertyInfo [] properties = type.GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (property.GetCustomAttribute<ConfigurationPropertyAttribute> () is { })
            {
                return true;
            }
        }

        return false;
    }
}
