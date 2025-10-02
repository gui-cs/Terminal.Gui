# CWP Pattern Analysis - Executive Summary & Recommendations

## Quick Facts

- **Total CWP Implementations**: 33+ event pairs
- **Virtual Method Overrides**: 100+ across codebase
- **Current Order**: Virtual method → Event (inheritance priority)
- **Proposed Order**: Event → Virtual method (composition priority)
- **Breaking Change Magnitude**: VERY HIGH
- **Estimated Effort**: 4-6 weeks for complete refactor
- **Risk Level**: HIGH

## The Core Issue (#3714)

External code cannot prevent views like `Slider` from handling mouse events because:

```csharp
// What users want but CANNOT do today:
slider.MouseEvent += (s, e) => 
{
    if (someCondition)
        e.Handled = true; // TOO LATE! Slider.OnMouseEvent already handled it
};
```

The virtual method (`OnMouseEvent`) runs first and can mark the event as handled, preventing the event from firing.

## Four Solution Options

### Option 1: Reverse Order Globally ⚠️ HIGH RISK

**Change**: Event first → Virtual method second (for all 33+ CWP patterns)

**Pros**:
- Solves #3714 completely
- Consistent pattern everywhere
- Enables external control (composition over inheritance)
- Single comprehensive solution

**Cons**:
- MAJOR breaking change
- 100+ view overrides need review/update
- Changes fundamental framework philosophy
- Breaks existing user code expectations
- Requires extensive testing
- Updates to helper classes, documentation, tests

**Effort**: 4-6 weeks
**Risk**: VERY HIGH
**Recommendation**: ❌ NOT RECOMMENDED unless major version change

### Option 2: Add "Before" Events 🎯 RECOMMENDED

**Change**: Add new pre-phase events (e.g., `BeforeMouseEvent`, `BeforeKeyDown`)

```csharp
// New pattern for critical events
protected bool RaiseMouseEvent(MouseEventArgs args)
{
    // New: BeforeMouseEvent (external code gets first chance)
    BeforeMouseEvent?.Invoke(this, args);
    if (args.Handled)
        return true;
    
    // Existing: OnMouseEvent (virtual method)
    if (OnMouseEvent(args) || args.Handled)
        return true;
    
    // Existing: MouseEvent (external code gets second chance)
    MouseEvent?.Invoke(this, args);
    return args.Handled;
}
```

**Pros**:
- Non-breaking (additive change only)
- Solves #3714 for mouse events
- Can apply selectively to problematic events
- Gradual migration path
- Clear intent (Before/During/After phases)

**Cons**:
- More API surface
- Two patterns coexist (inconsistency)
- More complex for new developers
- Partial solution only

**Implementation Plan**:
1. **Phase 1** (1 week): Add `BeforeMouseEvent`, `BeforeMouseClick`, `BeforeMouseWheel`
2. **Phase 2** (1 week): Add `BeforeKeyDown` if needed
3. **Phase 3** (ongoing): Add others as needed, deprecate old pattern gradually

**Effort**: 2-3 weeks initial, ongoing refinement
**Risk**: LOW
**Recommendation**: ✅ **RECOMMENDED** - Best balance of solving issue vs. risk

### Option 3: Add `MouseInputEnabled` Property 🔧 QUICK FIX

**Change**: Add property to View to disable mouse handling

```csharp
public class View
{
    public bool MouseInputEnabled { get; set; } = true;
    
    public bool? NewMouseEvent(MouseEventArgs mouseEvent)
    {
        if (!MouseInputEnabled)
            return false; // Don't process
            
        // ... existing code
    }
}

// Usage
slider.MouseInputEnabled = false; // Disable slider mouse handling
```

**Pros**:
- Minimal change
- Solves immediate #3714 issue
- No breaking changes
- Easy to implement
- Easy to understand

