# Plan: "No-Clear" / Preserve-Screen Mode for the ANSI Driver

## Problem Statement

When a Terminal.Gui app starts, several things conspire to clear all existing terminal content.
It is not possible to read the terminal screen buffer at startup (security/API constraints),
but it *is* possible to change the app/driver architecture so that the app does **not** clear the
terminal on startup. This plan focuses on the ANSI driver.

---

## Root Cause Analysis

There are **three cooperating mechanisms** that cause the terminal to be cleared when a TG app starts:

### 1. Alternate screen buffer switch (`AnsiOutput` constructor, line 111)

```
Write(EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll)  // ?1049h
```

The `?1049h` sequence saves the main-buffer cursor and switches to the **alternate screen
buffer**, which is always blank. This is the most visible "clear".

### 2. Explicit full-screen erase (`AnsiOutput` constructor, line 112)

```
Write(EscSeqUtils.CSI_ClearScreen(ClearScreenOptions.EntireScreen))  // CSI 2J
```

Erases all visible content. Redundant after `?1049h` but still sent.

### 3. All cells marked dirty in `OutputBufferImpl.ClearContents()`

Every cell is initialised with `IsDirty = true`, so the first frame loop in
`OutputBase.Write(IOutputBuffer)` overwrites every cell on screen — even cells where the app
drew nothing.

On exit, `?1049l` restores the main buffer and original cursor position, so the user's prior
scrollback and visible content come back. But **during the app run** the whole screen appears cleared.

---

## Goal

Allow TG to run without switching to the alternate screen buffer and without clearing the
terminal. The app draws on top of whatever the terminal already shows. Scrollback is preserved
throughout the session.

---

## Required Changes

### 1. Add a `NoClear` configuration property to `Driver`

- Add `public static bool NoClear { get; set; }` to `Terminal.Gui/Drivers/Driver.cs`
  (alongside `Force16Colors`, `SizeDetection`), decorated with `[ConfigurationProperty(Scope = typeof(SettingsScope))]`.
- Thread a corresponding instance flag through `IOutput` / `AnsiOutput`, set during construction.

**File:** `Terminal.Gui/Drivers/Driver.cs`

---

### 2. Modify `AnsiOutput` constructor

When `NoClear` is `true`:

- **Skip** `Write(EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll)`.
- **Skip** `Write(EscSeqUtils.CSI_ClearScreen(...))`.
- **Skip** `Write(EscSeqUtils.CSI_SetCursorPosition(1, 1))` — instead query the terminal's
  current cursor row via `CSI 6n` (CPR / Device Status Report) so TG knows where to begin
  rendering.
- Still send `CSI_HideCursor` and `CSI_EnableMouseEvents`.

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`

---

### 3. Modify `AnsiOutput.Dispose()`

When `NoClear` is `true`:

- **Skip** `Write(EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll)`.
- Instead: send `CSI_ShowCursor`, then position the cursor one row below the last rendered row
  so subsequent shell output appears cleanly beneath the TG UI.

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`

---

### 4. Make `OutputBufferImpl.ClearContents()` respect `NoClear`

This is the **most important behavioral change**. Currently every cell starts with
`IsDirty = true`, causing the entire terminal area to be overwritten on the first frame —
even cells where the app drew nothing.

- Add a `bool initiallyDirty` parameter (defaulting to `true` for backward compat) to
  `ClearContents()`.
- When `NoClear` is `true`, initialise cells with `IsDirty = false`.
- Only cells that view `Draw` operations actually touch will be marked dirty and flushed.
- The rest of the visible terminal stays untouched.

**File:** `Terminal.Gui/Drivers/Output/OutputBufferImpl.cs`

---

### 5. Cursor-position-based rendering offset (partial-screen mode)

When `NoClear` is `true`, TG needs to know **which terminal row to start drawing at**:

- After constructing `AnsiOutput`, send `CSI 6n` and parse the CPR response
  (`ESC[row;colR`) to get the current cursor row.
- Store this as `_startRow` in `AnsiOutput`.
- `SetCursorPositionImpl` adds `_startRow` to all row coordinates so the app renders
  starting at the correct position rather than from row 0.
- On size changes, recalculate how many rows are available below the start row.

**Files:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`,
`Terminal.Gui/Drivers/AnsiHandling/AnsiResponseParser*.cs`

---

### 6. Scroll-to-make-room logic

When `_startRow + appHeight > terminalHeight`, the terminal must scroll to make space:

- Send N newlines before beginning rendering to scroll the visible content up.
- Re-query the cursor position after scrolling to update `_startRow`.

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs`

---

### 7. Wire `NoClear` through the component factory

`AnsiComponentFactory` creates `AnsiOutput`; pass `Driver.NoClear` into the `AnsiOutput`
constructor so all pieces are aware of the mode.

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiComponentFactory.cs`

---

### 8. Mouse coordinate adjustment

Mouse events report absolute terminal rows. When `_startRow > 0` the app must subtract
`_startRow` from the reported row to obtain app-relative coordinates.

**File:** `Terminal.Gui/Drivers/AnsiDriver/AnsiInputProcessor.cs` (or wherever mouse events
are translated to app coordinates).

---

## What This Does NOT Require

- No changes to the Windows or .NET drivers.
- No changes to the view system, layout system, or `IApplication`.
- Default behavior (alt screen + full clear) is **unchanged** when `NoClear = false`.

---

## Caveats and Known Limitations

| Concern | Notes |
|---------|-------|
| **Non-fullscreen apps** | In `NoClear` mode, TG occupies the screen from the starting cursor row downward. If the app is full-screen height, it fills the visible terminal. Resize events need special handling because the "origin row" may shift. |
| **No content read-back** | TG still cannot read what is already on the terminal (security). It can only leave existing content alone; it cannot interact with or re-render it. |
| **Mouse coordinates** | Mouse events report absolute terminal rows; the app must subtract `_startRow` to get app-relative coordinates. |
| **CPR compatibility** | Some terminals (`screen`, `tmux`, legacy xterm configurations) do not respond to CPR reliably. A fallback default row of `0` (full-screen from top) is needed. |
| **Scrollback during run** | If the user scrolls the terminal while the app is running, the coordinate mapping will be wrong. This is inherent in the approach and not solvable without alt-screen. |

---

## File Change Summary

| File | Change |
|------|--------|
| `Terminal.Gui/Drivers/Driver.cs` | Add `NoClear` static config property |
| `Terminal.Gui/Drivers/AnsiDriver/AnsiOutput.cs` | Skip alt-buffer/clear on init; skip restore on dispose; add `_startRow` offset; CPR query |
| `Terminal.Gui/Drivers/AnsiDriver/AnsiComponentFactory.cs` | Pass `NoClear` flag to `AnsiOutput` |
| `Terminal.Gui/Drivers/Output/OutputBufferImpl.cs` | `ClearContents()` `initiallyDirty` param |
| `Terminal.Gui/Drivers/AnsiDriver/AnsiInputProcessor.cs` | Subtract `_startRow` from mouse row |
