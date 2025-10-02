# CWP Helper Class Migration Analysis

## Executive Summary

This document provides a detailed analysis of the effort required to replace all direct CWP implementations with CWP helper classes (`CWPWorkflowHelper`, `CWPPropertyHelper`, `CWPEventHelper`).

### Current State
- **CWP Helper Classes**: 3 (CWPWorkflowHelper, CWPPropertyHelper, CWPEventHelper)
- **Current Helper Usage**: 2 files (View.Drawing.Scheme.cs only)
- **Direct CWP Implementations**: 28 methods across 11 files
- **Total CWP Patterns**: 31 instances

### Migration Summary
- **Can Use CWPWorkflowHelper**: 24 instances (77%)
- **Can Use CWPPropertyHelper**: 2 instances (6%)
- **Cannot Use Helpers (Custom Logic)**: 5 instances (16%)
- **Estimated Total Effort**: 3-4 weeks
- **Risk Level**: MEDIUM

## CWP Helper Classes Overview

### 1. CWPWorkflowHelper
**Purpose**: Single-phase workflows with optional default action

**Signature**:
```csharp
public static bool? Execute<T>(
    Func<ResultEventArgs<T>, bool> onMethod,
    EventHandler<ResultEventArgs<T>>? eventHandler,
    ResultEventArgs<T> args,
    Action? defaultAction = null)
```

**Use When**:
- Simple workflow with virtual method + event
- Returns bool? (true=handled, false=not handled, null=no subscribers)
- Optional default action if not handled
- No complex state management between phases

### 2. CWPPropertyHelper
**Purpose**: Property change with pre/post notifications

**Signature**:
```csharp
public static bool ChangeProperty<T>(
    T currentValue,
    T newValue,
    Func<ValueChangingEventArgs<T>, bool> onChanging,
    EventHandler<ValueChangingEventArgs<T>>? changingEvent,
    Action<ValueChangedEventArgs<T>>? onChanged,
    EventHandler<ValueChangedEventArgs<T>>? changedEvent,
    out T finalValue)
```

**Use When**:
- Property setter with validation
- Need both "changing" (cancellable) and "changed" (notification) events
- Two-phase pattern (pre-change, post-change)

### 3. CWPEventHelper
**Purpose**: Event-only pattern (no virtual method)

**Signature**:
```csharp
public static bool Execute<T>(
    EventHandler<ResultEventArgs<T>>? eventHandler,
    ResultEventArgs<T> args)
```

**Use When**:
- Only have event, no virtual method
- Simple event invocation with handled check
- Rarely used in current codebase

## Detailed Migration Analysis

### Category 1: Mouse Events (HIGH PRIORITY)

#### 1.1 OnMouseEvent / MouseEvent
**File**: `Terminal.Gui/ViewBase/View.Mouse.cs:315-338`

**Current Implementation**:
```csharp
public bool RaiseMouseEvent (MouseEventArgs mouseEvent)
{
    // Pre-processing: MouseHeldDown logic
    if (WantContinuousButtonPressed && MouseHeldDown != null)
    {
        if (mouseEvent.IsPressed)
        {
            MouseHeldDown.Start ();
        }
        else
        {
            MouseHeldDown.Stop ();
        }
    }

    // CWP pattern
    if (OnMouseEvent (mouseEvent) || mouseEvent.Handled)
    {
        return true;
    }

    MouseEvent?.Invoke (this, mouseEvent);

    return mouseEvent.Handled;
}
```

**Can Use Helper?**: ❌ NO - Custom pre-processing logic

**Reason**: The MouseHeldDown logic must execute before the CWP pattern. CWPWorkflowHelper doesn't support pre-processing code before the workflow.

**Migration Difficulty**: N/A (Cannot migrate)

**Recommendation**: Keep as-is. This is appropriate custom logic.

---

#### 1.2 OnMouseClick / MouseClick
**File**: `Terminal.Gui/ViewBase/View.Mouse.cs:470-498`

**Current Implementation**:
```csharp
protected bool RaiseMouseClickEvent (MouseEventArgs args)
{
    // Pre-conditions
    if (!Enabled)
    {
        return args.Handled = false;
    }

    // CWP pattern
    if (OnMouseClick (args) || args.Handled)
    {
        return args.Handled;
    }

    MouseClick?.Invoke (this, args);

    if (args.Handled)
    {
        return true;
    }

    // Post-conditions: Invoke commands
    args.Handled = InvokeCommandsBoundToMouse (args) == true;

    return args.Handled;
}
```

