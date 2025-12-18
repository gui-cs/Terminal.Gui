#nullable enable
using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace ViewBaseTests.Adornments;

[Collection ("Global Test Setup")]
public class BorderArrangementTests (ITestOutputHelper output)
{
    [Fact]
    public void Arrangement_Handles_Wide_Glyphs_Correctly ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (6, 5);

        // Using a replacement char to make sure wide glyphs are handled correctly
        // in the shadow area, to not confusing with a space char.
        app.Driver?.GetOutputBuffer ().SetWideGlyphReplacement (Rune.ReplacementChar);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        superview.Text = """
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         🍎🍎🍎
                         """;

        View view = new ()
        {
            X = 2, Width = 4, Height = 4, BorderStyle = LineStyle.Single,
            Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable, CanFocus = true
        };
        superview.Add (view);

        app.Begin (superview);

        Assert.Equal ("Absolute(2)", view.X.ToString ());

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              🍎┌──┐
                                              🍎│  │
                                              🍎│  │
                                              🍎└──┘
                                              🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.F5.WithCtrl));
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              🍎◊──┐
                                              🍎│  │
                                              🍎│  │
                                              🍎└──↘
                                              🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(1)", view.X.ToString ());
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              �◊──┐
                                              �│  │
                                              �│  │
                                              �└──↘
                                              🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              ◊──┐🍎
                                              │  │🍎
                                              │  │🍎
                                              └──↘🍎
                                              🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);
    }

    [Fact]
    public void Arrangement_With_SubView_In_Border_Handles_Wide_Glyphs_Correctly ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        app.Driver?.SetScreenSize (8, 7);

        // Using a replacement char to make sure wide glyphs are handled correctly
        // in the shadow area, to not confusing with a space char.
        app.Driver?.GetOutputBuffer ().SetWideGlyphReplacement (Rune.ReplacementChar);

        // Don't remove this array even if it seems unused, it is used to map the attributes indexes in the DriverAssert
        // Otherwise the test won't detect issues with attributes not visibly by the naked eye
        Attribute [] attributes =
        [
            new (ColorName16.Blue, ColorName16.BrightBlue, TextStyle.None),
            new (ColorName16.BrightBlue, ColorName16.Blue, TextStyle.None),
            new (ColorName16.Green, ColorName16.BrightGreen, TextStyle.None),
            new (ColorName16.Magenta, ColorName16.BrightMagenta, TextStyle.None),
            new (ColorName16.BrightMagenta, ColorName16.Magenta, TextStyle.None)
        ];

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        superview.SetScheme (new () { Normal = attributes [0], Focus = attributes [1] });

        superview.Text = """
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         """;

        View view = new () { X = 6, Width = 2, Height = 1, Text = "🦮" };
        view.SetScheme (new () { Normal = attributes [2] });

        View view2 = new ()
        {
            X = 2, Width = 6, Height = 6, Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable, CanFocus = true
        };
        view2.Border!.Thickness = new (1);
        view2.Border.Add (new View { Height = Dim.Auto (), Width = Dim.Auto (), Text = "Hi" });
        view2.SetScheme (new () { Normal = attributes [3], HotNormal = attributes [4] });

        superview.Add (view, view2);

        app.Begin (superview);

        Assert.Equal ("Absolute(2)", view2.X.ToString ());

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              🍎Hi
                                              🍎
                                              🍎
                                              🍎
                                              🍎
                                              🍎
                                              🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        DriverAssert.AssertDriverAttributesAre (
                                                """
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11111111
                                                """,
                                                output,
                                                app.Driver,
                                                attributes);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.F5.WithCtrl));
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              🍎◊i
                                              🍎
                                              🍎
                                              🍎
                                              🍎
                                              🍎     ↘
                                              🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        DriverAssert.AssertDriverAttributesAre (
                                                """
                                                11433333
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11333333
                                                11111111
                                                """,
                                                output,
                                                app.Driver,
                                                attributes);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(1)", view2.X.ToString ());
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              �◊i
                                              �
                                              �
                                              �
                                              �
                                              �     ↘
                                              🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        DriverAssert.AssertDriverAttributesAre (
                                                """
                                                14333332
                                                13333330
                                                13333330
                                                13333330
                                                13333330
                                                13333330
                                                11111111
                                                """,
                                                output,
                                                app.Driver,
                                                attributes);

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(0)", view2.X.ToString ());
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              ◊i    🦮
                                                    🍎
                                                    🍎
                                                    🍎
                                                    🍎
                                                   ↘🍎
                                              🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);

        DriverAssert.AssertDriverAttributesAre (
                                                """
                                                43333322
                                                33333311
                                                33333311
                                                33333311
                                                33333311
                                                33333311
                                                11111111
                                                """,
                                                output,
                                                app.Driver,
                                                attributes);
    }
}