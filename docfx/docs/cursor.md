# Cursor and Focus Management - Design Analysis and Improvement Proposal

> [!IMPORTANT]
> This document describes the current cursor and focus implementation in Terminal.Gui v2_develop and proposes improvements to address design issues.

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

## Requirements for Any Cursor System Design

Based on analysis of the current implementation and issues, any cursor system design MUST meet these requirements:

1. **No Flickering** - The cursor should blink/pulse at the rate dictated by the terminal. Typing, mouse movement, view layout changes, Popovers visible, etc. should NOT cause the cursor to flicker.

2. **Hidden by Default** - By default, the cursor should not be visible. Views should not have to do anything special to keep the cursor invisible.

3. **Simple for Views** - Views that want to show the cursor should only have to:
   - Call `SetCursor (Point contentAreaRelativeCursorPosition, CursorVisibility visibilty)`. 
   - `View` will have get-only properties for `CursorVisibility` and `CursorPosition`.

4. **Only Visible When Appropriate** - The cursor should only be visible when:
   - `view.Enabled == true`
   - `view.Visible == true`
   - `view.CanFocus == true`
   - `view.HasFocus == true`
   - `view` is the most focused view (deepest in focus chain)
   - The cursor position within `view` is visible within the viewport (not scrolled out of view)

5. **Application-Managed** - The application/framework should be responsible for ultimate positioning and showing/hiding the cursor, not individual views.

6. **No Driver Access from Views** - View subclasses should NEVER directly call Driver APIs. Only `Application` and the `View` base class should interact with the driver.

7. **Separation of Draw Cursor and Terminal Cursor** - These are completely separate concepts:
   - **Draw Cursor**: Internal rendering state (`IOutputBuffer.Col/Row`) - where next character will be drawn
   - **Terminal Cursor**: Visible cursor indicator - where user's input will go
   - Drawing methods (`Move()`, `AddRune()`, `AddStr()`) affect ONLY the Draw Cursor
   - Cursor positioning (`SetCursor (point, visibilty)`) affects ONLY the Terminal Cursor

8. **Efficient** - Cursor positioning should not be recalculated unnecessarily. Cache when possible and only update when needed.

9. **Clear API Contract** - The cursor positioning API should be well-documented with clear expectations about when methods are called, what coordinates to return, and what views should/shouldn't do.

## New Design

**Goal:** Move from method-based (`PositionCursor()`) to property-based cursor management.

**New View API:**

```csharp
/// <summary>
///    Sets the cursor position and visibility for the view.
/// </summary>
/// <param name="position">The content area-relative cursor position, or null to hide the cursor.</param>
/// <param name="visibility">The cursor visibility style.</param>
public void SetCursor (Point? position, CursorVisibility visibility)
{
    _cursorPosition = position;
    CursorVisibility = visibility;
    SetCursorNeedsUpdate();
}

/// <summary>
/// Gets the viewport-relative cursor position, or null to hide the cursor.
/// </summary>
public Point? CursorPosition
{
    get => _cursorPosition;
    private set
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
public CursorVisibility CursorVisibility { 
    get => _cursorVisibility;
    private set{
    if (_cursorVisibility != value)
    {
       _cursorVisibility = value;
       SetCursorNeedsUpdate();
    }
}
```

**How it works:**
- Views call `SetCursor()` property instead of overriding `PositionCursor()`. They should only do this when the cursor position or visibility changes.
- Calling `SetCursor()` calls `SetCursorNeedsUpdate()` only if the position or visibility actually changed.
- `ApplicationNavigation.UpdateCursor()` reads the  most focused View's properties instead of calling a method
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
        
        // Update cursor position
        Cursor = new Point(visualColumn, 0), CursorVisibility.Default;
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

## See Also

- [Navigation Deep Dive](navigation.md) - Complete navigation and focus management documentation
- [Application Architecture](application.md) - Application lifecycle and instance-based architecture
- [Drivers](drivers.md) - Low-level driver architecture and cursor APIs
- [Keyboard](keyboard.md) - Keyboard event handling
- [View Deep Dive](View.md) - View lifecycle and hierarchy