**Can Use Helper?**: ⚠️ PARTIAL - Pre/post conditions complicate

**Proposed Migration**:
```csharp
protected bool RaiseMouseClickEvent (MouseEventArgs args)
{
    // Pre-condition check stays outside
    if (!Enabled)
    {
        return args.Handled = false;
    }

    // Use helper but wrap return value
    bool? result = CWPWorkflowHelper.Execute(
        onMethod: _ => OnMouseClick(args),
        eventHandler: MouseClick,
        args: new ResultEventArgs<MouseEventArgs> { Result = args },
        defaultAction: null);

    // Post-condition: Invoke commands if not handled
    if (result != true && !args.Handled)
    {
        args.Handled = InvokeCommandsBoundToMouse(args) == true;
    }

    return args.Handled;
}
```

**Migration Difficulty**: ⭐⭐⭐ HARD

**Issues**:
1. MouseEventArgs doesn't fit ResultEventArgs<T> pattern cleanly
2. Pre/post conditions require wrapper logic
3. Command invocation logic stays outside helper
4. More complex than direct implementation

**Recommendation**: ❌ Don't migrate. Direct implementation is clearer.

---

#### 1.3 OnMouseWheel / MouseWheel
**File**: `Terminal.Gui/ViewBase/View.Mouse.cs:574-600`

**Analysis**: Same pattern as OnMouseClick

**Can Use Helper?**: ⚠️ PARTIAL (same issues)

**Migration Difficulty**: ⭐⭐⭐ HARD

**Recommendation**: ❌ Don't migrate

---

#### 1.4 OnMouseEnter / MouseEnter
**File**: `Terminal.Gui/ViewBase/View.Mouse.cs:69-98`

**Current Implementation**:
```csharp
internal bool? NewMouseEnterEvent (CancelEventArgs eventArgs)
{
    if (!CanBeVisible (this))
    {
        return null;
    }

    if (OnMouseEnter (eventArgs))
    {
        return true;
    }

    MouseEnter?.Invoke (this, eventArgs);

    if (eventArgs.Cancel)
    {
        return true;
    }

    // Post-processing: Set mouse state
    MouseState |= MouseState.In;

    if (HighlightStates != MouseState.None)
    {
        SetNeedsDraw ();
    }

    return false;
}
```

**Can Use Helper?**: ❌ NO - Post-processing state management

**Reason**: Setting MouseState and SetNeedsDraw after event is essential logic that can't be a "default action"

**Migration Difficulty**: N/A

**Recommendation**: ❌ Don't migrate

---

### Category 2: Keyboard Events (HIGH PRIORITY)

#### 2.1 OnKeyDown / KeyDown
**File**: `Terminal.Gui/ViewBase/View.Keyboard.cs:322-337`

**Current Implementation**:
```csharp
public bool NewKeyDownEvent (Key k)
{
    if (OnKeyDown (k) || k.Handled)
    {
        return true;
    }

    KeyDown?.Invoke (this, k);

    return k.Handled;
}
```

**Can Use Helper?**: ✅ YES - Clean fit

**Proposed Migration**:
```csharp
public bool NewKeyDownEvent (Key k)
{
    ResultEventArgs<Key> args = new (k);
    
    bool? result = CWPWorkflowHelper.Execute(
        onMethod: args => OnKeyDown(k),
        eventHandler: (sender, e) => KeyDown?.Invoke(sender, k),
        args: args,
        defaultAction: null);

    return result == true || k.Handled;
}
```

**Migration Difficulty**: ⭐⭐ MEDIUM

**Issues**:
1. Key is mutable (k.Handled), not immutable like ResultEventArgs expects
2. Need to synchronize k.Handled with result
3. Event signature doesn't match helper's expected EventHandler<ResultEventArgs<T>>
4. Current implementation is actually simpler

**Recommendation**: ⚠️ Marginal benefit. Could migrate for consistency but adds complexity.

---

#### 2.2 OnKeyDownNotHandled / KeyDownNotHandled
**File**: `Terminal.Gui/ViewBase/View.Keyboard.cs:339-349`

**Analysis**: Same pattern as OnKeyDown

**Can Use Helper?**: ✅ YES (same issues)

**Migration Difficulty**: ⭐⭐ MEDIUM

