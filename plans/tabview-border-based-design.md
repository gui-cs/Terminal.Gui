# TabView via Border Tab Style Borders (Built into View) — Design Plan

## Concept

Instead of a `TabRow` containing dynamically-created header views, make **Border itself** support a "tab style" rendering mode. When enabled, a View's Border renders a small **tab header rectangle** (containing the View's `Title`) at an offset along any side (top, bottom, left, or right). A `TabView` is then a simple superview of `Tab` views whose Borders all use tab style — TabView computes each tab's offset so headers sit side-by-side.

**The critical design constraint:** all Tab views must share a single `LineCanvas` via `SuperViewRendersLineCanvas = true`. When adjacent tab headers overlap by one column, LineCanvas **auto-joins** the intersecting border lines into correct junction glyphs (`┬`, `┴`, `╮`, `╰`, etc.), producing the flowing connected style with zero manual line-drawing:

```
╭───┬───╮
│T1 │T2 │
│   ╰───┴────╮
│content     │
╰────────────╯
```

This eliminates `TabRow` entirely and makes the "tab" concept a **first-class Border capability**.

## How Border Renders Titles Today

**Definitive reference:** `./ViewBase/Adornment/Border.cs` and `Tests/UnitTests/View/Adornment/BorderTests.cs`

### `Border.Thickness.Top == 1` — Title Inline on Border Line

The title sits directly on the single top border line. The `┤` and `├` connectors flank the title text. The border occupies exactly 1 row at the top.

```
Single, View.Width = 10:
┌┤1234├──┐       ← row 0: top border line with title inline
│        │       ← row 1: content (inside the border)
│        │
└────────┘       ← bottom border line
```

Width variations (Single style):
```
Width 4:   ┌┤├┐         (too narrow for title text, just connectors)
Width 5:   ┌┤1├┐        (1 char fits)
Width 8:   ┌┤1234├┐     (full title, no extra border)
Width 10:  ┌┤1234├──┐   (full title + extra border line)
```

### `Border.Thickness.Top == 2` — Title in a Cap (2 Rows, No Bottom Line)

The border occupies 2 rows at the top. Row 0 has a small horizontal cap line above the title. Row 1 has the main border's top line with the title text. The connectors are **corner glyphs** (`╛`/`╘` for Double, `┘`/`└` for Single) — they terminate, meaning there is **no bottom line** closing the title area.

```
Double, View.Width = 10:
 ╒════╕            ← row 0 (topTitleLineY): cap line above title
╔╛1234╘══╗         ← row 1 (titleY): main border top + title + corner connectors ╛/╘
║        ║         ← row 2: content
╚════════╝         ← bottom border
```

Code (Border.cs lines 279-284):
```csharp
topTitleLineY = borderBounds.Y - 1;   // 1 row above the main border line
titleY = topTitleLineY + 1;            // title on the main border line
titleBarsLength = 2;                    // connectors span 2 rows (cap → title)
```

### `Border.Thickness.Top == 3` — Title in Enclosed Rectangle (3 Rows, WITH Bottom Line)

The border occupies 3 rows at the top. A complete rectangle encloses the title: top line (row 0), title + T-junction connectors (row 1), and a **bottom line** (row 2). The connectors are **T-junction glyphs** (`╡`/`╞` for Double, `┤`/`├` for Single) — they continue through, connecting all three rows.

```
Double, View.Width = 10:
 ╒════╕            ← row 0 (topTitleLineY): top of title rectangle
╔╡1234╞══╗         ← row 1 (titleY): main border top + title + T-junction connectors ╡/╞
║╘════╛  ║         ← row 2 (topTitleLineY+2): *** BOTTOM LINE of title rectangle ***
╚════════╝         ← bottom border

Rounded, View.Width = 10:
 ╭────╮
╭┤1234├──╮
│╰────╯  │        ← row 2: bottom line (╰────╯) — THIS gets suppressed in Tab mode
│        │
╰────────╯
```

Code (Border.cs lines 289-295):
```csharp
topTitleLineY = borderBounds.Y - 2;   // 2 rows above the main border line
titleY = topTitleLineY + 1;            // title on middle row
titleBarsLength = 3;                    // connectors span 3 rows (top → title → bottom)
sideLineLength++;                       // side borders extend up one more row
```

