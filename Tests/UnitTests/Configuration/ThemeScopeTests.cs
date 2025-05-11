using System.Text.Json;
using UnitTests;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class ThemeScopeTests
{
    public static readonly JsonSerializerOptions _jsonOptions = new ()
    {
        Converters =
        {
            //new AttributeJsonConverter (),
            //new ColorJsonConverter ()
        }
    };

    [Fact]
    [AutoInitShutdown]
    public void AllThemesPresent ()
    {
        Reset ();
        Assert.True (ThemeManager.Themes.ContainsKey ("Default"));
        Assert.True (ThemeManager.Themes.ContainsKey ("Dark"));
        Assert.True (ThemeManager.Themes.ContainsKey ("Light"));
    }

    [Fact]
    [AutoInitShutdown]
    public void Apply_ShouldApplyUpdatedProperties ()
    {
        Reset ();
        Assert.NotEmpty (ThemeManager.Themes);
        Alignment savedValue = Dialog.DefaultButtonAlignment;
        Alignment newValue = Alignment.Center != savedValue ? Alignment.Center : Alignment.Start;

        ThemeManager.Themes ["Default"] ["Dialog.DefaultButtonAlignment"].PropertyValue = newValue;

        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();
        Assert.Equal (newValue, Dialog.DefaultButtonAlignment);

        // Replace with the savedValue to avoid failures on other unit tests that rely on the default value
        ThemeManager.Themes ["Default"] ["Dialog.DefaultButtonAlignment"].PropertyValue = savedValue;
        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();
        Assert.Equal (savedValue, Dialog.DefaultButtonAlignment);
    }

    [Fact]
    public void UpdateToHardCodedDefaults_Resets_Config_Does_Not_Apply ()
    {
        Enable ();
        Load (ConfigLocations.LibraryResources);

        Assert.Equal ("Default", ThemeManager.Theme);
        ThemeManager.Theme = "Dark";
        Assert.Equal ("Default", ThemeManager.Theme);
        Apply ();
        Assert.Equal ("Dark", ThemeManager.Theme);

        // Act
        ThemeManager.UpdateToHardCodedDefaults ();
        Assert.Equal ("Default", ThemeManager.Theme);

        Disable ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TestSerialize_RoundTrip ()
    {
        Reset ();

        IDictionary<string, ThemeScope> initial = ThemeManager.Themes;

        string serialized = JsonSerializer.Serialize<IDictionary<string, ThemeScope>> (ThemeManager.Themes, _jsonOptions);

        IDictionary<string, ThemeScope> deserialized =
            JsonSerializer.Deserialize<IDictionary<string, ThemeScope>> (serialized, _jsonOptions);

        Assert.NotEqual (initial, deserialized);
        Assert.Equal (deserialized.Count, initial.Count);
    }

}
