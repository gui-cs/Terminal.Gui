# Plan: Revert MultiSelectedItems and Fix AllowsMultipleSelection Naming

## Overview

This plan addresses two fundamental issues in PR #4581:

1. **Revert `MultiSelectedItems`** - This property was mistakenly added and duplicates/competes with the existing marking system (`IsMarked`)
2. **Fix `AllowsMultipleSelection` naming** - This property actually controls marking behavior, not selection, and should be renamed to `AllowsMultipleMarking`

## Background: Selection vs Marking

Based on [`listview-selection-vs-marking.md`](https://github.com/gui-cs/Terminal.Gui/blob/ed459cc60150afdc3875f69c85090ae1a4992952/docfx/docs/listview-selection-vs-marking.md):

- **Selection** = Singular focus/highlight (exactly ONE item at a time, or none)
  - Drives navigation and determines which item activates on ENTER
  - Property: `SelectedItem` (int?)

- **Marking** = Multi-item flagging for batch operations (zero or more items)
  - Visual indicators: `[x]` (marked) or `[ ]` (unmarked) when `AllowsMarking` is true
  - API: `IListDataSource.IsMarked()` / `SetMark()`

**The Problem:** `MultiSelectedItems` conflates these two distinct concepts. It creates an independent multi-selection system that competes with the marking system, causing confusion and violating the selection-is-singular principle.

## Critical Files

### Files to Modify (Remove MultiSelectedItems)

1. **Terminal.Gui/Views/ListView/ListView.Selection.cs**
   - Lines 195-199: Remove `MultiSelectedItems` property definition
   - Lines 18-19: Remove `MultiSelectedItems.Clear()` from `AllowsMultipleSelection` setter
   - Lines 61-71: Remove/rewrite `GetAllSelectedItems()` method
   - Line 79: Remove/rewrite `IsSelected()` method
   - Lines 100-115: Remove/rewrite `SelectAll()` method
   - Lines 125-165: Rewrite `SetSelection()` to work with marks instead
   - Lines 167-174: Remove `UnselectAll()` method
   - Lines 243-246: Remove anchor initialization in `SelectedItem` setter
   - Line 193: Remove `_selectionAnchor` field

2. **Terminal.Gui/Views/ListView/ListView.cs**
   - Lines 196-248: Rewrite mouse handling (Ctrl+Click, Shift+Click) to use marks directly

3. **Terminal.Gui/Views/ListView/ListView.Drawing.cs**
   - Lines 28, 37-39, 54-60: Remove multi-selection rendering logic
   - Revert to using only `IsMarked()` for highlight determination

4. **Terminal.Gui/Views/ListView/ListView.Movement.cs**
   - Review all movement methods - the `extend` parameter logic depends on `SetSelection()`
   - Rewrite to extend marks instead of MultiSelectedItems

### Files to Modify (Rename AllowsMultipleSelection)

1. **Terminal.Gui/Views/ListView/ListView.Selection.cs** (line 9)
   - Rename `AllowsMultipleSelection` → `AllowsMultipleMarking`
   - Update XML documentation to clarify it controls marking behavior
   - Update setter logic to work with marks

2. **All files referencing `AllowsMultipleSelection`:**
   - Terminal.Gui/Views/ListView/ListView.cs
   - Terminal.Gui/Views/ListView/ListView.Drawing.cs
   - Terminal.Gui/Views/ListView/ListView.Movement.cs
   - Tests/UnitTestsParallelizable/Views/ListViewTests.cs

### Test Files to Remove/Rewrite

**Tests/UnitTestsParallelizable/Views/ListViewTests.cs**
- Lines 1630-2615: Remove or rewrite tests for MultiSelectedItems
- Search for "Claude - Opus 4.5" comments and review related tests
- Keep tests that verify marking behavior with Shift+Click, Ctrl+Click
- Update tests to use `IsMarked()` instead of `MultiSelectedItems`

### Documentation to Update

1. **AGENTS.md** - Add plan file location preference
2. **docfx/docs/listview-selection-vs-marking.md** - Verify accuracy after changes
3. **API XML docs** in ListView files - Ensure consistency with selection vs marking terminology

## Implementation Steps

### Phase 1: Understand Current Mark-Based Implementation

1. Read `IListDataSource.cs` to understand the marking API:
   - `bool IsMarked(int item)`
   - `void SetMark(int item, bool value)`

2. Review how marking currently works in:
   - `ListView.cs` - Mark toggling, rendering
   - `ListView.Drawing.cs` - How marks are displayed
   - `ListWrapper.cs` - Default marking implementation

3. Identify all places where `MultiSelectedItems` is used and determine the equivalent mark-based approach

### Phase 2: Rewrite Core Selection/Marking Logic

**Goal:** Make the extend-selection feature work by directly manipulating marks instead of MultiSelectedItems

1. **Rewrite `SetSelection()` method** (ListView.Selection.cs:125-165)
   ```csharp
   // Pseudocode for new implementation:
   public void SetSelection (int item, bool extendExistingSelection)
   {
       if (!AllowsMultipleMarking || !extendExistingSelection)
       {
           // Single-selection mode: just move SelectedItem
           if (!AllowsMultipleMarking && AllowsMarking && Source is { })
           {
               // Clear all marks except the new selection
               for (int i = 0; i < Source.Count; i++)
               {
                   Source.SetMark(i, i == item);
               }
           }
           _selectionAnchor = item;
       }
       else if (extendExistingSelection && _selectionAnchor.HasValue && AllowsMarking && Source is { })
       {
           // Multi-selection mode: mark range from anchor to item
           int start = Math.Min(_selectionAnchor.Value, item);
           int end = Math.Max(_selectionAnchor.Value, item);

           for (int i = start; i <= end; i++)
           {
               Source.SetMark(i, true);
           }
       }

       SelectedItem = item;
       EnsureSelectedItemVisible();
       SetNeedsDraw();
   }
   ```

2. **Remove/rewrite helper methods:**
   - `GetAllSelectedItems()` → Use marks: iterate Source.IsMarked()
   - `IsSelected()` → Check if item == SelectedItem OR Source.IsMarked(item)
   - `SelectAll()` → Mark all items using SetMark()
   - `UnselectAll()` → Unmark all items using SetMark()

3. **Update `AllowsMultipleSelection` setter** (rename to `AllowsMultipleMarking`)
   - Remove MultiSelectedItems.Clear()
   - Keep the logic that clears marks except SelectedItem

### Phase 3: Rewrite Mouse Interaction

**File:** Terminal.Gui/Views/ListView/ListView.cs (lines 196-248)

1. **Ctrl+Click** should toggle mark state:
   ```csharp
   // Pseudocode:
   if (isCtrlPressed && AllowsMultipleMarking && AllowsMarking)
   {
       Source.SetMark(index, !Source.IsMarked(index));
   }
   ```

2. **Shift+Click** should use SetSelection with extend=true:
   ```csharp
   // Pseudocode:
   if (isShiftPressed && AllowsMultipleMarking)
   {
       SetSelection(index, extendExistingSelection: true);
   }
   ```

3. **Regular click** should use SetSelection with extend=false

### Phase 4: Rewrite Drawing Logic

**File:** Terminal.Gui/Views/ListView/ListView.Drawing.cs

1. Remove all references to `MultiSelectedItems`
2. Use `Source.IsMarked(item)` to determine highlight state
3. Ensure visual consistency:
   - Marked items show `[x]` when AllowsMarking is true
   - Marked items use appropriate highlight attribute

### Phase 5: Update Movement Commands

**File:** Terminal.Gui/Views/ListView/ListView.Movement.cs

1. Review all movement methods (MoveDown, MoveUp, MovePageDown, etc.)
2. Ensure the `extend` parameter properly calls `SetSelection(item, extend)`
3. The rewritten `SetSelection()` will handle marking automatically

### Phase 6: Rename AllowsMultipleSelection

1. **Rename property:** `AllowsMultipleSelection` → `AllowsMultipleMarking`

2. **Update all references** across:
   - Terminal.Gui/Views/ListView/*.cs
   - Tests/UnitTestsParallelizable/Views/ListViewTests.cs
   - Any UICatalog scenarios using the property

3. **Update XML documentation:**
   ```csharp
   /// <summary>
   ///     If set to <see langword="true"/> more than one item can be marked simultaneously.
   ///     If <see langword="false"/> marking an item will cause all others to be unmarked.
   ///     The default is <see langword="false"/>.
   ///     Requires <see cref="AllowsMarking"/> to be <see langword="true"/> for visual indicators.
   /// </summary>
   ```

4. **Consider backward compatibility:**
   - Add `[Obsolete]` attribute to old property name if needed
   - Or make clean break since this is pre-beta

### Phase 7: Remove/Rewrite Tests

**File:** Tests/UnitTestsParallelizable/Views/ListViewTests.cs

1. **Remove tests** that explicitly test MultiSelectedItems:
   - Lines 1630-2615 (large test section added for this feature)
   - Search for tests with "Claude - Opus 4.5" comments

2. **Rewrite tests** to verify mark-based behavior:
   - Ctrl+Click toggles marks
   - Shift+Click extends marks from anchor
   - Shift+Arrow keys extend marks
   - AllowsMultipleMarking controls multi-mark behavior
   - Ensure tests verify `Source.IsMarked()` state

3. **Keep tests** that verify:
   - Single selection behavior (SelectedItem)
   - Mark rendering
   - Keyboard navigation with extend commands

### Phase 8: Update Documentation

1. **Update AGENTS.md:**
   ```markdown
   ## Agent Configuration

   **Plan File Location:** All plan files should be created in `./plans/` directory at repository root.
   ```

2. **Verify listview-selection-vs-marking.md** reflects the implementation

3. **Update any conceptual docs** mentioning ListView selection/marking

## Verification Steps

After implementation, verify:

1. **Basic Selection:**
   - Arrow keys move SelectedItem
   - Only one item selected at a time
   - Enter activates SelectedItem

2. **Marking (AllowsMarking=true, AllowsMultipleMarking=false):**
   - Space toggles mark on SelectedItem
   - Only one item can be marked at a time
   - Marking one item unmarks others

3. **Multi-Marking (AllowsMarking=true, AllowsMultipleMarking=true):**
   - Ctrl+Click toggles individual marks
   - Shift+Click extends marks from anchor to clicked item
   - Shift+Arrow extends marks
   - Multiple items can be marked simultaneously

4. **Visual Rendering:**
   - Marked items show `[x]` when AllowsMarking=true
   - SelectedItem has focus highlight
   - No blank space issues (viewport clamping still works)

5. **Tests:**
   - All existing tests pass
   - New/rewritten tests verify mark-based behavior
   - Code coverage meets 70% threshold

## Testing Strategy

1. **Unit Tests (UnitTestsParallelizable):**
   - Test marking behavior with keyboard (Space, Shift+Arrow, Ctrl+Click simulation)
   - Test multi-marking with various combinations
   - Test AllowsMultipleMarking flag behavior
   - Test visual rendering of marks

2. **Manual Testing with UICatalog:**
   - Run ListView scenario
   - Test all keyboard combinations (Shift+Arrow, Shift+Home/End, Ctrl+Click, etc.)
   - Verify visual appearance matches expectations
   - Test with AllowsMarking on/off
   - Test with AllowsMultipleMarking on/off

3. **Coverage Validation:**
   - Run: `dotnet test --collect:"XPlat Code Coverage"`
   - Verify ListView.Selection.cs coverage > 70%
   - Verify ListView.Drawing.cs coverage > 70%
   - Verify ListView.Movement.cs coverage > 70%

## Risks and Considerations

1. **API Breaking Change:**
   - Renaming `AllowsMultipleSelection` → `AllowsMultipleMarking` breaks existing code
   - Mitigation: This is pre-beta, acceptable breakage
   - Alternative: Keep both names with Obsolete attribute

2. **Behavioral Change:**
   - Apps using AllowsMultipleSelection without AllowsMarking will need updates
   - The feature now requires both AllowsMarking=true AND AllowsMultipleMarking=true
   - Mitigation: Clear documentation and migration guide

3. **Test Coverage:**
   - Removing 60+ tests will decrease coverage temporarily
   - Need to replace with equivalent mark-based tests
   - Mitigation: Write new tests incrementally as code is modified

4. **Mouse Shift Key Detection:**
   - Original PR mentioned Shift key detection issues in dotnet/ansi drivers
   - This may still be a problem after revert
   - Mitigation: Document known limitation if it persists

## Files Summary

**Remove MultiSelectedItems from:**
- Terminal.Gui/Views/ListView/ListView.Selection.cs (property + usages)
- Terminal.Gui/Views/ListView/ListView.cs (mouse handling)
- Terminal.Gui/Views/ListView/ListView.Drawing.cs (rendering)
- Terminal.Gui/Views/ListView/ListView.Movement.cs (movement commands)
- Tests/UnitTestsParallelizable/Views/ListViewTests.cs (tests)

**Rename AllowsMultipleSelection in:**
- Terminal.Gui/Views/ListView/ListView.Selection.cs (property definition)
- All ListView partial classes (references)
- All test files (references)

**Update:**
- AGENTS.md (add plan location preference)
- docfx/docs/listview-selection-vs-marking.md (verify accuracy)

## Success Criteria

- [ ] MultiSelectedItems property removed from all files
- [ ] AllowsMultipleSelection renamed to AllowsMultipleMarking
- [ ] All marking logic uses IListDataSource.IsMarked/SetMark
- [ ] Ctrl+Click toggles marks correctly
- [ ] Shift+Click extends marks correctly
- [ ] Shift+Arrow extends marks correctly
- [ ] Tests updated and passing
- [ ] Code coverage ≥ 70% for modified files
- [ ] AGENTS.md updated with plan location
- [ ] No compilation errors or warnings
- [ ] UICatalog ListView scenario works correctly
