# Inline Mode Implementation Plan

> **Issue**: [#4932 — Enable Inline rendering in addition to current AlternateScreenBuffer](https://github.com/gui-cs/Terminal.Gui/issues/4932)
> **PR**: [#4933](https://github.com/gui-cs/Terminal.Gui/pull/4933)
> **Branch**: `copilot/enable-inline-ren`

---

## Goal

Enable Terminal.Gui apps to render inline in the primary terminal buffer (like Claude Code CLI / GitHub Copilot CLI) rather than switching to the alternate screen buffer. The UI appears below the current shell prompt, sizes itself by content, grows dynamically, and on exit leaves the rendered content in scrollback history.

---

## Architecture: The Key Insight

### The Problem with the Current Approach

The current implementation (Phase 1, done) hacks inline rendering by:
- Shrinking `Driver.Screen` / `App.Screen` to just the inline region height
- Using an `InlineRowOffset` in `AnsiOutput.SetCursorPositionImpl` to translate Screen coordinates to terminal coordinates
- Manually scrolling the terminal and adjusting offsets when the view grows

This is fundamentally broken because:
1. **`Driver.Screen == App.Screen`** — both are shrunk, losing knowledge of the full terminal
2. **Growth requires manual offset manipulation** — scrolling, offset adjustment, Screen resize, re-layout — all tightly coupled
3. **`App.Screen` doesn't support non-zero Y** — the `Screen` setter throws `NotImplementedException` for non-zero origins
4. **Frame.Y can't move** — when the view needs to grow upward (scroll), there's no mechanism to shift it

### The Correct Architecture

**Separate `Driver.Screen` (full terminal) from `App.Screen` (the inline sub-rectangle):**

| Property | FullScreen Mode | Inline Mode |
|---|---|---|
| `Driver.Screen` | Full terminal (e.g., `0,0,156,82`) | Full terminal (e.g., `0,0,156,82`) — **never changes** |
| `App.Screen` | Same as Driver.Screen | Sub-rectangle (e.g., `0,72,156,10`) |
| `Runnable.Frame` | Relative to App.Screen | `0,0,156,10` — laid out within App.Screen |

In inline mode:
- **`App.Screen.Y`** = terminal row where inline region starts (initially from CPR response)
- **`App.Screen.Height`** = current inline region height (from view's Frame.Height)
- **`App.Screen.Width`** = full terminal width
- **`Driver.Screen`** = always full terminal dimensions

**Growth is simple**: `App.Screen = (0, Y-1, W, H+1)`. Y decreases, Height increases. Terminal scrolling (newlines + CSI A) makes room.

**Coordinate offset is automatic**: `SetCursorPositionImpl` adds `App.Screen.Y` to all row coordinates. No separate `InlineRowOffset` hack needed.

**Clear/Clip are bounded**: `ClearContents` and `Clip` operate within `App.Screen`, not the full terminal.

**Exit cursor positioning**: Just move to `App.Screen.Y + App.Screen.Height`.

### What This Enables

- **`InlineState` struct simplifies to just `InlineCursorRow`** (the initial CPR value). `InlineRowOffset` and `InlineContentHeight` are replaced by `App.Screen.Y` and `App.Screen.Height`.
- **`AnsiOutput` no longer needs inline-specific state** — it reads offset from `App.Screen.Y` via the driver.
- **Mouse coordinate correction** is just `mouseY -= App.Screen.Y`.
- **The existing `NotImplementedException` in `Screen` setter was anticipating this** — now we implement it.

---

## What's Already Done (Phase 1)

### Committed

**Commit 1** (`d386db7f4`): Fix inline mode double-draw by deferring initial LayoutAndDraw
- `AnsiOutput`: Skip `CSI ?1049h` in inline mode; stay in primary buffer
- `OutputBufferImpl`: `InlineMode` property; `ClearContents()` uses `initiallyDirty: false`
- `IOutputBuffer`: `DirtyLines` row-level tracking
- `OutputBase.Write`: Skip rows where `DirtyLines[row] == false`
- `ApplicationImpl.Run`: Skip `LayoutAndDraw()` in `Begin()` for inline mode
- `ApplicationMainLoop`: Defer first draw until `InitialSizeReceived` or 500ms timeout
- `ISizeMonitor`: `InitialSizeReceived` property
- `AnsiSizeMonitor`: `InitialSizeReceived` starts false, set true on CSI 18t response
- `InlineCLI example`: Label → ListView + ObservableCollection
- **Tests**: 4 inline draw timing tests, 4 AppModel tests

**Commit 2** (`1e3d8c767`): Position inline view at cursor row via ANSI CPR query
- `AnsiSizeMonitor`: CPR query (CSI ?6n), parse response, `InitialCursorRow`
- `ISizeMonitor`: `InitialCursorRow` property
- `AnsiOutput`: `InlineRowOffset`, `InlineContentHeight`, offset in `SetCursorPositionImpl`
- `ApplicationImpl.Screen`: `InlineCursorRow`, Screen resize to `termHeight - cursorRow`
- `ApplicationMainLoop`: Pass cursor row to `ApplicationImpl` when inline confirmed
- `InlineCLI example`: `Y = 0` instead of `AnchorEnd()`
- **Tests**: 3 cursor position tests in AnsiSizeMonitorTests

### Uncommitted (Current Working State)

- `ApplicationImpl.Screen`: Scroll-first for near-bottom fix + dynamic growth block
- `AnsiOutput`: `InlineContentHeight` property, fixed Dispose cursor positioning
- `InlineState` struct on `IDriver` (replaces scattered properties on `AnsiOutput` and `ApplicationImpl`)
- `MainLoopCoordinator`: Wires `AnsiOutput.InlineStateGetter` callback to `Driver.InlineState`

### Lessons Learned

1. **Pure ANSI only** — user explicitly rejected `Console.WindowWidth/Height` and native Win32 APIs. All size/position detection must use CSI escape sequences.
2. **`OutputBufferImpl.SetSize()` calls `ClearContents()` THREE times** — via Cols setter, Rows setter, and explicit call. The `InlineMode` flag must affect all three.
3. **`CsiCursorPattern` matches 'R' as F3 key** — `AnsiRequestScheduler` intercepts expected CPR responses before keyboard parsing.
4. **`Begin()` calls `LayoutAndDraw()` directly** — this bypassed the iteration-level deferral and caused double-draw. Fixed by skipping in `Begin()` for inline mode.
5. **`ObservableCollection<T>` required for `ListView`** — `SetSource<T>()` doesn't accept `List<T>`.
6. **Don't auto-commit** — user wants to review diffs locally first.
7. **Border.Thickness affects visual position** — a top thickness of 4 pushed the visible frame down, which looked like an offset bug but wasn't.

---

## Phase 2: Proper `App.Screen` Sub-Rectangle Architecture

### Step 1: Enable Non-Zero `App.Screen` Origin

**Files**: `ApplicationImpl.Screen.cs`, `IApplication.cs`

Currently the `Screen` setter throws `NotImplementedException` for non-zero X/Y. Remove that restriction.

- Remove the `NotImplementedException` guard in `ApplicationImpl.Screen.set`
- `App.Screen` becomes the **application-level viewport** — a sub-rectangle of the full terminal
- In FullScreen mode: `App.Screen == Driver.Screen` (unchanged)
- In Inline mode: `App.Screen = { X=0, Y=cursorRow, Width=termWidth, Height=viewHeight }`

**Key**: `Driver.SetScreenSize` should NOT be called when only `App.Screen.Y` changes. The driver's screen always represents the full terminal. `App.Screen` is a separate concept.

### Step 2: Decouple `App.Screen` from `Driver.Screen`

**Files**: `ApplicationImpl.Screen.cs`, `DriverImpl.cs`

Currently `App.Screen` getter returns `Driver.Screen`. Change this:

```
// FullScreen: App.Screen == Driver.Screen (default behavior)
// Inline: App.Screen is independently tracked
```

- Add a backing field `_screen` to `ApplicationImpl`
- `App.Screen` getter: return `_screen` if set, else `Driver.Screen`
- `App.Screen` setter: for inline mode, store in `_screen` without calling `Driver.SetScreenSize`
- `Driver.Screen` always reflects the full terminal dimensions

### Step 3: Thread `App.Screen` Through Layout/Draw/Clip

**Files**: `ApplicationImpl.Screen.cs`, `OutputBase.cs`, `OutputBufferImpl.cs`

The layout/draw pipeline currently uses `Screen` (which is `Driver.Screen`). Change to use `App.Screen`:

- `View.Layout(views, Screen.Size)` — use `App.Screen.Size` (just width × height, no offset)
- `Driver.Clip = new Region(Screen)` — use `App.Screen` bounds (includes offset for clipping)
- `OutputBufferImpl` dimensions: sized to `App.Screen.Size` (not full terminal)
- `SetCursorPositionImpl`: add `App.Screen.Y` to row coordinate (replaces `InlineRowOffset`)

### Step 4: Simplify `InlineState`

**Files**: `InlineState.cs`, `IDriver.cs`, `DriverImpl.cs`, `AnsiOutput.cs`

With `App.Screen.Y` and `App.Screen.Height` carrying the dynamic state:

- `InlineState.InlineRowOffset` → **removed** (replaced by `App.Screen.Y`)
- `InlineState.InlineContentHeight` → **removed** (replaced by `App.Screen.Height`)
- `InlineState.InlineCursorRow` → **kept** (the original CPR value, set once at startup)
- `AnsiOutput.InlineStateGetter` → simplified to just read `App.Screen.Y` for cursor offset

### Step 5: Simplify Inline Setup in `LayoutAndDraw`

**Files**: `ApplicationImpl.Screen.cs`

Replace the current scroll-then-resize-then-offset logic with:

```
1. First layout pass: layout against full terminal to get desired view height
2. Calculate: startRow = InlineCursorRow, neededHeight = view.Frame.Height
3. If startRow + neededHeight > termHeight: scroll terminal, adjust startRow
4. Set App.Screen = { 0, startRow, termWidth, neededHeight }
5. Re-layout against App.Screen.Size
6. Draw (cursor offset is automatic via App.Screen.Y)
```

### Step 6: Simplify Dynamic Growth

**Files**: `ApplicationImpl.Screen.cs`

When the view wants to grow:

```
1. View.Frame.Height > App.Screen.Height? 
2. extraRows = Frame.Height - App.Screen.Height
3. canScroll = min(extraRows, App.Screen.Y)  // can only scroll if Y > 0
4. Scroll terminal (newlines + CSI A)
5. App.Screen = { 0, App.Screen.Y - canScroll, W, App.Screen.Height + canScroll }
6. Re-layout, draw
```

### Step 7: Fix Mouse Coordinate Correction

**Files**: `AnsiInputProcessor.cs` (or wherever mouse events are dispatched)

Mouse events from the terminal are in absolute terminal coordinates. In inline mode, subtract `App.Screen.Y` from the mouse row:

```
if (Application.AppModel == AppModel.Inline)
    mouseEvent.Y -= App.Screen.Y;
```

### Step 8: Fix Dispose/Exit Cursor

**Files**: `AnsiOutput.cs`

On dispose in inline mode, position cursor at `App.Screen.Y + App.Screen.Height` (just below the inline region). Read from the driver/app rather than from local state.

### Step 9: Clean Up Output Buffer

**Files**: `OutputBufferImpl.cs`, `OutputBase.cs`

- `ClearContents()` should only clear within `App.Screen` bounds in inline mode
- `DirtyLines` optimization remains — still needed to skip untouched rows
- Buffer dimensions match `App.Screen.Size`, not full terminal

### Step 10: Update Tests

**Files**: Tests throughout

- Update existing inline tests for the new architecture
- Add tests verifying `App.Screen` vs `Driver.Screen` independence
- Add tests for growth (App.Screen.Y decreases, Height increases)
- Add tests for mouse coordinate correction
- Add tests for exit cursor positioning

### Step 11: Update InlineCLI Example

**Files**: `Examples/InlineCLI/InlineCLI.cs`

- Update status bar to show `App.Screen` vs `Driver.Screen`
- Verify growth works with ListView items
- Clean up any workarounds from Phase 1

---

## Edge Cases to Address

| Case | Behavior |
|---|---|
| Cursor at row 0 | `App.Screen.Y = 0`, full terminal available, no scrolling needed |
| Cursor at bottom (e.g., row 80 of 82) | Scroll terminal to make room, `App.Screen.Y` adjusts down |
| View grows when `App.Screen.Y == 0` | Can't scroll further — view is clipped at terminal height |
| Terminal resize during run | Re-query size; adjust `App.Screen` if needed |
| Multiple runnables on session stack | Each gets its own region? Or share? (deferred — single runnable for now) |

---

## Files Changed Summary (Phase 2)

| File | Change |
|---|---|
| `ApplicationImpl.Screen.cs` | Remove `NotImplementedException`; add `_screen` backing field; simplify inline setup/growth |
| `IApplication.cs` | Document that `Screen` can have non-zero Y in inline mode |
| `DriverImpl.cs` | `Screen` always returns full terminal; `InlineState` simplified |
| `InlineState.cs` | Remove `InlineRowOffset` and `InlineContentHeight` |
| `AnsiOutput.cs` | Read offset from app/driver instead of local state; simplify Dispose |
| `OutputBufferImpl.cs` | Buffer sized to `App.Screen.Size` |
| `OutputBase.cs` | Cursor offset uses `App.Screen.Y` |
| `AnsiInputProcessor.cs` | Mouse Y correction |
| `MainLoopCoordinator.cs` | Simplify wiring (remove `InlineStateGetter` callback) |
| `ApplicationMainLoop.cs` | Simplify — just set `InlineCursorRow` on driver |
| `InlineCLI example` | Update status bar displays |
| Tests | Update and expand |

---

## Open Questions

1. **Should `App.Screen` be a `Rectangle` or a new type?** Rectangle works, but a dedicated `AppViewport` type could enforce invariants (e.g., X is always 0 for now, Y >= 0, etc.).

2. **How does `OutputBufferImpl` sizing relate to `App.Screen` vs `Driver.Screen`?** Currently the buffer is sized to `Driver.Screen`. In inline mode it should probably be sized to `App.Screen` to avoid allocating a full-terminal buffer when only 10 rows are used.

3. **Should the offset live in `OutputBase.SetCursorPositionImpl` or in `DriverImpl`?** If `DriverImpl` applies the offset before calling output, the output layer stays pure. If output applies it, the driver stays pure. The output layer is probably the right place since it's where CSI sequences are emitted.

4. **What happens when `App.Screen.Y == 0` and the view still needs to grow?** The view is clipped at terminal height. Internal scrolling (scrolling the view's own content) would be the responsibility of the view (e.g., `ScrollBar`), not the framework.

---

## Related Issues

- **#4934**: Add generic ANSI startup readiness gate for terminal capability queries (created during this work)
- **#272**: Make non-fullscreen apps possible (original issue, now addressed by this work)
