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
│    ╰────┴──╮               ├────┴────┴──╮     
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

### Option B detailed design (refined)

**Key insight from user**: Z-order conflicts only occur with `Arrangement = ViewArrangement.Overlapped`.
Tiled (non-overlapped) views with `SuperViewRendersLineCanvas=true` should continue to use the
flat merge — their borders SHOULD auto-join (e.g., `Line`, `AttributePicker` segments,
`FileDialog` children, side-by-side `FrameView`s).

**This means the change can be scoped to DrawSubViews only, with a simple partition:**

1. **Tiled SubViews** (no `Overlapped` flag): Merge into parent's LineCanvas immediately,
   as today. Auto-join across tiled siblings works correctly.
   
2. **Overlapped SubViews** (`Arrangement.HasFlag(ViewArrangement.Overlapped)`): Each
   resolves its own LineCanvas independently. The resolved cell maps are composited
   in Z-order (painters' algorithm) directly to the output buffer during RenderLineCanvas.

**DrawSubViews algorithm (revised):**

```
DrawSubViews(context):
  // Collect overlapped views' resolved cell maps for deferred compositing
  List<(Dictionary<Point,Cell?>, int zOrder)> overlappedCellMaps = []

  foreach view in InternalSubViews.Reverse():   // reverse Z-order (highest Z last)
    view.Draw(context)
    
    if !view.SuperViewRendersLineCanvas:
      continue

    if view.Arrangement.HasFlag(Overlapped):
      // Resolve independently, defer rendering
      cellMap = view.LineCanvas.GetCellMap()
      overlappedCellMaps.Add((cellMap, zOrder))
      view.LineCanvas.Clear()
    else:
      // Tiled: flat merge into parent LC (preserves auto-join)
      LineCanvas.Merge(view.LineCanvas)
      view.LineCanvas.Clear()

  // Store overlappedCellMaps for use by RenderLineCanvas
  _pendingOverlappedCellMaps = overlappedCellMaps
```

**RenderLineCanvas (revised):**

```
RenderLineCanvas(context):
  // 1. Resolve parent's own LC (includes tiled SubViews' merged lines)
  parentCellMap = LineCanvas.GetCellMap()
  
  // 2. Render parent cell map
  foreach cell in parentCellMap: render(cell)
  
  // 3. Composite overlapped views' cell maps on top, in Z-order (low→high)
  foreach (cellMap, _) in _pendingOverlappedCellMaps:
    foreach cell in cellMap: render(cell)   // overwrites lower-Z cells
  
  LineCanvas.Clear()
  _pendingOverlappedCellMaps = null
```

**Why this works for all cases:**

| Scenario | Arrangement | Merge strategy | Auto-join? |
|----------|------------|----------------|------------|
| Side-by-side FrameViews | Tiled | Flat merge | ✅ Yes — T-junctions at shared borders |
| Line views in a container | Tiled | Flat merge | ✅ Yes — lines join correctly |
| Tab headers (focused/unfocused) | Overlapped | Per-view resolve | ✅ No cross-Z join (correct!) |
| Stacked dialog windows | Overlapped | Per-view resolve | ✅ Front window occludes back |
| Unfocused tabs (same Z-level) | Overlapped | Per-view resolve | ⚠️ No auto-join between unfocused tabs |

**The last row is a concern**: unfocused tabs currently share borders (the `╮` between Tab1 and
Tab2's top). With per-view resolve, these would render as two overlapping `┐` and `┌` instead
of a joined `┬`. However, looking at the expected test output:

```
╭────╮────╮    ← Tab1 focused: ╮ at (5,0) is Tab1's top-right corner meeting Tab2's top-left
│Tab1│Tab2│
│    ╰────┴──╮
```

The `╮` at col 5 row 0 is actually produced by Tab2's top-left corner AND Tab1's top-right
meeting at the same cell. With per-view resolve:
- Tab1 (higher Z) produces `╮` at (5,0)
- Tab2 (lower Z) produces `╭` at (5,0)
- Tab1 wins → `╮` → CORRECT!

And at col 5 row 2:
- Tab1 (higher Z) has NO line (gap)
- Tab2 (lower Z) produces `╰` at (5,2)
- Tab2's glyph shows through the gap → `╰` → CORRECT!

**This is exactly what painters' algorithm produces, and it matches the expected output.**

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
- [x] Created general-purpose tests independent of BorderSettings.Tab
- [ ] Validate Option B against AutoLineJoin tests (thought experiment)
- [ ] Validate Option B against non-Tab scenarios (FrameView nesting, etc.)
- [ ] Detail implementation plan with specific code changes
- [ ] Identify edge cases and risks

## Test Results Summary

### New tests (OverlappedLineCanvasTests.cs)

| Test | Result | What it proves |
|------|--------|---------------|
| `HigherZ_Gap_Not_Filled_By_LowerZ_Border` | **FAIL** | Higher-Z view's sides get ├/┤ junctions from lower-Z view's top line |
| `Overlapped_With_Padding_SubViews_HigherZ_Wins` | **FAIL** | Same bug: at overlap cell (0,1), got ├ instead of │ |
| `Overlapped_Views_HigherZ_Lines_Win_At_Shared_Cells` | PASS (doc) | Renders for design review; confirms ├ at overlap points |
| `Three_Overlapped_Staircase_HighestZ_Dominates` | PASS (doc) | Confirms ┤ junction artifacts in staircase pattern |
| `SameZ_SideBySide_AutoJoin_Still_Works` | **PASS** | Regression guard: non-overlapped peers auto-join correctly |

### Existing tests

| Suite | Result | Notes |
|-------|--------|-------|
| AutoLineJoinTests (20) | All pass | Same-Z auto-join works correctly |
| Border tests (112) | 109 pass, 3 skip | No regressions |
| TabView tests (24) | 21 pass, 3 fail | The 3 Tab rendering tests |

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

## Key Finding: The LineCanvas Merge Chain

Traced via `SuperViewRendersLineCanvas = true` audit:

```
TabTitleView (SuperViewRendersLineCanvas=true)
  → merges into BorderView's LineCanvas
    → BorderView.DrawTabBorder() writes directly to Adornment.Parent.LineCanvas (Tab's LC)
      Tab (SuperViewRendersLineCanvas=true)
        → DrawSubViews merges Tab.LineCanvas into Tabs.LineCanvas
          Tabs.DoRenderLineCanvas() → resolves ALL lines in one pass
```

**All lines from all tabs — focused AND unfocused — end up in one flat LineCanvas.**
The intersection resolver has no way to know that some lines should occlude others.

### Views using SuperViewRendersLineCanvas=true (audit)

| View | File | Purpose |
|------|------|---------|
| Tab | Views/TabView/Tab.cs:28 | Tab headers merge into Tabs container |
| TabTitleView | Adornment/TabTitleView.cs:16 | Tab title text merges into BorderView |
| Line | Views/Line.cs:60 | Line view merges into parent |
| AttributePicker children | Views/Color/AttributePicker.cs:102+ | Color picker segments |
| FileDialog children | Views/FileDialogs/FileDialog.cs:133 | File dialog structure |

**This confirms the issue is general**: Any two overlapped views with `SuperViewRendersLineCanvas=true`
that contribute border lines to the same parent will have the same auto-join conflict.

### No TabRow.cs exists

The `Tabs` class (formerly `TabView`) directly manages tab layout via `UpdateTabOffsets()`
and `UpdateZOrder()`. There is no intermediate `TabRow` container.

## Open Questions

1. **DoDrawAdornmentsSubViews merging**: Currently merges borderView.LineCanvas into
   parent's LC. Under Option B, should this be a separate Z-level or same as parent?
2. **Performance**: Per-view resolution means N resolve passes instead of 1. For most
   views this is trivial (few lines), but need to consider pathological cases.
3. **Same-Z auto-join**: Unfocused tabs adjacent to each other still need shared
   T-junctions. Option B's per-view isolation would lose these. Need a grouping strategy.
