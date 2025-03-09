using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class AdornmentTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeDriver]
    public void Border_Is_Cleared_After_Margin_Thickness_Change ()
    {
        View view = new () { Text = "View", Width = 6, Height = 3, BorderStyle = LineStyle.Rounded };

        // Remove border bottom thickness
        view.Border!.Thickness = new (1, 1, 1, 0);

        // Add margin bottom thickness
        view.Margin!.Thickness = new (0, 0, 0, 1);

        Assert.Equal (6, view.Width);
        Assert.Equal (3, view.Height);

        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
╭────╮
│View│
",
                                                       output
                                                      );

        // Add border bottom thickness
        view.Border!.Thickness = new (1, 1, 1, 1);

        // Remove margin bottom thickness
        view.Margin!.Thickness = new (0, 0, 0, 0);

        view.Draw ();

        Assert.Equal (6, view.Width);
        Assert.Equal (3, view.Height);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
╭────╮
│View│
╰────╯
",
                                                       output
                                                      );

        // Remove border bottom thickness
        view.Border!.Thickness = new (1, 1, 1, 0);

        // Add margin bottom thickness
        view.Margin!.Thickness = new (0, 0, 0, 1);

        Assert.Equal (6, view.Width);
        Assert.Equal (3, view.Height);

        View.SetClipToScreen ();
        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
╭────╮
│View│
",
                                                       output
                                                      );
    }
}
