# TextView Scroll Performance Fix

## Problem Statement

The `ConfigurationEditor` UICatalog scenario scrolls significantly slower than the `Editor` scenario, even though both use `TextView` with `WordWrap = false`. The root cause is a set of O(N×L) operations in `TextView` that run on every scroll/layout cycle, where N = total lines and L = average line length.

### Why ConfigurationEditor is worse than Editor

| Factor | Editor | ConfigurationEditor |
|--------|--------|---------------------|
| View hierarchy | 1 level (`Window → TV`) | 3 levels (`Window → Tabs → Tab → TV`) |
| Layout cascades | Infrequent | Frequent (deeper nesting → more `OnSubViewsLaidOut` calls) |
| `ReadOnly` | `false` | `true` (extra per-cell work in drawing) |
| `ContentsChanged` handler | Lightweight status bar update | Calls `IsDirty` → O(all cells) comparison |

Both scenarios suffer from the same core `TextView` bottlenecks, but ConfigurationEditor triggers the expensive paths more frequently due to deeper view hierarchy causing more layout passes.

---

## Root Causes (ordered by impact)

### RC1: `GetMaxVisibleLine()` scans ALL lines on every content-changed scroll

**File:** `TextModel.cs:65-82`, called from `UpdateContentSize()` (`TextView.Scrolling.cs:49-66`)

```csharp
int contentWidth = WordWrap ? Viewport.Width : _model.GetMaxVisibleLine (0, _model.Count, TabWidth);
```

`GetMaxVisibleLine` iterates every line in the model, calling `CellsToStringList()` + `CursorColumn()` on each. This is **O(N×L)** per call. It runs whenever `contentMayHaveChanged` is true (which includes `NeedsDraw`).

**Fix:** Cache the max visible line width in `TextModel`. Invalidate only when lines are added, removed, or modified — not on scroll.

### RC2: `OnSubViewsLaidOut` calls `WrapTextModel()` + `UpdateContentSize()` unconditionally

**File:** `TextView.cs:157-166`

```csharp
protected override void OnSubViewsLaidOut (LayoutEventArgs args)
{
    base.OnSubViewsLaidOut (args);
    WrapTextModel ();       // O(N×L) full rewrap even if width unchanged
    UpdateContentSize ();   // O(N×L) full line scan (see RC1)
}
```

**Fix:** Guard `WrapTextModel()` with a width-change check. Guard `UpdateContentSize()` with a dirty flag — only recalculate when content actually changed.

### RC3: `CellsToStringList()` allocates a new `List<string>` on every call

**File:** `TextModel.cs:578-588`

Called per-line in drawing, per-line in `GetMaxVisibleLine`, in `AdjustViewport`, in `PositionCursor`. Creates GC pressure from thousands of small allocations per frame.

**Fix:** This is a deeper refactor (caching or span-based). Defer to a follow-up. Focus on eliminating the call sites via RC1/RC2 fixes first.

### RC4: `AdjustViewport()` computes 3 redundant `DisplaySize`/`CursorColumn` calls

**File:** `TextView.Scrolling.cs:91-94`

Three separate calls each allocate a list and scan the current line. These could be consolidated.

**Fix:** Compute once, reuse results. Lower priority since it's O(L) not O(N×L).

### RC5: `PositionCursor()` called 2-4× per position change

`CurrentRow` and `CurrentColumn` setters each call `PositionCursor()`. When both change (common), it runs twice. `SetWrapModel`/`UpdateWrapModel` pairs trigger 4 calls total.

**Fix:** Use backing fields directly and call `PositionCursor()` once. Lower priority.

---

## Implementation Plan

### Phase 1: Add passing tests for existing behavior

Add tests that verify current correct behavior of the code paths we'll be modifying. These tests MUST pass before and after our changes.

**File:** `Tests/UnitTestsParallelizable/Views/TextViewPerformanceTests.cs`

