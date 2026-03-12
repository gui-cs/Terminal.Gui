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
        foreach ((Command command, PlatformKeyBinding platformBinding) in LinearRange<object>.DefaultKeyBindings!)
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
    public void LinearRange_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in LinearRange<object>.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
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
    [InlineData (Command.Accept)]
    [InlineData (Command.Activate)]
    public void LinearRange_DefaultKeyBindings_ContainsUniqueCommands (Command command) =>
        Assert.True (LinearRange<object>.DefaultKeyBindings!.ContainsKey (command));
}
