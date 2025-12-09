### Level 1: Basic View Command Flow

This diagram shows the fundamental command invocation flow within a single view, demonstrating the Cancellable Work Pattern with pre-events (e.g., `Activating`, `Accepting`) and the command handler execution.

```mermaid
flowchart TD
    input["User input (key/mouse)"] --> invoke["View.InvokeCommand(command)"]
    invoke --> |Command.Activate| act_pre["OnActivating + Activating handlers"]
    invoke --> |Command.Accept| acc_pre["OnAccepting + Accepting handlers"]

    act_pre --> |canceled| act_stop["Stop"]
    act_pre --> |not canceled| act_handler["Execute command handler"]
    act_handler --> act_done["Complete (returns bool?)"]

    acc_pre --> |canceled| acc_stop["Stop"]
    acc_pre --> |not canceled| acc_handler["Execute command handler"]
    acc_handler --> acc_prop["Propagate to default button/superview if unhandled"]
    acc_prop --> acc_done["Complete (returns bool?)"]
```

**Key Points:**
- Commands follow the Cancellable Work Pattern: pre-event → virtual method → event → handler
- `OnActivating`/`OnAccepting` or event handlers can cancel via `args.Cancel = true`
- Command handlers return `bool?`: `null` (no handler), `false` (executed but unhandled), `true` (handled/canceled)
- `Command.Activate` is handled locally (no propagation)
- `Command.Accept` may propagate (see Level 2)

### Level 2: Accept Propagation with Button.IsDefault

This diagram shows how `Command.Accept` propagates through the view hierarchy, including the special case where a default button intercepts the command even when invoked from another view.

```mermaid
flowchart TD
    input2["User input (Enter)"] --> tf_accept["TextField.InvokeCommand(Accept)"]
    tf_accept --> tf_pre["TextField OnAccepting + Accepting"]
    tf_pre --> |canceled| tf_stop["Stop"]
    tf_pre --> |not canceled| tf_default_check["Find sibling IsDefault button"]

    tf_default_check --> |found| call_default["Invoke default Button.Accept"]
    call_default --> btn_pre["Button OnAccepting + Accepting"]
    btn_pre --> btn_done["Handled → return true"]
    btn_done --> tf_result1["Handled by default button"]

    tf_default_check --> |not found/returned null| call_super["Propagate to SuperView (Dialog)"]
    call_super --> dlg_pre["Dialog OnAccepting + Accepting"]
    dlg_pre --> dlg_done["Handled/propagated result"]
```

**Key Points:**
- `Command.Accept` checks for a sibling `Button` with `IsDefault = true` in the `SuperView`
- If found and not the source view, the default button handles the command first
- If unhandled or no default button, command propagates to `SuperView`
- `SuperView` (e.g., `Dialog`) can handle accept to close or trigger actions
- This enables Enter key to activate default buttons from any focused view

### Level 3: Complete Flow with Shortcut, MenuBar, and Menu

This diagram illustrates the complete command flow in a complex hierarchical scenario involving `Shortcut`, `MenuBar`, `Menu`, and `MenuItem`, showing how commands route through multiple views and how `Accepted` events propagate back up the hierarchy.

```mermaid
flowchart TD
    sc_header["=== Scenario 1: Shortcut Activation (Alt+F) ==="]
    sc_header --> sc_input["Alt+F pressed"]
    sc_input --> sc_find["Shortcut finds MenuBarItem"]
    sc_find --> sc_hotkey["MenuBarItem.InvokeCommand(HotKey)"]
    sc_hotkey --> sc_pre["OnHandlingHotKey + HandlingHotKey"]
    sc_pre --> sc_focus["MenuBarItem sets focus"]
    sc_focus --> sc_show["MenuBar shows popover for MenuBarItem"]
    
    sc_show --> nav_header["=== Scenario 2: Menu Navigation (Arrow Keys) ==="]
    nav_header --> nav_input["Arrow keys pressed"]
    nav_input --> nav_activate["MenuItem.InvokeCommand(Activate)"]
    nav_activate --> nav_pre["MenuItem OnActivating + Activating"]
    nav_pre --> |canceled| nav_stop["Stop"]
    nav_pre --> |not canceled| nav_focus["MenuItem sets focus"]
    nav_focus --> nav_changed["Menu.SelectedMenuItemChanged raised"]
    nav_changed --> nav_bar["MenuBar.OnSelectedMenuItemChanged"]
    nav_bar --> nav_done["Update popover visibility if needed"]
    
    nav_done --> acc_header["=== Scenario 3: Accept Menu Item (Enter) ==="]
    acc_header --> acc_input["Enter pressed on MenuItem"]
    acc_input --> acc_pre["MenuItem OnAccepting + Accepting"]
    acc_pre --> |canceled| acc_stop["Stop"]
    acc_pre --> |has action| acc_exec["Execute menu item action"]
    acc_exec --> acc_accepted["MenuItem.RaiseAccepted"]
    acc_accepted --> acc_menu["Menu.OnAccepted propagates"]
    acc_menu --> acc_bar["MenuBar.OnAccepted"]
    acc_bar --> acc_close["MenuBar hides popover, deactivates"]
    
    acc_pre --> |has submenu| acc_sub["Propagate to parent Menu.Accept"]
    acc_sub --> acc_popover["Show submenu popover"]
    acc_popover --> acc_submenu_done["Submenu displayed"]
```

**Key Points:**
- **Scenario 1 (HotKey)**: Shortcut activates menu bar item via `Command.HotKey`, which sets focus and triggers MenuBar to show the popover
- **Scenario 2 (Activate)**: Arrow keys navigate menu items via `Command.Activate`, which is handled locally but raises `SelectedMenuItemChanged` for MenuBar coordination
- **Scenario 3 (Accept)**: Enter key executes menu items via `Command.Accept`, followed by `Accepted` event propagating up (MenuItem → Menu → MenuBar) to close menus
- `Command.Activate` doesn't propagate but uses view-specific event (`SelectedMenuItemChanged`) for hierarchical coordination
- `Accepted` is a post-event (not part of Cancellable Work Pattern pre-event phase) that signals action completion
- MenuBar uses `SelectedMenuItemChanged` to manage popover visibility, demonstrating current workaround for lack of generic `Activate` propagation
