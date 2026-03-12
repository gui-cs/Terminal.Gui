// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="NumericUpDown{T}.DefaultKeyBindings"/> static property.
/// </summary>
public class NumericUpDownDefaultKeyBindingsTests
{
    [Fact]
    public void NumericUpDown_DefaultKeyBindings_IsNotNull () => Assert.NotNull (NumericUpDown<int>.DefaultKeyBindings);

    [Fact]
    public void NumericUpDown_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in NumericUpDown<int>.DefaultKeyBindings!)
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
    public void NumericUpDown_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in NumericUpDown<int>.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void NumericUpDown_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (NumericUpDown<int>).GetProperty (nameof (NumericUpDown<int>.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property!.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }
}
