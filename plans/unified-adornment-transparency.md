# Unified Adornment Transparency Plan

## Problem

`DoDrawComplete` special-cases Border (uses `_lastLineCanvasRegion` + `LastTitleRect`) while
Margin/Padding get a generic `context.GetDrawnRegion()` intersected with their frame. This is
wrong — all three adornment types should be treated identically:

**Any adornment that draws content (text, subviews, line canvas) within its thickness should have
those drawn cells be opaque. Undrawn cells in a transparent adornment should be transparent.**

`LastTitleRect` is just one example of an adornment drawing text. Padding and Margin can draw
text too (`Padding.View.Text = "Pad"`). The current code doesn't handle Padding/Margin text as
opaque cells within a transparent adornment.

### Current Bugs

1. **Padding visual transparency doesn't work** — Transparent Padding fills with spaces instead
   of showing underlying content. Test `Padding_Transparent_Text_Is_Opaque_Over_Peer_SubViews`
   fails: actual shows the underlying window's Text where the padding Text should occlude it.

2. **Border special-casing** — `DoDrawComplete` builds Border's `CachedDrawnRegion` from
   `_lastLineCanvasRegion` + `LastTitleRect`, but Padding/Margin get `context.GetDrawnRegion()`
   intersected with their frame. This asymmetry means Border drawn cells are tracked precisely,
   but Padding/Margin get the cumulative drawn region of ALL layers.

3. **Missing test coverage** — No Padding or Margin visual transparency tests. No Padding or
   Margin TransparentMouse drawn-cell tests.

## Design Principle

Each adornment tracks its OWN drawn region. The parent's `DoDrawComplete` uses each adornment's
individual drawn region uniformly — no special cases for Border vs Padding vs Margin.

## Approach

1. **Each AdornmentView reports its drawn cells** during its own draw cycle. BorderView already
   does this for `LastTitleRect`. The LineCanvas is rendered by the parent (`DoRenderLineCanvas`)
   and stored in `_lastLineCanvasRegion`. Padding/Margin text rendering needs to similarly report
   drawn regions.

2. **DoDrawComplete uses a uniform approach** for all three adornments — no Border special-casing.
   For each transparent adornment, build CachedDrawnRegion the same way.

3. **Per-adornment drawn region tracking** — each AdornmentView (or AdornmentImpl) accumulates
   what it drew. Options:
   - AdornmentView stores a `DrawnRegion` property set during Draw()
   - Parent passes per-adornment DrawContext
   - AdornmentImpl stores the drawn region populated by AdornmentView

## Todos

### Phase A: Fix Padding/Margin visual transparency

- [ ] **A1: Investigate why transparent Padding clears to spaces** — Verify PaddingView respects
  `ViewportSettings.Transparent` in DoClearViewport. If clearing happens in the parent's draw
  cycle (not PaddingView's own Draw), that's the bug.

- [ ] **A2: Fix transparent Padding/Margin to not clear their area** — When
  `Padding.ViewportSettings.Transparent` is set, undrawn cells remain untouched.

- [ ] **A3: Make Padding/Margin text opaque within transparent adornment** — Drawn text cells
  should be excluded from clip, same as Border's title/lines are.

### Phase B: Unify CachedDrawnRegion logic

- [ ] **B1: Remove Border-specific CachedDrawnRegion logic** — Replace the special
  `_lastLineCanvasRegion` + `LastTitleRect` composition with a uniform approach.

- [ ] **B2: Implement per-adornment drawn region tracking** — Each AdornmentView accumulates
  what it drew during Draw(). Parent's DoDrawComplete reads this.

- [ ] **B3: Update DoDrawComplete** — Replace three separate code blocks with one uniform
  helper that processes all three adornments identically.

### Phase C: Comprehensive tests

- [ ] **C1: Padding visual transparency tests**
  - `Padding_Transparent_Undrawn_Cells_Show_Underlying_Content`
  - `Padding_Transparent_Text_Is_Opaque_Over_Peer_SubViews` (existing, currently failing)
  - `Padding_Transparent_SubViews_Are_Opaque`

- [ ] **C2: Margin visual transparency tests**
  - `Margin_Transparent_Undrawn_Cells_Show_Underlying_Content`
  - `Margin_Transparent_Text_Is_Opaque`

- [ ] **C3: Padding TransparentMouse drawn-cell tests**
  - `Padding_TransparentMouse_DrawnText_Clicks_Are_Captured`
  - `Padding_TransparentMouse_UndrawnCells_PassThrough`

- [ ] **C4: Margin TransparentMouse drawn-cell tests**
  - `Margin_TransparentMouse_DrawnText_Clicks_Are_Captured`
  - `Margin_TransparentMouse_UndrawnCells_PassThrough`

- [ ] **C5: Verify Border tests still pass** — Existing 8 Border tests must continue passing.

- [ ] **C6: Cross-adornment test** — All three adornments transparent, each with text. Verify
  each adornment's text is opaque while undrawn cells are transparent.

## Key Code Locations

- `View.Drawing.cs:DoDrawComplete` (~line 846) — clip exclusion + CachedDrawnRegion
- `View.Drawing.cs:DoDrawAdornments` (~line 300) — draws adornment views
- `View.Drawing.cs:DoRenderLineCanvas` (~line 807) — caches `_lastLineCanvasRegion`
- `BorderView.cs:OnDrawingContent` — draws title, reports LastTitleRect
- `PaddingView.cs` / `MarginView.cs` — draw text but don't report drawn regions
- `View.Layout.cs:GetViewsUnderLocation` (~line 1260) — per-cell hit-testing
