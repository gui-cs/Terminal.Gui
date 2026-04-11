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

## What Needs to Change for Inline Mode

### 1. Do not switch to the alternate screen buffer

Skip `CSI ?1049h` / `CSI ?1049l`. Stay in the primary (scrollback) buffer.

### 2. Discover the starting row

Query the current cursor position at startup using Device Status Report:

```
ESC[6n
```

The terminal replies with:

```
ESC[{row};{col}R
```

Terminal.Gui already has full ANSI response-parser infrastructure (`AnsiResponseParser`, `AnsiRequestScheduler`, `AnsiEscapeSequenceRequest`) that can handle this response. On Windows, `GetConsoleScreenBufferInfo` can return the cursor row synchronously without a round-trip.

### 3. Offset all output coordinates

All row coordinates written to the terminal must be offset by `startRow`. When rendering logical row `r`, the actual terminal row sent is `startRow + r`.

### 4. Reserve space by scrolling

Before rendering, if the app needs `N` rows and the cursor is too close to the bottom of the terminal, print `N` newlines to scroll the buffer and then move up N rows. This ensures the region exists without overwriting existing content above.

### 5. Mouse coordinate correction

The terminal reports mouse events in absolute terminal coordinates. Inline mode must subtract `startRow` from the reported row before dispatching to views.

### 6. Resize handling

When the terminal is resized, the inline region may shift (content above may reflow). The safest strategy is to re-query the cursor position after a resize event and update `startRow`.

### 7. Clean exit

On `Shutdown`, move the cursor to `startRow + inlineHeight` (the row immediately below the rendered region) so the shell prompt appears in the right place. Do **not** emit `CSI ?1049l`.

---

## Simpler / Partial Approach: Mintty Status Line

For a subset of the use-case (a dedicated panel at the bottom of the terminal), the **Mintty status line extension** offers a purpose-built mechanism:

- `CSI 2;<height>$~` — activate a status area of `<height>` rows at the bottom of the terminal.
- `CSI 0$~` — remove the status area on exit.

The terminal automatically handles resize and keeps normal output out of the status area.

This could be exposed as `Application.RunAsStatusLine(view, height: N)`. It is less portable (Mintty-only) but trivially simple to implement when supported.

Reference: <https://github.com/mintty/mintty/blob/master/wiki/CtrlSeqs.md#status-line--area>

---

## Technical Challenges Summary

| Challenge | Notes |
|---|---|
| Cursor position at startup | Query with `ESC[6n`; must wait for async response before first render |
| Mouse coordinates | Subtract `startRow` from reported row |
| Scrollback interference | Content scrolling past the inline region shifts its effective start row |
| Resize events | Re-query cursor position; layout must reflow within fixed inline height |
| Windows Console | Can use `GetConsoleScreenBufferInfo` for synchronous cursor-row query |
| `NetDriver` | Does not use alternate screen; inline mode may be simpler or already closer |
| Screen-reading approaches | DECRQCRA / screen-content APIs are unreliable and disabled in many terminals — avoid |

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

## Suggested Implementation Order

1. Add an `InlineMode` / `InlineHeight` property to `IApplication` / `ApplicationImpl`.
2. Modify `AnsiOutput` constructor: when `InlineMode` is `true`, skip `CSI ?1049h`, query start row, reserve vertical space.
3. Offset all row coordinates by `startRow` in the render path.
4. Correct mouse event row coordinates in `ApplicationMouse` / `AnsiInput`.
5. Implement clean-exit logic in `AnsiOutput.Dispose` / `ApplicationImpl.Shutdown`.
6. Add `Application.RunInline(view, height)` convenience method.
7. Add a `UICatalog` scenario demonstrating inline mode.
8. Optionally, add Mintty status-line support as a separate fast-follow.

---

## Prior Art

| Project | Language | Notes |
|---|---|---|
| [Ink](https://github.com/vadimdemedes/ink) | Node.js | Reference implementation of the inline pattern |
| Claude Code CLI | TypeScript | Forks Ink; adds cell-level diffing, double buffering |
| GitHub Copilot CLI | TypeScript | Uses the same inline strategy |
| [XtermSharp](https://github.com/migueldeicaza/XtermSharp) | C# | Full terminal emulator; does not solve the inline problem |
| Mintty | C | Status-line API for dedicated bottom-of-screen panel |
