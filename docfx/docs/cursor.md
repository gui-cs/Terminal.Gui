# Cursor Management in Terminal.Gui

> [!NOTE]
> This document describes the cursor management system in Terminal.Gui v2.

## Overview

Terminal.Gui provides a unified cursor management system that separates the **Terminal Cursor** (the visible cursor indicator users see) from the **Draw Cursor** (internal rendering state).

**Key Concepts:**
- **Terminal Cursor**: The visible, blinking cursor indicator that shows where user input will go
- **Draw Cursor**: Internal position (`IOutputBuffer.Col/Row`) for rendering - NOT related to Terminal Cursor
- **Cursor Class**: Immutable record consolidating position (screen coordinates) and style
- **CursorStyle Enum**: ANSI-first cursor shape definitions

## The Cursor Class

Terminal.Gui uses a `Cursor` record class to represent cursor state:

```csharp
public record Cursor
{
    /// <summary>Position in screen coordinates. Null = hidden.</summary>
    public Point? Position { get; init; }
    
    /// <summary>Cursor visual style (blinking bar, block, etc.)</summary>
    public CursorStyle Style { get; init; } = CursorStyle.Hidden;
    
    /// <summary>True if cursor should be visible</summary>
    public bool IsVisible => Position.HasValue && Style != CursorStyle.Hidden;
}
```

**Key characteristics:**
- Immutable value type (`record`)
- Position always in screen coordinates
- Null position means cursor is hidden
- Use `with` expression to modify: `cursor with { Position = newPos }`

## CursorStyle Enum

The `CursorStyle` enum is based on ANSI/VT DECSCUSR terminal standards:

```csharp
public enum CursorStyle
{
    Default = 0,           // Implementation-defined (usually BlinkingBlock)
    BlinkingBlock = 1,     // █ (blinking)
    SteadyBlock = 2,       // █ (steady)
    BlinkingUnderline = 3, // _ (blinking)
    SteadyUnderline = 4,   // _ (steady)
    BlinkingBar = 5,       // | (blinking, common in text editors)
    SteadyBar = 6,         // | (steady)
    Hidden = -1            // No visible cursor
}
```

**Platform Mapping:**
- **ANSI Terminals**: Maps directly to DECSCUSR escape sequences (CSI Ps SP q)
- **Windows Console**: Drivers convert to CONSOLE_CURSOR_INFO (bVisible, dwSize)

## Setting the Cursor in Views

Views use the `View.Cursor` property to manage cursor state:

### Basic Usage

```csharp
// Set cursor at column 5, row 0 in viewport - convert to screen coords
Point screenPos = ViewportToScreen (new Point (5, 0));
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };

// Hide cursor
Cursor = new Cursor { Position = null };
// or
Cursor = new Cursor { Style = CursorStyle.Hidden };

// Update position keeping same style
Point newScreenPos = ViewportToScreen (new Point (6, 0));
Cursor = Cursor with { Position = newScreenPos };
```

### TextField Example

```csharp
protected override void OnDrawContent (Rectangle viewport)
{
    // Calculate cursor position in content coordinates
    int cursorCol = _cursorPosition - _scrollOffset;
    
    // Only set cursor if within viewport
    if (cursorCol >= 0 && cursorCol < Viewport.Width && HasFocus)
    {
        Point screenPos = ViewportToScreen (new Point (cursorCol, 0));
        Cursor = new Cursor 
        { 
            Position = screenPos, 
            Style = CursorStyle.BlinkingBar 
        };
    }
    else
    {
        // Cursor outside viewport or no focus - hide it
        Cursor = new Cursor { Position = null };
    }
    
    // ... drawing code ...
}
```

## Coordinate Systems

**CRITICAL**: `Cursor.Position` must ALWAYS be in screen coordinates.

Views have three coordinate systems:
1. **Content Area**: View's internal coordinates (e.g., document position in TextView)
2. **Viewport**: Visible portion of content (accounting for scrolling)
3. **Screen**: Absolute terminal screen coordinates

**Always convert before setting cursor:**

