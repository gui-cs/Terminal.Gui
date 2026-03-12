// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="MenuBar.DefaultKeyBindings"/> static property.
/// </summary>
public class MenuBarDefaultKeyBindingsTests
{
    [Fact]
    public void MenuBar_DefaultKeyBindings_IsNotNull () => Assert.NotNull (MenuBar.DefaultKeyBindings);

    [Fact]
    public void MenuBar_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((string commandName, PlatformKeyBinding platformBinding) in MenuBar.DefaultKeyBindings!)
        {
            string [] [] allKeyArrays = [platformBinding.All ?? [], platformBinding.Windows ?? [], platformBinding.Linux ?? [], platformBinding.Macos ?? []];

            foreach (string [] keyArray in allKeyArrays)
            {
                foreach (string keyString in keyArray)
                {
                    Assert.True (Key.TryParse (keyString, out _), $"Key string '{keyString}' for command '{commandName}' should be parseable.");
                }
            }
        }
    }

    [Fact]
    public void MenuBar_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (string commandName in MenuBar.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.TryParse<Command> (commandName, out _), $"Command name '{commandName}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void MenuBar_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (MenuBar).GetProperty (nameof (MenuBar.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property!.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }

    [Fact]
    public void MenuBar_DefaultKey_IsF10 ()
    {
        Assert.Equal (Key.F10, MenuBar.DefaultKey);
    }

    [Fact]
    public void PopoverMenu_DefaultKey_IsShiftF10 ()
    {
        Assert.Equal (Key.F10.WithShift, PopoverMenu.DefaultKey);
    }

    [Fact]
    public void DropDownList_Toggle_F4_And_AltDown ()
    {
        Dictionary<string, PlatformKeyBinding>? bindings = DropDownList.DefaultKeyBindings;
        Assert.NotNull (bindings);
        Assert.True (bindings!.ContainsKey ("Toggle"), "Should contain Toggle command");

        PlatformKeyBinding toggle = bindings ["Toggle"];
        Assert.NotNull (toggle.All);
        Assert.Contains ("F4", toggle.All!);
        Assert.Contains ("Alt+CursorDown", toggle.All!);
    }
}
