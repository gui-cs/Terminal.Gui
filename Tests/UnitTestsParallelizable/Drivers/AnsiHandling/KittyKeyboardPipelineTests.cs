// Copilot - Opus 4.6

using System.Collections.Concurrent;

namespace DriverTests.AnsiHandling;

/// <summary>
///     Integration tests that inject raw ANSI escape sequences through the full input pipeline
///     (parser → driver → <c>app.Keyboard.KeyDown/KeyUp</c>) and verify correct event delivery.
/// </summary>
public class KittyKeyboardPipelineTests
{
    /// <summary>
    ///     Injects raw ANSI sequences into the full Application pipeline and collects KeyDown/KeyUp events.
    /// </summary>
    private static (List<Key> KeyDown, List<Key> KeyUp) InjectRawSequence (params string [] sequences)
    {
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> keyDownEvents = [];
        List<Key> keyUpEvents = [];
        app.Keyboard.KeyDown += (_, key) => keyDownEvents.Add (key);
        app.Keyboard.KeyUp += (_, key) => keyUpEvents.Add (key);

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;
        ConcurrentQueue<char> queue = ((AnsiInputProcessor)processor).InputQueue;

        foreach (string seq in sequences)
        {
            foreach (char ch in seq)
            {
                queue.Enqueue (ch);
            }

            processor.ProcessQueue ();
        }

        return (keyDownEvents, keyUpEvents);
    }

