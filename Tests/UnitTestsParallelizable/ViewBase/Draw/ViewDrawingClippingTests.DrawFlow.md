# Draw Flow Documentation: Draw_WithBorderSubView_DrawsCorrectly

This document explains the complete draw code flow for the unit test `Draw_WithBorderSubView_DrawsCorrectly`, which demonstrates how wide glyphs are clipped when overlapped by bordered subviews at different column alignments.

## Overview

Unlike the full application scenario in `WideGlyphs.cs`, this test directly calls the instance `Draw()` method without going through `Application.Run()` or the main loop. This provides a simpler path to understand the core drawing mechanics.

## Test Setup

```csharp
[Fact]
public void Draw_WithBorderSubView_DrawsCorrectly()
{
    // 1. Create driver with fake screen
    IDriver driver = CreateFakeDriver();
    driver.Clip = new Region(driver.Screen);
    
    // 2. Create superView with Dim.Auto sizing
    var superView = new View
    {
        X = 0,
        Y = 0,
        Width = Dim.Auto() + 4,  // Auto-size to content + 4 cols
        Height = Dim.Auto() + 1, // Auto-size to content + 1 row
        Driver = driver
    };
    
    // 3. Subscribe to DrawingContent to draw wide glyphs (??)
    Rune codepoint = Glyphs.Apple;  // ?? is a wide glyph (2 columns)
    
    superView.DrawingContent += (s, e) =>
    {
        var view = s as View;
        // Draw apples in every even column across the viewport
        for (var r = 0; r < view!.Viewport.Height; r++)
        {
            for (var c = 0; c < view.Viewport.Width; c += 2)
            {
                if (codepoint != default(Rune))
                {
                    view.AddRune(c, r, codepoint);
                }
            }
        }
    };
    
    // 4. Create three bordered subviews at X=0, X=1, X=2
    var viewWithBorderAtX0 = new View
    {
        Text = "viewWithBorderAtX0",
        BorderStyle = LineStyle.Dashed,
        X = 0,  // Even column
        Y = 1,
        Width = Dim.Auto(),
        Height = 3
    };
    
    var viewWithBorderAtX1 = new View
    {
        Text = "viewWithBorderAtX1",
        BorderStyle = LineStyle.Dashed,
        X = 1,  // Odd column
        Y = Pos.Bottom(viewWithBorderAtX0) + 1,
        Width = Dim.Auto(),
        Height = 3
    };
    
    var viewWithBorderAtX2 = new View
    {
        Text = "viewWithBorderAtX2",
        BorderStyle = LineStyle.Dashed,
        X = 2,  // Even column
        Y = Pos.Bottom(viewWithBorderAtX1) + 1,
        Width = Dim.Auto(),
        Height = 3
    };
    
    // 5. Add subviews and initialize
    superView.Add(viewWithBorderAtX0, viewWithBorderAtX1, viewWithBorderAtX2);
    superView.BeginInit();
    superView.EndInit();
    superView.LayoutSubViews();
    
    // 6. Draw the entire hierarchy
    superView.Draw();  // ? THIS IS WHERE DRAWING STARTS
}
```

## Complete Draw Flow

### Phase 1: Direct Draw() Call

**Entry Point:** `superView.Draw()` is called directly

```
superView.Draw()
??> View.Draw(DrawContext? context = null)
```

**File:** `Terminal.Gui\ViewBase\View.Drawing.cs`

**Initial State:**
- `Driver.Clip` = `Region(Screen)` - Set by test before calling Draw()
- `context` = `null` - No context passed, will be created
- `superView.NeedsDraw` = `true` - View needs drawing
- `superView.SubViewNeedsDraw` = `true` - Subviews need drawing

### Phase 2: View.Draw() Instance Method

