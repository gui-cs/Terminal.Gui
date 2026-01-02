# Cursor and Focus Management - Design Analysis and Improvement Proposal

> [!IMPORTANT]
> This document describes the current cursor and focus implementation in Terminal.Gui v2_develop and proposes improvements to address design issues.

## Purpose

This document serves as:
1. **Current State Documentation** - Describes how cursor positioning and focus management currently work
2. **Problem Analysis** - Identifies issues with the current design
3. **Improvement Proposal** - Proposes specific changes to improve the design

See end for list of issues this design addresses.

## Tenets for Cursor Support (Unless you know better ones...)

1. **More GUI than Command Line**. The concept of a cursor on the command line of a terminal is intrinsically tied to enabling the user to know where keyboard input is going to impact text editing. TUI apps have many more modalities than text editing where the keyboard is used (e.g. scrolling through a `ColorPicker`). Terminal.Gui's cursor system is biased towards the broader TUI experiences.

2. **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and the cursor should behave in a way consistent with the terminal.

3. **Separation of Concerns** - The "Draw Cursor" (where the next character will be drawn) and the "Terminal Cursor" (the visible cursor indicator) are completely separate concepts and must not be conflated.

## Lexicon & Taxonomy

- **Navigation** - Refers to the user-experience for moving Focus between views in the application view-hierarchy. See [Navigation](navigation.md) for a deep-dive.

- **Focus** - Indicates which View in the view-hierarchy is currently the one receiving keyboard input. Only one view-hierarchy in an application can have focus, and there is only one View in a focused hierarchy that is the most-focused; the one receiving keyboard input. See [Navigation](navigation.md) for a deep-dive.

- **Terminal Cursor** - A visual indicator to the user where keyboard input will have an impact. There is one Terminal Cursor per terminal session. This is what the user sees blinking on screen.

- **Cursor Position** - The screen-relative coordinates where the Terminal Cursor is displayed. Expressed as (col, row).

- **Cursor Style** - How the Terminal Cursor renders. The `CursorVisibility` enum defines supported styles: `Default`, `Invisible`, `Underline`, `UnderlineFix`, `Vertical`, `VerticalFix`, `Box`, `BoxFix`.

- **Draw Cursor** - The internal position tracked by `IOutputBuffer.Col` and `IOutputBuffer.Row` that indicates where the next `AddRune()` or `AddStr()` call will write. This is NOT the same as the Terminal Cursor and should NEVER be used for cursor positioning. The Draw Cursor is purely an implementation detail of the rendering system.

- **Caret** - Synonym for Terminal Cursor, particularly in text entry contexts.

- **Selection** - A visual indicator that something is selected. The Selection and Terminal Cursor are related but distinct:
  - In a `ListView`: Selection (highlighted item) and cursor position are the same, but the Terminal Cursor is invisible
  - In a `TextView`: The Terminal Cursor is at the start or end of the Selection
  - In a `TableView`: Multiple items can be selected while the Terminal Cursor indicates the current cell

- **Most Focused View** - The deepest View in the focus chain that has `HasFocus == true`. This is the view that should position the Terminal Cursor. Only one view in the entire application can be "most focused" at any given time.

- **Focus Chain** - The path from the top-level runnable down to the most focused view. All views in this chain have `HasFocus == true`.

## Current Implementation (v2_develop as of 2026-01-02)

This section documents how cursor and focus management currently works in Terminal.Gui v2.

### View-Level Cursor API

Views that want to display a cursor implement:

```csharp
/// <summary>
/// Gets or sets the cursor style to be used when the view is focused.
/// Default is CursorVisibility.Invisible.
/// </summary>
public CursorVisibility CursorVisibility { get; set; } = CursorVisibility.Invisible;

/// <summary>
/// Positions the cursor based on the currently focused view.
/// </summary>
/// <returns>
/// Viewport-relative cursor position, or null to hide the cursor.
/// </returns>
public virtual Point? PositionCursor();
```

