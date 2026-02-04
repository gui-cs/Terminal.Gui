# Plan: Unit Tests for View Command Behaviors

## Overview

This plan outlines adding/improving unit tests for each View subclass to ensure the current `Activate`, `Accept`, and `HotKey` command behaviors are tested and documented. These tests serve as behavioral specifications that will help identify regressions when command behaviors are modified.

**Reference Documentation:** See the **View Command Behaviors** table in `docfx/docs/command.md` for the authoritative documentation of expected behaviors.

## Test Requirements

### General Guidelines

1. **Location:** All new tests go in `Tests/UnitTestsParallelizable/Views/`
2. **Naming Convention:** `{ViewName}_Command_{CommandType}_{Scenario}`
3. **Required Comment:** Each test MUST include:
   ```csharp
   // Behavior documented in docfx/docs/command.md - View Command Behaviors table
   // This test verifies current behavior which may change per issue #4473
   ```
4. **No Application.Init:** Tests should not use `Application.Init` or static dependencies
5. **AI Attribution:** Include `// Claude - Opus 4.5` comment

### Test Structure Template

```csharp
// Claude - Opus 4.5
// Behavior documented in docfx/docs/command.md - View Command Behaviors table
// This test verifies current behavior which may change per issue #4473
[Fact]
public void ViewName_Command_Activate_ExpectedBehavior ()
{
    // Arrange
    ViewType view = new () { /* setup */ };
    bool eventFired = false;
    view.Activating += (_, _) => eventFired = true;

    // Act
    bool? result = view.InvokeCommand (Command.Activate);

    // Assert
    Assert.True (eventFired);
    Assert.True (result);

    view.Dispose ();
}
```

---

## Views to Test

### 1. Button

**File:** `ButtonTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `Button_Command_Activate_RaisesActivatingAndAccepting` | Activate | RaiseActivating → SetFocus → RaiseAccepting |
| `Button_Command_Accept_SameAsActivate` | Accept | Same behavior as Activate via HotKey handler |
| `Button_Command_HotKey_RaisesActivatingAndAccepting` | HotKey | SetFocus + RaiseActivating + RaiseAccepting |
| `Button_Space_InvokesActivate` | KeyBinding | Space key triggers Activate command |
| `Button_Enter_InvokesAccept` | KeyBinding | Enter key triggers Accept command |

```csharp
// Claude - Opus 4.5
// Behavior documented in docfx/docs/command.md - View Command Behaviors table
// This test verifies current behavior which may change per issue #4473
[Fact]
public void Button_Command_Activate_RaisesActivatingAndAccepting ()
{
    Button button = new () { Text = "Test" };
    bool activatingFired = false;
    bool acceptingFired = false;

    button.Activating += (_, _) => activatingFired = true;
    button.Accepting += (_, _) => acceptingFired = true;

    bool? result = button.InvokeCommand (Command.Activate);

    Assert.True (activatingFired);
    Assert.True (acceptingFired);
    Assert.True (result);

    button.Dispose ();
}
```

---

### 2. CheckBox

**File:** `CheckBoxTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `CheckBox_Command_Activate_TogglesState` | Activate | Toggles CheckState, raises Activating |
| `CheckBox_Command_Accept_ConfirmsState` | Accept | Confirms state (no toggle), raises Accepting |
| `CheckBox_Command_HotKey_InvokesActivate` | HotKey | Invokes Activate (toggles state + SetFocus) |
| `CheckBox_Space_TogglesState` | KeyBinding | Space triggers state toggle |
| `CheckBox_Enter_ConfirmsWithoutToggle` | KeyBinding | Enter confirms without toggling |

```csharp
// Claude - Opus 4.5
// Behavior documented in docfx/docs/command.md - View Command Behaviors table
// This test verifies current behavior which may change per issue #4473
[Fact]
public void CheckBox_Command_Activate_TogglesState ()
{
    CheckBox checkBox = new () { Text = "Test" };
    CheckState initialState = checkBox.CheckedState;
    bool activatingFired = false;

    checkBox.Activating += (_, _) => activatingFired = true;

    bool? result = checkBox.InvokeCommand (Command.Activate);

    Assert.True (activatingFired);
    Assert.NotEqual (initialState, checkBox.CheckedState);

    checkBox.Dispose ();
}

// Claude - Opus 4.5
// Behavior documented in docfx/docs/command.md - View Command Behaviors table
// This test verifies current behavior which may change per issue #4473
[Fact]
public void CheckBox_Command_Accept_ConfirmsStateWithoutToggle ()
{
    CheckBox checkBox = new () { Text = "Test", CheckedState = CheckState.Checked };
    CheckState initialState = checkBox.CheckedState;
    bool acceptingFired = false;

    checkBox.Accepting += (_, _) => acceptingFired = true;

    bool? result = checkBox.InvokeCommand (Command.Accept);

    Assert.True (acceptingFired);
    Assert.Equal (initialState, checkBox.CheckedState); // State unchanged

    checkBox.Dispose ();
}
```