The bottom line is drawn at Border.cs lines 382-386:
```csharp
lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY + 2),
             Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
             Orientation.Horizontal, lineStyle, normalAttribute);
```

**Key differences from `Top == 2`:**

| Aspect | `Top == 2` | `Top == 3` |
|--------|-----------|-----------|
| Connectors | `╛`/`╘` (corners, terminate) | `╡`/`╞` (T-junctions, continue) |
| Bottom line | None | `╘════╛` drawn at row 2 |
| `titleBarsLength` | 2 | 3 |
| `sideLineLength` | unchanged | `++` (extends up) |

### `Border.Thickness.Top == 4` — Same as 3, Extra Space Above

Identical rendering to `Top == 3` but with one additional empty row above. The title rectangle is the same shape; it just floats one row higher.

### Auto-Join with `SuperViewRendersLineCanvas`

From `SuperViewRendersLineCanvas_Title_AutoJoinsLines` test — two overlapping SubViews with different `LineStyle`s:

```
Without SuperViewRendersLineCanvas:     With SuperViewRendersLineCanvas:
┌┤A├──────┐                            ╔╡A╞═╦────┐
│    ║    │                            ║    ║    │
│    ║    │                            ║    ║    │
│════┌┤C├┄│                            ╠════╬┤C├┄┤
│    ┊    │                            │    ┊    ┊
│    ┊    │                            │    ┊    ┊
└─────────┘                            └────┴┄┄┄┄┘
```

When `SuperViewRendersLineCanvas = true`, all border lines render to the **same LineCanvas**, and overlapping lines auto-join into correct junction glyphs (`╦`, `╬`, `╠`, `┴`). **This is the mechanism Tab mode relies on** for producing the flowing connected tab style.

## How Auto-Join Produces the Flowing Style

### What LineCanvas Does

When two border lines are drawn at the same `(x, y)` on the same `LineCanvas`, it resolves them into the correct glyph:

| Overlap | Result |
|---------|--------|
| `╮` + `╭` at same cell | `┬` (T-junction) |
| `╯` + `╰` at same cell | `┴` (bottom T-junction) |
| horizontal end + vertical | `├`, `┤` |
| two verticals | continuous `│` |

### How It Works for Tabs

All `Tab` views are siblings inside `TabView`, all with `SuperViewRendersLineCanvas = true`. Every Tab's Border writes its header lines to **TabView's shared LineCanvas**. When tabs overlap by one column at shared edges:

```
Tab1's header:         Tab2's header:          Shared LineCanvas result:
╭────╮                      ╭────╮             ╭────┬────╮
│ T1 │                      │ T2 │             │ T1 │ T2 │
╰────╯                      ╰────╯             ╰────┴────╯
       ↑                    ↑
       Tab1 right edge overlaps Tab2 left edge
       → ╮ + ╭ = ┬ (top)
       → ╯ + ╰ = ┴ (bottom)
```

For the **selected tab** (T1), its header bottom is open (no bottom line drawn). The selected tab's content border top line runs along the same row. Unselected tabs DO draw their header bottom. Everything auto-joins:

```
Tab1 header (no bottom):   Tab2 header (closed):   Content top line:
╭────╮                      ╭────╮
│ T1 │                      │ T2 │
     (gap)                  ╰────╯                  ─────────────────

Combined on shared LineCanvas:
╭────┬────╮              ← ╮+╭ → ┬
│ T1 │ T2 │
│    ╰────┴──────╮       ← T2 bottom + content top auto-join → ┴
│ content        │          T1 sides continue into content borders
╰────────────────╯
```

The `╰` where T2's bottom-left meets the content line is an auto-join. The `┴` where T2's bottom-right meets both T2's right and the content line is an auto-join. The left side of T1 continues straight down from the header into the content border — it's one continuous vertical line on the LineCanvas.

## New Design Plan

### Design Philosophy

Amazon Principal Engineer tenets applied:

- **Exemplary Practitioner**: `Tab` and `Tabs` become THE reference implementation for building compound views in Terminal.Gui v2. It should teach other contributors the right patterns.
- **Technically Fearless**: Tackle the Border rendering problems head-on. The whole point is to prove the v2 infrastructure works.
- **Balanced and Pragmatic**: Solve the Border rendering, mouse/keyboard interaction, etc... problems with a minimal, targeted enhancement — not the full #3407 refactor.
- **Illuminate and Clarify**: Simple architecture. `Tab` is just a `View` that gets added to `Tabs`. No complex switch statements, no scattered layout math, no manual line drawing.
- **Flexible in Approach**: Leverage `Adornments`, `ViewportSettingsFlags.Transparent`, `ViewArrangement.Overlapped`, and `SuperViewRendersLineCanvas` to solve the rendering, selection, and nav problems with minimal changes to Border and View.
- **Respect What Came Before**: Preserve the spirit and capabilities of the original TabView (scrolling tabs, hotkeys, top/bottom positioning, mouse support) while completely rethinking the implementation.
- **Have Resounding Impact**: Proves out Command propagation, content scrolling, KeyBindings, MouseBindings, Adornments, and LineCanvas auto-joins working together in a real compound view.
- **Breaking changes to the API are ok**: This is a major new feature that requires API additions and some changes to existing Border behavior when `Tab` is enabled.

### Phase 1: Extend Border with Tab Support

```csharp
[Flags]
public enum BorderSettings
{
    None = 0,
    Title = 1,
    Gradient = 2,
    Tab = 4,                // NEW: Renders a tab header rectangle at TabOffset
}
```

When `Tab` is set:
- BorderView renders a **tab header** at `TabOffset` columns from the left, using the existing `Thickness.Top == 3` rendering path
- The header contains the View's `Title` with top line, vertical connectors, and title text
- `Title` flag behavior is implied
- `Thickness.Top` must be 3 (3 rows: header top, title+connectors, content top / header bottom)
- The bottom line of the title rectangle (Border.cs lines 382-386) is suppressed for the selected tab
- The Border area outside the header rectangle is transparent (mouse + visual)

### New `BorderView.TabOffset` 

```csharp
/// <summary>
///     Gets or sets the horizontal/vertical offset (in columns/rows) at which the tab header
///     rectangle starts. Only effective when <see cref="Settings"/> includes
///     <see cref="BorderSettings.Tab"/>.
/// </summary>
public int TabOffset { get; set; }
```

### New `BorderView.TabWidth`

```csharp
/// <summary>
///     Gets the rendered width/height of the tab header rectangle. Only meaningful when <see cref="Settings"/>
///     includes <see cref="BorderSettings.Tab"/>.
/// </summary>
public int TabWidth => (Parent?.TitleTextFormatter.FormatAndGetSize ().Width (or Height) ?? 0) + 2;
```

### New `BorderView.TabSide`

```csharp
/// <summary>
///     Gets or sets which side of the View, the Tab-syled Title will be placed. Only meaningful when <see cref="Settings"/>
///     includes <see cref="BorderSettings.Tab"/>.
/// </summary>
public Side TabSide {get;set}
```

When `BorderSettings.Tab` is set on `View.Border.BorderSettings`:

- The `Border` View is created via `GetOrCreateView`
- `Border.BorderStyle` defaults to `Rounded`
- Suppress the bottom line at `topTitleLineY + 2` (Border.cs lines 382-386). The title rectangle becomes open-bottomed, flowing into the content area. Draw the content border's top line in segments around the header gap instead.
- Make the `Border.ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse` - assuming there are no bugs, this will make it so clicking outside of the "tab" will pass through.
- Focus is indicated by the presence of the header bottom line. The selected tab has its header bottom line suppressed, creating an open gap that visually connects the header to the content area. Unselected tabs draw the full header rectangle, including the bottom line, creating a closed header. The `Title` text is always drawn using the `Normal`/`HotNormal` attributes, regardless of focus.

Depending on the `Thickness.Top` value, the header rectangle will be taller or shorter. The examples below show the visual difference as you increase `Thickness.Top` from 1 to 4. The "tab" concept starts to emerge at `Thickness.Top = 3` when the bottom line appears, but the exact visuals depend on the LineCanvas auto-join behavior.

