# New TabView — Design Plan

## Overview

Build `Tab` and `Tabs` View subclasses that leverage the **tab-style border rendering** built into `Border` (see [Borders Deep Dive](../docfx/docs/borders.md)). Each `Tab` is a View whose `Border.Settings = BorderSettings.Tab | BorderSettings.Title`; the `Tabs` container computes `TabOffset` for each `Tab` so headers sit side-by-side. `LineCanvas` auto-join produces flowing connected tab styles with zero manual line drawing:

```
╭────┬────╮
│Tab1│Tab2│
│    ╰────┴───────╮
│content for Tab1 │
╰─────────────────╯
```

### `Tab`

```cs
public class Tab : View
{
    public Tab ()
    {
        CanFocus = true;
        SuperViewRendersLineCanvas = true;
        Border.Settings = BorderSettings.Tab | BorderSettings.Title;

        // Overlapped enables z-order: focused tab renders above unselected tabs
        Arrangement = ViewArrangement.Overlapped;
    }  

    // gets the logical index of the tab, determined by Tabs when the Tab is added as a SubView
    // updated by Tabs OnSubViewAdded/Removed to maintain correct indices as tabs are added/removed
    public int TabIndex {get; internal set;}

    // gets or sets the side the tab header is on (top, bottom, left, right)
    // when part of a Tabs view, this is set by Tabs when the Tab is added as a SubView, and Tabs will update the Border.Thickness of all Tabs accordingly
    public Side TabSide { get; set; } 
}
```


### `Tabs`

```cs
public class Tabs : View, IValue<Tab?>, IDesignable
{
    public Tabs ()
    {
        CanFocus = true;
        Width = Din.Fill();
        Height = Dim.Fill();
    }

    // Tabs are SubViews of Tabs, retrieved via SubViews.OfType<Tab>()

 

    // TabSide determines which side the tab headers are on and which Border thickness to use
    // the set handler for TabSide will update the Border.Thickness of all Tabs accordingly
    public Side TabSide { get; set; } = Side.Top;

    // IValue<Tab?> implementation
    public Tab? Value
    {
        get => SubViews.OfType<Tab> ().FirstOrDefault (t => t.HasFocus);
        set
        {
            if (Value != value)
            {
                value?.SetFocus ();
                // raise ValueChanged event etc.
            }
        }
    }

    // Gets or sets the line style for the Tabs. When set call UpdateZOrder, UpdateTabOffsets, and UpdateTabBorderThickness to ensure all Tabs are updated with the new style.
    public LineStyle TabLineStyle {get;set;}

    // Gets the tabs in logical order (by TabIndex), which may differ from SubViews order due to z-ordering of focused tab
    public IEnumerable<Tab> Tabs => SubViews.OfType<Tab> ().OrderBy (t => t.TabIndex);

    private void UpdateZOrder()
    {
         // iterate over Tabs (logical order and use MoveTabToStart to order.

         // Then, use MoveTabToEnd for the focused tab to ensure it is drawn last (on top).       
    }

    private void UpdateTabOffsets()
    {
        // iterate over Tabs in logical order and set TabOffset based on TabSide and cumulative widths/heights of previous tabs
        // for Side.Top/Bottom, TabOffset is horizontal (columns from left)
        // for Side.Left/Right, TabOffset is vertical (rows from top)
    }

    private void UpdateTabBorderThickness()
    {
        // iterate over Tabs and set Border.Thickness based on TabSide
        // for Side.Top, set Thickness.Top = 3, others = 1
        // for Side.Bottom, set Thickness.Bottom = 3, others = 1
        // for Side.Left, set Thickness.Left = 3, others = 1
        // for Side.Right, set Thickness.Right = 3, others = 1
    }

    // override OnSubViewAdded/Removed to call UpdateZOrder, UpdateTabOffsets, and UpdateTabBorderThickness whenever a Tab is added or removed
    // OnSubViewAdded should set the TabIndex of the added Tab based on the current Tabs collection (e.g. max existing TabIndex + 1)
    // OnSubViewRemoved shoul set Width and Height to Dim.Fill() 

    // EnableForDesign should setup the tabs the way the TabStyles Scenario does (but using the Tab and Tabs API).


}
```

## Tab Style Renderings by Side

See the TabCompositionTests and BorderViewTests for the definitive look & feel. The draings below are slightly incorrect.

