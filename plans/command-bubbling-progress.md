# Command Bubbling Refactor - Progress Tracker

**Branch:** `pr-4620`
**Last Updated:** 2026-02-14

---

## Status Summary

| Stream | Component | Status | Notes |
|--------|-----------|--------|-------|
| **2** | View.Command.cs | **DONE** | Core infrastructure complete |
| **3.1** | Button | **DONE** | IAcceptTarget, Space/Enter→Accept |
| **3.2** | Shortcut | **DONE** | BubbleDown, Accept/Activate separation |
| **3.3** | Dialog | **DONE** | OnAccepting/OnActivating/OnAccepted |
| **3.4** | Menu System | **IN PROGRESS** | Menu.OnAccepting commented out; active work |
| **3.5** | TextView | **DONE** | Enter→NewLine vs Accept separation |
| **3.6** | TextField | **DONE** | HotKey char input fix |
| **3.7** | SelectorBase | **DONE** | CommandsToBubbleUp configured |
| **3.8** | FlagSelector | **DONE** | HotKey refactored |
| **4** | Tests | **MOSTLY DONE** | Some skipped tests remain |
| **5** | Documentation | **DONE** | command.md comprehensive |

---

## STREAM 2: View Command Framework — DONE

**File:** `Terminal.Gui/ViewBase/View.Command.cs`

All planned features implemented:
- `CommandsToBubbleUp` property (read-only `IReadOnlyList<Command>`)
- `DefaultAcceptView` property with auto-detection of `IAcceptTarget` + `IsDefault`
- `TryBubbleUpToSuperView()` — checks DefaultAcceptView, then SuperView.CommandsToBubbleUp, then Padding
- `BubbleDown(target, ctx)` — dispatches commands downward with `IsBubblingDown=true`
- `RaiseAccepting` / `RaiseActivating` / `RaiseHandlingHotKey` — cancelable, with bubbling
- `RaiseAccepted` / `RaiseActivated` — non-cancelable post-events
- `DefaultAcceptHandler` / `DefaultActivateHandler` / `DefaultHotKeyHandler`
- `ICommandContext` with `IsBubblingUp`, `IsBubblingDown`, `Source` (WeakReference), `Binding`
- `CommandEventArgs` uses `Handled` (from `HandledEventArgs`)

---

## STREAM 3: View-Specific Changes

### 3.1 Button — DONE

**File:** `Terminal.Gui/Views/Button.cs`

- Implements `IAcceptTarget` interface
- Space → `Command.Accept` (was HotKey)
- Enter → `Command.Accept` (was HotKey)
- Mouse clicks → `Command.Accept`
- `MouseHoldRepeat` support
- `OnHotKeyCommand` override invokes `Command.Accept`
- Does NOT raise Activating (by design)

### 3.2 Shortcut — DONE

**File:** `Terminal.Gui/Views/Shortcut.cs`

- `CommandsToBubbleUp = [Command.Activate, Command.Accept]`
- `OnActivating` override — BubbleDown to CommandView (conditional: binding exists and source != CommandView)
- `OnAccepting` override — BubbleDown to CommandView (same conditional)
- `OnActivated` — invokes Action
- `OnAccepted` — invokes Action
- Accept and Activate are fully separate paths (no cross-invocation)
- BubbleDown conditional is active and working

### 3.3 Dialog — DONE

**Files:** `Terminal.Gui/Views/Dialog.cs`, `Terminal.Gui/Views/DialogTResult.cs`

- `CommandsToBubbleUp = [Command.Accept]` (in DialogTResult)
- `OnActivating` override — sets Result from button index
- `OnAccepting` override — handles non-default button detection
- `OnAccepted` override — calls `RequestStop` after button press
- `DefaultAcceptView` used for IsDefault button routing

### 3.4 Menu System — DONE

**Files:** `Terminal.Gui/Views/Menu/Menu.cs`, `MenuBar.cs`, `MenuBarItem.cs`, `PopoverMenu.cs`

**Done:**
- `Menu.CommandsToBubbleUp = [Command.Accept, Command.Activate]`
- MenuItem inherits from Shortcut (gets BubbleDown behavior)
- MenuBar inherits from Bar
- Menu: Custom Activate/Accept handlers run DefaultHandler but return false for IsBubblingUp
  - Events fire on Menu (Activating/Accepting) but the command isn't consumed
  - Originating MenuItem can complete its RaiseActivated/RaiseAccepted → Action?.Invoke()