**How it works:**
- Views override `PositionCursor()` to return their desired cursor position (viewport-relative)
- Returning `null` hides the cursor
- The default implementation returns `null` (cursor hidden)
- Views that want a visible cursor set `CursorVisibility` to a value other than `Invisible`

**Example (TextField):**
```csharp
public class TextField : View
{
    public TextField()
    {
        CursorVisibility = CursorVisibility.Default; // Show cursor
    }

    public override Point? PositionCursor()
    {
        // Calculate viewport-relative cursor position based on text editing state
        return new Point(visualColumn, 0);
    }
}
```

### Application-Level Cursor Management

The `ApplicationMainLoop.SetCursor()` method is called once per iteration and manages the Terminal Cursor:

```csharp
private void SetCursor()
{
    // Get the most focused view
    View? mostFocused = App?.TopRunnableView?.MostFocused;

    if (mostFocused == null)
    {
        Output.SetCursorVisibility(CursorVisibility.Invisible);
        return;
    }

    // Ask the view where it wants the cursor
    Point? to = mostFocused.PositionCursor();

    if (to.HasValue)
    {
        // Convert viewport-relative to screen-absolute coordinates
        Point screenPos = mostFocused.ViewportToScreen(to.Value);
        
        // Position and show the cursor
        Output.SetCursorPosition(screenPos.X, screenPos.Y);
        Output.SetCursorVisibility(mostFocused.CursorVisibility);
    }
    else
    {
        // View returned null - hide cursor
        Output.SetCursorVisibility(CursorVisibility.Invisible);
    }
}
```

**Flow:**
1. Get the most focused view via `TopRunnableView.MostFocused`
2. Call `PositionCursor()` on that view
3. If the view returns a position:
   - Convert viewport-relative to screen coordinates
   - Position the cursor at those coordinates
   - Set cursor visibility from `view.CursorVisibility`
4. If the view returns `null`, hide the cursor

### Focus Management

Focus management determines which view is the "most focused" and should control the cursor.

#### View.Focused Property

```csharp
/// <summary>
/// Gets the currently focused SubView or Adornment, or null if nothing is focused.
/// </summary>
public View? Focused
{
    get
    {
        // Check SubViews first
        View? focused = GetSubViews(includePadding: true)
            .FirstOrDefault(v => v.HasFocus);
        
        if (focused is { })
            return focused;
        
        // Check Adornments
        if (Margin is { HasFocus: true }) return Margin;
        if (Border is { HasFocus: true }) return Border;
        if (Padding is { HasFocus: true }) return Padding;
        
        return null;
    }
}
```

**What it does:** Returns the immediate child view (SubView or Adornment) that has focus, or `null` if no child has focus.

**Key insight:** If `view.Focused == null` but `view.HasFocus == true`, then `view` itself is the most focused view.

#### View.MostFocused Property

```csharp
/// <summary>
/// Returns the most focused SubView down the subview-hierarchy.
/// </summary>
public View? MostFocused
{
    get
    {
        // TODO: Remove this API. It's duplicative of Application.Navigation.GetFocused.
        if (Focused is null)
            return null;
        
        View? most = Focused.MostFocused;
        
        if (most is { })
            return most;
        
        return Focused;
    }
}
```

**What it does:** Recursively walks down the focus chain to find the deepest focused view.

**Issues:**
- Returns `null` if the view has no focused children, even if the view itself has focus
- This violates the principle that there's always exactly one "most focused" view when any view has focus
- Causes the bugs described in issue #3444

#### ApplicationNavigation.GetFocused()

```csharp
public class ApplicationNavigation
{
    private View? _focused;
    
    /// <summary>
    /// Gets the most focused View in the application, if there is one.
    /// </summary>
    public View? GetFocused()
    {
        return _focused;
    }
    
    /// <summary>
    /// INTERNAL method to record the most focused View in the application.
    /// </summary>
    internal void SetFocused(View? value)
    {
        if (_focused == value)
            return;
        
        Debug.Assert(value is null or { CanFocus: true, HasFocus: true });
        
        _focused = value;
        FocusedChanged?.Invoke(this, EventArgs.Empty);
    }
}
```

