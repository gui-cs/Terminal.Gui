# Issue #5359 — `NeedsDrawRect` Coordinate-System Normalization

**Branch:** `fix-5359-needsdraw-coord-normalization` (based on `fix-5358-stop-child-redraws-escalating`, which will be retargeted to `develop` after #5431 merges)

**Parent issue:** [#4973](https://github.com/gui-cs/Terminal.Gui/issues/4973)
**Sibling PR:** [#5431](https://github.com/gui-cs/Terminal.Gui/pull/5431) — covers AC1 (union math)
**This PR covers:** AC2 (subview cascade) and AC3 (viewport movement / negative locations).

---

## Background

`NeedsDrawRect` is currently treated inconsistently across the codebase:

- `SetNeedsDraw()` no-arg passes the *current* `Viewport` rectangle (i.e., `Viewport.Location` is the scroll offset; can be non-zero or negative). That stores content-coord rects in `NeedsDrawRect`.
- `DoDrawAdornments` escalation does the same: `NeedsDrawRect = Viewport`.
- The subview cascade in `SetNeedsDraw(Rectangle)` intersects `subview.Frame` (in `this`'s content coords) directly with `viewPortRelativeRegion`. Only works when `this.Viewport.Location == (0, 0)`.
- `CanNarrowClearToNeedsDrawRect` (added in #5431) consumes `NeedsDrawRect` via `ViewportToScreen`, which expects **viewport-local** coords (`(0, 0)` = top-left visible cell). That's why #5431 had to add a `Viewport.Location == Point.Empty` guard.

The deferred follow-up called out in PR #5431's description is exactly this: *normalize the convention*.

## Convention

> **`NeedsDrawRect` and the `viewPortRelativeRegion` parameter of `SetNeedsDraw(Rectangle)` are in viewport-LOCAL coordinates: `(0, 0)` = top-left visible cell of the View's Viewport (inside `Padding`, after any scroll).**

This matches what `ViewportToScreen(in Rectangle)` already expects and is the meaning documented for `ViewportToScreen` itself.

## Changes

### 1. `View.NeedsDraw.cs`

- Update the XML doc on `NeedsDrawRect`, `SetNeedsDraw()`, and `SetNeedsDraw(Rectangle)` to explicitly state "viewport-local; `(0, 0)` = top-left visible cell."
- Fix `SetNeedsDraw()` no-arg to pass `new Rectangle(Point.Empty, Viewport.Size)` — never propagate the scroll offset into the dirty rect.
- Fix the subview cascade:
  - Translate `viewPortRelativeRegion` → `this`'s content coords by adding `Viewport.Location`.
  - Intersect with `subview.Frame` (already in `this`'s content coords).
  - Translate the intersection to subview-frame-local by subtracting `subview.Frame.Location`.
  - Translate to subview-viewport-local by subtracting `subview.GetViewportOffsetFromFrame()` and `subview.Viewport.Location`.
  - Clip to the subview's viewport bounds (`new Rectangle(Point.Empty, subview.Viewport.Size)`); if empty, fall back to `subview.SetNeedsDraw()` no-arg (the subview still needs *some* redraw since the parent invalidation touched its frame area, but it's all in adornment / scrolled-off territory — let it do a safe full redraw).

### 2. `View.Drawing.Adornments.cs`

- `DoDrawAdornments` escalation: `NeedsDrawRect = new Rectangle(Point.Empty, Viewport.Size)` instead of `Viewport`.

### 3. `View.Layout.cs`

- `SetFrame`'s `SuperView.SetNeedsDraw(Rectangle.Union(prev, frame))` passes a rect in SuperView's content coords. Translate to SuperView's viewport-local by subtracting `SuperView.Viewport.Location` before passing.

### 4. `View.Drawing.cs`

- `CanNarrowClearToNeedsDrawRect`: remove the `Viewport.Location != Point.Empty` early-return. Update the comment block to reflect that narrowing is now safe regardless of scroll because `NeedsDrawRect` is consistently viewport-local.

### 5. Tests

- `Tests/UnitTestsParallelizable/ViewBase/Draw/NeedsDrawTests.cs`:
  - `NeedsDrawRect_Is_Viewport_Relative` — update the scrolled-frame assertions. The post-scroll `view.Frame = (3, 3, 6, 6)` step previously asserted `NeedsDrawRect = (0, 0, 7, 7)` because the broken `SetNeedsDraw` passed `Viewport = (1, 1, 6, 6)`. With the fix it passes `(0, 0, 6, 6)`, and accumulating union with prior `(0, 0, 6, 6)` stays `(0, 0, 6, 6)`.

- New file `Tests/UnitTestsParallelizable/ViewBase/Draw/NeedsDrawCoordTests.cs`:
  - `SetNeedsDraw_NoArg_OnScrolledView_StoresViewportLocalRect` — Viewport at `(5, 3, 10, 8)`; `SetNeedsDraw()` → `NeedsDrawRect == (0, 0, 10, 8)`.
  - `SetNeedsDraw_NoArg_OnNegativeViewportLocation_StoresViewportLocalRect` — `AllowNegativeX`, `Viewport.X = -3`; `SetNeedsDraw()` → `NeedsDrawRect == (0, 0, W, H)`.
  - `SetNeedsDraw_ScrolledParent_CascadesViewportLocalRectToSubview` — parent scrolled by `(5, 0)`; invalidate content-area rect that overlaps a subview; subview's `NeedsDrawRect` matches the expected viewport-local slice.
  - `SetNeedsDraw_SubViewWithPadding_CascadesViewportLocalRectIntoSubviewViewport` — subview has `Padding.Thickness = (1)`; parent invalidation that covers subview's full frame should land as `(0, 0, viewport.W, viewport.H)` in subview-viewport-local, not as the frame-local rect.
  - `SetNeedsDraw_ScrolledSubview_CascadesIntoSubviewViewportAccountingForScroll` — subview is itself scrolled; cascade produces correct viewport-local rect.
  - `SetNeedsDraw_DirtyRegionInSubviewAdornmentOnly_FallsBackToFullSubviewRedraw` — invalidation overlaps only subview's padding/border; subview's `NeedsDrawRect` ends up as full viewport (safe fallback) and the subview's adornments are flagged.
  - `FrameworkNarrowing_NowWorks_OnScrolledView` — after removing the guard, a scrolled view with a partial `NeedsDrawRect` narrows the clear correctly.
  - `SetNeedsDraw_ZeroSizeViewport_IsNoOp` — edge case.

## Acceptance Criteria Mapping

| AC | Coverage |
|----|----------|
| AC1 (repeated small invalidations → correct union) | Already in PR #5431 (`Rectangle.Union`). Existing test `SetNeedsDraw_MultipleRectangles_Expands` covers it. |
| AC2 (parent → subview translation correct) | Fixed cascade + new tests `SetNeedsDraw_ScrolledParent_CascadesViewportLocalRectToSubview`, `…_SubViewWithPadding_…`, `…_ScrolledSubview_…`, `…_DirtyRegionInSubviewAdornmentOnly_…`. |
| AC3 (viewport movement / negative locations) | `SetNeedsDraw()` no-arg fix + new tests `…_OnScrolledView_…`, `…_OnNegativeViewportLocation_…`. |

## Out of Scope (per issue's "Non-Functional Requirements" / "do not expand")

- Region-based dirty rect (stay with `Rectangle`).
- Per-cell partial rendering / clipping at the IOutput level.
- Removing the `NeedsDrawRect = Viewport.Size` escalation in `DoDrawAdornments` entirely (that's tied to the `LayoutAndDraw force=true` fan-out tracked separately in #5434).

## Verification

```bash
dotnet build --no-restore
dotnet test --project Tests/UnitTestsParallelizable --no-build
dotnet test --project Tests/UnitTests.NonParallelizable --no-build
dotnet test --project Tests/IntegrationTests --no-build
```

Expect: all green; no new warnings; existing test count + new tests added.
