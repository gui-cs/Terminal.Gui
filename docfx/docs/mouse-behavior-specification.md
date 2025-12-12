# Terminal.Gui Mouse Behavior - Complete Specification

## Based on UICatalog Buttons.cs Analysis

This document specifies the complete mouse behavior for Terminal.Gui, based on actual UICatalog examples and the AppKit-inspired design.

---

## Executive Summary

**Key Design Principles:**

1. **ClickCount is metadata** on every mouse event (AppKit model)
2. **Flag type changes** based on ClickCount (Clicked → DoubleClicked → TripleClicked)
3. **MouseHoldRepeat** is about timer-based repetition, NOT multi-click semantics
4. **MouseState** provides visual feedback, independent of command execution
5. **One event per physical action** - no duplicate event emission

---

## The Three Button Types (from UICatalog Buttons.cs)

### 1. Normal Button (Default)
```csharp
var button = new Button
{
    Title = "Normal Button",
    MouseHoldRepeat = false,  // DEFAULT
    MouseHighlightStates = MouseState.In | MouseState.Pressed  // DEFAULT
};
button.Accepting += (s, e) =>
{
    // Execute action
    e.Handled = true;
};
```

### 2. Repeat Button (Press-and-Hold)
```csharp
var repeatButton = new Button
{
    Title = "Repeat Button",
    MouseHoldRepeat = true,  // ENABLES TIMER
    MouseHighlightStates = MouseState.In | MouseState.Pressed
};
repeatButton.Accepting += (s, e) =>
{
    // Fires repeatedly while held, or once per quick click
    e.Handled = true;
};
```

### 3. No Highlight Button
```csharp
var noHighlight = new Button
{
    Title = "No Visual Feedback",
    MouseHoldRepeat = false,
    MouseHighlightStates = MouseState.None  // NO VISUAL FEEDBACK
};
```

---

## Complete Behavior Matrix

### Normal Button (MouseHoldRepeat = false)

| User Action | MouseState | Accept Count | ClickCount Values | Notes |
|-------------|------------|--------------|-------------------|-------|
| **Single click** (press + immediate release) | Press: `Pressed`<br/>Release: `Unpressed` | **1** | Release: `1` | Standard click |
| **Press and hold** (2+ seconds) | Pressed → stays → Unpressed | **1** | Release: `1` | No timer, single Accept on release |
| **Double-click** (2 quick clicks) | Press→Unpress→Press→Unpress | **2** | Release(1): `1`<br/>Release(2): `2` | Two separate Accept invocations |
| **Triple-click** (3 quick clicks) | 3 press/release cycles | **3** | Release(1): `1`<br/>Release(2): `2`<br/>Release(3): `3` | Three Accept invocations |

**Key Point:** Each release fires Accept. ClickCount tracks which click in the sequence.

---

### Repeat Button (MouseHoldRepeat = true)