**What it does:** Maintains a single application-wide reference to the most focused view.

**How it gets updated:** View focus change logic calls `App.Navigation.SetFocused()` when the most focused view changes.

### Driver Cursor API

The `IOutput` interface provides low-level cursor control:

```csharp
public interface IOutput
{
    /// <summary>
    /// Moves the console cursor to the given location.
    /// </summary>
    void SetCursorPosition(int col, int row);
    
    /// <summary>
    /// Updates the console cursor visibility and style.
    /// </summary>
    void SetCursorVisibility(CursorVisibility visibility);
    
    /// <summary>
    /// Gets the current position of the console cursor.
    /// </summary>
    Point GetCursorPosition();
}
```

**CursorVisibility enum values:**
- `Default` - Terminal's default cursor style
- `Invisible` - Hide the cursor
- `Underline` / `UnderlineFix` - Underline cursor
- `Vertical` / `VerticalFix` - Vertical bar cursor
- `Box` / `BoxFix` - Block cursor

### Flicker Prevention

The v2 implementation prevents cursor flicker by:

1. **Hiding during rendering** - `OutputBase.Write()` hides the cursor while writing the screen buffer:
   ```csharp
   public virtual void Write(IOutputBuffer buffer)
   {
       // Hide cursor during rendering to prevent flicker
       SetCursorVisibility(CursorVisibility.Invisible);
       
       // ... render screen buffer ...
       
       // DO NOT restore cursor visibility here
       // ApplicationMainLoop.SetCursor() handles that
   }
   ```

2. **Single update per iteration** - `ApplicationMainLoop.SetCursor()` is called once per iteration, after all rendering is complete

3. **No redundant updates** - Cursor is only repositioned when `PositionCursor()` returns a different value

This separation prevents the cursor from flickering as it did in v1, where cursor visibility was saved/restored during each render cycle.

## Current Design Issues

### Issue #1: View.MostFocused Returns Null When View Has No SubViews

**Problem:** When a view has focus but no focused subviews, `view.MostFocused` returns `null` instead of returning the view itself.

**Code:**
```csharp
public View? MostFocused
{
    get
    {
        if (Focused is null)  // If no child has focus
            return null;      // Return null - WRONG!
        
        // ... recursively get MostFocused from child ...
    }
}
```

**Example:**
```csharp
var view = new View { CanFocus = true };
view.SetFocus();
Assert.True(view.HasFocus);        // ✓ Passes
Assert.Null(view.MostFocused);     // ✓ Passes - but this is WRONG!
// view.MostFocused should be view, not null
```

**Expected behavior:** When `view.HasFocus == true` and `view.Focused == null`, then `view` IS the most focused view and `view.MostFocused` should return `view`.

**Impact:**
- Breaks cursor positioning for leaf views (views with no subviews)
- `ApplicationMainLoop.SetCursor()` gets `null` from `MostFocused` and hides the cursor
- Related to issue #3444

### Issue #2: View.Focused Returns Null When View Has No SubViews

**Problem:** Similar to Issue #1, `view.Focused` returns `null` when the view has no subviews, even if the view itself has focus.

**Example:**
```csharp
var view = new View { CanFocus = true };
view.SetFocus();
Assert.True(view.HasFocus);     // ✓ Passes
Assert.Null(view.Focused);      // ✓ Passes - but should this be view?
```

**Expected behavior (debatable):** 
- Option A: `view.Focused` should return `view` when `view.HasFocus == true` and there are no focused subviews
- Option B: `view.Focused` should remain `null` (current behavior) and only `view.MostFocused` should return `view`

The current behavior is inconsistent with the "there's always one most focused view" principle.

### Issue #3: ApplicationMainLoop.SetCursor() Calls MostFocused Every Iteration

