#nullable enable
using System.Diagnostics;
using System.Text.Json;
using UnitTests;

namespace Terminal.Gui.ConfigurationTests;

public class SchemeManagerTests
{
    [Fact]
    [AutoInitShutdown]
    public void GetCurrentSchemes_Gets_Current_Schemes ()
    {
        Scheme? expected = SchemeManager.GetHardCodedSchemes ()! ["Base"];
        Assert.NotNull (expected);
        Scheme? current = SchemeManager.GetCurrentSchemes ()! ["Base"];
        Assert.Equal(expected, current);

        string nextTheme = ThemeManager.Themes!.ElementAt (1).Key;
        Dictionary<string, Scheme>? nextSchemes = ThemeManager.Themes! [nextTheme] ["Schemes"].PropertyValue as Dictionary<string, Scheme>;
        expected = nextSchemes! ["Base"];
        Assert.NotNull (expected);

        ThemeManager.Theme = nextTheme;

        current = SchemeManager.GetCurrentSchemes ()! ["Base"];
        Assert.Equal (expected, current);

    }

    [Fact]
    [AutoInitShutdown]
    public void GetDefautSchemes_Gets_Default_Theme_Schemes ()
    {
        Dictionary<string, Scheme?>? hardCoded = SchemeManager.GetHardCodedSchemes ();

        Dictionary<string, Scheme?>? current = SchemeManager.GetDefaultSchemes();

        Assert.Equal (hardCoded! ["Base"], current! ["Base"]);
    }

    [Fact]
    [AutoInitShutdown]
    public void Not_Case_Sensitive ()
    {
        Dictionary<string, Scheme?>? current = SchemeManager.GetDefaultSchemes ();
        Assert.NotNull (current);

        Assert.True (current!.ContainsKey ("Base"));
        Assert.True (current.ContainsKey ("base"));
    }

}
