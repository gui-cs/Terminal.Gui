# MouseClick/OnMouseClick API Removal Analysis

## Overview
This document provides a detailed analysis of all uses of the `OnMouseClick/MouseClick` API in the Terminal.Gui codebase, with complexity assessments for porting to `MouseBindings` and `Selecting` event pattern.

## API to be Removed
- `View.MouseClick` event (line 527 in View.Mouse.cs)
- `View.OnMouseClick()` virtual method (line 514 in View.Mouse.cs)
- `View.RaiseMouseClickEvent()` method (line 469 in View.Mouse.cs)

## Current Implementation Flow
```
NewMouseEvent() 
  -> RaiseMouseEvent() / OnMouseEvent() 
  -> if clicked: RaiseMouseClickEvent()
    -> OnMouseClick()
    -> MouseClick event
    -> InvokeCommandsBoundToMouse() -> Command.Select -> RaiseSelecting()
```

## Target Implementation Flow
```
NewMouseEvent() 
  -> RaiseMouseEvent() / OnMouseEvent()
  -> if clicked: InvokeCommandsBoundToMouse() -> Command.Select -> RaiseSelecting()
```

---

## Views Analysis

### 1. Button.cs - **SIMPLE** ✅
**Current Usage:**
- Lines 79: `MouseClick += Button_MouseClick;`
- Lines 118-127: Event handler that invokes `Command.HotKey`

**Complexity:** SIMPLE
- Already has proper command infrastructure via `HandleHotKeyCommand`
- The MouseClick handler just calls `InvokeCommand(Command.HotKey)`
- Can use: `MouseBindings.Add(MouseFlags.Button1Clicked, Command.HotKey)`

**Porting Strategy:**
1. Remove `MouseClick += Button_MouseClick;`
2. Remove `Button_MouseClick` method
3. Add: `MouseBindings.Replace(MouseFlags.Button1Clicked, Command.HotKey);`
4. Tests already exist that validate button clicks work

**Related Tests:**
- Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs

---

### 2. Label.cs - **SIMPLE** ✅
**Current Usage:**
- Line 30: `MouseClick += Label_MouseClick;`
- Lines 33-39: Event handler that invokes `Command.HotKey` when `!CanFocus`

**Complexity:** SIMPLE
- The MouseClick handler just invokes `Command.HotKey` when the label can't focus
- Already has `InvokeHotKeyOnNextPeer` command handler

**Porting Strategy:**
1. Remove `MouseClick += Label_MouseClick;`
2. Remove `Label_MouseClick` method
3. Override `OnSelecting` to invoke `Command.HotKey` when `!CanFocus`
4. Alternatively, use MouseBindings with conditional logic

**Related Tests:**
- Tests/UnitTestsParallelizable/Views/LabelTests.cs

---

### 3. ScrollBar.cs - **MODERATE** ⚠️
**Current Usage:**
- Line 520: Overrides `OnMouseClick(MouseEventArgs args)`
- Handles page-up/page-down style scrolling when clicking in the scrollbar track

**Complexity:** MODERATE
- Has custom logic for calculating position based on click location
- Needs to distinguish between clicking on slider vs track
- Logic is ~30 lines including distance calculations

**Porting Strategy:**
1. Override `OnSelecting` or create a custom command handler
2. Access mouse position from CommandContext
3. Keep the same position calculation logic
4. May need to use `OnMouseEvent` instead if more control needed

**Related Tests:**
- Need to check if tests exist for ScrollBar clicking

---

### 4. HexView.cs - **ALREADY DONE** ✅
**Current Usage:**
- Line 60: `AddCommand (Command.Select, HandleMouseClick);`
- Lines 359-390+: Command handler that processes mouse clicks

**Complexity:** NONE - Already using the target pattern!
- Already uses `AddCommand(Command.Select, ...)` 
- Already accesses mouse position via `CommandContext<MouseBinding>`
- No changes needed!

**Note:** This is the gold standard example of how to do it right.

---

### 5. TextView.cs - **COMPLEX** 🔴
**Current Usage:**
- Lines 1559, 1599, 1650, 1663, 1698, 1726: Calls `ProcessMouseClick()`
- Line 4046: Private method that calculates row/column from mouse position
- Called from `OnMouseEvent()` for various mouse flags

**Complexity:** COMPLEX
- ProcessMouseClick is called from multiple places in OnMouseEvent
- Used for Button1Clicked, Button1DoubleClicked, Button1TripleClicked, and dragging
- The logic is tightly integrated with the mouse event handling
- This is NOT using the MouseClick event/OnMouseClick - it's a private helper method

**Porting Strategy:**
- **NO CHANGES NEEDED** - This is a private helper method, not the API being removed
- The name is confusing but the functionality stays

---

### 6. TabView.cs - **MODERATE** ⚠️
**Current Usage:**
- Multiple lines: `tab.MouseClick += Tab_MouseClick!;` and `tab.Border!.MouseClick += Tab_MouseClick!;`
- Lines 389-392: Handler that forwards event to `_tabsBar.NewMouseEvent()`

**Complexity:** MODERATE
- Dynamically adds/removes MouseClick handlers as tabs are added/removed
- Forwards to another view's NewMouseEvent
- Need to ensure tab selection works via MouseBindings