**Problem:** Cursor position is recalculated every main loop iteration by calling `PositionCursor()`, even when nothing has changed.

**Code:**
```csharp
private void SetCursor()
{
    View? mostFocused = App?.TopRunnableView?.MostFocused;  // Traverses hierarchy
    Point? to = mostFocused.PositionCursor();               // Calculates position
    // ... update cursor ...
}
```

**Impact:**
- Wasteful for views where cursor positioning is expensive (e.g., complex text layout)
- Called ~50 times per second even when cursor hasn't moved

**Better approach:** Only call `PositionCursor()` when:
- Focus changes
- View calls `SetNeedsDraw()` or equivalent
- View layout changes
- View explicitly signals cursor position changed

### Issue #4: PositionCursor() Specification is Unclear

**Problem:** The API contract for `View.PositionCursor()` is poorly specified:

Questions:
1. Should derived classes call `base.PositionCursor()`?
2. If so, before or after their own logic?
3. What coordinates should be returned - viewport-relative or content-relative?
4. How should views handle being called when not focused?
5. Can views call `Move()` or `AddRune()` in `PositionCursor()`? (Spoiler: No! This would affect the Draw Cursor)

**Current documentation:**
```csharp
/// <summary>
/// Positions the cursor based on the currently focused view in the chain.
/// </summary>
/// <remarks>
/// Views that are focusable should override PositionCursor() to make sure that the cursor is
/// placed in a location that makes sense. Some terminals do not have a way of hiding the cursor,
/// so it can be distracting to have the cursor left at the last focused view.
/// </remarks>
/// <returns>Viewport-relative cursor position. Return null to hide cursor.</returns>
public virtual Point? PositionCursor()
```

The remarks mention "currently focused view in the chain" but don't clearly explain the method is ONLY called on the most focused view.

### Issue #5: Cursor State Tracked in Multiple Places

**Problem:** Cursor-related state is scattered across multiple locations:

1. `View.CursorVisibility` - Per-view cursor style
2. `View.PositionCursor()` - View calculates position on demand
3. `ApplicationNavigation._focused` - Application-level most focused view
4. `ApplicationMainLoop.SetCursor()` - Coordinates cursor updates
5. Driver cursor state (position, visibility)

This makes it hard to reason about when and how the cursor gets updated.

### Issue #6: No Event When Cursor Should Update

**Problem:** Views have no way to signal "my cursor position changed" without triggering a full redraw via `SetNeedsDraw()`.

**Example scenarios:**
- User types in TextField - cursor moves right
- User presses arrow key in TextView - cursor moves
- User scrolls TextView - cursor position unchanged in viewport coords, but might need update

Currently, these views must call `SetNeedsDraw()` which triggers a full redraw, even though only the cursor needs to move.

**Better approach:** Add a lighter-weight signal like `SetCursorNeedsUpdate()` or a `CursorPositionChanged` event.

### Issue #7: "Draw Cursor" vs "Terminal Cursor" Conflation in Documentation

**Problem:** While the code properly separates these concepts (fixed in January 2025), the documentation and naming could be clearer.

**Naming issues:**
- `Move(col, row)` suggests cursor movement but actually sets Draw Cursor position
- `OutputBuffer.Col` and `OutputBuffer.Row` are used during drawing but names don't indicate they're Draw Cursor state
- Easy for developers to confuse `Driver.Col` with cursor position

**Better approach:** More explicit naming and documentation emphasizing the separation. 

## Proposed Improvements

This section proposes specific changes to address the issues identified above.

### Proposal #1: Fix View.MostFocused and View.Focused

**Goal:** Ensure these properties return sensible values when a view has focus but no focused subviews.

**Changes to View.MostFocused:**

