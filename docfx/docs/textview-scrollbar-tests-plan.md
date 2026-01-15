# Plan: TextView Scrollbar Unit Tests for Content Changes

## Summary

Add failing unit tests that expose bugs in TextView's scrollbar integration when content changes. These tests should pass once the bugs are fixed.

## Test Results (After Initial Fix)

| Test | Result | Notes |
|------|--------|-------|
| `ContentSize_Updates_When_Lines_Inserted` | **PASS** | Content size IS updated via Model_LinesLoaded |
| `ContentSize_Updates_When_Lines_Deleted` | **PASS** | Content size IS updated via Model_LinesLoaded |
| `VerticalScrollBar_Becomes_Visible_When_Content_Exceeds_Viewport` | **PASS** | Fixed by adding UpdateHorizontalScrollBarVisibility() call |
| `VerticalScrollBar_Hides_When_Content_Fits_Viewport` | **PASS** | Fixed by adding UpdateHorizontalScrollBarVisibility() call |
| `ScrollBar_Position_Change_Updates_Viewport_After_Content_Change` | **PASS** | Fixed by adding UpdateHorizontalScrollBarVisibility() call |
| `Viewport_Change_Updates_ScrollBar_Position` | **PASS** | Scrollbar position binding works |
| `HorizontalScrollBar_Becomes_Visible_When_Line_Exceeds_Width` | **PASS** | Fixed by adding UpdateHorizontalScrollBarVisibility() call |
| `WordWrap_Toggle_Updates_ContentSize` | **PASS** | WordWrap correctly updates content size |
| `ScrollBar_VisibleContentSize_Is_Set_After_Layout` | **PASS** | VisibleContentSize is properly set |
| `ScrollBar_Position_Change_Actually_Updates_Viewport` | **PASS** | Scrollbar position change works |
| `ScrollBar_Position_Not_Reset_By_AdjustViewport_When_Cursor_At_Start` | **FAIL** | **NEW BUG** - AdjustViewport resets scroll position |
| `ScrollBar_Position_Maintained_After_Focus` | **PASS** | Focus change doesn't reset scroll |

## Root Cause Analysis (Refined After Testing)

### Primary Bug: Missing `UpdateHorizontalScrollBarVisibility()` call when content changes

**The Problem:**
- Content size IS correctly updated when Text changes (via `Model_LinesLoaded` -> `UpdateContentSize()`)
- `SetContentSize()` fires `ContentSizeChanged` event correctly
- Scrollbar's `ScrollableContentSize` IS correctly updated (via event binding in View.ScrollBars.cs:85-88)
- **BUT `UpdateHorizontalScrollBarVisibility()` is NEVER called**, so scrollbar `Visible` property is never updated

**Evidence from tests:**
- `GetContentSize().Height` correctly returns 10 after setting 10-line text
- `VerticalScrollBar.ScrollableContentSize` correctly returns 10
- `VerticalScrollBar.Visible` remains `false` even though content exceeds viewport

**Where `UpdateHorizontalScrollBarVisibility()` IS called:**
1. `UpdateScrollBars()` - called from EndInit() and ScrollBars property setter
2. `OnViewportChanged()` - when viewport changes

**Where `UpdateHorizontalScrollBarVisibility()` IS NOT called:**
- After `Text` property changes
- After `Model_LinesLoaded` event
- After `UpdateContentSize()`

### Secondary Issue (Original analysis was incorrect)

The original analysis about missing `UpdateContentSize()` calls in `OnContentsChanged()` was **partially wrong**:
- For `Text` property setter: `Model_LinesLoaded` IS called, which DOES call `UpdateContentSize()`
- For character-by-character typing via commands: `OnContentsChanged()` is called but does NOT call `UpdateContentSize()` (this may still be a bug for incremental edits)

### Root Cause Location

The bug is in `Model_LinesLoaded` at TextView.Text.cs:605-621:
```csharp
private void Model_LinesLoaded (object sender, EventArgs e)
{
    // ... existing code ...
    UpdateContentSize ();
    // MISSING: UpdateHorizontalScrollBarVisibility();
}
```

Or alternatively, `UpdateContentSize()` at TextView.Scrolling.cs:47-58 should call visibility update:
```csharp
private void UpdateContentSize ()
{
    if (!IsInitialized) return;

    int contentHeight = _model.Count;
    int contentWidth = WordWrap ? Viewport.Width : _model.GetMaxVisibleLine (0, _model.Count, TabWidth);

    SetContentSize (new Size (contentWidth, contentHeight));
    // MISSING: UpdateHorizontalScrollBarVisibility();
}
```

## Test Plan

### Test File Location
`Tests/UnitTestsParallelizable/Views/TextViewScrollingTests.cs`

### Tests to Add

#### 1. Content Size Updates on Text Insert
```csharp
[Fact]
public void ContentSize_Updates_When_Lines_Inserted ()
// Setup: TextView with ScrollBars=true, small viewport
// Act: Insert text that adds lines
// Assert: GetContentSize().Height reflects new line count
```

#### 2. Content Size Updates on Text Delete
```csharp
[Fact]
public void ContentSize_Updates_When_Lines_Deleted ()
// Setup: TextView with multi-line content
// Act: Delete lines
// Assert: GetContentSize().Height reflects reduced line count
```

#### 3. Scrollbar Visibility Changes When Content Grows
```csharp
[Fact]
public void VerticalScrollBar_Becomes_Visible_When_Content_Exceeds_Viewport ()
// Setup: TextView with ScrollBars=true, content fits in viewport
// Verify: Scrollbar not visible initially
// Act: Insert text that exceeds viewport height
// Assert: VerticalScrollBar.Visible == true
```

