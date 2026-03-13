# TabView via Border Tab Style — Design Plan

## Concept

Instead of a `TabRow` containing dynamically-created header views, make **Border itself** support a "tab style" rendering mode. When enabled, a View's Border renders a small **tab header rectangle** (containing the View's `Title`) at an offset along any side (top, bottom, left, or right). A `TabView` is then a simple superview of `Tab` views whose Borders all use tab style — TabView computes each tab's offset so headers sit side-by-side.

**The critical design constraint:** all Tab views must share a single `LineCanvas` via `SuperViewRendersLineCanvas = true`. When adjacent tab headers overlap by one column, LineCanvas **auto-joins** the intersecting border lines into correct junction glyphs (`┬`, `┴`, `╮`, `╰`, etc.), producing the flowing connected style with zero manual line-drawing:

```
╭───┬───╮
│T1 │T2 │
│   ╰───┴───╮
│content     │
╰────────────╯
```

This eliminates `TabRow` entirely and makes the "tab" concept a **first-class Border capability**.

## How Border Renders Titles Today

**Definitive reference:** `Tests/UnitTests/View/Adornment/BorderTests.cs`

Title rendering is controlled by `Border.Thickness.Top`. The value determines how many rows the Border's top adornment occupies, and how the title is positioned within those rows. All examples below show a View with `Title = "1234"` and `Border.Thickness = (1, N, 1, 1)` where N is the `Thickness.Top` value.

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

## The Key Change for Tab Mode

When `BorderSettings.Tab` is set on a View with `Thickness.Top == 3`:

**Selected tab** — suppress the bottom line at `topTitleLineY + 2` (Border.cs lines 382-386). The title rectangle becomes open-bottomed, flowing into the content area. Draw the content border's top line in segments around the header gap instead.

**Unselected tab** — the bottom line IS drawn (closed header). It auto-joins with the selected tab's content top segments on the shared LineCanvas.

The existing `Thickness.Top == 3` code path does 90% of the work. The changes are surgical:
1. Offset all title/line positions by `TabOffset`
2. Conditionally skip the bottom line (lines 382-386) when `IsSelectedTab`
3. Draw content top as two segments when `IsSelectedTab`
4. Skip content borders (left/right/bottom) when `!IsSelectedTab`

## Tab Style Renderings by Side

The Tab style works on all four sides. The `TabOffset` property is always along the axis of the tab strip:
- **Top/Bottom**: `TabOffset` is horizontal (columns from left edge)
- **Left/Right**: `TabOffset` is vertical (rows from top edge)

The `Thickness` on the tab side is 3 (for the 3-row/column header area). The opposite sides use 1 for the content border. The selected tab's header opens toward the content area; unselected tabs are fully closed.

### `Side.Top` — `Thickness.Top = 3`

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

At row 2 (content top / header bottom):
- Col 0: Tab1's left connector continues down as content left border → `│` (straight through)
- Col 5: Tab1's right connector (vertical from above) + content top segment (horizontal from right) → `╯`
- Col 5: Also Tab2's left connector bottom + Tab2's bottom line start → these auto-join with the `╯`
- Col 6-9: Tab2's bottom line (closed, unselected)
- Col 10: Tab2's right connector (vertical from above) + Tab2's bottom line (horizontal from left) + content top right segment (horizontal going right) → `┴`
- Col 11-16: Content top right segment
- Col 17: Content top ends + content right border starts → `╮`

**Selected = Tab2** (second tab, TabOffset = 5):
```
╭────┬────╮
│Tab1│Tab2│              Tab2's connectors meet content top segments
╰────┤    ╰───────╮     Tab1 bottom auto-joins with content top left segment
│content for T2   │
│                 │
╰─────────────────╯
```

At row 2:
- Col 0: Content left border starts (vertical down) + content top left segment starts (horizontal right) + Tab1's bottom left corner → `╰` ... wait. Actually Tab1's bottom line starts at col 1. Content left segment starts at col 0 going right, content left border at col 0 going down → `╭`. Hmm, but nothing comes from above at col 0 since Tab1's header starts at col 1.

