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

**Status: Implemented.** `BorderSettings.Tab`, `Border.TabSide`, `Border.TabOffset`, `Border.TabLength`, `TabHeaderRenderer`, `BorderView` integration, `BorderEditor` UI, and visual tests are all in place. The `TabLength` auto-computation bug (`+1` instead of `+2`) has been fixed.

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
- **`BorderSettings.Title` and `BorderSettings.Tab` are not mutually exclusive.** When both are set, the `Title` is displayed within the tab header. When only `Tab` is set (without `Title`), no title text is drawn in the header — it is an empty tab rectangle.
- When `Tab` is set and `Title` is not set, `TabLength` defaults to `2` (0 space for content + 2 vertical border lines).
- When `Tab` and `Title` are both set, `TabLength` auto-computes as `Title.GetColumns() + 2` (title text width + 2 border lines).
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

### New `BorderView.TabWidth` (implemented as `Border.TabLength`)

```csharp
/// <summary>
///     Gets or sets the total length of the tab parallel to the border edge (including border cells).
///     If null, the length will be determined based on View.Title.
///     Only used when BorderSettings.Tab is set.
/// </summary>
public int? TabLength
{
    get
    {
        if (field is null && Settings.HasFlag (BorderSettings.Tab))
        {
            int titleColumns = Parent?.Title?.GetColumns () ?? 0;
            // Two vertical border lines + title text width
            return titleColumns + 2;
        }
        return field;
    }
    set;
}
```

**Note:** The auto-computed default is `Title.GetColumns() + 2` (two vertical border lines flanking the title). When `Title` is empty/null and `Tab` is set, `TabLength` defaults to `2` (just the two border lines, 0 content). An earlier implementation incorrectly used `+ 1`; this was fixed.

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

These examples show `Side.Top`. All four sides follow the same pattern — visual examples for each `Side` variant are below.

#### Visual Examples by `TabSide` (Phase 1 reference)

All examples use `Thickness = 3` on the tab side, `Thickness = 1` on the other three sides, `BorderStyle = Rounded`, and `TabOffset = 0`.

**`HasFocus` determines whether the header is open or closed:**

- **`HasFocus == false`** → **Closed header.** The line adjacent to the content area is **drawn**, fully enclosing the header rectangle. This visually separates the header from the content, indicating that this tab is not selected.
- **`HasFocus == true`** → **Open header.** The line adjacent to the content area is **suppressed**, creating a gap that visually connects the header to the content area. This open flow indicates the tab is selected and its content is active.

In a multi-tab `Tabs` container, exactly one `Tab` has `HasFocus == true` (the selected tab) and all others have `HasFocus == false`. The visual difference — closed vs. open — is how the user distinguishes the selected tab from the rest.

##### Precise Rendering Spec for `Side.Top` (other sides follow by rotation)

This spec describes exactly how to draw a single tab with `BorderSettings.Tab`, `TabSide = Side.Top`, `Thickness.Top = 3`, `Thickness.Left = Thickness.Right = Thickness.Bottom = 1`. The title text is `"Tab"` (3 characters). These rules apply to all `Side` variants via rotation.

**Dimensions and coordinates:**

- **Tab header width** = title text columns + 2 (one border column each side). For `"Tab"` → `3 + 2 = 5`.
- **Tab header height** = `Thickness.Top` = 3 rows (top line, title row, closing/opening line).
- **Content area** = the remaining rectangle below the header, bounded by `Thickness.Left`, `Thickness.Right`, `Thickness.Bottom` (all = 1).
- **View total width** = content width + `Thickness.Left` + `Thickness.Right`. For `"content"` (7 chars) → `7 + 1 + 1 = 9`.
- **View total height** = content height + `Thickness.Top` + `Thickness.Bottom`. For 2-line content → `2 + 3 + 1 = 6`.

**Text placement:**

- Tab title text is placed **tight against borders** with no padding. The title occupies every cell between the left and right borders of the header. `│Tab│`, never `│ Tab │` or `│Tab │`.
- For `Side.Left`/`Side.Right`, text is rendered vertically using `TextDirection.TopBottom_LeftRight`, one character per row, still tight: `│T│` on each row, not `│ T │`.

**The three rows of the tab header (`Side.Top`):**

