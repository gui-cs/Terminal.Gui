# Refactor TitleView Layout Logic from BorderView into TitleView

## Context

TitleView is a lightweight View for rendering tab headers, but all its layout and
configuration logic lives in BorderView (~150 lines). This refactoring:

1. Encapsulates TitleView's layout within TitleView itself
2. Introduces `ITitleView` to enable future replaceability of the tab header view
3. Fixes the depth > 3 rendering bug (depth is currently hard-capped at 3)

**Pre-existing TabsTest failures are known — ignore them.**

## The Depth > 3 Bug

`GetTabDepth` (BorderView:449) caps depth at 3 via `Math.Min (thickness, 3)`.
With Thickness.Top = 5, the tab header should be 5 rows but renders as 3.

Current (wrong) output for `Top_Focused_Depth5_WithTitle` with Thickness(1,5,1,1):
```
                           <- blank (lost row)
╭───╮
│Tab│
│   ╰───╮                  <- join at depth 3, should be at depth 4
│       │
╰───────╯
```

Expected output:
```
╭───╮                      <- cap (row 0)
│Tab│                      <- title (row 1)
│   │                      <- extra padding (row 2)
│   ╰───╮                  <- content join (row 3)
│       │
╰───────╯
```

### Root Cause

1. `GetTabDepth` caps at 3 — remove the cap
2. `ComputeTitleViewThickness` only produces 0 or 1 for cap/contentSide — works fine for
   the border lines, but extra depth rows need to come from TitleView's **Padding**
3. `UpdateTitleViewLayout` padding logic (lines 221-232) only handles focused Bottom/Right
   — needs to handle all sides and compute padding from `depth - (cap + titleRows + contentSide)`

### Fix

- Remove `Math.Min (thickness, 3)` from `GetTabDepth` (becomes a simple property)
- In `UpdateLayout`, compute inner padding to fill extra depth:
  `int extraRows = Math.Max (0, tabDepth - 2 - contentSide)` (2 = cap + title row)
  Apply as padding on the content side of TitleView

---

## Files to Modify

- `Terminal.Gui/ViewBase/Adornment/TitleView.cs` — receives layout logic, implements ITitleView
- `Terminal.Gui/ViewBase/Adornment/BorderView.cs` — slims down, uses ITitleView
- New: `Terminal.Gui/ViewBase/Adornment/ITitleView.cs` — interface + TabLayoutContext

## ITitleView Interface

```csharp
public interface ITitleView : IOrientation
{
    int TabDepth { get; }
    Side TabSide { get; set; }
    Thickness BorderThickness { get; set; }
    void UpdateLayout (in TabLayoutContext context);
}
```

BorderView holds `_titleView` as `View?`, casts to `ITitleView` for layout.

## TabLayoutContext

Only values TitleView cannot derive from its own stored state:

```csharp
internal readonly record struct TabLayoutContext
{
    public required Rectangle BorderBounds { get; init; }
    public required int TabOffset { get; init; }
    public required int? TabLength { get; init; }
    public required bool HasFocus { get; init; }
    public required LineStyle? LineStyle { get; init; }
    public required string Title { get; init; }
    public required Point ScreenOrigin { get; init; }
}
```

## TabDepth as Computed Property

TitleView stores `TabSide` and `BorderThickness`. `TabDepth` is computed (NO cap):

```csharp
public int TabDepth => TabSide switch
{
    Side.Top => BorderThickness.Top,
    Side.Bottom => BorderThickness.Bottom,
    Side.Left => BorderThickness.Left,
    Side.Right => BorderThickness.Right,
    _ => 3
};
```

## What Moves to TitleView

1. **Static geometry methods** (`internal static`):
   - `ComputeHeaderRect`
   - `ComputeViewBounds`
   - `ComputeTitleViewThickness`

2. **Layout logic** — `UpdateLayout (in TabLayoutContext)` (current `UpdateTitleViewLayout` body)

3. **Padding computation** — new logic to handle depth > 3 by adding padding rows

## What Stays in BorderView

- `_titleView` field + `TitleView` property + `EnsureTitleView()`
- `IsFocusedOrLastTab()` — queries parent/Tabs hierarchy
- `GetTabBorderBounds()` — used by drawing and as input to UpdateLayout
- `ConfigureForTabMode()` — ViewportSettings flags, EnsureTitleView, sets TabSide/thickness
- All drawing: `DrawTabBorder`, `AddTabSideContentBorder`
- `DrawTabBorder` calls `TitleView.ComputeHeaderRect (...)` etc.

## Implementation Steps

