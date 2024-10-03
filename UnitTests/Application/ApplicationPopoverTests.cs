using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests;

using System;
using Terminal.Gui;
using Xunit;

public class ApplicationPopoverTests
{
    [Fact]
    public void Popover_SetAndGet ()
    {
        // Arrange
        var popover = new View ();

        // Act
        Application.Popover = popover;

        // Assert
        Assert.Equal (popover, Application.Popover);
    }

    [Fact]
    public void Popover_SetToNull ()
    {
        // Arrange
        var popover = new View ();
        Application.Popover = popover;

        // Act
        Application.Popover = null;

        // Assert
        Assert.Null (Application.Popover);
    }

    [Fact]
    public void Popover_VisibleChangedEvent ()
    {
        // Arrange
        var popover = new View ()
        {
            Visible = false
        };
        Application.Popover = popover;
        bool eventTriggered = false;

        popover.VisibleChanged += (sender, e) => eventTriggered = true;

        // Act
        popover.Visible = true;

        // Assert
        Assert.True (eventTriggered);
    }

    [Fact]
    public void Popover_InitializesCorrectly ()
    {
        // Arrange
        var popover = new View ();

        // Act
        Application.Popover = popover;

        // Assert
        Assert.True (popover.IsInitialized);
    }

    [Fact]
    public void Popover_SetsColorScheme ()
    {
        // Arrange
        var popover = new View ();
        var topColorScheme = new ColorScheme ();
        Application.Top = new Toplevel { ColorScheme = topColorScheme };

        // Act
        Application.Popover = popover;

        // Assert
        Assert.Equal (topColorScheme, popover.ColorScheme);
    }

    [Fact]
    public void Popover_VisibleChangedToTrue_SetsFocus ()
    {
        // Arrange
        var popover = new View ()
        {
            Visible = false,
            CanFocus = true
        };
        Application.Popover = popover;

        // Act
        popover.Visible = true;

        // Assert
        Assert.True (popover.Visible);
        Assert.True (popover.HasFocus);
    }

    [Fact]
    public void Popover_VisibleChangedToFalse_Hides_And_Removes_Focus ()
    {
        // Arrange
        var popover = new View ()
        {
            Visible = false,
            CanFocus = true
        };
        Application.Popover = popover;
        popover.Visible = true;

        // Act
        popover.Visible = false;

        // Assert
        Assert.False (popover.Visible);
        Assert.False (popover.HasFocus);
    }

    [Fact]
    public void Popover_Quit_Command_Hides ()
    {
        // Arrange
        var popover = new View ()
        {
            Visible = false,
            CanFocus = true
        };
        Application.Popover = popover;
        popover.Visible = true;
        Assert.True (popover.Visible);
        Assert.True (popover.HasFocus);

        // Act
        Application.OnKeyDown (Application.QuitKey);

        // Assert
        Assert.False (popover.Visible);
        Assert.False (popover.HasFocus);
    }


    [Fact]
    public void Popover_MouseClick_Outside_Hides_Passes_Event_On ()
    {
        // Arrange
        Application.Top = new Toplevel ()
        {
            Id = "top",
            Height = 10,
            Width = 10,
        };

        View otherView = new ()
        {
            X = 1,
            Y = 1,
            Height = 1,
            Width = 1,
            Id = "otherView",
        };

        bool otherViewPressed = false;
        otherView.MouseEvent += (sender, e) =>
                                {
                                    otherViewPressed = e.MouseEvent.Flags.HasFlag(MouseFlags.Button1Pressed);
                                };

        Application.Top.Add (otherView);

        var popover = new View ()
        {
            Id = "popover",
            X = 5,
            Y = 5,
            Width = 1,
            Height = 1,
            Visible = false,
            CanFocus = true
        };

        Application.Popover = popover;
        popover.Visible = true;
        Assert.True (popover.Visible);
        Assert.True (popover.HasFocus);

        // Act
        // Click on popover
        Application.OnMouseEvent (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (5, 5) });
        Assert.True (popover.Visible);

        // Click outside popover (on button)
        Application.OnMouseEvent (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (1, 1) });

        // Assert
        Assert.True (otherViewPressed);
        Assert.False (popover.Visible);

        Application.Top.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }

    [Theory]
    [InlineData (0, 0, false)]
    [InlineData (5, 5, true)]
    [InlineData (10, 10, false)]
    [InlineData (5, 10, false)]
    [InlineData (9, 9, false)]
    public void Popover_MouseClick_Outside_Hides (int mouseX, int mouseY, bool expectedVisible)
    {
        // Arrange
        Application.Top = new Toplevel ()
        {
            Id = "top",
            Height = 10,
            Width = 10,
        };
        var popover = new View ()
        {
            Id = "popover",
            X = 5,
            Y = 5,
            Width = 1,
            Height = 1,
            Visible = false,
            CanFocus = true
        };

        Application.Popover = popover;
        popover.Visible = true;
        Assert.True (popover.Visible);
        Assert.True (popover.HasFocus);

        // Act
        Application.OnMouseEvent (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (mouseX, mouseY) });

        // Assert
        Assert.Equal (expectedVisible, popover.Visible);

        Application.Top.Dispose ();
        Application.ResetState (ignoreDisposed: true);
    }
}
