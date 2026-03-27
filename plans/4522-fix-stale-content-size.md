# Fix: Stale Content Size Capture in LayoutSubViews (#4522)

## Status: COMPLETE

## Problem

In `View.Layout.cs`, `LayoutSubViews()` captures `contentSize` at line 721 **before** firing
`OnSubViewLayout` at line 723. Several View subclasses (Dialog, TableView, ListView, HexView)
call `SetContentSize()` from that callback. Children are then laid out with the **stale**
pre-callback `contentSize`.

Additionally, Dialog's `UpdateSizes()` had a secondary bug: for DimAuto dialogs, it didn't
floor content size at the Viewport size, causing the content area to shrink below what's visible.

## Fix Applied

### Change 1: Re-read contentSize after OnSubViewLayout (View.Layout.cs)
After `OnSubViewLayout` fires, re-read `contentSize` via `GetContentSize()` before firing the
`SubViewLayout` event and before laying out children. This ensures any `SetContentSize()` calls
made during the virtual method are reflected.

### Change 2: Fix Dialog UpdateSizes viewport floor (DialogTResult.cs)
Removed the `!Width.Has<DimAuto>` / `!Height.Has<DimAuto>` guards that prevented flooring at
Viewport size. For DimAuto dialogs, the Frame may be larger than `_minimumSubViewsSize` (e.g.
due to title width), so the content area should always be at least Viewport-sized.

## Verification

- [x] All 7 StaleContentSizeCaptureTests pass (including Test 3 which was the failing proof)
- [x] Full UnitTestsParallelizable suite passes (15,113 tests, 0 failures)
- [x] Full UnitTests.NonParallelizable suite passes (59 tests, 0 failures)
- [x] Commit the fix

## Files Changed
- `Terminal.Gui/ViewBase/View.Layout.cs` — LayoutSubViews method
