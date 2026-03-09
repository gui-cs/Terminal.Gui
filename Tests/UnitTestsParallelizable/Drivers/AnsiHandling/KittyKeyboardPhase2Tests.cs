// Claude - Opus 4.6

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
}