---

### 3. ComboBox

**File:** `ComboBoxTests.cs` (may need creation in UnitTestsParallelizable)

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `ComboBox_Command_Activate_TogglesDropdown` | Activate | Opens/closes dropdown |
| `ComboBox_Command_Accept_SelectsItem` | Accept | Selects highlighted item |
| `ComboBox_Command_HotKey_OpensDropdown` | HotKey | Opens dropdown + SetFocus |

---

### 4. ListView

**File:** `ListViewTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `ListView_Command_Activate_ChangesSelection` | Activate | Changes selection via arrow keys |
| `ListView_Command_Accept_FiresRowActivated` | Accept | Fires RowActivated event |
| `ListView_Command_HotKey_SetsFocus` | HotKey | SetFocus |

```csharp
// Claude - Opus 4.5
// Behavior documented in docfx/docs/command.md - View Command Behaviors table
// This test verifies current behavior which may change per issue #4473
[Fact]
public void ListView_Command_Accept_FiresRowActivated ()
{
    ListView listView = new () { Source = new ListWrapper<string> (["Item1", "Item2"]) };
    bool rowActivatedFired = false;

    listView.RowActivated += (_, _) => rowActivatedFired = true;

    bool? result = listView.InvokeCommand (Command.Accept);

    Assert.True (rowActivatedFired);

    listView.Dispose ();
}
```

---

### 5. TableView

**File:** `TableViewTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `TableView_Command_Activate_TogglesSelection` | Activate | Space toggles cell selection |
| `TableView_Command_Accept_FiresCellActivated` | Accept | Enter fires CellActivated event |
| `TableView_Space_TogglesSelection` | KeyBinding | Space triggers cell toggle |
| `TableView_Enter_FiresCellActivated` | KeyBinding | Enter triggers CellActivated |

---

### 6. TreeView

**File:** `TreeViewTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `TreeView_Command_Activate_SameAsAccept` | Activate | Same as Accept (special case) |
| `TreeView_Command_Accept_ActivatesNode` | Accept | Activates selected node |
| `TreeView_Command_HotKey_SetsFocus` | HotKey | SetFocus |

```csharp
// Claude - Opus 4.5
// Behavior documented in docfx/docs/command.md - View Command Behaviors table
// This test verifies current behavior which may change per issue #4473
// NOTE: TreeView is a special case - both Activate and Accept invoke the same handler
[Fact]
public void TreeView_Command_Activate_SameAsAccept ()
{
    TreeView<string> treeView = new ();
    treeView.AddObject ("Root");
    treeView.SelectedObject = "Root";

    int activateCallCount = 0;
    treeView.Accepting += (_, _) => activateCallCount++;

    treeView.InvokeCommand (Command.Activate);
    int afterActivate = activateCallCount;

    treeView.InvokeCommand (Command.Accept);
    int afterAccept = activateCallCount;

    // Both commands should trigger the same handler
    Assert.Equal (1, afterActivate);
    Assert.Equal (2, afterAccept);

    treeView.Dispose ();
}
```

---

### 7. TextField

**File:** `TextFieldTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `TextField_Command_Activate_PositionsCursor` | Activate | Click positions cursor |
| `TextField_Command_Accept_RaisesAccepting` | Accept | Raises Accepting (submit) |
| `TextField_Command_HotKey_SetsFocus` | HotKey | SetFocus |
| `TextField_Enter_RaisesAccepting` | KeyBinding | Enter raises Accepting |

---

### 8. TextView

**File:** `TextViewTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `TextView_Command_Activate_PositionsCursor` | Activate | Click positions cursor |
| `TextView_Command_Accept_NotTypical` | Accept | Not typical (multiline input) |
| `TextView_Command_HotKey_SetsFocus` | HotKey | SetFocus |

---

### 9. OptionSelector

**File:** `OptionSelectorTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `OptionSelector_Command_Activate_ForwardsToFocusedCheckBox` | Activate | Forwards to focused CheckBox's Activate |
| `OptionSelector_Command_Accept_RaisesAccepting` | Accept | Raises Accepting |
| `OptionSelector_Command_HotKey_ForwardsToFocusedItem` | HotKey | Forwards to focused item's Activate |

