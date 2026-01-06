# Cursor Class Design Exploration

**Note**: This is a design exploration document. Do not implement yet. This file will be deleted after review.

## Current State Analysis

### Current Cursor-Related Components

1. **CursorVisibility Enum** (`Terminal.Gui.Drivers.CursorVisibility`)
   - Based on Windows terminal model with hex encoding for platform-specific parameters
   - Values: Default, Invisible, Underline, UnderlineFix, Vertical, VerticalFix, Box, BoxFix
   - Encodes TERMINFO DECSUSR (Linux/Mac), NCurses curs_set, Windows CONSOLE_CURSOR_INFO parameters

2. **IOutput Interface** (`Terminal.Gui.Drivers.IOutput`)
   - `Point GetCursorPosition()`
   - `void SetCursorPosition(int col, int row)`
   - `CursorVisibility GetCursorVisibility()`
   - `void SetCursorVisibility(CursorVisibility visibility)`

3. **View.Cursor.cs** (View-level API)
   - `Point? CursorPosition { get; }` - content-area relative position
   - `CursorVisibility CursorVisibility { get; }`
   - `void SetCursor(Point? position, CursorVisibility visibility)` - public API for views
   - `void SetCursorNeedsUpdate()` - signals cursor changed without redraw

4. **ApplicationNavigation.UpdateCursor()** (Framework-level)
   - Reads View.CursorPosition and View.CursorVisibility
   - Converts content-area coords to screen coords
   - Checks all ancestor viewports for clipping
   - Calls IOutput.SetCursorPosition() and IOutput.SetCursorVisibility()

### Problems with Current Design

1. **Split API**: Position and visibility managed separately at all levels
2. **Null vs Invisible**: `Point? position` (null = hide) overlaps with `CursorVisibility.Invisible`
3. **Platform-specific encoding**: CursorVisibility enum encodes Windows/Unix parameters in hex
4. **No coordinate system tracking**: Position meaning changes by context (content-area vs viewport vs screen)
5. **Manual synchronization**: Callers must keep position and visibility in sync
6. **ANSI-baseline mismatch**: Enum designed for Windows, but we now use ANSI as baseline

## Proposed Unified Cursor Class Design

### ANSI-First CursorShape Enum

Replace `CursorVisibility` with ANSI-aligned enum:

```csharp
namespace Terminal.Gui.Drivers;

/// <summary>
/// Defines the shape of the terminal cursor, based on ANSI/VT terminal standards.
/// </summary>
/// <remarks>
/// <para>
/// This enum follows the ANSI/VT DECSCUSR (CSI Ps SP q) sequence standard where Ps indicates:
/// 0 = implementation defined (usually blinking block)
/// 1 = blinking block
/// 2 = steady block
/// 3 = blinking underline
/// 4 = steady underline
/// 5 = blinking bar (vertical I-beam)
/// 6 = steady bar (vertical I-beam)
/// </para>
/// <para>
/// Drivers map these values to platform-specific APIs:
/// - ANSI terminals: Use DECSCUSR escape sequences directly
/// - Windows Console: Map to CONSOLE_CURSOR_INFO (bVisible and dwSize)
/// - NCurses: Map to curs_set() and platform-specific extensions
/// </para>
/// <para>
/// To hide the cursor, use null for the cursor position. This enum only defines visible cursor shapes.
/// </para>
/// </remarks>
public enum CursorShape
{
    /// <summary>Blinking block cursor (default for most terminals).</summary>
    /// <remarks>ANSI DECSCUSR Ps=1 or Ps=0.</remarks>
    BlinkingBlock = 1,

    /// <summary>Steady (non-blinking) block cursor.</summary>
    /// <remarks>ANSI DECSCUSR Ps=2.</remarks>
    SteadyBlock = 2,

    /// <summary>Blinking underline cursor.</summary>
    /// <remarks>ANSI DECSCUSR Ps=3.</remarks>
    BlinkingUnderline = 3,

    /// <summary>Steady (non-blinking) underline cursor.</summary>
    /// <remarks>ANSI DECSCUSR Ps=4.</remarks>
    SteadyUnderline = 4,

    /// <summary>Blinking vertical bar cursor (I-beam, commonly used in text editors).</summary>
    /// <remarks>ANSI DECSCUSR Ps=5.</remarks>
    BlinkingBar = 5,

    /// <summary>Steady (non-blinking) vertical bar cursor (I-beam).</summary>
    /// <remarks>ANSI DECSCUSR Ps=6.</remarks>
    SteadyBar = 6
}
```

