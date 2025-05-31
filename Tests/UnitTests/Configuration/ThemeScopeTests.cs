using System.Collections.Concurrent;
using System.Text.Json;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeScopeTests
{
    [Fact]
    public void Load_AllThemesPresent ()
    {
        Enable (ConfigLocations.HardCoded);

        Load (ConfigLocations.All);
        Assert.True (ThemeManager.Themes!.ContainsKey ("Default"));
        Assert.True (ThemeManager.Themes.ContainsKey ("Dark"));
        Assert.True (ThemeManager.Themes.ContainsKey ("Light"));
        Disable (true);
    }

    [Fact]
    public void Apply_ShouldApplyUpdatedProperties ()
    {
        Enable (ConfigLocations.HardCoded);
        Assert.NotEmpty (ThemeManager.Themes!);

        Alignment savedButtonAlignment = Dialog.DefaultButtonAlignment;
        Alignment newButtonAlignment = Alignment.Center != savedButtonAlignment ? Alignment.Center : Alignment.Start;
        ThemeManager.GetCurrentTheme () ["Dialog.DefaultButtonAlignment"].PropertyValue = newButtonAlignment;

        LineStyle savedBorderStyle = Dialog.DefaultBorderStyle;
        LineStyle newBorderStyle = LineStyle.HeavyDotted;
        ThemeManager.GetCurrentTheme () ["Dialog.DefaultBorderStyle"].PropertyValue = newBorderStyle;

        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();
        Assert.Equal (newButtonAlignment, Dialog.DefaultButtonAlignment);
        Assert.Equal (newBorderStyle, Dialog.DefaultBorderStyle);

        // Replace with the savedValue to avoid failures on other unit tests that rely on the default value
        ThemeManager.GetCurrentTheme () ["Dialog.DefaultButtonAlignment"].PropertyValue = savedButtonAlignment;
        ThemeManager.GetCurrentTheme () ["Dialog.DefaultBorderStyle"].PropertyValue = savedBorderStyle;
        ThemeManager.GetCurrentTheme ().Apply ();
        Assert.Equal (savedButtonAlignment, Dialog.DefaultButtonAlignment);
        Assert.Equal (savedBorderStyle, Dialog.DefaultBorderStyle);
        Disable (true);
    }

    [Fact]
    public void UpdateToHardCodedDefaults_Resets_Config_Does_Not_Apply ()
    {
        Enable (ConfigLocations.HardCoded);

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
        Enable (ConfigLocations.HardCoded);

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
        Enable (ConfigLocations.HardCoded);

        var theme = new ThemeScope ();
        theme.LoadHardCodedDefaults ();
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
