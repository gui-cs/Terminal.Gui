using Color = Terminal.Gui.Color;
using Xunit.Abstractions;

namespace UnitTests.Views;

public class ColorPicker2Tests (ITestOutputHelper output)
{

    [Fact]
    [AutoInitShutdown]
    public void CellEventsBackgroundFill ()
    {
        var cp = new ColorPicker2 () { Width = 20, Height = 4, Value = new Color(0,0,0) };

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        var expected =
            @"
H:▲█████████████
S:▲█████████████
V:▲█████████████
Hex:#000000  ■
";

        TestHelpers.AssertDriverContentsAre (expected, output);

        top.Dispose ();
    }
}

