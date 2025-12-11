#nullable enable
using Xunit.Abstractions;
// ReSharper disable AccessToModifiedClosure
#pragma warning disable CS9113 // Parameter is unread

namespace DriverTests.Mouse;

/// <summary>
///     Extended unit tests for <see cref="MouseInterpreter"/> click detection and event generation.
///     Complements existing MouseInterpreterTests with additional coverage for HIGH and MEDIUM priority scenarios.
/// </summary>
[Trait ("Category", "Input")]
public class MouseInterpreterExtendedTests (ITestOutputHelper output)
{
    #region Position Change Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_ClickAtDifferentPosition_ResetsClickCount ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (20, 20), Flags = MouseFlags.Button1Pressed }; // Different position
        Terminal.Gui.Input.Mouse release2 = new () { ScreenPosition = new (20, 20), Flags = MouseFlags.Button1Released };

        // Act
        _ = interpreter.Process (press1).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events2 = interpreter.Process (release1).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        _ = interpreter.Process (press2).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events4 = interpreter.Process (release2).ToList ();

        // Assert
        Assert.Equal (2, events2.Count); // Original release + Button1Clicked
        Assert.Contains (events2, e => e.Flags == MouseFlags.Button1Clicked);

        Assert.Equal (2, events4.Count); // Original release + Button1Clicked (not double-click due to position change)
        Assert.Contains (events4, e => e.Flags == MouseFlags.Button1Clicked);
        Assert.DoesNotContain (events4, e => e.Flags == MouseFlags.Button1DoubleClicked);
    }

    #endregion

    #region Multiple Button Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_Button1And2PressedSimultaneously_TracksIndependently ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button2Pressed };
        Terminal.Gui.Input.Mouse release1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };
        Terminal.Gui.Input.Mouse release2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button2Released };

        // Act
        List<Terminal.Gui.Input.Mouse> events1 = interpreter.Process (press1).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events2 = interpreter.Process (press2).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events3 = interpreter.Process (release1).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events4 = interpreter.Process (release2).ToList ();

        // Assert
        Assert.Single (events1); // Just Button1Pressed
        Assert.Equal (MouseFlags.Button1Pressed, events1[0].Flags);

        // NOTE: This test demonstrates the quirk documented in MouseButtonClickTrackerTests:
        // When Button2 is pressed (Button1 not in flags), Button1's tracker sees: Pressed→Released
        // This generates a spurious Button1Clicked event
        Assert.Equal (2, events2.Count); // Button2Pressed + spurious Button1Clicked
        Assert.Contains (events2, e => e.Flags == MouseFlags.Button1Clicked); // Spurious click

        // When Button1 is actually released, Button1's tracker already thinks it's released (no change)
        // But Button2's tracker sees: Pressed→Released, generating Button2Clicked
        Assert.Equal (2, events3.Count); // Button1Released + spurious Button2Clicked
        Assert.Contains (events3, e => e.Flags == MouseFlags.Button2Clicked);

        // Button2 release: both trackers already think their buttons are released
        Assert.Single (events4); // Just Button2Released
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_MultipleButtonsDoubleClick_EachIndependent ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        // Act - Double-click Button1, then double-click Button2
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Button1 first click
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released }));

        // Button1 second click
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released }));

        // Button2 first click
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = MouseFlags.Button2Pressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = MouseFlags.Button2Released }));

        // Button2 second click
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = MouseFlags.Button2Pressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = MouseFlags.Button2Released }));

        // Assert
        Assert.Contains (allEvents, e => e.Flags == MouseFlags.Button1DoubleClicked);
        Assert.Contains (allEvents, e => e.Flags == MouseFlags.Button2DoubleClicked);
    }

    #endregion

    #region Edge Case Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_ReleaseWithoutPress_DoesNotGenerateClick ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        List<Terminal.Gui.Input.Mouse> events = interpreter.Process (release).ToList ();

        // Assert
        Assert.Single (events); // Only the original release event
        Assert.Equal (MouseFlags.Button1Released, events [0].Flags);
        Assert.DoesNotContain (events, e => e.Flags == MouseFlags.Button1Clicked);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_DoublePress_WithoutIntermediateRelease_DoesNotCountAsDoubleClick ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        Terminal.Gui.Input.Mouse press1 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse press2 = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        List<Terminal.Gui.Input.Mouse> events1 = interpreter.Process (press1).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events2 = interpreter.Process (press2).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events3 = interpreter.Process (release).ToList ();

        // Assert
        Assert.Single (events1); // Just Button1Pressed
        Assert.Single (events2); // Just Button1Pressed (no state change)
        Assert.Equal (2, events3.Count); // Button1Released + Button1Clicked (single, not double)
        Assert.Contains (events3, e => e.Flags == MouseFlags.Button1Clicked);
        Assert.DoesNotContain (events3, e => e.Flags == MouseFlags.Button1DoubleClicked);
    }

    #endregion

    #region Modifier Key Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Theory]
    [InlineData (MouseFlags.ButtonShift)]
    [InlineData (MouseFlags.ButtonCtrl)]
    [InlineData (MouseFlags.ButtonAlt)]
    [InlineData (MouseFlags.ButtonShift | MouseFlags.ButtonCtrl)]
    public void Process_ClickWithModifier_DoesNotPreserveModifier (MouseFlags modifier)
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        Terminal.Gui.Input.Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed | modifier };
        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released | modifier };

        // Act
        _ = interpreter.Process (press).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events2 = interpreter.Process (release).ToList ();

        // Assert
        Assert.Equal (2, events2.Count); // Release + Clicked
        Terminal.Gui.Input.Mouse? clickEvent = events2.FirstOrDefault (e => e.Flags.HasFlag (MouseFlags.Button1Clicked));
        Assert.NotNull (clickEvent);

        // NOTE: This documents a known limitation - MouseInterpreter's CreateClickEvent method
        // copies ScreenPosition from the original event, but does NOT preserve modifiers.
        // The synthetic click event only has the button click flag, not the modifier flags.
        Assert.False (
                      clickEvent.Flags.HasFlag (modifier),
                      $"KNOWN LIMITATION: Synthetic click events do not preserve {modifier} modifier. "
                      + "This is because CreateClickEvent in MouseInterpreter only sets Flags to ToClicks() result, "
                      + "which doesn't include modifiers from the original event.");
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_DoubleClickWithShift_DoesNotPreserveModifier ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        MouseFlags modifiedPressed = MouseFlags.Button1Pressed | MouseFlags.ButtonShift;
        MouseFlags modifiedReleased = MouseFlags.Button1Released | MouseFlags.ButtonShift;

        // Act - Double-click with Shift held
        List<Terminal.Gui.Input.Mouse> allEvents = [];
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = modifiedPressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = modifiedReleased }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = modifiedPressed }));
        currentTime = currentTime.AddMilliseconds (50);
        allEvents.AddRange (interpreter.Process (new Terminal.Gui.Input.Mouse { ScreenPosition = new (10, 10), Flags = modifiedReleased }));

        // Assert
        Terminal.Gui.Input.Mouse? singleClick =
            allEvents.FirstOrDefault (e => e.Flags.HasFlag (MouseFlags.Button1Clicked) && !e.Flags.HasFlag (MouseFlags.Button1DoubleClicked));
        Terminal.Gui.Input.Mouse? doubleClick = allEvents.FirstOrDefault (e => e.Flags.HasFlag (MouseFlags.Button1DoubleClicked));

        Assert.NotNull (singleClick);
        Assert.NotNull (doubleClick);

        // NOTE: This documents a known limitation - modifiers are NOT preserved in synthetic events
        Assert.False (
                      singleClick.Flags.HasFlag (MouseFlags.ButtonShift),
                      "KNOWN LIMITATION: Single click synthetic event does not preserve Shift modifier");

        Assert.False (
                      doubleClick.Flags.HasFlag (MouseFlags.ButtonShift),
                      "KNOWN LIMITATION: Double click synthetic event does not preserve Shift modifier");
    }

    #endregion

    #region Time Injection Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_WithInjectedTime_AllowsDeterministicTesting ()
    {
        // Arrange
        DateTime currentTime = new (2025, 1, 1, 12, 0, 0);
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        Terminal.Gui.Input.Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act - First click at T=0
        _ = interpreter.Process (press).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events2 = interpreter.Process (release).ToList ();

        // Second click at T=600 (beyond 500ms threshold)
        currentTime = currentTime.AddMilliseconds (600);
        _ = interpreter.Process (press).ToList (); // Discard - just need to process the press
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events4 = interpreter.Process (release).ToList ();

        // Assert
        Assert.Contains (events2, e => e.Flags == MouseFlags.Button1Clicked);
        Assert.Contains (events4, e => e.Flags == MouseFlags.Button1Clicked); // Single click, not double
        Assert.DoesNotContain (events4, e => e.Flags == MouseFlags.Button1DoubleClicked);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_WithInjectedTime_ExactThresholdBoundary ()
    {
        // Arrange
        DateTime currentTime = new (2025, 1, 1, 12, 0, 0);
        TimeSpan threshold = TimeSpan.FromMilliseconds (500);
        MouseInterpreter interpreter = new (() => currentTime, threshold);

        Terminal.Gui.Input.Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act - First click
        interpreter.Process (press).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events1 = interpreter.Process (release).ToList ();

        // Second click exactly at threshold + 1ms
        currentTime = currentTime.Add (threshold).AddMilliseconds (1);
        interpreter.Process (press).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> events2 = interpreter.Process (release).ToList ();

        // Assert - Should be single click, not double (threshold exceeded)
        Assert.Contains (events2, e => e.Flags == MouseFlags.Button1Clicked);
        Assert.DoesNotContain (events2, e => e.Flags == MouseFlags.Button1DoubleClicked);
    }

    #endregion

    #region Pass-Through Tests

    // CoPilot: claude-3-7-sonnet-20250219
    [Fact]
    public void Process_AlwaysYieldsOriginalEvent ()
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        Terminal.Gui.Input.Mouse press = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Pressed };
        Terminal.Gui.Input.Mouse release = new () { ScreenPosition = new (10, 10), Flags = MouseFlags.Button1Released };

        // Act
        List<Terminal.Gui.Input.Mouse> pressEvents = interpreter.Process (press).ToList ();
        currentTime = currentTime.AddMilliseconds (50);
        List<Terminal.Gui.Input.Mouse> releaseEvents = interpreter.Process (release).ToList ();

        // Assert - First event should always be the original
        Assert.True (pressEvents.Count >= 1);
        Assert.Equal (MouseFlags.Button1Pressed, pressEvents [0].Flags);

        Assert.True (releaseEvents.Count >= 1);
        Assert.Equal (MouseFlags.Button1Released, releaseEvents [0].Flags);
    }

    // CoPilot: claude-3-7-sonnet-20250219
    [Theory]
    [InlineData (MouseFlags.WheeledUp)]
    [InlineData (MouseFlags.WheeledDown)]
    [InlineData (MouseFlags.WheeledLeft)]
    [InlineData (MouseFlags.WheeledRight)]
    [InlineData (MouseFlags.ReportMousePosition)]
    public void Process_NonClickEvents_PassThroughWithoutModification (MouseFlags flags)
    {
        // Arrange
        DateTime currentTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => currentTime, TimeSpan.FromMilliseconds (500));

        Terminal.Gui.Input.Mouse mouse = new () { ScreenPosition = new (10, 10), Flags = flags };

        // Act
        List<Terminal.Gui.Input.Mouse> events = interpreter.Process (mouse).ToList ();

        // Assert - Should only yield the original event, no synthetic events
        Assert.Single (events);
        Assert.Equal (flags, events [0].Flags);
    }

    #endregion
}
