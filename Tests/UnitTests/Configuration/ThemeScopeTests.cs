using System.Collections.Concurrent;
using System.Text.Json;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeScopeTests
{
    [Fact]
    public void Load_AllThemesPresent ()
    {
        Enable (true);

        Load (ConfigLocations.All);
        Assert.True (ThemeManager.Themes.ContainsKey ("Default"));
        Assert.True (ThemeManager.Themes.ContainsKey ("Dark"));
        Assert.True (ThemeManager.Themes.ContainsKey ("Light"));
        Disable (true);
    }

    [Fact]
    public void Apply_ShouldApplyUpdatedProperties ()
    {
        Enable (true);
        Assert.NotEmpty (ThemeManager.Themes);
        Alignment savedValue = Dialog.DefaultButtonAlignment;
        Alignment newValue = Alignment.Center != savedValue ? Alignment.Center : Alignment.Start;

        ThemeManager.GetCurrentTheme () ["Dialog.DefaultButtonAlignment"].PropertyValue = newValue;

        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();
        Assert.Equal (newValue, Dialog.DefaultButtonAlignment);

        // Replace with the savedValue to avoid failures on other unit tests that rely on the default value
        ThemeManager.GetCurrentTheme () ["Dialog.DefaultButtonAlignment"].PropertyValue = savedValue;
        ThemeManager.GetCurrentTheme ().Apply ();
        Assert.Equal (savedValue, Dialog.DefaultButtonAlignment);
        Disable (true);
    }

    [Fact]
    public void UpdateToHardCodedDefaults_Resets_Config_Does_Not_Apply ()
    {
        Enable (true);

        Load (ConfigLocations.LibraryResources);

        Assert.Equal ("Default", ThemeManager.Theme);
        ThemeManager.Theme = "Dark";
        Assert.Equal ("Dark", ThemeManager.Theme);
        Apply ();
        Assert.Equal ("Dark", ThemeManager.Theme);

        // Act
        ThemeManager.ResetToHardCodedDefaults ();
        Assert.Equal ("Default", ThemeManager.Theme);

        Disable (true);
    }

    [Fact]
    public void Serialize_Themes_RoundTrip ()
    {
        Enable (true);

        IDictionary<string, ThemeScope> initial = ThemeManager.Themes;

        string serialized = JsonSerializer.Serialize (ThemeManager.Themes, SerializerContext.Options);

        ConcurrentDictionary<string, ThemeScope> deserialized =
            JsonSerializer.Deserialize<ConcurrentDictionary<string, ThemeScope>> (serialized, SerializerContext.Options);

        Assert.NotEqual (initial, deserialized);
        Assert.Equal (deserialized.Count, initial!.Count);

        Disable (true);
    }

    [Fact]
    public void Serialize_New_RoundTrip ()
    {
        Enable (true);

        var theme = new ThemeScope ();
        theme ["Dialog.DefaultButtonAlignment"].PropertyValue = Alignment.End;

        string json = JsonSerializer.Serialize (theme, SerializerContext.Options);

        var deserialized = JsonSerializer.Deserialize<ThemeScope> (json, SerializerContext.Options);

        Assert.Equal (
                      Alignment.End,
                      (Alignment)deserialized ["Dialog.DefaultButtonAlignment"].PropertyValue!
                     );

        Disable (true);
    }
}
