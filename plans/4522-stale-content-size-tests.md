# Plan: Failing Tests for Stale Content Size Capture in LayoutSubViews

**Issue:** [#4522](https://github.com/gui-cs/Terminal.Gui/issues/4522) — `NeedsLayout` is buggy  
**Sub-issue:** Stale Content Size Capture in `LayoutSubViews` ([comment by @duskfold](https://github.com/gui-cs/Terminal.Gui/issues/4522#issuecomment-4120387744))  
**PR:** [#4861](https://github.com/gui-cs/Terminal.Gui/pull/4861)  
**Branch:** `fix/4522-stale-content-size-capture`

## Problem Statement

In `View.Layout.cs`, `LayoutSubViews()` captures `contentSize` at line 721 *before* firing
`OnSubViewLayout` at line 723. Several View subclasses (`Dialog<TResult>`, `TableView`,
`ListView`, `HexView`) call `SetContentSize()` from within that callback (or from
`OnViewportChanged` which fires during layout). The children are then laid out using the
**stale** pre-callback `contentSize`.

Additionally, at line 771, `NeedsLayout` is set directly (`NeedsLayout = layoutStillNeeded`)
instead of calling `SetNeedsLayout()`, bypassing upward/downward propagation and preventing a
corrective layout pass on the next iteration.

## Bug Mechanics Summary

```
LayoutSubViews():
  721:  contentSize = GetContentSize()          ← snapshot (potentially stale)
  723:  OnSubViewLayout(...)                     ← Dialog calls SetContentSize() here
  741:  v.Layout(contentSize)                    ← uses stale value
  771:  NeedsLayout = layoutStillNeeded          ← bypasses SetNeedsLayout()
  773:  OnSubViewsLaidOut(...)                   ← HexView calls SetContentSize() here
```

## Approach

Write focused, minimal unit tests in `Tests/UnitTestsParallelizable` that demonstrate each
aspect of the bug. Each test should **fail on current code** and describe the correct expected
behavior so that future fixes can make them pass.

All tests use `CreateTestDriver()` (from `TestDriverBase`) — no `Application.Init`.

## Test File

`Tests/UnitTestsParallelizable/ViewBase/Layout/StaleContentSizeCaptureTests.cs`

## Tests

### Test 1: `LayoutSubViews_Uses_Stale_ContentSize_When_OnSubViewLayout_Changes_It`

**What it proves:** The core bug — `LayoutSubViews` captures `contentSize` before
`OnSubViewLayout` fires, so children are laid out with the wrong size.

**Setup:**
- Create a custom `View` subclass that overrides `OnSubViewLayout` to call
  `SetContentSize(new Size(50, 20))` (larger than default).
- Add a child view using `Width = Dim.Fill()`, `Height = Dim.Fill()`.
- Call `Layout()`.

**Assert:** The child's `Frame.Size` should equal the **new** content size `(50, 20)`, not
the stale pre-callback value. On current code, the child will have the wrong size.

### Test 2: `Dialog_Children_Use_Stale_ContentSize_After_Screen_Resize`

**What it proves:** The reported real-world scenario — a `Dialog` with `Dim.Fill` children
gets the wrong layout after a large screen size change (simulating maximize/restore).

**Setup:**
- Create a `Dialog` with `Width = Dim.Fill(4)`, `Height = Dim.Fill(2)`.
- Add children using `Dim.Fill()` / `Pos.Bottom(otherView)` chains.
- Set driver screen to 40×15, call `dialog.Layout()`, record child frames.
- Resize driver screen to 120×40 (simulating maximize), call `dialog.Layout()`.

**Assert:** Each child's `Frame` should reflect the **new** larger dialog content area.
On current code, children may retain sizes from the old layout because `UpdateSizes()` via
`OnSubViewLayout` changes content size after the snapshot.

### Test 3: `Dialog_OnSubViewLayout_SetContentSize_Diverges_From_Captured_Value`

**What it proves:** `Dialog.OnSubViewLayout` → `UpdateSizes()` → `SetContentSize()` produces
a content size that differs from what `LayoutSubViews` captured at line 721.

**Setup:**
- Create a `Dialog` with explicit (non-Auto) `Width` and `Height` (e.g., `Width = 60`, `Height = 20`).
- Add several subviews so `_minimumSubViewsSize` forces a content size larger than the viewport
  (so `UpdateSizes` computes `Max(minimumSubViewsSize, viewport)` ≠ viewport alone).
- Call `Layout()`.

**Assert:** After layout, `GetContentSize()` should equal the value that was used to lay out
children. On current code, `GetContentSize()` reflects the `SetContentSize` call from
`UpdateSizes`, but the children were laid out with the earlier stale value.

### Test 4: `NeedsLayout_Direct_Set_Does_Not_Propagate_To_SuperView`

**What it proves:** Line 771 sets `NeedsLayout = false` directly, bypassing `SetNeedsLayout`
propagation. If content size changed during layout, neither the view nor its SuperView know
they need another pass.

**Setup:**
- Create a SuperView → custom View (overrides `OnSubViewLayout` to call `SetContentSize`)
  → child view chain.
- Call `superView.Layout()`.
- After layout, the custom view's content size changed during `OnSubViewLayout`.

**Assert:** After layout, if content size changed, `NeedsLayout` should still be `true`
(or should have been propagated). On current code, `NeedsLayout` is set to `false` at line
771, masking the need for a corrective pass.

### Test 5: `LayoutSubViews_OnSubViewsLaidOut_SetContentSize_Is_Too_Late`

**What it proves:** Views that call `SetContentSize` from the `SubViewsLaidOut` event
(like `HexView`) change content size *after* children have already been laid out.

**Setup:**
- Create a custom `View` that subscribes to its own `SubViewsLaidOut` event and calls
  `SetContentSize(new Size(80, 30))`.
- Add a child with `Width = Dim.Fill()`, `Height = Dim.Fill()`.
- Call `Layout()`.

**Assert:** The child should be sized according to the content size `(80, 30)`. On current
code, the child is sized according to the content size that existed *before*
`SubViewsLaidOut` fired.

### Test 6: `ListView_OnViewportChanged_SetContentSize_Creates_Stale_Capture`

**What it proves:** `ListView.OnViewportChanged` calls `SetContentSize` during layout,
creating the same stale-capture condition.

**Setup:**
- Create a `ListView` with a data source of N items.
- Place it inside a container, layout at one size, then resize the container.
- Call `Layout()` again.

**Assert:** After the second layout, the `ListView`'s children (if any, or its own
`ContentSize`) should be consistent — `GetContentSize()` after layout should match what
was used during layout. Verify no stale divergence.

### Test 7: `TableView_RefreshContentSize_During_Layout_Creates_Stale_Capture`

**What it proves:** `TableView.OnViewportChanged` → `RefreshContentSize()` calls
`SetContentSize` during layout.

**Setup:**
- Create a `TableView` with sample data, place in a container.
- Layout, resize container, layout again.

**Assert:** After the second layout, `GetContentSize()` should be consistent with the
value used during child layout.

## Verification

```bash
cd issue-4522-needslayout
dotnet build --no-restore
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter "ClassName~StaleContentSizeCaptureTests"
```

All 7 tests should **fail** on the current codebase, proving the bugs exist.

## Notes

- Tests 1, 4, 5 use custom `View` subclasses to isolate the exact mechanics.
- Tests 2, 3 target the `Dialog` scenario from the issue report.
- Tests 6, 7 target the other affected classes (`ListView`, `TableView`).
- `HexView` is similar to Test 5 (uses `SubViewsLaidOut`) but is tested indirectly via the
  generic pattern. A dedicated `HexView` test could be added if needed.
- All tests go in `UnitTestsParallelizable` (no `Application.Init` dependency).
