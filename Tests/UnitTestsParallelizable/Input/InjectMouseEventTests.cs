using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace DriverTests.MouseTests;

/// <summary>
///     Parallelizable unit tests for IInputProcessor.InjectMouseEvent.
///     Tests validate the entire pipeline: MouseEventArgs → TInputRecord → Queue → ProcessQueue → Events.
///     fully implemented in InputProcessorImpl (base class). Only WindowsInputProcessor has a working implementation.
/// </summary>
[Trait ("Category", "Input")]
public class InjectMouseEventTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Mouse Event Sequencing Tests

    [Fact]
    public void InjectMouseEvent_HandlesCompleteClickSequence ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Simulate a complete click: press → release
        processor.InjectMouseEvent (
                                     null,
                                     new ()
                                     {
                                         ScreenPosition = new (10, 5),
                                         Flags = MouseFlags.LeftButtonPressed
                                     });

        processor.InjectMouseEvent (
                                     null,
                                     new ()
                                     {
                                         ScreenPosition = new (10, 5),
                                         Flags = MouseFlags.LeftButtonReleased
                                     });

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert - Process() emits Pressed and Released immediately (clicks are deferred)
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonReleased));

        // We should also see the synthetic Clicked event
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonClicked));
        Assert.Equal (3, receivedEvents.Count);
    }

    #endregion

    #region Thread Safety Tests

    [Fact (Skip = "Thread safety test has race conditions - needs investigation")]
    public void InjectMouseEvent_IsThreadSafe ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        ConcurrentBag<Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        const int THREAD_COUNT = 10;
        const int EVENTS_PER_THREAD = 100;
        Thread [] threads = new Thread [THREAD_COUNT];

        // Act - Enqueue mouse events from multiple threads
        for (var t = 0; t < THREAD_COUNT; t++)
        {
            int threadId = t;

            threads [t] = new (() =>
                               {
                                   for (var i = 0; i < EVENTS_PER_THREAD; i++)
                                   {
                                       processor.InjectMouseEvent (
                                                                    null,
                                                                    new ()
                                                                    {
                                                                        Timestamp = DateTime.Now,
                                                                        ScreenPosition = new (threadId, i),
                                                                        Flags = MouseFlags.LeftButtonPressed
                                                                    });
                                   }
                               });
            threads [t].Start ();
        }

        // Wait for all threads to complete
        foreach (Thread thread in threads)
        {
            thread.Join ();
        }

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert
        // Note: This test has race conditions between enqueueing and processing
        // The ANSIInput queue may not have all events when SimulateInputThread runs
        Assert.Equal (THREAD_COUNT * EVENTS_PER_THREAD, receivedEvents.Count);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     Simulates the input thread by manually draining ANSIInput's internal queue
    ///     and moving items to the InputBuffer. This is needed because tests don't
    ///     start the actual input thread via Run().
    /// </summary>
    private static void SimulateInputThread (AnsiInput ansiInput, ConcurrentQueue<char> inputBuffer)
    {
        // ANSIInput's Peek() checks _testInput
        while (ansiInput.Peek ())
        {
            // Read() drains _testInput and returns items
            foreach (char item in ansiInput.Read ())
            {
                // Manually add to InputBuffer (simulating what Run() would do)
                inputBuffer.Enqueue (item);
            }
        }
    }

    #endregion

    #region InjectMouseEvent Tests

    [Fact]
    public void InjectMouseEvent_AddsSingleMouseEventToQueue ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Note: Click events don't survive ANSI encoding - they're synthetic events
        // generated by the processor. Use Pressed for round-trip testing.
        Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (10, 5), // ANSI mouse uses screen coordinates
            Flags = MouseFlags.LeftButtonPressed
        };

        // Act
        processor.InjectMouseEvent (null, mouse);

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert - Verify the mouse event made it through
        Assert.Single (receivedEvents);
        Assert.Equal (mouse.ScreenPosition, receivedEvents [0].ScreenPosition);
        Assert.Equal (mouse.Flags, receivedEvents [0].Flags);
    }

    [Fact (Skip = "Skip for now")]
    public void InjectMouseEvent_SupportsMultipleEvents ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        // Simulate the user pressing and releasing the mouse button. This should cause
        // 3 synthetic events: Pressed, Released, Clicked
        Mouse [] events =
        [
            new () { Timestamp = DateTime.Now, ScreenPosition = new (10, 5), Flags = MouseFlags.LeftButtonPressed },
            new () { Timestamp = DateTime.Now, ScreenPosition = new (10, 5), Flags = MouseFlags.LeftButtonReleased }
        ];

        List<Mouse> receivedParsedEvents = [];
        List<Mouse> receivedSyntheticEvents = [];

        // ANSIInputProcessor.InjectMouseEvent calls RaiseMouseEventParsed directly (bypasses queue)
        processor.MouseEventParsed += (_, e) => receivedParsedEvents.Add (e);
        processor.SyntheticMouseEvent += (_, e) => receivedSyntheticEvents.Add (e);

        // Act
        // Note: ANSIInputProcessor.InjectMouseEvent bypasses the queue and calls RaiseMouseEventParsed directly
        // This means SimulateInputThread and ProcessQueue are not needed for this test
        foreach (Mouse mouse in events)
        {
            processor.InjectMouseEvent (null, mouse);
        }

        // Assert
        // MouseEventParsed fires for all raw events (pressed, released)
        Assert.Contains (receivedParsedEvents, e => e.Flags == MouseFlags.LeftButtonPressed && e.ScreenPosition == new Point (10, 5));
        Assert.Contains (receivedParsedEvents, e => e.Flags == MouseFlags.LeftButtonReleased && e.ScreenPosition == new Point (10, 5));
        Assert.Equal (2, receivedParsedEvents.Count);

        Assert.Contains (receivedSyntheticEvents, e => e.Flags == MouseFlags.LeftButtonPressed && e.ScreenPosition == new Point (10, 5));
        Assert.Contains (receivedSyntheticEvents, e => e.Flags == MouseFlags.LeftButtonReleased && e.ScreenPosition == new Point (10, 5));
        Assert.Contains (receivedSyntheticEvents, e => e.Flags == MouseFlags.LeftButtonClicked && e.ScreenPosition == new Point (10, 5));
        Assert.Equal (4, receivedSyntheticEvents.Count);

        // Should have two LeftButtonClicked events
        Assert.Equal (2, receivedSyntheticEvents.Count (e => e.Flags == MouseFlags.LeftButtonClicked && e.ScreenPosition == new Point (10, 5)));
    }

    [Theory]
    [InlineData (MouseFlags.LeftButtonPressed)]
    [InlineData (MouseFlags.MiddleButtonPressed)]
    [InlineData (MouseFlags.RightButtonPressed)]

    // Note: Button4 is not part of the standard ANSI SGR mouse protocol (only 3 buttons: left, middle, right)
    // Note: Double/Triple clicks are synthetic events generated by the processor
    // and cannot be encoded in ANSI. Test the Press events that generate them.
    public void InjectMouseEvent_SupportsAllButtonPresses (MouseFlags flags)
    {
        // Arrange
        var ansiInput = new AnsiInput ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        var processor = new AnsiInputProcessor (queue, null);
        processor.InputImpl = ansiInput;

        Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (10, 5),
            Flags = flags
        };

        Mouse? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.InjectMouseEvent (null, mouse);
        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.NotNull (receivedEvent);
        Assert.Equal (flags, receivedEvent.Flags);
    }

    [Theory]
    [InlineData (0, 0)]
    [InlineData (10, 5)]
    [InlineData (79, 24)] // Near screen edge (assuming 80x25)
    [InlineData (100, 100)] // Beyond typical screen
    public void InjectMouseEvent_PreservesPosition (int x, int y)
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (x, y),
            Flags = MouseFlags.LeftButtonPressed
        };

        Mouse? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.InjectMouseEvent (null, mouse);
        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.NotNull (receivedEvent);
        Assert.Equal (x, receivedEvent.ScreenPosition.X);
        Assert.Equal (y, receivedEvent.ScreenPosition.Y);
    }

    [Theory]
    [InlineData (MouseFlags.Ctrl)]
    [InlineData (MouseFlags.Alt)]
    [InlineData (MouseFlags.Ctrl | MouseFlags.Alt)]

    // Note: Shift modifier encoding in ANSI mouse protocol is complex and doesn't always round-trip correctly
    // The AnsiMouseEncoder uses approximations for Shift combinations that may not match the parser exactly
    // [InlineData (MouseFlags.Shift)] // Known limitation
    // [InlineData (MouseFlags.Shift | MouseFlags.Ctrl)] // Known limitation 
    // [InlineData (MouseFlags.Shift | MouseFlags.Alt)] // Known limitation
    // [InlineData (MouseFlags.Shift | MouseFlags.Ctrl | MouseFlags.Alt)] // Known limitation
    public void InjectMouseEvent_PreservesModifiers (MouseFlags modifiers)
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (10, 5),
            Flags = MouseFlags.LeftButtonPressed | modifiers
        };

        Mouse? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.InjectMouseEvent (null, mouse);
        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.NotNull (receivedEvent);
        Assert.True (receivedEvent.Flags.HasFlag (MouseFlags.LeftButtonPressed));

        if (modifiers.HasFlag (MouseFlags.Ctrl))
        {
            Assert.True (receivedEvent.Flags.HasFlag (MouseFlags.Ctrl));
        }

        if (modifiers.HasFlag (MouseFlags.Alt))
        {
            Assert.True (receivedEvent.Flags.HasFlag (MouseFlags.Alt));
        }
    }

    [Theory]
    [InlineData (MouseFlags.WheeledUp)]
    [InlineData (MouseFlags.WheeledDown)]
    [InlineData (MouseFlags.WheeledLeft)]
    [InlineData (MouseFlags.WheeledRight)]
    public void InjectMouseEvent_SupportsMouseWheel (MouseFlags wheelFlag)
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (10, 5),
            Flags = wheelFlag
        };

        Mouse? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.InjectMouseEvent (null, mouse);
        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.NotNull (receivedEvent);
        Assert.True (receivedEvent.Flags.HasFlag (wheelFlag));
    }

    [Fact]
    public void InjectMouseEvent_SupportsMouseMove ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        Mouse [] events =
        [
            new () { Timestamp = DateTime.Now, ScreenPosition = new (0, 0), Flags = MouseFlags.PositionReport },
            new () { Timestamp = DateTime.Now, ScreenPosition = new (5, 5), Flags = MouseFlags.PositionReport },
            new () { Timestamp = DateTime.Now, ScreenPosition = new (10, 10), Flags = MouseFlags.PositionReport }
        ];

        // Act
        foreach (Mouse mouse in events)
        {
            processor.InjectMouseEvent (null, mouse);
        }

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.Equal (3, receivedEvents.Count);
        Assert.Equal (new (0, 0), receivedEvents [0].ScreenPosition);
        Assert.Equal (new (5, 5), receivedEvents [1].ScreenPosition);
        Assert.Equal (new (10, 10), receivedEvents [2].ScreenPosition);
    }

    #endregion

    #region InputProcessor Pipeline Tests

    [Fact]
    public void InputProcessor_InjectMouseEvent_DoesNotThrow ()
    {
        // Arrange
        ConcurrentQueue<char> queue = new ();
        AnsiInputProcessor processor = new (queue);

        // Don't set InputImpl (or set to non-testable)

        // Act & Assert - Should not throw even if not implemented
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     processor.InjectMouseEvent (
                                                                                  null,
                                                                                  new ()
                                                                                  {
                                                                                      Timestamp = DateTime.Now,
                                                                                      ScreenPosition = new (10, 5),
                                                                                      Flags = MouseFlags.LeftButtonClicked
                                                                                  });
                                                     processor.ProcessQueue ();
                                                 });

        // The base implementation logs a critical message but doesn't throw
        Assert.Null (exception);
    }

    [Fact (Skip = "Skip for now")]
    public void InputProcessor_ProcessQueue_DrainsPendingMouseEvents ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Enqueue multiple events before processing
        processor.InjectMouseEvent (null, new () { Timestamp = DateTime.Now, ScreenPosition = new (1, 1), Flags = MouseFlags.LeftButtonPressed });
        processor.InjectMouseEvent (null, new () { Timestamp = DateTime.Now, ScreenPosition = new (2, 2), Flags = MouseFlags.PositionReport });
        processor.InjectMouseEvent (null, new () { Timestamp = DateTime.Now, ScreenPosition = new (3, 3), Flags = MouseFlags.LeftButtonReleased });

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert - After processing, all events should be received
        Assert.Empty (queue);
        Assert.Equal (3, receivedEvents.Count);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void InjectMouseEvent_WithInvalidEvent_DoesNotThrow ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        // Act & Assert - Empty/default mouse event should not throw
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     processor.InjectMouseEvent (null, new ());
                                                     SimulateInputThread (ansiInput, queue);
                                                     processor.ProcessQueue ();
                                                 });

        Assert.Null (exception);
    }

    [Fact]
    public void InjectMouseEvent_WithNegativePosition_DoesNotThrow ()
    {
        // Arrange
        AnsiInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        AnsiInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        // Act & Assert - Negative positions should not throw
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     processor.InjectMouseEvent (
                                                                                  null,
                                                                                  new ()
                                                                                  {
                                                                                      Timestamp = DateTime.Now,
                                                                                      ScreenPosition = new (-10, -5),
                                                                                      Flags = MouseFlags.LeftButtonClicked
                                                                                  });
                                                     SimulateInputThread (ansiInput, queue);
                                                     processor.ProcessQueue ();
                                                 });

        Assert.Null (exception);
    }

    #endregion
}
