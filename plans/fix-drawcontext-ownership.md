# Fix DrawContext Ownership: Per-View Tracking for CachedDrawnRegion

## Problem Statement

`DrawContext` serves two purposes that have conflicting ownership requirements:

1. **Clip exclusion** (upward communication): "Tell my SuperView what I drew so peer views
   don't overwrite me." This requires a **shared** context that accumulates across the
   entire hierarchy — SuperView, SubViews, peers — so `DoDrawComplete` can exclude the
   correct aggregate region from the clip.

2. **TransparentMouse hit-testing** (per-view identity): "Which cells did THIS specific
   view draw?" This requires a **per-view** context that tracks only what one view
   rendered, isolated from its SuperView's background fill and its peers' content.

Today, a single shared `DrawContext` is passed through the entire draw tree. This conflates
the two purposes. The result: a transparent view's `CachedDrawnRegion` includes cells drawn
by its SuperView (via `ClearViewport`) and by its peer SubViews (via their `DoDrawComplete`),
causing it to capture mouse events on cells it never touched.

### Concrete Failure Scenarios

**Scenario 1 — SuperView ClearViewport leak (the DropDownList bug):**
A Popover (transparent) contains a ListView (opaque). The Runnable behind it clears its
full viewport into the shared context. After the Popover's `DoDrawComplete`, the clip is
not properly excluded because `GetDrawnRegion()` includes the Runnable's clear. The
Runnable's next `ClearViewport` overwrites the ListView's content.

**Scenario 2 — Peer SubView leak:**
A Window has an opaque Button and a transparent overlay (TransparentMouse). The Button's
`DoDrawComplete` adds its frame to the shared context. The overlay's `CachedDrawnRegion`
picks this up, causing the overlay to capture mouse events over the Button — even though
the overlay drew nothing there.

Both scenarios are covered by existing tests:
- `Scrolling_TallDropdown_TopItemsDraw`
- `TransparentMouse_Overlay_Does_Not_Capture_Peer_SubView_Drawn_Cells`
- `View_TransparentMouse_DrawnCells_Captured_UndrawnCells_PassThrough`

## Why the Current Fix (ClearedRegion) Is Wrong

The current patch adds `_clearedRegion` tracking to `DrawContext` — a parallel bookkeeping
system that tries to distinguish "background fills" from "content draws" so that
`CachedDrawnRegion` can subtract the clears. This is wrong for several reasons:

1. **It doesn't address peer leaking.** The `_clearedRegion` tracks clears, but the peer
   Button's `DoDrawComplete` calls `AddDrawnRectangle` (not `AddClearedRectangle`). The
   only reason the peer test passes today is that the Button's drawn region happens to
   overlap with the cleared region and the `AddDrawnRectangle` call removes it from
   `_clearedRegion`. This is an accidental interaction, not a designed invariant.

2. **It adds conceptual weight without explanatory power.** "Cleared vs drawn" is not a
   distinction that exists in the rendering model. The driver doesn't know the difference.
   The distinction is an artifact of the shared-context problem — if each view tracked its
   own draws, the question would never arise.

3. **It complicates the DrawContext API surface.** `AddDrawnRectangle` now has a side effect
   on `_clearedRegion`. `AddClearedRectangle` writes to two regions. The ordering of
   `CachedDrawnRegion` computation relative to `AddDrawnRegion(exclusion)` in
   `DoDrawComplete` is load-bearing and fragile. Each new call site must understand a
   three-way interaction between `_drawnRegion`, `_clearedRegion`, and calling order.