```csharp
// From content area
Point contentPos = new Point (column, row);
Point screenPos = ContentToScreen (contentPos);
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };

// From viewport
Point viewportPos = new Point (column, row);
Point screenPos = ViewportToScreen (viewportPos);
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };
```

## Cursor Visibility Rules

The framework automatically hides the cursor when:
1. `view.Enabled == false`
2. `view.Visible == false`
3. `view.CanFocus == false`
4. `view.HasFocus == false`
5. View is not the most focused view (not deepest in focus chain)
6. Cursor position is outside any ancestor viewport bounds

Views only need to:
- Set `Cursor.Position` when they want the cursor visible
- Use `null` or `CursorStyle.Hidden` to hide
- Convert coordinates to screen space

## Efficient Cursor Updates

### SetCursorNeedsUpdate()

When cursor position changes without requiring a full redraw, use:

```csharp
public void SetCursorNeedsUpdate ()
{
    App?.Driver?.SetCursorNeedsUpdate (true);
}
```

This signals the driver that cursor position needs updating on next iteration without triggering view redraw.

**When to use:**
- Cursor moves but view content unchanged
- Blinking/animation of cursor
- Focus changes

**When NOT to use:**
- View content changed (use `SetNeedsDraw()` instead)
- Layout changed

### Example: Cursor Movement

```csharp
private void MoveCursorRight ()
{
    _cursorPosition++;
    
    // Calculate new screen position
    int viewportCol = _cursorPosition - _scrollOffset;
    
    if (viewportCol >= 0 && viewportCol < Viewport.Width)
    {
        Point screenPos = ViewportToScreen (new Point (viewportCol, 0));
        Cursor = Cursor with { Position = screenPos };
        SetCursorNeedsUpdate (); // Efficient - no redraw needed
    }
}
```

## Implementation Details

### ApplicationNavigation

The `ApplicationNavigation` class manages cursor positioning at the application level:

```csharp
public class ApplicationNavigation
{
    internal void UpdateCursor (IOutput output)
    {
        View? focused = GetFocused ();
        
        // Only show cursor for most focused, enabled view
        if (focused is null || !focused.Enabled || !focused.Visible)
        {
            output.SetCursorVisibility (CursorStyle.Hidden);
            return;
        }
        
        Cursor cursor = focused.Cursor;
        
        if (!cursor.IsVisible)
        {
            output.SetCursorVisibility (CursorStyle.Hidden);
            return;
        }
        
        // Check cursor is within all ancestor viewports
        Point? screenPos = cursor.Position;
        if (screenPos.HasValue && IsWithinAllAncestors (focused, screenPos.Value))
        {
            output.SetCursorPosition (screenPos.Value.X, screenPos.Value.Y);
            output.SetCursorVisibility (cursor.Style);
        }
        else
        {
            output.SetCursorVisibility (CursorStyle.Hidden);
        }
    }
}
```

### Driver Interface

Drivers implement cursor control through `IOutput`:

```csharp
public interface IOutput
{
    void SetCursorPosition (int col, int row);
    void SetCursorVisibility (CursorStyle style);
    void SetCursorNeedsUpdate (bool needsUpdate);
}
```

## Common Patterns

### Text Editor Pattern

```csharp
public class MyTextEditor : View
{
    private int _cursorRow, _cursorCol;
    private int _scrollTop, _scrollLeft;
    
    protected override void OnDrawContent (Rectangle viewport)
    {
        // Draw text content...
        
        // Update cursor
        if (HasFocus)
        {
            int viewportRow = _cursorRow - _scrollTop;
            int viewportCol = _cursorCol - _scrollLeft;
            
            if (viewportRow >= 0 && viewportRow < viewport.Height &&
                viewportCol >= 0 && viewportCol < viewport.Width)
            {
                Point screenPos = ViewportToScreen (new Point (viewportCol, viewportRow));
                Cursor = new Cursor 
                { 
                    Position = screenPos, 
                    Style = CursorStyle.BlinkingBar 
                };
            }
            else
            {
                Cursor = new Cursor { Position = null };
            }
        }
    }
}
```

### List Selection Pattern