```csharp
public void Draw(DrawContext? context = null)
{
    // Step 1: Early exit check
    if (!CanBeVisible(this))
    {
        return;  // Skip if not visible
    }
    
    // Step 2: Get current clip (what caller set)
    Region? originalClip = GetClip();  // ? Returns Driver.Clip = Screen
    
    if (NeedsDraw || SubViewNeedsDraw)  // ? TRUE - we need to draw
    {
        // Step 3: Draw Border and Padding
        DoDrawAdornments(originalClip);
        SetClip(originalClip);  // Restore original clip
        
        // Step 4: Set clip to Viewport
        originalClip = AddViewportToClip();  // ? INTERSECT WITH VIEWPORT
        // Now: Driver.Clip = Screen ? ViewportToScreen(superView.Viewport)
        
        context ??= new();  // Create context if null
        
        SetAttributeForRole(Enabled ? VisualRole.Normal : VisualRole.Disabled);
        
        // Step 5: Clear the Viewport background
        DoClearViewport(context);
        
        // Step 6: Draw subviews ? RECURSIVE DRAWING
        if (SubViewNeedsDraw)
        {
            DoDrawSubViews(context);
        }
        
        // Step 7: Draw the text (View.Text property)
        SetAttributeForRole(Enabled ? VisualRole.Normal : VisualRole.Disabled);
        DoDrawText(context);
        
        // Step 8: Draw the content ? FIRES DrawingContent EVENT
        DoDrawContent(context);
        
        // Step 9: Restore clip, draw LineCanvas, adornment subviews
        SetClip(originalClip);
        originalClip = AddFrameToClip();
        DoRenderLineCanvas(context);
        DoDrawAdornmentsSubViews();
        
        // Step 10: Cleanup
        Border?.AdvanceDrawIndicator();
        ClearNeedsDraw();
    }
    
    // Step 11: Cache clip for margin shadows
    Margin?.CacheClip();
    
    // Step 12: Reset the clip to what it was when we started
    SetClip(originalClip);
    
    // Step 13: We're done drawing
    DoDrawComplete(context);
}
```

### Phase 3: DoDrawContent() - Fires DrawingContent Event

**File:** `Terminal.Gui\ViewBase\View.Drawing.cs`

```csharp
private void DoDrawContent(DrawContext? context = null)
{
    if (!NeedsDraw || OnDrawingContent(context))
    {
        return;  // Skip if not needed or canceled
    }
    
    var dev = new DrawEventArgs(Viewport, Rectangle.Empty, context);
    DrawingContent?.Invoke(this, dev);  // ? FIRE EVENT
    
    if (dev.Cancel)
    {
        return;  // Event handler canceled
    }
    
    // No default drawing; event handlers do the work
}
```

**At this point:**
- `Driver.Clip` = `Screen ? ViewportToScreen(superView.Viewport)`
- The clip ensures all drawing operations stay within the viewport
- The event handler draws ?? glyphs across the viewport

### Phase 4: DrawingContent Event Handler

```csharp
superView.DrawingContent += (s, e) =>
{
    var view = s as View;  // view = superView
    
    // Draw apples in every even column
    for (var r = 0; r < view!.Viewport.Height; r++)
    {
        for (var c = 0; c < view.Viewport.Width; c += 2)  // c += 2 for wide glyphs
        {
            if (codepoint != default(Rune))
            {
                view.AddRune(c, r, codepoint);  // ? DRAW APPLE
            }
        }
    }
};
```

**What happens in AddRune():**

1. `AddRune(c, r, codepoint)` uses **viewport-relative coordinates**
2. The method converts to **screen-relative coordinates**
3. Checks if the position is within `Driver.Clip`
4. If yes, writes to the driver's output buffer
5. If no, the draw is clipped (not written)

