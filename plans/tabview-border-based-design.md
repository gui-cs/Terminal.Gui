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

Because the `Tab` objects are just SubViews of `Tabs`, the `ContentSize` of `Tab` can be set to be `SubViews.OfType<Tab>().Max(t => t.TabOffset + t.TabWidth)`. By doingthis scrolling is automatically supported — when the `TabOffset` of a tab is scrolled such that it would be partially or fully offscreen, the LineCanvas auto-joins will produce the correct clipped visuals. The content border top line will also auto-join with the visible portion of the tab header. `Tabs` will need code to ensure that when a tab is selected (focused), if its header is partially offscreen, the `TabOffset` is adjusted to bring the entire header into view.

We will use the built-into `View` scrollbar functionality. 

- In `Tabs`, set `ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar`
- In `Tabs`, change the position of the `HorizontalScrollBar` to be at the top of the `Padding`, just below the tab headers, instead of at the bottom (or, move the scrollbar out of Padding into Border). This way the scrollbar is visually connected to the tab headers and it scrolls the headers as expected. 

With the above we'd have:

```
╭────┬────┬────╮
│Tab1│Tab2│Tab3│
◄░░░░███████░░░░░░►
│content for T2   │
│                 │
╰─────────────────╯
```

But THEN, if we did this:

- make `ScrollBar._slider` public as `ScrollBar.Slider`
- In `Tabs`, set `HorizontalScrollBar.Slider.ViewportSettings |= ViewportSettingsFlags.Transparent`

We'd have this:

```
╭────┬────┬────╮
│Tab1│Tab2│Tab3│
◄────╯    ╰────┴──►
│content for T2   │
│                 │
╰─────────────────╯
```

Fucking magic.

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

Tab header scrolling allows more tabs than fit in the visible width (or height, for left/right sides). The approach uses the built-in `View` scrollbar infrastructure with a transparent slider, so the content border line shows through the scrollbar area.

#### Architecture

Tab headers are rendered by each tab's `BorderView` using `ComputeHeaderRect()`, which positions the header at `contentBorderRect.X + TabOffset` (for Top/Bottom). The `TabOffset` is an absolute value in content coordinates. When `Tabs` has a `ContentSize` larger than its `Viewport`, scrolling the `Viewport` shifts all content coordinates, which naturally shifts where tab headers render. `BorderView` already clips headers via `Rectangle.Intersect (headerRect, viewBounds)` and hides tabs when `clipped.IsEmpty`.

This means: **no changes to `BorderView` rendering are needed**. All scrolling behavior comes from setting `ContentSize` and managing `Viewport`.

#### Implementation Steps

##### 2a. Set ContentSize in Tabs based on total tab header span

In `Tabs`, after `UpdateTabOffsets()`, compute the total span of all tab headers and set `ContentSize` accordingly:

```csharp
private void UpdateContentSize ()
{
    View? lastTab = TabCollection.LastOrDefault ();

    if (lastTab is null)
    {
        ContentSizeTracksViewport = true;

        return;
    }

    int totalSpan = lastTab.Border.TabEnd;

    // ContentSize must be at least as large as Viewport to prevent negative scrolling
    if (_tabSide is Side.Top or Side.Bottom)
    {
        int width = Math.Max (totalSpan, Viewport.Width);
        SetContentSize (new Size (width, Viewport.Height));
    }
    else
    {
        int height = Math.Max (totalSpan, Viewport.Height);
        SetContentSize (new Size (Viewport.Width, height));
    }
}
```

Call `UpdateContentSize()` at the end of `UpdateTabOffsets()`, and also from `OnSubViewAdded`/`OnSubViewRemoved`/`TabSide` setter.

**Key concern:** `Border.TabEnd` returns `Frame.X + TabOffset + TabLength`. Since tab SubViews use `Dim.Fill()`, their `Frame.X` is 0. So `TabEnd` = `TabOffset + TabLength`. The last tab's `TabEnd` gives us the total span.

Actually, `TabEnd` on the `Border` includes `GetFrame().X`. For tabs inside a `Tabs` container, the tab's border frame starts at the tab's position. Since all tabs use `Dim.Fill()` and are overlapped, their frames all start at (0, 0) relative to the `Tabs` content area. So `Border.GetFrame()` will return the border's frame relative to its parent tab, which starts at X=0. Therefore `TabEnd = 0 + TabOffset + TabLength`. This is correct — the last tab's `TabEnd` gives the total header span in content coordinates.

##### 2b. Enable horizontal scrollbar with transparent slider

In the `Tabs` constructor:

```csharp
public Tabs ()
{
    CanFocus = true;
    Width = Dim.Fill ();
    Height = Dim.Fill ();

    // Enable horizontal scrollbar for tab header overflow
    ViewportSettings |= ViewportSettingsFlags.HasHorizontalScrollBar;
}
```

After the scrollbar is created (e.g., in an `Initialized` handler or after first layout), reposition and configure it:

```csharp
// In Tabs constructor or Initialized handler:
Initialized += (_, _) =>
{
    // Move scrollbar to top of Padding (just below tab headers) instead of bottom
    HorizontalScrollBar.Y = Pos.Func (_ => Padding.Thickness.Top);
    // Keep X and Width as default (fills the width)

    // Make the slider transparent so the content border line shows through
    HorizontalScrollBar.Slider.ViewportSettings |= ViewportSettingsFlags.Transparent;
};
```

**Default behavior (View.ScrollBars.cs):**
- `ConfigureHorizontalScrollBar` sets: `Y = Pos.AnchorEnd() - Pos.Func(_ => Padding.Thickness.Bottom - 1)` (bottom of padding)
- `ConfigureHorizontalScrollBarEvents` adjusts `Padding.Thickness.Bottom` when scrollbar visibility changes

**What we must change:**
- Reposition `HorizontalScrollBar.Y` to the top of Padding (below the tab depth area)
- Override the `VisibleChanged` handler to adjust `Padding.Thickness.Top` instead of `.Bottom`
- The scrollbar must sit at the exact row where the content border line is drawn, so the transparent slider lets the line show through

##### 2c. Scrollbar position calculation

For `Side.Top` with `TabDepth = 3`:
- Border thickness: `(1, 3, 1, 1)` → top=3 means rows 0-2 are: outer border, title, inner border
- The scrollbar should sit on the "inner border" row (row 2 of the border, which is the content border top line)
- In Padding coordinates, this is at Y=0 (the first row of the Padding area, which is immediately below Border)

For `Side.Bottom`:
- Border thickness: `(1, 1, 1, 3)` → bottom=3
- The scrollbar should sit at the bottom of the Padding area (the last row before the bottom border)
- `HorizontalScrollBar.Y = Pos.AnchorEnd()`

For `Side.Left`/`Side.Right`:
- Use `VerticalScrollBar` instead of `HorizontalScrollBar`
- Similar repositioning logic (left edge or right edge of padding)

**This means the scrollbar position must update when `TabSide` changes.** Add a `UpdateScrollBarPosition()` method called from the `TabSide` setter.

##### 2d. ScrollBar VisibleChanged override

The default `ConfigureHorizontalScrollBarEvents` adjusts `Padding.Thickness.Bottom` when the scrollbar appears/disappears. For `Tabs`, we need to suppress this and instead adjust the correct side:

```csharp
// After scrollbar creation, override the thickness adjustment
HorizontalScrollBar.VisibleChanged += (_, _) =>
{
    // The default handler already adjusted Padding.Thickness.Bottom.
    // We need to undo that and adjust the correct side based on TabSide.
    // ...
};
```

**Alternative (simpler):** Since the scrollbar overlays the content border line (transparent slider), we may NOT need to adjust Padding thickness at all. The scrollbar sits on top of the border line and the transparent slider lets the line show through. The `◄` and `►` buttons replace the first/last characters of the content border line.

This depends on whether the scrollbar is in `Padding` (which is inside Border) or needs to be in `Border` itself. If in Padding, the scrollbar row takes space from the content area. If we want zero space impact, the scrollbar must overlay the border line, which means it needs to be in `Border`, not `Padding`.

**Decision needed:** The plan says "move the scrollbar out of Padding into Border" as an option. This is the cleaner approach since:
1. The scrollbar replaces the content border line visually
2. No content space is lost
3. The transparent slider naturally overlays the border line drawing

To do this:
- Create a custom `ScrollBar` instance (not using `View.HorizontalScrollBar` which is locked to Padding)
- Add it to `Border.View` instead of `Padding.View`
- Manually wire up `ValueChanged` to adjust tab offsets (or Viewport)

**OR** keep it simple for now:
- Use the built-in `HorizontalScrollBar` in Padding
- Accept that the scrollbar takes 1 row from content when visible
- The transparent slider still shows the content border line
- Iterate on the "zero-space" approach later

##### 2e. Auto-scroll to focused tab

When a tab is selected (focused), ensure its header is fully visible by scrolling if needed:

