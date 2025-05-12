#nullable enable
using System.Diagnostics;
using System.Text.Json;
using UnitTests;

namespace Terminal.Gui.ConfigurationTests;

public class SchemeManagerTests
{
    [Fact]
    public void GetCurrentSchemes_Not_Enabled_Gets_Schemes ()
    {
        CM.Disable ();

        Dictionary<string, Scheme?>? schemes = SchemeManager.GetCurrentSchemes ();
        Assert.NotNull (schemes);
        Assert.NotNull (schemes ["Base"]);
    }

    [Fact]
    public void GetCurrentSchemes_Enabled_Gets_Current ()
    {
        CM.Enable();

        Dictionary<string, Scheme?>? schemes = SchemeManager.GetCurrentSchemes ();
        Assert.NotNull (schemes);
        Assert.NotNull (schemes ["Base"]);

        CM.Disable();
    }

    [Fact]
    public void GetHardCodedSchemes_Gets_HardCoded_Theme_Schemes ()
    {
        Dictionary<string, Scheme?>? hardCoded = SchemeManager.GetHardCodedSchemes ();

        Assert.Equal (View.GetHardCodedSchemes (), hardCoded);

    }

    [Fact]
    public void Not_Case_Sensitive ()
    {
        Dictionary<string, Scheme?>? current = SchemeManager.GetCurrentSchemes ();
        Assert.NotNull (current);

        Assert.True (current!.ContainsKey ("Base"));
        Assert.True (current.ContainsKey ("base"));
    }

}
