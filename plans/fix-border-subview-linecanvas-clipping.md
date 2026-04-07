# Fix: Border SubView LineCanvas Lines Not Clipped at Parent Bounds

## Bug Summary

When a SubView of a Border has `SuperViewRendersLineCanvas = true` and its own border
(`BorderStyle != None`), and the SubView's frame extends past the parent Border's bounds,
the SubView's border lines bleed into the parent's border columns. For example, a `║`
becomes `╫` because the SubView's `─` merges unclipped into the parent's LineCanvas.

**Failing test:** `AdornmentSubViewLineCanvasTests.BorderSubView_WithBorder_ClippedWhenExceedingParentBounds`

## Root Cause

The merge at `View.Drawing.Adornments.cs:50` is unclipped:

```csharp
// Line 43: clip set to border's frame (only affects raster drawing via Driver.Clip)
Region? saved = borderView.AddFrameToClip ();
// Line 44: subviews are drawn (their LineCanvas lines are generated)
borderView.DoDrawSubViews ();

// Line 50: ALL lines from borderView's LineCanvas are merged — NO BOUNDS CHECK
LineCanvas.Merge (borderView.LineCanvas);
```

`LineCanvas.Merge()` (LineCanvas.cs:510-524) copies every `StraightLine` unconditionally.
`Driver.Clip` (set by `AddFrameToClip`) only restricts raster output (`AddStr`, `Move`),
not LineCanvas data. The merged lines participate in intersection resolution and produce
corrupted junction glyphs where they cross the parent's border lines.

The class docs (LineCanvas.cs:44-48) describe a `Merge(LineCanvas, Region?)` overload for
clipped merging, but **this overload does not exist**.

## Draw Pipeline Context

```
View.Draw():
  1. DoDrawAdornments()         — Parent's border adds lines to this.LineCanvas
  2. AddViewportToClip()        — Clip to viewport (raster only)
  3. DoDrawSubViews()           — Content subviews drawn
  4. SetClip → AddFrameToClip() — Clip to frame (raster only)
  5. DoDrawAdornmentsSubViews() — Border subview lines merged into this.LineCanvas ← BUG
  6. DoRenderLineCanvas()       — Resolves all lines and renders to screen
```

The merge in step 5 must restrict lines to the border's content area before they enter
the parent's LineCanvas in step 6.

## Fix Options

### Option A: Clipped `Merge` overload on LineCanvas

Implement the documented but missing `Merge(LineCanvas, Rectangle clipBounds)` overload.
It would trim or discard each incoming `StraightLine` to fit within `clipBounds` before
adding it.

**Where to change:**
- `LineCanvas.cs` — Add `Merge(LineCanvas, Rectangle)` that clips each line using
  `StraightLineExtensions`-style logic (trim Start/Length to stay within bounds).
- `View.Drawing.Adornments.cs:50` — Pass the border view's frame rect:
  ```csharp
  Rectangle borderBounds = borderView.FrameToScreen ();
  LineCanvas.Merge (borderView.LineCanvas, borderBounds);
  ```
- Same pattern for Padding merge at line 84.

**Pros:**
- Clean, self-contained — clipping logic lives in LineCanvas where it belongs.
- The documentation already describes this overload; just implement it.
- Lines are trimmed *before* intersection resolution, so no corrupted junctions.
- `StraightLineExtensions.Exclude` already has line-splitting logic that can be reused
  to clip lines against a rectangle boundary.

**Cons:**
- Trimming lines can produce different junction types at the clip boundary (the docs
  warn about this). A line that was `PassOverHorizontal` may become `StartRight` after
  clipping, which could change the resolved glyph. This is acceptable — the clipped
  edge is at the parent's border, which already has its own lines providing the correct
  junction context.
- Must handle both horizontal and vertical lines, and both positive/negative lengths.

**Complexity:** Medium. The line-trimming math is straightforward (clamp start/end to
bounds, recompute length). `StraightLineExtensions` already demonstrates the pattern.

---

### Option B: Exclude-based approach — add exclusion region to parent LineCanvas

Instead of clipping lines before merge, merge everything, then exclude the out-of-bounds
cells from the parent's LineCanvas output.

**Where to change:**
- `View.Drawing.Adornments.cs:50` — After merge, compute the region outside the border
  view's frame and call `LineCanvas.Exclude()` on those areas.

**Pros:**
- Simpler implementation — no line-splitting math.
- Uses existing `Exclude` API.

**Cons:**
- **Does not fix the bug.** `Exclude` hides cells from `GetCellMap` output but lines
  still participate in intersection resolution. The out-of-bounds `─` still crosses the
  parent's `║` during resolution, producing `╫` — even though the `╫` cell at the
  parent's border column would be excluded, the parent's own `║` line at that position
  would ALSO be excluded because exclusion is position-based, not line-based.