Tests to add:
1. `GetMaxVisibleLine_Returns_Correct_Width_For_Mixed_Lines` — verifies `GetMaxVisibleLine` returns the width of the longest line
2. `UpdateContentSize_Sets_Correct_Width_And_Height` — verifies content size reflects model dimensions
3. `OnSubViewsLaidOut_Calls_UpdateContentSize` — verifies content size is updated after layout
4. `ContentSize_Correct_After_Text_Change` — verifies content size updates when text changes
5. `ContentSize_Correct_After_Line_Append` — verifies content size updates when lines are appended programmatically
6. `WrapTextModel_Only_Wraps_When_WordWrap_Enabled` — verifies `WrapTextModel()` is a no-op when `WordWrap = false`
7. `Viewport_Scrolling_Does_Not_Change_ContentSize_When_Content_Unchanged` — verifies that pure scrolling (changing Viewport.Y) does not alter content size

### Phase 2: Add failing tests that expose the performance issue

These tests document the expected optimized behavior and will FAIL against current code.

**File:** `Tests/UnitTestsParallelizable/Views/TextViewPerformanceTests.cs`

Tests to add:
1. `GetMaxVisibleLine_Is_Not_Called_On_Pure_Scroll` — instruments/counts calls; verifies `GetMaxVisibleLine` is NOT called when viewport changes without content changes
2. `UpdateContentSize_Skips_GetMaxVisibleLine_When_Content_Unchanged` — verifies that scrolling without editing doesn't trigger the full line scan
3. `OnSubViewsLaidOut_Skips_WrapTextModel_When_Width_Unchanged` — verifies that `WrapTextModel` is not called when viewport width hasn't changed
4. `ContentSize_Width_Cached_Between_Scrolls` — verifies content size width remains stable between scroll operations without recalculating

### Phase 3: Implement the performance fixes

#### Fix 1: Cache max line width in TextModel

**File:** `TextModel.cs`

- Add `_cachedMaxWidth` field and `_maxWidthDirty` flag
- `GetMaxVisibleLine()` returns cached value when not dirty
- Invalidate cache in `AddLine()`, `RemoveLine()`, `ReplaceLine()`, `LoadString()`, `LoadFile()`, and any method that modifies `_lines`
- On single-line edits, do incremental update: compare new line width vs cached max

#### Fix 2: Guard `OnSubViewsLaidOut` with width-change detection

**File:** `TextView.cs`

- Add `_lastWrapWidth` field
- In `OnSubViewsLaidOut`, only call `WrapTextModel()` if `Viewport.Width != _lastWrapWidth`
- Only call `UpdateContentSize()` if a dirty flag is set (content changed or wrap changed)
- Update `_lastWrapWidth` after wrapping

#### Fix 3: Add content-dirty flag to skip `UpdateContentSize` on scroll

**File:** `TextView.Scrolling.cs`

- Add `_contentSizeDirty` flag
- Set it to `true` in all text-modification code paths (where `OnContentsChanged` is called, in `LoadString`, etc.)
- In `UpdateContentSize()`, early-return if `!_contentSizeDirty`
- Reset flag after recalculating
- Always set dirty on first use (`!Used`)

---

## Verification Steps

1. All existing tests in `TextViewScrollingTests`, `TextViewTests`, `TextModelTests`, and other `TextView*Tests` must continue to pass
2. Phase 1 tests pass before AND after changes
3. Phase 2 tests fail before changes, pass after
4. Build with no new warnings
5. Manual verification: open ConfigurationEditor scenario and scroll — should be noticeably faster
6. Manual verification: open Editor scenario — should be at least as fast as before
7. Manual verification: edit text in both scenarios — content size must still update correctly

---

## Files Modified

| File | Change |
|------|--------|
| `Terminal.Gui/Views/TextInput/TextModel.cs` | Add max-width caching with dirty flag |
| `Terminal.Gui/Views/TextInput/TextView/TextView.cs` | Guard `OnSubViewsLaidOut` with width check |
| `Terminal.Gui/Views/TextInput/TextView/TextView.Scrolling.cs` | Add content-dirty flag, skip redundant `UpdateContentSize` |
| `Tests/UnitTestsParallelizable/Views/TextViewPerformanceTests.cs` | New test file with Phase 1 + Phase 2 tests |

## Out of Scope (follow-up)

- `CellsToStringList` allocation optimization (needs deeper refactor)
- `AdjustViewport` consolidation of redundant `DisplaySize` calls
- `PositionCursor` batching
- `IsDirty` O(N) optimization (only affects editing, not scrolling)
- `GetModelColFromWrappedLines` linear scan per-cell during word-wrap drawing
