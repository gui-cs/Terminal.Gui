# Cleanup: Remove Unnecessary `AddCommand` Overrides for Activate, Accept, and HotKey

## Problem Statement

`View.SetupCommands()` registers default handlers for `Command.Activate`, `Command.Accept`, and `Command.HotKey` that follow the CWP (Cancellable Workflow Pattern). Views should customize behavior by overriding virtual methods (`OnActivating`, `OnActivated`, `OnAccepting`, `OnAccepted`, `OnHandlingHotKey`, `OnHotKeyCommand`) rather than replacing the entire command handler via `AddCommand`.

## Scope

Refactor 6 Views. **Excluded**: Menu/MenuBar/MenuBarItem (complex bubbling — separate effort), Shortcut (legitimately needs custom flow), SelectorBase (already correct).

## Default Handler Flows (Reference)

```
DefaultActivateHandler:  RaiseActivating(OnActivating→Activating) → SetFocus → RaiseActivated(OnActivated→Activated) → return true
DefaultAcceptHandler:    RaiseAccepting(OnAccepting→Accepting) → BubbleDown to DefaultAcceptView → RaiseAccepted(OnAccepted→Accepted) → return bool
DefaultHotKeyHandler:    RaiseHandlingHotKey(OnHandlingHotKey→HandlingHotKey) → SetFocus → RaiseHotKeyCommand(OnHotKeyCommand→HotKeyCommand) → InvokeCommand(Activate) → return true
```

## Work Plan

### 1. Label — `OnHandlingHotKey` override replaces `AddCommand(HotKey)`

**File:** `Terminal.Gui/Views/Label.cs`

Remove `AddCommand(Command.HotKey, InvokeHotKeyOnNextPeer!)` and the `InvokeHotKeyOnNextPeer` method. Override `OnHandlingHotKey`: when `!CanFocus && HotKey.IsValid`, forward `Command.HotKey` to the next peer in SuperView's SubViews and cancel default flow. When `CanFocus`, return false to let `DefaultHotKeyHandler` proceed.

- [ ] Implement
- [ ] Tests pass: `Tests/UnitTests/Views/LabelTests.cs`, `Tests/UnitTestsParallelizable/Views/LabelTests.cs`

### 2. HexView — `OnActivating` override replaces `AddCommand(Activate)`

**File:** `Terminal.Gui/Views/HexView.cs`

Remove `AddCommand(Command.Activate, HandleMouseClick)`. Convert `HandleMouseClick` to an `OnActivating` override. For mouse bindings: do hex position calculation, call `SetFocus()`, set `args.Handled = true`. For non-mouse: call `base.OnActivating(args)`. Remove the `RaiseActivating()` call (it would be recursive inside `OnActivating`).

- [ ] Implement
- [ ] Tests pass: `Tests/UnitTestsParallelizable/Views/HexViewTests.cs`

### 3. ComboBox — `OnAccepting` override replaces `AddCommand(Accept)`

**File:** `Terminal.Gui/Views/ComboBox.cs`

Remove `AddCommand(Command.Accept, ...)`. Override `OnAccepting`: if source is `_search`, return false (skip). Otherwise, do `HasItems`/`SelectText` checks and set `args.Handled` as appropriate. Remove the `RaiseAccepting()` call from `ActivateSelected` (recursive inside `OnAccepting`).

- [ ] Implement
- [ ] Tests pass: `Tests/UnitTests/Views/ComboBoxTests.cs`, `Tests/UnitTestsParallelizable/Views/ComboBoxCommandTests.cs`

### 4. TreeView — `OnActivated`+`OnAccepted` overrides replace both `AddCommand` calls

**File:** `Terminal.Gui/Views/TreeView/TreeView.cs`

Remove `AddCommand(Command.Activate, ...)` and `AddCommand(Command.Accept, ...)`. Override `OnActivated` and `OnAccepted` to fire `ObjectActivated` when `SelectedObject` is non-null. Keep `ActivateSelectedObjectIfAny` as a public API (mark obsolete or simplify) since it's part of the public surface.

- [ ] Implement
- [ ] Check external callers of `ActivateSelectedObjectIfAny`
- [ ] Tests pass: `Tests/UnitTests/Views/TreeViewTests.cs`, `Tests/UnitTestsParallelizable/Views/TreeViewTests.cs`

### 5. TableView — `OnAccepted`+`OnActivating` overrides replace both `AddCommand` calls

**File:** `Terminal.Gui/Views/TableView/TableView.cs`

Remove `AddCommand(Command.Accept, ...)` and `AddCommand(Command.Activate, ...)`. Override `OnAccepted` to call `OnCellActivated`. Override `OnActivating` to handle `ToggleCurrentCellSelection` — if toggle succeeds, set `args.Handled = true` and return true.

- [ ] Implement
- [ ] Tests pass: `Tests/UnitTests/Views/TableViewTests.cs`, `Tests/UnitTestsParallelizable/Views/TableViewTests.cs`

### 6. LinearRange — `OnActivated`+`OnAccepting` overrides replace both `AddCommand` calls

**File:** `Terminal.Gui/Views/LinearRange/LinearRange.cs`

Remove `AddCommand(Command.Activate, ...)` and `AddCommand(Command.Accept, ...)` from `SetCommands()`. Override `OnActivated` to call `SetFocusedOption()`. Override `OnAccepting` to call `SetFocusedOption()` and return false (let CWP flow continue).

- [ ] Implement
- [ ] Tests pass: `Tests/UnitTestsParallelizable/Views/LinearRangeTests.cs`

### 7. Verification

- [ ] `dotnet build --no-restore` succeeds
- [ ] `dotnet test Tests/UnitTestsParallelizable --no-build` passes
- [ ] `dotnet test Tests/UnitTests --no-build` passes

## Out of Scope (Future Work)

- **Menu/MenuBar/MenuBarItem** — Complex interdependent bubbling; separate effort
- **Shortcut** — Legitimately needs `AddCommand` for deferred activation bubbling
- **SelectorBase** — Already uses virtual overrides correctly