```csharp
// In OnFocusedChanged or ChangeValue, after setting Value:
private void EnsureTabVisible (View tab)
{
    int tabStart = tab.Border.TabOffset;
    int tabEnd = tab.Border.TabEnd;

    if (_tabSide is Side.Top or Side.Bottom)
    {
        // Scroll left if tab starts before viewport
        if (tabStart < Viewport.X)
        {
            Viewport = Viewport with { X = tabStart };
        }
        // Scroll right if tab ends after viewport
        else if (tabEnd > Viewport.X + Viewport.Width)
        {
            Viewport = Viewport with { X = tabEnd - Viewport.Width };
        }
    }
    else
    {
        // Same logic for Y axis (Side.Left/Right)
        if (tabStart < Viewport.Y)
        {
            Viewport = Viewport with { Y = tabStart };
        }
        else if (tabEnd > Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = tabEnd - Viewport.Height };
        }
    }
}
```

Call `EnsureTabVisible()` from `ChangeValue()` after setting `_value`, and from anywhere that selects a tab programmatically.

**Important:** `Border.TabEnd` returns `GetFrame().X + TabOffset + TabLength`. For tabs with `Dim.Fill()` inside `Tabs`, `GetFrame().X` will be relative to the `Tabs` content area. Need to verify this returns the correct absolute content-coordinate position.

##### 2f. UpdateTabOffsets must NOT subtract scroll offset

`UpdateTabOffsets()` computes cumulative `TabOffset` values in **content coordinates** (not viewport coordinates). The viewport scroll handles the visual shift. Do NOT modify `UpdateTabOffsets()` to account for scrolling — that's the Viewport's job.

#### Summary of changes for scrolling

| File | Change |
|------|--------|
| `Tabs.cs` constructor | Add `ViewportSettings \|= HasHorizontalScrollBar` |
| `Tabs.cs` constructor/Initialized | Reposition scrollbar, set `Slider.ViewportSettings \|= Transparent` |
| `Tabs.cs` new method | `UpdateContentSize()` — sets `ContentSize` based on total tab header span |
| `Tabs.cs` `UpdateTabOffsets()` | Call `UpdateContentSize()` at the end |
| `Tabs.cs` new method | `EnsureTabVisible (View tab)` — auto-scrolls to keep focused tab visible |
| `Tabs.cs` `ChangeValue()` | Call `EnsureTabVisible()` after setting value |
| `Tabs.cs` `TabSide` setter | Call `UpdateScrollBarPosition()` to switch between H/V scrollbar |
| `Tabs.cs` new method | `UpdateScrollBarPosition()` — repositions scrollbar based on TabSide |
| No changes needed | `BorderView.cs` — already clips headers via `Rectangle.Intersect` |

#### Open questions

1. **Scrollbar in Padding vs Border**: Using the built-in scrollbar (in Padding) costs 1 row of content space. Moving to Border avoids this but requires manual scrollbar management. Start with Padding; optimize to Border later if needed.

2. **Left/Right side scrolling**: Uses `VerticalScrollBar` instead. Same pattern, different axis. The scrollbar would be positioned at the left or right edge of Padding.

