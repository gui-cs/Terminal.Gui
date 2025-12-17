using UnitTests;
using Xunit.Abstractions;

namespace ViewBaseTests.MouseTests;

public class HighlightStatesTests (ITestOutputHelper output)
{
    [Fact]
    public void HighlightStates_SubView_With_Single_Runnable_WorkAsExpected ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        app.Driver?.SetScreenSize (6, 1);

        Attribute focus = new (ColorName16.White, ColorName16.Black, TextStyle.None);
        Attribute highlight = new (ColorName16.Blue, ColorName16.Black, TextStyle.Italic);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        superview.SetScheme (new () { Focus = focus, Highlight = highlight });
        View view = new () { Width = Dim.Fill (), Height = Dim.Fill (), Text = "| Hi |", MouseHighlightStates = MouseState.In };
        superview.Add (view);

        app.Begin (superview);

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (focus, app.Driver.Contents? [0, i].Attribute);
        }

        DriverAssert.AssertDriverContentsAre ("| Hi |", output, app.Driver);

        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 0), Flags = MouseFlags.PositionReport });
        app.LayoutAndDraw ();

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (highlight, app.Driver.Contents? [0, i].Attribute);
        }

        DriverAssert.AssertDriverContentsAre ("| Hi |", output, app.Driver);

        app.Dispose ();
    }

    [Fact]
    public void HighlightStates_SubView_With_Multiple_Runnable_WorkAsExpected ()
    {
        IApplication app = Application.Create ();
        app.Init ("fake");

        app.Driver?.SetScreenSize (9, 5);

        Attribute focus = new (ColorName16.White, ColorName16.Black, TextStyle.None);
        Attribute highlight = new (ColorName16.Blue, ColorName16.Black, TextStyle.Italic);

        Runnable superview = new () { Width = Dim.Fill (), Height = Dim.Fill () };
        superview.SetScheme (new () { Focus = focus, Highlight = highlight });
        View view = new () { Width = Dim.Fill (), Height = Dim.Fill (), Text = "| Hi |", MouseHighlightStates = MouseState.In };
        superview.Add (view);

        app.Begin (superview);

        Attribute normal = new (ColorName16.Green, ColorName16.Magenta, TextStyle.None);
        Attribute highlight2 = new (ColorName16.Red, ColorName16.Yellow, TextStyle.Italic);

        Runnable modalSuperview = new () { Y = 1, Width = 9, Height = 4, BorderStyle = LineStyle.Single };
        modalSuperview.SetScheme (new () { Normal = normal, Highlight = highlight2 });
        View view2 = new () { Width = Dim.Fill (), Height = Dim.Fill (), Text = "| Hey |", MouseHighlightStates = MouseState.In };
        modalSuperview.Add (view2);

        app.Begin (modalSuperview);

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (focus, app.Driver.Contents? [0, i].Attribute);
        }

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (normal, app.Driver.Contents? [2, i].Attribute);
        }

        DriverAssert.AssertDriverContentsAre ("""
                                              | Hi |
                                              ┌───────┐
                                              │| Hey |│
                                              │       │
                                              └───────┘
                                              """
                                              , output, app.Driver);

        app.Mouse.RaiseMouseEvent (new () { ScreenPosition = new (2, 2), Flags = MouseFlags.PositionReport });
        app.LayoutAndDraw ();

        for (var i = 0; i < app.Driver?.Cols; i++)
        {
            Assert.Equal (focus, app.Driver.Contents? [0, i].Attribute);
        }

        for (var i = 1; i < app.Driver?.Cols - 1; i++)
        {
            Assert.Equal (highlight2, app.Driver?.Contents? [2, i].Attribute);
        }

        DriverAssert.AssertDriverContentsAre ("""
                                              | Hi |
                                              ┌───────┐
                                              │| Hey |│
                                              │       │
                                              └───────┘
                                              """,
                                              output, app.Driver);

        app.Dispose ();
    }
}
