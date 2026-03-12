// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="DateEditor.DefaultKeyBindings"/> static property.
/// </summary>
public class DateEditorDefaultKeyBindingsTests
{
    [Fact]
    public void DateEditor_DefaultKeyBindings_IsNotNull () => Assert.NotNull (DateEditor.DefaultKeyBindings);

    [Fact]
    public void DateEditor_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((string commandName, PlatformKeyBinding platformBinding) in DateEditor.DefaultKeyBindings!)
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
    public void DateEditor_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (string commandName in DateEditor.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.TryParse<Command> (commandName, out _), $"Command name '{commandName}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void DateEditor_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (DateEditor).GetProperty (nameof (DateEditor.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property!.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }
}
