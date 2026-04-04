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

1. Create `ITitleView.cs` with interface + `TabLayoutContext`
2. Add `TabSide`, `BorderThickness`, `TabDepth` to TitleView; implement `ITitleView`
3. Move 3 static geometry methods from BorderView → TitleView
4. Move layout body into `TitleView.UpdateLayout`, including depth > 3 padding fix
5. Replace `UpdateTitleViewLayout` in BorderView with context construction + cast call
6. Simplify `ConfigureForTabMode` — set TabSide/thickness, remove redundant BorderStyle/Orientation
7. Update `DrawTabBorder` to call `TitleView.ComputeHeaderRect (...)` etc.
8. Update `Top_Focused_Depth5_WithTitle` and `Top_Focused_Depth5_With2LineTitle` tests:
   uncomment the "should be" assertions, remove the wrong ones
9. Build + run tests

## Verification

```bash
dotnet build --verbosity quiet
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-class "*TabsTests"
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-class "*TabCompositionTests"
dotnet test --project Tests/UnitTestsParallelizable --no-build --filter-method "*Depth5*"
```