```csharp
/// <summary>
/// Returns the most focused SubView down the subview-hierarchy, or this view if
/// it has focus and no subview has focus.
/// </summary>
public View? MostFocused
{
    get
    {
        if (!HasFocus)
            return null;  // This view doesn't have focus
        
        if (Focused is null)
            return this;  // This view has focus, no child has focus, so this is most focused
        
        // A child has focus - recurse to find the deepest focused view
        View? most = Focused.MostFocused;
        
        return most ?? Focused;  // Return the deepest focused view
    }
}
```

**Changes to View.Focused (Option A - Recommended):**

```csharp
/// <summary>
/// Gets the currently focused SubView/Adornment, or this view if it has focus
/// and no child has focus, or null if this view doesn't have focus.
/// </summary>
public View? Focused
{
    get
    {
        if (!HasFocus)
            return null;  // This view doesn't have focus
        
        // Check SubViews
        View? focused = GetSubViews(includePadding: true)
            .FirstOrDefault(v => v.HasFocus);
        
        if (focused is { })
            return focused;
        
        // Check Adornments
        if (Margin is { HasFocus: true }) return Margin;
        if (Border is { HasFocus: true }) return Border;
        if (Padding is { HasFocus: true }) return Padding;
        
        // This view has focus but no child has focus
        return this;
    }
}
```

**Alternative (Option B - Minimal Change):**

Keep `Focused` returning `null` (current behavior) but fix `MostFocused` as shown above. This is less breaking but more confusing.

**Impact:**
- Fixes issue #3444 unit tests
- Makes cursor positioning work for leaf views
- More intuitive API: "most focused" always returns a view when any view has focus
- Potential breaking change if code relies on `MostFocused` returning `null`

### Proposal #2: Add Cursor Position Change Notification

**Goal:** Allow views to signal cursor position changes without triggering full redraw.

**Add to View:**

```csharp
/// <summary>
/// Signals that the cursor position needs to be updated without requiring a full redraw.
/// </summary>
public void SetCursorNeedsUpdate()
{
    // Signal to ApplicationMainLoop that SetCursor() should be called
    // even if no drawing occurs this iteration
    App?.SetCursorNeedsUpdate();
}
```

**Add to IApplication/ApplicationMainLoop:**

```csharp
private bool _cursorNeedsUpdate;

public void SetCursorNeedsUpdate()
{
    _cursorNeedsUpdate = true;
}

// In Iteration():
public void Iteration()
{
    // ... process input ...
    
    // Only layout/draw if needed
    if (needsRedraw)
        App?.LayoutAndDraw(forceRedraw: false);
    
    // Update cursor if drawing occurred OR if explicitly requested
    if (needsRedraw || _cursorNeedsUpdate)
    {
        SetCursor();
        _cursorNeedsUpdate = false;
    }
    
    // ... run timers ...
}
```

**Usage in Views:**

```csharp
public class TextField : View
{
    private int _cursorPosition;
    
    public int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            if (_cursorPosition != value)
            {
                _cursorPosition = value;
                SetCursorNeedsUpdate();  // Update cursor without full redraw
            }
        }
    }
}
```

**Benefits:**
- More efficient than `SetNeedsDraw()` when only cursor moves
- Explicit signal of intent
- Reduces unnecessary redraws

### Proposal #3: Cache Cursor Position

**Goal:** Avoid calling `PositionCursor()` every iteration when cursor hasn't moved.

**Add to ApplicationMainLoop:**

