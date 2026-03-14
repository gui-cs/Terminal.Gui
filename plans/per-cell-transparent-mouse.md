# Plan: Border Transparent & TransparentMouse Support (Issue #4834)

## Status

| Item | Status |
|------|--------|
| Border transparency failing tests | Done — 2 skipped, 1 passes (wrong reason) |
| DoDrawComplete comment rewrite | Done |
| DoDrawComplete baseline tests | Done — 8 tests, all passing |
| Phase 1: Visual transparency for Border | Not started |
| Phase 2: Drawn-region-aware TransparentMouse | Not started |

## Context

Issue #4834: `Border` should support `ViewportSettingsFlags.Transparent` and `ViewportSettingsFlags.TransparentMouse`. Prerequisite for TabView redesign (#4183), where each Tab's Border renders a small header rectangle and the rest must be transparent.

Border draws **non-rectangular** content — an outline of line segments plus optional title text. The interior is empty:

- **Transparent**: Only border lines and title text are drawn; the empty interior shows underlying views.
- **TransparentMouse**: Clicks on border lines/title go to the Border; clicks on the empty interior pass through.

## Failing Tests

Three tests in `Tests/UnitTestsParallelizable/ViewBase/Adornment/BorderTransparentTests.cs`:

| Test | Status | Why |
|------|--------|-----|
| `Border_Transparent_Shows_Underlying_Content_In_Interior` | **Skip** (not yet implemented) | Border doesn't honor `Transparent` — interior shows spaces, not underlying content |
| `Border_TransparentMouse_Interior_Clicks_Pass_Through` | passes (wrong reason) | Blanket `TransparentMouse` removes Border entirely |
| `Border_TransparentMouse_BorderLine_Clicks_Are_Captured` | **Skip** (not yet implemented) | Blanket `TransparentMouse` removes Border from ALL hits, including border line cells |

## Baseline Tests for DoDrawComplete

Eight tests in `Tests/UnitTestsParallelizable/ViewBase/Draw/DoDrawCompleteTests.cs` — all passing:

| Test | What it verifies |
|------|-----------------|
| `OpaqueView_ExcludesEntireFrameFromClip` | Opaque view punches out its entire borderFrame from Driver.Clip |
| `OpaqueView_UsesBorderFrameNotViewFrame` | When Border exists, uses Border.FrameToScreen() not View.FrameToScreen() |
| `OpaqueView_UpdatesDrawContext` | Opaque view calls `context.AddDrawnRectangle(borderFrame)` so SuperView knows |
| `TransparentView_ExcludesOnlyDrawnRegion` | Transparent view excludes only the actually-drawn cells, not the full frame |
| `TransparentView_ClampsDrawnRegionToViewport` | DrawnRegion is clipped to Viewport bounds before exclusion |
| `TransparentView_ExcludesBorderAndPadding` | Border and Padding thickness areas are always excluded, even for transparent views |
| `Adornment_SkipsClipExclusion` | Adornments (Margin/Border/Padding) do NOT modify Driver.Clip in DoDrawComplete |
| `TransparentParent_OpaqueChild_ContextFlows` | Opaque child's frame is added to parent's DrawContext via AddDrawnRectangle |

## Root Cause Analysis

### Problem 1: Visual Transparency doesn't work for Border

`DoDrawComplete` (View.Drawing.cs:821) has an `if (this is not Adornment)` guard that skips ALL clip exclusion logic for adornments. This means setting `Transparent` on a Border has no effect — the parent view's opaque path excludes the entire border frame from the clip regardless.

### Problem 2: Border draws to Parent.LineCanvas, not its own

Border.cs `OnDrawingContent` writes all border lines to `Parent.LineCanvas` (line 309), not to Border's own LineCanvas. The parent renders these later in its own `DoRenderLineCanvas` (View.Drawing.cs:144), and the drawn region gets reported to the **parent's** DrawContext — not the Border's. So the Border's own DrawContext never knows what cells the border lines occupy.

This was the user's observation: "I suspect [writing to Parent.LineCanvas] was a poor decision back when I did it." The fix should make Border use its own LineCanvas with `SuperViewRendersLineCanvas = true`, so lines are drawn by Border but merged into the parent for rendering. However, `Adornment.SuperViewRendersLineCanvas` currently throws `InvalidOperationException` (Adornment.cs:215-218).