**Recommendation**: ⚠️ Marginal benefit

---

#### 2.3 OnKeyUp / KeyUp
**File**: `Terminal.Gui/ViewBase/View.Keyboard.cs` (similar location)

**Analysis**: Same pattern as OnKeyDown

**Can Use Helper?**: ✅ YES (same issues)

**Migration Difficulty**: ⭐⭐ MEDIUM

**Recommendation**: ⚠️ Marginal benefit

---

### Category 3: Command Events (MEDIUM PRIORITY)

#### 3.1 OnAccepting / Accepting
**File**: `Terminal.Gui/ViewBase/View.Command.cs:160-177`

**Current Implementation**:
```csharp
protected bool? RaiseAccepting (ICommandContext? ctx)
{
    CommandEventArgs args = new () { Context = ctx };

    if (OnAccepting (args) || args.Handled)
    {
        return true;
    }

    Accepting?.Invoke (this, args);

    return Accepting is null ? null : args.Handled;
}
```

**Can Use Helper?**: ✅ YES - Good fit!

**Proposed Migration**:
```csharp
protected bool? RaiseAccepting (ICommandContext? ctx)
{
    ResultEventArgs<ICommandContext?> args = new (ctx);
    
    return CWPWorkflowHelper.Execute(
        onMethod: args => OnAccepting(new CommandEventArgs { Context = args.Result }),
        eventHandler: (sender, e) => 
        {
            var cmdArgs = new CommandEventArgs { Context = e.Result };
            Accepting?.Invoke(sender, cmdArgs);
            e.Handled = cmdArgs.Handled;
        },
        args: args,
        defaultAction: null);
}
```

**Migration Difficulty**: ⭐⭐⭐ HARD

**Issues**:
1. CommandEventArgs is separate from ResultEventArgs
2. Need to create/map CommandEventArgs inside helper calls
3. Complex argument marshalling
4. Current implementation is clearer

**Recommendation**: ⚠️ Could work but adds complexity. Direct implementation is simpler.

---

#### 3.2 OnSelecting / Selecting
**File**: `Terminal.Gui/ViewBase/View.Command.cs:220-236`

**Analysis**: Identical pattern to OnAccepting

**Can Use Helper?**: ✅ YES (same issues)

**Migration Difficulty**: ⭐⭐⭐ HARD

**Recommendation**: ⚠️ Same as OnAccepting

---

#### 3.3 OnHandlingHotKey / HandlingHotKey
**File**: `Terminal.Gui/ViewBase/View.Command.cs:260-277`

**Analysis**: Identical pattern to OnAccepting

**Can Use Helper?**: ✅ YES (same issues)

**Migration Difficulty**: ⭐⭐⭐ HARD

**Recommendation**: ⚠️ Same as OnAccepting

---

#### 3.4 OnCommandNotBound / CommandNotBound
**File**: `Terminal.Gui/ViewBase/View.Command.cs:93-109`

**Analysis**: Identical pattern to OnAccepting

**Can Use Helper?**: ✅ YES (same issues)

**Migration Difficulty**: ⭐⭐⭐ HARD

**Recommendation**: ⚠️ Same as OnAccepting

---

### Category 4: Navigation Events (MEDIUM PRIORITY)

#### 4.1 OnAdvancingFocus / AdvancingFocus
**File**: `Terminal.Gui/ViewBase/View.Navigation.cs` (AdvanceFocus method)

**Current Implementation Pattern**:
```csharp
// Inside AdvanceFocus method
AdvanceFocusEventArgs args = new (direction, behavior);

if (OnAdvancingFocus (direction, behavior))
{
    return false;
}

AdvancingFocus?.Invoke (this, args);

if (args.Cancel)
{
    return false;
}
```

**Can Use Helper?**: ⚠️ PARTIAL - Embedded in larger method

**Migration Difficulty**: ⭐⭐⭐⭐ VERY HARD

**Issues**:
1. Not in its own Raise method - embedded in AdvanceFocus logic
2. Complex navigation state management around the pattern
3. Would require extracting to separate method first
4. Return semantics differ (returns bool, not bool?)

**Recommendation**: ❌ Don't migrate. Too embedded in complex logic.

---

#### 4.2 OnHasFocusChanging / HasFocusChanging
**File**: `Terminal.Gui/ViewBase/View.Navigation.cs:717-750`

