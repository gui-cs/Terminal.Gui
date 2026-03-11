// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="HexView.DefaultKeyBindings"/> static property.
/// </summary>
public class HexViewDefaultKeyBindingsTests
{
    [Fact]
    public void HexView_DefaultKeyBindings_IsNotNull () => Assert.NotNull (HexView.DefaultKeyBindings);

    [Fact]
    public void HexView_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((string commandName, PlatformKeyBinding platformBinding) in HexView.DefaultKeyBindings!)
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
    public void HexView_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (string commandName in HexView.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.TryParse<Command> (commandName, out _), $"Command name '{commandName}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void HexView_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (HexView).GetProperty (nameof (HexView.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }

    [Theory]
    [InlineData ("StartOfPage")]
    [InlineData ("EndOfPage")]
    [InlineData ("Insert")]
    public void HexView_DefaultKeyBindings_ContainsUniqueCommands (string commandName) => Assert.True (HexView.DefaultKeyBindings!.ContainsKey (commandName));
}
