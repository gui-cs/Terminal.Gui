#nullable enable
using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.ViewTests;

[Trait ("Category", "Output")]
public class TransparentTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeDriver]

    public void Transparent_Text_Occludes ()
    {
        var super = new View
        {
            Id = "super",
            Width = 20,
            Height = 5,
        };
        super.DrawingContent += (sender, args) =>
                                {
                                    var s = sender as View;
                                    s!.FillRect(s!.Viewport, Glyphs.Stipple);
                                    args.Cancel = true;
                                };

        var sub = new View
        {
            X = 1,
            Y = 1,
            Width = 15,
            Height = 3,
            Id = "sub",
            Text = "Sub",
            ViewportSettings = ViewportSettingsFlags.Transparent,
            BorderStyle = LineStyle.Single
        };

        super.Add (sub);

        super.Layout ();
        super.Draw ();

        _ = DriverAssert.AssertDriverContentsWithFrameAre (
                                                        @"
░░░░░░░░░░░░░░░░░░░░
░┌─────────────┐░░░░
░│Sub░░░░░░░░░░│░░░░
░└─────────────┘░░░░
░░░░░░░░░░░░░░░░░░░░", output);
    }

    [Fact]
    [SetupFakeDriver]

    public void Transparent_SubView_Occludes ()
    {
        var super = new View
        {
            Id = "super",
            Width = 20,
            Height = 5,
        };
        super.DrawingContent += (sender, args) =>
                                {
                                    var s = sender as View;
                                    s!.FillRect (s!.Viewport, Glyphs.Stipple);
                                    args.Cancel = true;
                                };

        var sub = new View
        {
            X = 1,
            Y = 1,
            Width = 15,
            Height = 3,
            Id = "sub",
            ViewportSettings = ViewportSettingsFlags.Transparent,
            BorderStyle = LineStyle.Single
        };

        var subSub = new View
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = Dim.Auto(),
            Height = Dim.Auto(),
            Id = "subSub",
            Text = "subSub",
        };
        sub.Add (subSub);

        super.Add (sub);

        super.Layout ();
        super.Draw ();

        _ = DriverAssert.AssertDriverContentsWithFrameAre (
                                                          @"
░░░░░░░░░░░░░░░░░░░░
░┌─────────────┐░░░░
░│░░░subSub░░░░│░░░░
░└─────────────┘░░░░
░░░░░░░░░░░░░░░░░░░░", output);
    }
}
