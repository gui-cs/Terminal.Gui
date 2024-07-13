using ColorHelper;
using Color = Terminal.Gui.Color;
using Xunit.Abstractions;

namespace UnitTests.Views;

public class ColorPicker2Tests (ITestOutputHelper output)
{

    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_DefaultBootDraw ()
    {
        var cp = new ColorPicker2 () { Width = 20, Height = 4, Value = new Color(0,0,0) };

        cp.Style.ShowTextFields = false;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        var expected =
            @"
H:▲█████████████████
S:▲█████████████████
V:▲█████████████████
Hex:#000000  ■
";

        TestHelpers.AssertDriverContentsAre (expected, output);

        top.Dispose ();
    }
    [Fact]
    [AutoInitShutdown]
    public void ColorPicker_KeyboardNavigation ()
    {
        var cp = new ColorPicker2 () { Width = 20, Height = 4, Value = new Color (0, 0, 0) };
        cp.Style.ColorModel = ColorModel.RGB;
        cp.Style.ShowTextFields = false;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();

        var expected =
            @"
R:▲█████████████████
G:▲█████████████████
B:▲█████████████████
Hex:#000000  ■
";
        TestHelpers.AssertDriverContentsAre (expected, output);

        Assert.IsAssignableFrom <IColorBar>(cp.Focused);
        cp.NewKeyDownEvent (Key.CursorRight);

        cp.Draw ();

         expected =
            @"
R:█▲████████████████
G:▲█████████████████
B:▲█████████████████
Hex:#0F0000  ■
";
        TestHelpers.AssertDriverContentsAre (expected, output);


        cp.NewKeyDownEvent (Key.CursorRight);

        cp.Draw ();

        expected =
            @"
R:██▲███████████████
G:▲█████████████████
B:▲█████████████████
Hex:#1E0000  ■
";
        TestHelpers.AssertDriverContentsAre (expected, output);

        top.Dispose ();
    }
    public static IEnumerable<object []> ColorPickerTestData ()
    {
        yield return new object []
        {
            new Color(255, 0, 0),
            @"
R:█████████████████▲
G:▲█████████████████
B:▲█████████████████
Hex:#FF0000  ■
"
        };
        yield return new object []
        {
            new Color(0, 255, 0),
            @"
R:▲█████████████████
G:█████████████████▲
B:▲█████████████████
Hex:#00FF00  ■
"
        };
        yield return new object []
        {
            new Color(0, 0, 255),
            @"
R:▲█████████████████
G:▲█████████████████
B:█████████████████▲
Hex:#0000FF  ■
"
        };


        yield return new object []
        {
            new Color(125, 125, 125),
            @"
R:█████████▲████████
G:█████████▲████████
B:█████████▲████████
Hex:#7D7D7D  ■
"
        };
    }

    [Theory]
    [AutoInitShutdown]
    [MemberData (nameof (ColorPickerTestData))]
    public void ColorPicker_RGB_NoText (Color c, string expected)
    {
        var cp = new ColorPicker2 () { Width = 20, Height = 4, Value = c };

        cp.Style.ShowTextFields = false;
        cp.Style.ColorModel = ColorModel.RGB;
        cp.ApplyStyleChanges ();

        var top = new Toplevel ();
        top.Add (cp);
        Application.Begin (top);

        cp.Draw ();


        TestHelpers.AssertDriverContentsAre (expected, output);

        top.Dispose ();
    }
}

