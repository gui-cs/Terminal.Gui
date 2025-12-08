# Bug Fix: Wide Glyph Overlap with Border Drawing

## Problem

Borders drawn at odd columns (1, 3, etc.) that overlap wide glyphs (e.g., ?? which spans 2 columns) would not render their left border characters (`?`, `?`, `?`). Only horizontal and right border characters would appear.

**Example:** Border at column 1, with apple ?? at column 0
- Expected: `??????` (replacement char, left corner, horizontal, right corner, apple)
- Actual: `?????` (missing `?`)

This occurred because content drawn at odd columns (the second column of a wide glyph) would either not render at all or render with incorrect colors.

## Root Cause

In `OutputBufferImpl.AddStr()`, when a wide character (width=2) was written at column N, the code would:

1. Write the wide character's grapheme to column N and mark it `IsDirty = true`
2. Increment `Col` to N+1 (the second column of the wide character)
3. **Mark column N+1 as `IsDirty = false`** to prevent it from rendering separately
4. **Set column N+1's attribute** to match the wide character's attribute

The intent was to prevent the second column from being rendered independently, since wide characters naturally span both columns when rendered to the terminal.

**The bug:** When a border later wrote to column N+1:
- The border correctly wrote its character and set `IsDirty = true`
- But on any subsequent draw pass, column N+1 would be marked `IsDirty = false` again
- Result: The border character was skipped during rendering, or rendered with the wrong colors

## Fix

**Removed the entire block that modified the second column of wide characters:**

```csharp
if (textWidth > 1)
{
    // REMOVED:
    // if (validLocation)
    // {
    //     lock (Contents!)
    //     {
    //         if (Col < Cols && Row < Rows && Col < clipRect.Right)
    //         {
    //             Contents [Row, Col].IsDirty = false;
    //             Contents [Row, Col].Attribute = CurrentAttribute;
    //         }
    //     }
    // }
    
    Col++;  // Simply skip to the second column without modifying its state
}
```

**Why this works:**
- When a wide character is written at column N, only column N is modified
- Column N+1 is left untouched - it retains whatever state it had (dirty/clean, attribute intact)
- If content is later drawn at column N+1 (like a border), it marks the cell dirty and writes normally
- The rendering system already handles wide glyphs correctly - they naturally render across both columns
- No manual intervention needed to prevent column N+1 from rendering

## Testing Improvements

Added two unit tests in `ViewDrawingClippingTests.cs`:
- `Draw_WithBorderSubView_At_Col1_In_WideGlyph_DrawsCorrectly` - Tests border at column 1
- `Draw_WithBorderSubView_At_Col3_In_WideGlyph_DrawsCorrectly` - Tests border at column 3

**New test infrastructure:** Created `DriverAssert.AssertDriverOutputIs()` helper in `Tests/UnitTests/DriverAssert.cs` to verify raw ANSI output from the driver. This enables testing the actual terminal output sequences, not just the internal buffer state. This was essential for catching this bug, as the buffer contents looked correct but the output was wrong.

## Impact

This fix resolves:
- Borders at odd columns appearing incomplete when overlapping wide glyphs
- Any single-width content at odd columns failing to render over wide glyphs
- Incorrect colors for content drawn at odd columns over wide glyphs

The fix maintains correct wide glyph rendering while allowing proper layering of content at any column position.

## Files Changed

- **Fixed:** `Terminal.Gui/Drivers/OutputBufferImpl.cs` (AddStr method, ~line 232)
- **Tests:** `Tests/UnitTestsParallelizable/ViewBase/Draw/ViewDrawingClippingTests.cs`
  - Added `Draw_WithBorderSubView_At_Col1_In_WideGlyph_DrawsCorrectly`
  - Added `Draw_WithBorderSubView_At_Col3_In_WideGlyph_DrawsCorrectly`
- **Test Infrastructure:** `Tests/UnitTests/DriverAssert.cs`
  - Added `AssertDriverOutputIs()` for verifying raw ANSI output

---
*2025-01-20*
