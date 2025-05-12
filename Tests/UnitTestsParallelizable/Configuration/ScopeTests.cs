#nullable enable
using System.Reflection;

namespace Terminal.Gui.ConfigurationTests;

public class ScopeTests
{
    [Fact]
    public void Constructor_Scope_Is_Empty ()
    {
        // Arrange

        // Act
        var scope = new Scope<object> ();

        // Assert
        Assert.NotNull (scope);
        Assert.Empty (scope);
        Assert.NotNull (CM._allConfigPropertiesCache);
    }

    // The property key will be "ScopeTests.BoolProperty"
    [ConfigurationProperty (Scope = typeof (ScopeTestsScope))]
    public static bool? BoolProperty { get; set; } = true;

    // The property key will be "ScopeTests.StringProperty"
    [ConfigurationProperty (Scope = typeof (ScopeTestsScope))]
    public static string? StringProperty { get; set; } // null

    // The property key will be "ScopeTests.KeyProperty"
    [ConfigurationProperty (Scope = typeof (ScopeTestsScope))]
    public static Key? KeyProperty { get; set; } = Key.A;

    // The property key will be "ScopeTests.DictionaryProperty"
    [ConfigurationProperty (Scope = typeof (ScopeTestsScope))]
    public static Dictionary<string, ConfigProperty> DictionaryProperty { get; set; }

    public class ScopeTestsScope : Scope<ScopeTestsScope>
    {
    }

    // The property key will be "ScopeTests.DictionaryItemProperty1"
    [ConfigurationProperty (Scope = typeof (ScopeTestsDictionaryItemScope))]
    public static string? DictionaryItemProperty1 { get; set; } // null

    // The property key will be "ScopeTests.DictionaryItemProperty2"
    [ConfigurationProperty (Scope = typeof (ScopeTestsDictionaryItemScope))]
    public static string? DictionaryItemProperty2 { get; set; } // null
    public class ScopeTestsDictionaryItemScope : Scope<ScopeTestsDictionaryItemScope>
    {
    }


    [Fact]
    public void TestScope_Constructor_Creates_Properties ()
    {
        // Arrange

        // Act
        var scope = new ScopeTestsScope ();

        var cache = CM.GetHardCodedConfigPropertyCache ();

        Assert.NotNull (cache);

        // Assert
        Assert.NotNull (scope);
        Assert.True (scope.ContainsKey ("ScopeTests.BoolProperty"));

        Assert.Equal (typeof (ScopeTests).GetProperty ("BoolProperty"), scope ["ScopeTests.BoolProperty"].PropertyInfo);
        Assert.Null (scope ["ScopeTests.BoolProperty"].PropertyValue);
        Assert.False (scope ["ScopeTests.BoolProperty"].HasValue);

        Assert.Equal (typeof (ScopeTests).GetProperty ("StringProperty"), scope ["ScopeTests.StringProperty"].PropertyInfo);
        Assert.Null (scope ["ScopeTests.StringProperty"].PropertyValue);
        Assert.False (scope ["ScopeTests.StringProperty"].HasValue);
    }

    [Fact]
    public void UpdateFrom_Unknown_Key_Adds ()
    {
        // Arrange
        var scope = new ScopeTestsScope ();
        var scopeWithAddedProperty = new ScopeTestsScope ();
        scopeWithAddedProperty.Add ("AddedProperty", new ConfigProperty ()
        {
            Immutable = false,
            PropertyInfo = scope ["ScopeTests.BoolProperty"].PropertyInfo, // cheat and reuse the same PropertyInfo
            PropertyValue = false
        });

        // Act
        scope.UpdateFrom (scopeWithAddedProperty);

        // Assert
        Assert.NotNull (scope);
        Assert.NotEmpty (scope);
        Assert.True (scope.ContainsKey ("AddedProperty"));
        Assert.Equal (false, scopeWithAddedProperty ["AddedProperty"].PropertyValue);
    }