4. **It violates the Single Responsibility Principle.** `DrawContext` now answers two
   questions ("what was drawn for clip exclusion?" and "what should be excluded from
   hit-testing?") using two coupled data structures with interleaved mutation.

## Root Cause

The root cause is that `Draw()` passes a single `DrawContext` to all phases of drawing:

```
View.Draw(context):            ← context is shared with SuperView
  DoClearViewport(context)     ← SuperView's background fill pollutes context
  DoDrawSubViews(context)      ← each SubView.Draw(context) adds to same context
    SubViewA.Draw(context)     ← SubViewA's DoDrawComplete adds its frame
    SubViewB.Draw(context)     ← SubViewB reads SubViewA's draws via context
  DoDrawComplete(context)      ← reads everything accumulated by all descendants
```

`CachedDrawnRegion` needs to answer: "what did SubViewB itself draw?" But `context`
answers: "what did the SuperView, SubViewA, and SubViewB all draw collectively?"

The adornment system already solved this exact problem correctly. `DrawAdornments()` creates
**per-adornment DrawContexts**:

```csharp
DrawContext paddingContext = new ();       // fresh, isolated context
paddingView.Draw (paddingContext);         // only tracks what padding drew
Padding.LastDrawnRegion = paddingContext.GetDrawnRegion ().Clone ();
```

This is the right pattern. Each adornment gets its own context for draw tracking, and the
result is stored in `LastDrawnRegion` for later use. The view content drawing should follow
the same pattern.

## Design

### Principle

Each view that needs per-view draw tracking (`TransparentMouse` or `Transparent`) gets a
**local DrawContext** that tracks only what that view and its SubViews drew. The **shared
context** (from the SuperView) continues to serve its original purpose: aggregating drawn
regions for clip exclusion.

### Change 1: Introduce a local DrawContext in `Draw()`

In `View.Draw()`, create a local `DrawContext` alongside the shared one. Pass the local
context to the content-drawing phases. Merge the local context into the shared context
afterward.

```
View.Draw(sharedContext):
  DoDrawAdornments(...)
  AddViewportToClip()

  localContext = new DrawContext()         // NEW: per-view tracking

  DoClearViewport(sharedContext)           // clears go to shared (for clip exclusion)
                                          // NOT to local (not "content")
  DoDrawSubViews(sharedContext)           // SubViews use shared for their own clip exclusion
  DoDrawText(localContext)                // text is this view's content
  DoDrawContent(localContext)             // content is this view's content

  // Merge local into shared so SuperView can track what we covered
  sharedContext.AddDrawnRegion(localContext.GetDrawnRegion())

  DoDrawComplete(sharedContext, localContext)
```

### Change 2: Use `localContext` for `CachedDrawnRegion`

In `DoDrawComplete`, the `CachedDrawnRegion` for transparent views is computed from the
local context — which contains only what this view drew (text + content), not what the
SuperView cleared or what peers drew.

```csharp
if (viewTransparent || borderTransparent)
{
    CachedDrawnRegion = localContext?.GetDrawnRegion ();
}
```

No cleared-region subtraction needed. No ordering dependency. No side effects.

### Change 3: Remove `_clearedRegion` from DrawContext

`AddClearedRectangle`, `GetClearedRegion`, and `_clearedRegion` are deleted. `ClearViewport`
goes back to calling `AddDrawnRectangle` on the shared context (or a dedicated method — see
below). `DrawContext` returns to having a single responsibility.

### Change 4: Handle SubView draws in the shared context

SubViews still call `Draw(sharedContext)`. Their `DoDrawComplete` still adds their frames to
the shared context (for clip exclusion). This is correct — the shared context needs to know
about all SubViews so the Popover's `DoDrawComplete` can exclude the aggregate area.

The key insight: SubViews' contributions to the shared context do NOT leak into
`CachedDrawnRegion` because `CachedDrawnRegion` now reads from `localContext`, not
`sharedContext`.

### Change 5: Pass local context to SubViews for THEIR CachedDrawnRegion

Each SubView's `Draw()` creates its own `localContext`. So SubViewA's `CachedDrawnRegion`
comes from SubViewA's `localContext`, and SubViewB's from SubViewB's `localContext`. They
don't interfere.

## Detailed Implementation

### `View.Draw()` changes

```csharp
public void Draw (DrawContext? context = null)
{
    if (!CanBeVisible (this)) return;

    Region? originalClip = GetClip ();

    if (NeedsDraw || SubViewNeedsDraw)
    {
        DoDrawAdornments (originalClip);
        SetClip (originalClip);
        originalClip = AddViewportToClip ();

        context ??= new DrawContext ();

        // Per-view context for this view's own content draws.
        // Used for CachedDrawnRegion (TransparentMouse hit-testing).
        DrawContext localContext = new ();

        DoClearViewport (context);           // shared — protects from overwrites
        DoDrawSubViews (context);            // shared — SubViews manage their own locals
        DoDrawText (localContext);           // local — this view's content
        DoDrawContent (localContext);        // local — this view's content

        // Merge local draws into shared context for SuperView clip exclusion tracking
        context.AddDrawnRegion (localContext.GetDrawnRegion ());

        // Store the local context for DoDrawComplete to use for CachedDrawnRegion
        _localDrawContext = localContext;

        SetClip (originalClip);
        originalClip = AddFrameToClip ();
        DoRenderLineCanvas (context);
        DoDrawAdornmentsSubViews (context);
        ClearNeedsDraw ();
    }

    (Margin.View as MarginView)?.CacheClip ();
    SetClip (originalClip);
    DoDrawComplete (context);
}
```

### `DoDrawComplete()` changes

```csharp
// In the CachedDrawnRegion section:
if (viewTransparent || borderTransparent)
{
    CachedDrawnRegion = _localDrawContext?.GetDrawnRegion ();
}
```

No subtraction. No cleared-region tracking. No ordering sensitivity.

### `DrawContext` changes

Remove `_clearedRegion`, `AddClearedRectangle`, `GetClearedRegion`. The class returns to
its original single-purpose design.

### `ClearViewport` changes

```csharp
Driver.FillRect (toClear);
context?.AddDrawnRectangle (toClear);    // back to AddDrawnRectangle on shared context
```

This is safe because `CachedDrawnRegion` no longer reads from the shared context.

## What About DoDrawText?

`DoDrawText` currently receives the shared context. Under this design, it receives the
local context instead. This is correct: text drawn by this view IS this view's content
and should be part of its `CachedDrawnRegion` for hit-testing.

If a transparent view draws text, those cells should capture mouse events. If it doesn't
draw text, those cells should pass through. The local context captures this correctly.

## What About DoRenderLineCanvas / DoDrawAdornmentsSubViews?

These draw adornments (border lines, scrollbar indicators) that are tracked separately via
`LastDrawnRegion` on each adornment. They should continue to use the shared context for
clip exclusion, and their own `LastDrawnRegion` for `CachedDrawnRegion` (which is already
how adornments work via `CacheAdornmentDrawnRegion`). No changes needed.

## Verification

All three tests must pass:

1. `Scrolling_TallDropdown_TopItemsDraw` — ListView in Popover survives Runnable's
   ClearViewport because the shared context correctly tracks the ListView's frame for
   clip exclusion.

2. `View_TransparentMouse_DrawnCells_Captured_UndrawnCells_PassThrough` — Transparent
   view's CachedDrawnRegion contains only the 3x3 drawn block, not the SuperView's
   cleared viewport.

3. `TransparentMouse_Overlay_Does_Not_Capture_Peer_SubView_Drawn_Cells` — Overlay's
   CachedDrawnRegion contains only the 2x2 indicator, not the peer Button's frame.

Plus no regressions in the full parallelizable test suite (15,133 tests).

## Files Modified

| File | Change |
|------|--------|
| `Terminal.Gui/ViewBase/View.Drawing.cs` | Add `_localDrawContext` field. Split `Draw()` to use local vs shared context. Update `DoDrawComplete` to use local context for `CachedDrawnRegion`. |
| `Terminal.Gui/ViewBase/DrawContext.cs` | Remove `_clearedRegion`, `AddClearedRectangle`, `GetClearedRegion`. Revert `AddDrawnRectangle`/`AddDrawnRegion` to not touch `_clearedRegion`. |
| `Terminal.Gui/ViewBase/View.Drawing.cs` (`ClearViewport`) | Revert to `AddDrawnRectangle`. |

## Risk Assessment

**Low risk.** The shared context's behavior is unchanged — it still accumulates all drawn
regions for clip exclusion. The only behavioral change is how `CachedDrawnRegion` is
populated, which affects only views with `TransparentMouse`. The local context is a new
allocation per `Draw()` call, but `DrawContext` is lightweight (one `Region` field) and
short-lived.

The pattern already exists in the codebase (`DrawAdornments` creates per-adornment
contexts). This change extends that proven pattern to view content drawing.
