namespace UnitTests.ViewBaseTests;

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
        superView.App = ApplicationImpl.Instance;

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
        Assert.False (Application.Mouse.IsGrabbed (movableView.Border));

        // Simulate mouse press on the border to start dragging
        var pressEvent = new Mouse
        {
            Position = new (1, 0), // Top border area
            Flags = MouseFlags.LeftButtonPressed
        };

        bool? result = movableView.Border.NewMouseEvent (pressEvent);

        // The border should have grabbed the mouse
        Assert.True (result);
        Assert.True (superView.App.Mouse.IsGrabbed (movableView.Border));

        // Simulate mouse drag
        var dragEvent = new Mouse
        {
            Position = new (5, 2),
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport
        };

        result = movableView.Border.NewMouseEvent (dragEvent);
        Assert.True (result);

        // Mouse should still be grabbed
        Assert.True (superView.App.Mouse.IsGrabbed (movableView.Border));

        // Simulate mouse release to end dragging
        var releaseEvent = new Mouse
        {
            Position = new (5, 2),
            Flags = MouseFlags.LeftButtonReleased
        };

        result = movableView.Border.NewMouseEvent (releaseEvent);
        Assert.True (result);

        // Mouse should be released
        Assert.False (superView.App.Mouse.IsGrabbed (movableView.Border));
    }

    [Fact]
    public void MouseGrabHandler_WorksWithResizableView_UsingNewMouseEvent ()
    {
        // This test proves MouseGrabHandler works for resizing operations

        var superView = new View
        {
            App = ApplicationImpl.Instance,
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
        Assert.False (Application.Mouse.IsGrabbed (resizableView.Border));

        // Calculate position on right border (border is at right edge)
        // Border.Frame.X is relative to parent, so we use coordinates relative to the border
        var pressEvent = new Mouse
        {
            Position = new (resizableView.Border.Frame.Width - 1, 5), // Right border area
            Flags = MouseFlags.LeftButtonPressed
        };

        bool? result = resizableView.Border.NewMouseEvent (pressEvent);

        // The border should have grabbed the mouse for resizing
        Assert.True (result);
        Assert.True (superView.App.Mouse.IsGrabbed (resizableView.Border));

        // Simulate dragging to resize
        var dragEvent = new Mouse
        {
            Position = new (resizableView.Border.Frame.Width + 3, 5),
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.PositionReport
        };

        result = resizableView.Border.NewMouseEvent (dragEvent);
        Assert.True (result);
        Assert.True (superView.App.Mouse.IsGrabbed (resizableView.Border));

        // Simulate mouse release
        var releaseEvent = new Mouse
        {
            Position = new (resizableView.Border.Frame.Width + 3, 5),
            Flags = MouseFlags.LeftButtonReleased
        };

        result = resizableView.Border.NewMouseEvent (releaseEvent);
        Assert.True (result);

        // Mouse should be released
        Assert.False (superView.App.Mouse.IsGrabbed (resizableView.Border));
    }

    [Fact]
    public void MouseGrabHandler_ReleasesOnMultipleViews ()
    {
        // This test verifies MouseGrabHandler properly releases when switching between views

        var superView = new View { Width = 80, Height = 25 };
        superView.App = ApplicationImpl.Instance;

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
        var pressEvent1 = new Mouse
        {
            Position = new (1, 0),
            Flags = MouseFlags.LeftButtonPressed
        };

        view1.Border!.NewMouseEvent (pressEvent1);
        Assert.True (superView.App.Mouse.IsGrabbed (view1.Border));

        // Release on first view
        var releaseEvent1 = new Mouse
        {
            Position = new (1, 0),
            Flags = MouseFlags.LeftButtonReleased
        };

        view1.Border.NewMouseEvent (releaseEvent1);
        Assert.False (Application.Mouse.IsGrabbed (view1.Border));

        // Grab mouse on second view
        var pressEvent2 = new Mouse
        {
            Position = new (1, 0),
            Flags = MouseFlags.LeftButtonPressed
        };

        view2.Border!.NewMouseEvent (pressEvent2);
        Assert.True (superView.App.Mouse.IsGrabbed (view2.Border));

        // Release on second view
        var releaseEvent2 = new Mouse
        {
            Position = new (1, 0),
            Flags = MouseFlags.LeftButtonReleased
        };

        view2.Border.NewMouseEvent (releaseEvent2);
        Assert.False (superView.App.Mouse.IsGrabbed (view2.Border));
    }
}
