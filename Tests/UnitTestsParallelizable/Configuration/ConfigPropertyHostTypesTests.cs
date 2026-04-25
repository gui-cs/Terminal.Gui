// Claude - Opus 4.7
using System.Reflection;
using Terminal.Gui.Configuration;

namespace ConfigurationTests;

public class ConfigPropertyHostTypesTests
{
    /// <summary>
    ///     Guard-rail test: the hard-coded list in <see cref="ConfigPropertyHostTypes"/> must exhaustively cover every type in
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

        // Act
        HashSet<Type> registered = ConfigPropertyHostTypes.GetTypes ().ToHashSet ();

        // Assert
        Type [] missing = reflected.Except (registered).ToArray ();
        Type [] extra = registered.Except (reflected).ToArray ();

        Assert.True (
                     missing.Length == 0 && extra.Length == 0,
                     $"ConfigPropertyHostTypes drift detected.\nMissing (add to list): {string.Join (", ", missing.Select (t => t.FullName))}\nExtra (remove from list): {string.Join (", ", extra.Select (t => t.FullName))}");
    }

    private static bool HasConfigurationProperty (Type type)
    {
        // Mirror the production scan in ConfigProperty.Initialize (), which uses type.GetProperties () —
        // public instance properties only. Widening the binding flags here would flag types that the
        // production scan could never discover and the trimmer wouldn't preserve via PublicProperties.
        PropertyInfo [] properties = type.GetProperties ();

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