**Rationale**:
- Directly maps to ANSI DECSCUSR sequence values (Ps parameter)
- No platform-specific hex encoding - drivers handle mapping
- Clear naming: Shape + Blink state explicit
- Visibility controlled by nullable position (null = hidden), not enum value
- `BlinkingBlock` = 1 (ANSI default) is good default value

**Migration from CursorVisibility**:
- `Invisible` → null position (cursor hidden)
- `Default`/`Box` → `BlinkingBlock` 
- `BoxFix` → `SteadyBlock`
- `Underline` → `BlinkingUnderline`
- `UnderlineFix` → `SteadyUnderline`
- `Vertical` → `BlinkingBar`
- `VerticalFix` → `SteadyBar`

### Cursor Class

```csharp
namespace Terminal.Gui;

/// <summary>
/// Represents a cursor with position in screen coordinates and shape.
/// </summary>
/// <remarks>
/// <para>
/// This class consolidates cursor state that was previously split between position (Point?)
/// and visibility (CursorVisibility). The position is always in screen-absolute coordinates.
/// Views are responsible for converting from their content-area or viewport coordinates to
/// screen coordinates before setting the cursor.
/// </para>
/// <para>
/// Immutable value type - use with 'with' expression to modify:
/// <code>
/// Cursor newCursor = currentCursor with { Position = new Point(5, 0) };
/// </code>
/// </para>
/// <para>
/// To hide the cursor, set Position to null. The Shape property defines the visual appearance
/// when the cursor is visible.
/// </para>
/// </remarks>
public record Cursor
{
    /// <summary>
    /// Gets the cursor position in screen-absolute coordinates.
    /// </summary>
    /// <remarks>
    /// Null position indicates the cursor is hidden.
    /// When setting, ensure coordinates are in screen space (not content-area or viewport relative).
    /// Use <c>View.ContentToScreen()</c> or <c>View.ViewportToScreen()</c> to convert if needed.
    /// </remarks>
    public Point? Position { get; init; }

    /// <summary>
    /// Gets the cursor shape.
    /// </summary>
    /// <remarks>
    /// Defines the visual appearance when <see cref="Position"/> is not null.
    /// Default is <see cref="CursorShape.BlinkingBlock"/>.
    /// </remarks>
    public CursorShape Shape { get; init; } = CursorShape.BlinkingBlock;

    /// <summary>
    /// Gets whether the cursor is visible (has valid position).
    /// </summary>
    public bool IsVisible => Position.HasValue;

    /// <summary>
    /// Returns string representation for debugging.
    /// </summary>
    public override string ToString()
    {
        if (!IsVisible)
        {
            return "Cursor { Hidden }";
        }

        return $"Cursor {{ Position = {Position}, Shape = {Shape} }}";
    }
}
```

### Updated View API