**For ?? (Apple):**
- Width = 2 columns (it's a wide glyph)
- Drawing at `(c, r)` occupies columns `[c, c+1]`
- That's why we increment by 2: `c += 2`

### Phase 5: DoDrawSubViews() - Recursive Drawing

**File:** `Terminal.Gui\ViewBase\View.Drawing.cs`

```csharp
private void DoDrawSubViews(DrawContext? context = null)
{
    if (!NeedsDraw || OnDrawingSubViews(context))
    {
        return;
    }
    
    var dev = new DrawEventArgs(Viewport, Rectangle.Empty, context);
    DrawingSubViews?.Invoke(this, dev);
    
    if (dev.Cancel)
    {
        return;
    }
    
    if (!SubViewNeedsDraw)
    {
        return;
    }
    
    DrawSubViews(context);  // ? DRAW SUBVIEWS
}

public void DrawSubViews(DrawContext? context = null)
{
    if (InternalSubViews.Count == 0)
    {
        return;
    }
    
    // Draw subviews in reverse order (front-to-back)
    foreach (View view in InternalSubViews.Snapshot().Where(v => v.Visible).Reverse())
    {
        view.Draw(context);  // ? RECURSIVE CALL - back to Phase 2
    }
}
```

**Recursion for each subview:**

```
superView.Draw()
??> DoDrawSubViews()
    ??> DrawSubViews()
        ??> viewWithBorderAtX0.Draw()
        ?   ??> GetClip() returns current clip
        ?   ??> AddViewportToClip() intersects with viewWithBorderAtX0's viewport
        ?   ??> DoDrawAdornments() draws the dashed border
        ?   ??> DoDrawText() draws "viewWithBorderAtX0"
        ?   ??> DoDrawComplete() excludes viewWithBorderAtX0 from clip
        ?
        ??> viewWithBorderAtX1.Draw()
        ?   ??> (same process)
        ?
        ??> viewWithBorderAtX2.Draw()
            ??> (same process)
```

### Phase 6: DoDrawComplete() - Update Clip

**File:** `Terminal.Gui\ViewBase\View.Drawing.cs`

```csharp
private void DoDrawComplete(DrawContext? context)
{
    OnDrawComplete(context);
    DrawComplete?.Invoke(this, new DrawEventArgs(Viewport, Viewport, context));
    
    // Update the clip to exclude this view
    if (this is not Adornment)
    {
        if (ViewportSettings.HasFlag(ViewportSettingsFlags.Transparent))
        {
            // For transparent views, only exclude what was actually drawn
            context!.ClipDrawnRegion(ViewportToScreen(Viewport));
            ExcludeFromClip(context.GetDrawnRegion());
            ExcludeFromClip(Border?.Thickness.AsRegion(Border.FrameToScreen()));
            ExcludeFromClip(Padding?.Thickness.AsRegion(Padding.FrameToScreen()));
        }
        else
        {
            // For opaque views, exclude the entire view area
            Rectangle borderFrame = FrameToScreen();
            
            if (Border is { })
            {
                borderFrame = Border.FrameToScreen();
            }
            
            // Exclude view from clip so views "behind" it don't draw over it
            ExcludeFromClip(borderFrame);  // ? EXCLUDE FROM CLIP
            
            // Update context to track what was drawn
            context?.AddDrawnRectangle(borderFrame);
        }
    }
}
```

**This is the key to clipping:**

After each subview is drawn, its area is **excluded** from the clip. This means:
1. `viewWithBorderAtX0` draws first ? its area is excluded from clip
2. `viewWithBorderAtX1` draws second ? its area is excluded from clip  
3. `viewWithBorderAtX2` draws third ? its area is excluded from clip
4. When `superView.DrawingContent` fires, the clip has all three subviews excluded
5. Apples (??) can't draw where the subviews are

## Driver.Clip State Throughout Drawing

### Initial State (Before Draw)

```
Driver.Clip = Region(Screen)
  - Full screen rectangle (e.g., 80x25)
  - No exclusions
```

### After superView.AddViewportToClip()

```
Driver.Clip = Screen ? ViewportToScreen(superView.Viewport)
  - Clip is now limited to superView's visible area
  - Still no exclusions
```

### After viewWithBorderAtX0.Draw() ? DoDrawComplete()

```
Driver.Clip = Previous Clip - viewWithBorderAtX0.Border.Frame
  - viewWithBorderAtX0's border area is excluded
  - Apples can't draw there anymore
```

### After viewWithBorderAtX1.Draw() ? DoDrawComplete()

```
Driver.Clip = Previous Clip - viewWithBorderAtX1.Border.Frame
  - Both viewWithBorderAtX0 and viewWithBorderAtX1 excluded
  - Apples can't draw in either area
```

### After viewWithBorderAtX2.Draw() ? DoDrawComplete()

```
Driver.Clip = Previous Clip - viewWithBorderAtX2.Border.Frame
  - All three subviews excluded
  - Apples can't draw in any subview area
```

### When superView.DrawingContent Fires

```
Driver.Clip = Screen ? superView.Viewport - (all subviews)
  - Clip has "holes" where the subviews are
  - Drawing apples with AddRune() checks Driver.Clip
  - Apples outside clip don't get drawn (automatic clipping)
```

## Expected Output Explanation

```
??????????????????????????    <- Row 0: Apples fill the row
??????????????????????????    <- Row 1: Border at X=0 (even), apples after
?viewWithBorderAtX0???????    <- Row 2: Text inside border, apples after
??????????????????????????    <- Row 3: Border at X=0, apples after
??????????????????????????    <- Row 4: Apples fill the row
????????????????????? ????    <- Row 5: Border at X=1 (odd), ? = half apple
??viewWithBorderAtX1? ????    <- Row 6: Text inside border, apples after
????????????????????? ????    <- Row 7: Border at X=1, apples after
??????????????????????????    <- Row 8: Apples fill the row
??????????????????????????    <- Row 9: Border at X=2 (even), apple before & after
???viewWithBorderAtX2?????    <- Row 10: Apple + text + apples
??????????????????????????    <- Row 11: Border at X=2, apples before & after
??????????????????????????    <- Row 12: Apples fill the row
```

### Key Observations

**Row 0, 4, 8, 12:** Full rows of apples
- No subviews, clip is wide open
- Apples draw at columns 0, 2, 4, 6, 8, 10, 12, ...

**Rows 1-3 (viewWithBorderAtX0 at X=0):**
- Border starts at column 0 (even)
- Apple at column 0 is **fully clipped** (border is there)
- Apples resume at column 20 (after the border ends)
- No half-apple because border starts at even column

**Rows 5-7 (viewWithBorderAtX1 at X=1):**
- Border starts at column 1 (odd)
- Apple at column 0 is **partially clipped** ? appears as `?`
- The first column of the apple (col 0) draws
- The second column of the apple (col 1) is clipped (border is there)
- Result: Half-apple rendered as replacement char `?`
- Apples resume after the border ends

**Rows 9-11 (viewWithBorderAtX2 at X=2):**
- Border starts at column 2 (even)
- Apple at column 0 draws fully (before the border)
- Apple at column 2 is **fully clipped** (border is there)
- Apples resume after the border ends

## What This Test Validates

### 1. Wide Glyph Clipping at Even Columns
**viewWithBorderAtX0** (X=0):
- Border at even column
- Wide glyph at same even column is fully clipped
- No partial glyphs (no `?`)

### 2. Wide Glyph Clipping at Odd Columns
**viewWithBorderAtX1** (X=1):
- Border at odd column
- Wide glyph starting at even column (0) is partially clipped
- First column (0) draws, second column (1) clipped
- Results in half-glyph: `?`

### 3. Wide Glyph Clipping with Space Before Border
**viewWithBorderAtX2** (X=2):
- Border at even column (2)
- Wide glyph at column 0 draws fully (before border starts)
- Wide glyph at column 2 is fully clipped (border is there)
- Clean clipping with full glyphs before and after

## Coordinate Systems

### Viewport-Relative (used in DrawingContent)

```csharp
view.AddRune(c, r, codepoint);  // (c, r) relative to view's Viewport
```

- `(0, 0)` = Top-left of the view's viewport
- Used by drawing methods: `AddRune`, `AddStr`, `FillRect`
- Automatically converted to screen coordinates by the driver

### Screen-Relative (used by Driver.Clip)

```csharp
Rectangle screenRect = view.ViewportToScreen(new Rectangle(0, 0, 10, 10));
```

- `(0, 0)` = Top-left of the terminal screen
- Used by `Driver.Clip` (Region)
- Use `ViewportToScreen()` to convert from viewport to screen

### Frame-Relative (used by layout)

```csharp
Rectangle frame = view.Frame;  // Position relative to SuperView's content
```

- `(0, 0)` = Top-left of SuperView's content area
- Used for positioning views within their SuperView
- `FrameToScreen()` converts to screen coordinates

## Drawing Order and Clipping Strategy

### Drawing Order (Front-to-Back)

```
1. superView.DoDrawAdornments()     - Draw superView's border/padding (if any)
2. superView.DoClearViewport()      - Clear superView's background
3. superView.DoDrawSubViews()       - Draw subviews FIRST (front-to-back)
   ?? viewWithBorderAtX0.Draw()     - Draws border and text
   ?? viewWithBorderAtX1.Draw()     - Draws border and text
   ?? viewWithBorderAtX2.Draw()     - Draws border and text
4. superView.DoDrawText()           - Draw superView's text (if any)
5. superView.DoDrawContent()        - ? Draw apples (AFTER subviews)
6. superView.DoRenderLineCanvas()   - Draw lines (if any)
```

**Why this order?**
- Subviews draw **before** the parent's content
- This allows the parent's content to be "under" the subviews
- Each subview excludes its area from the clip
- When parent's `DrawingContent` fires, apples can't draw where subviews are

### Clipping Strategy

```
1. Start with Driver.Clip = Screen
2. Intersect with view's viewport ? clip to visible area
3. Draw subviews:
   - Each subview further intersects clip with its viewport
   - Each subview excludes its area from clip when done
4. Draw parent's content:
   - Content sees clip with all subviews excluded
   - Drawing operations automatically respect clip
```

## Key Differences from Full Application Flow

### What's Missing (compared to WideGlyphs.md)

1. **No Application.Run()** - Test calls `Draw()` directly
2. **No main loop** - No `RunIteration()`, `LayoutAndDraw()`, etc.
3. **No SessionStack** - Just one view hierarchy
4. **No input processing** - No keyboard/mouse events
5. **No cursor positioning** - No `SetCursor()` call
6. **Explicit layout** - Test calls `BeginInit()`, `EndInit()`, `LayoutSubViews()`
7. **Explicit draw** - Test calls `Draw()` directly

### What's the Same

1. **Draw() method behavior** - Identical to production
2. **Clip management** - Same clip intersection and exclusion
3. **Event firing** - Same `DrawingContent` event
4. **Coordinate systems** - Same viewport/screen/frame coordinates
5. **Drawing order** - Same front-to-back order
6. **Wide glyph handling** - Same wide character clipping

## Summary

The test flow is:

```
Test Setup
??> superView.Draw()                      [Phase 2: Instance Draw]
    ??> AddViewportToClip()                [Clip = Screen ? Viewport]
    ??> DoDrawSubViews()                   [Phase 5: Recursive Drawing]
    ?   ??> viewWithBorderAtX0.Draw()
    ?   ?   ??> DoDrawComplete()           [Exclude from clip]
    ?   ??> viewWithBorderAtX1.Draw()
    ?   ?   ??> DoDrawComplete()           [Exclude from clip]
    ?   ??> viewWithBorderAtX2.Draw()
    ?       ??> DoDrawComplete()           [Exclude from clip]
    ??> DoDrawContent()                    [Phase 3: Fire Event]
    ?   ??> DrawingContent event           [Phase 4: Draw Apples]
    ?       ??> AddRune() for each ??      [Automatic clipping]
    ??> DoDrawComplete()                   [Phase 6: Final Clip Update]
```

**At DrawingContent event:**
- `Driver.Clip` = Screen ? superView.Viewport - (all subviews)
- Clip has "holes" where subviews are located
- `AddRune(c, r, ??)` checks clip automatically
- Wide glyphs are clipped at odd columns, creating `?` characters

## References

### Key Files

- `Terminal.Gui\ViewBase\View.Drawing.cs` - All drawing methods
- `Terminal.Gui\ViewBase\View.Layout.cs` - Layout system
- `Tests\UnitTestsParallelizable\ViewBase\Draw\ViewDrawingClippingTests.cs` - This test

### Key Methods

- `View.Draw(DrawContext?)` - Main instance draw method
- `DoDrawSubViews(DrawContext?)` - Recursive subview drawing
- `DoDrawContent(DrawContext?)` - Fires DrawingContent event
- `DoDrawComplete(DrawContext?)` - Updates clip after drawing
- `AddViewportToClip()` - Intersects clip with viewport
- `ExcludeFromClip(Rectangle/Region)` - Excludes area from clip

### Key Concepts

- **Driver.Clip** - Region where drawing is allowed
- **Viewport** - Visible content area of a view
- **Wide Glyph** - Character occupying 2 columns (e.g., ??)
- **Clip Exclusion** - Removing drawn areas from clip
- **Half-Glyph** - Wide glyph partially clipped (renders as `?`)
- **DrawContext** - Tracks drawn regions for transparency
