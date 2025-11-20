using Xunit.Abstractions;

namespace UnitTests.ViewTests;

public class ArrangementTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void MouseGrabHandler_WorksWithMovableView_UsingNewMouseEvent ()
    {
        // This test proves that MouseGrabHandler works correctly with concurrent unit tests
        // using NewMouseEvent directly on views, without requiring Application.Init

        var superView = new View
        {
            Width = 80,
            Height = 25
        };
        superView.App = Application.Create ();

        var movableView = new View
        {
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 10,
            Y = 10,
            Width = 20,
            Height = 10
        };

        superView.Add (movableView);

        // Verify initial state
        Assert.NotNull (movableView.Border);
        Assert.Null (Application.Mouse.MouseGrabView);

        // Simulate mouse press on the border to start dragging
        var pressEvent = new MouseEventArgs
        {
            Position = new (1, 0), // Top border area
            Flags = MouseFlags.Button1Pressed
        };

        bool? result = movableView.Border.NewMouseEvent (pressEvent);

        // The border should have grabbed the mouse
        Assert.True (result);
        Assert.Equal (movableView.Border, superView.App.Mouse.MouseGrabView);

        // Simulate mouse drag
        var dragEvent = new MouseEventArgs
        {
            Position = new (5, 2),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        result = movableView.Border.NewMouseEvent (dragEvent);
        Assert.True (result);

        // Mouse should still be grabbed
        Assert.Equal (movableView.Border, superView.App.Mouse.MouseGrabView);

        // Simulate mouse release to end dragging
        var releaseEvent = new MouseEventArgs
        {
            Position = new (5, 2),
            Flags = MouseFlags.Button1Released
        };

        result = movableView.Border.NewMouseEvent (releaseEvent);
        Assert.True (result);

        // Mouse should be released
        Assert.Null (superView.App.Mouse.MouseGrabView);
    }

    [Fact]
    public void MouseGrabHandler_WorksWithResizableView_UsingNewMouseEvent ()
    {
        // This test proves MouseGrabHandler works for resizing operations

        var superView = new View
        {
            App = Application.Create (),
            Width = 80,
            Height = 25
        };

        var resizableView = new View
        {
            Arrangement = ViewArrangement.RightResizable,
            BorderStyle = LineStyle.Single,
            X = 10,
            Y = 10,
            Width = 20,
            Height = 10
        };

        superView.Add (resizableView);

        // Verify initial state
        Assert.NotNull (resizableView.Border);
        Assert.Null (Application.Mouse.MouseGrabView);

        // Calculate position on right border (border is at right edge)
        // Border.Frame.X is relative to parent, so we use coordinates relative to the border
        var pressEvent = new MouseEventArgs
        {
            Position = new (resizableView.Border.Frame.Width - 1, 5), // Right border area
            Flags = MouseFlags.Button1Pressed
        };

        bool? result = resizableView.Border.NewMouseEvent (pressEvent);

        // The border should have grabbed the mouse for resizing
        Assert.True (result);
        Assert.Equal (resizableView.Border, superView.App.Mouse.MouseGrabView);

        // Simulate dragging to resize
        var dragEvent = new MouseEventArgs
        {
            Position = new (resizableView.Border.Frame.Width + 3, 5),
            Flags = MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
        };

        result = resizableView.Border.NewMouseEvent (dragEvent);
        Assert.True (result);
        Assert.Equal (resizableView.Border, superView.App.Mouse.MouseGrabView);

        // Simulate mouse release
        var releaseEvent = new MouseEventArgs
        {
            Position = new (resizableView.Border.Frame.Width + 3, 5),
            Flags = MouseFlags.Button1Released
        };

        result = resizableView.Border.NewMouseEvent (releaseEvent);
        Assert.True (result);

        // Mouse should be released
        Assert.Null (superView.App.Mouse.MouseGrabView);
    }

    [Fact]
    public void MouseGrabHandler_ReleasesOnMultipleViews ()
    {
        // This test verifies MouseGrabHandler properly releases when switching between views

        var superView = new View { Width = 80, Height = 25 };
        superView.App = Application.Create ();

        var view1 = new View
        {
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 10,
            Y = 10,
            Width = 15,
            Height = 8
        };

        var view2 = new View
        {
            Arrangement = ViewArrangement.Movable,
            BorderStyle = LineStyle.Single,
            X = 30,
            Y = 10,
            Width = 15,
            Height = 8
        };

        superView.Add (view1, view2);
        superView.BeginInit ();
        superView.EndInit ();

        // Grab mouse on first view
        var pressEvent1 = new MouseEventArgs
        {
            Position = new (1, 0),
            Flags = MouseFlags.Button1Pressed
        };

        view1.Border!.NewMouseEvent (pressEvent1);
        Assert.Equal (view1.Border, superView.App.Mouse.MouseGrabView);

        // Release on first view
        var releaseEvent1 = new MouseEventArgs
        {
            Position = new (1, 0),
            Flags = MouseFlags.Button1Released
        };

        view1.Border.NewMouseEvent (releaseEvent1);
        Assert.Null (Application.Mouse.MouseGrabView);

        // Grab mouse on second view
        var pressEvent2 = new MouseEventArgs
        {
            Position = new (1, 0),
            Flags = MouseFlags.Button1Pressed
        };

        view2.Border!.NewMouseEvent (pressEvent2);
        Assert.Equal (view2.Border, superView.App.Mouse.MouseGrabView);

        // Release on second view
        var releaseEvent2 = new MouseEventArgs
        {
            Position = new (1, 0),
            Flags = MouseFlags.Button1Released
        };

        view2.Border.NewMouseEvent (releaseEvent2);
        Assert.Null (superView.App.Mouse.MouseGrabView);
    }
}
