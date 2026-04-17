# Plan: ContentSize Independent Width/Height (#4964)

## Problem Statement
`ContentSize` currently treats width and height as a single atomic `Size?` value. This means
setting the content width always sets the content height (and vice versa). The issue requests
making width and height independently controllable.

## Design

### Core Idea
Replace the single `Size? _contentSize` backing field with two independent nullable ints:
`int? _contentWidth` and `int? _contentHeight`. Each dimension can be independently set to null
(tracks viewport) or an explicit value.

### New API Surface

| New Method | Description |
|-----------|-------------|
| `SetContentWidth(int?)` | Set content width independently. null = track viewport width |
| `SetContentHeight(int?)` | Set content height independently. null = track viewport height |
| `GetContentWidth()` | Returns `_contentWidth ?? Viewport.Width` |
| `GetContentHeight()` | Returns `_contentHeight ?? Viewport.Height` |

### Existing API (kept for backward compat)

| Method | Updated Behavior |
|--------|-----------------|
| `SetContentSize(Size?)` | Sets both dimensions. `null` clears both. |
| `GetContentSize()` | Returns `new Size(GetContentWidth(), GetContentHeight())` |
| `ContentSizeTracksViewport` | `true` when BOTH `_contentWidth` and `_contentHeight` are null. Setting to `true` clears both. |

### Events
- Keep existing `ContentSizeChanging`/`ContentSizeChanged` (with `Size?` args) - fires when either dimension changes
- Keep virtual methods `OnContentSizeChanging`/`OnContentSizeChanged`
- No new per-dimension events (can be added later if needed)

### CWP Pattern
Since we have two independent backing fields but one set of events using `Size?`, we need to
manually implement the CWP flow rather than using `CWPPropertyHelper.ChangeProperty` directly.
The flow:
1. Compute old composite Size? and new composite Size?
2. If equal, no-op
3. Fire OnContentSizeChanging / ContentSizeChanging (can cancel)
4. Update the backing field(s)
5. Call SetNeedsLayout()
6. Fire OnContentSizeChanged / ContentSizeChanged

## Implementation Steps

### Phase 1: Core API (View.Content.cs)
- [x] Replace `Size? _contentSize` with `int? _contentWidth` and `int? _contentHeight`
- [x] Add `SetContentWidth(int?)` and `SetContentHeight(int?)` methods
- [x] Add `GetContentWidth()` and `GetContentHeight()` methods
- [x] Update `GetContentSize()` to compose from the two values
- [x] Update `SetContentSize(Size?)` to delegate to internal helper
- [x] Update `ContentSizeTracksViewport` property
- [x] Implement CWP flow for dimension changes

### Phase 2: Update Callers in Library
- [x] Update all internal `GetContentSize().Width` → `GetContentWidth()` where appropriate
- [x] Update all internal `GetContentSize().Height` → `GetContentHeight()` where appropriate
- [x] Update all internal `SetContentSize(new Size(...))` where it makes sense

### Phase 3: Update Tests
- [x] Update `ContentSizeTests.cs` to test the new independent API
- [x] Add tests for independent width/height setting
- [x] Add tests for mixed scenarios (width set, height tracks viewport)
- [x] Ensure all existing tests pass

### Phase 4: Update Examples & Docs
- [x] Update UICatalog scenarios that use ContentSize
- [x] Update documentation references

## Files Affected

### Source (Terminal.Gui/)
- `ViewBase/View.Content.cs` - Core changes
- `ViewBase/View.Layout.cs` - GetContentSize() references
- `ViewBase/View.ScrollBars.cs` - GetContentSize() references
- `ViewBase/View.Drawing.cs` - GetContentSize() references
- `ViewBase/View.Drawing.Clipping.cs` - GetContentSize() references
- `ViewBase/View.Text.cs` - GetContentSize() references
- `ViewBase/Layout/DimAuto.cs` - GetContentSize() references
- `Views/Bar.cs`, `Views/Shortcut.cs`, etc. - SetContentSize/GetContentSize callers

### Tests
- `ContentSizeTests.cs` - Primary test file
- Various other test files that reference ContentSize

### Examples
- Various UICatalog scenarios
