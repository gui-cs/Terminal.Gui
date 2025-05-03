#nullable enable
using System.Diagnostics;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class SchemeManagerTests
{

    [Fact]
    public void GetHardCodedSchemes_Gets_HardCodedDefaults ()
    {
        var hardCoded = SchemeManager.GetHardCodedSchemes ();

        // Check that the hardcoded schemes are not null
        Assert.NotNull (hardCoded);
        // Check that the hardcoded schemes are not empty
        Assert.NotEmpty (hardCoded);
    }

    [Fact]
    public void GetCurrentSchemes_Gets_Current_Schemes ()
    {
        
    }

    [Fact]
    public void Schemes_Set ()
    {

    }

    [Fact]
    public void Schemes_Get ()
    {

    }
}