    [Fact]
    public void UpdateFrom_HasValue_Updates ()
    {
        // Arrange
        ScopeTestsScope originalScope = new ScopeTestsScope ();
        originalScope.LoadHardCodedDefaults ();
        Assert.Equal (Key.A, originalScope ["ScopeTests.KeyProperty"].PropertyValue);

        ScopeTestsScope sourceScope = new ScopeTestsScope ();
        sourceScope ["ScopeTests.KeyProperty"].PropertyValue = Key.B;
        Assert.False (sourceScope ["ScopeTests.StringProperty"].HasValue);
        Assert.True (sourceScope ["ScopeTests.KeyProperty"].HasValue);

        // Act
        originalScope.UpdateFrom (sourceScope);

        // Assert
        Assert.True (originalScope ["ScopeTests.StringProperty"].HasValue);
        Assert.True (originalScope ["ScopeTests.KeyProperty"].HasValue);
        Assert.Equal (Key.B, originalScope ["ScopeTests.KeyProperty"].PropertyValue);
    }

    [Fact]
    public void UpdateFrom_HasValue_Dictionary_Updates ()
    {
        // Arrange
        ScopeTestsScope originalScope = new ScopeTestsScope ();
        originalScope.LoadHardCodedDefaults ();
        Assert.Null (originalScope ["ScopeTests.DictionaryProperty"].PropertyValue);

        // QUESTION: Should this be done automatically?
        originalScope ["ScopeTests.DictionaryProperty"].PropertyValue = new Dictionary<string, ConfigProperty> ();

        ScopeTestsScope sourceScope = new ScopeTestsScope ();
        sourceScope ["ScopeTests.DictionaryProperty"].PropertyValue = new Dictionary<string, ConfigProperty> ()
        {
            { "item1", ConfigProperty.GetAllConfigProperties () ["ScopeTests.DictionaryItemProperty1"] },
            { "item2", ConfigProperty.GetAllConfigProperties () ["ScopeTests.DictionaryItemProperty2"] }
        };

        Assert.False (sourceScope ["ScopeTests.KeyProperty"].HasValue);
        Assert.True (sourceScope ["ScopeTests.DictionaryProperty"].HasValue);

        Dictionary<string, ConfigProperty>? sourceDict = sourceScope ["ScopeTests.DictionaryProperty"].PropertyValue as Dictionary<string, ConfigProperty>;
        sourceDict! ["item1"].Immutable = false;
        sourceDict ["item2"].Immutable = false;
        Assert.NotNull (sourceDict);
        Assert.Equal (2, sourceDict!.Count);
        Assert.True (sourceDict.ContainsKey ("item1"));
        Assert.True (sourceDict.ContainsKey ("item2"));
        Assert.False (sourceDict ["item1"].HasValue);
        Assert.False (sourceDict ["item1"].Immutable);

        // Update the original scope with the source scope, which has no values
        originalScope.UpdateFrom (sourceScope);

        // Confirm original is unchanged
        Assert.NotNull (originalScope ["ScopeTests.DictionaryProperty"].PropertyValue);
        Dictionary<string, ConfigProperty>? destDict = originalScope ["ScopeTests.DictionaryProperty"].PropertyValue as Dictionary<string, ConfigProperty>;
        Assert.NotNull (destDict);
        Assert.Equal (0, destDict!.Count);
        Assert.False (destDict.ContainsKey ("item1"));
        Assert.False (destDict.ContainsKey ("item2"));

        // Confirm source is unchanged
        sourceDict ["item1"].PropertyValue = "hello";
        Assert.True (sourceDict ["item1"].HasValue);
        Assert.Equal ("hello", sourceDict ["item1"].PropertyValue);

        // Now update the original scope with the source scope again
        originalScope.UpdateFrom (sourceScope);

        // Confirm the original has been updated with only the values in source that have been set
        Assert.NotNull (originalScope ["ScopeTests.DictionaryProperty"].PropertyValue);
        destDict = originalScope ["ScopeTests.DictionaryProperty"].PropertyValue as Dictionary<string, ConfigProperty>;

        // 1 item (item1) should now be in the original scope
        Assert.Equal (1, destDict!.Count);
        Assert.True (destDict ["item1"].HasValue);
        Assert.Equal ("hello", destDict ["item1"].PropertyValue);

        originalScope.Apply ();

        // Verify apply worked
        Assert.Equal ("hello", DictionaryProperty ["item1"].PropertyValue);

        // The item property should not have had its value set
        Assert.Equal (null, DictionaryItemProperty1);

        DictionaryItemProperty1 = null;
    }

}