```csharp
private View? _lastCursorView;
private Point? _lastCursorPosition;
private CursorVisibility _lastCursorVisibility;

private void SetCursor()
{
    View? mostFocused = App?.TopRunnableView?.MostFocused;

    // Check if we need to update
    if (mostFocused == _lastCursorView && !_cursorNeedsUpdate)
    {
        // Same view, cursor hasn't moved, visibility hasn't changed
        return;
    }

    if (mostFocused == null)
    {
        if (_lastCursorVisibility != CursorVisibility.Invisible)
        {
            Output.SetCursorVisibility(CursorVisibility.Invisible);
            _lastCursorVisibility = CursorVisibility.Invisible;
        }
        _lastCursorView = null;
        _lastCursorPosition = null;
        return;
    }

    Point? to = mostFocused.PositionCursor();

    // Check if cursor position or visibility changed
    if (to == _lastCursorPosition && 
        mostFocused.CursorVisibility == _lastCursorVisibility &&
        mostFocused == _lastCursorView)
    {
        return;  // No changes
    }

    if (to.HasValue)
    {
        Point screenPos = mostFocused.ViewportToScreen(to.Value);
        Output.SetCursorPosition(screenPos.X, screenPos.Y);
        Output.SetCursorVisibility(mostFocused.CursorVisibility);
        
        _lastCursorPosition = to;
        _lastCursorVisibility = mostFocused.CursorVisibility;
    }
    else
    {
        Output.SetCursorVisibility(CursorVisibility.Invisible);
        _lastCursorPosition = null;
        _lastCursorVisibility = CursorVisibility.Invisible;
    }
    
    _lastCursorView = mostFocused;
}
```

**When to invalidate cache:**
- Focus changes (handled by checking `mostFocused != _lastCursorView`)
- View calls `SetCursorNeedsUpdate()` (handled by `_cursorNeedsUpdate` flag)
- Layout changes (could add `SetCursorNeedsUpdate()` call in layout system)

**Benefits:**
- Reduces calls to `PositionCursor()` by ~99% in steady state
- No performance impact when cursor is moving
- Simple to implement

### Proposal #4: Improve PositionCursor() Documentation

**Goal:** Make the API contract crystal clear.

**Updated documentation:**

```csharp
/// <summary>
/// Calculates where the cursor should be positioned for this view.
/// </summary>
/// <remarks>
/// <para>
/// This method is ONLY called on the most focused view in the application (the deepest view
/// in the focus chain with HasFocus == true).
/// </para>
/// <para>
/// IMPORTANT: Do NOT call Move(), AddRune(), or any drawing methods in this method. Those
/// methods affect the "Draw Cursor" (where characters are rendered), not the Terminal Cursor
/// (the visible cursor indicator). This method should only calculate and return a position.
/// </para>
/// <para>
/// Return viewport-relative coordinates. The framework will convert to screen coordinates.
/// Return null to hide the cursor.
/// </para>
/// <para>
/// Base implementation returns null (cursor hidden). Override to position the cursor at
/// the appropriate location for your view.
/// </para>
/// </remarks>
/// <returns>
/// Viewport-relative cursor position (col, row), or null to hide the cursor.
/// </returns>
/// <example>
/// public override Point? PositionCursor()
/// {
///     // Don't call base - it just returns null
///     
///     if (!CanFocus || !HasFocus)
///         return null;  // Shouldn't happen, but be defensive
///     
///     // Calculate cursor position based on your view's state
///     int visualColumn = GetVisualColumnForCursor();
///     int visualRow = GetVisualRowForCursor();
///     
///     return new Point(visualColumn, visualRow);
/// }
/// </example>
public virtual Point? PositionCursor()
{
    // Base implementation: hide cursor
    return null;
}
```

**Additional guidelines to document:**

1. **Don't call base** - The base implementation just returns `null`, so calling it is pointless
2. **Return coordinates relative to Viewport** - Not ContentArea, not screen coordinates
3. **Pure calculation** - No side effects, no drawing, no state changes
4. **Called frequently** - This may be called every iteration, so keep it fast
5. **Defensive checks optional** - The framework ensures this is only called on focused views, but defensive checks don't hurt

### Proposal #5: Consider Property-Based Cursor Position (Future)

**Goal:** Move from method-based (`PositionCursor()`) to property-based cursor management.

**Potential future API:**

```csharp
/// <summary>
/// Gets or sets the viewport-relative cursor position, or null to hide the cursor.
/// </summary>
public Point? CursorPosition
{
    get => _cursorPosition;
    set
    {
        if (_cursorPosition != value)
        {
            _cursorPosition = value;
            SetCursorNeedsUpdate();
        }
    }
}

/// <summary>
/// Gets or sets the cursor style for this view.
/// </summary>
public CursorVisibility CursorVisibility { get; set; } = CursorVisibility.Invisible;
```