These examples show `Side.Top`. All four sides are possible and follow the same pattern. Visual examples are provided in the descripiton of Phase 2 below and not duplicated here.

### `Border.Thickness.Top = 1`

(No dev will ever do this, but it is possible.)
```
│Tab╰───╮ 
│content│ 
│       │ 
╰───────╯
```

### `Border.Thickness.Top = 2`

(Devs who do this, will need to provide an alternative for indicating focus, since there is no ability to have bottom line.)
```
╭───╮
│Tab╰───╮ 
│content│ 
│       │ 
╰───────╯
```

### `Border.Thickness.Top = 3`

(The intended "tab" style. The bottom line of the header rectangle is drawn, creating a closed tab. Focus is indicated by suppressing the bottom line, creating an open tab that visually connects to the content area. The exact visuals depend on LineCanvas auto-join behavior.)
```
╭───╮
│Tab│         
│   ╰───╮ 
│content│ 
│       │ 
╰───────╯
```

### `Border.Thickness.Top = 4`

(Shown for completeness.)
```

╭───╮
│Tab│         
│   ╰───╮ 
│content│ 
│       │ 
╰───────╯
```

The Tab style works on all four sides. `BorderView` will gain a new property (`TabSide`?) of type `Side` which dictates which side the tabs are rendered on. There are renderings below that show the visuals per-side.

Another new property `BorderView.TabOffset` (int) specifies how many columns/rows from the left/top edge the tab header starts.

The `TabOffset` property is always along the axis of the tab strip:
- **Top/Bottom**: `TabOffset` is horizontal (columns from left edge)
- **Left/Right**: `TabOffset` is vertical (rows from top edge)

For `Border.BorderStyle == BorderStyle.Tab`, `BorderView.TabSide = Side.Top`, `Border.Thickness.Top = 3` and `BorderView.TabOffset = 2`, the rendering would look like this:
```
  ╭───╮
  │Tab│
╭─┴───┴─╮
│content│
│       │
╰───────╯
```

With `TabOffset = 4`:
```
    ╭───╮
    │Tab│
╭───┴───┤
│content│
│       │
╰───────╯
```

For completeness if `TabOffset = 5`, causing the right side of the tab to extend beyond the right-side's border line:
```
     ╭───
     │Tab
╭────┴──╮
│content│
│       │
╰───────╯
```


For completeness if `TabOffset = -1`, causing the left side of the tab to extend beyond the right-side's border line:
```
───╮
Tab│
╭──┴────╮
│content│
│       │
╰───────╯
```

IOW, it gets clipped by the view's right border, but the header is still visible and functional. The content top line auto-joins with the tab header's right connector at the intersection point, producing a flowing style.

**Important** the renderings above are what happens when the View *does not have focus* (`HasFocus == false`). When a View is using `BorderSettings.Tab`, focus is indicated not by the `Title` being rendered using the focus attribute, but by the presence of the header bottom line. The selected tab has its header bottom line suppressed, creating an open gap that visually connects the header to the content area. Unselected tabs draw the full header rectangle, including the bottom line, creating a closed header.

### Steps

#### 1. Get `Side.Top` working and tested

**Files:** `BorderSettings.cs`, `Border.cs`, `BorderView.cs`

1. Add `BorderSettings.Tab = 4`
2. Add `BorderView.TabSide` (Side), default `Side.Top`
3. Add `BorderView.TabOffset` (int), `Border.TabWidth` (computed)
4. In `OnDrawingContent`, when `Tab` is set:
   - Draw header rectangle at `TabOffset` (top, left, right, and conditionally bottom)
   - Draw title text inside header
   - If `Parent.HasFocus == true`: draw content top line in segments around the header, plus left/right/bottom normally
   - If `Parent.HasFocus == false`: draw content top line across bottom of header, plus left/right/bottom normally
   - Handle transparency for the header area