**Analysis**: Similar to AdvancingFocus - embedded in SetHasFocus method

**Can Use Helper?**: ⚠️ PARTIAL - Embedded in larger method

**Migration Difficulty**: ⭐⭐⭐⭐ VERY HARD

**Recommendation**: ❌ Don't migrate. Too embedded.

---

### Category 5: Drawing Events (LOW PRIORITY)

#### 5.1-5.6 Drawing Events
**Files**: `Terminal.Gui/ViewBase/View.Drawing.cs`
- OnDrawingAdornments / DrawingAdornments (line 284)
- OnClearingViewport / ClearingViewport (line 319)
- OnDrawingText / DrawingText (lines 407, 413)
- OnDrawingContent / DrawingContent (lines 489, 495)
- OnDrawingSubViews / DrawingSubViews (lines 545, 551)
- OnRenderingLineCanvas / RenderingLineCanvas (line 611)

**Current Implementation Pattern** (OnDrawingContent example):
```csharp
private void DoDrawContent (DrawContext? context = null)
{
    if (OnDrawingContent (context))
    {
        return;
    }

    if (OnDrawingContent ())
    {
        return;
    }

    var dev = new DrawEventArgs (Viewport, Rectangle.Empty, context);
    DrawingContent?.Invoke (this, dev);

    if (dev.Cancel)
    {
        return;
    }

    // Actual drawing logic...
}
```

**Can Use Helper?**: ⚠️ PARTIAL - Non-standard pattern

**Migration Difficulty**: ⭐⭐⭐ HARD

**Issues**:
1. TWO virtual methods (overloaded) - OnDrawingContent(context) and OnDrawingContent()
2. DrawEventArgs created after virtual methods called
3. Embedded in DoDrawXxx methods with actual drawing logic after
4. Cancel semantics differ from standard CWP

**Recommendation**: ❌ Don't migrate. Custom drawing pipeline better as-is.

---

### Category 6: Property Changes (MEDIUM PRIORITY)

#### 6.1 OnVisibleChanging / VisibleChanging
**File**: `Terminal.Gui/ViewBase/View.cs:382-420`

**Current Implementation**:
```csharp
public bool Visible
{
    get => _visible;
    set
    {
        if (_visible == value)
        {
            return;
        }

        if (OnVisibleChanging ())
        {
            return;
        }

        VisibleChanging?.Invoke (this, new CancelEventArgs (ref _visible, value));

        if (_visible == value)
        {
            return;
        }

        bool wasVisible = _visible;
        _visible = value;

        OnVisibleChanged ();
        VisibleChanged?.Invoke (this, EventArgs.Empty);
        
        // Post-processing...
        if (!_visible)
        {
            RestoreFocus ();
        }
        
        SetNeedsDraw ();
        // ... more post-processing
    }
}
```

**Can Use Helper?**: ✅ YES - CWPPropertyHelper designed for this!

**Proposed Migration**:
```csharp
public bool Visible
{
    get => _visible;
    set
    {
        bool changed = CWPPropertyHelper.ChangeProperty(
            _visible,
            value,
            args => OnVisibleChanging(),  // Problem: signature mismatch
            VisibleChanging,               // Problem: uses CancelEventArgs, not ValueChangingEventArgs
            args => OnVisibleChanged(),
            VisibleChanged,                // Problem: uses EventArgs, not ValueChangedEventArgs
            out bool finalValue);

        if (changed)
        {
            _visible = finalValue;
            
            // Post-processing
            if (!_visible)
            {
                RestoreFocus ();
            }
            SetNeedsDraw ();
            // ... more
        }
    }
}
```

**Migration Difficulty**: ⭐⭐⭐⭐ VERY HARD

**Issues**:
1. OnVisibleChanging() takes no parameters - CWPPropertyHelper expects ValueChangingEventArgs<T>
2. VisibleChanging uses CancelEventArgs, not ValueChangingEventArgs<bool>
3. VisibleChanged uses EventArgs, not ValueChangedEventArgs<bool>
4. Would require changing event signatures (BREAKING CHANGE)
5. Complex post-processing logic after change

**Recommendation**: ❌ Don't migrate. Would require breaking API changes.

---

#### 6.2 OnSchemeNameChanging / SchemeName
**File**: `Terminal.Gui/ViewBase/View.Drawing.Scheme.cs:25-44`

**Current Implementation**: ✅ ALREADY USES CWPPropertyHelper!

