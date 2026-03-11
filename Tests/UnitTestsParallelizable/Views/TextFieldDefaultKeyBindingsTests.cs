// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TextField.DefaultKeyBindings"/> static property.
/// </summary>
public class TextFieldDefaultKeyBindingsTests
{
    [Fact]
    public void TextField_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TextField.DefaultKeyBindings);

    [Fact]
    public void TextField_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((string commandName, PlatformKeyBinding platformBinding) in TextField.DefaultKeyBindings!)
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
    public void TextField_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (string commandName in TextField.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.TryParse<Command> (commandName, out _), $"Command name '{commandName}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TextField_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (TextField).GetProperty (nameof (TextField.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }

    [Fact]
    public void TextField_DefaultKeyBindings_ContainsUniqueCommands ()
    {
        Dictionary<string, PlatformKeyBinding> bindings = TextField.DefaultKeyBindings!;

        // Verify TextField-specific commands are present
        Assert.True (bindings.ContainsKey ("WordLeft"), "Should contain WordLeft");
        Assert.True (bindings.ContainsKey ("WordRight"), "Should contain WordRight");
        Assert.True (bindings.ContainsKey ("WordLeftExtend"), "Should contain WordLeftExtend");
        Assert.True (bindings.ContainsKey ("WordRightExtend"), "Should contain WordRightExtend");
        Assert.True (bindings.ContainsKey ("CutToEndOfLine"), "Should contain CutToEndOfLine");
        Assert.True (bindings.ContainsKey ("CutToStartOfLine"), "Should contain CutToStartOfLine");
        Assert.True (bindings.ContainsKey ("KillWordRight"), "Should contain KillWordRight");
        Assert.True (bindings.ContainsKey ("KillWordLeft"), "Should contain KillWordLeft");
        Assert.True (bindings.ContainsKey ("ToggleOverwrite"), "Should contain ToggleOverwrite");
        Assert.True (bindings.ContainsKey ("DeleteAll"), "Should contain DeleteAll");

        // Verify Emacs bindings (all platforms)
        Assert.True (bindings.ContainsKey ("Left"), "Should contain Left (Emacs Ctrl+B)");
        Assert.NotNull (bindings ["Left"].All);

        Assert.True (bindings.ContainsKey ("Right"), "Should contain Right (Emacs Ctrl+F)");
        Assert.NotNull (bindings ["Right"].All);
    }
}
