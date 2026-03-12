// Claude - Opus 4.6

using System.Reflection;

namespace ApplicationTests.Keyboard;

/// <summary>
///     Tests for <see cref="ApplicationKeyboard.DefaultKeyBindings"/> static property.
/// </summary>
public class ApplicationDefaultKeyBindingsTests
{
    [Fact]
    public void Application_DefaultKeyBindings_IsNotNull () => Assert.NotNull (ApplicationKeyboard.DefaultKeyBindings);

    [Fact]
    public void Application_DefaultKeyBindings_ContainsQuit ()
    {
        Dictionary<string, PlatformKeyBinding>? bindings = ApplicationKeyboard.DefaultKeyBindings;
        Assert.NotNull (bindings);
        Assert.True (bindings.ContainsKey ("Quit"));

        PlatformKeyBinding quit = bindings ["Quit"];
        Assert.NotNull (quit.All);
        Assert.Contains ("Esc", quit.All!);
    }

    [Fact]
    public void Application_DefaultKeyBindings_SuspendIsNonWindows ()
    {
        Dictionary<string, PlatformKeyBinding>? bindings = ApplicationKeyboard.DefaultKeyBindings;
        Assert.NotNull (bindings);
        Assert.True (bindings.ContainsKey ("Suspend"));

        PlatformKeyBinding suspend = bindings ["Suspend"];
        Assert.Null (suspend.All);
        Assert.NotNull (suspend.Linux);
        Assert.Contains ("Ctrl+Z", suspend.Linux!);
        Assert.NotNull (suspend.Macos);
        Assert.Contains ("Ctrl+Z", suspend.Macos!);
    }

    [Fact]
    public void Application_DefaultKeyBindings_AllKeyStringsParseable ()
    {
        Dictionary<string, PlatformKeyBinding>? bindings = ApplicationKeyboard.DefaultKeyBindings;
        Assert.NotNull (bindings);

        foreach (KeyValuePair<string, PlatformKeyBinding> entry in bindings)
        {
            string commandName = entry.Key;
            PlatformKeyBinding binding = entry.Value;

            AssertKeysParseable (binding.All, commandName, "All");
            AssertKeysParseable (binding.Windows, commandName, "Windows");
            AssertKeysParseable (binding.Linux, commandName, "Linux");
            AssertKeysParseable (binding.Macos, commandName, "Macos");
        }
    }

    [Fact]
    public void Application_DefaultKeyBindings_HasConfigurationPropertyAttribute ()
    {
        PropertyInfo? propertyInfo = typeof (ApplicationKeyboard).GetProperty ("DefaultKeyBindings", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull (propertyInfo);

        var attr = propertyInfo.GetCustomAttribute<ConfigurationPropertyAttribute> ();
        Assert.NotNull (attr);
    }

    [Fact]
    public void Application_DefaultKeyBindings_ContainsExpectedCommands ()
    {
        Dictionary<string, PlatformKeyBinding>? bindings = ApplicationKeyboard.DefaultKeyBindings;
        Assert.NotNull (bindings);

        string [] expectedCommands = ["Quit", "Suspend", "Arrange", "NextTabStop", "PreviousTabStop", "NextTabGroup", "PreviousTabGroup", "Refresh"];

        foreach (string command in expectedCommands)
        {
            Assert.True (bindings.ContainsKey (command), $"Expected command '{command}' not found in DefaultKeyBindings.");
        }

        Assert.Equal (expectedCommands.Length, bindings.Count);
    }

    private static void AssertKeysParseable (string []? keys, string commandName, string platformName)
    {
        if (keys is null)
        {
            return;
        }

        foreach (string keyString in keys)
        {
            bool parsed = Key.TryParse (keyString, out Key _);
            Assert.True (parsed, $"Key string '{keyString}' for command '{commandName}' ({platformName}) could not be parsed.");
        }
    }
}
