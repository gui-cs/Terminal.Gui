#nullable enable

namespace Terminal.Gui.ViewTests;

public class TransparentMouseTests
{
    private class MouseTrackingView : View
    {
        public bool MouseEventReceived { get; private set; }

        protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
        {
            MouseEventReceived = true;
            return true;
        }
    }

    [Fact]
    public void TransparentMouse_Passes_Mouse_Events_To_Underlying_View ()
    {
        // Arrange
        var top = new Toplevel ()
        {
            Id = "top",
        };
        Application.Top = top;

        var underlying = new MouseTrackingView { Id = "underlying", X = 0, Y = 0, Width = 10, Height = 10 };
        var overlay = new MouseTrackingView { Id = "overlay", X = 0, Y = 0, Width = 10, Height = 10, ViewportSettings = ViewportSettingsFlags.TransparentMouse };

        top.Add (underlying);
        top.Add (overlay);

        top.BeginInit ();
        top.EndInit ();
        top.Layout ();

        var mouseEvent = new MouseEventArgs
        {
            ScreenPosition = new (5, 5),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        Application.RaiseMouseEvent (mouseEvent);

        // Assert
        Assert.True (underlying.MouseEventReceived);

        top.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    public void NonTransparentMouse_Consumes_Mouse_Events ()
    {
        // Arrange
        var top = new Toplevel ();
        Application.Top = top;

        var underlying = new MouseTrackingView { X = 0, Y = 0, Width = 10, Height = 10 };
        var overlay = new MouseTrackingView { X = 0, Y = 0, Width = 10, Height = 10, ViewportSettings = ViewportSettingsFlags.None };

        top.Add (underlying);
        top.Add (overlay);

        top.BeginInit ();
        top.EndInit ();
        top.Layout ();

        var mouseEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (5, 5),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        Application.RaiseMouseEvent (mouseEvent);

        // Assert
        Assert.True (overlay.MouseEventReceived);
        Assert.False (underlying.MouseEventReceived);

        top.Dispose ();
        Application.ResetState (true);
    }

    [Fact]
    public void TransparentMouse_Stacked_TransparentMouse_Views ()
    {
        // Arrange
        var top = new Toplevel ();
        Application.Top = top;

        var underlying = new MouseTrackingView { X = 0, Y = 0, Width = 10, Height = 10, ViewportSettings = ViewportSettingsFlags.TransparentMouse };
        var overlay = new MouseTrackingView { X = 0, Y = 0, Width = 10, Height = 10, ViewportSettings = ViewportSettingsFlags.TransparentMouse };

        top.Add (underlying);
        top.Add (overlay);

        top.BeginInit ();
        top.EndInit ();
        top.Layout ();

        var mouseEvent = new MouseEventArgs
        {
            ScreenPosition = new Point (5, 5),
            Flags = MouseFlags.Button1Clicked
        };

        bool topHandled = false;
        top.MouseEvent += (sender, args) =>
                          {
                              topHandled = true;
                              args.Handled = true;
                          };

        // Act
        Application.RaiseMouseEvent (mouseEvent);

        // Assert
        Assert.False (overlay.MouseEventReceived);
        Assert.False (underlying.MouseEventReceived);
        Assert.True (topHandled);

        top.Dispose ();
        Application.ResetState (true);
    }
}
