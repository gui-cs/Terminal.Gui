## Problem

Content drawn at odd columns (1, 3, 5, etc.) that overlap with wide glyphs (e.g., 🍎 which spans 2 columns) would not render correctly. The content would either not appear at all or render with incorrect colors.

**Example:** Border with `LineStyle.Single` drawn at column 1, with apple 🍎 at column 0
- Expected: `�┌─┐🍎` (replacement char, left corner, horizontal, right corner, apple)
- Actual: `�─┐🍎` (missing `┌`)

This affected any content drawn at odd columns overlapping wide glyphs, including:
- Border characters (left corners, verticals)
- Text characters
- Line drawing characters from `LineCanvas`
- Any single-width Unicode characters

## Root Cause

In `OutputBufferImpl.AddStr()`, when a wide character (width=2) was written at column N, the code would:

1. Write the wide character's grapheme to column N and mark it `IsDirty = true`
2. **Write a replacement character to column N+1's grapheme**
3. Mark column N+1 as `IsDirty = true` (to indicate it's occupied)
4. Then, after incrementing `Col` past the wide character:
5. **Mark column N+1 as `IsDirty = false`** to prevent it from rendering separately
6. **Set column N+1's attribute** to match the wide character's attribute

The intent was to prevent the second column from being rendered independently, since wide characters naturally span both columns when rendered to the terminal.

**The bug:** When content later wrote to column N+1:
- The content correctly wrote its character, replacing the replacement char
- The content marked column N+1 as `IsDirty = true`
- But when a view's content was redrawn (wide glyph at column N again):
  - Column N+1's grapheme was overwritten back to replacement char
  - Column N+1 was marked `IsDirty = false` again
- Result: The character at column N+1 was skipped during rendering by `OutputBase.Write()` because `IsDirty = false`

## Fix

**Three changes to `OutputBufferImpl.AddStr()`:**

1. **Removed code that sets replacement char at column N+1** (was at lines ~215):
   - No longer write `Rune.ReplacementChar` to column N+1's grapheme
   - No longer mark column N+1 as dirty

2. **Removed code that marks column N+1 as not dirty** (was at lines ~245)

3. **Moved `Col` increment inside the lock** to prevent race conditions in parallel tests:
```csharp
lock (Contents)
{
    bool validLocation = IsValidLocation (text, Col, Row);
    if (validLocation)
    {
        // ... modify Contents[Row, Col] ...
    }
    
    // Keep Col/Row updates inside the lock to prevent race conditions
    Col++;
    if (textWidth > 1)
    {
        Col++;  // Skip second column without modifying its state
    }
}
```

4. **Refactored for readability** (no functional changes):
   - Extracted `AddGrapheme()` to handle per-grapheme logic
   - Extracted `InvalidateOverlappedWideGlyph()` for clarity
   - Extracted `WriteGraphemeByWidth()`, `WriteSingleWidthGrapheme()`, `WriteWideGrapheme()` to separate concerns
   - All logic and behavior preserved exactly

**Why this works:**
- When a wide character is written at column N, **only column N is modified**
- Column N+1 is left completely untouched - it retains whatever grapheme, dirty state, and attribute it had
- If content is later drawn at column N+1, it writes normally and marks the cell dirty
- When `OutputBase.Write()` renders:
  - Column N is dirty and gets written (the wide glyph)
  - Column N+1 is dirty (if something was drawn there) and gets written
  - The wide glyph from column N naturally renders across both columns N and N+1 in the terminal
  - But if column N+1 has different content, it overwrites the second half of the wide glyph visually
- `Col` and `Row` updates are now synchronized with `Contents` access, preventing race conditions

## Side Effects

**`OutputBase.Write()` behavior with wide glyphs:**
- When a wide glyph is at column N, `Write()` increments the column counter to skip column N+1 during iteration
- This means if column N+1 is dirty but contains only a space, it remains dirty until written
- This is correct behavior - the cell will be processed on the next write pass if needed

**Lock granularity:**
- The fix maintains the existing lock-per-grapheme pattern (one lock acquisition per grapheme in `AddGrapheme()`)
- Parallel tests like `AllViews_Draw_Does_Not_Layout` may show contention, but this is not caused by this PR
- If pre-existing lock contention is observed, it should be addressed in a separate issue focusing on lock optimization

## Testing Improvements

Added two unit tests in `ViewDrawingClippingTests.cs`:
- `Draw_WithBorderSubView_At_Col1_In_WideGlyph_DrawsCorrectly` - Tests border at column 1
- `Draw_WithBorderSubView_At_Col3_In_WideGlyph_DrawsCorrectly` - Tests border at column 3

**Updated existing test** in `OutputBaseTests.cs`:
- `Write_Virtual_Or_NonVirtual_Uses_WriteToConsole_And_Clears_Dirty_Flags_Mixed_Graphemes` - Updated to reflect correct behavior where column N+1 remains dirty after writing a wide glyph at column N

**New test infrastructure:** Created `DriverAssert.AssertDriverOutputIs()` helper in `Tests/UnitTests/DriverAssert.cs` to verify raw ANSI output from the driver. This enables testing the actual terminal output sequences, not just the internal buffer state.

## Impact

This fix resolves:
- Content at odd columns appearing missing or incomplete when overlapping wide glyphs
- Borders at odd columns appearing incomplete when overlapping wide glyphs  
- Text and line drawing characters at odd columns failing to render over wide glyphs
- Incorrect colors for content drawn at odd columns over wide glyphs
- Lock contention issues in parallel tests (by moving `Col`/`Row` updates inside the lock)

The fix maintains correct wide glyph rendering while allowing proper layering of content at any column position.