```csharp
public class MyListView : View
{
    private int _selectedIndex;
    
    protected override void OnDrawContent (Rectangle viewport)
    {
        // Draw list items with highlighting...
        
        // Hide cursor - use selection highlighting instead
        Cursor = new Cursor { Position = null };
    }
}
```

## Migration from Old API

### Before (v1 / early v2)

```csharp
// Old PositionCursor override
public override Point? PositionCursor ()
{
    if (!HasFocus) return null;
    
    int col = _cursorPos - _scrollOffset;
    if (col < 0 || col >= Viewport.Width) return null;
    
    Move (col, 0);  // ❌ WRONG - affects Draw Cursor
    return new Point (col, 0);  // Viewport-relative
}
```

### After (current v2)

```csharp
// New Cursor property approach
protected override void OnDrawContent (Rectangle viewport)
{
    // ... drawing code ...
    
    if (HasFocus)
    {
        int col = _cursorPos - _scrollOffset;
        
        if (col >= 0 && col < viewport.Width)
        {
            Point screenPos = ViewportToScreen (new Point (col, 0));
            Cursor = new Cursor 
            { 
                Position = screenPos,  // Screen-absolute
                Style = CursorStyle.BlinkingBar 
            };
        }
        else
        {
            Cursor = new Cursor { Position = null };
        }
    }
}
```

## Key Differences from Draw Cursor

**Terminal Cursor** vs **Draw Cursor** are completely separate:

| Aspect | Terminal Cursor | Draw Cursor |
|--------|----------------|-------------|
| Purpose | Show user where input goes | Track where next character renders |
| API | `View.Cursor` property | `IOutputBuffer.Col/Row` |
| Affected by | `Cursor = new Cursor { ... }` | `Move()`, `AddRune()`, `AddStr()` |
| Visibility | User sees blinking cursor | Internal only |
| Coordinates | Screen-absolute | Buffer-relative |
| Management | View sets explicitly | Rendering system updates |

**❌ NEVER do this:**
```csharp
// WRONG - Don't use Move() for cursor positioning
Move (cursorCol, cursorRow);  // This affects Draw Cursor, not Terminal Cursor
```

**✅ DO this:**
```csharp
// CORRECT - Use Cursor property
Point screenPos = ViewportToScreen (new Point (cursorCol, cursorRow));
Cursor = new Cursor { Position = screenPos, Style = CursorStyle.BlinkingBar };
```

## Best Practices

1. **Always use screen coordinates** for `Cursor.Position`
2. **Always convert** from content/viewport to screen before setting
3. **Use SetCursorNeedsUpdate()** for position-only changes (no redraw needed)
4. **Set to null** to hide cursor, don't use visibility tricks
5. **Never call `Move()`** for cursor positioning (it affects Draw Cursor)
6. **Don't access Driver directly** from views - use `View.Cursor` property
7. **Test with viewport scrolling** to ensure coordinates are correct
8. **Prefer immutable updates** using `with` expression

## Troubleshooting

### Cursor not visible

**Check:**
1. Is `Cursor.Position` set to a valid screen coordinate (not null)?
2. Is `Cursor.Style` something other than `Hidden`?
3. Does view have `HasFocus == true`?
4. Is view the most focused (deepest in focus chain)?
5. Is cursor position within all ancestor viewports?
6. Is view `Enabled` and `Visible`?

### Cursor in wrong position

**Check:**
1. Are you using screen coordinates (not content/viewport)?
2. Did you call `ViewportToScreen()` or `ContentToScreen()`?
3. Is viewport scrolling accounted for?
4. Are ancestor viewports considered?

### Cursor flickers

**Check:**
1. Are you calling `SetNeedsDraw()` when only cursor moved? (Use `SetCursorNeedsUpdate()` instead)
2. Is cursor being set/unset multiple times per frame?
3. Is cursor style changing unnecessarily?

## See Also

- [Navigation and Focus](navigation.md) - Focus management and view hierarchy
- [Drivers](drivers.md) - Driver architecture and platform abstraction
- [View Deep Dive](View.md) - View class and hierarchy
- [Events](events.md) - Keyboard and mouse event handling
