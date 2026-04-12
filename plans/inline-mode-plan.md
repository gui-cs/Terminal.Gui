# Inline Mode Implementation Plan

> **Issue**: [#4932 — Enable Inline rendering in addition to current AlternateScreenBuffer](https://github.com/gui-cs/Terminal.Gui/issues/4932)
> **PR**: [#4933](https://github.com/gui-cs/Terminal.Gui/pull/4933)
> **Branch**: `copilot/enable-inline-ren`
> **Status**: Core implementation complete — testing and polish remaining

---

## Goal

Enable Terminal.Gui apps to render inline in the primary terminal buffer (like Claude Code CLI / GitHub Copilot CLI) rather than switching to the alternate screen buffer. The UI appears below the current shell prompt, sizes itself by content, grows dynamically, and on exit leaves the rendered content in scrollback history.

---

## Completed Work

### Phase 1: Basic Inline Rendering
- `AnsiOutput`: Skip `CSI ?1049h` in inline mode; stay in primary buffer
- `OutputBufferImpl`: `InlineMode` flag; `ClearContents()` with `initiallyDirty: false`
- Row-level dirty tracking via `DirtyLines`
- Deferred first draw until ANSI size response or timeout
- `InlineCLI` example: Label → ListView + ObservableCollection

### Phase 2: Cursor Positioning via CPR
- `AnsiSizeMonitor`: CPR query (`CSI ?6n`), parse response, `InitialCursorRow`
- Cursor offset in `SetCursorPositionImpl` using `App.Screen.Y`
- Scroll-first logic for near-bottom positioning

### Phase 3: `App.Screen` Sub-Rectangle Architecture
- Decoupled `Driver.Screen` (full terminal) from `App.Screen` (inline sub-rectangle)
- `App.Screen.Y` = terminal row where inline region starts
- Layout/draw/clip all operate within `App.Screen` bounds
- `InlineState` simplified to just `InlineCursorRow`
- Dynamic growth: `App.Screen = { 0, Y-1, W, H+1 }` with terminal scrolling

### Phase 4: Instance-Based `AppModel` and Driver Wiring
- `AppModel` property on `IApplication` (replaces static `Application.AppModel`)
- Factory wiring through `IComponentFactory` → `DriverImpl` → `AnsiOutput`
- `ForceInlineCursorRow` for testing without real CPR

### Phase 5: PR #4935 Merge and `AnsiStartupGate`
- Merged PR #4935's ANSI startup readiness gate
- `AnsiStartupGate` created only for inline mode
- Defers first render until CPR + size responses arrive

### Phase 6: `RunnableWrapper<TView, TResult>`
- Generic wrapper for running any `View` as a `Runnable<TResult>` without dialog buttons
- Unlike `Prompt<TView, TResult>`, adds no Ok/Cancel buttons
- `ResultExtractor` func or auto-detect via `IValue<T>` interface
- `Height = Dim.Auto (DimAutoStyle.Content)` for correct inline sizing

### Phase 7: InlineColorPicker Example
- Demonstrates `RunnableWrapper<ColorPicker, Color?>` in inline mode
- ColorPicker with `ShowColorName = true`
- Double-click to accept, Esc to cancel
- Outputs selected color name to stdout

### Phase 8: Mouse Double-Subtraction Bug Fix
- **Bug**: `RaiseMouseEvent` mutated `Mouse.ScreenPosition` in-place; `MouseInterpreter` generator
  yielded same object for original + synthesized events → double subtraction of `App.Screen.Y`
- **Fix**: Save/restore `ScreenPosition` via `try/finally` in `RaiseMouseEvent`
- **Test**: `InlineMouseOffsetTests.RaiseMouseEvent_Inline_SynthesizedDoubleClick_HasCorrectOffset`

### Phase 9: AnsiSizeMonitor Race Condition Fixes
- **Bug 1**: `HandleCursorPositionResponse` called `CompleteStartupCursorPositionQuery()` BEFORE
  parsing `InitialCursorRow` — gate released with row still 0
- **Fix**: Moved gate completion to AFTER parsing
- **Bug 2**: `HandleSizeResponse` early-returned on null/empty response without completing the gate
- **Fix**: Added `CompleteStartupSizeQuery()` on the early-return path

---

## Remaining Work

### Full Test Suite Verification
- Run complete `UnitTestsParallelizable` and `UnitTests` suites
- Ensure no regressions from the mouse fix or other changes

### Manual Testing
- Test InlineColorPicker at various terminal rows (top, middle, bottom)
- Verify double-click works at all positions
- Verify Enter accepts, Esc cancels with correct exit codes
- Verify InlineCLI still works correctly

### PR Cleanup
- Review all uncommitted changes for stray debug code
- Ensure no new compiler warnings
- Update PR description with summary of changes

---

## Architecture Summary

| Property | FullScreen Mode | Inline Mode |
|---|---|---|
| `Driver.Screen` | Full terminal (e.g., `0,0,156,82`) | Full terminal — **never changes** |
| `App.Screen` | Same as Driver.Screen | Sub-rectangle (e.g., `0,72,156,10`) |
| `Runnable.Frame` | Relative to App.Screen | `0,0,156,10` — laid out within App.Screen |

### Key Design Decisions
- **Pure ANSI only** — no `Console.WindowWidth/Height` or Win32 APIs
- **Breaking changes OK** — this is beta
- `InlineState` contains only `InlineCursorRow` (set once from CPR)
- `App.Screen.Y` and `App.Screen.Height` carry all dynamic state
- Mouse coordinate correction: `mouseY -= App.Screen.Y` (with save/restore to avoid mutation leaking)

---

## Related Issues
- **#4934**: Add generic ANSI startup readiness gate (created during this work)
- **#272**: Make non-fullscreen apps possible (original issue)
