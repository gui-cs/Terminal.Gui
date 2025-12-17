using System;
using Terminal.Gui.App;
using Terminal.Gui.Time;
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
        _output.WriteLine ($"Expected >= 3 activations, got {activatingCount}");

        view.Dispose ();
    }

    #region Input Injection Tests (Application Level)

    [Fact]
    public void MouseHoldRepeat_True_AppInjection_Press_Release_Raises_Activating_Once ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();
        View view = new ()
        {
            Width = 10,
            Height = 10,
            MouseHoldRepeat = true
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatingCount = 0;
        view.Activating += (_, e) =>
        {
            activatingCount++;
            e.Handled = true;
        };

        // Act - Press at (0, 0)
        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonPressed,
                             ScreenPosition = new (0, 0)
                         });

        Assert.Equal (0, activatingCount); // Should not fire on press

        // Act - Release at (0, 0)
        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonReleased,
                             ScreenPosition = new (0, 0)
                         });

        // Assert - Activating should be raised exactly once on release
        // Note: Clicked event is synthesized but ignored when MouseHoldRepeat is true
        Assert.Equal (1, activatingCount);

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_True_AppInjection_Two_Press_Release_Cycles_Raises_Activating_Twice ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();
        View view = new ()
        {
            Width = 10,
            Height = 10,
            MouseHoldRepeat = true
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatingCount = 0;
        view.Activating += (_, e) =>
        {
            activatingCount++;
            e.Handled = true;
        };

        // Act - First press/release cycle
        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonPressed,
                             ScreenPosition = new (0, 0)
                         });

        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonReleased,
                             ScreenPosition = new (0, 0)
                         });

        Assert.Equal (1, activatingCount); // First release

        // Act - Second press/release cycle (will synthesize DoubleClicked)
        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonPressed,
                             ScreenPosition = new (0, 0)
                         });

        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonReleased,
                             ScreenPosition = new (0, 0)
                         });

        // Assert - Activating should be raised twice (once per release)
        // DoubleClicked is synthesized but ignored when MouseHoldRepeat is true
        Assert.Equal (2, activatingCount);

        (runnable as View)?.Dispose ();
    }

    [Fact (Skip = "TimedEvents doesn't support ITimer")]
    public void MouseHoldRepeat_True_AppInjection_Press_Wait_Release_Raises_Activating_Multiple_Times ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.CreateForTesting (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();
        View view = new ()
        {
            Width = 10,
            Height = 10,
            MouseHoldRepeat = true
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatingCount = 0;
        view.Activating += (_, e) =>
        {
            activatingCount++;
            _output.WriteLine ($"Activating #{activatingCount}");
            e.Handled = true;
        };

        // Act - Press button
        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonPressed,
                             ScreenPosition = new (0, 0)
                         });

        Assert.Equal (0, activatingCount); // Should not fire on press

        _output.WriteLine ($"After press: {app.TimedEvents?.Timeouts.Count ?? 0} timeouts registered");

        // Advance time and manually trigger timer processing
        // Initial delay is 500ms for MouseHoldRepeater
        time.Advance (TimeSpan.FromMilliseconds (500));
        _output.WriteLine ($"After time advance 500ms: {app.TimedEvents?.Timeouts.Count ?? 0} timeouts");
        app.TimedEvents?.RunTimers (); // First tick
        _output.WriteLine ($"After RunTimers #1: activatingCount={activatingCount}, timeouts={app.TimedEvents?.Timeouts.Count ?? 0}");

        time.Advance (TimeSpan.FromMilliseconds (50));
        app.TimedEvents?.RunTimers (); // Second tick
        _output.WriteLine ($"After RunTimers #2: activatingCount={activatingCount}, timeouts={app.TimedEvents?.Timeouts.Count ?? 0}");

        time.Advance (TimeSpan.FromMilliseconds (50));
        app.TimedEvents?.RunTimers (); // Third tick
        _output.WriteLine ($"After RunTimers #3: activatingCount={activatingCount}, timeouts={app.TimedEvents?.Timeouts.Count ?? 0}");

        // Act - Release button
        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonReleased,
                             ScreenPosition = new (0, 0)
                         });

        _output.WriteLine ($"After release: activatingCount={activatingCount}");

        // Assert - Should have >= 3 activations (3 from timer ticks + 1 from release)
        Assert.True (activatingCount >= 3, $"Expected >= 3 activations, got {activatingCount}");

        (runnable as View)?.Dispose ();
    }

    [Fact]
    public void MouseHoldRepeat_False_AppInjection_Press_Release_Raises_Activating_Once ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);

        IRunnable runnable = new Runnable ();
        View view = new ()
        {
            Width = 10,
            Height = 10,
            MouseHoldRepeat = false // Default behavior
        };
        (runnable as View)?.Add (view);
        app.Begin (runnable);

        var activatingCount = 0;
        view.Activating += (_, e) =>
        {
            activatingCount++;
            e.Handled = true;
        };

        // Act - Press at (0, 0)
        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonPressed,
                             ScreenPosition = new (0, 0)
                         });

        Assert.Equal (1, activatingCount); // Should fire on press

        // Act - Release at (0, 0) - synthesizes Clicked event
        app.InjectMouse (
                         new ()
                         {
                             Flags = MouseFlags.LeftButtonReleased,
                             ScreenPosition = new (0, 0)
                         });

        // Assert - Activating should still be 1 (not fired again on Clicked)
        Assert.Equal (1, activatingCount);

        (runnable as View)?.Dispose ();
    }

    #endregion
}
