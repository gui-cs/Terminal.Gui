// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="LinearRange{T}.DefaultKeyBindings"/> static property.
/// </summary>
public class LinearRangeDefaultKeyBindingsTests
{
    [Fact]
    public void LinearRange_DefaultKeyBindings_IsNotNull () => Assert.NotNull (LinearRange<object>.DefaultKeyBindings);

    [Fact]
    public void LinearRange_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((string commandName, PlatformKeyBinding platformBinding) in LinearRange<object>.DefaultKeyBindings!)
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
    public void LinearRange_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (string commandName in LinearRange<object>.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.TryParse<Command> (commandName, out _), $"Command name '{commandName}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void LinearRange_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property =
            typeof (LinearRange<object>).GetProperty (nameof (LinearRange<object>.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }

    [Theory]
    [InlineData ("Accept")]
    [InlineData ("Activate")]
    public void LinearRange_DefaultKeyBindings_ContainsUniqueCommands (string commandName) =>
        Assert.True (LinearRange<object>.DefaultKeyBindings!.ContainsKey (commandName));
}
