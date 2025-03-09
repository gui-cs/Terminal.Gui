using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ApplicationTests;

using System;
using Terminal.Gui;
using Xunit;

public class ApplicationPopoverTests
{
    //[Fact]
    //public void Popover_SetAndGet ()
    //{
    //    // Arrange
    //    var popover = new View ();

    //    // Act
    //    Application.PopoverHost = popover;

    //    // Assert
    //    Assert.Equal (popover, Application.PopoverHost);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_SetToNull ()
    //{
    //    // Arrange
    //    var popover = new View ();
    //    Application.PopoverHost = popover;

    //    // Act
    //    Application.PopoverHost = null;

    //    // Assert
    //    Assert.Null (Application.PopoverHost);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_VisibleChangedEvent ()
    //{
    //    // Arrange
    //    var popover = new View ()
    //    {
    //        Visible = false
    //    };
    //    Application.PopoverHost = popover;
    //    bool eventTriggered = false;

    //    popover.VisibleChanged += (sender, e) => eventTriggered = true;

    //    // Act
    //    popover.Visible = true;

    //    // Assert
    //    Assert.True (eventTriggered);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_InitializesCorrectly ()
    //{
    //    // Arrange
    //    var popover = new View ();

    //    // Act
    //    Application.PopoverHost = popover;

    //    // Assert
    //    Assert.True (popover.IsInitialized);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_SetsColorScheme ()
    //{
    //    // Arrange
    //    var popover = new View ();
    //    var topColorScheme = new ColorScheme ();
    //    Application.Top = new Toplevel { ColorScheme = topColorScheme };

    //    // Act
    //    Application.PopoverHost = popover;

    //    // Assert
    //    Assert.Equal (topColorScheme, popover.ColorScheme);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_VisibleChangedToTrue_SetsFocus ()
    //{
    //    // Arrange
    //    var popover = new View ()
    //    {
    //        Visible = false,
    //        CanFocus = true
    //    };
    //    Application.PopoverHost = popover;

    //    // Act
    //    popover.Visible = true;

    //    // Assert
    //    Assert.True (popover.Visible);
    //    Assert.True (popover.HasFocus);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Theory]
    //[InlineData(-1, -1)]
    //[InlineData (0, 0)]
    //[InlineData (2048, 2048)]
    //[InlineData (2049, 2049)]
    //public void Popover_VisibleChangedToTrue_Locates_In_Visible_Position (int x, int y)
    //{
    //    // Arrange
    //    var popover = new View ()
    //    {
    //        X = x,
    //        Y = y,
    //        Visible = false,
    //        CanFocus = true,
    //        Width = 1,
    //        Height = 1
    //    };
    //    Application.PopoverHost = popover;

    //    // Act
    //    popover.Visible = true;
    //    Application.LayoutAndDraw();

    //    // Assert
    //    Assert.True (Application.Screen.Contains (popover.Frame));

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_VisibleChangedToFalse_Hides_And_Removes_Focus ()
    //{
    //    // Arrange
    //    var popover = new View ()
    //    {
    //        Visible = false,
    //        CanFocus = true
    //    };
    //    Application.PopoverHost = popover;
    //    popover.Visible = true;

    //    // Act
    //    popover.Visible = false;

    //    // Assert
    //    Assert.False (popover.Visible);
    //    Assert.False (popover.HasFocus);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_Quit_Command_Hides ()
    //{
    //    // Arrange
    //    var popover = new View ()
    //    {
    //        Visible = false,
    //        CanFocus = true
    //    };
    //    Application.PopoverHost = popover;
    //    popover.Visible = true;
    //    Assert.True (popover.Visible);
    //    Assert.True (popover.HasFocus);

    //    // Act
    //    Application.RaiseKeyDownEvent (Application.QuitKey);

    //    // Assert
    //    Assert.False (popover.Visible);
    //    Assert.False (popover.HasFocus);

    //    Application.ResetState (ignoreDisposed: true);
    //}


    //[Fact]
    //public void Popover_MouseClick_Outside_Hides_Passes_Event_On ()
    //{
    //    // Arrange
    //    Application.Top = new Toplevel ()
    //    {
    //        Id = "top",
    //        Height = 10,
    //        Width = 10,
    //    };

