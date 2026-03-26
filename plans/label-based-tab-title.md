# Plan: Replace Tab Header Rendering with Label SubView

## Problem

Currently, `DrawTitleInTabHeader` manually manipulates the parent View's `TitleTextFormatter`,
and `TabHeaderRenderer` draws tab header lines (cap, edges, content-side border with
gap/separator). This is fragile and complex. The Label approach replaces **both**.

## Approach

The Label **IS** the tab. It is added as a SubView of `BorderView` with:
- `Border.LineStyle` matching the parent Border's LineStyle
- `Border.Thickness` varying per side based on depth and focus state
- `Border.Settings = BorderSettings.None` (border is just box lines, title is Label content)
- `CanFocus = false`, `TabStop = TabBehavior.NoStop`
- `Text` = parent View's `Title`
- For Left/Right tabs, `TextDirection.TopBottom_LeftRight`

The Label's border lines auto-join with the parent's content border via LineCanvas.
`TabHeaderRenderer` is removed entirely.

## Depth → Label Border Thickness Mapping

For a **Top** tab (others rotate accordingly):

| Depth | Cap (Top) | Left | Right | Content (Bottom) | Focus behavior |
|-------|-----------|------|-------|-------------------|----------------|
| 3     | 1         | 1    | 1     | focused?0:1       | Toggle content side |
| 2     | 1         | 1    | 1     | 0                 | Same for both |
| 1     | 0         | 1    | 1     | 0                 | Same for both |

- **Depth 3 focused**: Content-side thickness=0, Label Height=depth (content expands into gap)
- **Depth 3 unfocused**: Content-side thickness=1, Label Height=depth (separator auto-joins)
- **Depth ≤ 2**: No focus distinction in border lines (only title attributes differentiate)

## Content Border Drawing

Don't draw the tab-side content border when tab is visible (same as current approach).
The Label's border handles what was previously TabHeaderRenderer's job:
- **Unfocused depth ≥ 3**: Label has thickness=1 on content side → separator auto-joins
  with the parent's other border lines to create ├/┤ or ┬/┴ junctions.
- **Focused depth ≥ 3**: Label has thickness=0 on content side → open gap. The content-side
  border is drawn as split segments (left-of-tab + right-of-tab), and the Label's side
  edges create corners where they meet those segments.
- **Depth < 3**: Full content-side line is drawn. Label side edges auto-join with it.

## Label Positioning

Label frame = `headerRect` (from `ComputeHeaderRect`), converted to BorderView viewport coords.
When the header is partially clipped (negative offset / overflow), the Label's Frame is set to
the **unclipped** headerRect and the View system's natural clipping handles visibility.

## Depth 1 Corner Extensions

For depth 1, the Label is only 1 row/col. The side edges need to extend 1 cell outward
(away from content) so LineCanvas auto-join creates curved corners. This requires 2 manual
`AddLine` calls after the Label is positioned (same as current behavior).

---

## Implementation Plan

### Step 1: Move geometry helpers from TabHeaderRenderer to BorderView

Move `ComputeHeaderRect` and `ComputeViewBounds` as `private static` methods on `BorderView`.
These compute the tab header position and are still needed.

**File**: `Terminal.Gui/ViewBase/Adornment/BorderView.cs`

### Step 2: Create `_tabTitleLabel` and helper method

Add to `BorderView`:
- `private Label? _tabTitleLabel;`
- `private Label EnsureTabTitleLabel()` — creates the Label on first call:
  ```csharp
  _tabTitleLabel = new Label
  {
      // CanFocus = false, - label is already CanFocus = false
      TabStop = TabBehavior.NoStop,
  };
  _tabTitleLabel.Border.Settings = BorderSettings.None;
  Add (_tabTitleLabel);
  ```

**File**: `Terminal.Gui/ViewBase/Adornment/BorderView.cs`

### Step 3: Create `ComputeTabLabelThickness` helper

Static method that returns `Thickness` based on depth, focus, and side:

```csharp
static Thickness ComputeTabLabelThickness (Side tabSide, int depth, bool hasFocus)
{
    // "cap" = outward edge, "content" = inward edge (toward content area)
    int cap = depth >= 2 ? 1 : 0;
    int contentSide = (depth >= 3 && !hasFocus) ? 1 : 0;
    return tabSide switch
    {
        Side.Top    => new Thickness (1, cap, 1, contentSide),
        Side.Bottom => new Thickness (1, contentSide, 1, cap),
        Side.Left   => new Thickness (cap, 1, contentSide, 1),
        Side.Right  => new Thickness (contentSide, 1, cap, 1),
    };
}
```

**File**: `Terminal.Gui/ViewBase/Adornment/BorderView.cs`

### Step 4: Refactor `DrawTabBorder`

Replace the `TabHeaderRenderer.AddLines(...)` call and `DrawTitleInTabHeader(...)` call:

1. Compute `headerRect`, `viewBounds`, `clipped` as before (using moved helper methods).
2. If `clipped.IsEmpty` → hide Label, draw all 4 content border lines, return.
3. If visible:
   a. `EnsureTabTitleLabel()`
   b. Convert `headerRect` from screen → BorderView viewport coords.
   c. Set Label properties:
      - `Frame = viewportHeaderRect`
      - `Border.LineStyle = border.LineStyle`
      - `Border.Thickness = ComputeTabLabelThickness(...)`
      - `Text = Adornment.Parent.Title`
      - `HotKeySpecifier = Adornment.Parent.HotKeySpecifier`
      - For Left/Right: `TextFormatter.Direction = TextDirection.TopBottom_LeftRight`
      - `Visible = true`
   d. Set `LastTitleRect` from the Label's content area in screen coords.

4. Draw content border lines:
   - 3 non-tab sides: always (same as now).
   - Tab side: conditional on focus + depth:
     - If `!tabVisible`: draw full line.
     - If focused && depth ≥ 3: draw split segments (gap between Label side edges).
     - Otherwise: draw full line (auto-joins with Label's content-side border).

5. For depth 1: add 2 manual extension lines for corner auto-join.

**File**: `Terminal.Gui/ViewBase/Adornment/BorderView.cs`

### Step 5: Delete TabHeaderRenderer.cs

Remove `Terminal.Gui/Drawing/TabHeaderRenderer.cs` entirely.

### Step 6: Remove DrawTitleInTabHeader

Delete `DrawTitleInTabHeader` method and `ComputeClosingEdgeTitleArea` from BorderView.

### Step 7: Verify tests

Run all 54 BorderViewTests + full 15k suite. Fix visual differences.

Key concern: LineCanvas auto-join behavior when the Label's border meets the content border.
The expected glyphs (╭╮╰╯├┤┬┴) should be produced naturally by auto-join, but positions
must be exact.

### Step 8: Update spec

Update `plans/tabview-border-based-design.md` to document the Label-based approach.

---

## Risks & Mitigations

1. **LineCanvas auto-join may produce different glyphs** — The Label's border adds lines to
   the *parent's* LineCanvas (via the standard View drawing pipeline). Need to verify
   that the Label's border lines end up on the same LineCanvas as the content border.
   If not, may need to draw Label border lines on `Adornment.Parent.LineCanvas` explicitly.

2. **Label clipping vs manual clipping** — Current code manually skips title characters when
   the tab header is partially off-screen. The Label approach relies on View clipping. Need
   to verify the Label is clipped correctly by the BorderView's viewport.

3. **Coordinate systems** — headerRect is in screen coords, Label.Frame is in SuperView
   (BorderView) viewport coords. Must convert correctly.

4. **Label draw order** — The Label must draw AFTER the content border lines are added to
   LineCanvas, so its border lines auto-join correctly. Since SubViews draw after the
   View's own drawing, this should work naturally.

## Todos

1. `create-tab-label` — Create Label, helpers, thickness computation
2. `update-draw-tab-border` — Refactor DrawTabBorder to use Label
3. `delete-tab-header-renderer` — Remove TabHeaderRenderer.cs
4. `remove-draw-title` — Remove DrawTitleInTabHeader
5. `verify-tests` — Run all tests, fix failures
6. `update-spec` — Update design spec
