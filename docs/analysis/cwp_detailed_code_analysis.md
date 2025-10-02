# CWP Detailed Code Analysis

## Purpose
This document provides detailed code examples and analysis of how the Cancellable Work Pattern (CWP) is currently implemented and what would change if the order were reversed.

## Current Pattern Implementation

### Pattern Structure
```csharp
// Current: Virtual method FIRST, Event SECOND
protected bool RaiseXxxEvent(EventArgs args)
{
    // 1. Call virtual method first - gives derived classes priority
    if (OnXxx(args) || args.Handled)
    {
        return true; // Cancel if handled
    }
    
    // 2. Invoke event second - external code gets second chance
    Xxx?.Invoke(this, args);
    
    return args.Handled;
}

protected virtual bool OnXxx(EventArgs args) 
{ 
    return false; // Override in derived class
}

public event EventHandler<EventArgs>? Xxx;
```

## Case Study: Slider Mouse Event Issue (#3714)

### Current Implementation Problem

**File**: `Terminal.Gui/Views/Slider/Slider.cs:1290`

```csharp
protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
{
    if (!(mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked)
          || mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)
          || mouseEvent.Flags.HasFlag (MouseFlags.ReportMousePosition)
          || mouseEvent.Flags.HasFlag (MouseFlags.Button1Released)))
    {
        return false;
    }

    SetFocus ();
    
    if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
    {
        // ... handle mouse press
        return true; // PREVENTS external code from seeing this event
    }
    // ... more handling
}
```

**Problem**: External code CANNOT prevent Slider from handling mouse events because:
1. `View.RaiseMouseEvent` calls `OnMouseEvent` FIRST
2. Slider overrides `OnMouseEvent` and returns `true`
3. Event never reaches external subscribers

### What User Wants But Cannot Do

```csharp
// User wants to temporarily disable slider interaction
var slider = new Slider();

// THIS DOESN'T WORK - event fired AFTER OnMouseEvent returns true
slider.MouseEvent += (sender, args) =>
{
    if (someCondition)
    {
        args.Handled = true; // TOO LATE - already handled by OnMouseEvent
    }
};
```

### Proposed Solution with Reversed Order

```csharp
// Proposed: Event FIRST, Virtual method SECOND
protected bool RaiseMouseEvent(MouseEventArgs args)
{
    // 1. Invoke event first - external code gets priority
    MouseEvent?.Invoke(this, args);
    
    if (args.Handled)
    {
        return true; // Cancel if handled
    }
    
    // 2. Call virtual method second - derived classes get second chance
    if (OnMouseEvent(args) || args.Handled)
    {
        return true;
    }
    
    return args.Handled;
}
```

**With Reversed Order**:
```csharp
// NOW THIS WORKS
slider.MouseEvent += (sender, args) =>
{
    if (someCondition)
    {
        args.Handled = true; // External code PREVENTS Slider.OnMouseEvent
    }
};
```

## Comprehensive CWP Implementations

### 1. View.Mouse.cs - Mouse Events

#### RaiseMouseEvent
**File**: `Terminal.Gui/ViewBase/View.Mouse.cs:315`
```csharp
public bool RaiseMouseEvent (MouseEventArgs mouseEvent)
{
    // TODO: probably this should be moved elsewhere, please advise
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

    // CURRENT ORDER
    if (OnMouseEvent (mouseEvent) || mouseEvent.Handled)
    {
        return true;
    }

    MouseEvent?.Invoke (this, mouseEvent);

    return mouseEvent.Handled;
}
```

**Overrides Found** (30+):
- ListView.OnMouseEvent
- TextView.OnMouseEvent
- TextField.OnMouseEvent
- TableView.OnMouseEvent
- TreeView.OnMouseEvent
- ScrollBar.OnMouseEvent
- ScrollSlider.OnMouseEvent
- Slider.OnMouseEvent (the problematic one)
- MenuBar.OnMouseEvent
- TabRow.OnMouseEvent
- ColorBar.OnMouseEvent
- And many more...

**Impact of Reversal**: **CRITICAL**
- All 30+ overrides would need review
- Many depend on getting first access to mouse events
- Breaking change for custom views

#### RaiseMouseClickEvent
**File**: `Terminal.Gui/ViewBase/View.Mouse.cs:470`
```csharp
protected bool RaiseMouseClickEvent (MouseEventArgs args)
{
    // Pre-conditions
    if (!Enabled)
    {
        return args.Handled = false;
    }

    // CURRENT ORDER
    if (OnMouseClick (args) || args.Handled)
    {
        return args.Handled;
    }

    MouseClick?.Invoke (this, args);

    if (args.Handled)
    {
        return true;
    }

    // Post-conditions - Invoke commands
    args.Handled = InvokeCommandsBoundToMouse (args) == true;

    return args.Handled;
}
```

