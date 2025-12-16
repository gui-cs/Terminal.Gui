# Terminal.Gui Mouse Behavior - Complete Specification

## Executive Summary

**Key Design Principles:**

1. **Decouple Drivers from Application from Views**
2. **ANSI Mouse Esc Sequences** define core 'click' behavior (`press`->`release` is all that's sent).
3. **ClickCount is metadata** on every mouse event (AppKit model)
4. **Clicks are synthetic** and **Flag type changes** based on ClickCount (Clicked → DoubleClicked → TripleClicked)
5. **MouseHoldRepeat** is about timer-based repetition, NOT multi-click semantics
6. **MouseState** supports visual feedback, independent of command execution
7. **One event per physical action** - no duplicate event emission; IOW, Driver's follow ANSI model, synthesizing click events in a consistent way.

`Button` is a key example as it supports use cases that exercise a broad swath of functionality. Other built-in `View` subclasses that drive the design:

- `CheckBox` - Supports `MouseHightlightStates`, can act as both a checkbox and a radio-button, double-click to accept, and is used within the `Selector`s and `Shortcut`, requiring `Command` propagation.
- `FlagSelector`/`OptionSelector` -
- `ListView` - Currently does not support `MouseHightlightStates` or `MouseHeldDown` scenarios.

## The Three Button Types

### 1. Normal Button (Default) - Visual feedback on hover & press; no repeat by default
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

### 2. Repeat Button (Press-and-Hold) - Visual feedback on hover & press; repeat enabled
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

### 3. No Highlight Button - Visual feedback disabled; no repeat - acts just like ViewBase.
```csharp
var noHighlight = new Button
{
    Title = "No Visual Feedback",
    MouseHoldRepeat = false,
    MouseHighlightStates = MouseState.None  // NO VISUAL FEEDBACK
};
```

## Behavior Matrix

### Normal Button (MouseHoldRepeat = false)

| User Action | MouseState | Accept Count | ClickCount Values | Notes |
|-------------|------------|--------------|-------------------|-------|
| **Single click** (press + immediate release) | Press: `Pressed`<br/>Release: `Unpressed` | **1** | Release: `1` | Standard click |
| **Press and hold** (2+ seconds) | Pressed → stays → Unpressed | **1** | Release: `1` | No timer, single Accept on release |
| **Double-click** (2 quick clicks) | Press→Unpress→Press→Unpress | **2** | Release(1): `1`<br/>Release(2): `2` | Two separate Accept invocations |
| **Triple-click** (3 quick clicks) | 3 press/release cycles | **3** | Release(1): `1`<br/>Release(2): `2`<br/>Release(3): `3` | Three Accept invocations |

**Key Point:** Each release fires Accept. ClickCount tracks which click in the sequence.

### Repeat Button (MouseHoldRepeat = true)

**Key Principle:** MouseHoldRepeat buttons respond to **Press and Release events only**. Click/DoubleClick/TripleClick synthesized events are **ignored**. Each Press/Release cycle fires exactly one Accept (plus timer-generated Accepts during holds).

| User Action | MouseState | Accept Count | Notes |
|-------------|------------|--------------|-------|
| **Single click** (press + immediate release) | Press: `Pressed`<br/>Release: `Unpressed` | **1** | Too fast for timer to start; one Accept on release |
| **Press and hold** (2+ seconds) | Pressed → stays → Unpressed | **10+** | Timer fires ~500ms initial, then ~50ms intervals (via `SmoothAcceleratingTimeout`), plus 1 final Accept on release |
| **Double-click** (2 quick clicks) | Press→Release→Press→Release | **2** | Two Press/Release cycles = two Accepts (Click/DoubleClick events ignored) |
| **Triple-click** | 3 press/release cycles | **3** | Three Press/Release cycles = three Accepts (Click/DoubleClick/TripleClick events ignored) |
| **Hold then quick click** | Hold: many timer fires<br/>Quick click: one release | **10+ then +1** | Hold generates many Accepts via timer, quick click adds one more |

**Implementation Rule:** When `MouseHoldRepeat = true`, `View.NewMouseEvent` must ignore `IsSingleDoubleOrTripleClicked` events and only invoke commands on **Press** (to start timer) and **Release** (to fire final Accept and stop timer). This ensures every click fires Accept once, regardless of multi-click detection.

**Key Point:** Timer and multi-click are **independent** concepts. Timer repeats during continuous hold. Multi-click (double/triple) is detected but **ignored** when MouseHoldRepeat is true—only the Press/Release events matter.

## Mouse Event Flow (Pipeline)

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

**IMPORTANT:** When `MouseHoldRepeat = true`, Views must use **Press/Release events only**. Click/DoubleClick/TripleClick synthesized events are ignored to ensure consistent behavior.

**Hold for 2+ seconds:**
```
Press → Timer starts
Timer fires 10+ times → Accept fires repeatedly
Release → Timer stops, one final Accept
Total: 10+ Accepts (all from timer + release)
```

**Double-click quickly:**
```
Press #1 → Timer starts but...
Release #1 → Timer stops (< 500ms, never fired), one Accept
Press #2 → Timer starts but...
Release #2 → Timer stops, one Accept
Total: 2 Accepts (one per Press/Release cycle)
ClickCount is tracked but Click/DoubleClick events are IGNORED
```

**Triple-click quickly:**
```
Press #1 → Timer starts but...
Release #1 → Timer stops (< 500ms, never fired), one Accept
Press #2 → Timer starts but...
Release #2 → Timer stops, one Accept
Press #3 → Timer starts but...
Release #3 → Timer stops, one Accept
Total: 3 Accepts (one per Press/Release cycle)
ClickCount is tracked but Click/DoubleClick/TripleClick events are IGNORED
```

**Key Insight:** When `MouseHoldRepeat = true`, only Press/Release events matter. Click/DoubleClick/TripleClick are synthesized but **must be ignored** by the View's event handling logic. Each Press/Release cycle fires exactly one Accept (plus any timer-generated Accepts during the hold).

## Testing Scenarios

**IMPORTANT** - Some existing tests are currently failing due to the old delayed click behavior. These tests are marked with `Skip = "Broken in #4474"` and need to be updated once the new system is implemented. 

### Test 1: Normal Button Single Click
```
Expected: Accept fires once
Action: Press at (10,10), release at (10,10) within 100ms
Result: acceptCount increments by 1
Uses: LeftButtonClicked event binding
```

### Test 2: Normal Button Double Click
```
Expected: Accept fires twice
Action: Click, wait 200ms, click again (both at same position)
Result: acceptCount increments by 2
Uses: First fires LeftButtonClicked, second fires LeftButtonDoubleClicked (both must have bindings)
```

### Test 3: Repeat Button Hold
```
Expected: Accept fires 10+ times
Action: Press, hold for 2 seconds, release
Result: acceptCount increments by 10+ (timer + final release)
Uses: Press/Release events only (timer fires on Press, stops on Release)
```

### Test 4: Repeat Button Double Click (Quick)
```
Expected: Accept fires twice (one per Press/Release cycle)
Action: Press-release, immediately press-release (< 500ms total)
Result: acceptCount increments by 2
Uses: Press/Release events only (Click/DoubleClick events are IGNORED)
Implementation: View.NewMouseEvent must NOT invoke commands on IsSingleDoubleOrTripleClicked when MouseHoldRepeat=true
```

### Test 5: Repeat Button Triple Click
```
Expected: Accept fires three times (one per Press/Release cycle)
Action: Three quick press-release cycles
Result: acceptCount increments by 3
Uses: Press/Release events only (Click/DoubleClick/TripleClick events are IGNORED)
```

### Test 6: No Highlight Button
```
Expected: Same counts as normal button, no visual state changes
Action: Any click pattern
Result: MouseState never includes Pressed, but Accept fires correctly
```

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
A: You have to distinguish and ensure semantics work. Eg if you use separate commands per below, both will be invoked on a double-click. The use case will either have to be ok with Accept happening always, before Toggle, or you'll need to be able to "undo" Accept on Toggle. Or similar. 
```csharp
MouseBindings.Add(MouseFlags.LeftButtonClicked, Command.Accept);
MouseBindings.Add(MouseFlags.LeftButtonDoubleClicked, Command.Toggle);
```

**Q: Does MouseState.Pressed relate to ClickCount?**  
A: No. MouseState is visual state (button looks pressed). ClickCount is semantic (which click in a sequence). They're independent.
