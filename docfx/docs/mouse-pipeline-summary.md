# Mouse Event Pipeline - Quick Reference

> **See Also:** 
> - [Complete Mouse Pipeline Documentation](mouse.md#complete-mouse-event-pipeline)
> - [Command System Integration](command.md#command-system-summary)

## TL;DR - The Pipeline

```
ANSI Input → AnsiMouseParser → MouseInterpreter → MouseImpl → View → Commands
   (1-based)     (0-based screen)   (click synthesis)   (routing)  (viewport)  (Activate/Accept)
```

## Stage Summary

| Stage | Input | Output | Key Transformation | State Managed |
|-------|-------|--------|-------------------|---------------|
| **1. ANSI** | User action | `ESC[<0;10;5M` | Hardware event → ANSI | None |
| **2. Parser** | ANSI string | `Mouse{Pressed, Screen(9,4)}` | 1-based → 0-based<br/>Button code → MouseFlags | None |
| **3. Interpreter** | Press/Release | `Mouse{Clicked, Screen(9,4)}` | Press+Release → Clicked<br/>Timing → DoubleClicked | Last click time/pos/button |
| **4. MouseImpl** | Screen coords | `Mouse{Clicked, Viewport(2,1)}` | Screen → Viewport coords<br/>Find target view<br/>Handle grab | MouseGrabView<br/>ViewsUnderMouse |
| **5. View** | Viewport coords | Command invocation | Clicked → Command.Activate<br/>Grab/Ungrab<br/>MouseState updates | MouseState<br/>MouseGrabView |
| **6. Commands** | Command | Event | Activate → Activating<br/>Accept → Accepting | Command handlers |

## Critical Issues & Recommendations

### 🔴 **CRITICAL: Click Delay Bug**
**Problem:** MouseInterpreter defers clicks by 500ms → horrible UX

**Current Behavior:**
```
User clicks → Press (immediate) → Release (immediate) → Clicked (500ms later!) ❌
```

**Required Fix:**
```
User clicks → Press (immediate) → Release (immediate) → Clicked (immediate) ✅
```

**Implementation:** Remove deferred click logic in MouseInterpreter, emit clicks immediately after release

---

### ⚠️ **IMPORTANT: Pressed/Clicked Conversion**
**Problem:** Confusing logic to convert `Pressed` → `Clicked` in multiple places

**Current Behavior:**
- Driver emits `Pressed` and `Released`
- View converts `Pressed` → `Clicked` before binding lookup
- Multiple conversion points, easy to miss

**Recommended Fix:**
- MouseInterpreter already tracks press/release pairs
- Emit `Clicked` directly instead of requiring conversion
- Remove `ConvertPressedToClicked()` logic from View

---

### 💡 **ENHANCEMENT: Add DoubleClicked → Accept Binding**
**Problem:** Applications manually track double-click timing

**Current Behavior:**
```csharp
// ListView must do:
DateTime _lastClick;
if ((now - _lastClick).TotalMilliseconds < 500) 
    OpenItem(); // Accept action
else 
    SelectItem(); // Activate action
```

**Recommended:**
```csharp
// Framework provides:
MouseBindings.Add(MouseFlags.LeftButtonClicked, Command.Activate);
MouseBindings.Add(MouseFlags.LeftButtonDoubleClicked, Command.Accept);

// ListView just handles commands:
protected override bool OnActivating(...) => SelectItem();
protected override bool OnAccepting(...) => OpenItem();
```

---

## Key Concepts

### Coordinates
| Level | Origin | Example |
|-------|--------|---------|
| ANSI | 1-based, top-left = (1,1) | `ESC[<0;10;5M` |
| Screen | 0-based, top-left = (0,0) | `ScreenPosition = (9,4)` |
| Viewport | 0-based, relative to View | `Position = (2,1)` |

### Mouse Flags
| Category | Flags | Purpose |
|----------|-------|---------|
| **Raw Events** | `LeftButtonPressed`, `LeftButtonReleased` | From driver, immediate |
| **Synthetic Events** | `LeftButtonClicked`, `LeftButtonDoubleClicked` | From MouseInterpreter |
| **State** | Motion, Wheel, Modifiers | Continuous state |

### Commands
| Command | Trigger | Example |
|---------|---------|---------|
| `Activate` | Single click, spacebar | Select item, toggle checkbox, set focus |
| `Accept` | Enter, double-click | Execute button, open file, submit dialog |

### Mouse Grab
**When:** View has `MouseHighlightStates` or `MouseHoldRepeat`

**Lifecycle:**
1. **Press inside** → Auto-grab, set focus, `MouseState |= Pressed`
2. **Move outside** → `MouseState |= PressedOutside` (unless `WantContinuous`)
3. **Release inside** → Convert to Clicked, ungrab
4. **Clicked** → Invoke commands

**Grabbed View Receives:**
- ALL mouse events (even if outside viewport)
- Coordinates converted to viewport-relative
- `mouse.View` set to grabbed view

---

## Code Locations

```
Terminal.Gui/
├── Drivers/
│   ├── AnsiHandling/
│   │   └── AnsiMouseParser.cs           ← Stage 2: ANSI → Mouse
│   ├── MouseInterpreter.cs              ← Stage 3: Click synthesis
│   └── MouseButtonClickTracker.cs       ← Tracks button state
├── App/
│   └── Mouse/
│       └── MouseImpl.cs                 ← Stage 4: Routing & grab
└── ViewBase/
    └── View.Mouse.cs                    ← Stage 5: View processing
```

---

## Quick Debugging Checklist

**Mouse event not reaching view?**
1. Is view Enabled and Visible?
2. Is mouse position inside view's Viewport?
3. Is another view occluding it? (check z-order)
4. Is mouse grabbed by another view? (check `App.Mouse.MouseGrabView`)

**Click not invoking command?**
1. Is there a MouseBinding for the click flag?
2. Is the event being handled earlier in the pipeline?
3. Is `MouseHoldRepeat` causing grab behavior?
4. Check if `ConvertPressedToClicked` is being called

**Double-click not working?**
1. Check MouseInterpreter timing threshold (default 500ms)
2. Verify clicks are at same position (exact match required)
3. Ensure same button for both clicks
4. Application tracking timing? (see ListView example)

**Grab not releasing?**
1. Is `WhenGrabbedHandleClicked` being called?
2. Is mouse inside viewport when released?
3. Is `MouseHoldRepeat` preventing ungrab?

---

## Testing Tips

**IMPORTANT** - Some existing tests are currently failing due to the old delayed click behavior. These tests are marked with `Skip = "Broken in #4474"` and need to be updated once the new system is implemented. 

**Unit Tests:**
```csharp
// Test click synthesis
var interpreter = new MouseInterpreter();
interpreter.Process(new Mouse { Flags = LeftButtonPressed, Position = (10, 5) });
var clicked = interpreter.Process(new Mouse { Flags = LeftButtonReleased, Position = (10, 5) });
Assert.Equal(MouseFlags.LeftButtonClicked, clicked.Flags);

// Test double-click timing
// ... wait < 500ms ...
var doubleClick = interpreter.Process(new Mouse { Flags = LeftButtonReleased, Position = (10, 5) });
Assert.Equal(MouseFlags.LeftButtonDoubleClicked, doubleClick.Flags);
```

**Integration Tests:**
```csharp
// Test view command invocation
var view = new TestView();
var activateCalled = false;
view.Activating += (s, e) => activateCalled = true;

view.NewMouseEvent(new Mouse { 
    Flags = MouseFlags.LeftButtonClicked, 
    Position = (5, 5) 
});
Assert.True(activateCalled);
```

**Trace Logging:**
```csharp
// Enable in each pipeline stage:
Logging.Trace($"[AnsiParser] {input} → {mouse.Flags} at {mouse.ScreenPosition}");
Logging.Trace($"[Interpreter] {inFlags} → {outFlags}, clicks={clickCount}");
Logging.Trace($"[MouseImpl] Target={view.Id}, Grabbed={MouseGrabView?.Id}");
Logging.Trace($"[View] {mouse.Flags} → Command.{command}");
```

---

## Migration Guide (Breaking Changes OK)

### For Application Developers

**If you track double-click timing manually:**
```csharp
// OLD:
DateTime _lastClick;
view.Activating += (s, e) => {
    if ((DateTime.Now - _lastClick).TotalMilliseconds < 500)
        OpenItem();
    else
        SelectItem();
    _lastClick = DateTime.Now;
};

// NEW (after framework provides DoubleClicked → Accept):
view.Activating += (s, e) => SelectItem();
view.Accepting += (s, e) => OpenItem();
```

### For View Implementers

**If you override `OnMouseEvent`:**
```csharp
// BEFORE: Check for Pressed OR Clicked
protected override bool OnMouseEvent(Mouse mouse) {
    if (mouse.Flags.HasFlag(MouseFlags.LeftButtonPressed) || 
        mouse.Flags.HasFlag(MouseFlags.LeftButtonClicked))
        // ...
}

// AFTER: Only check Clicked (Pressed used for grab only)
protected override bool OnMouseEvent(Mouse mouse) {
    if (mouse.Flags.HasFlag(MouseFlags.LeftButtonClicked))
        // ...
}
```

**If you use `MouseHoldRepeat`:**
- No changes needed - grab behavior unchanged
- But understand it now auto-grabs on press

---

## See Also

- [Complete Pipeline Documentation](mouse.md#complete-mouse-event-pipeline)
- [Command System](command.md)
- [Mouse Behavior Tables](mouse.md#mouse-behavior---end-users-perspective)
- [Issue #4471 - Click Delay Bug](https://github.com/gui-cs/Terminal.Gui/issues/4471)
- [Issue #4473 - Command Propagation](https://github.com/gui-cs/Terminal.Gui/issues/4473)
