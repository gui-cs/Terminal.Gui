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
        foreach ((Command command, PlatformKeyBinding platformBinding) in MenuBar.DefaultKeyBindings!)
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
    public void MenuBar_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in MenuBar.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
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
        Dictionary<Command, PlatformKeyBinding>? bindings = DropDownList.DefaultKeyBindings;
        Assert.NotNull (bindings);
        Assert.True (bindings!.ContainsKey (Command.Toggle), "Should contain Toggle command");

        PlatformKeyBinding toggle = bindings [Command.Toggle];
        Assert.NotNull (toggle.All);
        Assert.Contains ("F4", toggle.All!);
        Assert.Contains ("Alt+CursorDown", toggle.All!);
    }
}
