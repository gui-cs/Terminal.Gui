using UnitTests;
using Xunit.Abstractions;

namespace ViewBaseTests.MouseTests;

public class HighlightStatesTests (ITestOutputHelper output)
{
    [Theory]
    [CombinatorialData]
    public void View_MouseEvent_With_Press_Release_Gets_3 (MouseState mouseState)
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        View view = new () { X = 0, Y = 0, Width = 10, Height = 10, MouseHighlightStates = mouseState };

        List<MouseFlags> receivedFlags = [];
        view.MouseEvent += MouseEventHandler;

        runnable.Add (view);
        app.Begin (runnable);

        // Act
        // Use Direct mode to bypass ANSI encoding which cannot preserve timestamps
        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);

        IInputInjector injector = app.GetInputInjector ();

        // First click at T+0
        injector.InjectMouse (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (
                              new () { ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (100) },
                              options);

        // Assert
        Assert.Equal (3, receivedFlags.Count);
        Assert.Equal (MouseFlags.LeftButtonPressed, receivedFlags [0]);
        Assert.Equal (MouseFlags.LeftButtonReleased, receivedFlags [1]);
        Assert.Equal (MouseFlags.LeftButtonClicked, receivedFlags [2]);

        view.MouseEvent -= MouseEventHandler;

        return;

        void MouseEventHandler (object? s, Mouse e) { receivedFlags.Add (e.Flags); }
    }

    [Theory]
    [CombinatorialData]
    public void View_MouseEvent_With_Press_Release_Raises_Activate (MouseState mouseState)
    {
        using IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

        using Runnable runnable = new ();
        View view = new () { X = 0, Y = 0, Width = 10, Height = 10, MouseHighlightStates = mouseState };

        int activateCount = 0;
        view.Activating += (_, _) => activateCount++;

        runnable.Add (view);
        app.Begin (runnable);

        // Act
        // Use Direct mode to bypass ANSI encoding which cannot preserve timestamps
        InputInjectionOptions options = new () { Mode = InputInjectionMode.Direct };
        DateTime baseTime = new (2025, 1, 1, 12, 0, 0);

        IInputInjector injector = app.GetInputInjector ();

        // First click at T+0
        injector.InjectMouse (new () { ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonPressed, Timestamp = baseTime }, options);

        injector.InjectMouse (
                              new () { ScreenPosition = new (5, 5), Flags = MouseFlags.LeftButtonReleased, Timestamp = baseTime.AddMilliseconds (100) },
                              options);

        // Assert
        Assert.Equal (1, activateCount);

    }

    [Fact]
    public void SubView_With_Single_Runnable_WorkAsExpected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

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
    public void SubView_With_Multiple_Runnable_WorkAsExpected ()
    {
        IApplication app = Application.Create ();
        app.Init (DriverRegistry.Names.ANSI);

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
