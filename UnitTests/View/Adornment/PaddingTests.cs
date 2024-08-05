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

        view.ColorScheme = new()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        };

        Assert.Equal (ColorName.Red, view.Padding.GetNormalColor ().Foreground.GetClosestNamedColor ());
        Assert.Equal (view.GetNormalColor (), view.Padding.GetNormalColor ());

        view.BeginInit ();
        view.EndInit ();
        View.Diagnostics = ViewDiagnosticFlags.Padding;
        view.Draw ();
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsAre (
                                             @"
PPP
P P
PPP",
                                             output
                                            );
        TestHelpers.AssertDriverAttributesAre ("0", null, view.GetNormalColor ());
    }
}