```csharp
namespace Terminal.Gui.ViewBase;

public partial class View
{
    private Cursor _cursor = new();

    /// <summary>
    /// Gets the current cursor for this view.
    /// </summary>
    /// <remarks>
    /// Use <see cref="SetCursor"/> to update the cursor position and shape.
    /// The cursor will only be visible when the view has focus and is the most focused view.
    /// Position is always in screen-absolute coordinates.
    /// </remarks>
    public Cursor Cursor => _cursor;

    /// <summary>
    /// Sets the cursor for this view.
    /// </summary>
    /// <param name="cursor">
    /// The cursor to set. Position must be in screen-absolute coordinates.
    /// Use <c>ContentToScreen()</c> or <c>ViewportToScreen()</c> to convert from view-relative coordinates.
    /// Set Position to null to hide the cursor.
    /// </param>
    /// <remarks>
    /// <para>
    /// Common patterns:
    /// <code>
    /// // Text cursor at column 5 in content area - convert to screen coords
    /// Point screenPos = ContentToScreen(new Point(5, 0));
    /// SetCursor(new Cursor { Position = screenPos, Shape = CursorShape.BlinkingBar });
    ///
    /// // Hide cursor
    /// SetCursor(new Cursor { Position = null });
    ///
    /// // Update position keeping same shape
    /// Point newScreenPos = ContentToScreen(new Point(6, 0));
    /// SetCursor(_cursor with { Position = newScreenPos });
    /// </code>
    /// </para>
    /// <para>
    /// This is more efficient than calling <see cref="SetNeedsDraw"/> when only
    /// the cursor needs to move.
    /// </para>
    /// </remarks>
    public void SetCursor(Cursor cursor)
    {
        if (_cursor == cursor)
        {
            return;
        }

        _cursor = cursor;

        if (HasFocus)
        {
            SetCursorNeedsUpdate();
        }
    }

    /// <summary>
    /// Signals that the cursor needs to be updated without requiring a full redraw.
    /// </summary>
    public void SetCursorNeedsUpdate()
    {
        App?.Navigation?.SetCursorNeedsUpdate();
    }
}
```

### Updated ApplicationNavigation API

```csharp
namespace Terminal.Gui.App;

public class ApplicationNavigation
{
    private bool _cursorNeedsUpdate = true;

    /// <summary>
    /// Signals that the cursor needs to be updated.
    /// </summary>
    public void SetCursorNeedsUpdate()
    {
        _cursorNeedsUpdate = true;
    }

    /// <summary>
    /// Updates the terminal cursor based on the currently focused view.
    /// </summary>
    /// <param name="output">The output driver to update.</param>
    public void UpdateCursor(IOutput output)
    {
        if (!_cursorNeedsUpdate)
        {
            return;
        }

        View? mostFocused = App?.TopRunnableView?.MostFocused;

        if (mostFocused is null || !mostFocused.Cursor.IsVisible)
        {
            output.SetCursorShape(null); // Hide cursor
            _cursorNeedsUpdate = false;
            return;
        }

        // Get cursor in screen coordinates (view is responsible for conversion)
        Cursor screenCursor = mostFocused.Cursor;

        if (screenCursor.Position.HasValue)
        {
            // Check if position is within all ancestor viewports
            bool withinViewports = true;
            View? current = mostFocused;

            while (current is not null)
            {
                Rectangle viewportBounds = current.ViewportToScreen(
                    new Rectangle(Point.Empty, current.Viewport.Size));

                if (!viewportBounds.Contains(screenCursor.Position.Value))
                {
                    withinViewports = false;
                    break;
                }

                current = current.SuperView;
            }

            if (withinViewports)
            {
                output.SetCursorPosition(screenCursor.Position.Value.X, screenCursor.Position.Value.Y);
                output.SetCursorShape(screenCursor.Shape);
            }
            else
            {
                output.SetCursorShape(null); // Hide - outside viewport
            }
        }
        else
        {
            output.SetCursorShape(null); // Hide - no position
        }

        _cursorNeedsUpdate = false;
    }
}
```

### Updated IOutput Interface

