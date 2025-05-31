#nullable enable
using System.Text.Json;
using UnitTests;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class AppSettingsScopeTests
{
    [Fact]
    public void Empty_By_Default_Disabled ()
    {
        Assert.False (IsEnabled);

        Assert.NotNull (Settings! ["AppSettings"].PropertyValue);

        AppSettingsScope? appSettings = (Settings! ["AppSettings"].PropertyValue as AppSettingsScope);
        Assert.Equal (10, appSettings!.Count); // 10 test properties

        Assert.Equal ("test", (((AppSettingsScope)Settings! ["AppSettings"].PropertyValue!)!) ["AppSettingsTestClass.ReferenceProperty"].PropertyValue);
    }

    [Fact]
    public void Empty_By_Default_Enabled ()
    {
        Enable (ConfigLocations.HardCoded);

        Assert.NotNull (Settings! ["AppSettings"].PropertyValue);

        AppSettingsScope? appSettings = (Settings! ["AppSettings"].PropertyValue as AppSettingsScope);
        Assert.Equal (10, appSettings!.Count); // 10 test properties
        Assert.Equal ("test", (((AppSettingsScope)Settings! ["AppSettings"].PropertyValue!)!) ["AppSettingsTestClass.ReferenceProperty"].PropertyValue);

        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void Apply_ShouldApplyUpdatedProperties ()
    {
        Enable (ConfigLocations.HardCoded);

        Assert.Null (AppSettingsTestClass.NullableValueProperty);
        Assert.NotEmpty (AppSettings!);
        Assert.Null (AppSettings! ["AppSettingsTestClass.NullableValueProperty"].PropertyValue);

        AppSettingsTestClass.NullableValueProperty = true;
        ResetToCurrentValues ();
        Assert.True (AppSettingsTestClass.NullableValueProperty);
        Assert.NotEmpty (AppSettings);
        Assert.True (AppSettings ["AppSettingsTestClass.NullableValueProperty"].PropertyValue as bool?);

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
        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void TestNullable ()
    {
        Enable (ConfigLocations.HardCoded);

        AppSettingsTestClass.NullableValueProperty = null;
        Assert.Null (AppSettingsTestClass.NullableValueProperty);

        ResetToCurrentValues ();
        Assert.Null (AppSettings! ["AppSettingsTestClass.NullableValueProperty"].PropertyValue);

        Apply ();
        Assert.Null (AppSettings! ["AppSettingsTestClass.NullableValueProperty"].PropertyValue);
        Assert.Null (AppSettingsTestClass.NullableValueProperty);

        AppSettingsTestClass.NullableValueProperty = true;
        ResetToCurrentValues ();
        Assert.True ((bool)AppSettings! ["AppSettingsTestClass.NullableValueProperty"].PropertyValue!);
        Assert.True (AppSettingsTestClass.NullableValueProperty);
        Assert.NotNull (AppSettingsTestClass.NullableValueProperty);
        Apply ();
        Assert.True (AppSettingsTestClass.NullableValueProperty);
        Assert.NotNull (AppSettingsTestClass.NullableValueProperty);

        Disable (resetToHardCodedDefaults: true);
    }

    [Fact]
    public void TestSerialize_RoundTrip ()
    {
        Enable (ConfigLocations.HardCoded);

        AppSettingsScope initial = AppSettings!;

        string serialized = JsonSerializer.Serialize (AppSettings, SerializerContext.Options);
        var deserialized = JsonSerializer.Deserialize<AppSettingsScope> (serialized, SerializerContext.Options);

        Assert.NotEqual (initial, deserialized);
        Assert.Equal (deserialized!.Count, initial.Count);

        Disable (resetToHardCodedDefaults: true);
    }

    public class AppSettingsTestClass
    {
        [ConfigurationProperty (Scope = typeof (AppSettingsScope))]
        public static bool ValueProperty { get; set; }

        [ConfigurationProperty (Scope = typeof (AppSettingsScope))]
        public static bool? NullableValueProperty { get; set; }

        [ConfigurationProperty (Scope = typeof (AppSettingsScope))]
        public static string ReferenceProperty { get; set; } = "test";

        [ConfigurationProperty (Scope = typeof (AppSettingsScope))]
        public static string? NullableReferenceProperty { get; set; }
    }
}