**IMPORTANT**: The drawings below are slightly incorrect. Use the existing tests as the source of truth for the exact visuals. The drawings are meant to illustrate the general layout and mechanics, but the actual line characters at junctions will depend on how `LineCanvas` auto-joins the lines based on the `AddLine` calls made by the `Border` rendering of each `Tab`. The key visual properties to verify in tests are:

### `Side.Top` — `Thickness.Top = 3` - "Tab View" with tabs on top

The `Thickness` on the tab side is 3 (for the 3-row/column header area). The opposite sides use 1 for the content border. The selected tab's header opens toward the content area; unselected tabs are fully closed.

Tab headers along the top edge. Selected tab's header bottom is suppressed; its vertical connectors meet the content top segments and auto-join into junction glyphs. All lines render to the same `LineCanvas` via `SuperViewRendersLineCanvas`.

**Selected = Tab1** (first tab, TabOffset = 0):
```
╭────┬────╮
│Tab1│Tab2│              Tab1's left connector IS the content left border (continuous │)
│    ╰────┴───────╮      Tab1's right connector + content top segment → ╯
│content for T1   │      Tab2 bottom + content top → ┴ at Tab2's right connector
│                 │      Content top right segment ends at right border → ╮
╰─────────────────╯
```

**Selected = Tab2** (second tab, TabOffset = 5):
```
╭────┬────╮
│Tab1│Tab2│              Tab2's connectors meet content top segments
├────╯    ╰───────╮     Tab1 bottom auto-joins with content top left segment
│content for T2   │
│                 │
╰─────────────────╯
```

**Three Tabs with Selected = Tab2** (offsets: Tab1 = 0, Tab2 = 5, Tab3 = 10):
```
╭────┬────┬────╮
│Tab1│Tab2│Tab3│
├────╯    ╰────┴──╮
│content for T2   │
│                 │
╰─────────────────╯
```

**Note:** The exact junction glyphs depend on how `AddLine` calls overlap on the shared `LineCanvas`. The renderings above show the *intent* — actual glyphs will be determined by LineCanvas auto-join resolution during implementation. The important visual properties are:
- Selected tab's header is open at the bottom, flowing into the content area
- Unselected tabs' headers are fully closed
- Adjacent tab headers share edges and auto-join at overlapping columns
- The content top line runs in segments around the selected tab's header gap

### `Side.Bottom` — `Thickness.Bottom = 3` - "Tab View" with tabs on bottom

Mirror of Top. Tab headers along the bottom edge. Selected tab's top line (closest to content) is suppressed.

**Selected = Tab1:**
```
╭─────────────────╮
│content for T1   │
│    ╭────┬───────╯
│Tab1│Tab2│
╰────┴────╯
```

**Selected = Tab2:**
```
╭─────────────────╮
│content for T2   │
├────╮    ╭───────╯
│Tab1│Tab2│
╰────┴────╯
```

Same auto-join mechanics, vertically mirrored:
- `Thickness.Bottom = 3` provides 3 rows at bottom for headers
- Selected tab suppresses its TOP line (the line adjacent to content)
- Content bottom line drawn in segments around the header gap
- Headers overlap by 1 row at shared edges → auto-join produces `┬`/`┴`

### `Side.Left` — `Thickness.Left = 3` - "Tab View" with tabs on left

Tab headers stacked vertically on the left edge. `TabOffset` is vertical (rows from top). Headers overlap by 1 row at shared horizontal edges. Selected tab's RIGHT line (closest to content) is suppressed. Tab text is oriented vertically.

**Selected = Tab1:**
```
╭────────────╮
│T content T1│
│a           │
│b           │
│1           │
├─╮          │
│T│          │
│a│          │
│b│          │
│2│          │
╰─┤          │
  │          │
  ╰──────────╯
```

**Selected = Tab2:**
```
╭─┬──────────╮
│T│content T2│
│a│          │
│b│          │
│1│          │
├─╯          │
│T           │
│a           │
│b           │
│2           │
╰─╮          │
  │          │
  ╰──────────╯
```

How it works:
- `Thickness.Left = 3` provides 3 columns on the left for headers
- `TabOffset` is the vertical row offset for each header
- Headers overlap by 1 row → auto-join at shared horizontal edge → `├` / `┤`
- Selected tab: right side open (no right vertical line on header), content left border drawn in segments
- Unselected tab: right side closed
- Content top/bottom auto-join with header corners

### `Side.Right` — `Thickness.Right = 3` - "Tab View" with tabs on right

Mirror of Left. Tab headers stacked vertically on the right edge. Selected tab's LEFT line (closest to content) is suppressed. Tab text is oriented vertically.

