#nullable enable
using System.Text.Json;
using UnitTests;
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
    [AutoInitShutdown (configLocation: ConfigLocations.LibraryResources)]
    public void Apply_ShouldApplyUpdatedProperties ()
    {
        Reset ();
        Assert.Null (AppSettingsTestClass.NullableValueProperty);
        Assert.NotEmpty (AppSettings!);
        Assert.Null (AppSettings! ["AppSettingsTestClass.NullableValueProperty"].PropertyValue);

        AppSettingsTestClass.NullableValueProperty = true;
        Reset ();
        Assert.True (AppSettingsTestClass.NullableValueProperty);
        Assert.NotEmpty (AppSettings);
        Assert.Null (AppSettings ["AppSettingsTestClass.NullableValueProperty"].PropertyValue as bool?);

        AppSettings ["AppSettingsTestClass.NullableValueProperty"].PropertyValue = false;
        Assert.False (AppSettings ["AppSettingsTestClass.NullableValueProperty"].PropertyValue as bool?);

        // ConfigurationManager.Settings should NOT apply theme settings
        Settings!.Apply ();
        Assert.True (AppSettingsTestClass.NullableValueProperty);

        // ConfigurationManager.Themes should NOT apply theme settings
        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();
        Assert.True (AppSettingsTestClass.NullableValueProperty);

        // ConfigurationManager.AppSettings should NOT apply theme settings
        AppSettings.Apply ();
        Assert.False (AppSettingsTestClass.NullableValueProperty);
    }

    [Fact]
    public void TestNullable ()
    {
        AppSettingsTestClass.NullableValueProperty = null;
        Assert.Null (AppSettingsTestClass.NullableValueProperty);

        ResetToCurrentValues ();
        Apply ();
        Assert.Null (AppSettingsTestClass.NullableValueProperty);

        AppSettingsTestClass.NullableValueProperty = true;
        ResetToCurrentValues ();
        Assert.NotNull (AppSettingsTestClass.NullableValueProperty);
        Apply ();
        Assert.NotNull (AppSettingsTestClass.NullableValueProperty);
    }

    [Fact]
    public void TestSerialize_RoundTrip ()
    {
        Reset ();

        AppScope initial = AppSettings!;

        string serialized = JsonSerializer.Serialize (AppSettings, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<AppScope> (serialized, _jsonOptions);

        Assert.NotEqual (initial, deserialized);
        Assert.Equal (deserialized!.Count, initial.Count);
    }

    public class AppSettingsTestClass
    {
        [SerializableConfigurationProperty (Scope = typeof (AppScope))]
        public static bool ValueProperty { get; set; }

        [SerializableConfigurationProperty (Scope = typeof (AppScope))]
        public static bool? NullableValueProperty { get; set; }

        [SerializableConfigurationProperty (Scope = typeof (AppScope))]
        public static string ReferenceProperty { get; set; } = "test";

        [SerializableConfigurationProperty (Scope = typeof (AppScope))]
        public static string? NullableReferenceProperty { get; set; }
    }
}
