// ReSharper disable AccessToModifiedClosure
#nullable disable
namespace DriverTests.Mouse;

public class MouseInterpreterTests
{
    [Theory]
    [MemberData (nameof (SequenceTests))]
    public void TestMouseEventSequences_InterpretedOnlyAsFlag (List<Terminal.Gui.Input.Mouse> events, params MouseFlags? [] expected)
    {
        // Arrange: Mock dependencies and set up the interpreter
        MouseInterpreter interpreter = new ();

        // Collect all results from processing the event sequence
        List<Terminal.Gui.Input.Mouse> allResults = [];

        // Act
        foreach (Terminal.Gui.Input.Mouse mouse in events)
        {
            allResults.AddRange (interpreter.Process (mouse));
        }

        // Assert - verify all expected click events were generated
        foreach (MouseFlags? expectedClick in expected.Where (e => e != null))
        {
            Assert.Contains (allResults, e => e.Flags == expectedClick);
        }

        // Also verify all original input events were passed through
        foreach (Terminal.Gui.Input.Mouse inputEvent in events)
        {
            Assert.Contains (allResults, e => e.Flags == inputEvent.Flags);
        }
    }

    public static IEnumerable<object []> SequenceTests ()
    {
        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button1Pressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.Button1Clicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button1Pressed },
                new (),
                new () { Flags = MouseFlags.Button1Pressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.Button1Clicked, null, MouseFlags.Button1DoubleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button1Pressed },
                new (),
                new () { Flags = MouseFlags.Button1Pressed },
                new (),
                new () { Flags = MouseFlags.Button1Pressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.Button1Clicked, null, MouseFlags.Button1DoubleClicked, null, MouseFlags.Button1TripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button2Pressed },
                new (),
                new () { Flags = MouseFlags.Button2Pressed },
                new (),
                new () { Flags = MouseFlags.Button2Pressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.Button2Clicked, null, MouseFlags.Button2DoubleClicked, null, MouseFlags.Button2TripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button3Pressed },
                new (),
                new () { Flags = MouseFlags.Button3Pressed },
                new (),
                new () { Flags = MouseFlags.Button3Pressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.Button3Clicked, null, MouseFlags.Button3DoubleClicked, null, MouseFlags.Button3TripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button4Pressed },
                new (),
                new () { Flags = MouseFlags.Button4Pressed },
                new (),
                new () { Flags = MouseFlags.Button4Pressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.Button4Clicked, null, MouseFlags.Button4DoubleClicked, null, MouseFlags.Button4TripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.Button1Pressed, Position = new (10, 11) },
                new () { Position = new (10, 11) },