    //    View otherView = new ()
    //    {
    //        X = 1,
    //        Y = 1,
    //        Height = 1,
    //        Width = 1,
    //        Id = "otherView",
    //    };

    //    bool otherViewPressed = false;
    //    otherView.MouseEvent += (sender, e) =>
    //                            {
    //                                otherViewPressed = e.Flags.HasFlag(MouseFlags.Button1Pressed);
    //                            };

    //    Application.Top.Add (otherView);

    //    var popover = new View ()
    //    {
    //        Id = "popover",
    //        X = 5,
    //        Y = 5,
    //        Width = 1,
    //        Height = 1,
    //        Visible = false,
    //        CanFocus = true
    //    };

    //    Application.PopoverHost = popover;
    //    popover.Visible = true;
    //    Assert.True (popover.Visible);
    //    Assert.True (popover.HasFocus);

    //    // Act
    //    // Click on popover
    //    Application.RaiseMouseEvent (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (5, 5) });
    //    Assert.True (popover.Visible);

    //    // Click outside popover (on button)
    //    Application.RaiseMouseEvent (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (1, 1) });

    //    // Assert
    //    Assert.True (otherViewPressed);
    //    Assert.False (popover.Visible);

    //    Application.Top.Dispose ();
    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Theory]
    //[InlineData (0, 0, false)]
    //[InlineData (5, 5, true)]
    //[InlineData (10, 10, false)]
    //[InlineData (5, 10, false)]
    //[InlineData (9, 9, false)]
    //public void Popover_MouseClick_Outside_Hides (int mouseX, int mouseY, bool expectedVisible)
    //{
    //    // Arrange
    //    Application.Top = new Toplevel ()
    //    {
    //        Id = "top",
    //        Height = 10,
    //        Width = 10,
    //    };
    //    var popover = new View ()
    //    {
    //        Id = "popover",
    //        X = 5,
    //        Y = 5,
    //        Width = 1,
    //        Height = 1,
    //        Visible = false,
    //        CanFocus = true
    //    };

    //    Application.PopoverHost = popover;
    //    popover.Visible = true;
    //    Assert.True (popover.Visible);
    //    Assert.True (popover.HasFocus);

    //    // Act
    //    Application.RaiseMouseEvent (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (mouseX, mouseY) });

    //    // Assert
    //    Assert.Equal (expectedVisible, popover.Visible);

    //    Application.Top.Dispose ();
    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_SetAndGet_ReturnsCorrectValue ()
    //{
    //    // Arrange
    //    var view = new View ();

    //    // Act
    //    Application.PopoverHost = view;

    //    // Assert
    //    Assert.Equal (view, Application.PopoverHost);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_SetToNull_HidesPreviousPopover ()
    //{
    //    // Arrange
    //    var view = new View { Visible = true };
    //    Application.PopoverHost = view;

    //    // Act
    //    Application.PopoverHost = null;

    //    // Assert
    //    Assert.False (view.Visible);
    //    Assert.Null (Application.PopoverHost);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_SetNewPopover_HidesPreviousPopover ()
    //{
    //    // Arrange
    //    var oldView = new View { Visible = true };
    //    var newView = new View ();
    //    Application.PopoverHost = oldView;

    //    // Act
    //    Application.PopoverHost = newView;

    //    // Assert
    //    Assert.False (oldView.Visible);
    //    Assert.Equal (newView, Application.PopoverHost);

    //    Application.ResetState (ignoreDisposed: true);
    //}

    //[Fact]
    //public void Popover_SetNewPopover_InitializesAndSetsProperties ()
    //{
    //    // Arrange
    //    var view = new View ();

    //    // Act
    //    Application.PopoverHost = view;

    //    // Assert
    //    Assert.True (view.IsInitialized);
    //    Assert.True (view.Arrangement.HasFlag (ViewArrangement.Overlapped));
    //    Assert.Equal (Application.Top?.ColorScheme, view.ColorScheme);

    //    Application.ResetState (ignoreDisposed: true);
    //}
}
