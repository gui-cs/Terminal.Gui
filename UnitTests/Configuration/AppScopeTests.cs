using System.Text.Json;
using static Terminal.Gui.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class AppScopeTests
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
    [AutoInitShutdown (configLocation: ConfigLocations.DefaultOnly)]
    public void Apply_ShouldApplyUpdatedProperties ()
    {
        Reset ();
        Assert.Null (AppSettingsTestClass.TestProperty);
        Assert.NotEmpty (AppSettings);
        Assert.Null (AppSettings ["AppSettingsTestClass.TestProperty"].PropertyValue);

        AppSettingsTestClass.TestProperty = true;
        Reset ();
        Assert.True (AppSettingsTestClass.TestProperty);
        Assert.NotEmpty (AppSettings);
        Assert.Null (AppSettings ["AppSettingsTestClass.TestProperty"].PropertyValue as bool?);

        AppSettings ["AppSettingsTestClass.TestProperty"].PropertyValue = false;
        Assert.False (AppSettings ["AppSettingsTestClass.TestProperty"].PropertyValue as bool?);

        // ConfigurationManager.Settings should NOT apply theme settings
        Settings.Apply ();
        Assert.True (AppSettingsTestClass.TestProperty);

        // ConfigurationManager.Themes should NOT apply theme settings
        ThemeManager.Themes! [ThemeManager.SelectedTheme]!.Apply ();
        Assert.True (AppSettingsTestClass.TestProperty);

        // ConfigurationManager.AppSettings should NOT apply theme settings
        AppSettings.Apply ();
        Assert.False (AppSettingsTestClass.TestProperty);
    }

    [Fact]
    public void TestNullable ()
    {
        AppSettingsTestClass.TestProperty = null;
        Assert.Null (AppSettingsTestClass.TestProperty);

        Initialize ();
        GetHardCodedDefaults ();
        Apply ();
        Assert.Null (AppSettingsTestClass.TestProperty);

        AppSettingsTestClass.TestProperty = true;
        Initialize ();
        GetHardCodedDefaults ();
        Assert.NotNull (AppSettingsTestClass.TestProperty);
        Apply ();
        Assert.NotNull (AppSettingsTestClass.TestProperty);
    }

    [Fact]
    public void TestSerialize_RoundTrip ()
    {
        Reset ();

        AppScope initial = AppSettings;

        string serialized = JsonSerializer.Serialize (AppSettings, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppScope> (serialized, _jsonOptions);

        Assert.NotEqual (initial, deserialized);
        Assert.Equal (deserialized.Count, initial.Count);
    }

    public class AppSettingsTestClass
    {
        [SerializableConfigurationProperty (Scope = typeof (AppScope))]
        public static bool? TestProperty { get; set; }
    }
}