                // Clicking the line below means no double click because it's a different location
                new () { Flags = MouseFlags.Button1Pressed, Position = new (10, 12) },
                new () { Position = new (10, 12) }
            },
            new MouseFlags? [] { null, MouseFlags.Button1Clicked, null, MouseFlags.Button1Clicked } //release is click because new position
        ];
    }

    /// <summary>
    ///     Tests the EXACT sequence of events for a double-click.
    ///     With deferred clicks, Process() should not emit any click events.
    ///     The final DoubleClicked event is retrieved via CheckForExpiredClicks().
    /// </summary>
    /// <remarks>
    ///     CoPilot - GitHub Copilot Edits
    /// </remarks>
    [Fact]
    public void DoubleClick_ShouldNotEmitSingleClick_BeforeDoubleClick ()
    {
        // Arrange
        DateTime mockTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => mockTime, TimeSpan.FromMilliseconds (500));
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a double-click: Press, Release, Press, Release
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = new (10, 10) }));

        // Assert - Extract only the synthetic click events (not pressed/released)
        List<Terminal.Gui.Input.Mouse> clickEvents = allEvents
                                                     .Where (e => e.Flags.HasFlag (MouseFlags.Button1Clicked)
                                                                  || e.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
                                                                  || e.Flags.HasFlag (MouseFlags.Button1TripleClicked))
                                                     .ToList ();

        // Process() should not emit any click events (all deferred)
        Assert.Empty (clickEvents);

        // Advance time beyond threshold
        mockTime = mockTime.Add (TimeSpan.FromMilliseconds (600));

        // Check for expired clicks - should get DoubleClicked
        List<Terminal.Gui.Input.Mouse> expiredClickEvents = interpreter.CheckForExpiredClicks ().ToList ();
        Assert.Single (expiredClickEvents);
        Assert.Equal (MouseFlags.Button1DoubleClicked, expiredClickEvents [0].Flags);
    }

    /// <summary>
    ///     Tests the EXACT sequence of events for a triple-click.
    ///     With deferred clicks, we should get ONLY TripleClicked after threshold expires.
    /// </summary>
    /// <remarks>
    ///     CoPilot - GitHub Copilot Edits
    /// </remarks>
    [Fact]
    public void TripleClick_ShouldNotEmitSingleOrDoubleClick_BeforeTripleClick ()
    {
        // Arrange
        MouseInterpreter interpreter = new ();
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a triple-click: Press, Release, Press, Release, Press, Release
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = new (10, 10) }));

        // Assert - Extract only the synthetic click events
        List<Terminal.Gui.Input.Mouse> clickEvents = allEvents
                                                     .Where (e => e.Flags.HasFlag (MouseFlags.Button1Clicked)
                                                                  || e.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
                                                                  || e.Flags.HasFlag (MouseFlags.Button1TripleClicked))
                                                     .ToList ();

        // For a triple-click with deferred clicks, NO click events should be emitted from Process()
        // ALL clicks are deferred and must be retrieved via CheckForExpiredClicks()
        Assert.Empty (clickEvents);
    }

    /// <summary>
    ///     Tests that a single isolated click eventually emits Clicked after the threshold expires.
    ///     This test requires a mechanism to check for expired pending clicks.
    /// </summary>
    /// <remarks>
    ///     CoPilot - GitHub Copilot Edits
    /// </remarks>
    [Fact]
    public void SingleClick_ShouldEmitClicked_AfterThresholdExpires ()
    {
        // Arrange
        DateTime mockTime = DateTime.Now;

        MouseInterpreter interpreter = new (
                                            () => mockTime,
                                            TimeSpan.FromMilliseconds (500)
                                           );
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a single click
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = new (10, 10) }));

        // At this point, with deferred implementation, NO click event should be emitted yet
        List<Terminal.Gui.Input.Mouse> immediateClickEvents = allEvents.Where (e => e.Flags.HasFlag (MouseFlags.Button1Clicked)).ToList ();

        // Current (incorrect) behavior: immediateClickEvents.Count == 1
        // Expected (correct) behavior: immediateClickEvents.Count == 0
        Assert.Empty (immediateClickEvents);

        // Advance time beyond threshold
        mockTime = mockTime.Add (TimeSpan.FromMilliseconds (600));

        // Check for expired clicks
        List<Terminal.Gui.Input.Mouse> expiredClickEvents = interpreter.CheckForExpiredClicks ().ToList ();
        Assert.Single (expiredClickEvents);
        Assert.Equal (MouseFlags.Button1Clicked, expiredClickEvents [0].Flags);
    }

    /// <summary>
    ///     Tests the exact event order for a complete double-click sequence.
    ///     With deferred clicks, NO click events should be emitted from Process().
    /// </summary>
    /// <remarks>
    ///     CoPilot - GitHub Copilot Edits
    /// </remarks>
    [Fact]
    public void DoubleClick_EventSequence_ShouldBeCorrect ()
    {
        // Arrange
        MouseInterpreter interpreter = new ();
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a double-click
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = new (10, 10) }));

        // Assert - Verify exact sequence (NO click events, all deferred)
        // Expected: Pressed, Released, Pressed, Released
        Assert.Equal (4, allEvents.Count);
        Assert.Equal (MouseFlags.Button1Pressed, allEvents [0].Flags);
        Assert.Equal (MouseFlags.Button1Released, allEvents [1].Flags);
        Assert.Equal (MouseFlags.Button1Pressed, allEvents [2].Flags);
        Assert.Equal (MouseFlags.Button1Released, allEvents [3].Flags);
    }

    /// <summary>
    ///     This test captures the exact issue shown in the screenshot/bug report.
    ///     With deferred clicks, Process() emits: Pressed, Released, Pressed, Released
    ///     The DoubleClicked is retrieved via CheckForExpiredClicks() after threshold.
    /// </summary>
    /// <remarks>
    ///     CoPilot - GitHub Copilot Edits
    /// </remarks>
    [Fact]
    public void DoubleClick_ShouldNotHaveClickedBetweenReleases ()
    {
        // Arrange
        DateTime mockTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => mockTime, TimeSpan.FromMilliseconds (500));
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a double-click at the same position
        Point pos = new (10, 10);
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = pos }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = pos }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Pressed, ScreenPosition = pos }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.Button1Released, ScreenPosition = pos }));

        // Get the index of each event type
        List<int> pressedIndices = allEvents.Select ((e, i) => new { e, i }).Where (x => x.e.Flags == MouseFlags.Button1Pressed).Select (x => x.i).ToList ();
        List<int> releasedIndices = allEvents.Select ((e, i) => new { e, i }).Where (x => x.e.Flags == MouseFlags.Button1Released).Select (x => x.i).ToList ();
        List<int> clickedIndices = allEvents.Select ((e, i) => new { e, i }).Where (x => x.e.Flags == MouseFlags.Button1Clicked).Select (x => x.i).ToList ();

        List<int> doubleClickedIndices = allEvents.Select ((e, i) => new { e, i })
                                                  .Where (x => x.e.Flags == MouseFlags.Button1DoubleClicked)
                                                  .Select (x => x.i)
                                                  .ToList ();

        // Assert - There should be NO Clicked or DoubleClicked events from Process()
        Assert.Empty (clickedIndices);
        Assert.Empty (doubleClickedIndices);

        // Advance time beyond threshold
        mockTime = mockTime.Add (TimeSpan.FromMilliseconds (600));

        // Check for expired clicks - should get DoubleClicked
        List<Terminal.Gui.Input.Mouse> expiredClickEvents = interpreter.CheckForExpiredClicks ().ToList ();
        Assert.Single (expiredClickEvents);
        Assert.Equal (MouseFlags.Button1DoubleClicked, expiredClickEvents [0].Flags);
    }
}
