using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace DriverTests.Keyboard;

/// <summary>
///     Parallelizable unit tests for IInput.EnqueueKeyDownEvent and InputProcessor.EnqueueKeyDownEvent.
///     Tests validate the entire pipeline: Key → TInputRecord → Queue → ProcessQueue → Events.
/// </summary>
[Trait ("Category", "Input")]
public class EnqueueKeyEventTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region Helper Methods

    /// <summary>
    ///     Simulates the input thread by manually draining ANSIInput's internal queue
    ///     and moving items to the InputBuffer. This is needed because tests don't
    ///     start the actual input thread via Run().
    /// </summary>
    private static void SimulateInputThread (ANSIInput ansiInput, ConcurrentQueue<char> inputBuffer)
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

    /// <summary>
    ///     Processes the input queue with support for keys that may be held by the ANSI parser (like Esc).
    ///     The parser holds Esc for 50ms waiting to see if it's part of an escape sequence.
    /// </summary>
    private static void ProcessQueueWithEscapeHandling (ANSIInputProcessor processor, int maxAttempts = 3)
    {
        // First attempt - process immediately
        processor.ProcessQueue ();

        // For escape sequences, we may need to wait and process again
        // The parser holds escape for 50ms before releasing
        for (var attempt = 1; attempt < maxAttempts; attempt++)
        {
            Thread.Sleep (60); // Wait longer than the 50ms escape timeout
            processor.ProcessQueue (); // This should release any held escape keys
        }
    }

    #endregion

    #region ANSIInput EnqueueKeyDownEvent Tests

    [Fact]
    public void EnqueueKeyDownEvent_AddsSingleKeyToQueue ()
    {
        // Arrange
        ANSIInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ANSIInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        Key key = Key.A;

        // Act
        processor.EnqueueKeyDownEvent (key);

        // Simulate the input thread moving items from _testInput to InputBuffer
        SimulateInputThread (ansiInput, queue);

        processor.ProcessQueue ();

        // Assert - Verify the key made it through
        Assert.Single (receivedKeys);
        Assert.Equal (key, receivedKeys [0]);
    }

    [Fact]
    public void EnqueueKeyDownEvent_SupportsMultipleKeys ()
    {
        // Arrange
        ANSIInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ANSIInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        Key [] keys = [Key.A, Key.B, Key.C, Key.Enter];
        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        // Act
        foreach (Key key in keys)
        {
            processor.EnqueueKeyDownEvent (key);
        }

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.Equal (keys.Length, receivedKeys.Count);
        Assert.Equal (keys, receivedKeys);
    }

    [Theory]
    [InlineData (KeyCode.A, false, false, false)] // A (no modifiers)
    [InlineData (KeyCode.A, true, false, false)] // Shift+A (uppercase)
    [InlineData (KeyCode.A, false, true, false)] // Ctrl+A
    [InlineData (KeyCode.A, false, false, true)] // Alt+A
    [InlineData (KeyCode.A, true, false, true)] // Shift+Alt+A (Alt+uppercase)
    // Note: The following combinations cannot be represented in ANSI sequences:
    // - Shift+Ctrl+A: Ctrl+A and Ctrl+Shift+A both encode as \x01 (Shift is lost)
    // - Shift+Ctrl+Alt+A: Same limitation - Shift is lost when Ctrl is present
    // [InlineData (KeyCode.A, true, true, false)]  // Known limitation - Shift lost with Ctrl
    // [InlineData (KeyCode.A, true, true, true)]   // Known limitation - Shift lost with Ctrl
    public void EnqueueKeyDownEvent_PreservesModifiers (KeyCode keyCode, bool shift, bool ctrl, bool alt)
    {
        // Arrange
        ANSIInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ANSIInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        Key key = new (keyCode);

        if (shift)
        {
            key = key.WithShift;
        }

        if (ctrl)
        {
            key = key.WithCtrl;
        }

        if (alt)
        {
            key = key.WithAlt;
        }

        Key? receivedKey = null;
        processor.KeyDown += (_, k) => receivedKey = k;

        // Act
        processor.EnqueueKeyDownEvent (key);
        SimulateInputThread (ansiInput, queue);

        // Alt combinations produce ESC+char sequences that require parser timeout handling
        if (alt)
        {
            ProcessQueueWithEscapeHandling (processor);
        }
        else
        {
            processor.ProcessQueue ();
        }

        // Assert
        Assert.NotNull (receivedKey);

        // When Ctrl is present with letter keys, Shift information cannot be preserved
        // in ANSI encoding because Ctrl+A and Ctrl+Shift+A both encode as \x01
        bool shiftLostDueToCtrl = ctrl && keyCode >= KeyCode.A && keyCode <= KeyCode.Z;

        if (shiftLostDueToCtrl)
        {
            // Skip Shift assertion when Ctrl is present - known ANSI limitation
            Assert.Equal (key.IsCtrl, receivedKey.IsCtrl);
            Assert.Equal (key.IsAlt, receivedKey.IsAlt);
        }
        else
        {
            Assert.Equal (key.IsShift, receivedKey.IsShift);
            Assert.Equal (key.IsCtrl, receivedKey.IsCtrl);
            Assert.Equal (key.IsAlt, receivedKey.IsAlt);
        }

        Assert.Equal (key.KeyCode & ~KeyCode.ShiftMask, receivedKey.KeyCode & ~KeyCode.ShiftMask);
    }

    [Theory]
    [InlineData (KeyCode.A, true, true, false)] // Shift+Ctrl+A - Shift lost
    [InlineData (KeyCode.A, true, true, true)] // Shift+Ctrl+Alt+A - Shift lost
    public void EnqueueKeyDownEvent_KnownLimitation_ShiftLostWithCtrl (KeyCode keyCode, bool shift, bool ctrl, bool alt)
    {
        // This test documents the known limitation that Shift cannot be preserved
        // when Ctrl is present with letter keys in ANSI encoding.
        // 
        // Root cause: Ctrl+A and Ctrl+Shift+A both encode as ASCII control code \x01.
        // The ANSI/VT100 protocol has no way to distinguish between them.

        // Arrange
        ANSIInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ANSIInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        Key key = new (keyCode);

        if (shift)
        {
            key = key.WithShift;
        }

        if (ctrl)
        {
            key = key.WithCtrl;
        }

        if (alt)
        {
            key = key.WithAlt;
        }

        Key? receivedKey = null;
        processor.KeyDown += (_, k) => receivedKey = k;

        // Act
        processor.EnqueueKeyDownEvent (key);
        SimulateInputThread (ansiInput, queue);

        if (alt)
        {
            ProcessQueueWithEscapeHandling (processor);
        }
        else
        {
            processor.ProcessQueue ();
        }

        // Assert - Document the expected behavior with the limitation
        Assert.NotNull (receivedKey);

        // Shift is lost when Ctrl is present
        Assert.False (receivedKey.IsShift, "Shift modifier cannot be preserved in ANSI when Ctrl is present on letter keys");

        // But Ctrl and Alt should still be preserved
        Assert.Equal (key.IsCtrl, receivedKey.IsCtrl);
        Assert.Equal (key.IsAlt, receivedKey.IsAlt);

        // Base key should match (ignoring Shift)
        Assert.Equal (key.KeyCode & ~KeyCode.ShiftMask, receivedKey.KeyCode & ~KeyCode.ShiftMask);
    }

    [Theory]
    [InlineData (KeyCode.Enter)]
    [InlineData (KeyCode.Tab)]
    [InlineData (KeyCode.Esc)]
    [InlineData (KeyCode.Backspace)]
    [InlineData (KeyCode.Delete)]
    [InlineData (KeyCode.CursorUp)]
    [InlineData (KeyCode.CursorDown)]
    [InlineData (KeyCode.CursorLeft)]
    [InlineData (KeyCode.CursorRight)]
    [InlineData (KeyCode.F1)]
    [InlineData (KeyCode.F12)]
    public void EnqueueKeyDownEvent_SupportsSpecialKeys (KeyCode keyCode)
    {
        // Arrange
        ANSIInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ANSIInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        Key key = new (keyCode);
        Key? receivedKey = null;
        processor.KeyDown += (_, k) => receivedKey = k;

        // Act
        processor.EnqueueKeyDownEvent (key);
        SimulateInputThread (ansiInput, queue);

        // Esc is special - the ANSI parser holds it waiting for potential escape sequences
        // We need to process with delay to let the parser release it after timeout
        if (keyCode == KeyCode.Esc)
        {
            ProcessQueueWithEscapeHandling (processor);
        }
        else
        {
            processor.ProcessQueue ();
        }

        // Assert
        Assert.NotNull (receivedKey);
        Assert.Equal (key.KeyCode, receivedKey.KeyCode);
    }

    [Fact]
    public void EnqueueKeyDownEvent_RaisesKeyDownAndKeyUpEvents ()
    {
        // Arrange
        ANSIInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ANSIInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        var keyDownCount = 0;
        var keyUpCount = 0;
        processor.KeyDown += (_, _) => keyDownCount++;
        processor.KeyUp += (_, _) => keyUpCount++;

        // Act
        processor.EnqueueKeyDownEvent (Key.A);
        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert - FakeDriver simulates KeyUp immediately after KeyDown
        Assert.Equal (1, keyDownCount);
        Assert.Equal (1, keyUpCount);
    }

    #endregion

    #region InputProcessor Pipeline Tests

    [Fact]
    public void InputProcessor_EnqueueKeyDownEvent_RequiresTestableInput ()
    {
        // Arrange
        ConcurrentQueue<char> queue = new ();
        ANSIInputProcessor processor = new (queue);

        // Don't set InputImpl (or set to non-testable)

        // Act & Assert - Should not throw, but also won't add to queue
        // (because InputImpl is null or not ITestableInput)
        processor.EnqueueKeyDownEvent (Key.A);
        processor.ProcessQueue ();

        // No events should be raised since no input was added
        var eventRaised = false;
        processor.KeyDown += (_, _) => eventRaised = true;
        processor.ProcessQueue ();
        Assert.False (eventRaised);
    }

    [Fact]
    public void InputProcessor_ProcessQueue_DrainsPendingInputRecords ()
    {
        // Arrange
        ANSIInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ANSIInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        // Act - Enqueue multiple keys before processing
        processor.EnqueueKeyDownEvent (Key.A);
        processor.EnqueueKeyDownEvent (Key.B);
        processor.EnqueueKeyDownEvent (Key.C);

        SimulateInputThread (ansiInput, queue);
        processor.ProcessQueue ();

        // Assert - After processing, queue should be empty and all keys received
        Assert.Empty (queue);
        Assert.Equal (3, receivedKeys.Count);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void EnqueueKeyDownEvent_IsThreadSafe ()
    {
        // Arrange
        ANSIInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ANSIInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        ConcurrentBag<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        const int threadCount = 10;
        const int keysPerThread = 100;
        Thread [] threads = new Thread [threadCount];

        // Act - Enqueue keys from multiple threads
        for (var t = 0; t < threadCount; t++)
        {
            threads [t] = new (() =>
                               {
                                   for (var i = 0; i < keysPerThread; i++)
                                   {
                                       processor.EnqueueKeyDownEvent (Key.A);
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
        Assert.Equal (threadCount * keysPerThread, receivedKeys.Count);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void EnqueueKeyDownEvent_WithInvalidKey_DoesNotThrow ()
    {
        // Arrange
        ANSIInput ansiInput = new ();
        ConcurrentQueue<char> queue = new ();
        ansiInput.Initialize (queue);

        ANSIInputProcessor processor = new (queue);
        processor.InputImpl = ansiInput;

        // Act & Assert - Empty/null key should not throw
        Exception? exception = Record.Exception (() =>
                                                 {
                                                     processor.EnqueueKeyDownEvent (Key.Empty);
                                                     SimulateInputThread (ansiInput, queue);
                                                     processor.ProcessQueue ();
                                                 });

        Assert.Null (exception);
    }

    #endregion
}
