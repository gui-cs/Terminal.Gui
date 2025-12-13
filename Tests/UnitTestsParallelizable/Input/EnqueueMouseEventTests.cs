#nullable enable
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace DriverTests.MouseTests;

/// <summary>
///     Parallelizable unit tests for IInputProcessor.EnqueueMouseEvent.
///     Tests validate the entire pipeline: MouseEventArgs → TInputRecord → Queue → ProcessQueue → Events.
///     fully implemented in InputProcessorImpl (base class). Only WindowsInputProcessor has a working implementation.
/// </summary>
[Trait ("Category", "Input")]
public class EnqueueMouseEventTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Mouse Event Sequencing Tests

    [Fact]
    public void FakeInput_EnqueueMouseEvent_HandlesCompleteClickSequence ()
    {
        // Arrange
        FakeInput fakeInput = new ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        FakeInputProcessor processor = new (queue);
        processor.InputImpl = fakeInput;

        List<Terminal.Gui.Input.Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Simulate a complete click: press → release
        processor.EnqueueMouseEvent (
                                     null,
                                     new ()
                                     {
                                         ScreenPosition = new (10, 5),
                                         Flags = MouseFlags.LeftButtonPressed
                                     });

        processor.EnqueueMouseEvent (
                                     null,
                                     new ()
                                     {
                                         ScreenPosition = new (10, 5),
                                         Flags = MouseFlags.LeftButtonReleased
                                     });

        SimulateInputThread (fakeInput, queue);
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

    [Fact]
    public void FakeInput_EnqueueMouseEvent_IsThreadSafe ()
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        ConcurrentBag<Terminal.Gui.Input.Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        const int threadCount = 10;
        const int eventsPerThread = 100;
        Thread [] threads = new Thread [threadCount];

        // Act - Enqueue mouse events from multiple threads
        for (var t = 0; t < threadCount; t++)
        {
            int threadId = t;

            threads [t] = new (() =>
                               {
                                   for (var i = 0; i < eventsPerThread; i++)
                                   {
                                       processor.EnqueueMouseEvent (
                                                                    null,
                                                                    new ()
                                                                    {
                                                                        Timestamp = DateTime.Now,
                                                                        ScreenPosition = new (threadId, i),
                                                                        Flags = MouseFlags.LeftButtonClicked
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

        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.Equal (threadCount * eventsPerThread, receivedEvents.Count);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     Simulates the input thread by manually draining FakeInput's internal queue
    ///     and moving items to the InputBuffer. This is needed because tests don't
    ///     start the actual input thread via Run().
    /// </summary>
    private static void SimulateInputThread (FakeInput fakeInput, ConcurrentQueue<char> inputBuffer)
    {
        // FakeInput's Peek() checks _testInput
        while (fakeInput.Peek ())
        {
            // Read() drains _testInput and returns items
            foreach (char item in fakeInput.Read ())
            {
                // Manually add to InputBuffer (simulating what Run() would do)
                inputBuffer.Enqueue (item);
            }
        }
    }

    #endregion

    #region FakeInputProcessor EnqueueMouseEvent Tests

    [Fact]
    public void FakeInput_EnqueueMouseEvent_AddsSingleMouseEventToQueue ()
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        List<Terminal.Gui.Input.Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        Terminal.Gui.Input.Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (10, 5),  // ANSI mouse uses screen coordinates
            Flags = MouseFlags.LeftButtonClicked
        };

        // Act
        processor.EnqueueMouseEvent (null, mouse);

        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert - Verify the mouse event made it through
        Assert.Single (receivedEvents);
        Assert.Equal (mouse.ScreenPosition, receivedEvents [0].ScreenPosition);
        Assert.Equal (mouse.Flags, receivedEvents [0].Flags);
    }

    [Fact (Skip = "Skip for now")]
    public void FakeInput_EnqueueMouseEvent_SupportsMultipleEvents ()
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        // Simulate the user pressing and releasing the mouse button. This should cause
        // 3 synthetic events: Pressed, Released, Clicked
        Terminal.Gui.Input.Mouse [] events =
        [
            new () { Timestamp = DateTime.Now, ScreenPosition = new (10, 5), Flags = MouseFlags.LeftButtonPressed },
            new () { Timestamp = DateTime.Now, ScreenPosition = new (10, 5), Flags = MouseFlags.LeftButtonReleased },
        ];

        List<Terminal.Gui.Input.Mouse> receivedParsedEvents = [];
        List<Terminal.Gui.Input.Mouse> receivedSyntheticEvents = [];

        // FakeInputProcessor.EnqueueMouseEvent calls RaiseMouseEventParsed directly (bypasses queue)
        processor.MouseEventParsed += (_, e) => receivedParsedEvents.Add (e);
        processor.SyntheticMouseEvent += (_, e) => receivedSyntheticEvents.Add (e);

        // Act
        // Note: FakeInputProcessor.EnqueueMouseEvent bypasses the queue and calls RaiseMouseEventParsed directly
        // This means SimulateInputThread and ProcessQueue are not needed for this test
        foreach (Terminal.Gui.Input.Mouse mouse in events)
        {
            processor.EnqueueMouseEvent (null, mouse);
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
    [InlineData (MouseFlags.LeftButtonClicked)]
    [InlineData (MouseFlags.MiddleButtonClicked)]
    [InlineData (MouseFlags.RightButtonClicked)]
    [InlineData (MouseFlags.Button4Clicked)]
    [InlineData (MouseFlags.LeftButtonDoubleClicked)]
    [InlineData (MouseFlags.LeftButtonTripleClicked)]
    public void FakeInput_EnqueueMouseEvent_SupportsAllButtonClicks (MouseFlags flags)
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        Terminal.Gui.Input.Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (10, 5),
            Flags = flags
        };

        Terminal.Gui.Input.Mouse? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.EnqueueMouseEvent (null, mouse);
        SimulateInputThread (fakeInput, queue);
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
    public void FakeInput_EnqueueMouseEvent_PreservesPosition (int x, int y)
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        Terminal.Gui.Input.Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (x, y),
            Flags = MouseFlags.LeftButtonClicked
        };

        Terminal.Gui.Input.Mouse? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.EnqueueMouseEvent (null, mouse);
        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.NotNull (receivedEvent);
        Assert.Equal (x, receivedEvent.ScreenPosition.X);
        Assert.Equal (y, receivedEvent.ScreenPosition.Y);
    }

    [Theory]
    [InlineData (MouseFlags.Shift)]
    [InlineData (MouseFlags.Ctrl)]
    [InlineData (MouseFlags.Alt)]
    [InlineData (MouseFlags.Shift | MouseFlags.Ctrl)]
    [InlineData (MouseFlags.Shift | MouseFlags.Alt)]
    [InlineData (MouseFlags.Ctrl | MouseFlags.Alt)]
    [InlineData (MouseFlags.Shift | MouseFlags.Ctrl | MouseFlags.Alt)]
    public void FakeInput_EnqueueMouseEvent_PreservesModifiers (MouseFlags modifiers)
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        Terminal.Gui.Input.Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (10, 5),
            Flags = MouseFlags.LeftButtonClicked | modifiers
        };

        Terminal.Gui.Input.Mouse? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.EnqueueMouseEvent (null, mouse);
        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.NotNull (receivedEvent);
        Assert.True (receivedEvent.Flags.HasFlag (MouseFlags.LeftButtonClicked));

        if (modifiers.HasFlag (MouseFlags.Shift))
        {
            Assert.True (receivedEvent.Flags.HasFlag (MouseFlags.Shift));
        }

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
    public void FakeInput_EnqueueMouseEvent_SupportsMouseWheel (MouseFlags wheelFlag)
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        Terminal.Gui.Input.Mouse mouse = new ()
        {
            Timestamp = DateTime.Now,
            ScreenPosition = new (10, 5),
            Flags = wheelFlag
        };

        Terminal.Gui.Input.Mouse? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.EnqueueMouseEvent (null, mouse);
        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.NotNull (receivedEvent);
        Assert.True (receivedEvent.Flags.HasFlag (wheelFlag));
    }

    [Fact]
    public void FakeInput_EnqueueMouseEvent_SupportsMouseMove ()
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        List<Terminal.Gui.Input.Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        Terminal.Gui.Input.Mouse [] events =
        [
            new () { Timestamp = DateTime.Now, ScreenPosition = new (0, 0), Flags = MouseFlags.PositionReport },
            new () { Timestamp = DateTime.Now, ScreenPosition = new (5, 5), Flags = MouseFlags.PositionReport },
            new () { Timestamp = DateTime.Now, ScreenPosition = new (10, 10), Flags = MouseFlags.PositionReport }
        ];

        // Act
        foreach (Terminal.Gui.Input.Mouse mouse in events)
        {
            processor.EnqueueMouseEvent (null, mouse);
        }

        SimulateInputThread (fakeInput, queue);
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
    public void InputProcessor_EnqueueMouseEvent_DoesNotThrow ()
    {
        // Arrange
        ConcurrentQueue<char> queue = new ();
        var processor = new FakeInputProcessor (queue);

        // Don't set InputImpl (or set to non-testable)

        // Act & Assert - Should not throw even if not implemented
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     processor.EnqueueMouseEvent (
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
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        List<Terminal.Gui.Input.Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Enqueue multiple events before processing
        processor.EnqueueMouseEvent (null, new () { Timestamp = DateTime.Now, ScreenPosition = new (1, 1), Flags = MouseFlags.LeftButtonPressed });
        processor.EnqueueMouseEvent (null, new () { Timestamp = DateTime.Now, ScreenPosition = new (2, 2), Flags = MouseFlags.PositionReport });
        processor.EnqueueMouseEvent (null, new () { Timestamp = DateTime.Now, ScreenPosition = new (3, 3), Flags = MouseFlags.LeftButtonReleased });

        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert - After processing, all events should be received
        Assert.Empty (queue);
        Assert.Equal (3, receivedEvents.Count);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void FakeInput_EnqueueMouseEvent_WithInvalidEvent_DoesNotThrow ()
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        // Act & Assert - Empty/default mouse event should not throw
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     processor.EnqueueMouseEvent (null, new ());
                                                     SimulateInputThread (fakeInput, queue);
                                                     processor.ProcessQueue ();
                                                 });

        Assert.Null (exception);
    }

    [Fact]
    public void FakeInput_EnqueueMouseEvent_WithNegativePosition_DoesNotThrow ()
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<char> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        // Act & Assert - Negative positions should not throw
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     processor.EnqueueMouseEvent (
                                                                                  null,
                                                                                  new ()
                                                                                  {
                                                                                      Timestamp = DateTime.Now,
                                                                                      ScreenPosition = new (-10, -5),
                                                                                      Flags = MouseFlags.LeftButtonClicked
                                                                                  });
                                                     SimulateInputThread (fakeInput, queue);
                                                     processor.ProcessQueue ();
                                                 });

        Assert.Null (exception);
    }

    #endregion
}