**Selected = Tab1:**
```
╭────────────╮
│content T1 T│
│           a│
│           b│
│           1│
│          ╭─┤
│          │T│
│          │a│
│          │b│
│          │2│
│          ├─╯
│          │
╰──────────╯ 
```

**Selected = Tab2:**
```
╭──────────┬─╮
│content T2│T│
│          │a│
│          │b│
│          │1│
│          ╰─┤
│           T│
│           a│
│           b│
│           2│
│          ╭─╯
│          │ 
╰──────────╯ 
```

Same mechanics as Left, horizontally mirrored.

### Summary: Which Thickness and Which Line Gets Suppressed

| Side | Thickness with value 3 | Selected tab suppresses | TabOffset axis | Content border segments |
|------|----------------------|------------------------|----------------|------------------------|
| Top | `Thickness.Top = 3` | Bottom line of header | Horizontal | Top line of content, split around header |
| Bottom | `Thickness.Bottom = 3` | Top line of header | Horizontal | Bottom line of content, split around header |
| Left | `Thickness.Left = 3` | Right line of header | Vertical | Left line of content, split around header |
| Right | `Thickness.Right = 3` | Left line of header | Vertical | Right line of content, split around header |

For Top/Bottom, the thickness is always 3 (fixed: top line, title row, bottom line). For Left/Right, the thickness equals the widest tab title + 2 (variable, to accommodate title text width).

### Continuation Line

The "continuation line" (from last tab to right edge) is simply the **right segment of the selected tab's content border top line** (step 8 in the rendering section). It starts at the last tab's right edge and extends to the view's right border. LineCanvas auto-joins the endpoint with the right vertical border to produce `╮`.

### Scrolling

When the total width of all tab headers exceeds the `Tabs` container width, headers must scroll. The approach: `Tabs` maintains a `_scrollOffset` that is subtracted from each tab's `TabOffset`. A custom `ScrollBar` in the `Border` adornment drives the offset. The scrollbar's slider is transparent so the content border line shows through.

**Why not Viewport scrolling?** All tab SubViews use `Dim.Fill()` — they share the same Frame. Viewport scrolling shifts ALL SubViews equally, which would move tab *content* along with headers. Only headers should scroll; content must stay put.

#### Scrolling Drawings (`Side.Top`)

Setup: 

- 5 tabs ("Tab1" through "Tab5"), each with `TabLength = 6` (4-char title + 2 border cells). 
- Total Tabs View.Width = 26 columns (including borders).


All 5 tab headers rendered at their natural offsets. Headers overflow the right edge — Tab4 is partially visible, Tab5 is fully clipped by `BorderView`'s `Rectangle.Intersect`.

Step 0: Start

**No scroll (`_scrollOffset = 0`), Tab1 selected, enough room for no scroll indicators**

```
01234567890123456789012345
╭────╮────╮────╮────╮────╮
│Tab1│Tab2│Tab3│Tab4│Tab5│ 
│    ╰────┴────┴────┴────┤
│Tab1 content            │
╰────────────────────────╯
```

Step 1: Reduce Tabs.Width to 28 columns, which reduces visible header area by 2 columns. Tab1 still selected.

**No scroll (`_scrollOffset = 0`), Tab1 selected:**

```
012345678901234567
╭────╮────╮────╮──
│Tab1│Tab2│Tab3│Ta 
│    ╰────┴────┴─►     Right scroll indicator appears, Tab4 clipped, Tab5 off-screen
│Tab2 content    │
╰────────────────╯
```

Step 2: Focus Tab2

```
012345678901234567
╭────╭────╮────╮──
│Tab1│Tab2│Tab3│Ta     Tab4 clipped, Tab5 off-screen
├────╯    ╰────┴─►
│Tab2 content    │
╰────────────────╯
```

Step 3: Scroll right 1 column (`_scrollOffset = 1`), Tab2 still selected:

**Scrolled right total of 1 column, Tab2 still selected:**

```
012345678901234567
────╭────╮────╮───
Tab1│Tab2│Tab3│Tab     Both scroll indicators appear, Tab1 clipped, Tab4 cliped, Tab5 off-screen
◄───╯    ╰────┴──►
Tab2 content     │
─────────────────╯
```


Step 4: Scroll right 4 more columns (`_scrollOffset = 5`), Tab2 still selected:

**Scrolled right total of 5 columns, Tab2 still selected:**

```
012345678901234567
╭────╮────╮────╮──
│Tab2│Tab3│Tab4│Ta    Tab1 off screen, tab5 clipped
◄    ╰────┴────┴─►
│Tab2 content    │
╰────────────────╯
```

