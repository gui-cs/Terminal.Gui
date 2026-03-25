# Plan: Replace Tab Header Rendering with Label SubView

## Problem
Currently, `DrawTitleInTabHeader` manually manipulates the parent View's `TitleTextFormatter`, and `TabHeaderRenderer` draws tab header lines (cap, edges, content-side border with gap/separator). This is fragile and complex. The Label approach replaces **both**.

## Approach
The Label **IS** the tab. It is added as a SubView of `BorderView` with:
- `Border.LineStyle` matching the parent Border's LineStyle
- `Border.Thickness` varying per side based on depth and focus state
- `Border.Settings = BorderSettings.Title` (to draw title in the border)
- `CanFocus = false`, `TabStop = TabBehavior.NoStop`
- Text = parent View's `Title`
- For Left/Right tabs, `TextDirection.TopBottom_LeftRight`

The Label's border lines auto-join with the parent's content border via LineCanvas.
`TabHeaderRenderer` is removed entirely.

## Depth → Label Border Thickness Mapping

For a **Top** tab (others rotate accordingly):
- **Depth 3** (thickness ≥ 3): `Thickness(Left=1, Top=1, Right=1, Bottom=focused?0:1)`
  - Top=1 → cap line, Left/Right=1 → side edges, Bottom → content side
  - Focused: Bottom=0 (open gap), label Height grows by 1 to maintain content
  - Unfocused: Bottom=1 (separator joins with content border)
- **Depth 2** (thickness = 2): `Thickness(Left=1, Top=1, Right=1, Bottom=0)`
  - Cap + side edges, no closing edge. Title on the bottom row.
  - Focused/unfocused look the same (only attributes differentiate)
- **Depth 1** (thickness = 1): `Thickness(Left=1, Top=0, Right=1, Bottom=0)`
  - Side edges only. Title on the single row.
  - Focused/unfocused look the same (only attributes differentiate)

For depth < 3, no focus distinction in border lines.

## Content Border Drawing
`DrawTabBorder` draws ALL 4 content border lines (including the tab side). The Label's border
lines auto-join with the content-side line via LineCanvas. For focused depth ≥ 3, the Label has
no content-side border (thickness=0), so the content border line shows through as a gap
interrupted by the Label's side edges.

Wait — if the content-side border is drawn as a full line, the Label's side edges would create
T-junctions. For focused state, we want the gap (no line between the side edges). So the
content-side full line needs to be excluded in the Label region, OR the content-side line
should NOT be drawn when the tab is visible (keeping current behavior where the tab side is
conditional).

**Resolution**: Keep the current approach: don't draw the tab-side content border when tab is
visible. The Label's border handles what was previously TabHeaderRenderer's job. For unfocused
depth ≥ 3, the Label has border thickness 1 on the content side, which draws the separator.
For focused, thickness 0 on content side → open gap.

## Label Positioning
The Label is positioned at the tab header location, computed the same way as today
(`ComputeHeaderRect` logic, but inline since TabHeaderRenderer is removed).
Coordinates converted from screen → BorderView viewport.

## Todos
1. `create-tab-label` - Add `_tabTitleLabel` field to BorderView, lazy creation, property sync
2. `update-draw-tab-border` - Draw all 4 content borders (tab side only when tab not visible),
   position Label with correct thickness per depth/focus, set text/direction
3. `delete-tab-header-renderer` - Remove TabHeaderRenderer.cs entirely
4. `remove-draw-title` - Remove DrawTitleInTabHeader and ComputeClosingEdgeTitleArea
5. `verify-tests` - Run all 54 BorderViewTests + full suite
6. `update-spec` - Update plans/tabview-border-based-design.md

## Notes
- The Label auto-joins with content border via LineCanvas — no manual junction handling needed
- Natural View clipping replaces manual `titleSkipChars` logic
- `LastTitleRect` set from Label's Frame (screen coords) for transparent parent exclusion
- Label's Scheme set from parent's focus state each draw cycle