**Overrides Found**:
- ScrollBar.OnMouseClick

**Impact of Reversal**: **HIGH**
- Click handling is critical for UI interaction
- Commands invoked after event - order matters
- Less overrides than OnMouseEvent but still significant

#### RaiseMouseWheelEvent
**File**: `Terminal.Gui/ViewBase/View.Mouse.cs:574`
```csharp
protected bool RaiseMouseWheelEvent (MouseEventArgs args)
{
    if (!Enabled)
    {
        return args.Handled = false;
    }

    // CURRENT ORDER
    if (OnMouseWheel (args) || args.Handled)
    {
        return args.Handled;
    }

    MouseWheel?.Invoke (this, args);

    if (args.Handled)
    {
        return true;
    }

    args.Handled = InvokeCommandsBoundToMouse (args) == true;

    return args.Handled;
}
```

**Overrides Found**: Fewer, mostly in scrollable views

**Impact of Reversal**: **MEDIUM-HIGH**
- Scroll behavior customization important
- Fewer overrides = lower risk
- Still breaking change

#### NewMouseEnterEvent
**File**: `Terminal.Gui/ViewBase/View.Mouse.cs:69`
```csharp
internal bool? NewMouseEnterEvent (CancelEventArgs eventArgs)
{
    // Pre-conditions
    if (!CanBeVisible (this))
    {
        return null;
    }

    // CURRENT ORDER
    if (OnMouseEnter (eventArgs))
    {
        return true;
    }

    MouseEnter?.Invoke (this, eventArgs);

    if (eventArgs.Cancel)
    {
        return true;
    }

    MouseState |= MouseState.In;

    if (HighlightStates != MouseState.None)
    {
        SetNeedsDraw ();
    }

    return false;
}
```

**Impact of Reversal**: **MEDIUM**
- Less commonly overridden
- State management (MouseState) happens after event
- Lower risk

### 2. View.Keyboard.cs - Keyboard Events

#### NewKeyDownEvent
**File**: `Terminal.Gui/ViewBase/View.Keyboard.cs:334`
```csharp
public bool NewKeyDownEvent (Key k)
{
    // CURRENT ORDER
    if (OnKeyDown (k) || k.Handled)
    {
        return true;
    }

    // fire event
    KeyDown?.Invoke (this, k);

    return k.Handled;
}
```

**Overrides Found** (15+):
- ListView.OnKeyDown
- TextView.OnKeyDown
- TextField.OnKeyDown
- TableView.OnKeyDown
- TreeView.OnKeyDown
- MenuBar.OnKeyDown
- And more...

**Impact of Reversal**: **CRITICAL**
- Core input handling
- Many views depend on first access to keys
- Text editing depends on this order
- Breaking change for all text input views

#### NewKeyUpEvent
**File**: Similar pattern

**Impact of Reversal**: **MEDIUM**
- Less commonly used
- Fewer overrides
- Lower risk than KeyDown

### 3. View.Command.cs - Command Events

#### RaiseAccepting
**Location**: View.Command.cs (similar to RaiseSelecting)

```csharp
protected bool? RaiseAccepting (ICommandContext? ctx)
{
    CommandEventArgs args = new () { Context = ctx };

    // CURRENT ORDER - Note the comment explicitly explaining the pattern!
    // Best practice is to invoke the virtual method first.
    // This allows derived classes to handle the event and potentially cancel it.
    if (OnAccepting (args) || args.Handled)
    {
        return true;
    }

    // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
    Accepting?.Invoke (this, args);

    return Accepting is null ? null : args.Handled;
}
```

**Note**: Code comment explicitly documents current pattern as "best practice"!

**Overrides Found**:
- CheckBox uses for state toggle validation
- RadioGroup uses for selection validation
- Button uses for action triggering
- MenuItemv2 uses for focus prevention

**Impact of Reversal**: **HIGH**
- State management depends on order
- Views may need to see state BEFORE external validation
- Or external validation may need to PREVENT state change
- Could go either way - depends on use case

#### RaiseSelecting
**File**: `Terminal.Gui/ViewBase/View.Command.cs:220`
```csharp
protected bool? RaiseSelecting (ICommandContext? ctx)
{
    CommandEventArgs args = new () { Context = ctx };

    // CURRENT ORDER - with explicit comment about best practice
    // Best practice is to invoke the virtual method first.
    // This allows derived classes to handle the event and potentially cancel it.
    if (OnSelecting (args) || args.Handled)
    {
        return true;
    }

    // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
    Selecting?.Invoke (this, args);

    return Selecting is null ? null : args.Handled;
}
```

