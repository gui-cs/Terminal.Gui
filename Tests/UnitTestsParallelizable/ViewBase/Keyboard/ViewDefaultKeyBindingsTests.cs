// Claude - Opus 4.6

using System.Reflection;

namespace ViewBaseTests.Keyboard;

/// <summary>
///     Tests for <see cref="View.DefaultKeyBindings"/> and <see cref="View.ViewKeyBindings"/> static properties.
/// </summary>
public class ViewDefaultKeyBindingsTests
{
    [Fact]
    public void View_DefaultKeyBindings_IsNotNull () => Assert.NotNull (View.DefaultKeyBindings);

    [Theory]
    [InlineData ("Left")]
    [InlineData ("Right")]
    [InlineData ("Up")]
    [InlineData ("Down")]
    [InlineData ("PageUp")]
    [InlineData ("PageDown")]
    [InlineData ("LeftStart")]
    [InlineData ("RightEnd")]
    [InlineData ("Start")]
    [InlineData ("End")]
    public void View_DefaultKeyBindings_ContainsNavigationCommands (string commandName) => Assert.True (View.DefaultKeyBindings!.ContainsKey (commandName));

    [Theory]
    [InlineData ("LeftExtend")]
    [InlineData ("RightExtend")]
    [InlineData ("UpExtend")]
    [InlineData ("DownExtend")]
    [InlineData ("PageUpExtend")]
    [InlineData ("PageDownExtend")]
    [InlineData ("LeftStartExtend")]
    [InlineData ("RightEndExtend")]
    [InlineData ("StartExtend")]
    [InlineData ("EndExtend")]
    public void View_DefaultKeyBindings_ContainsSelectionExtendCommands (string commandName) =>
        Assert.True (View.DefaultKeyBindings!.ContainsKey (commandName));

    [Theory]
    [InlineData ("Copy")]
    [InlineData ("Cut")]
    [InlineData ("Paste")]
    public void View_DefaultKeyBindings_ContainsClipboardCommands (string commandName) => Assert.True (View.DefaultKeyBindings!.ContainsKey (commandName));

    [Theory]
    [InlineData ("Undo")]
    [InlineData ("Redo")]
    [InlineData ("SelectAll")]
    [InlineData ("DeleteCharLeft")]
    [InlineData ("DeleteCharRight")]
    public void View_DefaultKeyBindings_ContainsEditingCommands (string commandName) => Assert.True (View.DefaultKeyBindings!.ContainsKey (commandName));

    [Fact]
    public void View_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        foreach ((string commandName, PlatformKeyBinding platformBinding) in View.DefaultKeyBindings!)
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
    public void View_DefaultKeyBindings_AllCommandNamesParseable ()
    {
        foreach (string commandName in View.DefaultKeyBindings!.Keys)
        {
            Assert.True (Enum.TryParse<Command> (commandName, out _), $"Command name '{commandName}' should parse to a Command enum value.");
        }
    }

    [Fact]
    public void View_DefaultKeyBindings_HasConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (View).GetProperty (nameof (View.DefaultKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.NotNull (attr);
    }

    [Fact]
    public void View_ViewKeyBindings_IsNull_ByDefault () =>

        // ViewKeyBindings should be null unless set via configuration
        Assert.Null (View.ViewKeyBindings);

    [Fact]
    public void View_ViewKeyBindings_HasConfigurationPropertyAttribute ()
    {
        PropertyInfo? property = typeof (View).GetProperty (nameof (View.ViewKeyBindings), BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (property);

        var attr = property.GetCustomAttribute<ConfigurationPropertyAttribute> ();

        Assert.NotNull (attr);
    }
}
