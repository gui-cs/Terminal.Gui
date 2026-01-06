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
/// Defines the shape and visibility of the terminal cursor, based on ANSI/VT terminal standards.
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
/// </remarks>
public enum CursorShape
{
    /// <summary>Cursor is hidden/invisible.</summary>
    /// <remarks>Maps to ANSI DECTCEM (CSI ? 25 l) hide cursor.</remarks>
    Hidden = 0,

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
- Separates visibility (Hidden vs visible shapes) from blinking behavior
- No platform-specific hex encoding - drivers handle mapping
- `Hidden` = 0 makes default initialization safe
- Clear naming: Shape + Blink state explicit

**Migration from CursorVisibility**:
- `Invisible` → `Hidden`
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
/// Represents a cursor with position, shape, and coordinate system information.
/// </summary>
/// <remarks>
/// <para>
/// This class consolidates cursor state that was previously split between position (Point?)
/// and visibility (CursorVisibility). It tracks the coordinate system to prevent errors
/// when converting between content-area, viewport, and screen coordinates.
/// </para>
/// <para>
/// Immutable value type - use with 'with' expression to modify:
/// <code>
/// Cursor newCursor = currentCursor with { Position = new Point(5, 0) };
/// </code>
/// </para>
/// </remarks>
public record Cursor
{
    /// <summary>
    /// Gets a hidden cursor (no position, Hidden shape).
    /// </summary>
    public static readonly Cursor Hidden = new() { Shape = CursorShape.Hidden };

    /// <summary>
    /// Gets the cursor position in the coordinate system specified by <see cref="CoordinateSystem"/>.
    /// </summary>
    /// <remarks>
    /// Null position indicates the cursor has no defined location (effectively hidden).
    /// Check <see cref="IsVisible"/> to determine if cursor should be shown.
    /// </remarks>
    public Point? Position { get; init; }

    /// <summary>
    /// Gets the cursor shape and visibility.
    /// </summary>
    public CursorShape Shape { get; init; } = CursorShape.Hidden;

    /// <summary>
    /// Gets the coordinate system that <see cref="Position"/> is relative to.
    /// </summary>
    public CursorCoordinateSystem CoordinateSystem { get; init; } = CursorCoordinateSystem.Screen;

    /// <summary>
    /// Gets whether the cursor is visible (has valid position and non-Hidden shape).
    /// </summary>
    public bool IsVisible => Position.HasValue && Shape != CursorShape.Hidden;

    /// <summary>
    /// Creates a cursor with content-area relative coordinates.
    /// </summary>
    /// <param name="position">Content-area relative position.</param>
    /// <param name="shape">Cursor shape.</param>
    /// <returns>New Cursor instance.</returns>
    public static Cursor ContentArea(Point position, CursorShape shape = CursorShape.BlinkingBar)
        => new() { Position = position, Shape = shape, CoordinateSystem = CursorCoordinateSystem.ContentArea };

    /// <summary>
    /// Creates a cursor with viewport-relative coordinates.
    /// </summary>
    /// <param name="position">Viewport-relative position.</param>
    /// <param name="shape">Cursor shape.</param>
    /// <returns>New Cursor instance.</returns>
    public static Cursor Viewport(Point position, CursorShape shape = CursorShape.BlinkingBar)
        => new() { Position = position, Shape = shape, CoordinateSystem = CursorCoordinateSystem.Viewport };

    /// <summary>
    /// Creates a cursor with screen-absolute coordinates.
    /// </summary>
    /// <param name="position">Screen-absolute position.</param>
    /// <param name="shape">Cursor shape.</param>
    /// <returns>New Cursor instance.</returns>
    public static Cursor Screen(Point position, CursorShape shape = CursorShape.BlinkingBar)
        => new() { Position = position, Shape = shape, CoordinateSystem = CursorCoordinateSystem.Screen };

