# Fix DropDownList Scrolling Bug: Blank Top Rows After Scroll

## Problem Statement

When a `DropDownList` popover list is scrolled down, the first `Viewport.Y` rows of the
visible area are blank. For example, scrolling down by 5 produces 5 blank rows at the top
of the list, while items at the bottom render correctly.

**Test:** `Scrolling_TallDropdown_TopItemsDraw` in `DropDownListTests.cs` (commit d56ba80)

**Observed output** (screen 30x10, scrolled to Viewport.Y=5):
```
Row 0: 'Item_00▼  '   <- dropdown button (correct)
Row 1: '       ▲  '   <- BLANK + scrollbar (should show Item_05)
Row 2: '       ░  '   <- BLANK + scrollbar (should show Item_06)
Row 3: '       ░  '   <- BLANK + scrollbar (should show Item_07)
Row 4: '       █  '   <- BLANK + scrollbar (should show Item_08)
Row 5: '       █  '   <- BLANK + scrollbar (should show Item_09)
Row 6: 'Item_10█  '   <- correct
Row 7: 'Item_11░  '   <- correct
Row 8: 'Item_12░  '   <- correct
Row 9: 'Item_13▼  '   <- correct
```

Note: A plain `ListView` scrolls correctly. The bug is specific to `ListView` inside a
`Popover` (via `DropDownList`).

## Architecture Context

### Key Components

1. **DropDownList** (`Terminal.Gui/Views/DropDownList.cs:131-150`)
   - Creates a `ListView` with `Dim.Auto(Content)` height (capped by screen space)
   - Sets `ViewportSettings = HasVerticalScrollBar`
   - Wraps it in `Popover<ListView, string?>`

2. **Popover** (`Terminal.Gui/App/Popovers/PopoverImpl.cs:77`)
   - Sets `ViewportSettings = Transparent | TransparentMouse` (required for popovers)
   - Fills the screen (`Width = Dim.Fill(), Height = Dim.Fill()`)
   - ContentView (the ListView) is positioned via `SetPosition()`

3. **ScrollBar** (`Terminal.Gui/ViewBase/View.ScrollBars.cs:84`)
   - Added as a SubView of `Padding.View` (NOT a direct SubView of ListView)
   - `Padding.Thickness.Right += 1` when scrollbar becomes visible
   - Draws during `DoDrawAdornments` and `DoDrawAdornmentsSubViews` phases

4. **LayoutAndDraw** (`Terminal.Gui/App/ApplicationImpl.Screen.cs:68-71`)
   - **Every frame**, calls `SetNeedsDraw()` AND `SetNeedsLayout()` on the visible popover
   - Popover is inserted at index 0 in the views list (draws first)
   - Draw order: Popover first, then Runnable views

### Draw Pipeline (View.Drawing.cs:83-178)

```
View.Draw():
  [1] originalClip = GetClip()
  [2] DoDrawAdornments(originalClip)     <- draws Border/Padding (ScrollBar draws here)
  [3] SetClip(originalClip)              <- restore
  [4] originalClip = AddViewportToClip() <- clip to viewport for content
  [5] DoClearViewport()                  <- fills viewport with spaces
  [6] DoDrawSubViews()                   <- draws child views
  [7] DoDrawText()
  [8] DoDrawContent()                    <- ListView.OnDrawingContent() renders items
  [9] SetClip(originalClip)              <- restore pre-viewport
  [10] AddFrameToClip()
  [11] DoRenderLineCanvas()
  [12] DoDrawAdornmentsSubViews()        <- re-draws padding SubViews (ScrollBar)
  [13] SetClip(originalClip)             <- restore
  [14] DoDrawComplete()                  <- ExcludeFromClip for this view
```

### ListView Content Rendering (ListView.Drawing.cs:6-154)

```csharp
int item = Viewport.Y;  // = 5 (scroll position)
for (var row = 0; row < Viewport.Height; row++, item++)
{
    Move (0, row);  // viewport-relative -> screen coords via ViewportToScreen
    Source.Render (..., item, ...);  // renders via AddStr
}
```

For row=0: `Move(0, 0)` -> screen (0, 1). Item 5 should render here. **BLANK.**
For row=5: `Move(0, 5)` -> screen (0, 6). Item 10 renders here. **CORRECT.**

## Confirmed Root Cause: Runnable's DoClearViewport overwrites ListView content

**Diagnostic evidence** (from enhanced test output):