- **Row 0 (header top):** `╭` at header left edge, `───` spanning header width − 2, `╮` at header right edge. Positioned at `TabOffset` columns from the left. Columns outside the header on this row are empty (transparent).
- **Row 1 (title):** `│` at header left edge, title text filling the interior, `│` at header right edge. Columns outside the header on this row are empty (transparent).
- **Row 2 (junction / opening row) — depends on `HasFocus`:**

  **`HasFocus == false` (closed):** This row spans the FULL view width. It is the content area's top border with the header's closing line merged in via LineCanvas auto-join:

  - If `TabOffset == 0`: The header left edge coincides with the content left border. The glyph is `├` (T-junction: the continuous left border meets the header closing line going right). Then `───` fills the header interior, `┴` at the header right edge (bottom-T: the header's right border going up meets the horizontal going both ways). Then `───` continues as the content top border to `╮` at the view's right edge.
  - If `TabOffset > 0`: `╭` at x=0 (content top-left corner), then `─` fills to the header left edge where a `┴` appears (content top line meets header left border coming down). Then `───` fills the header interior, another `┴` at the header right edge. Then `───` continues to `╮` at the view's right edge. If the header right edge IS the view right edge, the final glyph is `┤` instead of `╮`.

  **`HasFocus == true` (open):** The header's closing line is SUPPRESSED. This row has:

  - `│` at x=0 (the left border continues down from the header), then spaces filling the header interior (the open gap), then `╰` at the header right edge (the header right border curves into the continuation line), then `───` continues to `╮` at the view's right edge.
  - If `TabOffset > 0`: `│` at x=0 continues from above, spaces fill to header left edge where `╯` appears (header left border curves to meet continuation from left). Continue with spaces through the header gap. At the header right edge, `╰` curves into `───` continuing to `╮`. (This case applies to multi-tab layouts managed by `Tabs`.)

**Remaining content rows:**

- **Left border:** `│` at x=0 on every row from row 2 (or row 3 for closed) down to the bottom border row.
- **Right border:** `│` at x=width−1 on every content row.
- **Bottom border:** `╰` at x=0, `───` filling, `╯` at x=width−1.
- **Content text** fills the interior of the content rectangle.

**Key invariant:** The view's left border is ONE continuous vertical line from the top of the header (`╭` at row 0) through every content row down to `╰` at the bottom. It is never broken. When the header is at `TabOffset = 0`, the junction where the closed header's bottom line meets this border is `├` (a T-junction), not `╭` followed by a separate junction. The border is continuous — it does not restart.

**Whitespace rule:** Cells outside the header rectangle on rows 0 and 1 are **transparent** (not drawn). They are not spaces — they literally don't exist in the output, allowing underlying views to show through. Inside the header, text is tight against borders with zero padding.

##### `Side.Top` — `Thickness.Top = 3`

`HasFocus == false` (closed — header bottom line drawn):
```
╭───╮
│Tab│
├───┴───╮
│content│
╰───────╯
```

`HasFocus == true` (open — header bottom line suppressed, flows into content):
```
╭───╮
│Tab│
│   ╰───╮
│content│
╰───────╯
```

##### `Side.Bottom` — `Thickness.Bottom = 3`

`HasFocus == false` (closed — header top line drawn):
```
╭───────╮
│content│
├───┬───╯
│Tab│
╰───╯
```

`HasFocus == true` (open — header top line suppressed, flows into content):
```
╭───────╮
│content│
│   ╭───╯
│Tab│
╰───╯
```

##### `Side.Left` — `Thickness.Left = 3`

Tab text is rendered vertically using `TextDirection.TopBottom_LeftRight`.

`HasFocus == false` (closed — header right line drawn):
```
╭─┬───────╮
│T├content│
│a│       │
│b│       │
╰─┴───────╯
```

`HasFocus == true` (open — header right line suppressed, flows into content):
```
╭─────────╮
│T content│
│a        │
│b        │
╰─────────╯
```

##### `Side.Right` — `Thickness.Right = 3`

Tab text is rendered vertically using `TextDirection.TopBottom_LeftRight`.

`HasFocus == false` (closed — header left line drawn):
```
╭───────┬─╮
│content│T│
│       │a│
│       │b│
╰───────┴─╯
```

`HasFocus == true` (open — header left line suppressed, flows into content):
```
╭─────────╮
│content T│
│        a│
│        b│
╰─────────╯
```

##### Summary: Which Thickness Side = 3, Which Line Gets Suppressed

| `TabSide` | Thickness = 3 on | `HasFocus == true` suppresses | `HasFocus == false` draws | TabOffset axis |
|-----------|------------------|-------------------------------|---------------------------|----------------|
| Top       | `Thickness.Top`   | Bottom line of header         | Bottom line (closed)      | Horizontal     |
| Bottom    | `Thickness.Bottom` | Top line of header           | Top line (closed)         | Horizontal     |
| Left      | `Thickness.Left`  | Right line of header          | Right line (closed)       | Vertical       |
| Right     | `Thickness.Right` | Left line of header           | Left line (closed)        | Vertical       |

(HasFocus == true depicted):

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

### TabOffset

Another new property `BorderView.TabOffset` (int) specifies how many columns/rows from the left/top edge the tab header starts.

The `TabOffset` property is always along the axis of the tab strip:
- **Top/Bottom**: `TabOffset` is horizontal (columns from left edge)
- **Left/Right**: `TabOffset` is vertical (rows from top edge)

For `Border.BorderStyle == BorderStyle.Tab`, `BorderView.TabSide = Side.Top`, `Border.Thickness.Top = 3` and `BorderView.TabOffset = 2`, the rendering would look like this (HasFocus == false depicted):
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

For completeness if `TabOffset = -2`, causing the left side of the tab to extend beyond the right-side's border line:
```
──╮
ab│
╭─┴─────╮
│content│
│       │
╰───────╯
```


For completeness if `TabOffset = -4`, causing the left side of the tab to extend beyond the right-side's border line:
```
╮
│
├───────╮
│content│
│       │
╰───────╯
```

For completeness if `TabOffset = -5`, causing the left side of the tab to extend beyond the right-side's border line:
```


╭───────╮
│content│
│       │
╰───────╯
```

IOW, it gets clipped by the view's right border, but the header is still visible and functional. The content top line auto-joins with the tab header's right connector at the intersection point, producing a flowing style.

**Important** the renderings above are what happens when the View *does not have focus* (`HasFocus == false`). When a View is using `BorderSettings.Tab`, focus is indicated not by the `Title` being rendered using the focus attribute, but by the presence of the header bottom line. The selected tab has its header bottom line suppressed, creating an open gap that visually connects the header to the content area. Unselected tabs draw the full header rectangle, including the bottom line, creating a closed header.

#### `Side.Bottom` — `TabOffset` examples (`HasFocus == false`)

Same axis as Top (horizontal), but mirrored vertically. The junction row is the **content bottom border**. Junctions are `┬` (top-T: content bottom line meets header border going down).

`TabOffset = 2`:
```
╭───────╮
│content│
│       │
╰─┬───┬─╯
  │Tab│
  ╰───╯
```

`TabOffset = 4` (header right edge coincides with view right border → `┤`):
```
╭───────╮
│content│
│       │
╰───┬───┤
    │Tab│
    ╰───╯
```

`TabOffset = 5` (overflow right — header clipped):
```
╭───────╮
│content│
│       │
╰────┬──╯
     │Tab
     ╰───
```

`TabOffset = -1` (overflow left — header clipped):
```
╭───────╮
│content│
│       │
╰──┬────╯
Tab│
───╯
```

#### `Side.Left` — `TabOffset` examples (`HasFocus == false`)

`TabOffset` is **vertical** (rows from the top edge). The junction column is column 2 (the inner edge of `Thickness.Left = 3`). Junctions are `┤` (left-T: content left border continues vertically, horizontal goes left into header). Rows outside the header on columns 0–1 are transparent.

**Header height** = title text length + 2 (top/bottom border rows). For `"Tab"` → `3 + 2 = 5` rows.

**Key behavior:** The header occupies only its 5 rows on the left side. Below (or above) the header, columns 0–1 are transparent and the content left border continues as `│` at column 2.

`TabOffset = 0` (header at top, view height = 9):
```
╭─┬───────╮
│T├content│
│a│       │
│b│       │
╰─┤       │
  │       │
  │       │
  │       │
  ╰───────╯
```

Row-by-row at the junction column (column 2): `┬` at row 0 (header top meets content top — horizontal both ways, vertical down), `├` at row 1 (header `HasFocus == false` closing line meets content left border — content border continues vertically, horizontal goes right into content), `│` on rows 2–3 (content left border, header interior rows), `┤` at row 4 (header bottom meets content left border — vertical continues, horizontal goes left into header), `│` on rows 5–7 (content left border continues), `╰` at row 8 (content bottom-left).

`TabOffset = 2` (header starts 2 rows below top, view height = 9):
```
  ╭───────╮
  │content│
╭─┤       │
│T│       │
│a│       │
│b│       │
╰─┤       │
  │       │
  ╰───────╯
```

At column 2: `╭` at row 0 (content top-left), `│` on row 1, `┤` at row 2 (header top arrives — vertical continues, horizontal goes left), `│` on rows 3–5 (header interior, content border continues), `┤` at row 6 (header bottom departs — vertical continues, horizontal goes left), `│` at row 7, `╰` at row 8 (content bottom-left).

`TabOffset = 6` (overflow bottom — header clipped, view height = 9):
```
  ╭───────╮
  │content│
  │       │
  │       │
  │       │
  │       │
╭─┤       │
│T│       │
│a╰───────╯
```

**Overflow clipping:** The header starts at row 6 (5-row header would need rows 6–10, but the view ends at row 8). Row 6: `┤` junction (header top). Row 7: `│` (header interior, `T`). Row 8: the header's `a` character shares the row with the content bottom border `╰───────╯`. Characters `b` and the header bottom border row are clipped entirely — they would be at rows 9–10 which don't exist.

#### `Side.Right` — `TabOffset` examples (`HasFocus == false`)

Mirror of Left. `TabOffset` is **vertical**. The junction column is the inner edge of `Thickness.Right = 3`. Junctions are `├` (right-T: content right border continues vertically, horizontal goes right into header). Rows outside the header on the rightmost 2 columns are transparent.

`TabOffset = 0` (header at top, view height = 9):
```
╭───────┬─╮
│content│T│
│       │a│
│       │b│
│       ├─╯
│       │
│       │
│       │
╰───────╯
```

Row-by-row at the junction column (content right border): `┬` at row 0 (header top meets content top), `│` at row 1 (header closing line — `HasFocus == false`), `│` on rows 2–3 (header interior), `├` at row 4 (header bottom departs — vertical continues, horizontal goes right), `│` on rows 5–7, `╰` at row 8 (content bottom-right... wait, this is `╯`). Correction: `╯` at row 8.

`TabOffset = 2` (header starts 2 rows below top, view height = 9):
```
╭───────╮
│content│
│       ├─╮
│       │T│
│       │a│
│       │b│
│       ├─╯
│       │
╰───────╯
```

At the content right border column: `╮` at row 0 (content top-right), `│` on row 1, `├` at row 2 (header top arrives — vertical continues, horizontal goes right), `│` on rows 3–5 (header interior), `├` at row 6 (header bottom departs — vertical continues, horizontal goes right), `│` at row 7, `╯` at row 8 (content bottom-right).

`TabOffset = 6` (overflow bottom — header clipped, `HasFocus == false`, view height = 9):
```
╭───────╮
│content│
│       │
│       │
│       │
│       │
│       ├─╮
│       │T│
╰───────╯a│
```

**Overflow clipping (Right):** Same principle as Left. The header starts at row 6 (5-row header needs rows 6–10, view ends at row 8). Row 6: `├` junction. Row 7: header interior (`T`). Row 8: `a` shares the row with content bottom border `╰───────╯`. The `b` row and header bottom border are clipped.

#### `Side.Right` — Focused overflow (`HasFocus == true`, `TabOffset = 6`, view height = 9)

```
╭───────╮
│content│
│       │
│       │
│       │
│       │
│       ╰─╮
│        T│
╰─────── a│
```

**The focused overflow nuance:** When `HasFocus == true`, the border segment between header and content is suppressed (the "open gap" rule). For this overflow case:

- **Row 6:** The `├` junction from the unfocused version becomes `╰─╮`. The content right border is suppressed — instead, `╰` (the header's top-left corner curving right) bridges into the header top border `─`, ending at `╮` (header top-right corner). This creates the visual opening where the tab connects to the content.
- **Row 7:** The content right border `│` is suppressed entirely — replaced by a space. The tab text `T` and the header right border `│` remain. The content area visually "opens" into the header.
- **Row 8:** The content bottom border `╰───────` runs normally, but where it would meet the content right corner, there is a space (the gap continues). The clipped `a` and header right border `│` continue on the right.

**General principle for focused overflow on any side:** The open gap rule applies identically whether the header fits fully or overflows. The border segment between header and content is always suppressed when focused. In overflow cases, the gap interacts with the content corner glyph — the corner is replaced by the header's outer corner glyph curving into the header, and the content border is replaced by space for the extent of the gap.

This same principle applies to `Side.Left` overflow (mirrored) and to `Side.Top`/`Side.Bottom` overflow on the horizontal axis.

### Implementation Approach: General-Purpose `TabHeaderRenderer`

Rather than implementing Side.Top first and generalizing later, we build a **standalone, side-agnostic drawing algorithm** from the start. The algorithm is parameterized by `Side` — no per-side code paths, just coordinate transforms.

**Why this works:** LineCanvas auto-join handles all junction glyph resolution. A tab header is just 3 additional line segments (the outer sides of the header rectangle) added to the same LineCanvas as the content border. For `showSeparator == true`, a 4th closing line is added and auto-join produces the junction glyphs (`├`, `┤`, `┬`, `┴`). For `showSeparator == false`, the closing line is omitted and the content border is excluded in the gap region.

**New API:**

```csharp
// Core — just the border lines. Content drawing is the caller's responsibility.
static void AddLines (LineCanvas lineCanvas,
                      Rectangle contentBorderRect,
                      Side side,
                      int offset,          // along the edge
                      int length,          // parallel to edge (total including borders)
                      int depth,           // perpendicular to edge (total including borders)
                      bool showSeparator,  // true = draw closing line with junctions; false = open gap
                      LineStyle lineStyle,
                      Attribute? lineAttribute = null);

// Public so caller knows where to position content (View, text, anything)
static Rectangle ComputeHeaderRect (Rectangle contentBorderRect,
                                    Side side, int offset,
                                    int length, int depth);

// Computes the interior content area within the header rect (excludes border cells)
static Rectangle GetContentArea (Rectangle headerRect, Rectangle clipped, Side side);
```

**Key design decisions:**

- **`showSeparator`** replaces `hasFocus` (inverted semantics). `true` = closing line drawn (junctions), `false` = open gap. Generic — caller decides why.
- **`length` and `depth`** replace `tabText`-derived sizing. Enables arbitrary-size protrusions (multi-row headers, icon+label, etc.)
- **`lineAttribute`** colors the header border lines (may differ from content border).
- **`DrawText` removed** — caller owns content rendering. Could be text, a View, custom drawing.
- **`ComputeHeaderRect` is public** so caller can position their content.
- **`Render` convenience method removed** — it coupled text drawing to border drawing.

**Core algorithm (any side):**

```
1. Compute headerRect from (side, offset, length, depth, contentBorderRect)
2. Clip headerRect to viewBounds (overflow → partial header)
3. Add 3 outer header border lines to LineCanvas (the sides NOT adjacent to content)
4. If showSeparator → add closing line (adjacent to content); auto-join gives junctions
5. If !showSeparator → split content border into two segments around the gap
6. Caller draws content inside the header rect (text, View, whatever)
```

Steps 3–5 use coordinate rotation based on `Side`: each side defines which edge is "outer", "left-perp", "right-perp", and "closing". The math is identical across all sides.

#### New files

| File | Purpose |
|------|---------|
| `Terminal.Gui/Drawing/TabHeaderRenderer.cs` | `internal static class TabHeaderRenderer` — pure geometry drawing helper. Takes `LineCanvas`, `Side`, offset, length, depth, showSeparator, `LineStyle`, `Attribute`. Adds lines and exclusions. **No text drawing, no dependency on Border/BorderView/View.** |
| `Tests/UnitTestsParallelizable/Drawing/TabHeaderRendererTests.cs` | TDD tests using `CreateTestDriver ()` + `GetCanvas ()` pattern from `LineCanvasTests`. Each test creates a LineCanvas, adds content border lines, calls `TabHeaderRenderer.AddLines ()`, draws, and asserts with `DriverAssert.AssertDriverContentsAre`. Tests verify geometry only — simple text is drawn manually by the test (not by the renderer). |

#### Test-driven development order

Tests are written first, then the algorithm is iterated until they pass. Tests use the `TestDriverBase` + `GetCanvas` pattern (no Application.Init needed).

**Round 1 — Side.Top, showSeparator == true:**
- `Tab_Top_Separator_TabOffset0` — `├───┴───╮` junction row
- `Tab_Top_Separator_TabOffset2` — `╭─┴───┴──╮` with ┴ junctions
- `Tab_Top_Separator_Overflow` — header clipped at right edge

**Round 2 — Side.Top, showSeparator == false:**
- `Tab_Top_Open_TabOffset0` — open gap, `│` continues left border
- `Tab_Top_Open_TabOffset2` — `╯` and `╰` curves with gap

**Round 3 — Side.Bottom (mirror of Top):**
- Separator and open variants, same test structure

**Round 4 — Side.Left and Side.Right:**
- Vertical headers, `┤`/`├` junctions
- Overflow clipping at bottom edge
- Open gap overflow (the gap nuance with content corner interaction)

**Round 5 — Edge cases:**
- Header wider/taller than view (fully clipped)
- Negative offset (overflow at start edge)
- Single-cell length
- Zero-content-area (length or depth == 2, i.e. borders only)

### Steps

#### 1. Create `TabHeaderRenderer` and tests (all sides)

**Files:** `Terminal.Gui/Drawing/TabHeaderRenderer.cs`, `Tests/UnitTestsParallelizable/Drawing/TabHeaderRendererTests.cs`

1. Create test file with Round 1 tests (Side.Top, showSeparator == true)
2. Create `TabHeaderRenderer` with the geometry-only API (`AddLines`, `ComputeHeaderRect`, `GetContentArea`)
3. Iterate until Round 1 tests pass
4. Add Round 2–5 tests incrementally, fixing the algorithm as needed
5. All tests use `DriverAssert.AssertDriverContentsAre` with exact expected output matching the visual examples in this spec
6. Tests verify geometry only — simple text is drawn manually by the test to confirm content positioning, not by the renderer

#### 2. Wire `TabHeaderRenderer` into `BorderView`

**Files:** `BorderSettings.cs`, `Border.cs`, `BorderView.cs`

1. Add `BorderSettings.Tab = 4`
2. Add `Border.TabSide` (`Side`, default `Side.Top`), `Border.TabOffset` (`int`), `Border.TabText` (`string`)
3. In `BorderView.OnDrawingContent`, when `Tab` flag is set, call `TabHeaderRenderer` with the parent's `LineCanvas`, border bounds, and tab properties
4. Add integration tests verifying Border+Tab rendering end-to-end

#### 3. Enhance `BorderEditor` and Adornments scenario

**Files:** `Examples/UICatalog/Scenarios/EditorsAndHelpers/BorderEditor.cs`

1. Add UI for `BorderSettings.Tab` toggle, `TabSide` selector, `TabOffset` NumericUpDown, `TabText` TextField
2. Adornments scenario should work automatically if BorderEditor is updated correctly

### Exit Criteria

- `TabHeaderRenderer` is a standalone, fully-tested geometry-only drawing helper with no View/Border dependencies
- All four `Side`s produce correct output matching the visual examples in this spec
- showSeparator == true/false variants all correct, including overflow cases
- No text drawing in the renderer — caller owns content rendering
- `BorderView` calls `TabHeaderRenderer` when `BorderSettings.Tab` is set
- `BorderEditor` supports all tab properties
- All existing tests still pass

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

3. **Line ~226 (`TabWidth` property)**: ~~Contains pseudo-code `(or Height)` — `Parent?.TitleTextFormatter.FormatAndGetSize ().Width (or Height)` is not valid C#. Should be two separate expressions or a conditional based on `TabSide`.~~ **FIXED:** Implemented as `Border.TabLength` with auto-computation `Title.GetColumns() + 2`. The earlier `+ 1` bug was also fixed.

4. **Line ~364 (`IValue<Tab?>.Value` getter)**: `Focused as Tab` won't work correctly. `View.Focused` returns the *immediate* focused subview — if a control inside a Tab has focus, `Focused` returns that control, not the Tab. Should use `SubViews.OfType<Tab> ().FirstOrDefault (t => t.HasFocus)`.

5. **Line ~584**: `Scrollbar.Slider` — inconsistent casing. The class is `ScrollBar` (capital B), not `Scrollbar`.

6. **Line ~233**: `ViewportSettingsFlags.TransparentMouse` — should clarify this goes on the **Border adornment's** `ViewportSettings`, not the Tab view's ViewportSettings.

7. **Line ~604**: `ViewportSettingsFlags.MouseEventsTransparent` — the actual flag name is `ViewportSettingsFlags.TransparentMouse` (used elsewhere in the codebase, e.g. `Arrangement.cs`).

8. **Phase 3 scope**: Phase 3 ("Polish") bundles scrolling, all four sides, focus/hotkeys, and draw order — this is very large and should probably be split into 3-4 separate phases.

