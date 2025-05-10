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

    // The property key will be "ScopeTests.ScopeTestProperty"
    [ConfigurationProperty (Scope = typeof (ScopeTestsScope))]
    static public bool? ScopeTestProperty { get; set; } = true;

    public class ScopeTestsScope : Scope<ScopeTestsScope>
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
        Assert.True (scope.ContainsKey ("ScopeTests.ScopeTestProperty"));

        // Get the PropertyInfo of ScopeTestProperty
        var typeInfo = typeof (ScopeTests).GetProperty ("ScopeTestProperty");

        Assert.Equal (typeInfo, scope ["ScopeTests.ScopeTestProperty"].PropertyInfo);
        Assert.Null (scope ["ScopeTests.ScopeTestProperty"].PropertyValue);
        Assert.False (scope ["ScopeTests.ScopeTestProperty"].HasValue);
    }

    [Fact]
    public void Update_Unknown_Key_Adds ()
    {
        // Arrange
        var scope = new ScopeTestsScope ();
        var scopeWithAddedProperty = new ScopeTestsScope ();
        scopeWithAddedProperty.Add ("AddedProperty", new ConfigProperty ()
        {
            Immutable = false,
            PropertyInfo = scope ["ScopeTests.ScopeTestProperty"].PropertyInfo, // cheat and reuse the same PropertyInfo
            PropertyValue = false
        });

        // Act
        scope.DeepCloneFrom (scopeWithAddedProperty);

        // Assert
        Assert.NotNull (scope);
        Assert.NotEmpty (scope);
        Assert.True (scope.ContainsKey ("AddedProperty"));
        Assert.Equal (false, scopeWithAddedProperty ["AddedProperty"].PropertyValue);
    }
}
