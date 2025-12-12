#nullable enable
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
        app.Init ("fake");

        app.Driver?.SetScreenSize (6, 5);

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
        app.Init ("fake");

        app.Driver?.SetScreenSize (8, 7);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        superview.Text = """
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         🍎🍎🍎🍎
                         """;

        View view = new ()
        {
            X = 2, Width = 6, Height = 6, Arrangement = ViewArrangement.Movable | ViewArrangement.Resizable, CanFocus = true
        };
        view.Border!.Thickness = new (1);
        view.Border.Add (new View { Height = Dim.Auto (), Width = Dim.Auto (), Text = "Hi" });
        superview.Add (view);

        app.Begin (superview);

        Assert.Equal ("Absolute(2)", view.X.ToString ());

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

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(1)", view.X.ToString ());
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

        Assert.True (app.Keyboard.RaiseKeyDownEvent (Key.CursorLeft));
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        app.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsAre (
                                              """
                                              ◊i    🍎
                                                    🍎
                                                    🍎
                                                    🍎
                                                    🍎
                                                   ↘🍎
                                              🍎🍎🍎🍎
                                              """,
                                              output,
                                              app.Driver);
    }
}