using Xunit.Abstractions;
// ReSharper disable AccessToModifiedClosure
// ReSharper disable InconsistentNaming

namespace DriverTests.Mouse;

/// <summary>
///     Unit tests for <see cref="MouseButtonClickTracker"/> state machine and click detection logic.
/// </summary>
[Trait ("Category", "Input")]
public class MouseButtonClickTrackerTests (ITestOutputHelper output)
{
    #region Setup

    private readonly ITestOutputHelper _output = output;

    #endregion

    #region State Transition Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_SinglePress_ReturnsNull ()
    {
        // Arrange
        DateTime now = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => now, TimeSpan.FromMilliseconds (500), 0);
        Terminal.Gui.Input.Mouse mouse = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };

        // Act
        tracker.UpdateState (mouse, out int? numClicks);

        // Assert
        Assert.Null (numClicks);
        Assert.True (tracker.Pressed);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_Press_Release_Returns1 ()
    {
        // Arrange
        DateTime now = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => now, TimeSpan.FromMilliseconds (500), 0);

        Terminal.Gui.Input.Mouse pressEvent = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse releaseEvent = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (pressEvent, out int? pressClicks);
        tracker.UpdateState (releaseEvent, out int? releaseClicks);

        // Assert
        Assert.Null (pressClicks);
        Assert.Equal (1, releaseClicks);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_Press_Release_Press_Release_WithinThreshold_Returns2 ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => currentTime, TimeSpan.FromMilliseconds (500), 0);

        Terminal.Gui.Input.Mouse pressEvent1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse releaseEvent1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse pressEvent2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse releaseEvent2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (pressEvent1, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (releaseEvent1, out int? clicks2);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (pressEvent2, out int? clicks3);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (releaseEvent2, out int? clicks4);

        // Assert
        Assert.Null (clicks1); // First press
        Assert.Equal (1, clicks2); // First release
        Assert.Null (clicks3); // Second press
        Assert.Equal (2, clicks4); // Second release - double click!
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_TripleClick_WithinThreshold_Returns3 ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => currentTime, TimeSpan.FromMilliseconds (500), 0);

        Terminal.Gui.Input.Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act - Three complete click sequences
        tracker.UpdateState (press, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks1);

        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks2);

        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks3);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (2, clicks2);
        Assert.Equal (3, clicks3); // Triple click!
    }

    #endregion

    #region ScreenPosition Change Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_ScreenPositionChangeDuringSequence_ResetsCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => currentTime, TimeSpan.FromMilliseconds (500), 0);

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (20, 20), Flags = MouseFlags.Button1Pressed }; // Different position!
        Terminal.Gui.Input.Mouse release2 = new () { ScreenPosition = new (20, 20), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (press1, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks1);

        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press2, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (1, clicks2); // Reset to 1 because position changed
    }

    #endregion

    #region Timeout Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_TimeoutExceeded_ResetsCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => currentTime, TimeSpan.FromMilliseconds (500), 0);

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (press1, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks1);

        currentTime = currentTime.AddMilliseconds (600); // Exceed 500ms threshold!
        tracker.UpdateState (press2, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (1, clicks2); // Reset to 1 because timeout exceeded
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_ExactlyAtThreshold_ResetsCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        TimeSpan threshold = TimeSpan.FromMilliseconds (500);
        MouseButtonClickTracker tracker = new (() => currentTime, threshold, 0);

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (press1, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks1);

        currentTime = currentTime.Add (threshold).AddMilliseconds (1); // Just over threshold
        tracker.UpdateState (press2, out int? clicks2);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks3);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Null (clicks2); // Reset because we exceeded threshold, press doesn't return click count
        Assert.Equal (1, clicks3); // After reset, starts counting from 1 again
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_OneMsBelowThreshold_ContinuesCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        TimeSpan threshold = TimeSpan.FromMilliseconds (500);
        MouseButtonClickTracker tracker = new (() => currentTime, threshold, 0);

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (press1, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks1);

        currentTime = currentTime.AddMilliseconds (449); // 499ms total - just below 500ms threshold
        tracker.UpdateState (press2, out _);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks2);

        // Assert
        Assert.Equal (1, clicks1);
        Assert.Equal (2, clicks2); // Continues counting because within threshold
    }

    #endregion

    #region Edge Case Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_ReleaseWithoutPress_ReturnsNull ()
    {
        // Arrange
        DateTime now = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => now, TimeSpan.FromMilliseconds (500), 0);
        Terminal.Gui.Input.Mouse releaseEvent = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (releaseEvent, out int? numClicks);

        // Assert
        Assert.Null (numClicks); // No press before release, so no click
        Assert.False (tracker.Pressed);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_MultipleConsecutivePresses_OnlyLastCounts ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => currentTime, TimeSpan.FromMilliseconds (500), 0);

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse press3 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (press1, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press2, out int? clicks2);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press3, out int? clicks3);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks4);

        // Assert
        Assert.Null (clicks1);
        Assert.Null (clicks2); // No state change (already pressed)
        Assert.Null (clicks3); // No state change (already pressed)
        Assert.Equal (1, clicks4); // Single click when finally released
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_MultipleConsecutiveReleases_OnlyFirstCounts ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => currentTime, TimeSpan.FromMilliseconds (500), 0);

        Terminal.Gui.Input.Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse release3 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (press, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release1, out int? clicks2);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release2, out int? clicks3);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release3, out int? clicks4);

        // Assert
        Assert.Null (clicks1);
        Assert.Equal (1, clicks2); // Click registered on first release
        Assert.Null (clicks3); // No state change (already released)
        Assert.Null (clicks4); // No state change (already released)
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_DoublePress_WithoutIntermediateRelease_DoesNotCountAsDoubleClick ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => currentTime, TimeSpan.FromMilliseconds (500), 0);

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        tracker.UpdateState (press1, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (press2, out int? clicks2); // Second press without release
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks3);

        // Assert
        Assert.Null (clicks1);
        Assert.Null (clicks2); // No state change (already pressed)
        Assert.Equal (1, clicks3); // Only single click because no complete press-release cycle
    }

    #endregion

    #region Button Index Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Theory]
    [InlineData (0, MouseFlags.Button1Pressed, MouseFlags.Button1Released)]
    [InlineData (1, MouseFlags.Button2Pressed, MouseFlags.Button2Released)]
    [InlineData (2, MouseFlags.Button3Pressed, MouseFlags.Button3Released)]
    [InlineData (3, MouseFlags.Button4Pressed, MouseFlags.Button4Released)]
    public void UpdateState_CorrectButtonIndex_TracksCorrectButton (int buttonIdx, MouseFlags pressedFlag, MouseFlags releasedFlag)
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker = new (() => currentTime, TimeSpan.FromMilliseconds (500), buttonIdx);

        Terminal.Gui.Input.Mouse press = new () { ScreenPosition = new (10, 10), Flags = pressedFlag };
        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = releasedFlag };

        // Act
        tracker.UpdateState (press, out int? clicks1);
        currentTime = currentTime.AddMilliseconds (50);
        tracker.UpdateState (release, out int? clicks2);

        // Assert
        Assert.Null (clicks1);
        Assert.Equal (1, clicks2);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void UpdateState_MultipleButtonsSimultaneous_EachTrackerIndependent ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseButtonClickTracker tracker1 = new (() => currentTime, TimeSpan.FromMilliseconds (500), 0); // Tracking Button1
        MouseButtonClickTracker tracker2 = new (() => currentTime, TimeSpan.FromMilliseconds (500), 1); // Tracking Button2

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button2Pressed };
        Terminal.Gui.Input.Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button2Released };

        // Act - Press Button1, press Button2, release Button1, release Button2
        tracker1.UpdateState (press1, out int? t1_press1);
        tracker2.UpdateState (press1, out int? t2_press1);

        currentTime = currentTime.AddMilliseconds (50);
        tracker1.UpdateState (press2, out int? t1_press2);
        tracker2.UpdateState (press2, out int? t2_press2);

        currentTime = currentTime.AddMilliseconds (50);
        tracker1.UpdateState (release1, out int? t1_release1);
        tracker2.UpdateState (release1, out int? t2_release1);

        currentTime = currentTime.AddMilliseconds (50);
        tracker1.UpdateState (release2, out int? t1_release2);
        tracker2.UpdateState (release2, out int? t2_release2);

        // Assert - This demonstrates the quirk: trackers detect state changes based on whether
        // their button is pressed in the flags, not whether the event is FOR their button
        Assert.Null (t1_press1); // Tracker1: Button1 pressed → state changes to Pressed
        Assert.Null (t2_press1); // Tracker2: sees Button1 press (Button2 not in flags) → Released (initial) to Released (no change)

        // When Button2 is pressed (Button1 not in flags), Tracker1 sees: Pressed (current) → Released (new)
        Assert.Equal (1, t1_press2); // Tracker1: detects Pressed→Released transition! Generates click
        Assert.Null (t2_press2); // Tracker2: Button2 pressed → state changes to Pressed

        // When Button1 is released, Tracker1 already thinks it's released, so no change
        Assert.Null (t1_release1); // Tracker1: Released→Released (no change)

        // Tracker2 sees Button1 release (Button2 not in flags), interprets as Pressed→Released
        Assert.Equal (1, t2_release1); // Tracker2: detects Pressed→Released transition! Generates click

        Assert.Null (t1_release2); // Tracker1: Released→Released (no change)
        Assert.Null (t2_release2); // Tracker2: already released from previous event, Released→Released (no change)

        // This demonstrates why MouseInterpreter uses separate trackers and only feeds each tracker
        // events that have its button pressed or released flags set
    }

    #endregion
}
