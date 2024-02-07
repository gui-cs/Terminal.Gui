using System.Text.Json;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests; 

public class ThemeScopeTests {
    public static readonly JsonSerializerOptions _jsonOptions = new () {
                                                                           Converters = {
                                                                               //new AttributeJsonConverter (),
                                                                               //new ColorJsonConverter ()
                                                                           }
                                                                       };

    [Fact]
    public void AllThemesPresent () {
        Reset ();
        Assert.True (Themes.ContainsKey ("Default"));
        Assert.True (Themes.ContainsKey ("Dark"));
        Assert.True (Themes.ContainsKey ("Light"));
    }

    [Fact]
    [AutoInitShutdown]
    public void Apply_ShouldApplyUpdatedProperties () {
        Reset ();
        Assert.NotEmpty (Themes);
        Assert.Equal (Dialog.ButtonAlignments.Center, Dialog.DefaultButtonAlignment);

        Themes["Default"]["Dialog.DefaultButtonAlignment"].PropertyValue = Dialog.ButtonAlignments.Right;

        ThemeManager.Themes![ThemeManager.SelectedTheme]!.Apply ();
        Assert.Equal (Dialog.ButtonAlignments.Right, Dialog.DefaultButtonAlignment);
    }

    [Fact]
    public void GetHardCodedDefaults_ShouldSetProperties () {
        Reset ();
        GetHardCodedDefaults ();
        Assert.NotEmpty (Themes);
        Assert.Equal ("Default", Themes.Theme);
    }

    [Fact]
    public void TestSerialize_RoundTrip () {
        Reset ();

        Dictionary<string, ThemeScope> initial = ThemeManager.Themes;

        string serialized = JsonSerializer.Serialize<IDictionary<string, ThemeScope>> (Themes, _jsonOptions);
        IDictionary<string, ThemeScope> deserialized =
            JsonSerializer.Deserialize<IDictionary<string, ThemeScope>> (serialized, _jsonOptions);

        Assert.NotEqual (initial, deserialized);
        Assert.Equal (deserialized.Count, initial.Count);
    }

    [Fact]
    public void ThemeManager_ClassMethodsWork () {
        Reset ();
        Assert.Equal (ThemeManager.Instance, Themes);
        Assert.NotEmpty (ThemeManager.Themes);

        ThemeManager.SelectedTheme = "foo";
        Assert.Equal ("foo", ThemeManager.SelectedTheme);
        ThemeManager.Reset ();
        Assert.Equal (string.Empty, ThemeManager.SelectedTheme);

        Assert.Empty (ThemeManager.Themes);
    }
}
