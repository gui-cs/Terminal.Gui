#nullable enable
using Terminal.Gui.DrawingTests;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class SchemeTests
{
    [Theory]
    [CombinatorialData]
    public void GetAttributeForRole (VisualRole role)
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"]! };

        Assert.Equal (view.Scheme!.GetAttributeForRole (role), view.GetAttributeForRole (role));

        view.Dispose ();
    }


    [Fact]
    public void GetAttributeForRole_Normal ()
    {
        // Need to call Initialize to setup readonly statics
       // ConfigurationManager.Initialize();

        ThemeScope ts = new ThemeScope ();

        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"]! };

        Assert.Equal (view.Scheme!.Normal, view.GetAttributeForRole (VisualRole.Normal));

        view.Dispose ();
    }


    [Fact]
    public void GetAttributeForRole_Normal_DisabledView_Returns_Disabled ()
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"]! };

        Assert.Equal (view.Scheme!.Normal, view.GetAttributeForRole (VisualRole.Normal));

        view.Enabled = false;
        Assert.Equal (view.Scheme.Disabled, view.GetAttributeForRole (VisualRole.Disabled));
        view.Dispose ();
    }


    [Fact]
    public void GetHotNormalColor_Scheme ()
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes()? ["Base"]! };

        Assert.Equal (view.Scheme!.HotNormal, view.GetHotNormalColor ());

        view.Enabled = false;
        Assert.Equal (view.Scheme.Disabled, view.GetHotNormalColor ());
        view.Dispose ();
    }

    [Fact]
    public void GetNormalColor_Scheme ()
    {
        var view = new View { Scheme = SchemeManager.GetHardCodedSchemes ()? ["Base"]! };

        Assert.Equal (view.Scheme!.Normal, view.GetNormalColor ());

        view.Enabled = false;
        Assert.Equal (view.Scheme.Disabled, view.GetNormalColor ());
        view.Dispose ();
    }
}