3. **TabEnd accuracy**: Need to verify that `Border.TabEnd` returns the correct value when the tab's Frame starts at (0,0) due to `Dim.Fill()` / `ViewArrangement.Overlapped`. If `GetFrame()` returns the Border's frame (not the tab's frame), the value may already be correct since Border's frame is relative to its parent (the tab View).

4. **Content area scrolling vs header-only scrolling**: The current approach scrolls the entire Viewport, which would also scroll tab content. This is wrong — only headers should scroll. **This is the critical design issue.** Tab content should NOT scroll horizontally just because headers overflow.

   **Solution:** The tab headers are rendered by each tab's `Border` (an adornment, not content). Adornments render in screen coordinates, not viewport coordinates. `ComputeHeaderRect` uses `contentBorderRect` which is derived from the Border's frame, not the Viewport. So **Viewport scrolling does NOT affect header position**.

   This means we **cannot** use Viewport scrolling to scroll headers. We need a different approach:

   **Revised approach — ScrollOffset on TabOffset:**
   - Add a `_scrollOffset` field to `Tabs`
   - In `UpdateTabOffsets()`, subtract `_scrollOffset` from each tab's `TabOffset`
   - The scrollbar's `Value` drives `_scrollOffset`
   - `EnsureTabVisible()` adjusts `_scrollOffset` (not Viewport)
   - `ContentSize` and `Viewport` are NOT used for tab header scrolling
   - The scrollbar is a standalone `ScrollBar` added to the `Tabs` view (not the built-in one)

   This is cleaner because:
   - Tab content is unaffected by header scrolling
   - `TabOffset` directly controls header position; subtracting scroll offset is straightforward
   - The scrollbar can be positioned in the border area (row where content border line is)
   - Transparent slider shows the content border line through

   **Implementation of revised approach:**

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
               offset += tabLength.Value - 1;
           }
       }
   }
   ```

   The scrollbar is a custom `ScrollBar` instance managed by `Tabs`:

   ```csharp
   private ScrollBar? _headerScrollBar;

   private void SetupHeaderScrollBar ()
   {
       _headerScrollBar = new ScrollBar
       {
           Orientation = _tabSide is Side.Top or Side.Bottom
                             ? Orientation.Horizontal
                             : Orientation.Vertical,
           VisibilityMode = ScrollBarVisibilityMode.Auto
       };

       // Make slider transparent so border line shows through
       _headerScrollBar.Slider.ViewportSettings |= ViewportSettingsFlags.Transparent;

       _headerScrollBar.ValueChanged += (_, args) =>
       {
           _scrollOffset = args.NewValue;
           UpdateTabOffsets ();
           UpdateZOrder ();
           SetNeedsLayout ();
       };

       // Position based on TabSide (in the Border adornment area)
       // For Side.Top: at the content border line row
       // ...
   }
   ```

   `EnsureTabVisible()` adjusts `_scrollOffset`:

   ```csharp
   private void EnsureTabVisible (View tab)
   {
       int tabStart = IndexOf (tab);
       // Compute the absolute (unscrolled) offset for this tab
       var absOffset = 0;
       foreach (View t in TabCollection)
       {
           if (t == tab) break;
           absOffset += (t.Border.TabLength ?? 0) - 1;
       }
       int tabEnd = absOffset + (tab.Border.TabLength ?? 0);

       if (_tabSide is Side.Top or Side.Bottom)
       {
           int visibleWidth = Viewport.Width;
           if (absOffset < _scrollOffset)
           {
               _scrollOffset = absOffset;
           }
           else if (tabEnd > _scrollOffset + visibleWidth)
           {
               _scrollOffset = tabEnd - visibleWidth;
           }
       }
       // ... similar for Left/Right with Height

       _headerScrollBar.Value = _scrollOffset;
       UpdateTabOffsets ();
   }
   ```

   **ScrollBar sizing:** `_headerScrollBar.ScrollableContentSize` = total unscrolled tab header span. `_headerScrollBar.VisibleContentSize` = Viewport width (or height). Update these in `UpdateContentSize()` (renamed from its original purpose).

   **ScrollBar placement:** The scrollbar needs to sit where the content border line is drawn. For `Side.Top`, this is the bottom row of the border's tab-depth area. This requires adding the scrollbar as a SubView of the `Border` adornment's view, not Padding. Use `Border.GetOrCreateView()` then `Border.View!.Add(_headerScrollBar)`.

### Step 3: All Four Sides

Implement `Side.Bottom` (mirror of Top, `Thickness.Bottom = 3`), `Side.Left` (`Thickness.Left = 3`, vertical `TabOffset`), and `Side.Right` (`Thickness.Right = 3`, vertical `TabOffset`). See "Tab Style Renderings by Side" section for target visuals.

### Step 4: Focus, Hotkeys, and Draw Order

1. **Focus/Hotkeys**: Tab title `_` convention for hotkeys works via Tab's Title property. Click on header detected in Border's mouse handling.
2. **Draw order**: Selected tab should be drawn last (Z-order). May need to reorder SubViews or rely on focused-view-drawn-last.

## Resolved Design Questions

1. **Z-order of headers vs content**: With `Overlapped`, the selected tab is brought to the front (z-order), which means its content area could cover unselected tabs' headers. **Resolution**: This is not a problem because headers render in the **Border adornment** (Thickness.Top=3), which occupies space *above* the content Viewport. The selected tab's content renders inside its Viewport and cannot cover the Border area of sibling tabs. All Borders render to the same `LineCanvas` via `SuperViewRendersLineCanvas`.

2. **Vertical text for Side.Left/Right**: `TextDirection.TopBottom_LeftRight` already exists in the `TextDirection` enum and is supported by `TextFormatter`. Tab headers on left/right sides can use this for vertical title rendering.

3. **Migration from existing TabRow-based implementation**: Delete all existing `Tab.cs`, `TabRow.cs`, `TabView.cs`, and all tests. Start fresh with the Border-based design. Existing `TabViewVisualTests` patterns may inform new tests but won't be preserved.

