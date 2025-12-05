# Proposed Design for a modern Cursor system in v2

> [!IMPORTANT]
> This document is a work in progress and does not represent the final design or even the current implementation.

See end for list of issues this design addresses.

## Tenets for Cursor Support (Unless you know better ones...)

1. **More GUI than Command Line**. The concept of a cursor on the command line of a terminal is intrinsically tied to enabling the user to know where keyboard import is going to impact text editing. TUI apps have many more modalities than text editing where the keyboard is used (e.g. scrolling through a `ColorPicker`). Terminal.Gui's cursor system is biased towards the broader TUI experiences.

2. **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and the cursor should behave in a way consistent with the terminal.

## Lexicon & Taxonomy

- Navigation - Refers to the user-experience for moving Focus between views in the application view-hierarchy. See [Navigation](navigation.md) for a deep-dive.
- Focus - Indicates which View in the view-hierarchy is currently the one receiving keyboard input. Only one view-hexarchy in an application can have focus (`view.HasFocus == true`), and there is only one View in a focused hierarchy that is the most-focused; the one receiving keyboard input. See [Navigation](navigation.md) for a deep-dive.
- Cursor - A visual indicator to the user where keyboard input will have an impact. There is one Cursor per terminal session.
- Cursor Location - The top-left corner of the Cursor. In text entry scenarios, new text will be inserted to the left/top of the Cursor Location. 
- Cursor Size - The width and height of the cursor. Currently the size is limited to 1x1.
- Cursor Style - How the cursor renders. Some terminals support various cursor styles such as Block and Underline.
- Cursor Visibility - Whether the cursor is visible to the user or not. NOTE: Some ConsoleDrivers overload Cursor Style and Cursor Visibility, making "invisible" a style. Terminal.Gui HIDES this from developers and changing the visibility of the cursor does NOT change the style.
- Caret - Visual indicator that  where text entry will occur. 
- Selection - A visual indicator to the user that something is selected. It is common for the Selection and Cursor to be the same. It is also common for the Selection and Cursor to be distinct. In a `ListView` the Cursor and Selection (`SelectedItem`) are the same, but the `Cursor` is not visible. In a `TextView` with text selected, the `Cursor` is at either the start or end of the `Selection`. A `TableView' supports mutliple things being selected at once.
- **Draw Cursor** - The internal position tracked by `OutputBuffer.Col` and `OutputBuffer.Row` that indicates where the next `AddRune()` or `AddStr()` call will write. This is NOT the same as the visible terminal cursor and should never be used for cursor positioning.

## Requirements

- No flickering. The Cursor should blink/pulse at the rate dictated by the terminal. Typing, moving the mouse, view layout, etc... should not caue the cursor to flicker.
- By default, the Cursor should not be visible. A View or View subclass should have to do anything (this is already the case) to keep the Cursor invisible.
- Views that just want to show the cursor at a particular location in the Viewport should only have to:
  - Optionally, declare a desired Cursor Style. Set `Application.CursorStyle`.
  - Indicate the Cursor Locaiton when internal state dictates the location has changed (debatable if this should be in content or viewport-relative coords). Just set `this.CursorPosition`.
  - To hide the cursor, simply set `this.CursorPostion` to `null`.
- The Cursor should only be visible in Views where
  - `Enabled == true`
  - `Visible == true`
  - `CanFocus == true`
  - `this == SuperView.MostFocused`
- If a `ConsoleDriver` supports Cursor Styles other than Default, they should be supported per-application (NOT View). 
- Ensuring the cursor is visible or not should be handled by `Application`, not `View`.
- General V2 Requirement: View sub-class code should NEVER call a `Driver.` API. Only `Application` and the `View` base class should call `ConsoleDriver` APIs; before we ship v2, all `ConsoleDriver` APIs will be made `internal`.

## Design

### `View` Focus Changes

It doesn't make sense the every View instance has it's own notion of `MostFocused`. The current implemention is overly complicated and fragile because the concept of "MostFocused" is handled by `View`. There can be only ONE "most focused" view in an application. `MostFocused` should be a property on `Application`.

* Remove `View.MostFocused`
* Change all references to access `Application.MostFocusedView` (see `Application` below)
* Find all instances of `view._hasFocus = ` and change them to use `SetHasFocus` (today, anyplace that sets `_hasFocus` is a BUG!!).
* Change `SetFocus`/`SetHasFocus` etc... such that if the focus is changed to a different view heirarchy, `Application.MostFocusedView` gets set appropriately. 

**MORE THOUGHT REQUIRED HERE** - There be dragons given how `Runnable` has `OnEnter/OnLeave` overrrides. The above needs more study, but is directioally correct.

### `View` Cursor Changes
* Add `public Point? CursorPosition`
    - Backed with `private Point? _cursorPosition`
    - If `!HasValue` the cursor is not visible
    - If `HasValue` the cursor is visible at the Point.
    - On set, if `value != _cursorPosition`, call `OnCursorPositionChanged()`
* Add `public event EventHandler<LocaitonChangedEventArgs>? CursorPositionChanged`
* Add `internal void OnCursorPositionChanged(LocationChangedEventArgs a)`
  * Not virtual
  * Fires `CursorPositionChanged`

### `ConsoleDriver`s

* Remove `Refresh` and have `UpdateScreen` and `UpdateCursor` be called separately. The fact that `Refresh` in all drivers currently calls both is a source of flicker.

* Remove the `xxxCursorVisibility` APIs and replace with:
  * `internal int CursorStyle {get; internal set; }`
    - Backed with `private int _cursorStyle`
    - On set, calls `OnCursorStyleChanged()`
  * Add `internal abstract void OnCursorStyleChanged()`
    - Called by `base` whenever the cursor style changes, but ONLY if `value != _cursorStyle`.

  * Add `internal virtual (int Id, string StyleName) []  GetCursorStyles()`
    - Returns an array of styles supported by the driver, NOT including Invisible. 
    - The first item in array is always "Default".
    - Base implementation returns `{ 0, "Default" }`
    - `CursesDriver` and `WindowsDriver` will need to implement overrides.

  * Add `internal Point? CursorPosition {get; internal set; }`
    - Backed with `private Point? _cursorPosition`
    - If `!HasValue` the cursor is not visible
    - If `HasValue` the cursor is visible at the Point.
    - On set, calls `OnCursorPositionChanged` ONLY if `value != _cursorPosition`.
  * Add `internal abstract void OnCursorPositionChanged()`
    - Called by `base` whenever the cursor position changes. 
    - Depending on the value of `CursorPosition`:
        - If `!HasValue` the cursor is not visible - does whatever is needed to make the cursor invisible.
        - If `HasValue` the cursor is visible at the `CursorPosition` - does whatever is needed to make the cursor visible (using `CursorStyle`).

  * Make sure the drivers only make the cursor visible (or leave it visible) when `CursorPosition` changes!

### `Application`

* 

* Add `internal static View FocusedView {get; private set;}` 
  - Backed by `private static _focusedView`
  - On set, 
    - if `value != _focusedView` 
        - Unsubscribe from `_focusedView.CursorPositionChanged`
        - Subscribe to `value.CursorPositionChanged += CursorPositionChanged`        
        - `_focusedView = value`
        - Call `UpdateCursor` 

* Add `internal bool CursorPositionChanged (object sender, LocationChangedEventArgs a)`

    Called when:

    - `FocusedView`
        - Has changed to another View (should cover `FocusedView.Visible/Enable` changes)
        - Has changed layout - 
        - Has changeed it's `CursorPosition`
    - `CursorStyle` has changed

    Does:

    - If `FocusedView is {}` and `FocusedView.CursorPosition` is visible (e.g. w/in `FocusedView.SuperView.Viewport`) 
        - Does `Driver.CursorPosition = ToScreen(FocusedView.CursorPosition)`
    - Else
        - Makes driver cursor invisible with `Driver.CursorPosition = null`

* Add `public static int CursorStyle {get; internal set; }`
  - Backed with `private static int _cursorStyle
  - If `value != _cursorStyle`
    - Calls `ConsoleDriver.CursorStyle = _cursorStyle` 
    - Calls `UpdateCursor`