**Cons**:
- Band-aid solution (doesn't address root cause)
- Mouse-specific (doesn't help keyboard, etc.)
- All-or-nothing (can't selectively disable)
- Adds more properties to View

**Effort**: 1 week
**Risk**: VERY LOW
**Recommendation**: ⚠️ Acceptable as short-term fix, not long-term solution

### Option 4: Reverse Order for Mouse Only 🎯 TARGETED FIX

**Change**: Event first → Virtual method second, but ONLY for mouse events

**Pros**:
- Solves #3714 directly
- Limited scope reduces risk
- Mouse is the primary concern
- Clearer than "Before" events

**Cons**:
- Inconsistent pattern (mouse different from keyboard)
- Still breaking for mouse event overrides
- Confusing to have different orders
- Doesn't help future similar issues

**Effort**: 2 weeks
**Risk**: MEDIUM
**Recommendation**: ⚠️ Better than Option 1, but Option 2 is cleaner

## Detailed Recommendation: Option 2 (Before Events)

### Implementation Specification

#### For Mouse Events (High Priority)

```csharp
public partial class View // Mouse APIs
{
    /// <summary>
    /// Event raised BEFORE OnMouseEvent is called, allowing external code
    /// to cancel mouse event processing before the view handles it.
    /// </summary>
    public event EventHandler<MouseEventArgs>? BeforeMouseEvent;
    
    /// <summary>
    /// Event raised BEFORE OnMouseClick is called, allowing external code
    /// to cancel mouse click processing before the view handles it.
    /// </summary>
    public event EventHandler<MouseEventArgs>? BeforeMouseClick;
    
    public bool RaiseMouseEvent (MouseEventArgs mouseEvent)
    {
        // Phase 1: Before event (external pre-processing)
        BeforeMouseEvent?.Invoke(this, mouseEvent);
        if (mouseEvent.Handled)
        {
            return true;
        }
        
        // Phase 2: Virtual method (view processing)
        if (OnMouseEvent (mouseEvent) || mouseEvent.Handled)
        {
            return true;
        }

        // Phase 3: After event (external post-processing)
        MouseEvent?.Invoke (this, mouseEvent);

        return mouseEvent.Handled;
    }
    
    protected bool RaiseMouseClickEvent (MouseEventArgs args)
    {
        if (!Enabled)
        {
            return args.Handled = false;
        }

        // Phase 1: Before event
        BeforeMouseClick?.Invoke(this, args);
        if (args.Handled)
        {
            return args.Handled;
        }

        // Phase 2: Virtual method
        if (OnMouseClick (args) || args.Handled)
        {
            return args.Handled;
        }

        // Phase 3: After event
        MouseClick?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        // Post-conditions
        args.Handled = InvokeCommandsBoundToMouse (args) == true;

        return args.Handled;
    }
}
```

#### Usage Example (Solves #3714)

```csharp
var slider = new Slider();

// NOW THIS WORKS - external code can prevent slider from handling mouse
slider.BeforeMouseEvent += (sender, args) =>
{
    if (someCondition)
    {
        args.Handled = true; // Prevents Slider.OnMouseEvent from being called
    }
};

// Or use lambda with specific logic
slider.BeforeMouseEvent += (sender, args) =>
{
    if (args.Flags.HasFlag(MouseFlags.Button1Clicked) && IsModalDialogOpen())
    {
        args.Handled = true; // Block slider interaction when dialog is open
    }
};
```

#### For Keyboard Events (Medium Priority)

```csharp
public partial class View // Keyboard APIs
{
    /// <summary>
    /// Event raised BEFORE OnKeyDown is called, allowing external code
    /// to cancel key event processing before the view handles it.
    /// </summary>
    public event EventHandler<Key>? BeforeKeyDown;
    
    public bool NewKeyDownEvent (Key k)
    {
        // Phase 1: Before event
        BeforeKeyDown?.Invoke(this, k);
        if (k.Handled)
        {
            return true;
        }
        
        // Phase 2: Virtual method
        if (OnKeyDown (k) || k.Handled)
        {
            return true;
        }

        // Phase 3: After event (existing KeyDown)
        KeyDown?.Invoke (this, k);

        return k.Handled;
    }
}
```

#### For Other Events (Low Priority - As Needed)

Add `Before*` events for other patterns only if/when users request them.

### Migration Path

#### v2.x (Current)
- Keep existing pattern
- Add `Before*` events
- Document both patterns
- Mark as "preferred" for external control

#### v3.0 (Future)
- Consider deprecating old pattern
- Maybe reverse order globally
- Or standardize on Before/After pattern

### Documentation Updates

Add to `docfx/docs/events.md`:

````markdown
#### Three-Phase CWP Pattern

For critical events like mouse and keyboard input, a three-phase pattern is available:

```csharp
public class MyView : View
{
    public bool ProcessMouseEvent(MouseEventArgs args)
    {
        // Phase 1: BeforeMouseEvent - External pre-processing
        BeforeMouseEvent?.Invoke(this, args);
        if (args.Handled) return true;
        
        // Phase 2: OnMouseEvent - View processing (virtual method)
        if (OnMouseEvent(args) || args.Handled) return true;
        
        // Phase 3: MouseEvent - External post-processing
        MouseEvent?.Invoke(this, args);
        return args.Handled;
    }
}
```

**When to use each phase**:

- **BeforeXxx**: Use when you need to prevent the view from processing the event
  - Example: Disabling a slider when a modal dialog is open
  - Example: Implementing global keyboard shortcuts

- **OnXxx**: Override in derived classes to customize view behavior
  - Example: Custom mouse handling in a custom view
  - Example: Custom key handling in a specialized control

- **Xxx**: Use for external observation and additional processing
  - Example: Logging mouse activity
  - Example: Implementing additional behavior without inheritance
````

### Testing Strategy

1. **Unit Tests**: Add tests for new Before* events
2. **Integration Tests**: Test interaction between phases
3. **Scenario Tests**: Test #3714 scenario specifically
4. **Regression Tests**: Ensure existing code still works
5. **Documentation Tests**: Verify examples work

### Benefits of This Approach

1. **Solves #3714**: Users can now prevent Slider from handling mouse
2. **Non-Breaking**: All existing code continues to work
3. **Clear Intent**: "Before" explicitly means "external pre-processing"
4. **Selective Application**: Add to problematic events, not all 33
5. **Future-Proof**: Creates migration path to v3.0
6. **Minimal Risk**: Additive only, no changes to existing behavior
7. **Gradual Adoption**: Users can adopt at their own pace

### Risks & Mitigations

**Risk**: Three phases more complex than two
**Mitigation**: Good documentation, clear examples, gradual rollout

**Risk**: People still use wrong phase
**Mitigation**: Intellisense XML docs explain when to use each

**Risk**: Two patterns coexist
**Mitigation**: Document clearly, provide migration examples

## Alternative Considerations

### Could We Just Fix Slider?

No - this is a systemic issue. Any view that overrides `OnMouseEvent` has the same problem. Fixing just Slider doesn't help:
- ListView
- TextView
- TableView
- TreeView
- ScrollBar
- And 20+ others

### Could We Change Slider To Not Override OnMouseEvent?

Yes, but:
- Still doesn't solve systemic issue
- Other views have same problem
- Not a general solution
- Slider's implementation is reasonable

### Could We Add Multiple Overload Virtual Methods?

E.g., `OnMouseEventBefore` and `OnMouseEventAfter`?

No - virtual methods can't solve this because:
- External code can't override virtual methods
- Virtual methods are for inheritance, not composition
- Issue is specifically about external code priority

## Implementation Checklist (Option 2)

- [ ] **Week 1: Mouse Events**
  - [ ] Add `BeforeMouseEvent` to View
  - [ ] Add `BeforeMouseClick` to View
  - [ ] Add `BeforeMouseWheel` to View
  - [ ] Update `RaiseMouseEvent` to invoke BeforeMouseEvent
  - [ ] Update `RaiseMouseClickEvent` to invoke BeforeMouseClick
  - [ ] Update `RaiseMouseWheelEvent` to invoke BeforeMouseWheel
  - [ ] Add unit tests for new events
  - [ ] Test Slider scenario specifically

- [ ] **Week 2: Documentation & Examples**
  - [ ] Update `events.md` with three-phase pattern
  - [ ] Update `cancellable-work-pattern.md` with new pattern
  - [ ] Add examples showing how to use BeforeMouseEvent
  - [ ] Add example solving #3714 specifically
  - [ ] Update API docs (XML comments)

- [ ] **Week 3: Keyboard Events (If Needed)**
  - [ ] Assess if keyboard has similar issues
  - [ ] Add `BeforeKeyDown` if needed
  - [ ] Add unit tests
  - [ ] Update documentation

- [ ] **Week 4: Other Events (As Needed)**
  - [ ] Assess other event patterns
  - [ ] Add Before* events as needed
  - [ ] Update documentation

- [ ] **Week 5-6: Testing & Refinement**
  - [ ] Integration testing
  - [ ] Scenario testing
  - [ ] User testing
  - [ ] Bug fixes
  - [ ] Documentation refinement

## Conclusion

**Recommended Solution**: **Option 2 - Add Before Events**

This solution:
✅ Solves issue #3714
✅ Non-breaking change
✅ Clear and understandable
✅ Minimal risk
✅ Provides migration path
✅ Can be applied selectively

Estimated effort: **2-3 weeks**
Risk level: **LOW**

The key insight is that we don't need to change the existing pattern - we can ADD to it. This preserves all existing behavior while enabling the new use cases that #3714 requires.

## References

- Issue #3714: Mouse event interception problem
- Issue #3417: Related mouse event handling issue
- `docfx/docs/events.md`: Current CWP documentation
- `docfx/docs/cancellable-work-pattern.md`: CWP philosophy
- `Terminal.Gui/ViewBase/View.Mouse.cs`: Mouse event implementation
- `Terminal.Gui/ViewBase/View.Keyboard.cs`: Keyboard event implementation
- `Terminal.Gui/App/CWP/CWPWorkflowHelper.cs`: CWP helper class
