#nullable enable
using System.Collections.Concurrent;

namespace DriverTests.Unix;

/// <summary>
///     Tests for ITestableInput implementation in UnixInput.
/// </summary>
[Trait ("Category", "Unix")]
[Trait ("Platform", "Unix")]
public class UnixInputTestableTests
{
    #region Helper Methods

    /// <summary>
    ///     Simulates the input thread by manually draining UnixInput's internal test queue
    ///     and moving items to the InputBuffer. This is needed because tests don't
    ///     start the actual input thread via Run().
    /// </summary>
    private static void SimulateInputThread (UnixInput unixInput, ConcurrentQueue<char> inputBuffer)
    {
        // UnixInput's Peek() checks _testInput first
        while (unixInput.Peek ())
        {
            // Read() drains _testInput first and returns items
            foreach (char item in unixInput.Read ())
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
    private static void ProcessQueueWithEscapeHandling (UnixInputProcessor processor, int maxAttempts = 3)
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

    [Fact]
    public void UnixInput_ImplementsITestableInput ()
    {
        // Arrange & Act
        var unixInput = new UnixInput ();

        // Assert
        Assert.IsAssignableFrom<ITestableInput<char>> (unixInput);
    }

    [Fact]
    public void UnixInput_InjectInput_EnqueuesCharacter ()
    {
        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var testableInput = (ITestableInput<char>)unixInput;

        // Act
        testableInput.InjectInput ('a');

        // Assert
        Assert.True (unixInput.Peek ());
        List<char> read = unixInput.Read ().ToList ();
        Assert.Single (read);
        Assert.Equal ('a', read [0]);
    }

    [Fact]
    public void UnixInput_InjectInput_SupportsMultipleCharacters ()
    {
        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var testableInput = (ITestableInput<char>)unixInput;

        // Act
        testableInput.InjectInput ('a');
        testableInput.InjectInput ('b');
        testableInput.InjectInput ('c');

        // Assert
        List<char> read = unixInput.Read ().ToList ();
        Assert.Equal (3, read.Count);
        Assert.Equal (new [] { 'a', 'b', 'c' }, read);
    }

    [Fact]
    public void UnixInput_Peek_ReturnsTrueWhenTestInputAvailable ()
    {
        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var testableInput = (ITestableInput<char>)unixInput;

        // Act & Assert - Initially false
        Assert.False (unixInput.Peek ());

        // Add input
        testableInput.InjectInput ('x');

        // Assert - Now true
        Assert.True (unixInput.Peek ());
    }

    [Fact]
    public void UnixInput_TestInput_HasPriorityOverRealInput ()
    {
        // This test verifies that test input is returned before any real terminal input
        // Since we can't easily simulate real terminal input in a unit test,
        // we just verify the order of test inputs

        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var testableInput = (ITestableInput<char>)unixInput;

        // Act - Add inputs in specific order
        testableInput.InjectInput ('1');
        testableInput.InjectInput ('2');
        testableInput.InjectInput ('3');

        // Assert - Should come out in FIFO order
        List<char> read = unixInput.Read ().ToList ();
        Assert.Equal (new [] { '1', '2', '3' }, read);
    }

    [Fact]
    public void UnixInputProcessor_InjectKeyDownEvent_WorksWithTestableInput ()
    {
        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = unixInput;

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        // Act
        processor.InjectKeyDownEvent (Key.A);

        // Simulate the input thread moving items from _testInput to InputBuffer
        SimulateInputThread (unixInput, queue);

        // Process the queue
        processor.ProcessQueue ();

        // Assert
        Assert.Single (receivedKeys);
        Assert.Equal (Key.A, receivedKeys [0]);
    }

    [Fact]
    public void UnixInputProcessor_InjectMouseEvent_GeneratesAnsiSequence ()
    {
        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = unixInput;

        List<Mouse> receivedMouse = [];
        processor.SyntheticMouseEvent += (_, m) => receivedMouse.Add (m);

        var mouse = new Mouse
        {
            Flags = MouseFlags.LeftButtonPressed,
            ScreenPosition = new (10, 20)
        };

        // Act
        processor.InjectMouseEvent (null, mouse);

        // Simulate the input thread
        SimulateInputThread (unixInput, queue);

        // Process the queue
        processor.ProcessQueue ();

        // Assert - Should have received the mouse event back
        Assert.NotEmpty (receivedMouse);

        // Find the pressed event (original + clicked)
        Mouse? pressedEvent = receivedMouse.FirstOrDefault (m => m.Flags.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.NotNull (pressedEvent);
        Assert.Equal (new Point (10, 20), pressedEvent.ScreenPosition);
    }

    [Fact]
    public void UnixInputProcessor_InjectMouseEvent_SupportsRelease ()
    {
        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = unixInput;

        List<Mouse> receivedMouse = [];
        processor.SyntheticMouseEvent += (_, m) => receivedMouse.Add (m);

        var mouse = new Mouse
        {
            Flags = MouseFlags.LeftButtonReleased,
            ScreenPosition = new (10, 20)
        };

        // Act
        processor.InjectMouseEvent (null, mouse);

        // Simulate the input thread
        SimulateInputThread (unixInput, queue);

        processor.ProcessQueue ();

        // Assert
        Mouse? releasedEvent = receivedMouse.FirstOrDefault (m => m.Flags.HasFlag (MouseFlags.LeftButtonReleased));
        Assert.NotNull (releasedEvent);
        Assert.Equal (new Point (10, 20), releasedEvent.ScreenPosition);
    }

    [Fact]
    public void UnixInputProcessor_InjectMouseEvent_SupportsModifiers ()
    {
        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = unixInput;

        List<Mouse> receivedMouse = [];
        processor.SyntheticMouseEvent += (_, m) =>
        {
            receivedMouse.Add (m);
        };

        // Test Ctrl+Alt (button code 24 for left button)
        var mouse = new Mouse
        {
            Flags = MouseFlags.LeftButtonPressed | MouseFlags.Ctrl | MouseFlags.Alt,
            ScreenPosition = new (5, 5)
        };

        // Act
        processor.InjectMouseEvent (null, mouse);

        // Debug: check what's in the queue
        List<char> inputChars = [];
        while (unixInput.Peek ())
        {
            inputChars.AddRange (unixInput.Read ());
        }
        string ansiSeq = new (inputChars.ToArray ());

        // Re-add to queue
        foreach (char ch in ansiSeq)
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // Assert
        Mouse? event1 = receivedMouse.FirstOrDefault (m => m.Flags.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.NotNull (event1);
        Assert.True (event1.Flags.HasFlag (MouseFlags.Ctrl), $"Expected Ctrl flag, got: {event1.Flags}");
        Assert.True (event1.Flags.HasFlag (MouseFlags.Alt), $"Expected Alt flag, got: {event1.Flags}");
    }

    [Theory]
    [InlineData (MouseFlags.WheeledUp)]
    [InlineData (MouseFlags.WheeledDown)]
    // Note: WheeledLeft and WheeledRight (codes 68/69) have complex ANSI encoding with Shift+Ctrl variations
    // These are tested separately in AnsiMouseParserDebugTests
    public void UnixInputProcessor_InjectMouseEvent_SupportsWheelEvents (MouseFlags wheelFlag)
    {
        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = unixInput;

        List<Mouse> receivedMouse = [];
        processor.SyntheticMouseEvent += (_, m) => receivedMouse.Add (m);

        var mouse = new Mouse
        {
            Flags = wheelFlag,
            ScreenPosition = new (15, 15)
        };

        // Act
        processor.InjectMouseEvent (null, mouse);

        // Simulate the input thread
        SimulateInputThread (unixInput, queue);

        processor.ProcessQueue ();

        // Assert
        Mouse? wheelEvent = receivedMouse.FirstOrDefault (m => m.Flags.HasFlag (wheelFlag));
        Assert.NotNull (wheelEvent);
        Assert.Equal (new Point (15, 15), wheelEvent.ScreenPosition);
    }


    #region UnixInput InjectKeyDownEvent Tests

    [Fact]
    public void UnixInput_InjectKeyDownEvent_AddsSingleKeyToQueue ()
    {
        // Arrange
        var UnixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        UnixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = UnixInput;

        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        Key key = Key.A;

        // Act
        processor.InjectKeyDownEvent (key);

        // Simulate the input thread moving items from _testInput to InputBuffer
        SimulateInputThread (UnixInput, queue);

        processor.ProcessQueue ();

        // Assert - Verify the key made it through
        Assert.Single (receivedKeys);
        Assert.Equal (key, receivedKeys [0]);
    }

    [Fact]
    public void UnixInput_InjectKeyDownEvent_SupportsMultipleKeys ()
    {
        // Arrange
        var UnixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        UnixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = UnixInput;

        Key [] keys = [Key.A, Key.B, Key.C, Key.Enter];
        List<Key> receivedKeys = [];
        processor.KeyDown += (_, k) => receivedKeys.Add (k);

        // Act
        foreach (Key key in keys)
        {
            processor.InjectKeyDownEvent (key);
        }

        SimulateInputThread (UnixInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.Equal (keys.Length, receivedKeys.Count);
        Assert.Equal (keys, receivedKeys);
    }

    [Theory]
    [InlineData (KeyCode.A, false, false, false)]
    [InlineData (KeyCode.A, true, false, false)] // Shift+A
    [InlineData (KeyCode.A, false, true, false)] // Ctrl+A
    [InlineData (KeyCode.A, false, false, true)] // Alt+A
    // Note: Ctrl+Shift+Alt+A is not tested because ANSI doesn't have a standard way to represent
    // Shift with Ctrl combinations (Ctrl+A is 0x01 regardless of Shift state)
    public void UnixInput_InjectKeyDownEvent_PreservesModifiers (KeyCode keyCode, bool shift, bool ctrl, bool alt)
    {
        // Arrange
        var UnixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        UnixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = UnixInput;

        var key = new Key (keyCode);

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
        processor.InjectKeyDownEvent (key);
        SimulateInputThread (UnixInput, queue);
        
        // Alt combinations start with ESC, so they need escape handling
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
        Assert.Equal (key.IsShift, receivedKey.IsShift);
        Assert.Equal (key.IsCtrl, receivedKey.IsCtrl);
        Assert.Equal (key.IsAlt, receivedKey.IsAlt);
        Assert.Equal (key.KeyCode, receivedKey.KeyCode);
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
    public void UnixInput_InjectKeyDownEvent_SupportsSpecialKeys (KeyCode keyCode)
    {
        // Arrange
        var UnixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        UnixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = UnixInput;

        var key = new Key (keyCode);
        Key? receivedKey = null;
        processor.KeyDown += (_, k) => receivedKey = k;

        // Act
        processor.InjectKeyDownEvent (key);
        SimulateInputThread (UnixInput, queue);

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
    public void UnixInput_InjectKeyDownEvent_RaisesKeyDownEvent ()
    {
        // Arrange
        var unixInput = new UnixInput ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        var processor = new UnixInputProcessor (queue, null);
        processor.InputImpl = unixInput;

        var keyDownCount = 0;
        processor.KeyDown += (_, _) => keyDownCount++;

        // Act
        processor.InjectKeyDownEvent (Key.A);
        SimulateInputThread (unixInput, queue);
        processor.ProcessQueue ();

        Assert.Equal (1, keyDownCount);
    }

    #endregion

    #region Mouse Event Sequencing Tests

    [Fact]
    public void UnixInput_InjectMouseEvent_HandlesCompleteClickSequence ()
    {
        // Arrange
        UnixInput unixInput = new ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        UnixInputProcessor processor = new (queue);
        processor.InputImpl = unixInput;

        List<Terminal.Gui.Input.Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Simulate a complete click: press → release
        processor.InjectMouseEvent (
                                     null,
                                     new ()
                                     {
                                         Position = new (10, 5),
                                         Flags = MouseFlags.LeftButtonPressed
                                     });

        processor.InjectMouseEvent (
                                     null,
                                     new ()
                                     {
                                         Position = new (10, 5),
                                         Flags = MouseFlags.LeftButtonReleased
                                     });

        SimulateInputThread (unixInput, queue);
        processor.ProcessQueue ();

        // Assert - Process() emits Pressed and Released immediately (clicks are deferred)
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonPressed));
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonReleased));
        // We should also see the synthetic Clicked event
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (MouseFlags.LeftButtonClicked));
        Assert.Equal (3, receivedEvents.Count);
    }

    [Theory]
    [InlineData(MouseFlags.WheeledUp)]
    [InlineData(MouseFlags.WheeledDown)]
    [InlineData(MouseFlags.WheeledLeft)]
    [InlineData(MouseFlags.WheeledRight)]
    public void UnixInput_InjectMouseEvent_Wheel_Events (MouseFlags wheelEvent)
    {
        // Arrange
        UnixInput unixInput = new ();
        ConcurrentQueue<char> queue = new ();
        unixInput.Initialize (queue);

        UnixInputProcessor processor = new (queue);
        processor.InputImpl = unixInput;

        List<Terminal.Gui.Input.Mouse> receivedEvents = [];
        processor.SyntheticMouseEvent += (_, e) => receivedEvents.Add (e);

        // Act - Simulate a wheel event
        processor.InjectMouseEvent (
                                     null,
                                     new ()
                                     {
                                         Position = new (10, 5),
                                         Flags = wheelEvent
                                     });

        SimulateInputThread (unixInput, queue);
        processor.ProcessQueue ();

        // Assert
        Assert.Contains (receivedEvents, e => e.Flags.HasFlag (wheelEvent));
        Assert.Single (receivedEvents);
        
        // Note: ANSI codes 68 and 69 (horizontal wheel) always include Shift flag per ANSI spec
        if (wheelEvent is MouseFlags.WheeledLeft or MouseFlags.WheeledRight)
        {
            Terminal.Gui.Input.Mouse wheelEventReceived = receivedEvents.First (e => e.Flags.HasFlag (wheelEvent));
            Assert.True (wheelEventReceived.Flags.HasFlag (MouseFlags.Shift), 
                        $"Horizontal wheel events should include Shift flag, got: {wheelEventReceived.Flags}");
        }
    }

    #endregion

}