| User Action | MouseState | Accept Count | ClickCount Values | Notes |
|-------------|------------|--------------|-------------------|-------|
| **Single click** (press + immediate release) | Press: `Pressed`<br/>Release: `Unpressed` | **1** | Release: `1` | Too fast for timer to start |
| **Press and hold** (2+ seconds) | Pressed → stays → Unpressed | **10+** | All: `1` | Timer fires ~500ms initial, then ~50ms intervals (via `SmoothAcceleratingTimeout`) |
| **Double-click** (2 quick clicks) | Press→Unpress→Press→Unpress | **2** | Release(1): `1`<br/>Release(2): `2` | Two releases = two Accepts (timer doesn't start) |
| **Triple-click** | 3 press/release cycles | **3** | Release(1-3): `1,2,3` | Three releases = three Accepts |
| **Hold then quick click** | Hold: many timer fires<br/>Quick click: one release | **10+ then +1** | Hold: `1` (repeated)<br/>Click: `1` or `2` | Mixed repetition + click |

**Key Point:** Timer fires Accept repeatedly with ClickCount=1. Quick releases also fire Accept with appropriate ClickCount.

---

## Mouse Event Flow (Complete Pipeline)

### Stage 1: ANSI Input → AnsiMouseParser
```
ANSI: ESC[<0;10;5M (button=0, x=10, y=5, terminator='M')
      ESC[<0;10;5m (button=0, x=10, y=5, terminator='m')
      
Output: Mouse { Timestamp = 0, Flags=LeftButtonPressed, ScreenPosition=(9,4) }
        Mouse { Timestamp = 42, Flags=LeftButtonReleased, ScreenPosition=(9,4) }
```

### Stage 2: MouseInterpreter (Click Synthesis + ClickCount)

**Single Click:**
```
Input:  Pressed(time=0, pos=(10,10))
        Released(time=42, pos=(10,10))

Output: Pressed  + ClickCount=1
        Released + ClickCount=1
        Clicked  + ClickCount=1 (synthesized)
```

**Double Click:**
```
Input:  Pressed(time=0, pos=(10,10))
        Released(time=42, pos=(10,10))
        Pressed(time=200, pos=(10,10))   ← Within 500ms threshold
        Released(time=300, pos=(10,10))

Output: Pressed  + ClickCount=1
        Released + ClickCount=1
        Clicked  + ClickCount=1 (synthesized)
        
        Pressed  + ClickCount=2  ← Count incremented!
        Released + ClickCount=2
        DoubleClicked + ClickCount=2 (synthesized, NOT Clicked!)
```

**Triple Click:**
```
Similar pattern, third release emits:
        Released + ClickCount=3
        TripleClicked + ClickCount=3 (synthesized)
```

**Key Behaviors:**
- ClickCount increments on each press if within threshold + same position
- Flag type changes: Clicked → DoubleClicked → TripleClicked
- Both Released AND Clicked/DoubleClicked/TripleClicked are emitted
- Pressed events always emitted with current ClickCount

### Stage 3: MouseImpl (Routing & Grab)
```
1. Find deepest view under mouse
2. Convert screen → viewport coordinates
3. Handle mouse grab (if MouseHighlightStates or WantContinuous)
4. Send to View.NewMouseEvent()
```

### Stage 4: View.NewMouseEvent (Visual State + Commands)

**For Views with MouseHighlightStates:**
```
Pressed  → Grab mouse, MouseState |= Pressed (visual feedback)
Released → MouseState &= ~Pressed, Ungrab
         → Invoke commands bound to Clicked/DoubleClicked/etc.
```

**For Views with MouseHoldRepeat:**
```
Pressed  → Grab mouse, MouseState |= Pressed, Start timer
Timer    → Fire Accept command repeatedly (~50ms intervals using `SmoothAcceleratingTimeout`)
Released → Stop timer, MouseState &= ~Pressed, Ungrab
         → Invoke commands bound to Released
```

---

## Default MouseBindings

### View Base Class (All Views)
```csharp
private void SetupMouse()
{
    MouseBindings = new();
    
    // Pressed → Activate (for selection/interaction on press)
    MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
    MouseBindings.Add (MouseFlags.MiddleButtonPressed, Command.Activate);
    MouseBindings.Add (MouseFlags.Button4Pressed, Command.Activate);
    MouseBindings.Add (MouseFlags.RightButtonPressed, Command.Context);
    MouseBindings.Add (MouseFlags.LeftButtonPressed | MouseFlags.ButtonCtrl, Command.Context);
    
    // Clicked → Accept (single click action)
    MouseBindings.Add(MouseFlags.LeftButtonClicked, Command.Accept);
}
```

### Button Class
```csharp
// Normal button: inherits defaults, uses Accept on Clicked/DoubleClicked

// Repeat button (MouseHoldRepeat = true):
// Sets HightlightStates = MouseState.In | MouseState.Pressed | MouseState.PressedOutside;
// Timer fires Accept repeatedly via MouseHeldDown
// Bindings stay the same - Accept on Clicked/DoubleClicked for quick clicks
```

### ListView Class (Example of Custom Handling)
```csharp
// Option 1: Use ClickCount in handler
MouseBindings.Add(MouseFlags.LeftButtonPressed, Command.Activate);
MouseBindings.Add(MouseFlags.LeftButtonDoubleClicked, Command.Accept);

protected override bool OnActivating(CommandEventArgs args)
{
    if (args.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouse })
    {
        // ClickCount available for custom logic
        SelectItem(mouse.Position);  // Always select on click
        return true;
    }
    return base.OnActivating(args);
}

protected override bool OnAccepting(CommandEventArgs args)
{
    if (args.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouse })
    {
        OpenItem(mouse.Position);  // Open on double-click
        return true;
    }
    return base.OnAccepting(args);
}
```

---

## MouseState vs ClickCount vs Commands

### Three Independent Concerns

| Concern | Purpose | Set By | Used For |
|---------|---------|--------|----------|
| **MouseState** | Visual feedback | View.NewMouseEvent | Button "pressed" appearance, hover effects |
| **ClickCount** | Semantic metadata | MouseInterpreter | Distinguishing single/double/triple click intent |
| **Command** | Action to execute | MouseBindings | Activate, Accept, Toggle, etc. |

### Relationships

```
MouseState.Pressed  ≠  Command.Activate
  ↑ Visual state        ↑ Action execution
  
ClickCount = 2  →  MouseFlags.DoubleClicked  →  Command.Accept
  ↑ Metadata        ↑ Event type                  ↑ Action
```

**Example:** Button with `MouseHighlightStates = MouseState.Pressed`
```
Press   → MouseState |= Pressed (button LOOKS pressed)
        → Command.Activate fires (action on press)
        → ClickCount = 1 (metadata)

Release → MouseState &= ~Pressed (button looks normal)
        → Command.Accept fires (action on release)
        → MouseFlags = LeftButtonClicked (event type)
```

---

## MouseHoldRepeat Deep Dive

### Timer Behavior
```csharp
// When MouseHoldRepeat = true:

Press → Grab → Start Timer (500ms initial delay)
  ↓
Timer.Tick (after 500ms) → Fire Accept
  ↓
Timer.Tick (every ~50ms) → Fire Accept (with 0.5 acceleration)
  ↓
Release → Stop Timer → Ungrab → Fire Accept once more (from release)
```

### ClickCount Interaction

**Hold for 2+ seconds:**
```
Press(ClickCount=1) → Timer starts
Timer fires 10+ times → All with ClickCount=1 (same press sequence)
Release(ClickCount=1) → Timer stops, final Accept
```

**Double-click quickly:**
```
Press(ClickCount=1) → Timer starts but...
Release(ClickCount=1) → Timer stops (< 500ms, never fired), Accept
Press(ClickCount=2) → Timer starts but...
Release(ClickCount=2) → Timer stops, Accept
Total: 2 Accepts (one per release, timer never fired)
```

**Key Insight:** Timer and multi-click are **independent**. Timer repeats with ClickCount=1 until release. Quick clicks don't trigger timer but still track ClickCount.

---

## Implementation Checklist

### MouseInterpreter Changes
- [x] Track ClickCount on all events (Pressed, Released, Clicked, etc.)
- [x] Emit Clicked/DoubleClicked/TripleClicked based on ClickCount
- [x] Immediate emission (no 500ms delay) - ALREADY FIXED
- [ ] Add `Mouse.ClickCount` property

### Mouse Class Changes
```csharp
public class Mouse : HandledEventArgs
{
    // ... existing properties ...
    
    /// <summary>
    /// Number of consecutive clicks at this position (1 = single, 2 = double, 3 = triple).
    /// Tracked on all mouse events (Pressed, Released, Clicked, etc.).
    /// Applications can check this in OnMouseEvent or command handlers to distinguish
    /// single vs double-click intent.
    /// </summary>
    public int ClickCount { get; set; } = 1;
}
```

### View.Mouse Changes
- [ ] No changes needed - default bindings work with new system
- [ ] Documentation updates to explain ClickCount usage
- [ ] Example handlers showing ClickCount checking

### MouseHeldDown (Continuous Press)
- [ ] No changes needed - timer already works correctly
- [ ] Timer fires Accept with ClickCount=1
- [ ] Quick releases bypass timer, fire Accept with appropriate ClickCount

---

## Testing Scenarios

**IMPORTANT** - Some existing tests are currently failing due to the old delayed click behavior. These tests are marked with `Skip = "Broken in #4474"` and need to be updated once the new system is implemented. 

### Test 1: Normal Button Single Click
```
Expected: Accept fires once, ClickCount=1
Action: Press at (10,10), release at (10,10) within 100ms
Result: acceptCount increments by 1
```

### Test 2: Normal Button Double Click
```
Expected: Accept fires twice, ClickCount=1 then ClickCount=2
Action: Click, wait 200ms, click again (both at same position)
Result: acceptCount increments by 2
Binding: First fires LeftButtonClicked, second fires LeftButtonDoubleClicked
```

### Test 3: Repeat Button Hold
```
Expected: Accept fires 10+ times, all ClickCount=1
Action: Press, hold for 2 seconds, release
Result: acceptCount increments by 10+ (timer + final release)
```

### Test 4: Repeat Button Double Click (Quick)
```
Expected: Accept fires twice, ClickCount=1 then ClickCount=2
Action: Click-release, immediately click-release (< 500ms total)
Result: acceptCount increments by 2 (timer never starts)
```

### Test 5: No Highlight Button
```
Expected: Same counts as normal button, no visual state changes
Action: Any click pattern
Result: MouseState never includes Pressed, but Accept fires correctly
```

---

## Migration Guide

### For Application Developers

**Current code (no ClickCount):**
```csharp
button.Accepting += (s, e) =>
{
    // Just execute action
    DoSomething();
    e.Handled = true;
};
```

**New code (with ClickCount for custom behavior):**
```csharp
button.Accepting += (s, e) =>
{
    if (e.Context is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } mouse })
    {
        if (mouse.ClickCount == 2)
        {
            DoSpecialDoubleClickThing();
        }
        else
        {
            DoNormalThing();
        }
        e.Handled = true;
    }
};
```

**Or use separate bindings:**
```csharp
// Clear defaults if needed
MouseBindings.Clear();

// Bind different commands to different click types
MouseBindings.Add(MouseFlags.LeftButtonClicked, Command.Accept);
MouseBindings.Add(MouseFlags.LeftButtonDoubleClicked, Command.Toggle);

// Handle separately
button.Accepting += (s, e) => { DoNormalThing(); e.Handled = true; };
button.Toggling += (s, e) => { DoDoubleClickThing(); e.Handled = true; };
```

### For View Implementers

**No changes needed** - the new system is backwards compatible!

Existing bindings and handlers work exactly the same. ClickCount is **additional metadata** available if needed.

---

## FAQ

**Q: Why emit both Clicked and DoubleClicked for a double-click?**  
A: We emit Clicked on first release, DoubleClicked on second. Apps bind to the flags they care about. This matches how OSes work.

**Q: Should apps track timing themselves for single vs double-click?**  
A: No! Framework provides Clicked vs DoubleClicked flags. Apps just bind to the appropriate flag and handle the corresponding command.

**Q: What about MouseHoldRepeat and double-click?**  
A: They're independent. Timer fires Accept repeatedly with ClickCount=1. Quick double-click fires Accept twice with ClickCount=1 and 2. Both work correctly.

**Q: When is ClickCount actually useful?**  
A: For low-level handlers (OnMouseEvent) or when you want to handle Clicked but behave differently based on ClickCount. Most apps just bind to Clicked vs DoubleClicked flags.

**Q: What if I want different actions on single vs double click?**  
A: Use separate commands:
```csharp
MouseBindings.Add(MouseFlags.LeftButtonClicked, Command.Accept);
MouseBindings.Add(MouseFlags.LeftButtonDoubleClicked, Command.Toggle);
```

**Q: Does MouseState.Pressed relate to ClickCount?**  
A: No. MouseState is visual state (button looks pressed). ClickCount is semantic (which click in a sequence). They're independent.

---

## Summary

✅ **ClickCount on every event** - AppKit-style metadata  
✅ **Flag type changes** - Clicked → DoubleClicked → TripleClicked  
✅ **Immediate emission** - no 500ms delay (already fixed)  
✅ **MouseHoldRepeat** - timer-based, independent of ClickCount  
✅ **MouseState** - visual feedback, independent of commands  
✅ **Backward compatible** - existing code works unchanged  
✅ **Flexible** - apps can use flags OR check ClickCount  

**The design is clean, complete, and ready to implement!**
