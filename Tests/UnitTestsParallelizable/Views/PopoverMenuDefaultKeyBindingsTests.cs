// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="PopoverMenu.DefaultKeyBindings"/> static property.
/// </summary>
public class PopoverMenuDefaultKeyBindingsTests
{
    [Fact]
    public void PopoverMenu_DefaultKeyBindings_IsNotNull () => Assert.NotNull (PopoverMenu.DefaultKeyBindings);

    [Fact]
    public void PopoverMenu_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in PopoverMenu.DefaultKeyBindings!)
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
    public void PopoverMenu_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in PopoverMenu.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void PopoverMenu_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (PopoverMenu).GetProperty (nameof (PopoverMenu.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }
}
