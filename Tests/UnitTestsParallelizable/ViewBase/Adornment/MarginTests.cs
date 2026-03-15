using System.Text;
using UnitTests;

namespace ViewBaseTests.Adornments;

public class MarginTests (ITestOutputHelper output)
{
    [Fact]
    public void Margin_Is_Transparent ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (5, 5);

        var view = new View { Height = 3, Width = 3 };
        view.Margin!.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.Margin.Thickness = new Thickness (1);

        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        runnable.SetScheme (new Scheme { Normal = new Attribute (Color.Red, Color.Green), Focus = new Attribute (Color.Green, Color.Red) });

        runnable.Add (view);
        Assert.Equal (ColorName16.Red, view.Margin.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, runnable.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());

        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (@"", output, app.Driver);
        DriverAssert.AssertDriverAttributesAre ("0", output, app.Driver, runnable.GetAttributeForRole (VisualRole.Normal));
    }

    [Fact]
    public void Margin_ViewPortSettings_Not_Transparent_Is_NotTransparent ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver!.SetScreenSize (5, 5);

        var view = new View { Height = 3, Width = 3 };
        view.Margin!.Diagnostics = ViewDiagnosticFlags.Thickness;
        view.Margin.Thickness = new Thickness (1);
        view.Margin.ViewportSettings = ViewportSettingsFlags.None;

        Runnable<bool> runnable = new ();
        app.Begin (runnable);

        runnable.SetScheme (new Scheme { Normal = new Attribute (Color.Red, Color.Green), Focus = new Attribute (Color.Green, Color.Red) });

        runnable.Add (view);
        Assert.Equal (ColorName16.Red, view.Margin.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, runnable.GetAttributeForRole (VisualRole.Normal).Foreground.GetClosestNamedColor16 ());

        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre ("""

                                              MMM
                                              M M
                                              MMM
                                              """,
                                              output,
                                              app.Driver);
        DriverAssert.AssertDriverAttributesAre ("0", output, app.Driver, runnable.GetAttributeForRole (VisualRole.Normal));
    }

    [Fact]
    public void Is_Visually_Transparent ()
    {
        var view = new View { Height = 3, Width = 3 };
        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent), "Margin should be transparent by default.");
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

        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse),
                     "Margin should be transparent to mouse when ShadowStyle is Transparent.");

        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent),
                     "Margin should be transparent when ShadowStyle is Transparent..");
    }

    [Fact]
    public void Margin_Uses_ShadowStyle_Opaque ()
    {
        var view = new View { Height = 3, Width = 3, ShadowStyle = ShadowStyle.Opaque };
        Assert.Equal (ShadowStyle.Opaque, view.Margin!.ShadowStyle);

        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse),
                     "Margin should be transparent to mouse when ShadowStyle is Opaque.");
        Assert.True (view.Margin!.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent), "Margin should be transparent when ShadowStyle is Opaque..");
    }

    [Fact]
    public void Margin_Layouts_Correctly ()
    {
        View superview = new () { Width = 10, Height = 5 };
        View view = new () { Width = 3, Height = 1, BorderStyle = LineStyle.Single };
        view.Margin!.Thickness = new Thickness (1);
        View view2 = new () { X = Pos.Right (view), Width = 3, Height = 1, BorderStyle = LineStyle.Single };
        view2.Margin!.Thickness = new Thickness (1);
        View view3 = new () { Y = Pos.Bottom (view), Width = 3, Height = 1, BorderStyle = LineStyle.Single };
        view3.Margin!.Thickness = new Thickness (1);
        superview.Add (view, view2, view3);

        superview.LayoutSubViews ();

        Assert.Equal (new Rectangle (0, 0, 10, 5), superview.Frame);
        Assert.Equal (new Rectangle (0, 0, 3, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);
        Assert.Equal (new Rectangle (3, 0, 3, 1), view2.Frame);
        Assert.Equal (Rectangle.Empty, view2.Viewport);
        Assert.Equal (new Rectangle (0, 1, 3, 1), view3.Frame);
        Assert.Equal (Rectangle.Empty, view3.Viewport);
    }

    [Fact]
    public void ShadowStyle_Opaque_Draws_Correctly ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (3, 2);
        app.Driver!.Force16Colors = true;

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        // Add a button with a transparent shadow to the Padding adornment
        ShadowDemoView demoView = new ();

        window.Add (demoView);
        app.Begin (window);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[30m\x1b[107mA▖X▝▘X
                                           """,
                                           output,
                                           app.Driver);
    }

    [Fact]
    public void ShadowStyle_Transparent_Draws_Correctly ()
    {
        // Arrange
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);
        app.Driver?.SetScreenSize (3, 2);
        app.Driver!.Force16Colors = true;

        using Runnable window = new ();
        window.Width = Dim.Fill ();
        window.Height = Dim.Fill ();

        window.ClearingViewport += (_, args) =>
                                   {
                                       window.FillRect (args.NewViewport, new Rune ('X'));
                                       args.Cancel = true;
                                   };
        window.SetScheme (new Scheme (new Attribute (Color.Black, Color.White)));

        // Add a button with a transparent shadow to the Padding adornment
        ShadowDemoView demoView = new () { ShadowStyle = ShadowStyle.Transparent };

        window.Add (demoView);
        app.Begin (window);

        DriverAssert.AssertDriverOutputIs ("""
                                           \x1b[30m\x1b[107mA\x1b[90m\x1b[107mX\x1b[30m\x1b[107mXX\x1b[90m\x1b[107mX\x1b[30m\x1b[107mX
                                           """,
                                           output,
                                           app.Driver);
    }
}

internal sealed class ShadowDemoView : View
{
    public ShadowDemoView ()
    {
        Id = "shadowDemoView";
        Text = "A";
        ShadowStyle = ShadowStyle.Opaque;
        Width = Dim.Auto ();
        Height = Dim.Auto ();
    }
}
