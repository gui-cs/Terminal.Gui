// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="DropDownList.DefaultKeyBindings"/> static property.
/// </summary>
public class DropDownListDefaultKeyBindingsTests
{
    [Fact]
    public void DropDownList_DefaultKeyBindings_IsNotNull () => Assert.NotNull (DropDownList.DefaultKeyBindings);

    [Fact]
    public void DropDownList_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in DropDownList.DefaultKeyBindings!)
        {
            string [] [] allKeyArrays = [platformBinding.All ?? [], platformBinding.Windows ?? [], platformBinding.Linux ?? [], platformBinding.Macos ?? []];

            foreach (string [] keyArray in allKeyArrays)
            {
                foreach (string keyString in keyArray)
                {
                    Assert.True (Key.TryParse (keyString, out _), $"Key string '{keyString}' for command '{command}' should be parseable.");
                }
            }
        }
    }

    [Fact]
    public void DropDownList_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in DropDownList.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void DropDownList_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (DropDownList).GetProperty (nameof (DropDownList.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }

    [Fact]
    public void DropDownList_DefaultKeyBindings_ContainsToggle () => Assert.True (DropDownList.DefaultKeyBindings!.ContainsKey (Command.Toggle));
}
