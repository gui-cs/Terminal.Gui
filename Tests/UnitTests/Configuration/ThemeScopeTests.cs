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
    [AutoInitShutdown (configLocation: ConfigLocations.Default)]
    public void AllThemesPresent ()
    {
        ResetAllSettings ();
        Assert.True (ConfigurationManager.ThemeManager.ContainsKey ("Default"));
        Assert.True (ConfigurationManager.ThemeManager.ContainsKey ("Dark"));
        Assert.True (ConfigurationManager.ThemeManager.ContainsKey ("Light"));
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigLocations.Default)]
    public void Apply_ShouldApplyUpdatedProperties ()
    {
        ResetAllSettings ();
        Assert.NotEmpty (ConfigurationManager.ThemeManager);
        Alignment savedValue = Dialog.DefaultButtonAlignment;
        Alignment newValue = Alignment.Center != savedValue ? Alignment.Center : Alignment.Start;

        ConfigurationManager.ThemeManager ["Default"] ["Dialog.DefaultButtonAlignment"].PropertyValue = newValue;

        CM.ThemeManager! [ThemeManager.SelectedTheme]!.Apply ();
        Assert.Equal (newValue, Dialog.DefaultButtonAlignment);

        // Replace with the savedValue to avoid failures on other unit tests that rely on the default value
        ConfigurationManager.ThemeManager ["Default"] ["Dialog.DefaultButtonAlignment"].PropertyValue = savedValue;
        CM.ThemeManager! [ThemeManager.SelectedTheme]!.Apply ();
        Assert.Equal (savedValue, Dialog.DefaultButtonAlignment);
    }

    [Fact]
    public void GetHardCodedDefaults_ShouldSetProperties ()
    {
        ResetAllSettings ();
        ResetToCurrentValues ();
        Assert.NotEmpty (ConfigurationManager.ThemeManager);
        Assert.Equal ("Default", ConfigurationManager.ThemeManager.Theme);
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigLocations.Default)]
    public void TestSerialize_RoundTrip ()
    {
        ResetAllSettings ();

        Dictionary<string, ThemeScope> initial = ConfigurationManager.ThemeManager!.Themes;

        string serialized = JsonSerializer.Serialize<IDictionary<string, ThemeScope>> (ConfigurationManager.ThemeManager, _jsonOptions);

        IDictionary<string, ThemeScope> deserialized =
            JsonSerializer.Deserialize<IDictionary<string, ThemeScope>> (serialized, _jsonOptions);

        Assert.NotEqual (initial, deserialized);
        Assert.Equal (deserialized.Count, initial.Count);
    }

    [Fact]
    [AutoInitShutdown (configLocation: ConfigLocations.Default)]
    public void ThemeManager_ClassMethodsWork ()
    {
        ResetAllSettings ();
        Assert.Equal (ConfigurationManager.ThemeManager!, ConfigurationManager.ThemeManager);
        Assert.NotEmpty (ConfigurationManager.ThemeManager!);

        ThemeManager.SelectedTheme = "foo";
        Assert.Equal ("foo", ThemeManager.SelectedTheme);
        CM.ThemeManager.Clear ();
        Assert.Equal (string.Empty, ThemeManager.SelectedTheme);

        Assert.Empty (ConfigurationManager.ThemeManager!);
    }
}