* Add `public (int Id, string StyleName) []  GetCursorStyles()`
  - Calls through to `ConsoleDriver.GetCursorStyles()`



# Issues with Current Design

## `Driver.Row/Col`, which are changed via `Move` serves two purposes that confuse each other:

a) Where the next `AddRune` will put the next rune (**the "Draw Cursor"**)
b) The current "Cursor Location" (the visible terminal cursor)

**These are completely separate concepts that were conflated in the original design.**

The **Draw Cursor** (`OutputBuffer.Col`/`OutputBuffer.Row`) tracks where drawing operations will write characters. Every call to `Move()` during view drawing updates these values. By the end of drawing, they point to wherever the last `AddRune()` or `AddStr()` call left them - typically the bottom-right of the last drawn element.

The **Terminal Cursor** is the visible cursor indicator in the terminal that shows the user where their input will go. This should ONLY be positioned based on `View.PositionCursor()` for the focused view.

### The Core Problem

The conflation of these two concepts caused the cursor to be positioned at arbitrary "Draw Cursor" locations (wherever drawing happened to finish) instead of where the application actually wanted it. Any code that tried to use `Driver.Col`/`Driver.Row` for cursor positioning was fundamentally broken.

### The Fix (Applied 2025-01-13)

**In `OutputBase.Write(IOutputBuffer)`**: Removed the cursor visibility save/restore pattern that was causing flickering.

