#nullable enable
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Xunit.Abstractions;

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
        var props = ConfigurationManager.GetConfigPropertiesByScope (typeof (CMTestsScope));

        Assert.NotNull (props);
        Assert.NotEmpty (props);
    }
}
