# Specification: Adornment Transparency Support (Border & Padding)

## Issue

**#4834**: `Border` (and `Padding`) should support `ViewportSettingsFlags.Transparent` and `ViewportSettingsFlags.TransparentMouse`.

**Prerequisite for**: TabView redesign (#4183), where each Tab's Border renders a small header rectangle and the rest must be transparent.

## Problem Statement

Border draws **non-rectangular** content — an outline of line segments plus optional title text. The interior is empty. When transparency is enabled:

- **Transparent (visual)**: Only border lines and title text should be drawn; the empty interior should show underlying views.
- **TransparentMouse**: Clicks on border lines/title go to the Border; clicks on the empty interior pass through to views beneath.

Neither works on `v2_develop` today.

---

## Root Causes (on v2_develop)

### 1. Visual transparency fails for Border

`DoDrawComplete` (in `View.Drawing.cs`) has an `if (this is not Adornment)` guard that skips **all** clip exclusion logic for adornments. Setting `Transparent` on a Border has no effect — the parent view's opaque path excludes the entire border frame from the clip regardless.

### 2. Border writes to Parent.LineCanvas, not its own

`Border.OnDrawingContent` writes all border lines to `Parent.LineCanvas`, not to Border's own LineCanvas. The parent renders these later in its own `DoRenderLineCanvas`, and the drawn region gets reported to the **parent's** DrawContext — not the Border's. So the Border's own DrawContext never knows what cells the border lines actually occupy.

Additionally, `Adornment.SuperViewRendersLineCanvas` throws `InvalidOperationException` — adornments are blocked from using their own LineCanvas.

### 3. TransparentMouse is blanket per-view

`GetViewsUnderLocation` (in `View.Layout.cs`) does `RemoveAll(v => v!.ViewportSettings.HasFlag(excludeViewportSettingsFlags))` — blanket removal. There is no per-cell hit-testing.

### 4. Parent clears viewport even when Border is transparent

`DoClearViewport` clears the viewport unconditionally for opaque views. When the Border is transparent, the parent must NOT clear the border's interior area, otherwise the underlying content gets overwritten with blanks.

---

## Architecture of the Fix

The fix has two phases:

1. **Phase 1 — Visual Transparency**: Make `Border.Transparent` actually work (show through to underlying content).
2. **Phase 2 — Per-Cell TransparentMouse**: Make `TransparentMouse` respect which cells were actually drawn vs. empty.

Both phases apply to Padding as well, though Border is the primary use case.

---

## Phase 1: Visual Transparency for Border

### 1a. Fix Border LineCanvas Ownership

**Files**: `Adornment.cs`, `Border.cs`

**Problem**: Border writes to `Parent.LineCanvas`. The drawn region is tracked on the parent, not the Border.

**Changes**:

1. **`Adornment.cs`** — Change the `SuperViewRendersLineCanvas` property override. Remove the `InvalidOperationException` throw. Replace with a standard auto-property:

   ```csharp
   /// Gets or sets whether this Adornment's LineCanvas will be merged into
   /// its Parent view's LineCanvas for rendering.
   public override bool SuperViewRendersLineCanvas { get; set; }
   ```

   Default is `false` (backward-compatible). When `true`, the adornment adds lines to its own LineCanvas and the parent merges and renders them.

2. **`Border.cs` (`OnDrawingContent`)** — At the top of the method, set:
   ```csharp
   SuperViewRendersLineCanvas = true;
   ```
   Then change all references from `Parent?.LineCanvas` / `Parent.LineCanvas` to `this.LineCanvas` / `LineCanvas`. Border now writes to its own LineCanvas.

3. **`Border.cs` (`OnDrawingContent`)** — For the title text exclusion, change from `Parent?.LineCanvas.Exclude(...)` to `LineCanvas.Exclude(...)`. Also add `context?.AddDrawnRectangle(titleRect)` to report the title rectangle to the DrawContext for clip exclusion.

**Effect**: Border adds lines to its own LineCanvas → parent's draw loop merges them (via existing `SuperViewRendersLineCanvas` merge in `DoDrawSubViews`) → parent's `DoRenderLineCanvas` renders all lines. The Border's own DrawContext tracks what IT drew (title text).

### 1b. Merge Border LineCanvas in Parent Draw Cycle

**File**: `View.Drawing.cs` (in `Draw` method, after `DoClearViewport`/`DoDrawSubViews` but before `DoRenderLineCanvas`)

**Change**: Add a merge step for Border's LineCanvas:

```csharp
if (Border is { SuperViewRendersLineCanvas: true })
{
    LineCanvas.Merge (Border.LineCanvas);
    Border.LineCanvas.Clear ();
}
```

This must happen **after** `ClearNeedsDraw` (which clears our LineCanvas) but **before** `DoRenderLineCanvas` — so border lines participate in line-join resolution with any lines SubViews added.

**Note**: The existing merge in `DoDrawSubViews` handles SubViews with `SuperViewRendersLineCanvas`, but Border is an adornment (drawn via `DrawAdornments`), not a SubView. The merge for Border needs its own explicit step.

### 1c. Make Border Participate in Clip Exclusion When Transparent

**File**: `View.Drawing.cs` (`DoDrawComplete` method)

**Change**: Relax the adornment guard. The current code:
```csharp
if (this is not Adornment) { /* clip exclusion */ }
```

Must become:
```csharp
if (this is Adornment && !ViewportSettings.HasFlag(ViewportSettingsFlags.Transparent))
{
    return; // Opaque adornments skip — their parent handles them.
}
```

This lets transparent adornments participate in the drawn-region clip exclusion path.

### 1d. Prevent Viewport Clearing for Views with Transparent Border

**File**: `View.Drawing.cs` (`DoClearViewport` method)

**Change**: Skip clearing if the view's Border has `Transparent` set:

```csharp
if (ViewportSettings.HasFlag(ViewportSettingsFlags.Transparent)
    || (Border is { } && Border.ViewportSettings.HasFlag(ViewportSettingsFlags.Transparent)))
{
    return; // Don't clear — transparent content should show through.
}

ClearViewport(context);
OnClearedViewport();
ClearedViewport?.Invoke(this, new DrawEventArgs(Viewport, Viewport, null));
```

### 1e. "Effectively Transparent" Concept in Clip Exclusion

**File**: `View.Drawing.cs` (`DoDrawComplete`)

A view is treated as "effectively transparent" if:
- It has `ViewportSettingsFlags.Transparent` set directly, **OR**
- Its `Border` has `Transparent` set (the border interior should show through).

In both cases, the **transparent path** is used for clip exclusion:
- Only actually-drawn cells are excluded from `Driver.Clip`.
- Padding is always excluded (it draws fills that fully occupy its thickness area).
- Border is NOT excluded from clip if it's transparent — its drawn cells are already in the context's drawn region via LineCanvas rendering.
- The drawn region is clamped to the Border's frame (for transparent Border) or the Viewport (for directly-transparent views).

**Opaque path** (default, unchanged):
- Exclude the entire view frame (Border frame inward). Margin is never included here.
- Record the rectangle in DrawContext so the SuperView knows what area this subview occupied.

### 1f. Pass DrawContext Through to Adornments

**File**: `View.Drawing.cs`

**Changes**:
1. `DoDrawAdornments` signature: add `DrawContext? drawContext` parameter.
2. `DrawAdornments` signature: add `DrawContext? drawContext` parameter.
3. Pass `drawContext` through to `Margin?.Draw(drawContext)`, `Border?.Draw(drawContext)`, `Padding?.Draw(drawContext)`.

This enables adornments to receive and use the DrawContext for region tracking.

---

## Phase 2: Per-Cell TransparentMouse Hit-Testing

### 2a. Add `CachedDrawnRegion` Property to View

**File**: `View.Drawing.cs`

```csharp
/// Gets the cached drawn region from the last draw pass. Populated during
/// DoDrawComplete for views with TransparentMouse set. Used by mouse
/// hit-testing to determine which cells should receive mouse events.
/// Returns null if not drawn yet or TransparentMouse not set.
/// Invalidated by SetNeedsDraw().
internal Region? CachedDrawnRegion { get; set; }
```

### 2b. Add `_lastLineCanvasRegion` to Track LineCanvas Cells

**File**: `View.Drawing.cs` (in `DoRenderLineCanvas`)

```csharp
private Region? _lastLineCanvasRegion;
```

After rendering the line canvas and reporting to context, cache the region:
```csharp
_lastLineCanvasRegion = cellMap.Count > 0 ? lineRegion : null;
```

This captures the exact cells where lines were rendered, including merged Border lines.

### 2c. Cache Drawn Region in `DoDrawComplete`

**File**: `View.Drawing.cs` (`DoDrawComplete`)

**Before** clip exclusion (so we get fine-grained drawn regions, not the full frame rectangle), add caching logic for adornments and the view itself:

**For adornments** (only when `this is not Adornment` — i.e., from the parent view's perspective):

```csharp
// Border: Build from LineCanvas region intersected with Border frame + title rect
if (Border is { } && Border.ViewportSettings.HasFlag(TransparentMouse))
{
    Region borderDrawnRegion = new();

    if (_lastLineCanvasRegion is { })
    {
        Region lineRegion = _lastLineCanvasRegion.Clone();
        lineRegion.Intersect(Border.FrameToScreen());
        borderDrawnRegion.Union(lineRegion);
    }

    if (Border.LastTitleRect is { } titleRect)
    {
        borderDrawnRegion.Union(titleRect);
    }

    Border.CachedDrawnRegion = borderDrawnRegion;
}

// Margin: Build from context drawn region intersected with Margin frame
if (Margin is { } && Margin.ViewportSettings.HasFlag(TransparentMouse))
{
    Region marginDrawnRegion = context.GetDrawnRegion().Clone();
    marginDrawnRegion.Intersect(Margin.FrameToScreen());
    Margin.CachedDrawnRegion = marginDrawnRegion;
}

// Padding: Same approach as Margin
if (Padding is { } && Padding.ViewportSettings.HasFlag(TransparentMouse))
{
    Region paddingDrawnRegion = context.GetDrawnRegion().Clone();
    paddingDrawnRegion.Intersect(Padding.FrameToScreen());
    Padding.CachedDrawnRegion = paddingDrawnRegion;
}
```

**For the view itself** (after clip exclusion):
```csharp
if (ViewportSettings.HasFlag(TransparentMouse))
{
    if (isEffectivelyTransparent)
    {
        CachedDrawnRegion = context!.GetDrawnRegion().Clone();
    }
    else
    {
        // Opaque view with TransparentMouse — cache the entire border frame.
        Rectangle frame = Border is { } ? Border.FrameToScreen() : FrameToScreen();
        CachedDrawnRegion = new Region(frame);
    }
}
```

### 2d. Add `LastTitleRect` to Border

**File**: `Border.cs`

```csharp
/// Gets the screen-coordinate rectangle of the title text from the last draw pass.
/// Used by the parent view to build CachedDrawnRegion for TransparentMouse hit-testing.
internal Rectangle? LastTitleRect { get; set; }
```

Set it in `OnDrawingContent` after drawing the title:
```csharp
LastTitleRect = titleRect;
```

This is needed because the title is drawn directly (not via LineCanvas), so it won't appear in `_lastLineCanvasRegion`.

### 2e. Invalidate Cache in `SetNeedsDraw`

**File**: `View.NeedsDraw.cs` (at the start of `SetNeedsDraw(Rectangle)`)

```csharp
// Invalidate the cached drawn region for TransparentMouse hit-testing.
CachedDrawnRegion = null;
```

### 2f. Report Drawn Region in ShadowView

**File**: `ShadowView.cs` (at the end of `OnDrawingContent`)

```csharp
context?.AddDrawnRectangle(ViewportToScreen(Viewport));
```

This ensures shadow cells are included in the Margin's CachedDrawnRegion.

### 2g. Build Margin CachedDrawnRegion in `DrawShadows`

**File**: `Margin.cs` (in `DrawShadows`, after drawing and clearing clip)

Since Margin with shadows is drawn in a separate pass (not through the normal `Draw` flow), we build its `CachedDrawnRegion` here by iterating visible SubViews (the ShadowViews):

```csharp
if (margin.ViewportSettings.HasFlag(TransparentMouse))
{
    Region marginDrawnRegion = new();
    foreach (View subView in margin.InternalSubViews)
    {
        if (subView.Visible)
        {
            marginDrawnRegion.Union(subView.FrameToScreen());
        }
    }
    margin.CachedDrawnRegion = marginDrawnRegion.IsEmpty() ? null : marginDrawnRegion;
}
```

### 2h. Modify Hit-Testing in `GetViewsUnderLocation`

**File**: `View.Layout.cs` (in `GetViewsUnderLocation`)

**Stage 1 — Adornment filtering**: For each adornment (Margin, Border, Padding), change from blanket removal to per-cell check:

```csharp
if (viewsUnderLocation.Contains(v.Margin) && v.Margin!.ViewportSettings.HasFlag(excludeFlags))
{
    if (v.Margin.CachedDrawnRegion is null
        || !v.Margin.CachedDrawnRegion.Contains(location.X, location.Y))
    {
        ret = true;
    }
}
// Same pattern for Border and Padding
```

**Stage 2 — Direct view filtering**: Change from `RemoveAll(v => v!.ViewportSettings.HasFlag(...))` to:

```csharp
viewsUnderLocation.RemoveAll(v =>
{
    if (!v!.ViewportSettings.HasFlag(excludeFlags))
        return false;

    if (v.CachedDrawnRegion is { } drawnRegion)
        return !drawnRegion.Contains(location.X, location.Y);

    return true; // null cache = blanket removal (fallback)
});
```

**Note**: `screenLocation` is an `in` parameter, so it must be captured into a local variable for use inside the lambda.

---

## Rename: `DrawMargins` → `DrawShadows`

**File**: `Margin.cs`, `View.Drawing.cs`

The method `DrawMargins` is renamed to `DrawShadows` because it only draws Margins that have shadows. All call sites are updated. Comments referencing "Transparent margins" are updated to reference "shadows".

---

## Files Changed Summary

| File | Changes |
|------|---------|
| `Adornment.cs` | Remove `SuperViewRendersLineCanvas` throw; make it a standard auto-property |
| `Border.cs` | Write to own `LineCanvas`; set `SuperViewRendersLineCanvas = true`; add `LastTitleRect`; report title to DrawContext |
| `View.Drawing.cs` | Add `CachedDrawnRegion`; add `_lastLineCanvasRegion`; merge Border LineCanvas; relax adornment guard in `DoDrawComplete`; cache drawn regions; skip viewport clear for transparent Border; pass `DrawContext` to adornments |
| `View.NeedsDraw.cs` | Clear `CachedDrawnRegion` in `SetNeedsDraw` |
| `View.Layout.cs` | Per-cell hit-testing in `GetViewsUnderLocation` |
| `ShadowView.cs` | Report drawn region to DrawContext |
| `Margin.cs` | Rename `DrawMargins`→`DrawShadows`; build `CachedDrawnRegion` for shadow Margins |
| `Padding.cs` | *(Minor early-return refactoring only — no transparency-specific changes)* |
| `View.Adornments.cs` | *(Pass-through only — `DrawAdornments` signature gains `DrawContext?` parameter)* |

---

## Edge Cases

| Case | Expected Behavior |
|------|-------------------|
| Before first draw | `CachedDrawnRegion` is `null`; TransparentMouse falls back to blanket removal (no regression) |
| View never reports drawn regions | Same — null cache, blanket behavior |
| Stale cache between draws | Invalidated by `SetNeedsDraw`; between draws, cache reflects last visual state |
| Layout changes | Trigger `SetNeedsDraw`, which clears cache |
| Border with `Thickness = 0` | No drawn region, fully transparent to mouse |
| Border with no title and no lines | Empty drawn region — all mouse events pass through |
| Thick border (e.g., `Thickness(2)`) | Only the actual line cells are drawn; gap cells between lines pass through |
| Opaque view with `TransparentMouse` | Cache is the entire border frame (all cells receive clicks) |
| Transparent adornment drawn without context (Margin via `DrawShadows`) | Cannot participate in clip exclusion — early return |

---

## Tests

Two new test files were created (can be copied as-is from this branch):

### `Tests/UnitTestsParallelizable/ViewBase/Adornment/BorderTransparentTests.cs`

Tests for Border visual transparency and TransparentMouse:

| Test | What It Proves |
|------|----------------|
| `Border_Transparent_Shows_Underlying_Content_In_Interior` | Interior of transparent border shows underlying view content |
| `Border_TransparentMouse_Interior_Clicks_Pass_Through` | Mouse clicks on empty interior pass through |
| `Border_TransparentMouse_BorderLine_Clicks_Are_Captured` | Mouse clicks on border lines are received by the border |
| `Border_TransparentMouse_Title_Clicks_Are_Captured` | Mouse clicks on title text are received by the border |
| `View_TransparentMouse_DrawnCells_Captured_UndrawnCells_PassThrough` | Per-cell hit-testing works for regular views |
| `View_TransparentMouse_NullCache_FallsBackToBlanketRemoval` | Null cache = blanket fallback (backward compat) |
| `Border_TransparentMouse_ThickBorder_EmptyCells_PassThrough` | Thick border gap cells pass through |
| `Margin_TransparentMouse_Shadow_Clicks_Are_Captured` | Shadow cells in Margin receive clicks |

### `Tests/UnitTestsParallelizable/ViewBase/Draw/DoDrawCompleteTests.cs`

Baseline tests for the clip exclusion logic in `DoDrawComplete`:

| Test | What It Proves |
|------|----------------|
| `OpaqueView_ExcludesEntireFrameFromClip` | Opaque view punches out its entire borderFrame |
| `OpaqueView_UsesBorderFrameNotViewFrame` | Uses `Border.FrameToScreen()` when Border exists |
| `OpaqueView_UpdatesDrawContext` | Opaque view calls `context.AddDrawnRectangle(borderFrame)` |
| `TransparentView_ExcludesOnlyDrawnRegion` | Transparent view excludes only drawn cells |
| `TransparentView_ClampsDrawnRegionToViewport` | Drawn region is clipped to Viewport bounds |
| `TransparentView_ExcludesBorderAndPadding` | Border/Padding thickness areas always excluded for transparent views |
| `Adornment_SkipsClipExclusion` | Opaque adornments skip clip exclusion |
| `TransparentParent_OpaqueChild_ContextFlows` | Opaque subview's frame added to parent's DrawContext |
| `CachedDrawnRegion_PopulatedAfterDraw_WhenTransparentMouse` | Cache populated after Draw() |
| `CachedDrawnRegion_Null_WhenNotTransparentMouse` | No caching without flag |
| `CachedDrawnRegion_TransparentView_ContainsOnlyDrawnCells` | Only drawn cells cached |
| `CachedDrawnRegion_BorderAdornment_PopulatedAfterDraw` | Border adornment gets cache |
| `CachedDrawnRegion_Border_IncludesTitleAndLines` | Cache has lines + title |
| `CachedDrawnRegion_ClearedBySetNeedsDraw` | SetNeedsDraw invalidates cache |
| `CachedDrawnRegion_RepopulatedAfterRedraw` | Full invalidate-redraw cycle |
| `ShadowView_ReportsDrawnRegionToContext` | Shadow cells in DrawContext |

### Other modified test files (incidental changes)

These files had changes in the branch but primarily for adapting to signature changes or refactoring. Review before copying:

- `Tests/UnitTestsParallelizable/ViewBase/Adornment/AdornmentTests.cs`
- `Tests/UnitTestsParallelizable/ViewBase/Adornment/MarginTests.cs`
- `Tests/UnitTestsParallelizable/ViewBase/Draw/StaticDrawTests.cs`
- `Tests/UnitTestsParallelizable/ViewBase/Draw/ViewDrawingFlowTests.cs`
- `Tests/UnitTests/View/Adornment/BorderTests.cs`
- `Tests/UnitTests/View/Adornment/AdornmentTests.cs`
- `Tests/UnitTests/View/Draw/TransparentTests.cs`

---

## Code Style Changes (Non-Functional)

The branch also made several code style fixes that are **not related to transparency** but will appear in the diff. These are independent refactorings applied to files being modified:

1. **Early-return refactoring** — Nested `if/else` blocks converted to guard clauses (in `Margin.cs`, `Padding.cs`, `Adornment.cs`, `View.Drawing.cs`)
2. **Target-typed `new`** — `new()` replaced with `new TypeName()` per project conventions (throughout `Border.cs`, `Margin.cs`, `Adornment.cs`)
3. **`field` keyword** — Backing field `_shadowSize` replaced with C# 12 `field` keyword (in `Margin.cs`, `View.Adornments.cs`)
4. **Null-conditional simplification** — `if (x is { }) { x.Visible = true; }` → `x?.Visible = true` (in `Margin.cs`)
5. **Removed unused `ClearFrame` method** (in `View.Drawing.cs`)

These should be applied in a separate commit or alongside the transparency work.

---

## Verification Checklist

1. `dotnet build --no-restore` — zero errors, zero new warnings
2. `dotnet test --project Tests/UnitTestsParallelizable --no-build` — all pass
3. `dotnet test --project Tests/UnitTests --no-build` — all pass
4. `Border_Transparent_Shows_Underlying_Content_In_Interior` — passes (interior shows underlying content)
5. `Border_TransparentMouse_Interior_Clicks_Pass_Through` — passes (correct reason: per-cell, not blanket)
6. `Border_TransparentMouse_BorderLine_Clicks_Are_Captured` — passes
7. UICatalog `Transparent` scenario — transparent views still work
8. UICatalog scenarios with borders/windows — no visual regressions
