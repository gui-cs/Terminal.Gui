# Command Bubbling Status

## Design

The ancestor decides whether to **consume** (stop originator) or **notify** (let originator continue) via its handler's return value:

### Core (`View.Command.cs`)

1. `TryBubbleUp` returns the ancestor's handler result (unchanged from original)
2. `DefaultActivateHandler` returns `false` when `IsBubblingUp` — **notification** is the default. The Activating event fires on the ancestor, but Activated and SetFocus are skipped. The originator continues its own processing.
3. `DefaultAcceptHandler` has a `CommandWillBubbleToAncestor` guard that skips the `DefaultAcceptView` redirect when Accept will also bubble via `CommandsToBubbleUp` (prevents double-path reaching the same ancestor).
4. `CommandWillBubbleToAncestor(Command)` — helper that mirrors `TryBubbleUp`'s checks to predict whether a command will bubble.

### Views that consume (override the default notification behavior)

- **OptionSelector**: `OnActivating` consumes when `IsBubblingUp` (applies value change via `ApplyActivation`, calls `RaiseActivated`, returns `true`). `OnActivated` handles direct (non-bubble) invocations.
- **FlagSelector**: `OnActivating` toggles the CheckBox when `IsBubblingUp` but returns `false` (notification). The bubble continues to Shortcut which consumes.
- **Shortcut**: `HandleActivate` returns `false` when `IsBubblingUp` (deferred path). `CommandView_Activated` now also handles the case where `IsBubblingUp == true` in the event context (for views like FlagSelector/OptionSelector that call `RaiseActivated` from `OnActivating`).

### Views that use default notification (zero boilerplate)

- **Bar**: `CommandsToBubbleUp = [Command.Accept, Command.Activate]` — no custom handlers needed.
- **Menu**: `CommandsToBubbleUp = [Command.Accept, Command.Activate]` — custom handlers removed.
- **MenuBar**: Custom handler overrides removed.

## Remaining Work

### Phase 5: Bridge Activate Across PopoverMenu

PopoverMenu breaks the SuperView chain between Menu and MenuBar. Accept already bridges via event subscriptions. Activate needs the same bridging:

1. **PopoverMenu**: Subscribe to `menu.Activated` → `RaiseActivated`
2. **MenuBarItem**: Uncomment `RaiseActivated` in `OnPopoverMenuOnActivated`
3. **MenuBar**: Subscribe to `mbi.Activated` → `RaiseActivated` in `OnSubViewAdded`

### Phase 6: Unskip CommandBubblingTests

Evaluate skipped tests in `CommandBubblingTests.cs` after Phase 5.

## Files Changed

### Core
- `Terminal.Gui/ViewBase/View.Command.cs` — DefaultActivateHandler IsBubblingUp guard, DefaultAcceptHandler CommandWillBubbleToAncestor, TryBubbleUp docs
- `Terminal.Gui/Views/Bar.cs` — CommandsToBubbleUp enabled, custom handlers removed
- `Terminal.Gui/Views/Menu/Menu.cs` — Custom handlers removed
- `Terminal.Gui/Views/Menu/MenuBar.cs` — Custom handler overrides removed
- `Terminal.Gui/Views/Shortcut.cs` — CommandView_Activated handles IsBubblingUp context
- `Terminal.Gui/Views/Selectors/OptionSelector.cs` — OnActivating + ApplyActivation pattern
- `Terminal.Gui/Views/Selectors/FlagSelector.cs` — OnActivating toggles for bubble, returns false

### Tests
- `Tests/UnitTestsParallelizable/Views/BarTests.cs` — BUGBUG fixes, 10 new tests
- `Tests/UnitTestsParallelizable/Views/PopoverMenuTests.cs` — New file (tests skipped pending Phase 5)
- `Tests/UnitTestsParallelizable/Views/ShortcutTests.Command.cs` — FlagSelector test updated
