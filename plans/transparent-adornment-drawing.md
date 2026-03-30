# Transparent Adornment Drawing — Design Plan

## Problem Statement

When SubViews of Adornments (e.g., Tab headers inside a Border) draw their LineCanvas lines,
the current merge strategy (`LineCanvas.Merge`) has no way to respect Z-order occlusion.
The attempted fix — `Merge(LineCanvas, Region? exclude)` — clips incoming lines against a
"prior drawn region," but this **fragments lines before intersection resolution**, breaking
auto-line-join (e.g., T-junctions and corners become gaps or wrong glyphs).

### Concrete symptom

3 TabView tests fail (`Top_TwoTabs_Tab1Focused_DrawsCorrectly`, etc.):
- Expected: focused tab's bottom border is **open** (gap merges seamlessly with content border)
- Actual: auto-join produces **closed** T-junctions/crosses because the exclude-based merge
  fragments the lines that would otherwise connect.

### Why the exclude approach is fundamentally flawed

`Merge(LineCanvas, Region? exclude)` splits a StraightLine into sub-segments before adding
them to the target canvas. The intersection resolver then sees disconnected short segments
instead of one continuous line, so it cannot produce correct junction glyphs. This is a
**structural** problem — no tuning of the exclude region fixes it.

## Deep Trace: What Happens During Tab Drawing

### View hierarchy for a TabView with 2 tabs (Tab1 focused)

```
Tabs (the container view)
  ├─ Tab1 (focused) — SuperViewRendersLineCanvas = true, Arrangement = Overlapped
  │    └─ Border (Adornment, Settings = Tab | Title)
  │         └─ BorderView
  │              └─ TabTitleView ("Tab1") — SuperViewRendersLineCanvas = true
  └─ Tab2 (unfocused) — SuperViewRendersLineCanvas = true, Arrangement = Overlapped
       └─ Border (Adornment, Settings = Tab | Title)
            └─ BorderView
                 └─ TabTitleView ("Tab2") — SuperViewRendersLineCanvas = true
```

### Drawing flow for the Tabs container

1. **DoDrawAdornments** — Tabs itself has a Border. `DrawTabBorder()` is called on each
   Tab's BorderView during `OnDrawingContent`. This adds content-border lines (3 sides +
   gap segments on the tab-side) directly to `Adornment.Parent.LineCanvas` (= Tabs.LineCanvas).

2. **DoClearViewport** — Clears the content area.

3. **DoDrawSubViews** — Iterates SubViews in reverse Z-order (unfocused tabs first, then focused).
   For each Tab:
   - `tab.Draw(context)` — This triggers the Tab's own draw, which calls its BorderView's
     `DrawTabBorder()`, adding lines to Tab's LineCanvas.
   - `LineCanvas.Merge(tab.LineCanvas)` — Tab's lines merge into Tabs' LineCanvas.
   
   **THE PROBLEM**: Both Tab1 and Tab2's border lines are merged into the same LineCanvas.
   The unfocused tab (Tab2) draws a full top line across its header. The focused tab (Tab1)
   draws gap segments (split around the open area). When all these lines are in the same
   LineCanvas, intersection resolution sees all of them and produces T-junctions at the
   gap boundaries instead of the expected corners (╮/╰).

4. **DoDrawAdornmentsSubViews** — Processes Border's SubViews (TabTitleView). These have
   `SuperViewRendersLineCanvas = true`, so their LineCanvas is merged into borderView's
   LineCanvas, then into Tabs' LineCanvas.

5. **DoRenderLineCanvas** — Final resolution of all merged lines. This is where the wrong
   glyphs appear.

### Expected vs Actual (Tab1 focused, 14x5)

```
Expected:                    Actual:
╭────╮────╮                  ╭────┬────╮        
│Tab1│Tab2│                  │Tab1│Tab2│        
│    ╰────┴──╮               ├────┤────┴──╮     
│Tab1 content│               │Tab1 content│     
╰────────────╯               ╰────────────╯     
```