    /// <summary>
    /// Converts this cursor to screen coordinates using the provided view context.
    /// </summary>
    /// <param name="view">View that owns this cursor (for coordinate conversion).</param>
    /// <returns>
    /// Cursor with screen coordinates, or <see cref="Hidden"/> if position is outside
    /// any ancestor viewport.
    /// </returns>
    public Cursor ToScreen(View view)
    {
        if (!Position.HasValue || Shape == CursorShape.Hidden)
        {
            return Hidden;
        }

        Point screenPos = CoordinateSystem switch
        {
            CursorCoordinateSystem.ContentArea => view.ContentToScreen(Position.Value),
            CursorCoordinateSystem.Viewport => view.ViewportToScreen(Position.Value),
            CursorCoordinateSystem.Screen => Position.Value,
            _ => throw new ArgumentOutOfRangeException()
        };

        // Check if position is within all ancestor viewports
        View? current = view;
        while (current is not null)
        {
            Rectangle viewportBounds = current.ViewportToScreen(
                new Rectangle(Point.Empty, current.Viewport.Size));

            if (!viewportBounds.Contains(screenPos))
            {
                return Hidden; // Outside ancestor viewport
            }

            current = current.SuperView;
        }

        return new Cursor
        {
            Position = screenPos,
            Shape = Shape,
            CoordinateSystem = CursorCoordinateSystem.Screen
        };
    }

    /// <summary>
    /// Returns string representation for debugging.
    /// </summary>
    public override string ToString()
    {
        if (!IsVisible)
        {
            return "Cursor { Hidden }";
        }

        return $"Cursor {{ Position = {Position}, Shape = {Shape}, CoordinateSystem = {CoordinateSystem} }}";
    }
}

/// <summary>
/// Defines the coordinate system used for cursor position.
/// </summary>
public enum CursorCoordinateSystem
{
    /// <summary>Position is relative to view's content area (scrollable area).</summary>
    ContentArea,

    /// <summary>Position is relative to view's viewport (visible area).</summary>
    Viewport,

    /// <summary>Position is absolute screen coordinates.</summary>
    Screen
}
```

### Updated View API

```csharp
namespace Terminal.Gui.ViewBase;

public partial class View
{
    private Cursor _cursor = Cursor.Hidden;

    /// <summary>
    /// Gets the current cursor for this view.
    /// </summary>
    /// <remarks>
    /// Use <see cref="SetCursor"/> to update the cursor position and shape.
    /// The cursor will only be visible when the view has focus and is the most focused view.
    /// </remarks>
    public Cursor Cursor => _cursor;

    /// <summary>
    /// Sets the cursor for this view.
    /// </summary>
    /// <param name="cursor">
    /// The cursor to set. Use <see cref="Cursor.ContentArea"/>, <see cref="Cursor.Viewport"/>,
    /// or <see cref="Cursor.Screen"/> factory methods, or <see cref="Cursor.Hidden"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Common patterns:
    /// <code>
    /// // Text cursor at column 5 in content area
    /// SetCursor(Cursor.ContentArea(new Point(5, 0), CursorShape.BlinkingBar));
    ///
    /// // Hide cursor
    /// SetCursor(Cursor.Hidden);
    ///
    /// // Update position keeping same shape
    /// SetCursor(_cursor with { Position = new Point(6, 0) });
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
        App?.Driver?.SetCursorNeedsUpdate(true);
    }
}
```

### Updated ApplicationNavigation API

```csharp
namespace Terminal.Gui.App;