#### 4. Scrollbar Visibility Changes When Content Shrinks
```csharp
[Fact]
public void VerticalScrollBar_Hides_When_Content_Fits_Viewport ()
// Setup: TextView with ScrollBars=true, content exceeds viewport (scrollbar visible)
// Act: Delete text until content fits
// Assert: VerticalScrollBar.Visible == false
```

#### 5. Scrollbar Position Can Be Changed After Content Change
```csharp
[Fact]
public void ScrollBar_Position_Change_Updates_Viewport_After_Content_Change ()
// Setup: TextView with ScrollBars=true
// Act: Insert text exceeding viewport, then change scrollbar position
// Assert: Viewport.Y changes to match scrollbar position
// This tests the specific bug the user reported
```

#### 6. Viewport Change Updates Scrollbar Position
```csharp
[Fact]
public void Viewport_Change_Updates_ScrollBar_Position ()
// Setup: TextView with content exceeding viewport
// Act: Programmatically set Viewport.Y
// Assert: VerticalScrollBar.Position matches Viewport.Y
```

#### 7. Horizontal Scrollbar With Long Lines
```csharp
[Fact]
public void HorizontalScrollBar_Becomes_Visible_When_Line_Exceeds_Width ()
// Setup: TextView with ScrollBars=true, WordWrap=false
// Act: Insert very long line
// Assert: HorizontalScrollBar.Visible == true AND GetContentSize().Width reflects line length
```

#### 8. Word Wrap Changes Update Content Size
```csharp
[Fact]
public void WordWrap_Toggle_Updates_ContentSize ()
// Setup: TextView with long lines that would wrap
// Act: Toggle WordWrap on
// Assert: ContentSize height increases (more lines from wrapping)
```

## Files to Modify

1. **Create:** `Tests/UnitTestsParallelizable/Views/TextViewScrollingTests.cs`
   - New test class with all tests above

## Verification

Run tests before and after fix:
```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~TextViewScrollingTests" --no-build
```

Expected results:
- Before fix: Tests 3, 4, 5, 7, 8 should FAIL (expose the bug)
- After fix: All tests should PASS

## Fix Approach (For Reference)

The simplest fix would be to add `UpdateHorizontalScrollBarVisibility()` call at the end of `UpdateContentSize()`:

```csharp
// In TextView.Scrolling.cs
private void UpdateContentSize ()
{
    if (!IsInitialized) return;

    int contentHeight = _model.Count;
    int contentWidth = WordWrap ? Viewport.Width : _model.GetMaxVisibleLine (0, _model.Count, TabWidth);

    SetContentSize (new Size (contentWidth, contentHeight));
    UpdateHorizontalScrollBarVisibility ();  // ADD THIS LINE
}
```

**Alternative approaches:**
1. Call `UpdateHorizontalScrollBarVisibility()` in `Model_LinesLoaded` after `UpdateContentSize()`
2. Subscribe to `ContentSizeChanged` event in TextView and call `UpdateHorizontalScrollBarVisibility()`
3. Override `OnContentSizeChanged` and call visibility update there

**Follow the ListView pattern (ListView.cs:821):**
ListView calls `SetContentSize()` in `OnViewportChanged()` which triggers the View base class's viewport/scrollbar synchronization. TextView could similarly ensure that any content change flows through to visibility updates.

**Additional consideration for incremental edits:**
The tests use `Text` property setter which triggers `Model_LinesLoaded`. For character-by-character typing (via keyboard commands), `OnContentsChanged()` is called but may not trigger the same path. This should be verified with additional tests if needed.

## Secondary Bug: AdjustViewport Resets Scrollbar Position

**The Problem:**
When the user scrolls via scrollbar, `Viewport.Y` changes correctly. However, `AdjustViewport()` is called from `OnSubViewsLaidOut()` (and many other places) and it **resets the viewport to show the cursor**, effectively undoing the user's scroll action.

**Evidence:**
Test `ScrollBar_Position_Not_Reset_By_AdjustViewport_When_Cursor_At_Start` FAILS:
1. Set `tv.VerticalScrollBar.Position = 5` → `Viewport.Y = 5`
2. Call `tv.LayoutSubViews()`
3. `Viewport.Y` is reset to 0 (cursor position)

**Root Cause Location:**
`AdjustViewport()` in TextView.Scrolling.cs:74-134 always adjusts viewport to ensure cursor is visible:
```csharp
// Handle vertical scrolling - this forces viewport to show cursor
if (CurrentRow < Viewport.Y)
{
    Viewport = Viewport with { Y = CurrentRow };  // Forces viewport back to cursor!
    need = true;
}
```

When cursor is at row 0 and user scrolls to row 5, this condition `CurrentRow < Viewport.Y` (0 < 5) is true, so viewport is reset to 0.

**Fix Approach:**
`AdjustViewport()` should NOT adjust viewport when the user is explicitly scrolling via scrollbar. Options:
1. Add a flag to track "user is scrolling" and skip cursor-following logic
2. Only call `AdjustViewport()` when cursor position changes, not on every layout
3. Separate "ensure cursor visible" logic from "adjust viewport" logic
4. Check if cursor is already visible and only adjust if it's NOT visible

The pattern from CharMap/HexView should be studied - they don't have this problem because they don't have an `AdjustViewport()` function that forces cursor visibility on every layout.
