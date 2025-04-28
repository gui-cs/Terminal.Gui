#nullable enable
namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class SchemeTests
{
    [Fact]
    public void GetHotNormalColor_Scheme ()
    {
        var view = new View { Scheme = Colors.Schemes ["Base"] };

        Assert.Equal (view.Scheme!.HotNormal, view.GetHotNormalColor ());

        view.Enabled = false;
        Assert.Equal (view.Scheme.Disabled, view.GetHotNormalColor ());
        view.Dispose ();
    }

    [Fact]
    public void GetNormalColor_Scheme ()
    {
        var view = new View { Scheme = Colors.Schemes ["Base"] };

        Assert.Equal (view.Scheme!.Normal, view.GetNormalColor ());

        view.Enabled = false;
        Assert.Equal (view.Scheme.Disabled, view.GetNormalColor ());
        view.Dispose ();
    }
}
