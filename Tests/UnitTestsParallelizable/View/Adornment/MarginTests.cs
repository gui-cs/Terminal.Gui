#nullable enable
using UnitTests;
using Xunit.Abstractions;

namespace UnitTests_Parallelizable.ViewTests;

public class MarginTests (ITestOutputHelper output)
{
    [Fact]
    public void Margin_Is_Transparent ()
    {
        IApplication? app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (5, 5);

        var view = new View { Height = 3, Width = 3 };
        view.Margin!.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.Margin.Thickness = new (1);

        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        runnable.SetScheme (new ()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        });

        runnable.Add (view);
        Assert.Equal (ColorName16.Red, view.Margin.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, runnable.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());

        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                             @"",
                                             output,
                                             app.Driver
                                            );
        DriverAssert.AssertDriverAttributesAre ("0", output, app.Driver, runnable.GetAttributeForRole (VisualRole.Normal));
    }

    [Fact]
    public void Margin_ViewPortSettings_Not_Transparent_Is_NotTransparent ()
    {
        IApplication? app = Application.Create ();
        app.Init ("fake");
        app.Driver!.SetScreenSize (5, 5);

        var view = new View { Height = 3, Width = 3 };
        view.Margin!.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.Margin.Thickness = new (1);
        view.Margin.ViewportSettings = ViewportSettingsFlags.None;

        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        runnable.SetScheme (new ()
        {
            Normal = new (Color.Red, Color.Green), Focus = new (Color.Green, Color.Red)
        });

        runnable.Add (view);
        Assert.Equal (ColorName16.Red, view.Margin.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, runnable.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());

        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              @"
MMM
M M
MMM",
                                              output,
                                              app.Driver
                                             );
        DriverAssert.AssertDriverAttributesAre ("0", output, app.Driver, runnable.GetAttributeForRole (VisualRole.Normal));
    }
    [Fact]
    public void Is_Visually_Transparent ()
    {
        var view = new View { Height = 3, Width = 3 };
        Assert.True(view.Margin!.ViewportSettings.HasFlag(ViewportSettingsFlags.Transparent), "Margin should be transparent by default.");
    }

    [Fact]
    public void Is_Transparent_To_Mouse ()
    {
        var view = new View { Height = 3, Width = 3 };
        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse), "Margin should be transparent to mouse by default.");
    }

    [Fact]
    public void When_Not_Visually_Transparent ()
    {
        var view = new View { Height = 3, Width = 3 };

        // Give the Margin some size
        view.Margin!.Thickness = new Thickness (1, 1, 1, 1);

        // Give it Text
        view.Margin!.Text = "Test";

        // Strip off ViewportSettings.Transparent
        view.Margin!.ViewportSettings &= ~ViewportSettingsFlags.Transparent;

        // 

    }

    [Fact]
    public void Thickness_Is_Empty_By_Default ()
    {
        var view = new View { Height = 3, Width = 3 };
        Assert.Equal (Thickness.Empty, view.Margin!.Thickness);
    }

    // ShadowStyle
    [Fact]
    public void Margin_Uses_ShadowStyle_Transparent ()
    {
        var view = new View { Height = 3, Width = 3, ShadowStyle = ShadowStyle.Transparent };
        Assert.Equal (ShadowStyle.Transparent, view.Margin!.ShadowStyle);
        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse), "Margin should be transparent to mouse when ShadowStyle is Transparent.");
        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent), "Margin should be transparent when ShadowStyle is Transparent..");
    }

    [Fact]
    public void Margin_Uses_ShadowStyle_Opaque ()
    {
        var view = new View { Height = 3, Width = 3, ShadowStyle = ShadowStyle.Opaque };
        Assert.Equal (ShadowStyle.Opaque, view.Margin!.ShadowStyle);
        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse), "Margin should be transparent to mouse when ShadowStyle is Opaque.");
        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent), "Margin should be transparent when ShadowStyle is Opaque..");
    }

}