| Capture Point | Rows 1-5 | Rows 6-9 |
|---|---|---|
| After Popover DrawComplete | Item_05-09 (CORRECT) | Item_10-13 (CORRECT) |
| After Runnable ClearedViewport | BLANK (OVERWRITTEN) | Item_10-13 (survived) |
| Final buffer | BLANK | Item_10-13 |

**Key finding:** `Driver.Clip` after the Popover's `DoDrawComplete` is `{X=0,Y=0,Width=30,Height=10}`
(the FULL SCREEN). The Popover's transparent `DoDrawComplete` completely FAILED to exclude the
ListView area from the clip. The Runnable then drew over it.

The partial survival of rows 6-9 indicates the clip exclusion is partially working - only the
top `Viewport.Y` rows are unprotected.

### Why the transparent DoDrawComplete fails

The Popover uses the **transparent path** in `DoDrawComplete` (View.Drawing.cs:928-1038):

```csharp
// viewTransparent = true for Popover
if (!viewTransparent)
{
    exclusion.Combine (ViewportToScreen (Viewport), RegionOp.Union);
    // ^^^ SKIPPED because Popover is transparent
}

// Instead, only context-tracked drawn regions are excluded:
if (context is { })
{
    Region contentDrawn = context.GetDrawnRegion ().Clone ();
    contentDrawn.Intersect (Border.FrameToScreen ());
    exclusion.Combine (contentDrawn, RegionOp.Union);
}
```

The context's `GetDrawnRegion()` should include the ListView's full frame (added by the
ListView's `DoDrawComplete` via `context.AddDrawnRectangle(fullFrame)`). But the clip bounds
after DrawComplete show NO exclusion happened at all, meaning the context's drawn region is
empty or the exclusion logic has a bug.

**Most likely issue:** The DrawContext is being reset or the drawn region is not accumulating
correctly through the SubView draw chain. The context is passed from Popover.Draw →
DoDrawSubViews → ListView.Draw, but something in the pipeline may be creating a new context
or losing the accumulated regions.

Alternatively, the clip at line 166 (`SetClip(originalClip)`) restores the clip to the
pre-frame state, which may undo the exclusion done during the if-block. Then `DoDrawComplete`
runs at line 171, but if it starts from the full-screen clip, its exclusion may not have the
right base to work from.

## Fix Strategy

### Primary fix: Ensure the Popover's DoDrawComplete correctly excludes SubView drawn areas

1. **Trace the DrawContext accumulation** through the Popover draw pipeline to find where
   the ListView's `AddDrawnRectangle` gets lost.

2. **Check if `SetClip(originalClip)` at line 166 resets the clip to full screen** before
   DoDrawComplete runs. If so, DoDrawComplete needs to use the context's drawn region
   (not the current Driver.Clip) for its exclusion.

3. **Verify context sharing**: Ensure the same DrawContext is passed from Popover.Draw
   through DoDrawSubViews to ListView.Draw and back.

### Alternative approaches:

1. **Uncomment `ClearViewport` context tracking** (line 469 in View.Drawing.cs):
   ```csharp
   context.AddDrawnRectangle (toClear);  // currently commented out
   ```
   This would add the cleared viewport area to the context, protecting it from overwrites.

2. **Fix the clip exclusion for opaque SubViews of transparent views**: The Popover is
   transparent but contains an opaque ListView. The DoDrawComplete should still exclude
   the opaque SubView's full frame from the clip, even when the parent is transparent.

3. **Stop calling `SetNeedsLayout()` on popovers every frame** (ApplicationImpl.Screen.cs:71).
   While this doesn't directly cause the clip issue, it forces unnecessary re-layout and
   may exacerbate timing-related bugs.

## Verification

1. `Scrolling_TallDropdown_TopItemsDraw` passes
2. Existing scrollbar tests pass (run `*ScrollBar*` and `*DropDown*` tests)
3. The UICatalog DropDownList scenario works visually (manual check)
4. No regression in other Popover functionality

## Files Likely Modified

| File | Reason |
|------|--------|
| `Terminal.Gui/App/ApplicationImpl.Screen.cs` | Remove/conditionalize `SetNeedsLayout()` |
| `Terminal.Gui/ViewBase/View.Drawing.cs` | Fix DoDrawComplete exclusion or ClearViewport context tracking |
| `Terminal.Gui/ViewBase/View.ScrollBars.cs` | Guard against Padding.Thickness double-adjust |
| `Tests/.../DropDownListTests.cs` | Update/enhance test |