public class ApplicationNavigation
{
    /// <summary>
    /// Updates the terminal cursor based on the currently focused view.
    /// </summary>
    public void UpdateCursor()
    {
        if (App?.Driver?.GetCursorNeedsUpdate() == false)
        {
            return;
        }

        View? mostFocused = App?.TopRunnableView?.MostFocused;

        if (mostFocused is null)
        {
            App?.Driver?.SetCursorShape(CursorShape.Hidden);
            return;
        }

        // Get cursor in view's coordinate system
        Cursor viewCursor = mostFocused.Cursor;

        // Convert to screen coordinates (handles viewport clipping)
        Cursor screenCursor = viewCursor.ToScreen(mostFocused);

        // Apply to driver
        if (screenCursor.IsVisible && screenCursor.Position.HasValue)
        {
            App.Driver.SetCursorPosition(screenCursor.Position.Value.X, screenCursor.Position.Value.Y);
            App.Driver.SetCursorShape(screenCursor.Shape);
        }
        else
        {
            App.Driver.SetCursorShape(CursorShape.Hidden);
        }

        App.Driver.SetCursorNeedsUpdate(false);
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
    /// Gets the current cursor shape.
    /// </summary>
    CursorShape GetCursorShape();

    /// <summary>
    /// Sets the cursor shape and visibility.
    /// </summary>
    /// <param name="shape">The cursor shape. Use <see cref="CursorShape.Hidden"/> to hide.</param>
    void SetCursorShape(CursorShape shape);
}
```

## Benefits of Proposed Design

### 1. Type Safety
- Single `Cursor` object prevents position/visibility desync
- Coordinate system tracked explicitly prevents coordinate confusion
- Immutable record prevents accidental mutation

### 2. Clearer API
```csharp
// Old API - unclear coordinate system, split state
view.SetCursor(new Point(5, 0), CursorVisibility.Vertical);

// New API - explicit coordinate system, unified state
view.SetCursor(Cursor.ContentArea(new Point(5, 0), CursorShape.BlinkingBar));
```

### 3. ANSI-First Design
- `CursorShape` enum values match ANSI DECSCUSR sequence parameters
- Drivers map to platform APIs (Windows, NCurses) internally
- No hex encoding exposure in public API

### 4. Reduced Null Confusion
```csharp
// Old: null position vs Invisible visibility both mean "hidden"
SetCursor(null, CursorVisibility.Invisible); // redundant
SetCursor(new Point(5, 0), CursorVisibility.Invisible); // conflicting

// New: single concept
SetCursor(Cursor.Hidden); // clear
SetCursor(Cursor.ContentArea(new Point(5, 0), CursorShape.Hidden)); // still hidden
```

### 5. Built-in Coordinate Conversion
```csharp
// Cursor class handles viewport clipping
Cursor screenCursor = viewCursor.ToScreen(view);
// Returns Cursor.Hidden if outside any ancestor viewport
```

### 6. Simpler Driver API
```csharp
// Old: two separate methods
driver.SetCursorPosition(x, y);
driver.SetCursorVisibility(visibility);

// New: still two methods but clearer semantics
driver.SetCursorPosition(x, y);
driver.SetCursorShape(shape); // Shape includes visibility (Hidden = invisible)
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
   - Record class (reference): Easier to cache/share, GC pressure
   - Record struct (value): No allocations, copy overhead
   - **Recommendation**: Record class - cursor state is small and not created frequently

2. **Should Position allow null or use separate Hidden state?**
   - Current: `Point? Position` + `CursorShape.Hidden` (two ways to hide)
   - Alternative: `Point Position` required + `Shape.Hidden` only
   - **Recommendation**: Keep nullable - allows "position unknown" vs "hidden at position"

3. **Should ToScreen() be on Cursor or a separate converter?**
   - Current: `cursor.ToScreen(view)` - convenient but couples Cursor to View
   - Alternative: `ViewCursorConverter.ToScreen(cursor, view)` - decoupled
   - **Recommendation**: Keep on Cursor - convenience outweighs coupling concern

4. **Factory methods vs constructors?**
   - Current: Static factory methods (Cursor.ContentArea, etc.)
   - Alternative: Public init-only properties + examples
   - **Recommendation**: Factories - clearer intent, harder to misuse

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

        // Update cursor - content area coordinates
        SetCursor(Cursor.ContentArea(
            new Point(_cursorPos, 0),
            CursorShape.BlinkingBar));

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
        SetCursor(Cursor.Hidden);
    }
}
```

### Cursor Style Customization
```csharp
// Application sets cursor style preference
Application.CursorShape = CursorShape.SteadyBlock; // Accessibility - no blinking

// TextField respects preference
SetCursor(Cursor.ContentArea(_cursorPos, Application.CursorShape));
```

### Multi-cursor (Future Enhancement)
```csharp
// Cursor record makes it easy to track multiple cursors
public class MultiCursorTextView : TextView
{
    private List<Cursor> _cursors = new();

    // Primary cursor sent to framework
    public override Cursor Cursor => _cursors.FirstOrDefault() ?? Cursor.Hidden;

    // Render additional cursors in OnDrawContent
    protected override void OnDrawContent(Rectangle contentArea)
    {
        base.OnDrawContent(contentArea);

        // Draw secondary cursors
        foreach (Cursor cursor in _cursors.Skip(1))
        {
            if (cursor.IsVisible && cursor.Position.HasValue)
            {
                // Draw custom cursor indicator
            }
        }
    }
}
```

## Conclusion

The proposed `Cursor` class design:
- ✅ Consolidates split position/visibility state
- ✅ Makes coordinate systems explicit and type-safe
- ✅ Aligns with ANSI terminal standards (baseline)
- ✅ Simplifies driver implementation
- ✅ Provides clear migration path
- ✅ Enables future enhancements (multi-cursor, accessibility)

**Recommendation**: Proceed with implementation in phases, starting with non-breaking additions.
