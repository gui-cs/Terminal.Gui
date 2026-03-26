# Bug: Zero-Area Rectangles Corrupt Region Union Operations

## Summary

Zero-width and zero-height rectangles (e.g., `{X=0, Y=0, Width=0, Height=2}`) are not filtered out during `Region.Combine` operations. This causes the sweep-line algorithm in `MergeRectangles` to produce incorrect results, expanding rectangles to cover areas they should not.

## Observed Symptom

In `View.Drawing.AddDrawnRegionForAdornment`, the `exclusion` region for a `Border` adornment incorrectly covers cells that were never drawn. Specifically, a rectangle that should be `{X=2, Y=1, W=2, H=1}` (row 1 only) becomes `{X=2, Y=0, W=2, H=2}` (rows 0–1), causing transparency artifacts.

## Reproduction Path

A `TabView` subview with `TabOffset = -1` causes its `Border`'s `LineCanvas` to render one column to the left of the view's origin at `X = -1`. When `DoRenderLineCanvas` returns the drawn region, it includes a rectangle at `{X=-1, Y=0, W=1, H=2}`. This region is then intersected with the adornment's frame `{X=0, Y=0, W=4, H=2}` (line 1023 in `View.Drawing.cs`), which clips it to `{X=0, Y=0, W=0, H=2}` — a **zero-width rectangle**.

This zero-width rectangle is kept because `Rectangle.IsEmpty` returns `false` (it only returns `true` when *all four fields* are zero). The rectangle then enters the `Union` operation (line 1024), where it poisons the sweep-line merge.

## Root Cause

Two sites in `Region.cs` use `Rectangle.IsEmpty` to filter degenerate rectangles, but `.IsEmpty` is insufficient:

### Site 1: `MergeRectangles` (line 568)

```csharp
foreach (Rectangle r in rectangles)
{
    if (!r.IsEmpty)  // ← does NOT catch {0,0,0,2}
    {
        events.Add ((r.Left, true, r.Top, r.Bottom));
        events.Add ((r.Right, false, r.Top, r.Bottom));
    }
}
```

A zero-width rect like `{0,0,0,2}` has `Left == Right == 0`, so it generates a **start event** and an **end event at the same X coordinate**. The sort order places end events before start events at the same X. When the end event fires, the interval `(0,2)` is not yet in `activeCounts`, so the decrement is **silently dropped**. The start event then increments `activeCounts[(0,2)]` to 1, creating a **phantom +1** that is never balanced.

This phantom count propagates through subsequent X groups, causing `MergeVerticalIntervals` to see an active interval `(0,2)` where none should exist, which merges with the legitimate interval `(1,2)` and produces an inflated output rectangle.

### Site 2: `CombineInternal` / Intersect case (line 177)

```csharp
Rectangle intersected = Rectangle.Intersect (rect1, rect2);

if (!intersected.IsEmpty)  // ← does NOT catch zero-width/zero-height results
{
    intersections.Add (intersected);
}
```

`Rectangle.Intersect` can produce zero-area results (e.g., width=0 or height=0) when the input rectangles share only an edge. These are stored in the region and later fed to `MergeRectangles`, triggering Site 1.

## Sweep-Line Trace

Input to `MergeRectangles` (from the Union at line 1024):

| Source       | Rectangle              | Left | Right | Top | Bottom | Note              |
|--------------|------------------------|------|-------|-----|--------|-------------------|
| exclusion[0] | `{0, 0, 1, 2}`        | 0    | 1     | 0   | 2      |                   |
| exclusion[1] | `{1, 0, 1, 2}`        | 1    | 2     | 0   | 2      |                   |
| lineRegion[0]| `{0, 0, 0, 2}`        | 0    | 0     | 0   | 2      | **zero-width**    |
| lineRegion[1]| `{0, 0, 1, 1}`        | 0    | 1     | 0   | 1      |                   |
| lineRegion[2]| `{1, 0, 1, 2}`        | 1    | 2     | 0   | 2      |                   |
| lineRegion[3]| `{2, 1, 2, 1}`        | 2    | 4     | 1   | 2      |                   |

**At X=0** (sorted: end before start):

1. `(0, false, 0, 2)` — End event for zero-width rect. `activeCounts` is empty → **decrement lost**.
2. `(0, true, 0, 1)` — Start. `activeCounts[(0,1)] = 1`.
3. `(0, true, 0, 2)` — Start. `activeCounts[(0,2)] = 1`.
4. `(0, true, 0, 2)` — Start (duplicate). `activeCounts[(0,2)] = 2`. ← **Should be 1**.

**At X=2** (after processing X=1):

`activeCounts` has `(0,2): 3`. Two end events bring it to 1, not 0. So `(0,2)` remains active.

**At X=2..4**:

`MergeVerticalIntervals` sees `{(0,2), (1,2)}`, merges them → `{2, 0, 2, 2}`. **Wrong.** Should be `{2, 1, 2, 1}`.

## Fix

Both sites should check for positive area instead of relying on `Rectangle.IsEmpty`:

### Site 1 — `MergeRectangles` line 568

```csharp
// Before:
if (!r.IsEmpty)

// After:
if (r.Width > 0 && r.Height > 0)
```

### Site 2 — `CombineInternal` Intersect case line 177

```csharp
// Before:
if (!intersected.IsEmpty)

// After:
if (intersected.Width > 0 && intersected.Height > 0)
```

## Tests

`Tests/UnitTestsParallelizable/Drawing/Region/ZeroAreaRectangleTests.cs` — 6 tests that fail before the fix:

| Test | Bug site |
|------|----------|
| `MergeRectangles_ZeroWidthRect_IsIgnored` | Site 1 |
| `MergeRectangles_ZeroHeightRect_IsIgnored` | Site 1 |
| `Intersect_ProducingZeroWidthRect_ExcludesItFromResult` | Site 2 |
| `Intersect_ProducingZeroHeightRect_ExcludesItFromResult` | Site 2 |
| `Intersect_MixedResults_OnlyKeepsPositiveAreaRects` | Site 2 |
| `Union_WithZeroWidthRectInSource_DoesNotCorruptResult` | Sites 1 + 2 (end-to-end) |
