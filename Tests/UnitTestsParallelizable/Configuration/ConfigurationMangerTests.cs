#nullable enable
namespace Terminal.Gui.ConfigurationTests;

public class ConfigurationManagerTests
{
    [Fact]
    public void Disable_With_ResetToHardCodedDefaults_True_Works_When_Disabled ()
    {
        Assert.False (ConfigurationManager.IsEnabled);
        ConfigurationManager.Disable (true);
    }

    [ConfigurationProperty (Scope = typeof (CMTestsScope))]
    public static bool? TestProperty { get; set; }

    private class CMTestsScope : Scope<CMTestsScope>
    {
    }

    [Fact]
    public void GetConfigPropertiesByScope_Gets ()
    {
        var props = ConfigurationManager.GetConfigPropertiesByScope ("CMTestsScope");

        Assert.NotNull (props);
        Assert.NotEmpty (props);
    }
}
