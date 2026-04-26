// Claude - Opus 4.7
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    ///     Guard-rail test: the <see cref="DynamicDependencyAttribute"/> set on <c>GetTypes</c> must exactly match the
    ///     <c>_types</c> array it returns. The attributes are what the trimmer actually reads to preserve members; if
    ///     someone adds a type to the array but forgets the matching attribute (or vice-versa), AOT builds would silently
    ///     lose rooting again without the reflected-hosts test above catching it.
    /// </summary>
    [Fact]
    public void ConfigPropertyHostTypes_DynamicDependencies_Match_Returned_Types ()
    {
        // Arrange
        MethodInfo getTypes = typeof (ConfigPropertyHostTypes).GetMethod (
                                                                          nameof (ConfigPropertyHostTypes.GetTypes),
                                                                          BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

        HashSet<Type> rooted = getTypes
                               .GetCustomAttributes<DynamicDependencyAttribute> ()
                               .Select (a => a.Type!)
                               .ToHashSet ();

        HashSet<Type> returned = ConfigPropertyHostTypes.GetTypes ().ToHashSet ();

        // Assert
        Type [] missingRoots = returned.Except (rooted).ToArray ();
        Type [] extraRoots = rooted.Except (returned).ToArray ();

        Assert.True (
                     missingRoots.Length == 0 && extraRoots.Length == 0,
                     $"ConfigPropertyHostTypes [DynamicDependency] drift detected.\nTypes in array but not rooted by attribute: {string.Join (", ", missingRoots.Select (t => t.FullName))}\nTypes rooted by attribute but not in array: {string.Join (", ", extraRoots.Select (t => t.FullName))}");
    }

    private static bool HasConfigurationProperty (Type type)
    {
        // Mirror the production scan in ConfigProperty.Initialize (), which calls type.GetProperties () — i.e.,
        // public properties only (includes inherited public members; excludes non-public and, by default, statics
        // not declared on `type` itself). Widening the binding flags here would flag types the production scan
        // could never discover and the trimmer wouldn't preserve via PublicProperties.
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
