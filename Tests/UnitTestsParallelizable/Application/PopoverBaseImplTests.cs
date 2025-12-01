using System;
using Terminal.Gui;
using Terminal.Gui.App;
using Xunit;
namespace UnitTests_Parallelizable.ApplicationTests;

public class PopoverBaseImplTests
{
    // Minimal concrete implementation for testing
    private class TestPopover : PopoverBaseImpl { }

    [Fact]
    public void Constructor_SetsDefaults ()
    {
        var popover = new TestPopover ();

        Assert.Equal ("popoverBaseImpl", popover.Id);
        Assert.True (popover.CanFocus);
        Assert.Equal (Dim.Fill (), popover.Width);
        Assert.Equal (Dim.Fill (), popover.Height);
        Assert.True (popover.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent));
        Assert.True (popover.ViewportSettings.HasFlag (ViewportSettingsFlags.TransparentMouse));
    }

    [Fact]
    public void Runnable_Property_CanBeSetAndGet ()
    {
        var popover = new TestPopover ();
        var top = new Runnable ();
        popover.Current = top;
        Assert.Same (top, popover.Current);
    }

    [Fact]
    public void Show_ThrowsIfPopoverMissingRequiredFlags ()
    {
        var popover = new TestPopover ();

        // Popover missing Transparent flags
        popover.ViewportSettings = ViewportSettingsFlags.None; // Remove required flags

        var popoverManager = new ApplicationPopover ();
        // Test missing Transparent flags
        Assert.ThrowsAny<Exception> (() => popoverManager.Show (popover));

    }


    [Fact]
    public void Show_ThrowsIfPopoverMissingQuitCommand ()
    {
        var popover = new TestPopover ();

        // Popover missing Command.Quit binding
        popover.KeyBindings.Clear (); // Remove all key bindings

        var popoverManager = new ApplicationPopover ();
        Assert.ThrowsAny<Exception> (() => popoverManager.Show (popover));
    }

    [Fact]
    public void Show_Throw_If_Not_Registered ()
    {
        var popover = new TestPopover ();

        var popoverManager = new ApplicationPopover ();
        Assert.Throws<InvalidOperationException> (() => popoverManager.Show (popover));
    }
}