5. New Unit tests for Border tab rendering in isolation (in `UnitTestsParallelizable`)
6. Enhance `BorderEditor` to support editing `Tab` settings.
7. Enhanced `Adornments` Scenario (should not require any work if `BorderEditor` is updated correctly) to include a `Border` with `Tab` settings, allowing visual testing of the tab rendering in the Adornments scenario.

#### 2. Get `Side.Bottom` working and tested

Should be fairly mechanical given Top.

#### 3. Get `Side.Left` and `Side.Right` working and tested 

Should be fairly mechanical given the above.

### Exit Critera

- Robust new tests that both logically and visually prove the `BorderSettings.Tab` and related functionality work
- All four `Side`s supported
- `BorderEditor` supports changing `BorderSettings`, `BorderSides`, `TabOffset` (numericupdown), and `TabWidth` (numericupdown) implemented.

## Phase 2 - "TabView" Style - Enables a replacement for the old TabView

Uses the capabilty built in Phase 1 to build a new TabView View-subclass, related helper Views, TabView Example Scenario, and to re-enable the example Scenarios that used the old TabView that were previously commented out/disabled in this PR.

### `Tab`

```cs
public class Tab : View
{
    public Tab
    {
        Canfocus = true;
        SuperViewRendersLineCanvas = true;
        BorderStyle = LineStyle.Rounded;
        Border.Settings = BorderSettings.Tab | BorderSettings.Title;

        // here for example only; in reality thisd would get set based on the TabView's TabSide
        Border.Thickness = new Thickness (1, 3, 1, 1);

        // This enables Views to be arranged overlapping each other with the subview order determining z-order.
        // The focused view is automatically brought to the front, so the selected tab will always render above the unselected tabs.
        Arrangement = ViewArrangement.Overlapped;
    }
}
```


### `Tabs`

```cs
public class Tabs : View, IValue<Tab?>
{
    // The `Tab`s are just subviews of `Tabs`. They can be retrieved via `Tabs.SubViews.OfType<Tab>()` or similar.

    public Side TabSide { get; set; } = Side.Top;

    // IValue<Tab?> implementation
    public Tab? Value
    {
        get => Focused as Tab;
        set
        {
            if (Focused != value)
            {
                value.SetFocus ();

                // code to raise ValueChanged event if needed etc..
            }
        }
    }

    // other properties and methods compute TabOffsets, prevent key events from reaching unfocused tabs, etc.
}
```

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
╭──────────────╮
│ T  content T1│
│ a            │
│ b            │
│ 1            │
├───╮          │
│ T │          │
│ a │          │
│ b │          │
│ 2 │          │
╰───┤          │
    │          │
    ╰──────────╯
```

**Selected = Tab2:**
```
╭───┬──────────╮
│ T │content T2│
│ a │          │
│ b │          │
│ 1 │          │
├───╯          │
│ T            │
│ a            │
│ b            │
│ 2            │
╰───╮          │
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
╭──────────────╮
│content T1  T │
│            a │
│            b │
│            1 │
│          ╭───┤
│          │ T │
│          │ a │
│          │ b │
│          │ 2 │
│          ├───╯
│          │
╰──────────╯
```

**Selected = Tab2:**
```
╭──────────┬───╮
│content T2│ T │
│          │ a │
│          │ b │
│          │ 1 │
│          ╰───┤
│            T │
│            a │
│            b │
│            2 │
│          ╭───╯
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

- make `ScrollBar._slider` public as `Scrollbar.Slider`
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

## Implementation Phases

### Phase 0: Prerequisites — Border transparency support

**BLOCKER:** `Border` does not currently support `ViewportSettingsFlags.Transparent` or `ViewportSettingsFlags.TransparentMouse`. This must be fixed first, in a separate PR.

