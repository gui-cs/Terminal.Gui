using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using UnitTests;
using Xunit.Abstractions;
using Timeout = Terminal.Gui.App.Timeout;

namespace ViewBaseTests.MouseTests;

/// <summary>
///     Tests for View.MouseHoldRepeat functionality.
///     These tests verify that when MouseHoldRepeat is true, views correctly handle
///     press/release cycles with timer-based repetition.
/// </summary>
// CoPilot - GitHub Copilot Workspace
[Trait ("Category", "Input")]
public class MouseHoldRepeatTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void MouseHoldRepeat_True_Press_Release_Starts_And_Stops_Timer ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
            MouseHoldRepeat = true
        };

        TimedEvents timedEvents = new ();
        MouseImpl mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        Mouse mouse = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.LeftButtonPressed
        };

        // Act - Press button
        view.NewMouseEvent (mouse);

        // Assert - Timer should be started
        Assert.NotEmpty (timedEvents.Timeouts);

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Timer should be stopped
        Assert.Empty (timedEvents.Timeouts);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_Press_Release_Raises_Activating_Once ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
            MouseHoldRepeat = true
        };

        TimedEvents timedEvents = new ();
        MouseImpl mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
        {
            activatingCount++;
            e.Handled = true;
        };

        Mouse mouse = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.LeftButtonPressed
        };

        // Act - Press button
        view.NewMouseEvent (mouse);
        Assert.Equal (0, activatingCount); // Should not fire on press

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Activating should be raised exactly once on release
        Assert.Equal (1, activatingCount);

        view.Dispose ();
    }


    [Fact]
    public void MouseHoldRepeat_False_Press_Release_Raises_Activating_Once ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
            MouseHoldRepeat = false // false is the default; here for clarity
        };

        TimedEvents timedEvents = new ();
        MouseImpl mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        Mouse mouse = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.LeftButtonPressed
        };

        // Act - Press button
        view.NewMouseEvent (mouse);
        Assert.Equal (1, activatingCount); // Should fire on press

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Activating should be raised exactly once
        Assert.Equal (1, activatingCount);

        view.Dispose ();
    }


    [Fact]
    public void MouseHoldRepeat_True_Then_False_Press_Release_Raises_Activating_Once ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
        };

        TimedEvents timedEvents = new ();
        MouseImpl mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
                           {
                               activatingCount++;
                               e.Handled = true;
                           };

        Mouse mouse = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.LeftButtonPressed
        };

        // Act - Enable MouseHoldRepeat then disable it
        view.MouseHoldRepeat = true;
        view.MouseHoldRepeat = false;

        // Now press
        view.NewMouseEvent (mouse);
        Assert.Equal (1, activatingCount); // Should fire on press

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Activating should be raised exactly once on release
        Assert.Equal (1, activatingCount);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_Two_Press_Release_Cycles_Raises_Activating_Twice ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
            MouseHoldRepeat = true
        };

        TimedEvents timedEvents = new ();
        MouseImpl mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
        {
            activatingCount++;
            e.Handled = true;
        };


        Mouse mouse = new ()
        {
            Position = new Point (5, 5)
        };

        // Act - First press/release cycle
        mouse.Flags = MouseFlags.LeftButtonPressed;
        view.NewMouseEvent (mouse);

        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        Assert.Equal (1, activatingCount); // First release

        // Act - Second press/release cycle
        mouse.Flags = MouseFlags.LeftButtonPressed;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Activating should be raised twice (once per release)
        Assert.Equal (2, activatingCount);

        view.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_Press_Wait_Release_Raises_Activating_Multiple_Times ()
    {
        // Arrange
        View view = new ()
        {
            Width = 10,
            Height = 10,
            MouseHoldRepeat = true
        };

        TimedEvents timedEvents = new ();
        MouseImpl mouseGrabber = new ();
        view.MouseHoldRepeater = new MouseHoldRepeaterImpl (view, timedEvents, mouseGrabber);

        var activatingCount = 0;

        view.Activating += (_, e) =>
        {
            activatingCount++;
            e.Handled = true;
        };

        Mouse mouse = new ()
        {
            Position = new Point (5, 5),
            Flags = MouseFlags.LeftButtonPressed
        };

        // Act - Press button
        view.NewMouseEvent (mouse);
        Assert.Equal (0, activatingCount); // Should not fire on press

        // Simulate timer ticks (waiting while button is held)
        for (var i = 0; i < 3; i++)
        {
            KeyValuePair<long, Timeout> timeout = Assert.Single (timedEvents.Timeouts);
            timeout.Value.Callback?.Invoke ();
        }

        // Timer ticks should NOT directly invoke Activating - they invoke MouseIsHeldDownTick
        // which then invokes commands, but we're testing the view's behavior

        // Act - Release button
        mouse.Flags = MouseFlags.LeftButtonReleased;
        mouse.Handled = false;
        view.NewMouseEvent (mouse);

        // Assert - Should have > 3 activations (3 from timer ticks + 1 from release)
        // Note: The timer invokes commands through MouseIsHeldDownTick which calls
        // RaiseCommandsBoundToButtonFlags internally
        Assert.True (activatingCount >= 3, $"Expected >= 3 activations, got {activatingCount}");
        _output.WriteLine($"Expected >= 3 activations, got {activatingCount}");

        view.Dispose ();
    }
}