**How it would work:**
- Views update `CursorPosition` property instead of overriding `PositionCursor()`
- Setting the property automatically calls `SetCursorNeedsUpdate()`
- ApplicationMainLoop reads the property instead of calling a method
- More discoverable, more testable, clearer intent

**Example usage:**

```csharp
public class TextField : View
{
    private int _textCursorPosition;
    
    private void UpdateCursorPosition()
    {
        // Calculate visual column
        int visualColumn = GetVisualColumnForCursor(_textCursorPosition);
        
        // Update cursor position property
        CursorPosition = new Point(visualColumn, 0);
    }
    
    public override bool OnKeyDown(Key key)
    {
        if (key == Key.CursorRight)
        {
            _textCursorPosition++;
            UpdateCursorPosition();  // Updates CursorPosition property
            return true;
        }
        return base.OnKeyDown(key);
    }
}
```

**Benefits:**
- Clearer ownership - view owns its cursor position
- Easier to test - can set/get without calling methods
- Automatic update notification
- Can subscribe to property change events

**Drawbacks:**
- Breaking change from current `PositionCursor()` method
- More state to maintain
- Less flexible for complex cursor positioning logic

**Recommendation:** Keep `PositionCursor()` method for now, but consider property-based approach for v3 or as an alternative API.

### Proposal #6: Centralize Cursor Management in Application.Navigation

**Goal:** Move cursor management out of ApplicationMainLoop into ApplicationNavigation where focus is managed.

**Rationale:**
- Cursor positioning is fundamentally tied to focus
- ApplicationNavigation already tracks the most focused view
- Keeps related concerns together

**Changes:**

```csharp
public class ApplicationNavigation
{
    private View? _focused;
    private Point? _lastCursorPosition;
    private CursorVisibility _lastCursorVisibility;
    
    /// <summary>
    /// Updates the terminal cursor based on the currently focused view.
    /// </summary>
    public void UpdateCursor(IOutput output)
    {
        if (_focused == null)
        {
            output.SetCursorVisibility(CursorVisibility.Invisible);
            return;
        }
        
        Point? to = _focused.PositionCursor();
        
        if (to.HasValue)
        {
            Point screenPos = _focused.ViewportToScreen(to.Value);
            output.SetCursorPosition(screenPos.X, screenPos.Y);
            output.SetCursorVisibility(_focused.CursorVisibility);
        }
        else
        {
            output.SetCursorVisibility(CursorVisibility.Invisible);
        }
    }
    
    internal void SetFocused(View? value)
    {
        if (_focused == value)
            return;
        
        _focused = value;
        FocusedChanged?.Invoke(this, EventArgs.Empty);
        
        // Cursor needs update when focus changes
        App?.SetCursorNeedsUpdate();
    }
}
```

**In ApplicationMainLoop:**

```csharp
public void Iteration()
{
    // ... process input, layout, draw ...
    
    // Update cursor
    App?.Navigation.UpdateCursor(Output);
    
    // ... run timers ...
}
```

**Benefits:**
- Clearer separation of concerns
- Cursor management co-located with focus management
- ApplicationMainLoop is simpler

## Requirements for Any Cursor System Design

Based on analysis of the current implementation and issues, any cursor system design MUST meet these requirements:

1. **No Flickering** - The cursor should blink/pulse at the rate dictated by the terminal. Typing, mouse movement, view layout changes, etc. should NOT cause the cursor to flicker.

2. **Hidden by Default** - By default, the cursor should not be visible. Views should not have to do anything special to keep the cursor invisible.

3. **Simple for Views** - Views that want to show the cursor should only have to:
   - Set `CursorVisibility` to a value other than `Invisible`
   - Return the desired viewport-relative position from `PositionCursor()` (or set `CursorPosition` property)
   - Return `null` (or set `CursorPosition = null`) to hide the cursor