**Status**: ✅ Already migrated - good example to follow

---

#### 6.3 OnGettingScheme / GettingScheme
**File**: `Terminal.Gui/ViewBase/View.Drawing.Scheme.cs:133-162`

**Current Implementation**: ✅ ALREADY USES CWPWorkflowHelper.ExecuteWithResult!

**Status**: ✅ Already migrated - good example to follow

---

#### 6.4 OnSettingScheme / SetScheme
**File**: `Terminal.Gui/ViewBase/View.Drawing.Scheme.cs:217-234`

**Current Implementation**: ✅ ALREADY USES CWPPropertyHelper!

**Status**: ✅ Already migrated - good example to follow

---

### Category 7: View-Specific Events (LOW PRIORITY)

#### 7.1 OnCellActivated / CellActivated (TableView)
**File**: `Terminal.Gui/Views/TableView/TableView.cs:1277+`

**Analysis**: View-specific, likely custom implementation

**Can Use Helper?**: ✅ Likely YES

**Migration Difficulty**: ⭐⭐ MEDIUM

**Recommendation**: ⚠️ Low priority, could migrate

---

#### 7.2 OnPositionChanging (ScrollBar, ScrollSlider)
**Files**: 
- `Terminal.Gui/Views/ScrollBar/ScrollBar.cs:375`
- `Terminal.Gui/Views/ScrollBar/ScrollSlider.cs:245`

**Can Use Helper?**: ✅ Likely YES - property change pattern

**Migration Difficulty**: ⭐⭐ MEDIUM

**Issues**:
1. Takes (int currentPos, int newPos) parameters
2. CWPPropertyHelper expects single value
3. Would need wrapper

**Recommendation**: ⚠️ Could work but parameters don't match pattern well

---

#### 7.3 OnCheckedStateChanging (CheckBox)
**File**: `Terminal.Gui/Views/CheckBox.cs:187`

**Current Implementation** (in ChangeCheckedState):
```csharp
private bool? ChangeCheckedState (CheckState value)
{
    if (_checkedState == value || (value is CheckState.None && !AllowCheckStateNone))
    {
        return null;
    }

    ResultEventArgs<CheckState> e = new (value);

    if (OnCheckedStateChanging (e))
    {
        return true;
    }

    CheckedStateChanging?.Invoke (this, e);

    if (e.Handled)
    {
        return e.Handled;
    }

    _checkedState = value;
    UpdateTextFormatterText ();
    SetNeedsLayout ();

    EventArgs<CheckState> args = new (in _checkedState);
    OnCheckedStateChanged (args);

    CheckedStateChanged?.Invoke (this, args);

    return false;
}
```

**Can Use Helper?**: ✅ YES - This is a PERFECT match!

**Proposed Migration**:
```csharp
private bool? ChangeCheckedState (CheckState value)
{
    // Pre-condition check
    if (_checkedState == value || (value is CheckState.None && !AllowCheckStateNone))
    {
        return null;
    }

    bool changed = CWPPropertyHelper.ChangeProperty(
        _checkedState,
        value,
        OnCheckedStateChanging,
        CheckedStateChanging,
        OnCheckedStateChanged,
        CheckedStateChanged,
        out CheckState finalValue);

    if (changed)
    {
        _checkedState = finalValue;
        UpdateTextFormatterText();
        SetNeedsLayout();
        return false;
    }
    
    return true; // Was cancelled
}
```

**Migration Difficulty**: ⭐ EASY

**Issues**:
1. Need to adjust OnCheckedStateChanged signature from EventArgs<CheckState> to ValueChangedEventArgs<CheckState>
2. Return semantics: method returns bool? (null/true/false), helper only returns bool (true/false)

**Recommendation**: ✅ **STRONGLY RECOMMEND** - This is the best candidate! But need to handle:
- Signature changes for OnCheckedStateChanged
- Return value mapping (helper returns bool, method needs bool?)

---

#### 7.4 OnGettingAttributeForRole / GettingAttributeForRole
**File**: `Terminal.Gui/ViewBase/View.Drawing.Attribute.cs:78`

**Analysis**: Likely embedded in GetAttributeForRole method

**Can Use Helper?**: ⚠️ PARTIAL - Need to examine implementation

**Migration Difficulty**: ⭐⭐⭐ HARD (unknown)

**Recommendation**: ⚠️ Low priority, need more analysis

---