Position (5,0): Expected `╮` (top-right of focused tab), got `┬` (T-junction from both tabs' lines)
Position (0,2): Expected `│` (continuation of left side), got `├` (T-junction)
Position (5,2): Expected `╰` (bottom-right of focused tab, connecting to gap), got `┤`

The auto-join resolver sees lines from BOTH tabs meeting at these cells and produces
junctions. But the focused tab's gap should mean its bottom-left corner merges
seamlessly — the unfocused tab's top line should NOT contribute at the gap cells.

### Root cause: single flat LineCanvas with no Z-awareness

All lines from all tabs end up in one LineCanvas. The resolver has no concept of
"this line belongs to the focused tab and should take priority" vs "this line belongs
to an unfocused tab and should be occluded."

## Current Drawing Pipeline (View.Draw)

```
1. DoDrawAdornments        — Border + Padding draw their own lines
2. AddViewportToClip
3. DoClearViewport          — clear content area
4. DoDrawSubViews           — SubViews drawn in reverse Z (high-Z first)
   └─ foreach subview:
      ├─ view.Draw(context)
      └─ LineCanvas.Merge(view.LineCanvas)   ← THIS is where the problem is
5. context.AddDrawnRectangle(clearedRect)
6. DoDrawText / DoDrawContent
7. DoDrawAdornmentsSubViews — Adornment SubViews (title labels, etc.)
   └─ borderView.DoDrawSubViews()
   └─ LineCanvas.Merge(borderView.LineCanvas) — merges TabTitleView lines
8. DoRenderLineCanvas       — final resolve + render
9. DoDrawComplete           — clip exclusion for transparency
```

Key insight: Steps 4 and 7 both merge SubView line canvases into the SuperView's
LineCanvas. The merge is order-dependent but has no Z-aware occlusion mechanism
that preserves line continuity.

## What Needs to Happen (High-Level)

The core issue: **two views at different Z-levels contribute lines to the same
LineCanvas, and the lower-Z view's lines must not appear where the higher-Z view
already drew.** But we can't fragment lines pre-merge because that breaks auto-join.

### Design Options

#### Option A: Post-resolve cell-level exclusion ❌ REJECTED
- Merge ALL lines, resolve, then remove occluded cells
- **Rejected**: Wrong junction glyphs would be computed at boundaries because the resolver
  saw all lines. Removing cells after resolution leaves artifacts.

#### Option B: Per-Z-level LineCanvas compositing ⭐ RECOMMENDED
- Each Z-level (or each view) gets its own LineCanvas
- Lines within the same Z-level auto-join correctly
- Final compositing: render lower-Z first, then higher-Z overwrites
- Adjacent same-Z views that need shared junctions can share a canvas
- **Pro**: Each view's auto-join is self-consistent; compositing is trivial and correct
- **Con**: Cross-Z auto-join is lost — but this is actually CORRECT behavior!
  (A focused tab's gap should NOT auto-join with an unfocused tab's border)

#### Option C: Tagged lines with priority-aware resolution
- Each StraightLine carries a Z-order tag
- During intersection resolution, if lines from different Z-levels meet at a cell,
  only the highest-Z lines participate in glyph selection
- **Pro**: Preserves cross-Z auto-join where desired; correct occlusion
- **Con**: Significant change to intersection resolver; complex semantics

#### Option D: Hybrid — merge all, resolve, then patch boundaries ❌ REJECTED
- Too complex; multiple resolution passes; fragile boundary detection

### Why Option B is the right answer

The key insight from tracing the test failures: **cross-Z auto-join is the bug, not a feature.**

When Tab1 (focused) leaves a gap in its bottom border, and Tab2 (unfocused) draws a full
top line that passes through that gap area, we DON'T want auto-join to create a T-junction
there. The focused tab's gap is intentional visual design — it should show "open" to connect
the tab header to the content area below.

Option B naturally produces this: Tab2's lines resolve independently (full border), Tab1's
lines resolve independently (border with gap). Tab1 renders on top, overwriting Tab2's
cells where they overlap. The gap remains open because Tab1 simply has no line there.

This aligns with how painters' algorithm works in 2D graphics — and it's the correct
mental model for overlapped views.

### Option B detailed design

**Phase 1: Per-view LineCanvas isolation during DrawSubViews**

Instead of merging each SubView's LineCanvas into the SuperView's immediately:
1. Each SubView draws normally (populating its own LineCanvas)
2. Resolve each SubView's LineCanvas independently → cell maps
3. Render cell maps in Z-order (back to front), with higher-Z overwriting lower-Z
4. The SuperView's OWN LineCanvas (from DoDrawAdornments) is treated as the lowest Z

**Phase 2: Same-Z shared canvas (for cross-view auto-join within same Z)**

Views at the same Z-level that need auto-join (e.g., unfocused tabs adjacent to each
other) can share a canvas. This is an optimization that preserves correct junctions
between peer views.

**Phase 3: Adornment SubView integration**

`DoDrawAdornmentsSubViews` currently merges TabTitleView's lines into the parent's
LineCanvas. Under Option B, the TabTitleView's lines should merge into its containing
Tab's per-view canvas (same Z-level), not into the top-level Tabs' canvas directly.

### Implementation approach

The change is primarily in `DrawSubViews` and `DoRenderLineCanvas`:

```
DrawSubViews:
  - Don't merge SubView LCs into parent LC immediately
  - Instead, collect per-SubView resolved cell maps in Z-order
  
DoRenderLineCanvas:
  - Resolve parent's own LC → base cell map
  - Composite SubView cell maps on top in Z-order
  - Render the final composite
```

This keeps the change localized to the draw pipeline without touching LineCanvas
intersection resolution at all.

## Investigation Status

- [x] Identified 3 failing TabView tests
- [x] AutoLineJoin tests all pass (20/20) — confirms the base merge is fine
- [x] Border tests all pass (109/109, 3 skipped)
- [x] Read DrawSubViews and the `#if PRIOR_DRAWN_REGION` code
- [x] Read `Merge(LineCanvas, Region?)` — confirmed fragmentation issue
- [x] Study BorderView drawing (how Tab borders are composed)
- [x] Study Tab drawing pipeline and SuperViewRendersLineCanvas flow
- [x] Study how adornment SubViews' LineCanvas lines flow to SuperView
- [x] Trace a specific failing test to understand exact line composition
- [x] Evaluate design options against all test cases
- [ ] Validate Option B against AutoLineJoin tests (thought experiment)
- [ ] Validate Option B against non-Tab scenarios (FrameView nesting, etc.)
- [ ] Detail implementation plan with specific code changes
- [ ] Identify edge cases and risks

## Amazon PE Tenets Applied

1. **Understand deeply before proposing** — Traced the full drawing pipeline cell-by-cell
   through a failing test to understand exactly which lines produce which wrong glyphs.
2. **Insist on the highest standards** — Reject fragmentation hacks and post-hoc patching;
   design a solution rooted in correct compositing semantics (painters' algorithm).
3. **Think big** — Option B is a general compositing model, not a Tab-specific fix.
   It correctly handles ANY overlapped views with merged line canvases.
4. **Earn trust** — AutoLineJoin tests verify same-Z auto-join still works. Existing
   tests with non-overlapped views won't change behavior.
5. **Simplify** — Per-view resolution + painters' compositing is a well-understood model.
   No new abstractions in LineCanvas itself — changes are in the draw pipeline only.

## Files of Interest

- `Terminal.Gui/ViewBase/View.Drawing.cs` — Main draw pipeline, DrawSubViews, RenderLineCanvas
- `Terminal.Gui/Drawing/LineCanvas/LineCanvas.cs` — Merge methods, GetCellMap, GetCellMapWithRegion
- `Terminal.Gui/ViewBase/Adornment/BorderView.cs` — Border drawing, DrawTabBorder
- `Terminal.Gui/ViewBase/Adornment/TabTitleView.cs` — Tab title rendering
- `Terminal.Gui/Views/TabView/Tab.cs` — Tab view with SuperViewRendersLineCanvas
- `Tests/UnitTestsParallelizable/Views/TabView/TabsTests.cs` — 3 failing tests
- `Tests/UnitTestsParallelizable/Drawing/AutoLineJoinTests.cs` — Auto-join regression guard

## Open Questions

1. **Which views use SuperViewRendersLineCanvas?** Need to audit all usages to ensure
   Option B doesn't break non-Tab scenarios (e.g., FrameViews inside FrameViews).
2. **DoDrawAdornmentsSubViews merging**: Currently merges borderView.LineCanvas into
   parent's LC. Under Option B, should this be a separate Z-level or same as parent?
3. **Performance**: Per-view resolution means N resolve passes instead of 1. For most
   views this is trivial (few lines), but need to consider pathological cases.