---

### 10. FlagSelector

**File:** `FlagSelectorTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `FlagSelector_Command_Activate_ForwardsToFocusedCheckBox` | Activate | Forwards to focused CheckBox's Activate |
| `FlagSelector_Command_Accept_RaisesAccepting` | Accept | Raises Accepting |
| `FlagSelector_Command_HotKey_ForwardsToFocusedItem` | HotKey | Forwards to focused item's Activate |

---

### 11. Menu

**File:** `MenuTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `Menu_Command_Activate_FocusesMenuItem` | Activate | Focuses MenuItem |
| `Menu_Command_Accept_ExecutesOrOpensSubmenu` | Accept | Executes command or opens submenu |
| `Menu_Command_HotKey_ActivatesMatchingItem` | HotKey | Activates item with matching hotkey |

---

### 12. MenuBar

**File:** Create `MenuBarCommandTests.cs` in UnitTestsParallelizable

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `MenuBar_Command_Activate_FocusesMenuBarItem` | Activate | Focuses MenuBarItem |
| `MenuBar_Command_Accept_ShowsPopoverOrExecutes` | Accept | Shows PopoverMenu or executes command |
| `MenuBar_Command_HotKey_ActivatesMatchingItem` | HotKey | Activates item with matching hotkey |

---

### 13. MenuItem

**File:** `MenuBarItemTests.cs` (existing) or new `MenuItemTests.cs`

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `MenuItem_Command_Activate_SetsFocus` | Activate | Sets focus, raises SelectedMenuItemChanged |
| `MenuItem_Command_Accept_ExecutesAction` | Accept | Executes Action or opens submenu |
| `MenuItem_Command_HotKey_InvokesAccept` | HotKey | Invokes Accept |

---

### 14. Shortcut

**File:** `ShortcutTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `Shortcut_Command_Activate_DispatchesCommand` | Activate | DispatchCommand flow |
| `Shortcut_Command_Accept_SameAsActivate` | Accept | Same as Activate |
| `Shortcut_Command_HotKey_SameAsActivate` | HotKey | Same as Activate |
| `Shortcut_AllCommands_InvokeSameHandler` | All | Verifies unified handling |

```csharp
// Claude - Opus 4.5
// Behavior documented in docfx/docs/command.md - View Command Behaviors table
// This test verifies current behavior which may change per issue #4473
// NOTE: Shortcut has unified handling - all three commands invoke DispatchCommand
[Fact]
public void Shortcut_AllCommands_InvokeSameDispatchCommand ()
{
    Shortcut shortcut = new () { Title = "Test", Key = Key.T.WithCtrl };
    int dispatchCount = 0;

    shortcut.Accepting += (_, _) => dispatchCount++;

    shortcut.InvokeCommand (Command.Activate);
    shortcut.InvokeCommand (Command.Accept);
    shortcut.InvokeCommand (Command.HotKey);

    // All three should invoke the same DispatchCommand method
    Assert.Equal (3, dispatchCount);

    shortcut.Dispose ();
}
```

---

### 15. Dialog

**File:** `DialogTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `Dialog_Command_Accept_SetsResultAndStops` | Accept | Button press sets Result, calls RequestStop |
| `Dialog_ContainedButton_Accept_PropagatesUp` | Accept | Accept propagates from button to dialog |

---

### 16. Wizard

**File:** `WizardTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `Wizard_NextButton_Accept_AdvancesStep` | Accept | Next button advances step |
| `Wizard_FinishButton_Accept_Completes` | Accept | Finish button completes wizard |

---

### 17. TabView

**File:** Create `TabViewCommandTests.cs` in UnitTestsParallelizable

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `TabView_Command_Activate_NotExplicitlyHandled` | Activate | Not explicitly handled |
| `TabView_Command_Accept_NotExplicitlyHandled` | Accept | Not explicitly handled |
| `TabView_Command_HotKey_SetsFocus` | HotKey | SetFocus |
| `TabView_LeftRight_NavigatesTabs` | KeyBinding | Left/Right commands for tab navigation |

---

### 18. ScrollBar

**File:** `ScrollBarTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `ScrollBar_Click_JumpsPosition` | Mouse | Click on track jumps scroll position |
| `ScrollBar_Command_Accept_NotTypical` | Accept | Not typical |