```csharp
namespace Terminal.Gui.Drivers;

public interface IOutput : IDisposable
{
    // ... existing members ...

    /// <summary>
    /// Gets the current position of the terminal cursor.
    /// </summary>
    Point GetCursorPosition();

    /// <summary>
    /// Moves the terminal cursor to the specified screen coordinates.
    /// </summary>
    void SetCursorPosition(int col, int row);

    /// <summary>
    /// Gets the current cursor shape, or null if cursor is hidden.
    /// </summary>
    CursorShape? GetCursorShape();

    /// <summary>
    /// Sets the cursor shape and visibility.
    /// </summary>
    /// <param name="shape">The cursor shape, or null to hide the cursor.</param>
    void SetCursorShape(CursorShape? shape);
}
```

## Benefits of Proposed Design

### 1. Type Safety
- Single `Cursor` object prevents position/shape desync
- Position always in screen coordinates - no ambiguity
- Immutable record prevents accidental mutation
- Nullable position for visibility is idiomatic C#

### 2. Clearer API
```csharp
// Old API - unclear coordinate system, split state
view.SetCursor(new Point(5, 0), CursorVisibility.Vertical);

// New API - explicit conversion, unified state
Point screenPos = view.ContentToScreen(new Point(5, 0));
view.SetCursor(new Cursor { Position = screenPos, Shape = CursorShape.BlinkingBar });
```

### 3. ANSI-First Design
- `CursorShape` enum values match ANSI DECSCUSR sequence parameters
- Drivers map to platform APIs (Windows, NCurses) internally
- No hex encoding exposure in public API
- Nullable shape at driver level (null = hide) aligns with ANSI DECTCEM

### 4. Simplified Null Handling
```csharp
// Old: null position vs Invisible visibility both mean "hidden"
SetCursor(null, CursorVisibility.Invisible); // redundant
SetCursor(new Point(5, 0), CursorVisibility.Invisible); // conflicting

// New: single concept
SetCursor(new Cursor { Position = null }); // hidden, clear
SetCursor(new Cursor { Position = screenPos, Shape = CursorShape.BlinkingBar }); // visible
```

### 5. Explicit Coordinate Responsibility
```csharp
// Views must explicitly convert coordinates
Point contentPos = new Point(_cursorCol, 0);
Point screenPos = ContentToScreen(contentPos);
SetCursor(new Cursor { Position = screenPos, Shape = CursorShape.BlinkingBar });

// Framework checks viewport clipping at screen level in ApplicationNavigation
```

### 6. Simpler Driver API
```csharp
// Driver level uses nullable shape
driver.SetCursorPosition(x, y);
driver.SetCursorShape(CursorShape.BlinkingBar); // visible
driver.SetCursorShape(null); // hidden
```

## Migration Strategy

### Phase 1: Add New Types (Non-Breaking)
1. Add `CursorShape` enum alongside `CursorVisibility`
2. Add `Cursor` class
3. Add `IOutput.SetCursorShape()` alongside `SetCursorVisibility()`

### Phase 2: Update Framework Internals
1. Update `ApplicationNavigation.UpdateCursor()` to use `Cursor` class
2. Update `View` to use `Cursor` property alongside old `CursorPosition`/`CursorVisibility`
3. Mark old APIs as `[Obsolete]` with migration guidance

### Phase 3: Update Built-in Views
1. Update `TextField`, `TextView`, `CharMap` to use `SetCursor(Cursor)`
2. Test thoroughly

### Phase 4: Remove Deprecated APIs (v3 Breaking Change)
1. Remove `CursorVisibility` enum
2. Remove old `View.SetCursor(Point?, CursorVisibility)` overload
3. Remove `IOutput.SetCursorVisibility()`
4. Remove `View.CursorPosition` and `View.CursorVisibility` properties

## Open Questions

1. **Should Cursor be a class or record struct?**
   - Record class (reference): Easier to cache/share, small GC pressure
   - Record struct (value): No allocations, copy overhead for passing around
   - **Recommendation**: Record class - cursor state changes infrequently, clarity over micro-optimization

