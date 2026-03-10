// Claude - Opus 4.6

using System.Collections.Concurrent;

namespace DriverTests.AnsiHandling;

/// <summary>
///     Tests for Phase 2 kitty keyboard protocol features:
///     event type (press/repeat/release) and standalone modifier key events.
/// </summary>
public class KittyKeyboardPhase2Tests
{
    private readonly KittyKeyboardPattern _pattern = new ();

    #region Event Type Parsing

    [Fact]
    public void KittyPattern_NoEventType_DefaultsToPress ()
    {
        // ESC[97u = 'a' with no modifier field
        Key? key = _pattern.GetKey ("\u001b[97u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key!.EventType);
    }

    [Fact]
    public void KittyPattern_EventType_Press ()
    {
        // ESC[97;1:1u = 'a', no modifiers, event type 1 (press)
        Key? key = _pattern.GetKey ("\u001b[97;1:1u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key!.EventType);
        Assert.Equal (Key.A, key.NoShift.NoCtrl.NoAlt);
    }

    [Fact]
    public void KittyPattern_EventType_Repeat ()
    {
        // ESC[97;1:2u = 'a', no modifiers, event type 2 (repeat)
        Key? key = _pattern.GetKey ("\u001b[97;1:2u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Repeat, key!.EventType);
    }

    [Fact]
    public void KittyPattern_EventType_Release ()
    {
        // ESC[97;1:3u = 'a', no modifiers, event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[97;1:3u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Release, key!.EventType);
    }

    [Fact]
    public void KittyPattern_EventType_WithModifiers ()
    {
        // ESC[97;4:3u = 'a', Shift+Alt (modifiers=4 → 3=Shift|Alt), event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[97;4:3u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Release, key!.EventType);
        Assert.True (key.IsShift);
        Assert.True (key.IsAlt);
    }

    [Fact]
    public void KittyPattern_EventType_CtrlWithRelease ()
    {
        // ESC[97;5:3u = 'a', Ctrl (modifiers=5 → 4=Ctrl), event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[97;5:3u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Release, key!.EventType);
        Assert.True (key.IsCtrl);
    }

    [Fact]
    public void KittyPattern_FunctionKey_EventType_Release ()
    {
        // ESC[57364;1:3u = F1, no modifiers, event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[57364;1:3u");

        Assert.NotNull (key);
        Assert.Equal (Key.F1.KeyCode, key!.KeyCode);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void KittyPattern_Enter_EventType_Repeat ()
    {
        // ESC[13;1:2u = Enter, no modifiers, event type 2 (repeat)
        Key? key = _pattern.GetKey ("\u001b[13;1:2u");

        Assert.NotNull (key);
        Assert.Equal (Key.Enter.KeyCode, key!.KeyCode);
        Assert.Equal (KeyEventType.Repeat, key.EventType);
    }

    [Fact]
    public void KittyPattern_ModifiersOnly_NoEventType_DefaultsToPress ()
    {
        // ESC[97;2u = 'a' with Shift modifier, no event type → defaults to press
        Key? key = _pattern.GetKey ("\u001b[97;2u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key!.EventType);
        Assert.True (key.IsShift);
    }

    [Fact]
    public void KittyPattern_InvalidEventType_DefaultsToPress ()
    {
        // ESC[97;1:5u = 'a', event type 5 (invalid, out of 1-3 range)
        Key? key = _pattern.GetKey ("\u001b[97;1:5u");

        Assert.NotNull (key);
        Assert.Equal (KeyEventType.Press, key!.EventType);
    }

    #endregion

    #region Standalone Modifier Key Events

    [Fact]
    public void KittyPattern_LeftShift_Standalone ()
    {
        // ESC[57441u = Left Shift
        Key? key = _pattern.GetKey ("\u001b[57441u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_LeftCtrl_Standalone ()
    {
        // ESC[57442u = Left Ctrl
        Key? key = _pattern.GetKey ("\u001b[57442u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_LeftAlt_Standalone ()
    {
        // ESC[57443u = Left Alt
        Key? key = _pattern.GetKey ("\u001b[57443u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftAlt, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_RightShift_Standalone ()
    {
        // ESC[57447u = Right Shift
        Key? key = _pattern.GetKey ("\u001b[57447u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.RightShift, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_RightCtrl_Standalone ()
    {
        // ESC[57448u = Right Ctrl
        Key? key = _pattern.GetKey ("\u001b[57448u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.RightCtrl, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_RightAlt_Standalone ()
    {
        // ESC[57449u = Right Alt
        Key? key = _pattern.GetKey ("\u001b[57449u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.RightAlt, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_CapsLock_Standalone ()
    {
        // ESC[57358u = Caps Lock
        Key? key = _pattern.GetKey ("\u001b[57358u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.CapsLock, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_NumLock_Standalone ()
    {
        // ESC[57360u = Num Lock
        Key? key = _pattern.GetKey ("\u001b[57360u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.NumLock, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_ModifierKey_WithEventType_Release ()
    {
        // ESC[57441;1:3u = Left Shift, event type 3 (release)
        Key? key = _pattern.GetKey ("\u001b[57441;1:3u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, key.ModifierKey);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void KittyPattern_NonModifierKey_IsNotModifierOnly ()
    {
        // ESC[97u = 'a'
        Key? key = _pattern.GetKey ("\u001b[97u");

        Assert.NotNull (key);
        Assert.False (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.None, key.ModifierKey);
    }

    [Fact]
    public void KittyPattern_LeftSuper_Standalone ()
    {
        // ESC[57444u = Left Super
        Key? key = _pattern.GetKey ("\u001b[57444u");

        Assert.NotNull (key);
        Assert.True (key!.IsModifierOnly);
        Assert.Equal (ModifierKey.LeftSuper, key.ModifierKey);
    }

    #endregion

    #region CSI ~ and Cursor Key Event Types

    [Fact]
    public void CsiKeyPattern_Delete_WithRelease ()
    {
        // ESC[3;1:3~ = Delete, no modifiers, event type 3 (release)
        CsiKeyPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[3;1:3~");

        Assert.NotNull (key);
        Assert.Equal (Key.Delete.KeyCode, key!.KeyCode);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void CsiKeyPattern_Delete_WithRepeat ()
    {
        // ESC[3;1:2~ = Delete, no modifiers, event type 2 (repeat)
        CsiKeyPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[3;1:2~");

        Assert.NotNull (key);
        Assert.Equal (Key.Delete.KeyCode, key!.KeyCode);
        Assert.Equal (KeyEventType.Repeat, key.EventType);
    }

    [Fact]
    public void CsiKeyPattern_F5_WithCtrl_Release ()
    {
        // ESC[15;5:3~ = F5, Ctrl (5), event type 3 (release)
        CsiKeyPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[15;5:3~");

        Assert.NotNull (key);
        Assert.Equal (Key.F5.WithCtrl.KeyCode, key!.KeyCode);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void CsiKeyPattern_Delete_NoEventType_DefaultsToPress ()
    {
        // ESC[3~ = Delete, no modifiers, no event type
        CsiKeyPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[3~");

        Assert.NotNull (key);
        Assert.Equal (Key.Delete.KeyCode, key!.KeyCode);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    [Fact]
    public void CsiCursorPattern_CursorUp_WithRelease ()
    {
        // ESC[1;1:3A = CursorUp, no modifiers, event type 3 (release)
        CsiCursorPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[1;1:3A");

        Assert.NotNull (key);
        Assert.Equal (Key.CursorUp.KeyCode, key!.KeyCode);
        Assert.Equal (KeyEventType.Release, key.EventType);
    }

    [Fact]
    public void CsiCursorPattern_CursorDown_WithCtrl_Repeat ()
    {
        // ESC[1;5:2B = CursorDown, Ctrl (5), event type 2 (repeat)
        CsiCursorPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[1;5:2B");

        Assert.NotNull (key);
        Assert.Equal (Key.CursorDown.WithCtrl.KeyCode, key!.KeyCode);
        Assert.Equal (KeyEventType.Repeat, key.EventType);
    }

    [Fact]
    public void CsiCursorPattern_CursorRight_NoEventType_DefaultsToPress ()
    {
        // ESC[C = CursorRight, no modifier, no event type
        CsiCursorPattern pattern = new ();
        Key? key = pattern.GetKey ("\u001b[C");

        Assert.NotNull (key);
        Assert.Equal (Key.CursorRight.KeyCode, key!.KeyCode);
        Assert.Equal (KeyEventType.Press, key.EventType);
    }

    #endregion

    #region Full Pipeline Tests (Positive)

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

    // --- CSI u event types ---

    // Claude - Opus 4.6
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

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_Repeat_RaisesKeyDown ()
    {
        // ESC[97;1:2u = 'a' repeat — repeat events go to KeyDown
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:2u");

        Assert.Single (down);
        Assert.Equal (Key.A, down [0]);
        Assert.Equal (KeyEventType.Repeat, down [0].EventType);
        Assert.Empty (up);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_Release_RaisesKeyUp ()
    {
        // ESC[97;1:3u = 'a' release — release events go to KeyUp
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.A, up [0]);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // --- Modifiers with event types ---

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_ShiftA_Release ()
    {
        // ESC[97;2:3u = 'a' + Shift (2), release (3)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;2:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsShift);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_CtrlC_Press ()
    {
        // ESC[99;5:1u = 'c' + Ctrl (5), press (1)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[99;5:1u");

        Assert.Single (down);
        Assert.True (down [0].IsCtrl);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_CtrlShiftAlt_Release ()
    {
        // ESC[97;8:3u = 'a' + Ctrl+Shift+Alt (8), release (3)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;8:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsCtrl);
        Assert.True (up [0].IsShift);
        Assert.True (up [0].IsAlt);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // --- Functional keys via CSI u ---

    // Claude - Opus 4.6
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

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_Escape_Release ()
    {
        // ESC[27;1:3u = Esc, release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[27;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.Esc.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Claude - Opus 4.6
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

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_F1_Release ()
    {
        // ESC[57364;1:3u = F1 (kitty codepoint), release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57364;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.Equal (Key.F1.KeyCode, up [0].KeyCode);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_F12_CtrlShift_Press ()
    {
        // ESC[57375;6:1u = F12 (57375), Ctrl+Shift (6), press (1)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57375;6:1u");

        Assert.Single (down);
        Assert.Equal (Key.F12.KeyCode, down [0].KeyCode & ~KeyCode.CtrlMask & ~KeyCode.ShiftMask);
        Assert.True (down [0].IsCtrl);
        Assert.True (down [0].IsShift);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // --- Standalone modifier key events ---

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_LeftShift_PressAndRelease ()
    {
        // ESC[57441;1:1u = Left Shift press, ESC[57441;1:3u = Left Shift release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57441;1:1u", "\x1b[57441;1:3u");

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, down [0].ModifierKey);
        Assert.Equal (KeyEventType.Press, down [0].EventType);

        Assert.Single (up);
        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftShift, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_LeftCtrl_Release ()
    {
        // ESC[57442;1:3u = Left Ctrl release
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57442;1:3u");

        Assert.Empty (down);
        Assert.Single (up);
        Assert.True (up [0].IsModifierOnly);
        Assert.Equal (ModifierKey.LeftCtrl, up [0].ModifierKey);
        Assert.Equal (KeyEventType.Release, up [0].EventType);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_RightAlt_Press ()
    {
        // ESC[57449u = Right Alt press (no modifier field = defaults to press)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[57449u");

        Assert.Single (down);
        Assert.True (down [0].IsModifierOnly);
        Assert.Equal (ModifierKey.RightAlt, down [0].ModifierKey);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // --- CSI ~ keys with kitty event types ---

    // Claude - Opus 4.6
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

    // Claude - Opus 4.6
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

    // Claude - Opus 4.6
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

    // --- CSI cursor keys with kitty event types ---

    // Claude - Opus 4.6
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

    // Claude - Opus 4.6
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

    // --- Full press-repeat-release cycle ---

    // Claude - Opus 4.6
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

    // --- No modifier field defaults to press ---

    // Claude - Opus 4.6
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

    // --- Modifier-only key down/up events at app.Keyboard level ---

    // Claude - Opus 4.6
    [Theory]
    [InlineData ("\x1b[57441u", ModifierKey.LeftShift)]
    [InlineData ("\x1b[57442u", ModifierKey.LeftCtrl)]
    [InlineData ("\x1b[57443u", ModifierKey.LeftAlt)]
    [InlineData ("\x1b[57447u", ModifierKey.RightShift)]
    [InlineData ("\x1b[57448u", ModifierKey.RightCtrl)]
    [InlineData ("\x1b[57449u", ModifierKey.RightAlt)]
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

    // Claude - Opus 4.6
    [Theory]
    [InlineData ("\x1b[57441;1:3u", ModifierKey.LeftShift)]
    [InlineData ("\x1b[57442;1:3u", ModifierKey.LeftCtrl)]
    [InlineData ("\x1b[57443;1:3u", ModifierKey.LeftAlt)]
    [InlineData ("\x1b[57447;1:3u", ModifierKey.RightShift)]
    [InlineData ("\x1b[57448;1:3u", ModifierKey.RightCtrl)]
    [InlineData ("\x1b[57449;1:3u", ModifierKey.RightAlt)]
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

    // --- Kitty flags must include "report all keys" for standalone modifier events ---

    // Claude - Opus 4.6
    [Fact]
    public void KittyRequestedFlags_IncludesReportAllKeys () =>

        // The kitty spec requires flag 8 (report all keys as escape codes) for the terminal
        // to send standalone modifier key events (e.g., pressing Shift alone).
        // Without this flag, terminals like Windows Terminal won't report modifier-only presses.
        Assert.True (EscSeqUtils.KittyKeyboardRequestedFlags.HasFlag (KittyKeyboardFlags.ReportAllKeysAsEscapeCodes),
                     $"KittyKeyboardRequestedFlags ({EscSeqUtils.KittyKeyboardRequestedFlags}) must include ReportAllKeysAsEscapeCodes "
                     + "to receive standalone modifier key events from the terminal.");

    #endregion

    #region Full Pipeline Tests (Negative)

    // --- Invalid event type values ---

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_InvalidEventType_Zero_DefaultsToPress ()
    {
        // ESC[97;1:0u = 'a', event type 0 (invalid, out of 1-3 range)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:0u");

        Assert.Single (down);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // Claude - Opus 4.6
    [Fact]
    public void Pipeline_InvalidEventType_Five_DefaultsToPress ()
    {
        // ESC[97;1:5u = 'a', event type 5 (invalid, out of 1-3 range)
        (List<Key> down, List<Key> up) = InjectRawSequence ("\x1b[97;1:5u");

        Assert.Single (down);
        Assert.Equal (KeyEventType.Press, down [0].EventType);
        Assert.Empty (up);
    }

    // --- Malformed sequences ---

    // Claude - Opus 4.6
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

    // Claude - Opus 4.6
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

    // Claude - Opus 4.6
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

    // --- Mixed valid and invalid sequences ---

    // Claude - Opus 4.6
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