### Problem 3: TransparentMouse is blanket per-view

`GetViewsUnderLocation` (View.Layout.cs:1303) does `RemoveAll(v => v!.ViewportSettings.HasFlag(excludeViewportSettingsFlags))` — blanket removal. No per-cell checking.

## Implementation

### Phase 1: Visual Transparency for Border

#### 1a. Fix Border LineCanvas ownership

**Problem**: Border writes to `Parent.LineCanvas`. This means the drawn region is tracked on the parent, not the Border.

**Fix in `Adornment.cs` (~line 215)**: Change `SuperViewRendersLineCanvas` override to allow setting it (remove the throw). Or better: have Adornment return `Parent?.SuperViewRendersLineCanvas ?? false` — meaning adornments' line canvases get merged into their Parent view's LineCanvas (which is where Border already writes today, but through the proper merge mechanism).

**Fix in `Border.cs` (`OnDrawingContent`, ~line 309)**: Change from writing to `Parent?.LineCanvas` directly to writing to `this.LineCanvas` (Border's own). Set Border's `SuperViewRendersLineCanvas = true` so the parent merges and renders it.

**Fix in `View.Drawing.cs` (`DoDrawSubViews`, ~line 703-718)**: The merge logic at line 715 (`LineCanvas.Merge(view.LineCanvas)`) uses `SuperView.LineCanvas`. For adornments, the "SuperView" in this context is actually the `Parent` view. Need to ensure the merge target is correct — when the view being drawn is an Adornment, merge into `Parent.LineCanvas` instead of `SuperView.LineCanvas`.

This change means:
- Border adds lines to its own LineCanvas
- Border's `RenderLineCanvas` is skipped (because `SuperViewRendersLineCanvas = true`)
- Parent's draw loop merges Border's LineCanvas into its own
- Parent's `RenderLineCanvas` renders all lines and reports the drawn region to the parent's context
- Border's own DrawContext tracks what IT drew (title text, etc.)

#### 1b. Make Border participate in clip exclusion when Transparent

**Fix in `View.Drawing.cs` (`DoDrawComplete`, ~line 821)**: Relax the Adornment guard. When an Adornment has `Transparent` set, it should participate in the drawn-region clip exclusion path:

```csharp
if (this is not Adornment || ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent))
{
    if (ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent))
    {
        // Transparent path: only exclude drawn regions
        context!.ClipDrawnRegion (ViewportToScreen (Viewport));
        ExcludeFromClip (context.GetDrawnRegion ());
    }
    else if (this is not Adornment)
    {
        // Opaque non-adornment path (existing code)
        ...
    }
}
```

#### 1c. Prevent viewport clearing for Transparent Border

When Border has `Transparent` set, its viewport should NOT be cleared during `DoClearViewport`. The existing `Transparent` flag logic in `ClearViewport` (View.Drawing.cs:414) should handle this — but need to verify it works for adornments too.

### Phase 2: Drawn-Region-Aware TransparentMouse

#### 2a. Add `_cachedDrawnRegion` to View (`View.Drawing.cs`)

```csharp
private Region? _cachedDrawnRegion;
internal Region? CachedDrawnRegion => _cachedDrawnRegion;
```

#### 2b. Cache drawn region in `DoDrawComplete` (`View.Drawing.cs` ~line 810)

**Only if `TransparentMouse` is set**:

In the Transparent path (after clip exclusion), cache `context.GetDrawnRegion()`. For adornments with `TransparentMouse`, add caching after the Adornment guard.

For Border specifically, the cached region must include both:
- Lines drawn via LineCanvas (reported to context by `RenderLineCanvas`)
- Title text (reported to context by `DrawText`)

After Phase 1 fixes, the Border's own DrawContext should have the title text region. The LineCanvas region is in the parent's context. We need to ensure the Border's cached region includes both — this may mean the parent passes the line region back to the Border, or the Border caches its region from the parent's context after merge.

**Alternative approach**: Cache the region at the parent level for all adornments. When hit-testing asks "is this point on the Border?", check the parent view's line canvas drawn region intersected with the Border's frame. This avoids the cross-context issue.

**Simplest approach**: After the parent renders its LineCanvas (which includes Border's lines), have the parent set `_cachedDrawnRegion` on its Border adornment by intersecting the line canvas region with the Border's frame. This way the Border's cached region accurately reflects the cells where border lines were drawn.

#### 2c. Invalidate cache in `SetNeedsDraw` (`View.NeedsDraw.cs` ~line 58)

Clear `_cachedDrawnRegion = null` at the start of `SetNeedsDraw(Rectangle)`.

#### 2d. Report drawn region in ShadowView (`ShadowView.cs` ~line 47)

Add `context?.AddDrawnRectangle (ViewportToScreen (Viewport));` at the end of `OnDrawingContent`.

#### 2e. Modify hit-testing (`View.Layout.cs` ~line 1262)

In `GetViewsUnderLocation`, change both filtering stages:

**Stage 1 — Adornment filtering (lines ~1275-1300)**:
```csharp
if (viewsUnderLocation.Contains (v.Margin) && v.Margin!.ViewportSettings.HasFlag (excludeViewportSettingsFlags))
{
    // Per-cell check: if the adornment has a cached drawn region, check it
    if (v.Margin.CachedDrawnRegion is null || !v.Margin.CachedDrawnRegion.Contains (screenLocation.X, screenLocation.Y))
    {
        ret = true;
    }
}
```

Same pattern for Border and Padding checks.

**Stage 2 — Direct view filtering (line ~1303)**:
```csharp
viewsUnderLocation.RemoveAll (v =>
{
    if (!v!.ViewportSettings.HasFlag (excludeViewportSettingsFlags))
    {
        return false;
    }

    if (v.CachedDrawnRegion is { } drawnRegion)
    {
        return !drawnRegion.Contains (screenLocation.X, screenLocation.Y);
    }

    return true; // null cache = blanket removal (fallback)
});
```

## Files to Modify

| File | Change |
|------|--------|
| `Terminal.Gui/ViewBase/Adornment/Adornment.cs` | Allow `SuperViewRendersLineCanvas` to be set (remove throw) |
| `Terminal.Gui/ViewBase/Adornment/Border.cs` | Write to own LineCanvas instead of `Parent.LineCanvas`; set `SuperViewRendersLineCanvas = true` |
| `Terminal.Gui/ViewBase/View.Drawing.cs` | Add `_cachedDrawnRegion`; relax Adornment guard in `DoDrawComplete`; cache drawn region; ensure LineCanvas merge works for adornments |
| `Terminal.Gui/ViewBase/View.NeedsDraw.cs` | Clear `_cachedDrawnRegion` in `SetNeedsDraw` |
| `Terminal.Gui/ViewBase/View.Layout.cs` | Drawn-region check in `GetViewsUnderLocation` |
| `Terminal.Gui/ViewBase/Adornment/ShadowView.cs` | Report drawn region to DrawContext |

## Edge Cases

| Case | Behavior |
|------|----------|
| Before first draw | `CachedDrawnRegion` is null; falls back to blanket `TransparentMouse` (no regression) |
| View doesn't report drawn regions | Same — null cache, blanket behavior |
| Stale cache between draws | Invalidated by `SetNeedsDraw`; between draws, cache reflects last visual state |
| Layout changes | Trigger `SetNeedsDraw`, which clears cache |
| Border with no lines (Thickness=0) | No drawn region, fully transparent to mouse |
| Existing code that reads `Parent.LineCanvas` from Border | Must be updated to use `this.LineCanvas` or verified to still work after merge |

## Verification

1. `dotnet build --no-restore` — zero errors
2. `dotnet test --project Tests/UnitTestsParallelizable --no-build` — no regressions
3. `Border_Transparent_Shows_Underlying_Content_In_Interior` — passes (interior shows underlying content)
4. `Border_TransparentMouse_Interior_Clicks_Pass_Through` — passes (for the right reason now)
5. `Border_TransparentMouse_BorderLine_Clicks_Are_Captured` — passes (border lines receive mouse events)
6. UICatalog `Transparent` scenario — transparent views still work
7. UICatalog scenarios with borders/windows — no visual regressions
