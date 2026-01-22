# ListView Multi-Selection and Rendering Fixes - Implementation Plan

## Executive Summary

Fixes nine related issues in ListView (Issue #4580):

1. ✅ **AllowsMultipleSelection without AllowsMarking** - Add independent selection tracking
2. ✅ **Keyboard Shift+Arrow extension** - Add Command.UpExtend/DownExtend support
3. ✅ **Mouse Shift+Click extension** - Detect Shift modifier in Command.Activate
4. ✅ **Shift+Space behavior** - Make Command.Activate work for keyboard when AllowsMarking=true
5. ✅ **Mark rendering attribute** - Marks should always use Normal attribute for clarity
6. ✅ **Custom mark rendering API** - Allow IListDataSource to override mark rendering
7. ✅ **Horizontal scrolling with marks** - Content area width is 2 columns too narrow when AllowsMarking=true
8. ✅ **Horizontal scrolling max offset** - Scrolling continues until only last column visible; should stop at (contentWidth - viewportWidth)
9. ✅ **Vertical scrolling max offset** - Scrolling continues until only last row visible; should stop at (contentHeight - viewportHeight)

## Design Philosophy

**Separation of Concerns:**
- **Marking** = Data operation (checkboxes in data source) controlled by `AllowsMarking`
- **Selection** = UI/navigation operation (highlighting for actions) controlled by `AllowsMultipleSelection`
- These systems should work independently or together

**Pattern:** Follow TableView's proven multi-selection architecture while maintaining backward compatibility.

## Implementation Details

### 1. Add Selection Tracking (Independent of Marking)

**File:** `Terminal.Gui/Views/ListView.cs`

**Add properties:**
```csharp
/// <summary>
/// When <see cref="AllowsMultipleSelection"/> is enabled, contains indices of all selected items.
/// Independent of <see cref="AllowsMarking"/>.
/// </summary>
public HashSet<int> MultiSelectedItems { get; } = [];

/// <summary>
/// Anchor point for range selection operations (Shift+Click, Shift+Arrow).
/// </summary>
private int? _selectionAnchor;
```

**Update SelectedItem setter:**
```csharp
public int? SelectedItem
{
    get;
    set
    {
        if (Source is null) return;
        if (value.HasValue && (value < 0 || value >= Source.Count))
            throw new ArgumentException("SelectedItem must be >= 0 and < Count");

        field = value;
        _selectionAnchor = value; // Reset anchor when directly setting SelectedItem
        OnSelectedChanged();
        SetNeedsDraw();
    }
}
```

### 2. Add Selection Management Methods

**File:** `Terminal.Gui/Views/ListView.cs`

**Add SetSelection method (like TableView):**
```csharp
/// <summary>
/// Sets selected item, optionally extending selection to create a range.
/// </summary>
/// <param name="item">Item index to select</param>
/// <param name="extendExistingSelection">
/// If true and <see cref="AllowsMultipleSelection"/> enabled,
/// extends from <see cref="_selectionAnchor"/> to <paramref name="item"/>
/// </param>
public void SetSelection (int item, bool extendExistingSelection)
{
    if (Source is null || item < 0 || item >= Source.Count)
        return;

    if (!AllowsMultipleSelection || !extendExistingSelection)
    {
        MultiSelectedItems.Clear();
        _selectionAnchor = item;
    }
    else if (extendExistingSelection && _selectionAnchor.HasValue)
    {
        // Create range from anchor to item
        MultiSelectedItems.Clear();
        int start = Math.Min(_selectionAnchor.Value, item);
        int end = Math.Max(_selectionAnchor.Value, item);
        for (int i = start; i <= end; i++)
            MultiSelectedItems.Add(i);
    }

    SelectedItem = item;
    EnsureSelectedItemVisible();
    SetNeedsDraw();
}
```

**Add helper methods:**
```csharp
/// <summary>
/// Gets all selected item indices (SelectedItem + MultiSelectedItems).
/// </summary>
public IEnumerable<int> GetAllSelectedItems()
{
    HashSet<int> all = [.. MultiSelectedItems];
    if (SelectedItem.HasValue)
        all.Add(SelectedItem.Value);
    return all.OrderBy(i => i);
}

/// <summary>
/// Returns true if item is selected.
/// </summary>
public bool IsSelected(int item)
{
    return item == SelectedItem || MultiSelectedItems.Contains(item);
}

/// <summary>
/// Selects all items when AllowsMultipleSelection is true.
/// </summary>
public void SelectAll()
{
    if (!AllowsMultipleSelection || Source is null) return;
    MultiSelectedItems.Clear();
    for (int i = 0; i < Source.Count; i++)
        MultiSelectedItems.Add(i);
    SetNeedsDraw();
}

/// <summary>
/// Clears multi-selection.
/// </summary>
public void UnselectAll()
{
    MultiSelectedItems.Clear();
    SetNeedsDraw();
}
```

### 3. Update Command Handlers

**File:** `Terminal.Gui/Views/ListView.cs` (constructor)

**Replace Up/Down commands:**
```csharp
// Replace existing
AddCommand(Command.Up, ctx => RaiseActivating(ctx) == true || MoveUp(false));
AddCommand(Command.Down, ctx => RaiseActivating(ctx) == true || MoveDown(false));

// Add extend commands
AddCommand(Command.UpExtend, ctx => RaiseActivating(ctx) == true || MoveUp(true));
AddCommand(Command.DownExtend, ctx => RaiseActivating(ctx) == true || MoveDown(true));
AddCommand(Command.PageUpExtend, () => MovePageUp(true));
AddCommand(Command.PageDownExtend, () => MovePageDown(true));
AddCommand(Command.StartExtend, () => MoveHome(true));
AddCommand(Command.EndExtend, () => MoveEnd (true));
```

**Update Command.Activate handler:**
```csharp
AddCommand(Command.Activate,
    ctx =>
    {
        if (RaiseActivating(ctx) == true)
            return true;

        if (!HasFocus && CanFocus)
            SetFocus();

        // Mouse handling
        if (ctx?.Binding is MouseBinding { MouseEvent: { } mouse })
        {
            Point position = mouse.Position!.Value;
            int index = Viewport.Y + position.Y;

            if (Source is { } && index < Source.Count)
            {
                bool shift = mouse.Flags.HasFlag(MouseFlags.Shift);
                bool ctrl = mouse.Flags.HasFlag(MouseFlags.Ctrl);

                if (ctrl)
                {
                    // Toggle item in selection (union)
                    if (MultiSelectedItems.Contains(index))
                        MultiSelectedItems.Remove(index);
                    else
                        MultiSelectedItems.Add(index);
                    SelectedItem = index;
                    _selectionAnchor = index;
                    SetNeedsDraw();
                }
                else
                {
                    SetSelection(index, shift);
                }
            }
            return true;
        }

        // Keyboard Space - toggle marking if AllowsMarking
        if (AllowsMarking && SelectedItem.HasValue)
        {
            MarkUnmarkSelectedItem();
            return true;
        }

        return true;
    });
```

### 4. Update Movement Methods

**File:** `Terminal.Gui/Views/ListView.cs`

**Update signatures to accept `bool extend` parameter:**
```csharp
public bool MoveDown(bool extend = false)
{
    if (Source is null || Source.Count == 0)
        return false;

    bool moved = false;
    if (SelectedItem is null || SelectedItem >= Source.Count)
    {
        SetSelection(SelectedItem is null ? 0 : Source.Count - 1, extend);
        moved = true;
    }
    else if (SelectedItem + 1 < Source.Count)
    {
        SetSelection(SelectedItem.Value + 1, extend);
        // Update viewport as needed...
        moved = true;
    }
    return moved;
}

// Similarly update: MoveUp, MovePageDown, MovePageUp, MoveHome, MoveEnd
```

### 5. Add Key Bindings

**File:** `Terminal.Gui/Views/ListView.cs` (constructor)

```csharp
// Shift+Arrow for extending selection
KeyBindings.Add(Key.CursorUp.WithShift, Command.UpExtend);
KeyBindings.Add(Key.CursorDown.WithShift, Command.DownExtend);
KeyBindings.Add(Key.PageUp.WithShift, Command.PageUpExtend);
KeyBindings.Add(Key.PageDown.WithShift, Command.PageDownExtend);
KeyBindings.Add(Key.Home.WithShift, Command.StartExtend);
KeyBindings.Add(Key.End.WithShift, Command.EndExtend);

// Existing Shift+Space preserved for backward compatibility
// KeyBindings.Add(Key.Space.WithShift, Command.Activate, Command.Down);
```

### 6. Fix Mark Rendering Attribute

**File:** `Terminal.Gui/Views/ListView.cs` in `OnDrawingContent`

**Current issue (lines 762-767):** Marks rendered with row's attribute (Focus/Highlight/etc)

**Fix:** Render marks with Normal attribute:
```csharp
if (AllowsMarking)
{
    // Save current attribute
    Attribute savedAttr = current;

    // Render marks with Normal attribute for clarity
    Attribute normalAttr = GetAttributeForRole(VisualRole.Normal);
    if (current != normalAttr)
    {
        SetAttribute(normalAttr);
        current = normalAttr;
    }

    AddRune (Source.IsMarked (item)
        ? AllowsMultipleSelection ? Glyphs.CheckStateChecked : Glyphs.Selected
        : AllowsMultipleSelection ? Glyphs.CheckStateUnChecked : Glyphs.UnSelected);
    AddRune ((Rune)' ');

    // Restore attribute for content rendering
    if (current != savedAttr)
    {
        SetAttribute(savedAttr);
        current = savedAttr;
    }
}
```

### 7. Add Custom Mark Rendering API

**File:** `Terminal.Gui/Views/IListDataSource.cs`

**Add default interface method:**
```csharp
/// <summary>
/// Renders the mark indicator for an item. Override to customize mark rendering.
/// </summary>
/// <param name="listView">The ListView rendering to</param>
/// <param name="item">Item index</param>
/// <param name="row">Row in viewport</param>
/// <param name="isMarked">Whether item is marked</param>
/// <param name="allowsMultiple">Whether multiple selection is enabled</param>
/// <returns>True if custom rendering was done; false to use default</returns>
/// <remarks>
/// Default implementation returns false, causing ListView to use default mark rendering.
/// Override and return true to provide custom mark glyphs, positioning, or attributes.
/// When this returns false, ListView renders marks in columns 0-1, then calls Render() starting at column 2.
/// When this returns true, you must render marks yourself (if desired) and Render() will be called starting at column 0.
/// </remarks>
bool RenderMark(ListView listView, int item, int row, bool isMarked, bool allowsMultiple)
{
    return false; // Default: use ListView's rendering
}
```

**File:** `Terminal.Gui/Views/ListView.cs` in `OnDrawingContent`

**Update mark rendering to support custom:**
```csharp
int markWidth = 0; // Track width used by marks

if (AllowsMarking)
{
    // Try custom rendering first
    bool customRendered = Source.RenderMark(this, item, row, Source.IsMarked(item), AllowsMultipleSelection);

    if (customRendered)
    {
        // Custom renderer handled marks, col stays at 0
        // Custom renderer must handle mark width themselves
    }
    else
    {
        // Default rendering with Normal attribute
        Attribute savedAttr = current;
        Attribute normalAttr = GetAttributeForRole(VisualRole.Normal);
        if (current != normalAttr)
        {
            SetAttribute(normalAttr);
            current = normalAttr;
        }

        AddRune (Source.IsMarked (item)
            ? AllowsMultipleSelection ? Glyphs.CheckStateChecked : Glyphs.Selected
            : AllowsMultipleSelection ? Glyphs.CheckStateUnChecked : Glyphs.UnSelected);
        AddRune ((Rune)' ');
        markWidth = 2;

        if (current != savedAttr)
        {
            SetAttribute(savedAttr);
            current = savedAttr;
        }
    }
}

int col = markWidth;
Source.Render (this, isSelected, item, col, row, f.Width - col, start);
```

### 8. Fix Horizontal Scrolling Width Issue

**File:** `Terminal.Gui/Views/ListView.cs` in `OnDrawingContent`

**Current issue (line 726):** `int col = AllowsMarking ? 2 : 0;`

This sets col=2 when marking is enabled, but it's used both for:
1. Positioning the call to `Source.Render()` (correct)
2. Calculating available width: `f.Width - col` (incorrect - reduces content area by 2)

**Problem:** When horizontally scrolling with marks enabled, the content area is 2 columns narrower than it should be because `f.Width - col` subtracts the mark width from the available rendering width.

**Fix:** Use separate variables for mark offset and content width:
```csharp
int markWidth = AllowsMarking ? 2 : 0;  // Width reserved for marks
int contentStartCol = markWidth;         // Where content rendering starts

// ... mark rendering code ...

// Pass full width to Source.Render, it handles scrolling via viewportX
Source.Render (this, isSelected, item, contentStartCol, row, f.Width, start);
```

**Rationale:** The `viewportX` parameter (mapped to `start` variable) controls horizontal scrolling within the content. The `width` parameter should always be the full viewport width, not reduced by mark width. The Source.Render() implementation handles clipping to the actual available space.

### 11. Fix Horizontal/Vertical Scrolling Max Offsets

**Files:** `Terminal.Gui/Views/ListView.cs`

**Current issue:**
- Horizontal scrolling (`LeftItem` setter) allows scrolling until only the last column is visible
- Vertical scrolling (`TopItem` setter) allows scrolling until only the last row is visible
- Should clamp to `max(0, contentSize - viewportSize)`

**Investigation needed:**
Need to check how `LeftItem` and `TopItem` setters (or the underlying Viewport setters) handle maximum values. They should clamp to prevent over-scrolling.

**Expected behavior:**
```csharp
// Horizontal
int maxLeftItem = Math.Max(0, MaxItemLength - Viewport.Width);
if (value > maxLeftItem) value = maxLeftItem;

// Vertical
int maxTopItem = Math.Max(0, Source.Count - Viewport.Height);
if (value > maxTopItem) value = maxTopItem;
```

**Note:** This may already be handled by the View base class Viewport logic, need to verify during implementation.

### 9. Update Rendering for Multi-Selection Highlight

**File:** `Terminal.Gui/Views/ListView.cs` in `OnDrawingContent`

**Update attribute determination (around line 733):**
```csharp
for (var row = 0; row < f.Height; row++, item++)
{
    bool isSelectedItem = item == SelectedItem;
    bool isMultiSelected = MultiSelectedItems.Contains(item);

    // Determine visual role based on selection state
    VisualRole role;
    if (focused && isSelectedItem)
        role = VisualRole.Focus;           // Focused + SelectedItem
    else if (isMultiSelected)
        role = VisualRole.Highlight;       // In MultiSelectedItems
    else if (isSelectedItem)
        role = VisualRole.Active;          // SelectedItem without focus
    else
        role = VisualRole.Normal;          // Not selected

    Attribute newAttribute = GetAttributeForRole(role);
    // ... rest of rendering
}
```

### 10. Update AllowsMultipleSelection Property

**File:** `Terminal.Gui/Views/ListView.cs`

**Update setter:**
```csharp
public bool AllowsMultipleSelection
{
    get;
    set
    {
        field = value;

        if (Source is { } && !field)
        {
            // Clear multi-selection tracking
            MultiSelectedItems.Clear();

            // Clear marks except selected (existing behavior)
            for (var i = 0; i < Source.Count; i++)
            {
                if (Source.IsMarked(i) && SelectedItem.HasValue && i != SelectedItem.Value)
                    Source.SetMark(i, false);
            }
        }

        SetNeedsDraw();
    }
}
```

## Testing Strategy

**File:** `Tests/UnitTestsParallelizable/Views/ListViewTests.cs`

### New Tests to Add:

1. **Multi-selection without marking:**
   ```csharp
   [Fact]
   // Claude - Opus 4.5
   public void MultiSelect_Without_Marking_Uses_HashSet()
   {
       var lv = new ListView {
           Source = new ListWrapper<string>(["One", "Two", "Three"]),
           AllowsMultipleSelection = true,
           AllowsMarking = false
       };

       lv.SetSelection(0, false);
       lv.SetSelection(2, false);
       Assert.Empty(lv.MultiSelectedItems);  // Cleared on non-extend selection

       lv.SetSelection(0, false);
       lv.SetSelection(2, true);  // Extend
       Assert.Contains(0, lv.MultiSelectedItems);
       Assert.Contains(1, lv.MultiSelectedItems);
       Assert.Contains(2, lv.MultiSelectedItems);
   }
   ```

2. **Keyboard Shift+Down extends selection:**
   ```csharp
   [Fact]
   // Claude - Opus 4.5
   public void ShiftDown_Extends_Selection_Range()
   {
       var lv = new ListView {
           Source = new ListWrapper<string>(["1", "2", "3", "4"]),
           AllowsMultipleSelection = true
       };

       lv.SelectedItem = 0;
       lv.NewKeyDownEvent(Key.CursorDown.WithShift);

       Assert.Equal(1, lv.SelectedItem);
       Assert.True(lv.IsSelected(0));
       Assert.True(lv.IsSelected(1));
   }
   ```

3. **Mouse Shift+Click extends selection:**
   ```csharp
   [Fact]
   // Claude - Opus 4.5
   public void Mouse_ShiftClick_Extends_Selection()
   {
       var lv = new ListView {
           Source = new ListWrapper<string>(["1", "2", "3", "4"]),
           AllowsMultipleSelection = true,
           Height = 4
       };
       lv.BeginInit();
       lv.EndInit();
       lv.Draw();

       lv.SelectedItem = 0;
       lv.NewMouseEvent(new() {
           Position = new(0, 2),
           Flags = MouseFlags.LeftButtonPressed | MouseFlags.Shift
       });

       Assert.Equal(2, lv.SelectedItem);
       Assert.Contains(0, lv.MultiSelectedItems);
       Assert.Contains(1, lv.MultiSelectedItems);
       Assert.Contains(2, lv.MultiSelectedItems);
   }
   ```

4. **Mark rendering uses Normal attribute:**
   ```csharp
   [Fact]
   // Claude - Opus 4.5
   public void Marks_Rendered_With_Normal_Attribute()
   {
       var lv = new ListView {
           Source = new ListWrapper<string>(["One"]),
           AllowsMarking = true,
           Height = 1,
           Width = 10
       };
       lv.BeginInit();
       lv.EndInit();
       lv.SetFocus();
       lv.SelectedItem = 0;
       lv.Source.SetMark(0, true);
       lv.Draw();

       // Verify mark glyph is rendered with Normal attribute
       Attribute markAttr = lv.Screen[0, 0].Attribute;
       Assert.Equal(lv.GetAttributeForRole(VisualRole.Normal), markAttr);
   }
   ```

5. **Horizontal scrolling with marks:**
   ```csharp
   [Fact]
   // Claude - Opus 4.5
   public void HorizontalScroll_With_Marks_Shows_Full_Content()
   {
       var lv = new ListView {
           Source = new ListWrapper<string>(["0123456789ABCDEF"]),
           AllowsMarking = true,
           Width = 10,  // Narrow width to force scrolling
           Height = 1
       };
       lv.BeginInit();
       lv.EndInit();

       // Scroll right
       lv.LeftItem = 5;
       lv.Draw();

       // With marks (2 cols), should show: "☐ 56789AB" (8 chars: 2 for mark + 6 visible)
       // Verify content is not cut off by 2 extra columns
       TestHelpers.AssertDriverContentsAre(@"
☐ 56789AB", output);
   }
   ```

6. **Preserve existing Shift+Space tests:**
   - `AllowsMarking_True_SpaceWithShift_SelectsThenDown_SingleSelection` (line 350)
   - `AllowsMarking_True_SpaceWithShift_SelectsThenDown_MultipleSelection` (line 411)

## Backward Compatibility

✅ **No breaking changes:**
- Existing `AllowsMarking=true` behavior unchanged
- Existing Shift+Space tests continue to pass
- Space toggles marking when `AllowsMarking=true`
- `AllowsMultipleSelection=false` clears multi-selection
- Default interface method in `IListDataSource` (no implementation required)
- Existing IListDataSource implementations continue to work

✅ **New capabilities enabled:**
- Multi-selection works without marking
- Shift+Arrow extends selection
- Shift+Click extends selection
- Ctrl+Click toggles individual items
- Custom mark rendering possible
- Horizontal scrolling width fixed

## Verification Steps

1. **Build and test:**
   ```bash
   dotnet build --no-restore
   dotnet test Tests/UnitTestsParallelizable --no-build --filter "ClassName~ListViewTests"
   ```

2. **Run UICatalog scenario:**
   ```bash
   cd Examples/UICatalog
   dotnet run -- ListViewWithSelection
   ```
   - Toggle "AllowsMarking" OFF, "AllowsMultiSelect" ON
   - Test Shift+Down/Up extends selection (items highlighted)
   - Test Shift+Click extends selection
   - Test Ctrl+Click toggles items
   - Toggle "AllowsMarking" ON
   - Verify marks render with Normal attribute (clearly visible when row selected)
   - Test horizontal scrolling - content should not be cut off

3. **Verify no regressions:**
   ```bash
   dotnet test Tests/UnitTestsParallelizable --no-build
   ```

## Critical Files

- **Terminal.Gui/Views/ListView.cs** - Core implementation (~250 lines changed)
- **Terminal.Gui/Views/IListDataSource.cs** - Add RenderMark method (~20 lines)
- **Terminal.Gui/Views/TableView/TableView.Selection.cs** - Reference pattern
- **Terminal.Gui/Views/TableView/TableView.Mouse.cs** - Reference for mouse handling
- **Tests/UnitTestsParallelizable/Views/ListViewTests.cs** - Add tests (~150 lines)
- **Examples/UICatalog/Scenarios/ListViewWithSelection.cs** - Manual testing scenario

## Summary of Changes

| Issue | Solution | Estimated LOC |
|-------|----------|---------------|
| Multi-select without marking | Add `MultiSelectedItems` HashSet | ~50 |
| Keyboard Shift+Arrow | Add extend commands and keybindings | ~30 |
| Mouse Shift+Click | Update Command.Activate handler | ~20 |
| Shift+Space behavior | Fixed by Command.Activate update | ~5 |
| Mark rendering attribute | Wrap mark rendering with attribute save/restore | ~15 |
| Custom mark rendering | Add RenderMark default interface method | ~30 |
| Horizontal scrolling width | Fix width calculation in rendering | ~10 |
| Scroll offset clamping | Clamp horiz/vert scroll to proper max values | ~20 |
| Update movement methods | Add `extend` parameter to all Move* methods | ~60 |
| Update rendering | Use VisualRole.Highlight for multi-selected | ~15 |
| Tests | Add comprehensive test coverage | ~150 |
| **Total** | | **~405 lines** |