Step 5: Scroll right 1 more columns (`_scrollOffset = 6`), Tab2 still selected:

**Scrolled right total of 6 columns, Tab2 still selected:**

```
012345678901234567
────╮────╮────╮───
Tab2│Tab3│Tab4│Tab    Tab1 off screen, tab2 clipped, tab5 clipped
◄   ╰────┴────┴──►
Tab2 content     │
─────────────────╯
```

Step 6: Select Tab 4 

**Scrolled right total of 6 columns, Tab4 still selected:**

```
012345678901234567
────╭────╭────╮───
Tab2│Tab3│Tab4│Ta    Tab1 off screen, tab2 clipped, tab5 clipped
◄───┴────╯    ╰──►
Tab4 content     │
─────────────────╯
```

Step 7: Reduce width to 24 columns, which reduces visible header area by 3 columns. Tab4 still selected.

**Reduced Tabs.Width by 3:**

```
012345678901234567
────╭────╭────╮
Tab2│Tab3│Tab4│   Tab1 off screen, tab2, tab5 off screen
◄───┴────╯    ►
Tab4 content  │
──────────────╯
```

Step 8: Increase width to 30 columns. Tab4 still selected.

**Increased Tabs.Width by 4 to to 30**

```
01234567890123456789
────╭────╭────╮────╮
Tab2│Tab3│Tab4│Tab5│    Tab1 off screen, tab2, clipped, tab5 clipped
◄───┴────╯    ╰────►
Tab4 content       │
───────────────────╯
```

Step 9: Select Tab1

`EnsureTabVisible(tab1)` sets `_scrollOffset` so Tab1's left edge aligns with the container's left edge.

```
01234567890123456789
╭────╮────╮────╮────
│Tab1│Tab2│Tab3│Tab4 
│    ╰────┴────┴───►   Left scroll indicator disappears, Tab1 fully visible, tab4 clipped, tab5 off screen
│Tab1 content      │
╰──────────────────╯
```

Step 10: Select Tab4

`EnsureTabVisible(tab4)` sets `_scrollOffset` so Tab4's left edge aligns with the container's left edge.

```
01234567890123456789
────╭────╭────╭────╮
Tab1│Tab2│Tab3│Tab4│    Left scroll indicator appears Tab1 clipped, tab5 off screen
◄───┴────╯────╯    ►
Tab4 content       │
───────────────────╯
```

Step 11: Set Width back to original 26 columns, which increases visible header area by 4 columns. Tab4 still selected.
```
01234567890123456789012345
╭────╭────╭────╭────╮────╮
│Tab1│Tab2│Tab3│Tab4│Tab5│ 
├────┴────┴────╯    ╰────┤  No scroll indcators. All visible.
│Tab1 content            │
╰────────────────────────╯
```

The content border line is drawn by `BorderView.DrawTabSideContentBorderLine`. Because the slider is transparent, those line characters show through the scrollbar. The `◄►` buttons appear at the edges, indicating more tabs exist off-screen.

## Implementation Steps

### Step 0: Build a set of ViewBase tests that use nothing but View or test-defined test sub-classes to prove the basic concepts:

1. Two Views arragned to look like this:

```
╭────┬────╮
│Tab1│Tab2│
│    ╰────┴───────╮
│content for Tab1 │
╰─────────────────╯
```

2. Same thing for Left, Right, and Bottom

3. A test that proves setting focus to Tab2 in the first example causes Tab2 to get focus and the visuals are correct.

```
╭────┬────╮
│Tab1│Tab2│         
├────╯    ╰───────╮ 
│content for Tab  │
╰─────────────────╯
```

### Step 1: Add `Tab` and `Tabs` Views

1. Add `Tab` and `Tabs` views as described above.
2. Unit tests for Tab/Tabs behavior, including focus management and TabOffset calculations.
3. New Scenario based on `TabViewExample` named `TabsExample` that uses the new `Tabs` and `Tab` views, with multiple tabs and content, demonstrating focus switching and TabOffset.
4. Update Scenarios that were disabled in this PR with `#if false` to use the new `Tabs` and `Tab` views instead of the old `TabView`.

### Step 2: Scrolling

Tab header scrolling allows more tabs than fit in the visible width (or height, for left/right sides).

**Why Viewport scrolling doesn't work:** All tab SubViews use `Dim.Fill()` — they share the same Frame covering the full content area. Viewport scrolling shifts ALL SubViews equally, which moves tab content along with headers. Only headers should scroll; content must stay put.