    #region CSI u Event Types

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Press_RaisesKeyDown ()
    {
        // ESC[97;1:1u = 'a' press
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:1u");

        Assert.Single (down);
        Assert.Equal (Key.A, down [0]);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    [Fact]
    public void Pipeline_AltGrE_Press_RaisesEuroKeyDown ()
    {
        // ESC[8364;1:1u = Euro symbol press
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[8364;1:1u");

        Assert.Single (down);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Equal ((KeyCode)8364, down [0].KeyCode);
        Assert.Equal ("€", down [0].AsGrapheme);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Repeat_RaisesKeyDown ()
    {
        // ESC[97;1:2u = 'a' repeat
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:2u");

        Assert.Single (down);
        Assert.Equal (Key.A, down [0]);
        Assert.Equal (KeyEventType.Repeat, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Release_RaisesKeyUp ()
    {
        // ESC[97;1:3u = 'a' release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.A, up [0]);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_ShiftA_Release ()
    {
        // ESC[97;2:3u = 'a' + Shift(2), release(3)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;2:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.A.WithShift.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_CtrlC_Press ()
    {
        // ESC[99;5:1u = 'c'(99) + Ctrl(5), press(1)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[99;5:1u");

        Assert.Single (down);
        Assert.Equal (Key.C.WithCtrl.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_CtrlShiftAlt_Release ()
    {
        // ESC[97;8:3u = 'a', Ctrl+Shift+Alt (8), release (3)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;8:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsCtrl);
        Assert.True (up [0].IsShift);
        Assert.True (up [0].IsAlt);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Enter_Repeat ()
    {
        // ESC[13;1:2u = Enter, repeat
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[13;1:2u");

        Assert.Single (down);
        Assert.Equal (Key.Enter.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Repeat, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Escape_Release ()
    {
        // ESC[27;1:3u = Escape, release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[27;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.Esc.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Tab_Press ()
    {
        // ESC[9;1:1u = Tab, press
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[9;1:1u");

        Assert.Single (down);
        Assert.Equal (Key.Tab.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_F1_Release ()
    {
        // ESC[57364;1:3u = F1, release (kitty functional key encoding)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57364;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.F1.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_F12_CtrlShift_Press ()
    {
        // ESC[57375;6:1u = F12 (kitty), Ctrl+Shift (6), press (1)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57375;6:1u");

        Assert.Single (down);
        Assert.Equal (Key.F12.WithCtrl.WithShift.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    #endregion

    #region Standalone Modifier Key Events

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_LeftShift_PressAndRelease ()
    {
        // Press then release Left Shift
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57441u", // press (no event type = press)
                                                            "\x1b[57441;1:3u" // release
                                                           );

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, down [0].ModifierKey);
        Assert.Equal (KeyEventType.Press, down [0].EventType);

        Assert.Single (up);
        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_LeftCtrl_Release ()
    {
        // ESC[57442;1:3u = Left Ctrl, release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57442;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_RightAlt_Press ()
    {
        // ESC[57449u = Right Alt, press (no event type = press)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57449u");

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.RightAlt, down [0].ModifierKey);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Theory]
    [InlineData ("\x1b[57441u", ModifierKey.LeftShift)]
    [InlineData ("\x1b[57442u", ModifierKey.LeftCtrl)]
    [InlineData ("\x1b[57443u", ModifierKey.LeftAlt)]
    [InlineData ("\x1b[57447u", ModifierKey.RightShift)]
    [InlineData ("\x1b[57448u", ModifierKey.RightCtrl)]
    [InlineData ("\x1b[57449u", ModifierKey.RightAlt)]
    [InlineData ("\x1b[57453u", ModifierKey.AltGr)]
    public void Pipeline_ModifierPress_RaisesKeyDown (string sequence, ModifierKey expectedModifier)
    {
        // Standalone modifier press should raise app.Keyboard.KeyDown
        (List<Key> down, List<Key> up) = InjectRawSequence (sequence);

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (expectedModifier, down [0].ModifierKey);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Theory]
    [InlineData ("\x1b[57441;1:3u", ModifierKey.LeftShift)]
    [InlineData ("\x1b[57442;1:3u", ModifierKey.LeftCtrl)]
    [InlineData ("\x1b[57443;1:3u", ModifierKey.LeftAlt)]
    [InlineData ("\x1b[57447;1:3u", ModifierKey.RightShift)]
    [InlineData ("\x1b[57448;1:3u", ModifierKey.RightCtrl)]
    [InlineData ("\x1b[57449;1:3u", ModifierKey.RightAlt)]
    [InlineData ("\x1b[57453;1:3u", ModifierKey.AltGr)]
    public void Pipeline_ModifierRelease_RaisesKeyUp (string sequence, ModifierKey expectedModifier)
    {
        // Standalone modifier release should raise app.Keyboard.KeyUp
        (List<Key> down, List<Key> up) = InjectRawSequence (sequence);

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (expectedModifier, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    [Fact]
    public void Pipeline_LeftAltPress_WithCtrlModifier_PreservesBothStates ()
    {
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57443;5u");

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftAlt, down [0].ModifierKey);
        Assert.True (down [0].IsCtrl);
        Assert.False (down [0].IsAlt);
        Assert.Empty (up);
    }

    #endregion

    #region CSI ~ and Cursor Keys

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_Delete_Release_CsiTilde ()
    {
        // ESC[3;1:3~ = Delete, no modifiers, release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[3;1:3~");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.Delete.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_PageUp_Repeat_CsiTilde ()
    {
        // ESC[5;1:2~ = PageUp, no modifiers, repeat
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[5;1:2~");

        Assert.Single (down);
        Assert.Equal (Key.PageUp.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Repeat, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_F5_Ctrl_Release_CsiTilde ()
    {
        // ESC[15;5:3~ = F5 + Ctrl (5), release (3)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[15;5:3~");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.F5.WithCtrl.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_CursorUp_Release_CsiCursor ()
    {
        // ESC[1;1:3A = CursorUp, no modifiers, release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[1;1:3A");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.CursorUp.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_CursorDown_CtrlShift_Repeat ()
    {
        // ESC[1;6:2B = CursorDown, Ctrl+Shift (6), repeat (2)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[1;6:2B");

        Assert.Single (down);
        Assert.Equal (Key.CursorDown.WithCtrl.WithShift.KeyCode, down [0].KeyCode);
        Assert.Equal (KeyEventType.Repeat, down [0].EventType);
        Assert.Empty (up);
    }

    #endregion

    #region Full Press-Repeat-Release Cycle

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_FullCycle_PressRepeatRelease ()
    {
        // Simulates holding 'a': press → repeat → repeat → release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:1u", // press
                                                            "\x1b[97;1:2u", // repeat
                                                            "\x1b[97;1:2u", // repeat
                                                            "\x1b[97;1:3u" // release
                                                           );

        Assert.Equal (3, down.Count);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Equal (KeyEventType.Repeat, down [1].EventType);
        Assert.Equal (KeyEventType.Repeat, down [2].EventType);

        Assert.Single (up);
        Assert.Equal (KeyEventType.Release, up [0].EventType);

        // All should be the same key
        Assert.All (down, k => Assert.Equal (Key.A, k));
        Assert.Equal (Key.A, up [0]);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_NoModifierField_DefaultsToPress ()
    {
        // ESC[97u = 'a' with no modifier/event type field
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97u");

        Assert.Single (down);
        Assert.Equal (Key.A, down [0]);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    #endregion

    #region Error Handling and Robustness

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_InvalidEventType_Zero_DefaultsToPress ()
    {
        // ESC[97;1:0u = 'a', event type 0 (invalid, out of 1-3 range)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:0u");

        Assert.Single (down);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_InvalidEventType_Five_DefaultsToPress ()
    {
        // ESC[97;1:5u = 'a', event type 5 (invalid, out of 1-3 range)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:5u");

        Assert.Single (down);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_MalformedSequence_NoTerminator_DropsInput ()
    {
        // ESC[97;1:1 (missing 'u' terminator) — parser should not produce a key event.
        // The incomplete sequence will be held, then expire as stale on next ProcessQueue
        // after time advances past the 50ms threshold.
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> keyDownEvents = [];
        app.Keyboard.KeyDown += (_, key) => keyDownEvents.Add (key);

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;
        ConcurrentQueue<char> queue = ((AnsiInputProcessor)processor).InputQueue;

        // Inject incomplete sequence
        foreach (char ch in "\x1b[97;1:1")
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // No kitty key should have been produced (sequence is still held in parser)
        // None of the received keys should have kitty event metadata
        bool hasKittyEvent = keyDownEvents.Any (k => k.EventType != KeyEventType.Press || k.ModifierKey != ModifierKey.None);
        Assert.False (hasKittyEvent, "Incomplete kitty sequence should not produce a kitty-typed key event");
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_InvalidKeycode_DoesNotCrash ()
    {
        // ESC[999999u = Huge codepoint — may not map to any key but should not crash
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[999999u");

        // Should not crash; the key may or may not be produced depending on Rune.IsValid
        // but the pipeline should remain functional
        int total = down.Count + up.Count;
        Assert.True (total >= 0); // Mainly verifying no exception
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_NegativeKeycode_Ignored ()
    {
        // ESC[-1u = negative keycode — parser regex won't match \d+ for negative
        // Should be released as individual characters after timeout, not as a kitty key
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> keyDownEvents = [];
        app.Keyboard.KeyDown += (_, key) => keyDownEvents.Add (key);

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;
        ConcurrentQueue<char> queue = ((AnsiInputProcessor)processor).InputQueue;

        foreach (char ch in "\x1b[-1u")
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // Advance past the escape timeout so held sequences get released
        timeProvider.Advance (TimeSpan.FromMilliseconds (60));
        processor.ProcessQueue ();

        // The sequence should NOT have been parsed as a kitty key
        bool hasModifierKey = keyDownEvents.Any (k => k.ModifierKey != ModifierKey.None);
        Assert.False (hasModifierKey, "Negative keycode should not produce a modifier key event");
    }

    // Copilot - Opus 4.6
    [Fact]
    public void Pipeline_ValidAfterInvalid_StillWorks ()
    {
        // Inject garbage first, then a valid kitty sequence.
        // The pipeline should recover and process the valid sequence.
        VirtualTimeProvider timeProvider = new ();
        timeProvider.SetTime (new DateTime (2025, 1, 1, 12, 0, 0));

        using IApplication app = Application.Create (timeProvider);
        app.Init (DriverRegistry.Names.ANSI);

        List<Key> keyDownEvents = [];
        List<Key> keyUpEvents = [];
        app.Keyboard.KeyDown += (_, key) => keyDownEvents.Add (key);
        app.Keyboard.KeyUp += (_, key) => keyUpEvents.Add (key);

        IInputProcessor processor = app.Driver?.GetInputProcessor ()!;
        ConcurrentQueue<char> queue = ((AnsiInputProcessor)processor).InputQueue;

        // Inject an incomplete/garbage escape sequence
        foreach (char ch in "\x1b[ZZZZ")
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // Advance time to flush stale sequence
        timeProvider.Advance (TimeSpan.FromMilliseconds (60));
        processor.ProcessQueue ();

        // Now inject a valid kitty release
        foreach (char ch in "\x1b[98;1:3u")
        {
            queue.Enqueue (ch);
        }

        processor.ProcessQueue ();

        // The valid kitty release should be received
        Assert.Single (keyUpEvents);
        Assert.Equal (Key.B, keyUpEvents [0]);
        Assert.Equal (KeyEventType.Release, keyUpEvents [0].EventType);
    }

    #endregion
}
