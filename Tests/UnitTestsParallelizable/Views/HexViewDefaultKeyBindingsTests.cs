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
        foreach ((Command command, PlatformKeyBinding platformBinding) in HexView.DefaultKeyBindings!)
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
    public void HexView_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in HexView.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
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
    [InlineData (Command.StartOfPage)]
    [InlineData (Command.EndOfPage)]
    [InlineData (Command.Insert)]
    public void HexView_DefaultKeyBindings_ContainsUniqueCommands (Command command) => Assert.True (HexView.DefaultKeyBindings!.ContainsKey (command));
}