**Impact of Reversal**: **MEDIUM-HIGH**
- Similar to Accepting
- Selection state management
- Could benefit from external control

### 4. View.Navigation.cs - Focus Events

#### AdvanceFocus (calls OnAdvancingFocus)
**Pattern**: Similar CWP implementation

**Impact of Reversal**: **HIGH**
- Focus flow is complex
- Many views customize focus behavior
- Breaking focus flow could cause navigation issues

#### OnHasFocusChanging
**Pattern**: Property change with CWP

**Impact of Reversal**: **HIGH**
- Focus state management
- Many views track focus state
- Breaking change for focus-aware views

### 5. View.Drawing.cs - Drawing Events

All drawing events follow CWP pattern:
- OnDrawingAdornments / DrawingAdornments
- OnClearingViewport / ClearingViewport  
- OnDrawingText / DrawingText
- OnDrawingContent / DrawingContent
- OnDrawingSubViews / DrawingSubViews
- OnRenderingLineCanvas / RenderingLineCanvas

**Impact of Reversal**: **LOW-MEDIUM**
- Mostly for debugging/customization
- Less likely to cause breaking changes
- State changes in drawing less critical

### 6. OrientationHelper - Property Change Pattern

**File**: `Terminal.Gui/ViewBase/Orientation/OrientationHelper.cs`

```csharp
public Orientation Orientation
{
    get => _orientation;
    set
    {
        if (_orientation == value)
        {
            return;
        }

        Orientation prev = _orientation;

        // Check if changing is cancelled
        if (_orientationChangingCallback?.Invoke (prev, value) == true)
        {
            return;
        }

        var changingArgs = new CancelEventArgs<Orientation> (ref _orientation, value, nameof (Orientation));
        _orientationChangingEvent?.Invoke (this, changingArgs);

        if (changingArgs.Cancel || _orientation == value)
        {
            return;
        }

        _orientation = value;

        // Notify that change happened
        _orientationChangedCallback?.Invoke (_orientation);
        _orientationChangedEvent?.Invoke (this, new EventArgs<Orientation> (_orientation));
    }
}
```

**Note**: Uses callback pattern instead of virtual methods, but still calls callback BEFORE event!

**Tests That Validate Current Order**:
`Tests/UnitTestsParallelizable/View/Orientation/OrientationTests.cs`:

```csharp
[Fact]
public void OrientationChanging_VirtualMethodCalledBeforeEvent ()
{
    var radioGroup = new CustomView ();
    bool eventCalled = false;

    radioGroup.OrientationChanging += (sender, e) =>
    {
        eventCalled = true;
        // Test EXPLICITLY checks virtual method called BEFORE event
        Assert.True (radioGroup.OnOrientationChangingCalled, 
                     "OnOrientationChanging was not called before the event.");
    };

    radioGroup.Orientation = Orientation.Horizontal;
    Assert.True (eventCalled, "OrientationChanging event was not called.");
}

[Fact]
public void OrientationChanged_VirtualMethodCalledBeforeEvent ()
{
    var radioGroup = new CustomView ();
    bool eventCalled = false;

    radioGroup.OrientationChanged += (sender, e) =>
    {
        eventCalled = true;
        // Test EXPLICITLY checks virtual method called BEFORE event
        Assert.True (radioGroup.OnOrientationChangedCalled, 
                     "OnOrientationChanged was not called before the event.");
    };

    radioGroup.Orientation = Orientation.Horizontal;
    Assert.True (eventCalled, "OrientationChanged event was not called.");
}
```

**Impact**: These tests would FAIL if order reversed - they explicitly test current behavior!

## Helper Classes Analysis

### CWPWorkflowHelper
**File**: `Terminal.Gui/App/CWP/CWPWorkflowHelper.cs:42`

```csharp
public static bool? Execute<T> (
    Func<ResultEventArgs<T>, bool> onMethod,
    EventHandler<ResultEventArgs<T>>? eventHandler,
    ResultEventArgs<T> args,
    Action? defaultAction = null)
{
    ArgumentNullException.ThrowIfNull (onMethod);
    ArgumentNullException.ThrowIfNull (args);

    // CURRENT ORDER: Virtual method first
    bool handled = onMethod (args) || args.Handled;
    if (handled)
    {
        return true;
    }

    // Event second
    eventHandler?.Invoke (null, args);
    if (args.Handled)
    {
        return true;
    }

    // Default action last (if neither handled)
    if (defaultAction is {})
    {
        defaultAction ();
        return true;
    }

    return eventHandler is null ? null : false;
}
```

**Impact of Changing Helper**: **HIGH**
- Central helper used by multiple workflows
- Single change affects all users
- But also single place to change!

