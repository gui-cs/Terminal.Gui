using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class MarginTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeDriver]
    public void Margin_Uses_SuperView_ColorScheme ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (5, 5);
        var view = new View { Height = 3, Width = 3 };
        view.Margin.Thickness = new (1);

        var superView = new View ();

        superView.ColorScheme = new()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        };

        superView.Add (view);
        Assert.Equal (ColorName.Red, view.Margin.GetNormalColor ().Foreground.GetClosestNamedColor ());
        Assert.Equal (ColorName.Red, superView.GetNormalColor ().Foreground.GetClosestNamedColor ());
        Assert.Equal (superView.GetNormalColor (), view.Margin.GetNormalColor ());
        Assert.Equal (superView.GetFocusColor (), view.Margin.GetFocusColor ());

        superView.BeginInit ();
        superView.EndInit ();
        View.Diagnostics = ViewDiagnosticFlags.Padding;
        view.Draw ();
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsAre (
                                             @"
MMM
M M
MMM",
                                             output
                                            );
        TestHelpers.AssertDriverAttributesAre ("0", null, superView.GetNormalColor ());
    }
}
