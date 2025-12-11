// ReSharper disable AccessToModifiedClosure
#nullable disable
namespace DriverTests.MouseTests;

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
                new () { Flags = MouseFlags.LeftButtonPressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.LeftButtonClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.LeftButtonPressed },
                new (),
                new () { Flags = MouseFlags.LeftButtonPressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.LeftButtonClicked, null, MouseFlags.LeftButtonDoubleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.LeftButtonPressed },
                new (),
                new () { Flags = MouseFlags.LeftButtonPressed },
                new (),
                new () { Flags = MouseFlags.LeftButtonPressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.LeftButtonClicked, null, MouseFlags.LeftButtonDoubleClicked, null, MouseFlags.LeftButtonTripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.MiddleButtonPressed },
                new (),
                new () { Flags = MouseFlags.MiddleButtonPressed },
                new (),
                new () { Flags = MouseFlags.MiddleButtonPressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.MiddleButtonClicked, null, MouseFlags.MiddleButtonDoubleClicked, null, MouseFlags.MiddleButtonTripleClicked }
        ];

        yield return
        [
            new List<Terminal.Gui.Input.Mouse>
            {
                new () { Flags = MouseFlags.RightButtonPressed },
                new (),
                new () { Flags = MouseFlags.RightButtonPressed },
                new (),
                new () { Flags = MouseFlags.RightButtonPressed },
                new ()
            },
            new MouseFlags? [] { null, MouseFlags.RightButtonClicked, null, MouseFlags.RightButtonDoubleClicked, null, MouseFlags.RightButtonTripleClicked }
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
                new () { Flags = MouseFlags.LeftButtonPressed, Position = new (10, 11) },
                new () { Position = new (10, 11) },

                // Clicking the line below means no double click because it's a different location
                new () { Flags = MouseFlags.LeftButtonPressed, Position = new (10, 12) },
                new () { Position = new (10, 12) }
            },
            new MouseFlags? [] { null, MouseFlags.LeftButtonClicked, null, MouseFlags.LeftButtonClicked } //release is click because new position
        ];
    }

    /// <summary>
    ///     Tests the EXACT sequence of events for a double-click.
    ///     With immediate click emission, Process() DOES emit click events immediately.
    ///     First release emits Button1Clicked, second release emits Button1DoubleClicked.
    /// </summary>
    /// <remarks>
    ///     Updated for immediate click emission (fix for Issue #4471).
    /// </remarks>
    [Fact]
    public void DoubleClick_ShouldEmitBothClickedAndDoubleClicked ()
    {
        // Arrange
        DateTime mockTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => mockTime, TimeSpan.FromMilliseconds (500));
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a double-click: Press, Release, Press, Release
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (10, 10) }));

        // Assert - Extract only the synthetic click events (not pressed/released)
        List<Terminal.Gui.Input.Mouse> clickEvents = allEvents
                                                     .Where (e => e.Flags.HasFlag (MouseFlags.LeftButtonClicked)
                                                                  || e.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked)
                                                                  || e.Flags.HasFlag (MouseFlags.LeftButtonTripleClicked))
                                                     .ToList ();

        // With immediate emission, we get BOTH Clicked and DoubleClicked
        Assert.Equal (2, clickEvents.Count);
        Assert.Equal (MouseFlags.LeftButtonClicked, clickEvents [0].Flags);
        Assert.Equal (MouseFlags.LeftButtonDoubleClicked, clickEvents [1].Flags);

        // CheckForExpiredClicks should now return nothing (clicks emitted immediately)
        List<Terminal.Gui.Input.Mouse> expiredClickEvents = interpreter.CheckForExpiredClicks ().ToList ();
        Assert.Empty (expiredClickEvents);
    }

    /// <summary>
    ///     Tests the EXACT sequence of events for a triple-click.
    ///     With immediate click emission, we get Clicked, DoubleClicked, and TripleClicked.
    /// </summary>
    /// <remarks>
    ///     Updated for immediate click emission (fix for Issue #4471).
    /// </remarks>
    [Fact]
    public void TripleClick_ShouldEmitAllThreeClickEvents ()
    {
        // Arrange
        MouseInterpreter interpreter = new ();
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a triple-click: Press, Release, Press, Release, Press, Release
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (10, 10) }));

        // Assert - Extract only the synthetic click events
        List<Terminal.Gui.Input.Mouse> clickEvents = allEvents
                                                     .Where (e => e.Flags.HasFlag (MouseFlags.LeftButtonClicked)
                                                                  || e.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked)
                                                                  || e.Flags.HasFlag (MouseFlags.LeftButtonTripleClicked))
                                                     .ToList ();

        // With immediate emission, we get ALL THREE click events
        Assert.Equal (3, clickEvents.Count);
        Assert.Equal (MouseFlags.LeftButtonClicked, clickEvents [0].Flags);
        Assert.Equal (MouseFlags.LeftButtonDoubleClicked, clickEvents [1].Flags);
        Assert.Equal (MouseFlags.LeftButtonTripleClicked, clickEvents [2].Flags);
    }

    /// <summary>
    ///     Tests that a single isolated click emits Clicked immediately (no delay).
    /// </summary>
    /// <remarks>
    ///     Updated for immediate click emission (fix for Issue #4471).
    /// </remarks>
    [Fact]
    public void SingleClick_ShouldEmitClickedImmediately ()
    {
        // Arrange
        DateTime mockTime = DateTime.Now;

        MouseInterpreter interpreter = new (
                                            () => mockTime,
                                            TimeSpan.FromMilliseconds (500)
                                           );
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a single click
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (10, 10) }));

        // Assert - With immediate emission, click event should be emitted right away
        List<Terminal.Gui.Input.Mouse> immediateClickEvents = allEvents.Where (e => e.Flags.HasFlag (MouseFlags.LeftButtonClicked)).ToList ();

        // NEW (correct) behavior: immediateClickEvents.Count == 1
        Assert.Single (immediateClickEvents);
        Assert.Equal (MouseFlags.LeftButtonClicked, immediateClickEvents [0].Flags);

        // CheckForExpiredClicks should return nothing (clicks already emitted)
        List<Terminal.Gui.Input.Mouse> expiredClickEvents = interpreter.CheckForExpiredClicks ().ToList ();
        Assert.Empty (expiredClickEvents);
    }

    /// <summary>
    ///     Tests the exact event order for a complete double-click sequence.
    ///     With immediate click emission, click events ARE emitted from Process().
    /// </summary>
    /// <remarks>
    ///     Updated for immediate click emission (fix for Issue #4471).
    /// </remarks>
    [Fact]
    public void DoubleClick_EventSequence_ShouldBeCorrect ()
    {
        // Arrange
        MouseInterpreter interpreter = new ();
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a double-click
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (10, 10) }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (10, 10) }));

        // Assert - Verify exact sequence (WITH click events, emitted immediately)
        // Expected: Pressed, Released, Clicked, Pressed, Released, DoubleClicked
        Assert.Equal (6, allEvents.Count);
        Assert.Equal (MouseFlags.LeftButtonPressed, allEvents [0].Flags);
        Assert.Equal (MouseFlags.LeftButtonReleased, allEvents [1].Flags);
        Assert.Equal (MouseFlags.LeftButtonClicked, allEvents [2].Flags);
        Assert.Equal (MouseFlags.LeftButtonPressed, allEvents [3].Flags);
        Assert.Equal (MouseFlags.LeftButtonReleased, allEvents [4].Flags);
        Assert.Equal (MouseFlags.LeftButtonDoubleClicked, allEvents [5].Flags);
    }

    /// <summary>
    ///     Tests that a double-click sequence emits both Clicked and DoubleClicked events.
    ///     This captures the NEW behavior where clicks are emitted immediately.
    /// </summary>
    /// <remarks>
    ///     Updated for immediate click emission (fix for Issue #4471).
    /// </remarks>
    [Fact]
    public void DoubleClick_ShouldEmitClickedThenDoubleClicked ()
    {
        // Arrange
        DateTime mockTime = DateTime.Now;
        MouseInterpreter interpreter = new (() => mockTime, TimeSpan.FromMilliseconds (500));
        List<Terminal.Gui.Input.Mouse> allEvents = [];

        // Act - Simulate a double-click at the same position
        Point pos = new (10, 10);
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = pos }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = pos }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = pos }));
        allEvents.AddRange (interpreter.Process (new () { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = pos }));

        // Get the index of each event type
        List<int> pressedIndices = allEvents.Select ((e, i) => new { e, i }).Where (x => x.e.Flags == MouseFlags.LeftButtonPressed).Select (x => x.i).ToList ();
        List<int> releasedIndices = allEvents.Select ((e, i) => new { e, i }).Where (x => x.e.Flags == MouseFlags.LeftButtonReleased).Select (x => x.i).ToList ();
        List<int> clickedIndices = allEvents.Select ((e, i) => new { e, i }).Where (x => x.e.Flags == MouseFlags.LeftButtonClicked).Select (x => x.i).ToList ();

        List<int> doubleClickedIndices = allEvents.Select ((e, i) => new { e, i })
                                                  .Where (x => x.e.Flags == MouseFlags.LeftButtonDoubleClicked)
                                                  .Select (x => x.i)
                                                  .ToList ();

        // Assert - With immediate emission, we get BOTH Clicked and DoubleClicked from Process()
        Assert.Single (clickedIndices);
        Assert.Single (doubleClickedIndices);

        // Verify order: Pressed, Released, Clicked, Pressed, Released, DoubleClicked
        Assert.Equal (0, pressedIndices [0]);
        Assert.Equal (1, releasedIndices [0]);
        Assert.Equal (2, clickedIndices [0]);
        Assert.Equal (3, pressedIndices [1]);
        Assert.Equal (4, releasedIndices [1]);
        Assert.Equal (5, doubleClickedIndices [0]);

        // CheckForExpiredClicks should return nothing (clicks already emitted)
        List<Terminal.Gui.Input.Mouse> expiredClickEvents = interpreter.CheckForExpiredClicks ().ToList ();
        Assert.Empty (expiredClickEvents);
    }
}