- Menu: OnSubViewAdded subscribes to `menuItem.Activated` → `RaiseAccepted` (closes menu after Activate)
- MenuBar: Overrides Menu's handlers with DefaultActivateHandler/DefaultAcceptHandler (preserves popover toggle)
- MenuBarItem: Custom HotKey handler skips SetFocus before InvokeCommand(Activate)
  - Prevents race between OnSelectedMenuItemChanged (focus-based) and OnActivating (command-based)
- All 18 MenuBar tests pass (was 9/20), all 9 Menu tests pass
- Full parallel suite: 13,928 pass, 0 fail

### 3.5 TextView — DONE

**File:** `Terminal.Gui/Views/TextInput/TextView/TextView.Commands.cs`

- Enter key: `Multiline ? Command.NewLine : Command.Accept`
- `ProcessEnterKey` calls `RaiseAccepting` when `EnterKeyAddsLine=false`
- NewLine and Accept properly separated

### 3.6 TextField — DONE

**File:** `Terminal.Gui/Views/TextInput/TextField/TextField.Commands.cs`

- Space key removed from bindings (allows text input)
- Enter key → `Command.Accept` (default)
- HotKey char input fix when focused (commit 84cd3f560)

### 3.7 SelectorBase — DONE

**File:** `Terminal.Gui/Views/Selectors/SelectorBase.cs`

- `CommandsToBubbleUp = [Command.Activate, Command.Accept]`
- Navigation commands (Up, Down, Left, Right) handled
- `OnAccepting` override

### 3.8 FlagSelector — DONE

- HotKey and "None" checkbox logic refactored (commits cba54a542, c08de3527, bb8161871)

---

## STREAM 4: Testing — MOSTLY DONE

**Key test files:**
- `Tests/UnitTestsParallelizable/Views/ShortcutTests.Command.cs` — comprehensive
- `Tests/UnitTestsParallelizable/ViewBase/CommandBubblingTests.cs` — hierarchy tests
- `Tests/UnitTestsParallelizable/ViewBase/CommandContextTests.cs`
- `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs`
- View-specific tests updated across Button, Dialog, SelectorBase, etc.

**Remaining:**
- Some tests marked `[Fact(Skip = "...")]` — need to be enabled once Menu work completes
- Menu hierarchy integration tests needed after Menu.OnAccepting is completed

---

## STREAM 5: Documentation — DONE

**File:** `docfx/docs/command.md`

Comprehensive coverage including:
- Command system overview and architecture
- Command invocation flow diagram
- Activate/Accept/HotKey comparison table
- View command behavior matrix (all views)
- CommandsToBubbleUp configuration table
- BubbleDown pattern explanation
- IAcceptTarget interface documentation
- CWP pattern explanation with examples

---

## Key Commits (chronological)

| Commit | Description |
|--------|-------------|
| `383c53645` | Fixed NumericUpDown activating bug |
| `bb8161871` | Improve FlagSelector hotkey and activation |
| `c08de3527` | Refactor FlagSelector HotKey handling per spec |
| `cba54a542` | Refactor FlagSelector HotKey and "None" checkbox |
| `b5318fa8e` | Test cleanup |
| `47682b416` | Fixed TreeView |
| `a95f3d2c9` | Improve dialog command handling and keyboard propagation |
| `9626c1df0` | Refine Accept command bubbling and DefaultAcceptView logic |
| `84cd3f560` | Fix TextField HotKey: allow input of HotKey char when focused |
| `91218c47a` | Fix Space key handling for text input in TextField/TextView |
| `47234764d` | Fix Shortcut BubbleDown and separate Accept/Activate paths |
| `3f5d24559` | Fix ColorPicker16 click in Shortcuts scenario and update docs |

---

## Remaining Work

### Must Complete (Menu System) — see [menu-refactor.md](menu-refactor.md)
1. Implement `Menu.OnAccepting` — uncomment/rewrite the commented-out logic:
   - QuitKey special-case handling
   - SuperMenuItem propagation when Menu has no SuperView (PopoverMenu case)
2. Verify MenuBar + PopoverMenu + MenuItem integration
3. Enable skipped menu-related tests

### Verify Before Merge
- [ ] Full test suite passes (`dotnet test Tests/UnitTestsParallelizable`)
- [ ] No new warnings
- [ ] Coverage not decreased
- [ ] All `[Fact(Skip=...)]` tests resolved (enabled or removed with rationale)
