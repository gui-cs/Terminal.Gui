using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class MarginTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeDriver]
    public void Margin_Is_Transparent ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (5, 5);

        var view = new View { Height = 3, Width = 3 };
        view.Margin!.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.Margin.Thickness = new (1);

        Application.Top = new Toplevel ();
        Application.TopLevels.Push (Gui.Application.Top);

        Application.Top.ColorScheme = new()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        };

        Application.Top.Add (view);
        Assert.Equal (ColorName16.Red, view.Margin.GetNormalColor ().Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, Application.Top.GetNormalColor ().Foreground.GetClosestNamedColor16 ());

        Application.Top.BeginInit ();
        Application.Top.EndInit ();
        Application.LayoutAndDraw();

        DriverAssert.AssertDriverContentsAre (
                                             @"
MMM
M M
MMM",
                                             output
                                            );
        DriverAssert.AssertDriverAttributesAre ("0", output, null, Application.Top.GetNormalColor ());

        Application.ResetState (true);
    }
}