- **GitHub Issue:** [#4834](https://github.com/gui-cs/Terminal.Gui/issues/4834) — `Border` should support `ViewportSettings.Transparent` & `ViewportSettings.TransparentMouse`
- Once #4834 is merged, verify:
  - A `Border` with `ViewportSettingsFlags.Transparent` renders only border lines/title; empty areas are transparent and underlying views show through.
  - A `Border` with `ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse` passes mouse events through transparent areas while still capturing clicks on border lines/title text.

### Phase 2: Add `Tab` and `Tabs` Views, implement "TabView" style

1. Add `Tab` and `Tabs` views as described above.
2. Unit tests for Tab/Tabs behavior, including focus management and TabOffset calculations.
3. New Scenario based on `TabViewExample` named `TabsExample` that uses the new `Tabs` and `Tab` views, with multiple tabs and content, demonstrating focus switching and TabOffset.
4. Update Scenarios that were disabled in this PR with `#if false` to use the new `Tabs` and `Tab` views instead of the old `TabView`.

### Phase 3: Polish

1. **Scrolling**: Add `TabView.ScrollOffset` subtracted from all `TabOffset` values. Headers with negative effective offset are clipped.
2. **All four sides**: Implement `Side.Bottom` (mirror of Top, `Thickness.Bottom = 3`), `Side.Left` (`Thickness.Left = N`, vertical `TabOffset`), and `Side.Right` (`Thickness.Right = N`, vertical `TabOffset`). See "Tab Style Renderings by Side" section for target visuals.
3. **Focus/Hotkeys**: Tab title `_` convention for hotkeys works via Tab's Title property. Click on header detected in Border's mouse handling.
4. **Draw order**: Selected tab should be drawn last (Z-order). May need to reorder SubViews or rely on focused-view-drawn-last.

## Open Questions

### Resolved

1. **Z-order of headers vs content**: With `Overlapped`, the selected tab is brought to the front (z-order), which means its content area could cover unselected tabs' headers. **Resolution**: This is not a problem because headers render in the **Border adornment** (Thickness.Top=3), which occupies space *above* the content Viewport. The selected tab's content renders inside its Viewport and cannot cover the Border area of sibling tabs. All Borders render to the same `LineCanvas` via `SuperViewRendersLineCanvas`.

2. **Vertical text for Side.Left/Right**: `TextDirection.TopBottom_LeftRight` already exists in the `TextDirection` enum and is supported by `TextFormatter`. Tab headers on left/right sides can use this for vertical title rendering.

3. **Migration from existing TabRow-based implementation**: Delete all existing `Tab.cs`, `TabRow.cs`, `TabView.cs`, and all tests. Start fresh with the Border-based design. Existing `TabViewVisualTests` patterns may inform new tests but won't be preserved.

## Issues Found in This Plan (To Fix)

The following errors/inconsistencies should be corrected:Borde

1. **Line ~292**: `Border.BorderStyle == BorderStyle.Tab` — `BorderStyle` is actually `LineStyle` (Rounded, Single, etc.). The Tab flag is on `BorderSettings`, not `BorderStyle`. Should be `Border.Settings.HasFlag (BorderSettings.Tab)`.

2. **Line ~337 (`Tab` constructor)**: `Canfocus = true` — typo, should be `CanFocus` (capital F).

3. **Line ~226 (`TabWidth` property)**: Contains pseudo-code `(or Height)` — `Parent?.TitleTextFormatter.FormatAndGetSize ().Width (or Height)` is not valid C#. Should be two separate expressions or a conditional based on `TabSide`.

4. **Line ~364 (`IValue<Tab?>.Value` getter)**: `Focused as Tab` won't work correctly. `View.Focused` returns the *immediate* focused subview — if a control inside a Tab has focus, `Focused` returns that control, not the Tab. Should use `SubViews.OfType<Tab> ().FirstOrDefault (t => t.HasFocus)`.

5. **Line ~584**: `Scrollbar.Slider` — inconsistent casing. The class is `ScrollBar` (capital B), not `Scrollbar`.

6. **Line ~233**: `ViewportSettingsFlags.TransparentMouse` — should clarify this goes on the **Border adornment's** `ViewportSettings`, not the Tab view's ViewportSettings.

7. **Line ~604**: `ViewportSettingsFlags.MouseEventsTransparent` — the actual flag name is `ViewportSettingsFlags.TransparentMouse` (used elsewhere in the codebase, e.g. `Arrangement.cs`).

8. **Phase 3 scope**: Phase 3 ("Polish") bundles scrolling, all four sides, focus/hotkeys, and draw order — this is very large and should probably be split into 3-4 separate phases.

