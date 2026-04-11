# Inline / Non-Fullscreen Rendering — Research Findings

> Related issue: [#272 Make non-fullscreen apps possible](https://github.com/gui-cs/Terminal.Gui/issues/272)

## Goal

Enable Terminal.Gui developers to build apps that behave like **Claude Code CLI** and **GitHub Copilot CLI** — rendering UI inline in the current terminal buffer rather than switching to a separate, full-screen alternate screen buffer.  
The user should be able to continue their normal terminal session above/around the TG app, and the TG UI should appear and disappear as ordinary terminal output.

---

## How Claude Code CLI and Copilot CLI Achieve This

These tools do **not** use the ANSI alternate screen buffer (`CSI ?1049h`). Instead:

1. **Inline rendering** — output begins at the current cursor row, directly below the shell prompt, inside the primary (scrollback) buffer.
2. **Cell-level diffing** — they maintain an in-memory virtual "screen" and emit only the minimal ANSI escape sequences needed to update changed cells, avoiding full clears and full redraws.
3. **Virtual cursor tracking** — they track a virtual cursor offset *relative to their starting row*, not to the top-left of the terminal.
4. **Rewind-and-redraw** — for dynamic/animated content they move the cursor up N lines (via `CSI {n}A`) and overwrite only the changed cells; the rest of the terminal remains untouched.
5. **Transparent exit** — on exit they move the cursor to the row after the last rendered line so the next shell prompt appears naturally. The rendered content stays in scrollback history exactly like any other program output.

Both tools are built on or inspired by **[Ink](https://github.com/vadimdemedes/ink)**, a Node.js "React for terminals" library that uses the same inline strategy.

---

## What Terminal.Gui Currently Does (the Incompatibility)

In `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs` (constructor, ~line 111):

```csharp
// Activate alternate screen buffer, hide cursor, enable mouse tracking
Write(EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);  // ESC[?1049h
Write(EscSeqUtils.CSI_ClearScreen(EscSeqUtils.ClearScreenOptions.EntireScreen));
Write(EscSeqUtils.CSI_SetCursorPosition(1, 1));
```

Every TG application therefore:
1. Switches to the alternate screen buffer (hides scrollback, removes context).
2. Clears the entire screen.
3. Renders from absolute row 1, column 1.

This is the fundamental incompatibility with the inline pattern.

Relevant constants (defined in `EscSeqUtils.cs`):

| Constant | Sequence | Meaning |
|---|---|---|
| `CSI_SaveCursorAndActivateAltBufferNoBackscroll` | `ESC[?1049h` | Enter alternate screen |
| `CSI_RestoreCursorAndRestoreAltBufferWithBackscroll` | `ESC[?1049l` | Exit alternate screen |

---

## Root Cause: Three Cooperating Clear Mechanisms

There are three distinct mechanisms that conspire to clear the terminal when a TG app starts:

### 1. Alternate screen buffer switch (`AnsiOutput` constructor)

`Write(EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll)` — the `?1049h` sequence
switches to the **alternate screen buffer**, which is always blank. This is the most visible
"clear" and eliminates scrollback history for the duration of the run.

### 2. Explicit full-screen erase (`AnsiOutput` constructor)

`Write(EscSeqUtils.CSI_ClearScreen(ClearScreenOptions.EntireScreen))` — erases all visible
content. Redundant after `?1049h` but still sent.

### 3. All cells marked dirty in `OutputBufferImpl.ClearContents()`

Every cell is initialised with `IsDirty = true`, so the first frame loop in
`OutputBase.Write(IOutputBuffer)` overwrites every cell on screen — even cells the app never
drew anything into. This is the **most important** mechanism to fix for inline mode: even if
mechanisms 1 and 2 are skipped, the first render pass will overwrite the entire terminal area.

On exit, `?1049l` restores the main buffer and original cursor position, so the user's prior
scrollback and visible content come back — but **during the app run** the entire screen appears
cleared.

---

## What Needs to Change for Inline Mode

### 1. Do not switch to the alternate screen buffer

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`

Skip `CSI ?1049h` / `CSI ?1049l`. Stay in the primary (scrollback) buffer.  
Still send `CSI_HideCursor` and `CSI_EnableMouseEvents` as usual.

### 2. Discover the starting row

**Files:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`, `Terminal.Gui/Drivers/AnsiHandling/AnsiResponseParser*.cs`

Query the current cursor position at startup using Device Status Report:

```
ESC[6n
```

The terminal replies with:

```
ESC[{row};{col}R
```

Terminal.Gui already has full ANSI response-parser infrastructure (`AnsiResponseParser`, `AnsiRequestScheduler`, `AnsiEscapeSequenceRequest`) that can handle this response. On Windows, `GetConsoleScreenBufferInfo` can return the cursor row synchronously without a round-trip.  
Store the result as `_startRow` in `AnsiOutput`. Fall back to row `0` if the terminal does not reply (e.g. `screen`, `tmux`, legacy xterm).

### 3. Offset all output coordinates

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`

All row coordinates written to the terminal must be offset by `_startRow`. `SetCursorPositionImpl` should add `_startRow` to every row so the app renders starting at the correct position rather than from row 0. On size changes, recalculate how many rows are available below `_startRow`.

### 4. Stop marking all cells dirty at startup

**File:** `Terminal.Gui/Drivers/Output/OutputBufferImpl.cs`

This is the **most important behavioral change**. Add a `bool initiallyDirty` parameter (defaulting to `true` for backward compatibility) to `ClearContents()`. When inline mode is active, initialise cells with `IsDirty = false`. Only cells that `Draw` operations actually touch will be marked dirty and flushed — the rest of the visible terminal stays untouched.

### 5. Reserve space by scrolling

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`

Before rendering, if the app needs `N` rows and the cursor is too close to the bottom of the terminal, print `N` newlines to scroll the buffer and then move up N rows. Re-query the cursor position after scrolling to update `_startRow`. This ensures the region exists without overwriting existing content above.

### 6. Mouse coordinate correction

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiInputProcessor.cs`

The terminal reports mouse events in absolute terminal coordinates. Inline mode must subtract `_startRow` from the reported row before dispatching to views.

### 7. Resize handling

When the terminal is resized, the inline region may shift (content above may reflow). The safest strategy is to re-query the cursor position after a resize event and update `_startRow`.

### 8. Clean exit

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`

On `Shutdown`, move the cursor to `_startRow + inlineHeight` (the row immediately below the rendered region) so the shell prompt appears in the right place. Send `CSI_ShowCursor`. Do **not** emit `CSI ?1049l`.

### 9. Wire inline mode through the driver and component factory

**Files:** `Terminal.Gui/Drivers/Driver.cs`, `Terminal.Gui/Drivers/AnsiDriver/AnsiComponentFactory.cs`

Add an `InlineMode` (or `NoClear`) configuration property to `Driver` alongside `Force16Colors` and `SizeDetection`, decorated with `[ConfigurationProperty(Scope = typeof(SettingsScope))]`. `AnsiComponentFactory` creates `AnsiOutput`; pass the flag into the `AnsiOutput` constructor so all pieces are aware of the mode. Default behavior (alt screen + full clear) is **unchanged** when the flag is `false`.

---

## Technical Challenges Summary

| Challenge | Notes |
|---|---|
| Cursor position at startup | Query with `ESC[6n`; must wait for async response before first render; fall back to row `0` if no reply |
| Dirty-cell init | `OutputBufferImpl.ClearContents()` must not mark all cells dirty in inline mode — this is what actually overwrites the screen |
| Mouse coordinates | Subtract `_startRow` from reported row |
| Scrollback interference | Content scrolling past the inline region shifts its effective start row |
| Resize events | Re-query cursor position; layout must reflow within fixed inline height |
| Windows Console | Can use `GetConsoleScreenBufferInfo` for synchronous cursor-row query |
| `NetDriver` | Does not use alternate screen; inline mode may be simpler or already closer |
| Screen-reading approaches | DECRQCRA / screen-content APIs are unreliable and disabled in many terminals — avoid |
| CPR compatibility | `screen`, `tmux`, legacy xterm may not respond to `ESC[6n`; default to row `0` |
| Scrollback during run | If the user scrolls while the app is running, coordinate mapping will be wrong — inherent in the approach |

---

## Proposed API (Sketch)

```csharp
// Existing full-screen behaviour (unchanged)
Application.Run(new Window());

// New inline mode — renders a fixed-height region at the current cursor
Application.RunInline(view, height: 10);

// Or via Init
Application.Init(inline: true, inlineHeight: 10);
```

---

## File Change Summary

| File | Change |
|------|--------|
| `Terminal.Gui/Drivers/Driver.cs` | Add `InlineMode` (or `NoClear`) static config property |
| `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs` | Skip alt-buffer/clear on init; skip restore on dispose; `_startRow` offset; CPR query; scroll-to-make-room |
| `Terminal.Gui/Drivers/AnsiDriver/AnsiComponentFactory.cs` | Pass inline-mode flag to `AnsiOutput` |
| `Terminal.Gui/Drivers/Output/OutputBufferImpl.cs` | `ClearContents()` `initiallyDirty` param — most critical change |
| `Terminal.Gui/Drivers/AnsiDriver/AnsiInputProcessor.cs` | Subtract `_startRow` from mouse row |

---

## Suggested Implementation Order

1. Add an `InlineMode` / `InlineHeight` property to `Driver` and wire it through `AnsiComponentFactory` to `AnsiOutput`.
2. Modify `AnsiOutput` constructor: when `InlineMode` is `true`, skip `CSI ?1049h`, query start row via `ESC[6n`, reserve vertical space.
3. Fix `OutputBufferImpl.ClearContents()` — add `initiallyDirty` parameter, pass `false` in inline mode.
4. Offset all row coordinates by `_startRow` in the render path (`SetCursorPositionImpl`).
5. Correct mouse event row coordinates in `AnsiInputProcessor`.
6. Implement clean-exit logic in `AnsiOutput.Dispose`.
7. Add `Application.RunInline(view, height)` convenience method.
8. Add a `UICatalog` scenario demonstrating inline mode.

---

## Prior Art

| Project | Language | Notes |
|---|---|---|
| [Ink](https://github.com/vadimdemedes/ink) | Node.js | Reference implementation of the inline pattern |
| Claude Code CLI | TypeScript | Forks Ink; adds cell-level diffing, double buffering |
| GitHub Copilot CLI | TypeScript | Uses the same inline strategy |
| [XtermSharp](https://github.com/migueldeicaza/XtermSharp) | C# | Full terminal emulator; does not solve the inline problem |
