using UnitTests;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class PaddingTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeDriver]
    public void Padding_Uses_Parent_ColorScheme ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (5, 5);
        var view = new View { Height = 3, Width = 3 };
        view.Padding.Thickness = new (1);
        view.Padding.Diagnostics = ViewDiagnosticFlags.Thickness;

        view.ColorScheme = new()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        };

        Assert.Equal (ColorName16.Red, view.Padding.GetNormalColor ().Foreground.GetClosestNamedColor16 ());
        Assert.Equal (view.GetNormalColor (), view.Padding.GetNormalColor ());

        view.BeginInit ();
        view.EndInit ();
        view.Draw ();

        DriverAssert.AssertDriverContentsAre (
                                             @"
PPP
P P
PPP",
                                             output
                                            );
        DriverAssert.AssertDriverAttributesAre ("0", output, null, view.GetNormalColor ());
    }
}
