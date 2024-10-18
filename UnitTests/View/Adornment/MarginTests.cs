﻿using Xunit.Abstractions;

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
        Assert.Equal (ColorName16.Red, view.Margin.GetNormalColor ().Foreground.GetClosestNamedColor16 ());
        Assert.Equal (ColorName16.Red, superView.GetNormalColor ().Foreground.GetClosestNamedColor16 ());
        Assert.Equal (superView.GetNormalColor (), view.Margin.GetNormalColor ());
        Assert.Equal (superView.GetFocusColor (), view.Margin.GetFocusColor ());

        superView.BeginInit ();
        superView.EndInit ();
        View.Diagnostics = ViewDiagnosticFlags.Padding;
        view.SetNeedsDisplay();
        view.Draw ();
        View.Diagnostics = ViewDiagnosticFlags.Off;

        TestHelpers.AssertDriverContentsAre (
                                             @"
MMM
M M
MMM",
                                             output
                                            );
        TestHelpers.AssertDriverAttributesAre ("0", output, null, superView.GetNormalColor ());
    }
}
