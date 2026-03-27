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

        // Thickness set by Tabs based on TabSide; example for Side.Top:
        Border.Thickness = new Thickness (1, 3, 1, 1);

        // BorderStyle should inherit automatically from Tabs (SuperView).

        // Overlapped enables z-order: focused tab renders above unselected tabs
        Arrangement = ViewArrangement.Overlapped;
    }  
}
```


### `Tabs`

```cs
public class Tabs : View, IValue<Tab?>
{
    public Tabs ()
    {
        CanFocus = true;
        BorderStyle = LineStyle.Rounded;
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

    // Computes TabOffsets, manages thickness per TabSide, prevents key events from reaching unfocused tabs, etc.
}
```

#### Z-order and Focus

The focused tab will always be the last subview. the rest need to be in reverse order as they appear.

## Tab Style Renderings by Side

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

Add `Tabs.ScrollOffset` subtracted from all `TabOffset` + `TabEnd` values. Headers with negative effective offset are clipped. Use built-in `View` scrollbar with transparent slider as described above.

### Step 3: All Four Sides

Implement `Side.Bottom` (mirror of Top, `Thickness.Bottom = 3`), `Side.Left` (`Thickness.Left = 3`, vertical `TabOffset`), and `Side.Right` (`Thickness.Right = 3`, vertical `TabOffset`). See "Tab Style Renderings by Side" section for target visuals.

### Step 4: Focus, Hotkeys, and Draw Order

1. **Focus/Hotkeys**: Tab title `_` convention for hotkeys works via Tab's Title property. Click on header detected in Border's mouse handling.
2. **Draw order**: Selected tab should be drawn last (Z-order). May need to reorder SubViews or rely on focused-view-drawn-last.

## Resolved Design Questions

1. **Z-order of headers vs content**: With `Overlapped`, the selected tab is brought to the front (z-order), which means its content area could cover unselected tabs' headers. **Resolution**: This is not a problem because headers render in the **Border adornment** (Thickness.Top=3), which occupies space *above* the content Viewport. The selected tab's content renders inside its Viewport and cannot cover the Border area of sibling tabs. All Borders render to the same `LineCanvas` via `SuperViewRendersLineCanvas`.

2. **Vertical text for Side.Left/Right**: `TextDirection.TopBottom_LeftRight` already exists in the `TextDirection` enum and is supported by `TextFormatter`. Tab headers on left/right sides can use this for vertical title rendering.

3. **Migration from existing TabRow-based implementation**: Delete all existing `Tab.cs`, `TabRow.cs`, `TabView.cs`, and all tests. Start fresh with the Border-based design. Existing `TabViewVisualTests` patterns may inform new tests but won't be preserved.

