#nullable enable
namespace Terminal.Gui.ConfigurationTests;

public class ScopeTests
{
    [Fact]
    public void Constructor_Initializes_CM ()
    {
        // Arrange

        // Act
        var scope = new Scope<object> ();

        // Assert
        Assert.NotNull (scope);
        Assert.Empty (scope);
        Assert.NotNull (CM._allConfigPropertiesCache);
    }

    [SerializableConfigurationProperty (Scope = typeof (ScopeTestsScope))]
    private bool? TestProperty { get; set; }

    private class ScopeTestsScope : Scope<ScopeTestsScope>
    {
    }

    [Fact]
    public void TestScope_Constructor_Creates_Properties ()
    {
        // Arrange

        // Act
        var scope = new ScopeTestsScope ();

        // Assert
        Assert.NotNull (scope);
        Assert.Empty (scope);
    }
}