**Approach:** `_scrollOffset` subtracted from `TabOffset` values. A custom `ScrollBar` (not the built-in `View.HorizontalScrollBar`) drives the offset. The scrollbar sits in the Border adornment area with a transparent slider so the content border line shows through.

#### Implementation Steps

##### 2a. Add `_scrollOffset` field and integrate into `UpdateTabOffsets`

```csharp
private int _scrollOffset;

internal void UpdateTabOffsets ()
{
    var offset = 0;

    foreach (View tab in TabCollection)
    {
        tab.Border.TabOffset = offset - _scrollOffset;

        int? tabLength = tab.Border.TabLength;

        if (tabLength is { })
        {
            // Subtract 1 because adjacent tabs share an edge
            offset += tabLength.Value - 1;
        }
    }

    // Update scrollbar sizing
    UpdateScrollBarSizes (offset);
}
```

`TabOffset` values can go negative — `BorderView` already handles this via `Rectangle.Intersect (headerRect, viewBounds)`, hiding headers that are fully off-screen and clipping partial ones.

##### 2b. Create custom ScrollBar in Border

The scrollbar lives in the `Border` adornment (not Padding) so it overlays the content border line at zero space cost:

```csharp
private ScrollBar? _headerScrollBar;

private void SetupHeaderScrollBar ()
{
    Border.GetOrCreateView ();

    _headerScrollBar = new ScrollBar
    {
        Orientation = _tabSide is Side.Top or Side.Bottom
                          ? Orientation.Horizontal
                          : Orientation.Vertical,
        VisibilityMode = ScrollBarVisibilityMode.Auto
    };

    // Transparent slider lets the content border line show through
    _headerScrollBar.Slider.ViewportSettings |= ViewportSettingsFlags.Transparent;

    _headerScrollBar.ValueChanged += (_, args) =>
                                     {
                                         _scrollOffset = args.NewValue;
                                         UpdateTabOffsets ();
                                         UpdateZOrder ();
                                         SetNeedsLayout ();
                                     };

    PositionHeaderScrollBar ();
    Border.View!.Add (_headerScrollBar);
}
```

Call `SetupHeaderScrollBar ()` from the `Tabs` constructor or `Initialized` handler (after Border exists).

##### 2c. Position the ScrollBar based on TabSide

```csharp
private void PositionHeaderScrollBar ()
{
    if (_headerScrollBar is null)
    {
        return;
    }

    // The scrollbar sits on the content border line row/column
    switch (_tabSide)
    {
        case Side.Top:
            _headerScrollBar.Orientation = Orientation.Horizontal;
            _headerScrollBar.X = 0;
            _headerScrollBar.Y = Pos.AnchorEnd () - 0; // Bottom row of Border (content border line)
            _headerScrollBar.Width = Dim.Fill ();
            _headerScrollBar.Height = 1;

            break;

        case Side.Bottom:
            _headerScrollBar.Orientation = Orientation.Horizontal;
            _headerScrollBar.X = 0;
            _headerScrollBar.Y = 0; // Top row of Border (content border line)
            _headerScrollBar.Width = Dim.Fill ();
            _headerScrollBar.Height = 1;

            break;

        case Side.Left:
            _headerScrollBar.Orientation = Orientation.Vertical;
            _headerScrollBar.X = Pos.AnchorEnd (); // Right column of Border
            _headerScrollBar.Y = 0;
            _headerScrollBar.Width = 1;
            _headerScrollBar.Height = Dim.Fill ();

            break;

        case Side.Right:
            _headerScrollBar.Orientation = Orientation.Vertical;
            _headerScrollBar.X = 0; // Left column of Border
            _headerScrollBar.Y = 0;
            _headerScrollBar.Width = 1;
            _headerScrollBar.Height = Dim.Fill ();

            break;
    }
}
```

Call `PositionHeaderScrollBar ()` from the `TabSide` setter after updating thickness/offsets.

##### 2d. Update ScrollBar sizes

Keep the scrollbar's `ScrollableContentSize` and `VisibleContentSize` in sync:

```csharp
private void UpdateScrollBarSizes (int totalHeaderSpan)
{
    if (_headerScrollBar is null)
    {
        return;
    }

    _headerScrollBar.ScrollableContentSize = totalHeaderSpan;

    _headerScrollBar.VisibleContentSize = _tabSide is Side.Top or Side.Bottom
                                              ? Viewport.Width
                                              : Viewport.Height;
}
```