**Previous (Broken) Code:**
```csharp
CursorVisibility? savedVisibility = _cachedCursorVisibility;
SetCursorVisibility (CursorVisibility.Invisible);  // Hide while drawing

// ... draw everything ...

SetCursorVisibility (savedVisibility ?? CursorVisibility.Default);  // PROBLEM: Restores stale visibility!
_cachedCursorVisibility = savedVisibility;
```

The problem: After drawing, cursor visibility was restored to `savedVisibility`, which was whatever was set previously. This was often wrong:
- If views didn't want the cursor visible (returned `null` from `PositionCursor()`), it would get shown anyway
- The cursor would flicker on/off every frame during scrolling or other drawing operations
- The "saved" visibility was stale and didn't reflect the application's current intent

**Fixed Code:**
```csharp
// Hide cursor while writing to prevent flickering
// Note: ApplicationMainLoop.SetCursor() is responsible for positioning and 
// showing the cursor after drawing is complete
SetCursorVisibility (CursorVisibility.Invisible);

// ... draw everything ...

// DO NOT restore cursor visibility here - let ApplicationMainLoop.SetCursor() handle it
```

Now `OutputBase.Write()` only hides the cursor during drawing. The responsibility for showing the cursor at the correct location with the correct visibility is left entirely to `ApplicationMainLoop.SetCursor()`, which:
1. Calls `View.PositionCursor()` on the focused view
2. Converts the viewport-relative position to screen coordinates  
3. Sets the cursor position and visibility appropriately

This separation of concerns eliminates the flickering and ensures the cursor is only shown when and where the application actually wants it.

### Implications for Future Design

Any future cursor system design MUST maintain this separation:
- **Drawing operations** (`Move()`, `AddRune()`, `AddStr()`) should NEVER affect the visible terminal cursor
- **Cursor positioning** should be a separate, explicit operation based on application/view intent
- `OutputBuffer.Col` and `OutputBuffer.Row` are internal state for drawing and should not be exposed for cursor positioning

## The actual cursor position RARELY changes (relative to `Mainloop.Iteration`).

Derived from above, the current design means we need to call `View.PositionCursor` every iteration. For some views this is a low-cost operation. For others it involves a lot of math. 

This is just stupid.

**Potential optimization**: Cache the last cursor position and only call `PositionCursor()` when:
- Focus changes
- The focused view signals its cursor position changed (e.g. via `SetNeedsDraw()`)
- Layout changes

## Flicker

Related to the above, we need constantly Show/Hide the cursor every iteration. This causes ridiculous cursor flicker. 

**FIXED 2025-01-13**: The root cause was `OutputBase.Write()` restoring stale cursor visibility after drawing. See fix details above.

## `View.PositionCursor` is poorly spec'd and confusing to implement correctly

Should a view call `base.PositionCursor`? If so, before or after doing stuff? 

## Setting cursor visibility in `OnEnter` actually makes no sense

First, leaving it up to views to do this is fragile.

Second, when a View gets focus is but one of many places where cursor visibilty should be updated. 

# Related Issues

- [#3444](https://github.com/gui-cs/Terminal.Gui/issues/3444) - Cursor flickers in bottom right during TableView scrolling (FIXED 2025-01-13)