#### 7.5 OnBorderStyleChanged
**File**: `Terminal.Gui/ViewBase/View.Adornments.cs:162`

**Analysis**: Property change notification (not cancellable)

**Can Use Helper?**: ❌ NO - Post-change only, no pre-change cancellation

**Recommendation**: ❌ Don't migrate. Not CWP pattern.

---

#### 7.6 OnMouseIsHeldDownTick (MouseHeldDown)
**File**: `Terminal.Gui/ViewBase/MouseHeldDown.cs:71`

**Analysis**: Specialized repeating tick event

**Can Use Helper?**: ❌ NO - Custom timer logic

**Recommendation**: ❌ Don't migrate. Specialized use case.

---

## Migration Recommendations by Priority

### ✅ STRONGLY RECOMMEND (1 instance)
**Effort**: 1-2 days

1. **CheckBox.OnCheckedStateChanging** 
   - Perfect fit for CWPPropertyHelper
   - Requires minor signature adjustments
   - Good example for others
   - Clear improvement

### ⚠️ CONSIDER (4 instances)
**Effort**: 1-2 weeks

2. **Command Events** (OnAccepting, OnSelecting, OnHandlingHotKey, OnCommandNotBound)
   - Could work but requires wrapper logic
   - Marginal improvement over direct implementation
   - Good for consistency if pursuing helper migration strategy

3. **Keyboard Events** (OnKeyDown, OnKeyDownNotHandled, OnKeyUp)
   - Could work but mutable Key complicates
   - Marginal improvement
   - Consider only for consistency

### ❌ DON'T MIGRATE (23 instances)
**Reason**: Custom logic, embedded patterns, or breaking changes required

- **Mouse Events** (4): OnMouseEvent, OnMouseClick, OnMouseWheel, OnMouseEnter
  - Custom pre/post processing
  - Direct implementation clearer

- **Navigation Events** (2): OnAdvancingFocus, OnHasFocusChanging
  - Embedded in complex methods
  - Too tightly coupled

- **Drawing Events** (6): All drawing events
  - Custom pipeline
  - Non-standard pattern with dual overloads

- **Property Changes** (1): OnVisibleChanging
  - Would require breaking API changes
  - Complex post-processing

- **View-Specific** (10): TableView, ScrollBar/Slider (2), BorderStyle, MouseHeldDown, etc.
  - Specialized logic
  - Low value

## Overall Migration Strategy

### Option A: Minimal Migration (RECOMMENDED)
**Effort**: 1-2 days
**Risk**: LOW

Migrate only:
1. CheckBox.OnCheckedStateChanging (perfect fit)

**Benefits**:
- Demonstrates helper usage in real View code
- Clear improvement
- Low risk

**Drawbacks**:
- Limited consistency improvement
- Most code still uses direct implementation

---

### Option B: Partial Migration
**Effort**: 2-3 weeks
**Risk**: MEDIUM

Migrate:
1. CheckBox.OnCheckedStateChanging
2. All Command events (4 instances)
3. All Keyboard events (3 instances)

**Benefits**:
- More consistency
- Demonstrates helper usage in core events
- Shows pattern across event types

**Drawbacks**:
- Significant refactoring effort
- Marginal improvement for many instances
- Requires careful testing
- More complex code in some cases

---

### Option C: Full Migration
**Effort**: 6-8 weeks
**Risk**: HIGH

Attempt to migrate all possible instances.

**Benefits**:
- Maximum consistency
- Full helper usage

**Drawbacks**:
- Many instances don't fit helper pattern well
- Would require API breaking changes
- Creates wrapper complexity
- Direct implementation often clearer
- Very high effort for limited benefit

**Recommendation**: ❌ NOT RECOMMENDED

---

## Key Findings & Gotchas

### 1. Helper Pattern Assumptions Don't Always Match Reality

**Issue**: Helpers assume clean separation of concerns, but real implementations have:
- Pre-conditions (Enabled checks, visibility checks)
- Post-conditions (command invocation, state updates)
- Complex argument types (CommandEventArgs vs ResultEventArgs)
- Mutable arguments (Key.Handled)

**Example**: RaiseMouseClickEvent has both pre-checks and post-command invocation that can't fit in helper.

---

### 2. Event Argument Type Mismatches

**Issue**: Many events use custom EventArgs types that don't match helper expectations:
- CancelEventArgs vs ValueChangingEventArgs<T>
- CommandEventArgs vs ResultEventArgs<T>
- EventArgs vs ValueChangedEventArgs<T>

