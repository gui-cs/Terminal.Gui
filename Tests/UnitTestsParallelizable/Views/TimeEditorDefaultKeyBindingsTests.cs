// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TimeEditor.DefaultKeyBindings"/> static property.
/// </summary>
public class TimeEditorDefaultKeyBindingsTests
{
    [Fact]
    public void TimeEditor_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TimeEditor.DefaultKeyBindings);

    [Fact]
    public void TimeEditor_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in TimeEditor.DefaultKeyBindings!)
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
    public void TimeEditor_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in TimeEditor.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TimeEditor_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (TimeEditor).GetProperty (nameof (TimeEditor.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }
}
