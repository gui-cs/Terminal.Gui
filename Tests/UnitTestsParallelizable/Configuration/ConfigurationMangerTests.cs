#nullable enable
namespace Terminal.Gui.ConfigurationTests;

public class ConfigurationManagerTests
{
    [ConfigurationProperty (Scope = typeof (CMTestsScope))]
    public static bool? TestProperty { get; set; }

    private class CMTestsScope : Scope<CMTestsScope>
    {
    }

    [Fact]
    public void GetConfigPropertiesByScope_Gets ()
    {
        var props = ConfigurationManager.GetUninitializedConfigPropertiesByScope ("CMTestsScope");

        Assert.NotNull (props);
        Assert.NotEmpty (props);
    }
}