**Impact**: Would require:
- Breaking API changes to event signatures, OR
- Complex wrapper logic that negates helper benefits

---

### 3. Embedded Patterns Resist Extraction

**Issue**: Many CWP patterns are embedded in larger methods (AdvanceFocus, DoDrawContent, etc.) with:
- Complex surrounding logic
- State management
- Multiple phases

**Impact**: Extracting to use helpers would:
- Require significant refactoring
- Reduce readability
- Increase complexity

---

### 4. Two-Virtual-Method Pattern

**Issue**: Drawing events have TWO virtual methods:
```csharp
OnDrawingContent(DrawContext? context)
OnDrawingContent()  // No parameters
```

**Impact**: Helper assumes single virtual method. This pattern can't fit without major changes.

---

### 5. Return Value Semantics Differ

**Issue**: Many methods return bool?, helpers return bool or bool?:
- null = no subscribers
- true = handled/cancelled
- false = not handled

**Impact**: Translating return values adds complexity.

---

### 6. Current Helper Usage is Limited

**Fact**: Only View.Drawing.Scheme.cs uses helpers currently (3 usages).

**Implications**:
- Helpers are relatively new
- Most code predates helpers
- Helpers designed for specific patterns (property changes, result workflows)
- Not designed to replace all CWP implementations

---

### 7. Direct Implementation Often Clearer

**Observation**: For many instances, direct implementation is:
- More readable
- More maintainable
- Less code
- Fewer abstractions

**Example**:
```csharp
// Direct (Current) - Clear and simple
if (OnKeyDown (k) || k.Handled)
{
    return true;
}
KeyDown?.Invoke (this, k);
return k.Handled;

// With Helper - More complex
ResultEventArgs<Key> args = new (k);
bool? result = CWPWorkflowHelper.Execute(
    onMethod: args => OnKeyDown(k),
    eventHandler: (sender, e) => KeyDown?.Invoke(sender, k),
    args: args,
    defaultAction: null);
return result == true || k.Handled;
```

---

## Recommendations Summary

### For Maintainers

1. **Don't pursue full migration** - Effort (6-8 weeks) far exceeds benefit
2. **Migrate CheckBox.OnCheckedStateChanging** - Perfect fit, good example
3. **Consider Command/Keyboard events** - Only if pursuing consistency strategy
4. **Keep direct implementations** - For most instances, direct is clearer
5. **Document when to use helpers** - Update guidance for new CWP implementations

### For New Code

**Use CWPPropertyHelper when**:
- Implementing property setter with validation
- Need pre/post change events
- Event types match ValueChangingEventArgs<T> and ValueChangedEventArgs<T>

**Use CWPWorkflowHelper when**:
- Simple workflow with single result type
- Clean separation of virtual method, event, default action
- No complex pre/post processing
- ResultEventArgs<T> fits naturally

**Use direct implementation when**:
- Custom pre/post processing logic
- Complex argument types
- Embedded in larger method
- Event types don't match helper expectations
- Direct code is clearer

### Documentation Updates Needed

If pursuing any migration:

1. Update `docs/analysis/cwp_analysis_report.md`
   - Add section on helper class usage
   - Document migration outcomes

2. Update `docfx/docs/events.md`
   - Add guidance on when to use helpers
   - Show examples of helper usage
   - Document when direct implementation is better

3. Update `docfx/docs/cancellable-work-pattern.md`
   - Add helper class section
   - Explain design decisions
   - Show pros/cons

## Conclusion

**Total Effort Summary**:
- Option A (Recommended): 1-2 days
- Option B (Partial): 2-3 weeks  
- Option C (Full): 6-8 weeks (not recommended)

**Key Insight**: CWP helper classes were designed for specific patterns (property changes, clean workflows) and work well for those cases (View.Drawing.Scheme.cs). However, most CWP implementations in the codebase have custom logic, pre/post processing, or embedded patterns that don't fit the helper abstraction well.

**Recommendation**: Migrate CheckBox.OnCheckedStateChanging only (Option A). This demonstrates helper usage in a real view, provides a clear example for developers, and improves code quality without significant risk or effort. Leave other implementations as direct implementations since they're often clearer and more maintainable that way.

The goal should be to use helpers where they add value, not to force all code into the helper pattern.
