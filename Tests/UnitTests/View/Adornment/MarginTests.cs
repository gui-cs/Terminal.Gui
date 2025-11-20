using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.ViewTests;

public class MarginTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeApplication]
    public void Margin_Is_Transparent ()
    {
        Application.Driver!.SetScreenSize (5, 5);

        var view = new View { Height = 3, Width = 3 };
        view.Margin!.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.Margin.Thickness = new (1);

        Application.TopRunnable = new Toplevel ();
        Application.SessionStack.Push (Application.TopRunnable);

        Application.TopRunnable.SetScheme (new()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        });

        Application.TopRunnable.Add (view);
        Assert.Equal (ColorName16.Red, view.Margin.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, Application.TopRunnable.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());

        Application.TopRunnable.BeginInit ();
        Application.TopRunnable.EndInit ();
        Application.LayoutAndDraw();

        DriverAssert.AssertDriverContentsAre (
                                             @"",
                                             output
                                            );
        DriverAssert.AssertDriverAttributesAre ("0", output, null, Application.TopRunnable.GetAttributeForRole (VisualRole.Normal));

        Application.ResetState (true);
    }

    [Fact]
    [SetupFakeApplication]
    public void Margin_ViewPortSettings_Not_Transparent_Is_NotTransparent ()
    {
        Application.Driver!.SetScreenSize (5, 5);

        var view = new View { Height = 3, Width = 3 };
        view.Margin!.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.Margin.Thickness = new (1);
        view.Margin.ViewportSettings = ViewportSettingsFlags.None;

        Application.TopRunnable = new Toplevel ();
        Application.SessionStack.Push (Application.TopRunnable);

        Application.TopRunnable.SetScheme (new ()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        });

        Application.TopRunnable.Add (view);
        Assert.Equal (ColorName16.Red, view.Margin.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, Application.TopRunnable.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());

        Application.TopRunnable.BeginInit ();
        Application.TopRunnable.EndInit ();
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              @"
MMM
M M
MMM",
                                              output
                                             );
        DriverAssert.AssertDriverAttributesAre ("0", output, null, Application.TopRunnable.GetAttributeForRole (VisualRole.Normal));

        Application.ResetState (true);
    }
}
