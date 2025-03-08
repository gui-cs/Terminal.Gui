#nullable enable
namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class ColorSchemeTests
{
    [Fact]
    public void GetHotNormalColor_ColorScheme ()
    {
        var view = new View { ColorScheme = Colors.ColorSchemes ["Base"] };

        Assert.Equal (view.ColorScheme!.HotNormal, view.GetHotNormalColor ());

        view.Enabled = false;
        Assert.Equal (view.ColorScheme.Disabled, view.GetHotNormalColor ());
        view.Dispose ();
    }

    [Fact]
    public void GetNormalColor_ColorScheme ()
    {
        var view = new View { ColorScheme = Colors.ColorSchemes ["Base"] };

        Assert.Equal (view.ColorScheme!.Normal, view.GetNormalColor ());

        view.Enabled = false;
        Assert.Equal (view.ColorScheme.Disabled, view.GetNormalColor ());
        view.Dispose ();
    }
}
