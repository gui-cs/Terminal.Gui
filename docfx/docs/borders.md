# Borders Deep Dive

[Border](~/api/Terminal.Gui.ViewBase.Border.yml) is the adornment that draws the visual frame, title, and tab header for a [View](~/api/Terminal.Gui.ViewBase.View.yml). It is one of the three adornment layers (Margin → Border → Padding) that surround a View's content area.

This deep dive covers Border's rendering modes, the tab header system, and how `LineCanvas` auto-join produces flowing connected tab styles.

## Table of Contents

- [Border Basics](#border-basics)
- [Title Rendering by Thickness](#title-rendering-by-thickness)
- [Tab Style Borders](#tab-style-borders)
- [Tab Header Geometry](#tab-header-geometry)
- [Focus and Attributes](#focus-and-attributes)
- [Tab Offset and Clipping](#tab-offset-and-clipping)
- [Auto-Join with SuperViewRendersLineCanvas](#auto-join-with-superviewrenderslinecanvas)
- [Border Line Positioning](#border-line-positioning)
- [Implementation: TabTitleView](#implementation-tabtitleview)
- [Drag-to-Slide Example](#drag-to-slide-example)

---

## Border Basics

Every [View](~/api/Terminal.Gui.ViewBase.View.yml) has a `Border` adornment accessible via `View.Border`. The border's appearance is controlled by:

- **`BorderStyle`** ([LineStyle](~/api/Terminal.Gui.Drawing.LineStyle.yml)) — The line style (`Single`, `Double`, `Rounded`, etc.)
- **`Border.Thickness`** ([Thickness](~/api/Terminal.Gui.Drawing.Thickness.yml)) — How many rows/columns each side occupies
- **`Border.Settings`** ([BorderSettings](~/api/Terminal.Gui.ViewBase.BorderSettings.yml)) — Flags controlling title and tab rendering

The border is rendered by [BorderView](~/api/Terminal.Gui.ViewBase.BorderView.yml), the internal `AdornmentView` created when `Border.GetOrCreateView()` is called (or implicitly when `BorderStyle` is set).

---

## Title Rendering by Thickness

The `Thickness` on the title side determines how many rows (or columns) the border occupies and how the title is rendered within that space.

### `Thickness.Top == 1` — Title Inline on Border Line

The title sits directly on the single top border line with `┤` and `├` connectors:

```
┌┤Title├──┐
│         │
└─────────┘
```

### `Thickness.Top == 2` — Title with Cap Line (No Closing Edge)

Two rows: a cap line above the title, then the main border line with the title. Corner connectors (`┘`/`└`) terminate — there is no closing line:

```
 ╭─────╮
╭┘Title└──╮
│         │
╰─────────╯
```

### `Thickness.Top == 3` — Title in Enclosed Rectangle

Three rows: top cap, title row, and a closing line. T-junction connectors (`┤`/`├`) continue through:

```
 ╭─────╮
╭┤Title├──╮
│╰─────╯  │
│         │
╰─────────╯
```

### `Thickness.Top ≥ 4` — Same as 3 with Extra Space

Identical rendering to thickness 3, with additional empty rows above. The title rectangle is the same shape, just positioned higher.

---

## Tab Style Borders

When `Border.Settings` includes `BorderSettings.Tab`, the border renders a **tab header** — a small rectangle containing the View's `Title` that protrudes from one side of the content border. This is the foundation for building tabbed interfaces.

### Enabling Tab Style

```csharp
view.BorderStyle = LineStyle.Rounded;
view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
view.Border.TabSide = Side.Top;
view.Border.TabOffset = 0;
view.Border.Thickness = new Thickness (1, 3, 1, 1); // 3 on the tab side
```

### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `Border.Settings` | `BorderSettings` | Must include `BorderSettings.Tab` to enable tab rendering |
| `Border.TabSide` | `Side` | Which side the tab header appears on (`Top`, `Bottom`, `Left`, `Right`) |
| `Border.TabOffset` | `int` | Offset along the tab side where the header starts (can be negative) |
| `Border.TabLength` | `int?` | Total length of the tab including borders. `null` = auto-compute from `Title` |
| `BorderView.TabTitle` | `View?` | The `View` rendering the tab title (for custom mouse handling) |

When both `Tab` and `Title` are set, `TabLength` auto-computes as `Title.GetColumns() + 2` (title text width + two border columns). When only `Tab` is set without `Title`, `TabLength` defaults to `2` (just the border columns, no text).

---

## Tab Header Geometry

The tab-side thickness determines the **depth** of the header: `depth = min(sideThickness - 1, 3)`.

| Title-Side Thickness | Header Depth | Header Structure |
|----------------------|--------------|------------------|
| 1 | 0 | No header (border line only) |
| 2 | 1 | Title text inline on closing edge |
| 3 | 2 | Cap line + title row |
| 4+ | 3 | Cap line + title row + closing edge |

### Visual Examples by Side (Thickness = 3, Depth = 2)

All examples use `BorderStyle = Rounded`, `TabOffset = 0`.

#### `Side.Top`

**Unfocused** (closed — header closing line drawn):
```
╭───╮
│Tab│
├───┴───╮
│content│
╰───────╯
```

**Focused** (open — header closing line suppressed):
```
╭───╮
│Tab│
│   ╰───╮
│content│
╰───────╯
```

#### `Side.Bottom`

**Unfocused:**
```
╭───────╮
│content│
├───┬───╯
│Tab│
╰───╯
```

**Focused:**
```
╭───────╮
│content│
│   ╭───╯
│Tab│
╰───╯
```

#### `Side.Left`

Tab text is rendered vertically using `TextDirection.TopBottom_LeftRight`.

**Unfocused:**
```
╭─┬───────╮
│T├content│
│a│       │
│b│       │
╰─┴───────╯
```

**Focused:**
```
╭─────────╮
│T content│
│a        │
│b        │
╰─────────╯
```

#### `Side.Right`

**Unfocused:**
```
╭───────┬─╮
│content│T│
│       │a│
│       │b│
╰───────┴─╯
```

**Focused:**
```
╭─────────╮
│content T│
│        a│
│        b│
╰─────────╯
```

### Summary: Which Line Gets Suppressed

| `TabSide` | Thickness = 3 on | Focused suppresses | TabOffset axis |
|-----------|------------------|--------------------|----------------|
| Top | `Thickness.Top` | Bottom line of header | Horizontal |
| Bottom | `Thickness.Bottom` | Top line of header | Horizontal |
| Left | `Thickness.Left` | Right line of header | Vertical |
| Right | `Thickness.Right` | Left line of header | Vertical |

---

## Focus and Attributes

Focus state affects tab rendering in two ways:

### Border Geometry (Depth ≥ 3 Only)

At depth ≥ 3, the header has a closing edge (the line adjacent to the content area):
- **Focused**: Closing edge is **suppressed** (open gap), visually connecting the header to the content
- **Unfocused**: Closing edge is **drawn** (closed), visually separating the header from content

**At depth < 3**, focused and unfocused tabs render with **identical border geometry** — only the title text attributes differentiate them.

### Title Text Attributes

The tab title text always uses the owning View's focus-appropriate attributes:

| View State | Title Text | Hotkey Character |
|------------|------------|------------------|
| Focused | `VisualRole.Focus` | `VisualRole.HotFocus` |
| Unfocused | `VisualRole.Normal` | `VisualRole.HotNormal` |

This is handled by the `TabTitleView` inner class, which overrides `OnDrawingText` to consult `OwnerView.HasFocus` rather than its own `HasFocus` (which is always `false` since it cannot receive focus).

---

## Tab Offset and Clipping

`TabOffset` positions the tab header along the tab side. It can be positive (shifted right/down), zero (at the start), or negative (shifted left/up, partially off-screen).

### Positive Offset (`TabOffset = 2`)

```
  ╭───╮
  │Tab│
╭─┴───┴─╮
│content │
╰────────╯
```

### Negative Offset (`TabOffset = -1`)

```
───╮
Tab│
╭──┴────╮
│content│
╰───────╯
```

### Fully Clipped (`TabOffset = -5`, tab length = 5)

```
╭───────╮
│content│
╰───────╯
```

### Clipping Mechanism

The `TabTitleView` is positioned at the **unclipped** header rectangle coordinates. The View system's natural viewport clipping handles partial visibility — both border lines and text are clipped automatically. No manual substring calculations or cap-line extensions are needed.

---

## Auto-Join with SuperViewRendersLineCanvas

The tab header is rendered by a `TabTitleView` SubView that has `SuperViewRendersLineCanvas = true`. This means its border lines merge into the parent View's `LineCanvas` instead of rendering independently.

### How LineCanvas Auto-Join Works

When two border lines overlap at the same `(x, y)` on the same `LineCanvas`, the system resolves them into the correct junction glyph:

| Overlap | Result |
|---------|--------|
| `╮` + `╭` | `┬` (top T-junction) |
| `╯` + `╰` | `┴` (bottom T-junction) |
| horizontal end + vertical | `├` or `┤` |
| two verticals | continuous `│` |

### Multi-Tab Auto-Join

When multiple tab Views share a `LineCanvas` (via a common SuperView with `SuperViewRendersLineCanvas = true`), adjacent tab headers overlap by one column. LineCanvas automatically produces the flowing connected style:

```
Tab1's header:    Tab2's header:    Combined result:
╭────╮                 ╭────╮       ╭────┬────╮
│Tab1│                 │Tab2│       │Tab1│Tab2│
╰────╯                 ╰────╯       ╰────┴────╯
       ↑              ↑
       Tab1 right overlaps Tab2 left
       ╮ + ╭ → ┬ (top), ╯ + ╰ → ┴ (bottom)
```

With the selected tab open and unselected tabs closed:
```
╭────┬────╮
│Tab1│Tab2│
│    ╰────┴───────╮
│content for Tab1 │
╰─────────────────╯
```

---

## Border Line Positioning

When `BorderSettings.Tab` is set, border line positioning differs from the standard model.

**Non-tab sides** (the 3 sides without the tab): The content border line is drawn at the **outer edge** of the thickness.

**Tab side**: The content border line is drawn at `thickness - 1` from the outer edge. The rows/columns between the outer edge and the content border line form the **tab header region**.

| Title-Side Thickness | Content Border Position (Side.Top) | Tab Header Region | Header Depth |
|----------------------|------------------------------------|-------------------|--------------|
| 1 | y = 0 | None (border only) | 0 |
| 2 | y = 1 | y = 0 (1 row) | 1 |
| 3 | y = 2 | y = 0–1 (2 rows) | 2 |
| 4 | y = 3 | y = 0–2 (3 rows) | 3 |
| N (N ≥ 4) | y = N − 1 | y = 0 to N − 2 | capped at 3 |

General rule: content border at `y = thickness − 1`. Header depth = `min(thickness − 1, 3)`.

---

## Implementation: TabTitleView

The tab header is rendered by `TabTitleView`, a private `View` subclass inside `BorderView`. It handles both the header border frame and the title text.

### Configuration

```csharp
TabTitleView:
  CanFocus = false
  TabStop = TabBehavior.NoStop
  SuperViewRendersLineCanvas = true      // border lines merge into parent LineCanvas
  OwnerView = parentView                 // for focus-aware attribute selection
  BorderStyle = parentLineStyle          // matching parent's line style
  Border.Settings = BorderSettings.None  // no title rendering on the view's own border
  Border.Thickness = ComputeTabLabelThickness (side, depth, hasFocus)
  HotKeySpecifier = parent.HotKeySpecifier
```

### Border Thickness by Depth (Side.Top)

| Depth | Border.Thickness (left, top, right, bottom) | Notes |
|-------|---------------------------------------------|-------|
| 3 (focused) | `(1, 1, 1, 0)` | No bottom = open gap |
| 3 (unfocused) | `(1, 1, 1, 1)` | Bottom = closed separator |
| 2 | `(1, 1, 1, 0)` | Cap line, no closing edge |
| 1 | `(1, 0, 1, 0)` | Side edges only, no cap |

Other sides rotate accordingly.

### Key Methods

| Method | Purpose |
|--------|---------|
| `DrawTabBorder()` | Main entry. Computes geometry, positions TabTitleView, draws content border segments. |
| `EnsureTabTitleView()` | Lazy-creates the TabTitleView with correct configuration. |
| `ComputeHeaderRect()` | Computes the unclipped header rectangle in screen coordinates. |
| `ComputeTabLabelThickness()` | Maps depth + side + focus → `Thickness` for the TabTitleView's border. |
| `AddTabSideContentBorder()` | Draws content border with gap/separator segments on the tab side. |

### Draw Pipeline

The tab rendering relies on the View draw pipeline ordering that enables adornment SubView border lines to auto-join with the parent View's border. The pipeline order is:

```
DoDrawAdornments → DoClearViewport → DoDrawSubViews → DoDrawText → DoDrawContent
→ DoDrawAdornmentsSubViews → DoRenderLineCanvas → DoDrawComplete
```

The key change: `DoDrawAdornmentsSubViews` now runs **before** `DoRenderLineCanvas`, so SubView border lines are merged into the parent's `LineCanvas` before it is rendered to screen.

---

## Drag-to-Slide Example

`BorderView.TabTitle` exposes the tab title `View`, allowing developers to hook `MouseEvent` for custom behaviors like drag-to-slide. Because `TabTitleView` is a SubView of `BorderView`, it receives mouse events **before** the border's `Arranger` — no conflict with move/resize.

```csharp
// Hook after first draw (TabTitleView is lazily created)
Point? dragStart = null;
int dragStartOffset = 0;
bool hooked = false;

view.DrawComplete += (_, _) =>
{
    if (hooked || ((BorderView)view.Border.View!).TabTitle is not { } tabTitle)
    {
        return;
    }

    hooked = true;

    tabTitle.MouseEvent += (_, mouse) =>
    {
        if (!dragStart.HasValue && mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
        {
            dragStart = mouse.ScreenPosition;
            dragStartOffset = view.Border.TabOffset;
            view.App?.Mouse.GrabMouse (tabTitle);
            mouse.Handled = true;
        }

        if (dragStart.HasValue
            && mouse.Flags is (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport))
        {
            int delta = view.Border.TabSide is Side.Top or Side.Bottom
                            ? mouse.ScreenPosition.X - dragStart.Value.X
                            : mouse.ScreenPosition.Y - dragStart.Value.Y;

            view.Border.TabOffset = dragStartOffset + delta;
            mouse.Handled = true;
        }

        if (mouse.Flags.HasFlag (MouseFlags.LeftButtonReleased) && dragStart.HasValue)
        {
            dragStart = null;
            view.App?.Mouse.UngrabMouse ();
            mouse.Handled = true;
        }
    };
};
```
