// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TextView.DefaultKeyBindings"/> static property.
/// </summary>
public class TextViewDefaultKeyBindingsTests
{
    [Fact]
    public void TextView_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TextView.DefaultKeyBindings);

    [Fact]
    public void TextView_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((string commandName, PlatformKeyBinding platformBinding) in TextView.DefaultKeyBindings!)
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
    public void TextView_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (string commandName in TextView.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.TryParse<Command> (commandName, out _), $"Command name '{commandName}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TextView_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (TextView).GetProperty (nameof (TextView.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }

    [Fact]
    public void TextView_DefaultKeyBindings_ContainsTextViewSpecificCommands ()
    {
        Dictionary<string, PlatformKeyBinding> bindings = TextView.DefaultKeyBindings!;

        // Verify TextView-specific commands are present
        Assert.True (bindings.ContainsKey ("ToggleExtend"), "Should contain ToggleExtend");
        Assert.True (bindings.ContainsKey ("CutToEndOfLine"), "Should contain CutToEndOfLine");
        Assert.True (bindings.ContainsKey ("CutToStartOfLine"), "Should contain CutToStartOfLine");
        Assert.True (bindings.ContainsKey ("DeleteAll"), "Should contain DeleteAll");
        Assert.True (bindings.ContainsKey ("WordLeft"), "Should contain WordLeft");
        Assert.True (bindings.ContainsKey ("WordRight"), "Should contain WordRight");
        Assert.True (bindings.ContainsKey ("WordLeftExtend"), "Should contain WordLeftExtend");
        Assert.True (bindings.ContainsKey ("WordRightExtend"), "Should contain WordRightExtend");
        Assert.True (bindings.ContainsKey ("KillWordRight"), "Should contain KillWordRight");
        Assert.True (bindings.ContainsKey ("KillWordLeft"), "Should contain KillWordLeft");
        Assert.True (bindings.ContainsKey ("ToggleOverwrite"), "Should contain ToggleOverwrite");
        Assert.True (bindings.ContainsKey ("Open"), "Should contain Open");
    }
}