2. **Should Position allow null or require separate hidden state?**
   - Current proposal: `Point? Position` (null = hidden) + `CursorShape` for when visible
   - Alternative: Always require `Point Position` + add `bool IsVisible` property
   - **Decision per @tig**: Use nullable - idiomatic C#, clear intent

3. **Should coordinate conversion be automatic or manual?**
   - Current proposal: Manual - views call `ContentToScreen()` before `SetCursor()`
   - Original design: Automatic - `Cursor.ContentArea()` factory + `ToScreen()` method
   - **Decision per @tig**: Manual - views responsible for conversion, explicit is better

4. **Should IOutput.SetCursorShape accept nullable?**
   - Current proposal: `SetCursorShape(CursorShape? shape)` where null = hide
   - Alternative: Keep `CursorShape` non-nullable, add separate `HideCursor()` method
   - **Recommendation**: Nullable - simpler API, aligns with ANSI DECTCEM concept

## Example Usage Scenarios

### Text Editor Cursor Movement
```csharp
// TextField updates cursor as user types
protected override bool OnKeyPress(Key e)
{
    if (e.IsCharKey)
    {
        // Insert character
        _text.Insert(_cursorPos, e.AsRune);
        _cursorPos++;

        // Convert content-area cursor position to screen coordinates
        Point screenPos = ContentToScreen(new Point(_cursorPos, 0));
        
        // Update cursor
        SetCursor(new Cursor
        {
            Position = screenPos,
            Shape = CursorShape.BlinkingBar
        });

        return true;
    }
    return base.OnKeyPress(e);
}
```

### View with No Cursor
```csharp
// Label never shows cursor
public class Label : View
{
    public Label()
    {
        CanFocus = false;
        SetCursor(new Cursor { Position = null });
    }
}
```

### Cursor Style Customization
```csharp
// Application-level cursor shape preference
public static CursorShape DefaultCursorShape { get; set; } = CursorShape.BlinkingBar;

// TextField respects preference
Point screenPos = ContentToScreen(new Point(_cursorPos, 0));
SetCursor(new Cursor
{
    Position = screenPos,
    Shape = DefaultCursorShape // User can set to SteadyBlock for accessibility
});
```

### Scrolling Text View
```csharp
// TextView handles viewport clipping automatically
protected override void UpdateCursor()
{
    if (!_cursorVisible)
    {
        SetCursor(new Cursor { Position = null });
        return;
    }

    // Cursor position in content area
    Point contentPos = new Point(_cursorCol, _cursorRow);
    
    // Convert to screen - ContentToScreen handles viewport offset
    Point screenPos = ContentToScreen(contentPos);
    
    // ApplicationNavigation.UpdateCursor() will check viewport clipping
    SetCursor(new Cursor
    {
        Position = screenPos,
        Shape = CursorShape.BlinkingBar
    });
}
```

## Conclusion

The proposed `Cursor` class design:
- ✅ Consolidates split position/shape state into single immutable object
- ✅ Uses nullable position for visibility (null = hidden) - clear and idiomatic
- ✅ Stores position in screen coordinates - views responsible for conversion
- ✅ Aligns with ANSI terminal standards (CursorShape enum based on DECSCUSR)
- ✅ Simplifies driver implementation (nullable shape at IOutput level)
- ✅ Provides clear migration path from current CursorVisibility model
- ✅ Explicit coordinate conversion prevents bugs

**Recommendation**: Proceed with implementation in phases, starting with non-breaking additions.

**Design Decisions Summary** (per @tig feedback):
1. ✅ Use nullable `Point? Position` instead of `CursorShape.Hidden` enum value
2. ✅ Remove automatic coordinate mapping - users convert manually via `ContentToScreen()`/`ViewportToScreen()`
3. ✅ Store coordinates in screen space only (no `CursorCoordinateSystem` enum needed)
4. ✅ No multi-cursor support (simplified design, can be added later if needed)