- Would need careful region math to only exclude the *SubView's* cells outside bounds
  without excluding the parent's own border cells at those positions.
- Fragile and semantically wrong — the problem is that lines exist where they shouldn't,
  not that their output needs hiding.

**Verdict: Not viable** without significant additional work to make exclusion line-aware.

---

### Option C: Clip in `DoDrawSubViews` — restrict the SubView's own LineCanvas generation

Prevent the SubView from generating LineCanvas lines outside the border's frame in the
first place, by clipping the SubView's layout/frame before it draws.

**Where to change:**
- `View.Drawing.Adornments.cs:44` or the SubView's own `Draw()` — Constrain the
  SubView's effective frame to the intersection of its frame and the border view's frame
  before drawing.

**Pros:**
- Fixes the problem at the source — lines are never generated outside bounds.
- No post-hoc filtering or trimming needed.

**Cons:**
- Changing the SubView's frame/layout is invasive and could have side effects on hit
  testing, mouse events, and other layout-dependent behavior.
- The SubView's `BorderView.OnDrawingContent` adds lines based on the SubView's
  `FrameToScreen()`. Changing the frame changes the border geometry, not just clips it.
- Would need to be undone after drawing, adding complexity.
- Conceptually wrong — layout shouldn't change during draw.

**Verdict: Too invasive.** Mixing layout mutation with draw is a design smell.

---

### Option D: Filter during `RenderLineCanvas` — clip at output time

Instead of clipping during merge, filter the resolved `cellMap` in `RenderLineCanvas`
to only include cells within the view's frame.

**Where to change:**
- `View.Drawing.LineCanvas.cs:48-60` — Skip cells outside `FrameToScreen()`.

**Pros:**
- Simple one-line check in the render loop.
- No changes to LineCanvas data structure.

**Cons:**
- **Does not fix junction corruption.** The out-of-bounds lines still participate in
  intersection resolution. Even if the corrupted `╫` cell is not rendered, the parent's
  `║` line at that position may resolve differently because of the intersecting `─`.
  The resolved glyph at the parent's border column would be wrong even if we skip
  rendering out-of-bounds cells.
- Only addresses the symptom (rendering) not the cause (unclipped lines in the canvas).

**Verdict: Insufficient.** Junction corruption happens during resolution, not rendering.

## Recommendation

**Option A** is the correct fix. It addresses the root cause (unclipped lines entering
the parent's LineCanvas), uses the existing documented API contract, and produces correct
junction glyphs because the parent's own border lines are the only lines at the boundary
during intersection resolution.

### Implementation sketch

```csharp
// LineCanvas.cs — new overload
public void Merge (LineCanvas lineCanvas, Rectangle clipBounds)
{
    foreach (StraightLine line in lineCanvas._lines)
    {
        // Clip the line to clipBounds; may produce 0 or 1 clipped line
        StraightLine? clipped = ClipLine (line, clipBounds);

        if (clipped is { })
        {
            AddLine (clipped);
        }
    }

    // Exclusion regions are position-based — intersect with clipBounds
    if (lineCanvas._exclusionRegion is { })
    {
        Region clippedExclusion = lineCanvas._exclusionRegion.Clone ();
        clippedExclusion.Intersect (clipBounds);
        _exclusionRegion ??= new Region ();
        _exclusionRegion.Union (clippedExclusion);
    }
}

private static StraightLine? ClipLine (StraightLine line, Rectangle bounds)
{
    Rectangle lineBounds = line.Bounds;
    Rectangle clipped = Rectangle.Intersect (lineBounds, bounds);

    if (clipped.IsEmpty)
    {
        return null;
    }

    // Recompute Start and Length from the clipped rectangle
    Point newStart = line.Orientation == Orientation.Horizontal
        ? new Point (clipped.X, clipped.Y)
        : new Point (clipped.X, clipped.Y);

    int newLength = line.Orientation == Orientation.Horizontal
        ? clipped.Width
        : clipped.Height;

    // Preserve direction (sign of Length)
    if (line.Length < 0)
    {
        newLength = -newLength;
        // Adjust start for negative-direction lines
        // ... (handle negative length start offset)
    }

    return new StraightLine (newStart, newLength, line.Orientation, line.Style, line.Attribute);
}
```

Call site in `View.Drawing.Adornments.cs`:

```csharp
if (borderView.LineCanvas.Bounds != Rectangle.Empty)
{
    Rectangle clipBounds = borderView.FrameToScreen ();
    LineCanvas.Merge (borderView.LineCanvas, clipBounds);
    borderView.LineCanvas.Clear ();
}
```

Same for the Padding merge at line 82-86.