**Porting Strategy:**
1. Instead of adding MouseClick handlers, handle clicks in the tab's Command.Select
2. Override OnSelecting in the tab to handle selection
3. Or, handle in TabView's OnMouseEvent and forward appropriately

**Related Tests:**
- Tests/UnitTests/Views/TabViewTests.cs

---

### 7. TabRow.cs - **SIMPLE** ✅
**Current Usage:**
- Lines with `_leftScrollIndicator.MouseClick` and `_rightScrollIndicator.MouseClick`
- Forwards to `_host.Tab_MouseClick`

**Complexity:** SIMPLE
- Similar to TabView, just for scroll indicators
- Can use Command.Select on the scroll indicator buttons

**Porting Strategy:**
1. Remove MouseClick event handlers
2. Use Command.Select or Accepting on the scroll indicator buttons

---

### 8. TreeTableSource.cs - **SIMPLE** ✅
**Current Usage:**
- Line ~: `_tableView.MouseClick += Table_MouseClick;`
- Handler expands/collapses tree nodes

**Complexity:** SIMPLE
- Just needs to handle expansion on click
- Can override OnSelecting or use MouseBindings

**Porting Strategy:**
1. Remove MouseClick handler
2. Use Selecting event or override OnSelecting
3. Keep the same expansion logic

---

### 9. CheckBoxTableSourceWrapper.cs - **SIMPLE** ✅
**Current Usage:**
- Line ~: `tableView.MouseClick += TableView_MouseClick;`
- Handler toggles checkboxes

**Complexity:** SIMPLE
- Toggles checkbox state on click
- Can use Selecting event

**Porting Strategy:**
1. Remove MouseClick handler
2. Use Selecting event or override OnSelecting
3. Keep the same toggle logic

---

### 10. FileDialog.cs - **SIMPLE** ✅
**Current Usage:**
- Line ~: `_tableView.MouseClick += OnTableViewMouseClick;`
- Handler for file selection

**Complexity:** SIMPLE
- Handles file selection in dialog
- Can use Selecting event

**Porting Strategy:**
1. Remove MouseClick handler
2. Use Selecting event
3. Keep the same selection logic

---

## Scenarios (Examples) Analysis

All scenarios in Examples/UICatalog use the MouseClick event for demonstration/logging purposes. These are:
1. Bars.cs - Commented out
2. TextAlignmentAndDirection.cs
3. EventLog.cs
4. TableEditor.cs
5. ListColumns.cs
6. ViewExperiments.cs
7. Mouse.cs
8. CharacterMap.cs
9. ContextMenus.cs
10. TreeViewFileSystem.cs

**Complexity:** SIMPLE for all
**Strategy:** Port to use Selecting event or MouseBindings for demonstration

---

## Tests Analysis

Test files using MouseClick:
1. Tests/UnitTests/Views/ShortcutTests.cs
2. Tests/UnitTests/Views/TabViewTests.cs
3. Tests/UnitTests/Views/TextFieldTests.cs
4. Tests/UnitTestsParallelizable/Application/MouseTests.cs
5. Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs
6. Tests/UnitTestsParallelizable/Views/LabelTests.cs
7. Tests/UnitTestsParallelizable/Views/TextValidateFieldTests.cs
8. Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseEventRoutingTests.cs
9. Tests/UnitTests/Application/Mouse/ApplicationMouseTests.cs
10. Tests/IntegrationTests/FluentTests/GuiTestContextMouseEventTests.cs
11. Tests/UnitTests/View/Mouse/MouseTests.cs

**Strategy:**
- Tests that validate the MouseClick event itself should be removed
- Tests that validate behavior (e.g., "clicking sets focus") should be updated to verify via Selecting or command invocation
- Ensure all behaviors are still tested after the API is removed

---

## Summary

### Complexity Breakdown:
- **Already Done:** 1 (HexView)
- **Simple (1-2 steps):** 6 (Button, Label, TabRow, TreeTableSource, CheckBoxTableSourceWrapper, FileDialog)
- **Moderate (3-5 steps):** 2 (ScrollBar, TabView)
- **Complex/No Change:** 1 (TextView - private method, not the API)
- **Scenarios:** 10 (All simple)

### Recommended Porting Order:
1. **Phase 1:** Port Scenarios (all simple, good for learning)
2. **Phase 2:** Port Simple Views (Button, Label, etc.)
3. **Phase 3:** Port Moderate Views (ScrollBar, TabView)
4. **Phase 4:** Update/Remove Tests
5. **Phase 5:** Remove the API from View.Mouse.cs

### Key Insights:
1. Most Views just forward MouseClick to a Command - very simple ports
2. HexView shows the ideal pattern - already done
3. TextView's ProcessMouseClick is NOT part of the API being removed
4. The MouseClick->Command.Select->Selecting flow already exists, we're just removing the middle MouseClick layer

---

## Next Steps
1. ✅ Create this analysis document
2. ⬜ Port Scenarios (Examples/UICatalog)
3. ⬜ Port Simple Views
4. ⬜ Port Moderate Views  
5. ⬜ Update Tests
6. ⬜ Remove API from View.Mouse.cs