- [x] 1. Create `ITitleView.cs` with interface + `TabLayoutContext`
- [x] 2. Add `TabSide`, `BorderThickness`, `TabDepth` to TitleView; implement `ITitleView`
- [x] 3. Move 3 static geometry methods from BorderView → TitleView
- [x] 4. Move layout body into `TitleView.UpdateLayout`, including depth > 3 padding fix
- [x] 5. Replace `UpdateTitleViewLayout` in BorderView with context construction + cast call
- [x] 6. Simplify `ConfigureForTabMode` — set TabSide/thickness, remove redundant BorderStyle/Orientation
- [x] 7. Update `DrawTabBorder` to call `TitleView.ComputeHeaderRect (...)` etc.
- [x] 8. Fix depth > 3 test expectations (Depth4, Depth5, ThickBorder — all 4 sides)
- [ ] 9. Write new ITitleView / refactoring tests (see test plan below)
- [x] 10. Build + run all tests — 0 regressions (14 pre-existing TabsTests failures unchanged)

## Test Plan

### New tests for ITitleView contract (in `Tests/UnitTestsParallelizable/ViewBase/Adornment/`)

#### ITitleView property tests
- [ ] `TabDepth_Computed_FromSideAndThickness_Top` — set TabSide=Top, BorderThickness=(1,5,1,1), assert TabDepth==5
- [ ] `TabDepth_Computed_FromSideAndThickness_Left` — TabSide=Left, BorderThickness=(4,1,1,1), assert TabDepth==4
- [ ] `TabSide_Set_UpdatesTabDepth` — change TabSide, verify TabDepth recomputes
- [ ] `BorderThickness_Set_UpdatesTabDepth` — change thickness, verify TabDepth recomputes

#### UpdateLayout tests
- [ ] `UpdateLayout_HidesTitleView_WhenBorderBoundsEmpty` — pass zero-size bounds, assert Visible==false
- [ ] `UpdateLayout_HidesTitleView_WhenTabLengthNull` — pass TabLength=null, assert Visible==false
- [ ] `UpdateLayout_SetsTextFromContext` — verify Text matches context.Title
- [ ] `UpdateLayout_SetsOrientation_Horizontal_ForTopSide` — TabSide=Top → Orientation.Horizontal
- [ ] `UpdateLayout_SetsOrientation_Vertical_ForLeftSide` — TabSide=Left → Orientation.Vertical
- [ ] `UpdateLayout_SetsBorderThickness_ForDepth3` — verify ComputeTitleViewThickness output applied
- [ ] `UpdateLayout_SetsPadding_ForDepth5` — verify extra padding rows for depth > 3

#### Static geometry method tests (moved from BorderView, verify still work)
- [ ] `ComputeHeaderRect_Top_ReturnsCorrectRect`
- [ ] `ComputeHeaderRect_Left_ReturnsCorrectRect`
- [ ] `ComputeViewBounds_Top_IncludesHeaderProtrusion`
- [ ] `ComputeTitleViewThickness_Depth2_HasCap_NoContentSide`
- [ ] `ComputeTitleViewThickness_Depth3_Unfocused_HasContentSide`
- [ ] `ComputeTitleViewThickness_Depth3_Focused_NoContentSide`

#### Depth > 3 rendering tests (fix existing + add new)
- [ ] `Top_Focused_Depth5_WithTitle` — uncomment "should be" assertion
- [ ] `Top_Focused_Depth5_With2LineTitle` — uncomment "should be" assertion
- [ ] `Top_Focused_Depth4_WithTitle` — new: verify depth 4 renders correctly
- [ ] `Bottom_Focused_Depth5_WithTitle` — new: verify bottom side depth > 3

### Existing tests that must still pass
- All `TabsTests` (filter-class `*TabsTests`)
- All `TabCompositionTests` (filter-class `*TabCompositionTests`)
- All `BorderViewTests` (filter-class `*BorderViewTests`)

## Verification

```bash
dotnet build --verbosity quiet
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-class "*TabsTests"
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-class "*TabCompositionTests"
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-class "*BorderViewTests"
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-method "*Depth5*"
```

---

## Status

**Current:** Core refactoring complete. New ITitleView tests (step 9) still pending.

## Progress Log

1. Created `ITitleView.cs` — had to make `TabLayoutContext` `public` (not `internal`) because `ITitleView` is public
2. Moved static geometry methods and layout body to TitleView
3. BorderView property `TitleView` shadows class name `TitleView` — used `using TitleViewType = ...` alias for static calls in `DrawTabBorder`
4. Removing `Math.Min(thickness, 3)` cap fixed depth > 3 rendering automatically
5. Updated 10 test expectations for depth 4/5/thick border — all now show correct extra padding rows
6. Verified 0 regressions: same 14 pre-existing TabsTests failures, all BorderViewTests and TabCompositionTests pass

## Lessons Learned

- Property names that shadow type names require using aliases for static member access
- The depth cap removal was the simplest part — just not adding `Math.Min` in the computed property
- Extra depth rows are handled naturally by TitleView's `Frame` being taller — padding fills the space between title text and the content-side border
