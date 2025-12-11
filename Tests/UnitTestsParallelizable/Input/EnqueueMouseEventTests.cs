#nullable enable
using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace DriverTests.Mouse;

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
        var fakeInput = new FakeInput ();
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        List<MouseEventArgs> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Simulate a complete click: press → release → click
        processor.EnqueueMouseEvent (
                                     null,
                                     new ()
                                     {
                                         Timestamp = DateTime.Now,
                                         Position = new (10, 5),
                                         Flags = MouseFlags.LeftButtonPressed
                                     });

        processor.EnqueueMouseEvent (
                                     null,
                                     new ()
                                     {
                                         Timestamp = DateTime.Now,
                                         Position = new (10, 5),
                                         Flags = MouseFlags.LeftButtonReleased
                                     });

        // The MouseInterpreter in the processor should synthesize a clicked event
        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert
        // We should see whole synthetic sequence: Pressed, Released, Clicked
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonReleased));
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
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        ConcurrentBag<MouseEventArgs> receivedEvents = [];
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
                                                                        Position = new (threadId, i),
                                                                        Flags = MouseFlags.Button1Clicked
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
    private static void SimulateInputThread (FakeInput fakeInput, ConcurrentQueue<ConsoleKeyInfo> inputBuffer)
    {
        // FakeInput's Peek() checks _testInput
        while (fakeInput.Peek ())
        {
            // Read() drains _testInput and returns items
            foreach (ConsoleKeyInfo item in fakeInput.Read ())
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
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        List<MouseEventArgs> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        MouseEventArgs mouseEvent = new ()
        {
            Timestamp = DateTime.Now,
            Position = new (10, 5),
            Flags = MouseFlags.Button1Clicked
        };

        // Act
        processor.EnqueueMouseEvent (null, mouseEvent);

        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert - Verify the mouse event made it through
        Assert.Single (receivedEvents);
        Assert.Equal (mouseEvent.Position, receivedEvents [0].Position);
        Assert.Equal (mouseEvent.Flags, receivedEvents [0].Flags);
    }

    [Fact (Skip = "Skip for now")]
    public void FakeInput_EnqueueMouseEvent_SupportsMultipleEvents ()
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        // Simulate the user pressing and releasing the mouse button. This should cause
        // 3 synthetic events: Pressed, Released, Clicked
        MouseEventArgs [] events =
        [
            new () { Timestamp = DateTime.Now, Position = new (10, 5), Flags = MouseFlags.Button1Pressed },
            new () { Timestamp = DateTime.Now, Position = new (10, 5), Flags = MouseFlags.Button1Released },
        ];

        List<MouseEventArgs> receivedParsedEvents = [];
        List<MouseEventArgs> receivedSyntheticEvents = [];

        // FakeInputProcessor.EnqueueMouseEvent calls RaiseMouseEventParsed directly (bypasses queue)
        processor.MouseEventParsed += (_, e) => receivedParsedEvents.Add (e);
        processor.SyntheticMouseEvent += (_, e) => receivedSyntheticEvents.Add (e);

        // Act
        // Note: FakeInputProcessor.EnqueueMouseEvent bypasses the queue and calls RaiseMouseEventParsed directly
        // This means SimulateInputThread and ProcessQueue are not needed for this test
        foreach (MouseEventArgs mouseEvent in events)
        {
            processor.EnqueueMouseEvent (null, mouseEvent);
        }

        // Assert
        // MouseEventParsed fires for all raw events (pressed, released)
        Assert.Contains (receivedParsedEvents, e => e.Flags == MouseFlags.Button1Pressed && e.Position == new Point (10, 5));
        Assert.Contains (receivedParsedEvents, e => e.Flags == MouseFlags.Button1Released && e.Position == new Point (10, 5));
        Assert.Equal (2, receivedParsedEvents.Count);

        Assert.Contains (receivedSyntheticEvents, e => e.Flags == MouseFlags.Button1Pressed && e.Position == new Point (10, 5));
        Assert.Contains (receivedSyntheticEvents, e => e.Flags == MouseFlags.Button1Released && e.Position == new Point (10, 5));
        Assert.Contains (receivedSyntheticEvents, e => e.Flags == MouseFlags.Button1Clicked && e.Position == new Point (10, 5));
        Assert.Equal (4, receivedSyntheticEvents.Count);

        // Should have two Button1Clicked events
        Assert.Equal (2, receivedSyntheticEvents.Count (e => e.Flags == MouseFlags.Button1Clicked && e.Position == new Point (10, 5)));
    }

    [Theory]
    [InlineData (MouseFlags.Button1Clicked)]
    [InlineData (MouseFlags.Button2Clicked)]
    [InlineData (MouseFlags.Button3Clicked)]
    [InlineData (MouseFlags.Button4Clicked)]
    [InlineData (MouseFlags.Button1DoubleClicked)]
    [InlineData (MouseFlags.Button1TripleClicked)]
    public void FakeInput_EnqueueMouseEvent_SupportsAllButtonClicks (MouseFlags flags)
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        MouseEventArgs mouseEvent = new ()
        {
            Timestamp = DateTime.Now,
            Position = new (10, 5),
            Flags = flags
        };

        MouseEventArgs? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.EnqueueMouseEvent (null, mouseEvent);
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
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        MouseEventArgs mouseEvent = new ()
        {
            Timestamp = DateTime.Now,
            Position = new (x, y),
            Flags = MouseFlags.Button1Clicked
        };

        MouseEventArgs? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.EnqueueMouseEvent (null, mouseEvent);
        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.NotNull (receivedEvent);
        Assert.Equal (x, receivedEvent.Position!.Value.X);
        Assert.Equal (y, receivedEvent.Position!.Value.Y);
    }

    [Theory]
    [InlineData (MouseFlags.ButtonShift)]
    [InlineData (MouseFlags.ButtonCtrl)]
    [InlineData (MouseFlags.ButtonAlt)]
    [InlineData (MouseFlags.ButtonShift | MouseFlags.ButtonCtrl)]
    [InlineData (MouseFlags.ButtonShift | MouseFlags.ButtonAlt)]
    [InlineData (MouseFlags.ButtonCtrl | MouseFlags.ButtonAlt)]
    [InlineData (MouseFlags.ButtonShift | MouseFlags.ButtonCtrl | MouseFlags.ButtonAlt)]
    public void FakeInput_EnqueueMouseEvent_PreservesModifiers (MouseFlags modifiers)
    {
        // Arrange
        var fakeInput = new FakeInput ();
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        MouseEventArgs mouseEvent = new ()
        {
            Timestamp = DateTime.Now,
            Position = new (10, 5),
            Flags = MouseFlags.Button1Clicked | modifiers
        };

        MouseEventArgs? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.EnqueueMouseEvent (null, mouseEvent);
        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.NotNull (receivedEvent);
        Assert.True (receivedEvent.Flags.HasFlag (MouseFlags.Button1Clicked));

        if (modifiers.HasFlag (MouseFlags.ButtonShift))
        {
            Assert.True (receivedEvent.Flags.HasFlag (MouseFlags.ButtonShift));
        }

        if (modifiers.HasFlag (MouseFlags.ButtonCtrl))
        {
            Assert.True (receivedEvent.Flags.HasFlag (MouseFlags.ButtonCtrl));
        }

        if (modifiers.HasFlag (MouseFlags.ButtonAlt))
        {
            Assert.True (receivedEvent.Flags.HasFlag (MouseFlags.ButtonAlt));
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
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        MouseEventArgs mouseEvent = new ()
        {
            Timestamp = DateTime.Now,
            Position = new (10, 5),
            Flags = wheelFlag
        };

        MouseEventArgs? receivedEvent = null;
        processor.SyntheticMouseEvent += (_, e) => receivedEvent = e;

        // Act
        processor.EnqueueMouseEvent (null, mouseEvent);
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
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        List<MouseEventArgs> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        MouseEventArgs [] events =
        [
            new () { Timestamp = DateTime.Now, Position = new (0, 0), Flags = MouseFlags.ReportMousePosition },
            new () { Timestamp = DateTime.Now, Position = new (5, 5), Flags = MouseFlags.ReportMousePosition },
            new () { Timestamp = DateTime.Now, Position = new (10, 10), Flags = MouseFlags.ReportMousePosition }
        ];

        // Act
        foreach (MouseEventArgs mouseEvent in events)
        {
            processor.EnqueueMouseEvent (null, mouseEvent);
        }

        SimulateInputThread (fakeInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.Equal (3, receivedEvents.Count);
        Assert.Equal (new (0, 0), receivedEvents [0].Position);
        Assert.Equal (new (5, 5), receivedEvents [1].Position);
        Assert.Equal (new (10, 10), receivedEvents [2].Position);
    }

    #endregion

    #region InputProcessor Pipeline Tests

    [Fact]
    public void InputProcessor_EnqueueMouseEvent_DoesNotThrow ()
    {
        // Arrange
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
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
                                                                                      Position = new (10, 5),
                                                                                      Flags = MouseFlags.Button1Clicked
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
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
        fakeInput.Initialize (queue);

        var processor = new FakeInputProcessor (queue);
        processor.InputImpl = fakeInput;

        List<MouseEventArgs> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Enqueue multiple events before processing
        processor.EnqueueMouseEvent (null, new () { Timestamp = DateTime.Now, Position = new (1, 1), Flags = MouseFlags.Button1Pressed });
        processor.EnqueueMouseEvent (null, new () { Timestamp = DateTime.Now, Position = new (2, 2), Flags = MouseFlags.ReportMousePosition });
        processor.EnqueueMouseEvent (null, new () { Timestamp = DateTime.Now, Position = new (3, 3), Flags = MouseFlags.Button1Released });

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
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
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
        ConcurrentQueue<ConsoleKeyInfo> queue = new ();
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
                                                                                      Position = new (-10, -5),
                                                                                      Flags = MouseFlags.Button1Clicked
                                                                                  });
                                                     SimulateInputThread (fakeInput, queue);
                                                     processor.ProcessQueue ();
                                                 });

        Assert.Null (exception);
    }

    #endregion
}