Let me be more precise. Tab1 is unselected (header fully closed, TabOffset=0). Tab1's header rectangle spans cols 1 to 5 at rows 0-2. Tab1's bottom line is at row 2 from col 1 to col 5. Tab2 is selected (header open, TabOffset=5). Content top left segment goes from col 0 to col 6, content top right segment from col 10 to right.

Row 2 exact auto-join:
- Col 0: content left border (down) + content top left segment (right) → `╭`
- Col 1: Tab1's left connector (from above) + Tab1's bottom line (going right) + content top left segment (passing through horizontally) → `┴`
- Col 2-4: Tab1's bottom line + content top left segment (coincident horizontal) → `─`
- Col 5: Tab1's right connector (from above) + Tab1's bottom line (from left) + content top left segment (from left) + Tab2's left connector (from above, same column) → `┤` (vertical from above, horizontal from left, nothing going right = open gap for selected tab)
- Col 6-9: gap (nothing drawn — Tab2's header bottom suppressed, content top left segment ended at col 5)
- Col 10: Tab2's right connector (from above) + content top right segment (going right) → `╰`
- Col 11-16: content top right segment → `─`
- Col 17: content top right segment (from left) + content right border (going down) → `╮`

```
╭────┬────╮
│Tab1│Tab2│
╭┴───┤    ╰──────╮
│content for T2   │
│                 │
╰─────────────────╯
```

Hmm, the `╭` at col 0 and `┴` at col 1 look odd. In practice, if Tab1's header starts at col 1 (because the header rectangle is offset by 1 from `borderBounds.X`, matching how the existing `Thickness.Top == 3` rendering works), the content left border at col 0 just goes straight down with no interference.

**Note:** The exact junction glyphs depend on how `AddLine` calls overlap on the shared `LineCanvas`. The renderings above show the *intent* — actual glyphs will be determined by LineCanvas auto-join resolution during implementation. The important visual properties are:
- Selected tab's header is open at the bottom, flowing into the content area
- Unselected tabs' headers are fully closed
- Adjacent tab headers share edges and auto-join at overlapping columns
- The content top line runs in segments around the selected tab's header gap

### `Side.Bottom` — `Thickness.Bottom = 3`

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
╰──────╮    ╭────╯
│Tab1│Tab2│
╰────┴────╯
```

Same auto-join mechanics, vertically mirrored:
- `Thickness.Bottom = 3` provides 3 rows at bottom for headers
- Selected tab suppresses its TOP line (the line adjacent to content)
- Content bottom line drawn in segments around the header gap
- Headers overlap by 1 row at shared edges → auto-join produces `┬`/`┴`

### `Side.Left` — `Thickness.Left = widest_tab_title + 2`

Tab headers stacked vertically on the left edge. `TabOffset` is vertical (rows from top). Headers overlap by 1 row at shared horizontal edges. Selected tab's RIGHT line (closest to content) is suppressed.

**Selected = Tab1:**
```
╭──────╮
│ Tab1 ├──────────╮
├──────┤          │
│ Tab2 │ content  │
╰──────┴──────────╯
```

**Selected = Tab2:**
```
╭──────╮
│ Tab1 │
├──────╮
│ Tab2 ├──────────╮
╰──────┴──────────╯
                    (content to the right of Tab2's open right side)
```

How it works:
- `Thickness.Left` = title width + 2 (e.g., 8 for " Tab1 " + borders)
- `TabOffset` is the vertical row offset for each header
- Headers overlap by 1 row → auto-join at shared horizontal edge → `├` / `┤`
- Selected tab: right side open (no right vertical line on header), content left border drawn in segments
- Unselected tab: right side closed
- Content top/bottom auto-join with header corners

### `Side.Right` — `Thickness.Right = widest_tab_title + 2`

Mirror of Left. Tab headers stacked vertically on the right edge. Selected tab's LEFT line (closest to content) is suppressed.

**Selected = Tab1:**
```
          ╭──────╮
╭─────────┤ Tab1 │
│         ├──────┤
│ content │ Tab2 │
╰─────────┴──────╯
```

**Selected = Tab2:**
```
          ╭──────╮
          │ Tab1 │
          ╭──────┤
╭─────────┤ Tab2 │
╰─────────┴──────╯
```

Same mechanics as Left, horizontally mirrored.

### Summary: Which Thickness and Which Line Gets Suppressed

| Side | Thickness with value 3 | Selected tab suppresses | TabOffset axis | Content border segments |
|------|----------------------|------------------------|----------------|------------------------|
| Top | `Thickness.Top = 3` | Bottom line of header | Horizontal | Top line of content, split around header |
| Bottom | `Thickness.Bottom = 3` | Top line of header | Horizontal | Bottom line of content, split around header |
| Left | `Thickness.Left = N` | Right line of header | Vertical | Left line of content, split around header |
| Right | `Thickness.Right = N` | Left line of header | Vertical | Right line of content, split around header |

For Top/Bottom, the thickness is always 3 (fixed: top line, title row, bottom line). For Left/Right, the thickness equals the widest tab title + 2 (variable, to accommodate title text width).

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

## New API Surface

### 1. `BorderSettings.Tab` (new flag)

```csharp
[Flags]
public enum BorderSettings
{
    None = 0,
    Title = 1,
    Gradient = 2,
    Tab = 4,        // NEW: Renders a tab header rectangle at TabOffset
}
```

When `Tab` is set:
- Border renders a **tab header** at `TabOffset` columns from the left, using the existing `Thickness.Top == 3` rendering path
- The header contains the View's `Title` with top line, vertical connectors, and title text
- `Title` flag behavior is implied
- `Thickness.Top` must be 3 (3 rows: header top, title+connectors, content top / header bottom)
- The bottom line of the title rectangle (Border.cs lines 382-386) is suppressed for the selected tab
- The Border area outside the header rectangle is transparent (mouse + visual)

### 2. `Border.TabOffset` (new property)

```csharp
/// <summary>
///     Gets or sets the horizontal offset (in columns) at which the tab header
///     rectangle starts. Only effective when <see cref="Settings"/> includes
///     <see cref="BorderSettings.Tab"/>.
/// </summary>
public int TabOffset { get; set; }
```

### 3. `Border.TabWidth` (read-only, computed)

```csharp
/// <summary>
///     Gets the rendered width of the tab header rectangle (title width + 2 for
///     left/right border chars). Only meaningful when <see cref="Settings"/>
///     includes <see cref="BorderSettings.Tab"/>.
/// </summary>
public int TabWidth => (Parent?.TitleTextFormatter.FormatAndGetSize ().Width ?? 0) + 2;
```

### 4. `Border.IsSelectedTab` (new property)

```csharp
/// <summary>
///     Gets or sets whether this tab is the selected (active) tab. When true,
///     the header bottom is open and content borders are drawn. When false,
///     only the closed header rectangle is drawn. Only effective when
///     <see cref="Settings"/> includes <see cref="BorderSettings.Tab"/>.
/// </summary>
public bool IsSelectedTab { get; set; }
```

## What Border Draws in Tab Mode

Tab mode builds on the existing `Thickness.Top == 3` rendering path. The three rows are:
- **Row 0** (`topTitleLineY`): Header top horizontal line
- **Row 1** (`titleY`): Title text + vertical connectors (the `┤` and `├` flanking the title)
- **Row 2** (`topTitleLineY + 2` = `borderBounds.Y`): The main border row / content top

### Existing Code Involved (Border.cs)

The `Thickness.Top == 3` path (lines 289-295) sets:
```csharp
topTitleLineY = borderBounds.Y - 2;  // row 0
titleY = topTitleLineY + 1;           // row 1
titleBarsLength = 3;                   // vertical connectors span 3 rows
sideLineLength++;                      // extend side borders up
```

Then (lines 374-387) draws two horizontal lines:
- **Line at `topTitleLineY`** (row 0): top of title rectangle — `╭────╮`
- **Line at `topTitleLineY + 2`** (row 2): bottom of title rectangle — `╰────╯` ← **THIS is suppressed in Tab mode for selected tab**

The vertical connectors (lines 394-401) span `titleBarsLength = 3`, connecting top through title to bottom — `┤` and `├` (or `╡`/`╞` for Double).

### Selected Tab (`IsSelectedTab = true`)

The `Thickness.Top == 3` rendering with the bottom line suppressed, offset by `TabOffset`:

```
     ╭────╮              ← row 0: header top at TabOffset (existing line 376, offset)
╭────┤Tab1├──────────╮   ← row 1: main border left + connector + title + connector + right
│                     │   ← row 2: content top in segments (bottom line SUPPRESSED)
│ content             │
╰─────────────────────╯
```

Lines drawn to `Parent.LineCanvas`:
1. **Header top** (row 0): `AddLine (horizontal)` at `(borderBounds.X + 1 + TabOffset, topTitleLineY)`, width = `TabWidth` — existing line 376, offset by `TabOffset`
2. **Row 1 — left of title**: `AddLine (horizontal)` from `borderBounds.X` to left connector — existing line 391, but starting at `borderBounds.X` and extending to `TabOffset + 1`
3. **Left connector** (rows 0-2): `AddLine (vertical)` at `(borderBounds.X + 1 + TabOffset, topTitleLineY)`, length = 3 — existing line 394, offset
4. **Right connector** (rows 0-2): Same, at right edge of title — existing line 397, offset
5. **Row 1 — right of title**: `AddLine (horizontal)` from right connector to right edge — existing line 404, offset
6. **Title text**: `TitleTextFormatter.Draw` at row 1, offset by `TabOffset` — existing line 317, offset
7. **Row 2 — bottom of title rectangle: SUPPRESSED** (skip lines 382-386)
8. **Row 2 — content top segments**: Two horizontal segments that skip the header gap:
   - Left: from `borderBounds.X` to `borderBounds.X + TabOffset + 1`
   - Right: from `borderBounds.X + TabOffset + TabWidth` to right edge
9. **Content left, right, bottom**: Normal border drawing (existing code, unchanged)

The connectors (step 3-4) extend down to row 2. The content top segments (step 8) start/end at the same X positions. LineCanvas auto-joins them into `╯` and `╰` (Rounded) or `┘`/`└` (Single).

### Unselected Tab (`IsSelectedTab = false`)

Border draws the header rectangle fully closed, but skips content borders:

```
     ╭────╮         ← row 0: header top
     │Tab2│         ← row 1: connectors + title (NO full-width line — just the header portion)
     ╰────╯         ← row 2: header bottom DRAWN (existing lines 382-386)
```

Lines drawn:
1. **Header top** (row 0): Same as selected
2. **Left/right connectors**: Same as selected
3. **Title text**: Same as selected
4. **Header bottom** (row 2): `AddLine (horizontal)` at `(TabOffset, row 2)` — existing lines 382-386 (KEPT for unselected)
5. **Row 1 — full-width line**: NOT drawn (only the header portion at TabOffset)
6. **No content borders** — not the selected tab

The header bottom line (row 2) sits on the same Y as the selected tab's content top segments. On the shared LineCanvas, they auto-join at intersection points (producing `┴` where the unselected tab's bottom-right meets the content top line, `╰` at the bottom-left, etc.).

## Tab Visibility Model

All Tab views remain **`Visible = true`** at all times — their Borders must draw headers regardless of selection. Content hiding is achieved by:

- **Selected tab**: SubViews visible, viewport renders normally
- **Unselected tabs**: `ViewportSettings |= Transparent` — viewport not cleared, SubViews not drawn. Only the Border (header) renders.

This replaces the current Wizard pattern (`Visible = false` on non-selected tabs) with a transparency-based approach that keeps borders active.

## TabView Architecture

```csharp
public class TabView : View
{
    public TabView ()
    {
        SuperViewRendersLineCanvas = true;
        // TabView has NO border of its own — selected tab provides the content frame
        Border!.Thickness = new Thickness (0);
        BorderStyle = LineStyle.None;
    }
}
```

Each `Tab` fills the entire `TabView`:
```csharp
tab.X = 0;
tab.Y = 0;
tab.Width = Dim.Fill ();
tab.Height = Dim.Fill ();
tab.SuperViewRendersLineCanvas = true;
tab.BorderStyle = LineStyle.Rounded;
tab.Border!.Settings = BorderSettings.Tab | BorderSettings.Title;
tab.Border!.Thickness = new Thickness (1, 3, 1, 1);
// Thickness.Top = 3 → row 0 = header top, row 1 = title+connectors, row 2 = content top/header bottom
// Thickness.Left/Right/Bottom = 1 → content frame (only drawn when selected)
```

### Tab Offset Calculation

```csharp
void UpdateTabOffsets ()
{
    int offset = 0;

    foreach (Tab tab in Tabs)
    {
        tab.Border!.TabOffset = offset;
        offset += tab.Border.TabWidth - 1;  // -1 for shared-edge overlap → auto-join
    }
}
```

### Selection Update

```csharp
void UpdateSelection ()
{
    foreach (Tab tab in Tabs)
    {
        bool isSelected = tab == SelectedTab;
        tab.Border!.IsSelectedTab = isSelected;

        if (isSelected)
        {
            // Normal rendering — content visible
            tab.ViewportSettings &= ~ViewportSettingsFlags.Transparent;
        }
        else
        {
            // Transparent — only Border (header) draws, content hidden
            tab.ViewportSettings |= ViewportSettingsFlags.Transparent;
        }
    }
}
```

### What Gets Eliminated

| Current | New |
|---------|-----|
| `TabRow.cs` (~327 lines) | Deleted |
| Dynamic header View creation | Not needed — Border draws headers |
| `UpdateHeaderAppearance()` | Replaced by `IsSelectedTab` toggle |
| `UpdateBorderGaps()` | Not needed — content top line drawn in segments |
| Continuation line drawing | Natural — it's just the right segment of the content top line |
| `EnsureHeaderVisible()` + scroll infra | Simplified to base offset arithmetic |

### Continuation Line

The "continuation line" (from last tab to right edge) is simply the **right segment of the selected tab's content border top line** (step 8 in the rendering section). It starts at the last tab's right edge and extends to the view's right border. LineCanvas auto-joins the endpoint with the right vertical border to produce `╮`.

## Implementation Phases

### Phase 1: Extend Border with Tab Support

**Files:** `BorderSettings.cs`, `Border.cs`

1. Add `BorderSettings.Tab = 4`
2. Add `Border.TabOffset` (int), `Border.TabWidth` (computed), `Border.IsSelectedTab` (bool)
3. In `OnDrawingContent`, when `Tab` is set:
   - Draw header rectangle at `TabOffset` (top, left, right, and conditionally bottom)
   - Draw title text inside header
   - If `IsSelectedTab`: draw content top line in segments around the header, plus left/right/bottom normally
   - If `!IsSelectedTab`: skip content borders entirely (only draw the header rectangle)
   - Handle transparency for the header area
4. Unit tests for Border tab rendering in isolation (in `UnitTestsParallelizable`)

### Phase 2: Simplify TabView

**Files:** `TabView.cs`, `Tab.cs`; **Delete:** `TabRow.cs`

1. Delete `TabRow`
2. Update `Tab` to configure its Border for tab mode
3. Rewrite `TabView`:
   - No `_tabRow`, no Padding manipulation
   - `UpdateTabOffsets()` computes offsets
   - `UpdateSelection()` toggles `IsSelectedTab` + transparency
   - Same keyboard commands (Left/Right/Home/End)
   - Mouse: Border detects clicks in header rectangle, TabView routes to tab selection
4. Update all tests

### Phase 3: Migrate BorderTests to Parallelizable

**Goal:** Move `Tests/UnitTests/View/Adornment/BorderTests.cs` to `Tests/UnitTestsParallelizable/ViewBase/Adornment/` and eliminate all `[AutoInitShutdown]` / `Application.Init` dependencies.

**Current state of Border test files:**

| File | Location | Parallel-safe? | Pattern |
|------|----------|---------------|---------|
| `BorderTests.cs` | `UnitTests/View/Adornment/` | **No** — uses `[AutoInitShutdown]`, `Application.Begin/End` | Legacy |
| `BorderGapTests.cs` | `UnitTestsParallelizable/Drawing/` | **Yes** — extends `TestDriverBase` | Modern |
| `BorderArrangementKeyboardTests.cs` | `UnitTestsParallelizable/ViewBase/Adornment/` | **Yes** — no static state | Modern |
| `BorderArrangementTests.cs` | `UnitTestsParallelizable/ViewBase/Adornment/` | **Yes** — uses `IApplication.Create()` | Modern |

**Migration steps:**

1. **Create `Tests/UnitTestsParallelizable/ViewBase/Adornment/BorderTests.cs`** with the migrated tests
2. **Convert `[AutoInitShutdown]` tests** to use `TestDriverBase` pattern (like `BorderGapTests.cs`):
   - Extend `TestDriverBase`
   - Create driver via `CreateTestDriver()`
   - Set `view.Driver = driver` directly
   - Use `BeginInit()`/`EndInit()` + `Draw()` instead of `Application.Begin()`
   - Use `DriverAssert.AssertDriverContentsAre()` for assertions
3. **Convert `[SetupFakeApplication]` tests** — these are closer to parallel-safe already:
   - Replace `ApplicationImpl.Instance.Driver` with `CreateTestDriver()`
   - Remove `[SetupFakeApplication]` attribute
4. **Preserve all existing visual assertions** — the ASCII art expectations are the spec
5. **Add `// Claude - Opus 4.5` comment** to migrated file
6. **Delete the original** `UnitTests/View/Adornment/BorderTests.cs`
7. **Verify** no coverage decrease

**Tests to migrate (from `BorderTests.cs`):**

| Test | Current Pattern | Migration Notes |
|------|----------------|-----------------|
| `Border_Parent_HasFocus_Title_Uses_FocusAttribute` | `[SetupFakeApplication]` | Replace `ApplicationImpl.Instance.Driver` → `CreateTestDriver()` |
| `Border_Uses_Parent_Scheme` | `[SetupFakeApplication]` | Same as above |
| `Border_With_Title_Border_Double_Thickness_Top_Four_Size_Width` | `[AutoInitShutdown]`, `Application.Begin` | Rewrite: `TestDriverBase`, `driver.SetScreenSize()`, manual init |
| `Border_With_Title_Border_Double_Thickness_Top_Three_Size_Width` | `[AutoInitShutdown]`, `Application.Begin` | Same |
| `Border_With_Title_Border_Double_Thickness_Top_Two_Size_Width` | `[AutoInitShutdown]`, `Application.Begin` | Same |
| `Border_With_Title_Size_Height` | `[AutoInitShutdown]`, `Application.Begin` | Same |
| `Border_With_Title_Size_Width` | `[AutoInitShutdown]`, `Application.Begin` | Same |
| `FrameToScreen_NestedSuperView_WithBorder` | No static state | Already parallel-safe — move as-is |
| `FrameToScreen_SuperView_WithBorder` | No static state | Already parallel-safe — move as-is |
| `HasSuperView` | `[AutoInitShutdown]`, `Application.Begin` | Rewrite |
| `HasSuperView_Title` | `[AutoInitShutdown]`, `Application.Begin` | Rewrite |
| `NoSuperView` | `[AutoInitShutdown]`, `Application.Begin` | Rewrite |
| `View_BorderStyle_Defaults` | No static state | Already parallel-safe — move as-is |
| `View_SetBorderStyle` | No static state | Already parallel-safe — move as-is |
| `SuperViewRendersLineCanvas_No_SubViews_AutoJoinsLines` | `[SetupFakeApplication]` | Replace driver pattern |
| `SuperViewRendersLineCanvas_Title_AutoJoinsLines` | `[SetupFakeApplication]` | Replace driver pattern |

**Migration pattern example** — converting from `[AutoInitShutdown]` to `TestDriverBase`:

```csharp
// BEFORE (in UnitTests, non-parallel):
[Theory]
[AutoInitShutdown]
[InlineData (8)]
public void Border_With_Title_Size_Width (int width)
{
    var win = new Window { Title = "1234", Width = Dim.Fill (), Height = Dim.Fill () };
    SessionToken rs = Application.Begin (win);
    Application.Driver!.SetScreenSize (width, 3);
    AutoInitShutdownAttribute.RunIteration ();

    var expected = @"
┌┤1234├┐
│      │
└──────┘";

    _ = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
    Application.End (rs);
    win.Dispose ();
}

// AFTER (in UnitTestsParallelizable, parallel-safe):
[Theory]
[InlineData (8)]
public void Border_With_Title_Size_Width (int width)
{
    IDriver driver = CreateTestDriver ();
    driver.SetScreenSize (width, 3);

    Window win = new ()
    {
        Driver = driver,
        Title = "1234",
        Width = Dim.Fill (),
        Height = Dim.Fill ()
    };
    win.BeginInit ();
    win.EndInit ();
    win.SetRelativeLayout (new Size (width, 3));
    win.Layout ();
    win.Draw ();

    var expected = @"
┌┤1234├┐
│      │
└──────┘";

    DriverAssert.AssertDriverContentsAre (expected, output);
    win.Dispose ();
}
```

### Phase 4: Add Tab Mode Tests

**File:** `Tests/UnitTestsParallelizable/ViewBase/Adornment/BorderTabTests.cs`

New parallel-safe tests for the Tab mode feature, following the `TestDriverBase` pattern:

1. **`Border_Tab_Selected_NoBottomLine`** — verify that `IsSelectedTab = true` suppresses the `╰────╯` bottom line
2. **`Border_Tab_Unselected_HasBottomLine`** — verify closed header rectangle
3. **`Border_Tab_Offset_ShiftsHeader`** — verify `TabOffset` shifts header position
4. **`Border_Tab_Width_Computed`** — verify `TabWidth` = title width + 2
5. **`Border_Tab_AutoJoin_TwoTabs`** — two tabs with `SuperViewRendersLineCanvas`, verify `┬`/`┴` at shared edge
6. **`Border_Tab_Selected_ContentBorderSegments`** — verify content top line drawn in segments around header gap
7. **`Border_Tab_Unselected_NoContentBorder`** — verify no left/right/bottom for unselected tab
8. **`Border_Tab_ContinuationLine`** — verify right segment of content top extends to right edge and auto-joins to `╮`
9. **`Border_Tab_Side_Top`** — verify rendering with `Thickness.Top == 3` (primary case)
10. **`Border_Tab_Side_Bottom`** — verify mirrored rendering with `Thickness.Bottom == 3`
11. **`Border_Tab_Side_Left`** — verify vertical tab strip with `Thickness.Left == N`, vertical `TabOffset`
12. **`Border_Tab_Side_Right`** — verify vertical tab strip with `Thickness.Right == N`, vertical `TabOffset`

### Phase 5: Polish

1. **Scrolling**: Add `TabView.ScrollOffset` subtracted from all `TabOffset` values. Headers with negative effective offset are clipped.
2. **All four sides**: Implement `Side.Bottom` (mirror of Top, `Thickness.Bottom = 3`), `Side.Left` (`Thickness.Left = N`, vertical `TabOffset`), and `Side.Right` (`Thickness.Right = N`, vertical `TabOffset`). See "Tab Style Renderings by Side" section for target visuals.
3. **Focus/Hotkeys**: Tab title `_` convention for hotkeys works via Tab's Title property. Click on header detected in Border's mouse handling.
4. **Draw order**: Selected tab should be drawn last (Z-order). May need to reorder SubViews or rely on focused-view-drawn-last.
5. Visual tests.

## Open Questions

1. **Mouse hit testing**: All tabs are full-sized overlapping views. Clicking an unselected tab's header might hit the selected tab first (it's on top). Solutions: (a) make the selected tab's Border `TransparentMouse` in the header row outside its own header rectangle, or (b) have TabView override `OnMouseEvent` to check coordinates against all tabs' `TabOffset`/`TabWidth` ranges.

2. **Draw order for overlapping tabs**: The selected tab must render its content border ON TOP of unselected tabs' transparent viewports. This likely requires the selected tab to be the last-drawn SubView.

3. **Unselected tab viewport clearing**: Even with `Transparent`, if multiple unselected tabs draw their headers at different offsets, they must not interfere with each other. Since headers are drawn via `LineCanvas` (not viewport), and the viewport is transparent (not cleared), this should work — but needs verification.

4. **Minimal code path changes**: The `Tab` flag leverages the existing `Thickness.Top == 3` code path almost entirely. The only changes are: (a) offset the title/lines by `TabOffset`, (b) conditionally suppress the bottom line at `topTitleLineY + 2` when `IsSelectedTab`, (c) draw content top line in segments when `IsSelectedTab`, and (d) skip content borders when `!IsSelectedTab`. This keeps the change surgical rather than a separate code path.