### CWPEventHelper
**File**: `Terminal.Gui/App/CWP/CWPEventHelper.cs:42`

```csharp
public static bool Execute<T> (
    EventHandler<ResultEventArgs<T>>? eventHandler,
    ResultEventArgs<T> args)
{
    ArgumentNullException.ThrowIfNull (args);

    if (eventHandler == null)
    {
        return false;
    }

    // Only event - no virtual method in this helper
    eventHandler.Invoke (null, args);
    return args.Handled;
}
```

**Note**: This helper is event-only, no virtual method, so order not applicable.

## Documentation Analysis

### docfx/docs/events.md
**File**: `docfx/docs/events.md`

Current documentation explicitly states:
```markdown
The [Cancellable Work Pattern (CWP)](cancellable-work-pattern.md) is a core 
pattern in Terminal.Gui that provides a consistent way to handle cancellable 
operations. An "event" has two components:

1. **Virtual Method**: `protected virtual OnMethod()` that can be overridden 
   in a subclass so the subclass can participate
2. **Event**: `public event EventHandler<>` that allows external subscribers 
   to participate

The virtual method is called first, letting subclasses have priority. 
Then the event is invoked.
```

**Impact**: Documentation explicitly documents current order. Would need update.

### docfx/docs/cancellable-work-pattern.md
**File**: `docfx/docs/cancellable-work-pattern.md`

Extensive documentation on CWP philosophy and design decisions.

**Impact**: Major documentation update required if order changes.

## Code That DEPENDS on Current Order

### 1. Explicit in Code Comments
**File**: `Terminal.Gui/ViewBase/View.Command.cs:225-226`
```csharp
// Best practice is to invoke the virtual method first.
// This allows derived classes to handle the event and potentially cancel it.
```

This comment appears in multiple places and explicitly documents current design as "best practice"!

### 2. Explicit in Tests
- `OrientationTests.OrientationChanging_VirtualMethodCalledBeforeEvent`
- `OrientationTests.OrientationChanged_VirtualMethodCalledBeforeEvent`

Tests would fail if order reversed.

### 3. Implicit in View Implementations

Many views override virtual methods with the ASSUMPTION they get first access:

**Example - TextView**:
```csharp
protected override bool OnMouseEvent (MouseEventArgs ev)
{
    // TextView ASSUMES it processes mouse first
    // to update cursor position, selection, etc.
    // External code sees results AFTER TextView processes
}
```

**Example - ListView**:
```csharp
protected override bool OnKeyDown (Key key)
{
    // ListView ASSUMES it processes keys first
    // to navigate items, select, etc.
    // External code sees results AFTER ListView processes
}
```

### 4. State Management Dependencies

Views often:
1. Override `OnXxx` to UPDATE STATE first
2. Then let event handlers see UPDATED STATE
3. Event handlers may READ state assuming it's current

Reversing order means:
1. Event handlers see OLD STATE
2. Virtual method updates state AFTER
3. Or: Event must prevent state update entirely

## Summary of Breaking Changes

### If Order Reversed Globally

**Code Changes Required**:
1. Update 33+ virtual method implementations
2. Update all view overrides (100+ locations)
3. Update CWPWorkflowHelper
4. Update CWPPropertyHelper  
5. Update documentation (events.md, cancellable-work-pattern.md)
6. Update code comments explaining "best practice"
7. Fix failing tests (OrientationTests and likely more)

**Affected Views** (partial list):
- Slider (the issue trigger)
- ListView
- TextView, TextField, TextValidateField
- TableView
- TreeView
- MenuBar, Menu
- ScrollBar, ScrollSlider
- TabView, TabRow
- CheckBox, RadioGroup, Button
- HexView
- ColorBar
- DateField, TimeField
- And 20+ more...

**Risk Assessment**: **VERY HIGH**
- Fundamental behavioral change
- Breaks inheritance assumptions
- Requires extensive testing
- May break user code that depends on order

## Conclusion

The current CWP order (virtual method first, event second) is:
1. **Explicitly documented** as "best practice" in code comments
2. **Tested explicitly** by unit tests
3. **Depended upon implicitly** by 100+ view implementations
4. **Philosophically consistent** with inheritance-over-composition

Reversing the order would:
1. **Solve issue #3714** (Slider mouse event prevention)
2. **Enable external control** (composition-over-inheritance)
3. **Break existing code** extensively
4. **Require major refactoring** across codebase
5. **Need comprehensive testing** and documentation updates

The choice represents a fundamental design philosophy trade-off:
- **Current**: Tight coupling via inheritance (derived classes have priority)
- **Proposed**: Loose coupling via composition (external code has priority)

Neither is "wrong" - it's a design decision with trade-offs either way.