4. **Only Visible When Appropriate** - The cursor should only be visible when:
   - `view.Enabled == true`
   - `view.Visible == true`
   - `view.CanFocus == true`
   - `view.HasFocus == true`
   - `view` is the most focused view (deepest in focus chain)

5. **Application-Managed** - The application/framework should be responsible for positioning and showing/hiding the cursor, not individual views.

6. **No Driver Access from Views** - View subclasses should NEVER directly call Driver APIs. Only `Application` and the `View` base class should interact with the driver.

7. **Separation of Draw Cursor and Terminal Cursor** - These are completely separate concepts:
   - **Draw Cursor**: Internal rendering state (`IOutputBuffer.Col/Row`) - where next character will be drawn
   - **Terminal Cursor**: Visible cursor indicator - where user's input will go
   - Drawing methods (`Move()`, `AddRune()`, `AddStr()`) affect ONLY the Draw Cursor
   - Cursor positioning (`PositionCursor()`, `SetCursorPosition()`) affects ONLY the Terminal Cursor

8. **Efficient** - Cursor positioning should not be recalculated unnecessarily. Cache when possible and only update when needed.

9. **Clear API Contract** - The cursor positioning API should be well-documented with clear expectations about when methods are called, what coordinates to return, and what views should/shouldn't do.

## Summary

The current Terminal.Gui v2 cursor and focus implementation has made significant progress:

✅ **What Works Well:**
- Separation of Draw Cursor and Terminal Cursor (fixed January 2025)
- No flicker - cursor is hidden during rendering and shown once per iteration
- Simple view API - override `PositionCursor()` and set `CursorVisibility`
- Application-managed - ApplicationMainLoop handles cursor updates
- No driver access from views

❌ **What Needs Improvement:**
- `View.MostFocused` returns `null` for leaf views with focus (Issue #1)
- `View.Focused` returns `null` for leaf views with focus (Issue #2)
- Cursor position recalculated every iteration even when unchanged (Issue #3)
- `PositionCursor()` API contract unclear (Issue #4)
- No lightweight way to signal cursor moved (Issue #6)

**Recommended Implementation Priority:**

1. **Fix View.MostFocused** (Proposal #1) - HIGH PRIORITY
   - Fixes unit tests in issue #3444
   - Makes cursor work for leaf views
   - Small, focused change with clear benefits

2. **Improve Documentation** (Proposal #4) - HIGH PRIORITY
   - Low cost, high value
   - Helps developers implement views correctly
   - Can be done immediately

3. **Add Cursor Update Notification** (Proposal #2) - MEDIUM PRIORITY
   - Nice optimization for text editing views
   - Relatively simple to implement
   - Improves performance for common case

4. **Cache Cursor Position** (Proposal #3) - MEDIUM PRIORITY
   - Good optimization
   - Low risk
   - Can be done independently

5. **Centralize in Navigation** (Proposal #6) - LOW PRIORITY
   - Better architecture but not urgent
   - Larger refactoring
   - Consider for future work

6. **Property-Based API** (Proposal #5) - FUTURE
   - Breaking change
   - Better for v3 or as alternative API
   - Evaluate based on feedback

## Related Issues

- [#3444](https://github.com/gui-cs/Terminal.Gui/issues/3444) - Re-factor Cursor handling per cursor.md (this issue)
- [#3432](https://github.com/gui-cs/Terminal.Gui/issues/3432) - Related cursor/focus issues
- Cursor flicker issue (FIXED 2025-01-13 in OutputBase.Write)

## See Also

- [Navigation Deep Dive](navigation.md) - Complete navigation and focus management documentation
- [Application Architecture](application.md) - Application lifecycle and instance-based architecture
- [Drivers](drivers.md) - Low-level driver architecture and cursor APIs
- [Keyboard](keyboard.md) - Keyboard event handling
- [View Deep Dive](View.md) - View lifecycle and hierarchy