Called from `UpdateTabOffsets ()` (which computes `totalHeaderSpan` as the cumulative offset) and from `FrameChanged` (viewport size may change on resize).

##### 2e. Auto-scroll to focused tab

When a tab is selected, ensure its header is fully visible by adjusting `_scrollOffset`:

```csharp
private void EnsureTabVisible (View tab)
{
    // Compute the absolute (unscrolled) offset for this tab
    var absOffset = 0;

    foreach (View t in TabCollection)
    {
        if (t == tab)
        {
            break;
        }

        absOffset += (t.Border.TabLength ?? 0) - 1;
    }

    int tabLength = tab.Border.TabLength ?? 0;
    int tabEnd = absOffset + tabLength;

    int visibleSize = _tabSide is Side.Top or Side.Bottom
                          ? Viewport.Width
                          : Viewport.Height;

    if (absOffset < _scrollOffset)
    {
        _scrollOffset = absOffset;
    }
    else if (tabEnd > _scrollOffset + visibleSize)
    {
        _scrollOffset = tabEnd - visibleSize;
    }

    if (_headerScrollBar is { })
    {
        _headerScrollBar.Value = _scrollOffset;
    }

    UpdateTabOffsets ();
}
```

Call from `ChangeValue ()` after setting `_value`.

##### 2f. Visual result

See the "Scrolling Drawings" section above for detailed before/after renderings showing scrolled states with 5 tabs. The key visual behaviors:

- Headers with negative `TabOffset` are clipped or hidden by `BorderView`'s `Rectangle.Intersect`
- The `◄►` scrollbar buttons appear at the content border line edges when headers overflow
- The transparent slider lets the content border line (junctions, gaps) show through
- Tab content is unaffected by header scrolling

#### Summary of changes

| Location | Change |
|----------|--------|
| `Tabs.cs` new field | `private int _scrollOffset` |
| `Tabs.cs` `UpdateTabOffsets ()` | Subtract `_scrollOffset` from each `TabOffset`; call `UpdateScrollBarSizes ()` |
| `Tabs.cs` new field + method | `_headerScrollBar` + `SetupHeaderScrollBar ()` — custom ScrollBar in Border |
| `Tabs.cs` new method | `PositionHeaderScrollBar ()` — positions by TabSide |
| `Tabs.cs` new method | `UpdateScrollBarSizes (int)` — syncs ScrollableContentSize/VisibleContentSize |
| `Tabs.cs` new method | `EnsureTabVisible (View)` — adjusts `_scrollOffset` to keep focused tab visible |
| `Tabs.cs` `ChangeValue ()` | Call `EnsureTabVisible ()` after setting value |
| `Tabs.cs` `TabSide` setter | Call `PositionHeaderScrollBar ()` |
| `BorderView.cs` | No changes — already clips via `Rectangle.Intersect` |

### Step 3: All Four Sides

Implement `Side.Bottom` (mirror of Top, `Thickness.Bottom = 3`), `Side.Left` (`Thickness.Left = 3`, vertical `TabOffset`), and `Side.Right` (`Thickness.Right = 3`, vertical `TabOffset`). See "Tab Style Renderings by Side" section for target visuals.

### Step 4: Focus, Hotkeys, and Draw Order

1. **Focus/Hotkeys**: Tab title `_` convention for hotkeys works via Tab's Title property. Click on header detected in Border's mouse handling.
2. **Draw order**: Selected tab should be drawn last (Z-order). May need to reorder SubViews or rely on focused-view-drawn-last.

## Resolved Design Questions

1. **Z-order of headers vs content**: With `Overlapped`, the selected tab is brought to the front (z-order), which means its content area could cover unselected tabs' headers. **Resolution**: This is not a problem because headers render in the **Border adornment** (Thickness.Top=3), which occupies space *above* the content Viewport. The selected tab's content renders inside its Viewport and cannot cover the Border area of sibling tabs. All Borders render to the same `LineCanvas` via `SuperViewRendersLineCanvas`.

2. **Vertical text for Side.Left/Right**: `TextDirection.TopBottom_LeftRight` already exists in the `TextDirection` enum and is supported by `TextFormatter`. Tab headers on left/right sides can use this for vertical title rendering.

3. **Migration from existing TabRow-based implementation**: Delete all existing `Tab.cs`, `TabRow.cs`, `TabView.cs`, and all tests. Start fresh with the Border-based design. Existing `TabViewVisualTests` patterns may inform new tests but won't be preserved.

