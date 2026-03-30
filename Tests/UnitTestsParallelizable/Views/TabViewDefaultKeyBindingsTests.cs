// Claude - Opus 4.6

using System.Reflection;

namespace ViewsTests;

/// <summary>
///     Tests for <see cref="TabView.DefaultKeyBindings"/> static property.
/// </summary>
public class TabViewDefaultKeyBindingsTests
{
    [Fact]
    public void TabView_DefaultKeyBindings_IsNotNull () => Assert.NotNull (TabView.DefaultKeyBindings);

    [Fact]
    public void TabView_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((Command command, PlatformKeyBinding platformBinding) in TabView.DefaultKeyBindings!)
        {
            Key [] [] allKeyArrays = [platformBinding.All ?? [], platformBinding.Windows ?? [], platformBinding.Linux ?? [], platformBinding.Macos ?? []];

            foreach (Key [] keyArray in allKeyArrays)
            {
                foreach (Key key in keyArray)
                {
                    Assert.NotEqual (Key.Empty, key);
                }
            }
        }
    }

    [Fact]
    public void TabView_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (Command command in TabView.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.IsDefined (command), $"Command name '{command}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void TabView_DefaultKeyBindings_DoesNotHaveConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (TabView).GetProperty (nameof (TabView.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property!.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.Null (attr);
    }
}