---

### 19. HexView

**File:** `HexViewTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `HexView_Click_PositionsCursor` | Activate | Click positions cursor |
| `HexView_DoubleClick_TogglesSide` | Mouse | Double-click toggles hex/text side |

---

### 20. NumericUpDown

**File:** `NumericUpDownTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `NumericUpDown_UpArrow_IncrementsValue` | Command.Up | Up arrow increments value |
| `NumericUpDown_DownArrow_DecrementsValue` | Command.Down | Down arrow decrements value |
| `NumericUpDown_ButtonAccept_ChangesValue` | Accept | Internal button Accepting changes value |

---

### 21. DatePicker

**File:** `DatePickerTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `DatePicker_CalendarCellSelection_ChangesDate` | Activate | Calendar cell selection changes date |
| `DatePicker_InternalInteractions_Work` | Various | Via internal button/field interactions |

---

### 22. ColorPicker

**File:** `ColorPickerTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `ColorPicker_BarValueChange_UpdatesColor` | Activate | Color bar value changes update color |
| `ColorPicker_DoubleClick_RaisesAccepting` | Accept | Double-click raises Accepting |

---

### 23. ProgressBar

**File:** Create `ProgressBarTests.cs` in UnitTestsParallelizable (if not exists)

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `ProgressBar_CannotFocus` | N/A | Verify CanFocus = false |
| `ProgressBar_Commands_NotHandled` | All | Commands are not handled |

```csharp
// Claude - Opus 4.5
// Behavior documented in docfx/docs/command.md - View Command Behaviors table
// This test verifies current behavior which may change per issue #4473
// NOTE: ProgressBar has CanFocus = false and does not handle commands
[Fact]
public void ProgressBar_CannotFocus_DoesNotHandleCommands ()
{
    ProgressBar progressBar = new ();

    Assert.False (progressBar.CanFocus);

    // Commands should not be handled
    bool? activateResult = progressBar.InvokeCommand (Command.Activate);
    bool? acceptResult = progressBar.InvokeCommand (Command.Accept);

    // Results should indicate not handled or no handler
    Assert.NotEqual (true, activateResult);
    Assert.NotEqual (true, acceptResult);

    progressBar.Dispose ();
}
```

---

### 24. SpinnerView

**File:** Create `SpinnerViewTests.cs` in UnitTestsParallelizable

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `SpinnerView_DisplayOnly_DoesNotHandleCommands` | All | Display only, no command handling |

---

### 25. Bar

**File:** `BarTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `Bar_Commands_DelegatedToShortcuts` | All | Commands handled by contained Shortcuts |

---

### 26. Label

**File:** `LabelTests.cs` (existing) - Add command behavior tests

| Test Name | Command | Expected Behavior |
|-----------|---------|-------------------|
| `Label_CannotFocus_ByDefault` | N/A | Usually CanFocus = false |
| `Label_HotKey_ForwardsToNextFocusable` | HotKey | Forwards HotKey to next focusable view |

---

## Implementation Priority

### Phase 1: Core Interactive Views (High Priority)
1. Button
2. CheckBox
3. TextField
4. ListView
5. TableView

### Phase 2: Composite/Menu Views (Medium Priority)
6. Menu/MenuItem/MenuBar
7. Shortcut
8. Dialog
9. ComboBox

### Phase 3: Selector Views (Medium Priority)
10. OptionSelector
11. FlagSelector
12. TreeView

### Phase 4: Specialized Views (Lower Priority)
13. Wizard
14. TabView
15. HexView
16. NumericUpDown
17. DatePicker
18. ColorPicker
19. ScrollBar

### Phase 5: Display-Only Views (Lowest Priority)
20. ProgressBar
21. SpinnerView
22. Label
23. Bar

---

## Test Execution

Run all command behavior tests:
```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~Command"
```

Run tests for a specific view:
```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~Button_Command"
```

---

## Notes

1. **Behavioral Specification:** These tests document current behavior. When command behaviors change (per issue #4473), update both the tests AND the table in `command.md`.

2. **Regression Detection:** If a test fails after a change, verify whether:
   - The change was intentional (update test and docs)
   - The change was unintentional (revert or fix)

3. **Coverage Target:** Aim for 100% coverage of documented command behaviors in the View Command Behaviors table.

4. **Avoiding Static Dependencies:** Tests must not use `Application.Init`. Use direct `InvokeCommand` calls and event subscriptions.
