#nullable enable
using UnitTests;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class NeedsDrawTests ()
{
    [Fact]
    [AutoInitShutdown]
    public void Frame_Set_After_Initialize_Update_NeededDisplay ()
    {
        var frame = new FrameView ();

        var label = new Label
        {
            SchemeName = "Menu", X = 0, Y = 0, Text = "This should be the first line."
        };

        var view = new View
        {
            X = 0, // don't overcomplicate unit tests
            Y = 1,
            Height = Dim.Auto (DimAutoStyle.Text),
            Width = Dim.Auto (DimAutoStyle.Text),
            Text = "Press me!"
        };

        frame.Add (label, view);

        frame.X = Pos.Center ();
        frame.Y = Pos.Center ();
        frame.Width = 40;
        frame.Height = 8;

        Toplevel top = new ();

        top.Add (frame);

        RunState runState = Application.Begin (top);

        top.SubViewsLaidOut += (s, e) => { Assert.Equal (new (0, 0, 80, 25), top.NeedsDrawRect); };

        frame.SubViewsLaidOut += (s, e) => { Assert.Equal (new (0, 0, 40, 8), frame.NeedsDrawRect); };

        label.SubViewsLaidOut += (s, e) => { Assert.Equal (new (0, 0, 38, 1), label.NeedsDrawRect); };

        view.SubViewsLaidOut += (s, e) => { Assert.Equal (new (0, 0, 13, 1), view.NeedsDrawRect); };

        Assert.Equal (new (0, 0, 80, 25), top.Frame);
        Assert.Equal (new (20, 8, 40, 8), frame.Frame);

        Assert.Equal (
                      new (20, 8, 60, 16),
                      new Rectangle (
                                     frame.Frame.Left,
                                     frame.Frame.Top,
                                     frame.Frame.Right,
                                     frame.Frame.Bottom
                                    )
                     );
        Assert.Equal (new (0, 0, 30, 1), label.Frame);
        Assert.Equal (new (0, 1, 9, 1), view.Frame); // this proves frame was set
        Application.End (runState);
        top.Dispose ();
    }
}
