# Command Propagation Analysis and Design

> **Status**: Analysis Document (Step 1 of Issue Resolution)
>
> **Issue**: [Menu etc. with Selectors are broken - Formalize/Fix Command propagation](https://github.com/gui-cs/Terminal.Gui/issues/XXXXX)
>
> **Created**: 2026-01-09
>
> **Author**: GitHub Copilot

## Table of Contents

- [Executive Summary](#executive-summary)
- [Background](#background)
- [Current State Analysis](#current-state-analysis)
- [Problems Identified](#problems-identified)
- [Shortcut + CommandView Scenario Analysis](#shortcut--commandview-scenario-analysis)
- [Design Requirements](#design-requirements)
- [Proposed Solution](#proposed-solution)
- [Implementation Impact](#implementation-impact)
- [References](#references)

---

## Executive Summary

Terminal.Gui currently implements a **command propagation** mechanism for `Command.Accept` that allows events to bubble up the View hierarchy until handled. This mechanism is hard-coded in `View.RaiseAccepting()` and includes special-case logic for:

1. Invoking `Command.Accept` on peer Views with `IsDefault == true` (e.g., default `Button`)
2. Bubbling the command up to the `SuperView` if not handled

However, this approach has **three critical problems**:

1. **One-off Implementation**: The propagation logic is specific to `Command.Accept` only
2. **Incomplete Coverage**: `Command.Activate` does NOT propagate, breaking hierarchical components like `MenuBar` that need to respond to subview activations
3. **Tight Coupling**: Views like `PopoverMenu` resort to view-specific events (`SelectedMenuItemChanged`) that couple subviews to superview implementation details

This document analyzes the current state, identifies problems, establishes design requirements, and proposes a solution that:

- Generalizes propagation to support multiple commands
- Enables opt-in propagation for superviews
- Preserves the decoupling principle of the Cancellable Work Pattern (CWP)

---

## Background

### The Cancellable Work Pattern (CWP)

Terminal.Gui implements the **Cancellable Work Pattern** throughout the framework. CWP provides a structured workflow where:

1. **Virtual Methods** enable subclass customization
2. **Events** enable external subscribers to participate
3. **Cancellation** enables workflow interruption via `Handled` or `Cancel` flags

For commands, the pattern looks like:

```csharp
protected bool? RaiseAccepting(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };
    
    // 1. Virtual method (subclass priority)
    if (OnAccepting(args) || args.Handled)
    {
        return true;
    }
    
    // 2. Event (external subscribers)
    Accepting?.Invoke(this, args);
    
    // 3. Check handled flag
    return Accepting is null ? null : args.Handled;
}
```

### Current Command System

The `Command` enum defines standard actions that Views can perform:

- **`Command.Accept`**: Confirms actions (e.g., button press, dialog submission, menu command execution)
- **`Command.Activate`**: Changes state or prepares for interaction (e.g., checkbox toggle, list item selection, button focus, menu navigation)
- **`Command.HotKey`**: Handles hot key activation
- **Movement commands**: `Up`, `Down`, `Left`, `Right`, etc.

Commands are invoked via:
- **Key bindings**: `KeyBindings` map keys to commands
- **Mouse bindings**: `MouseBindings` map mouse actions to commands
- **Programmatic**: `InvokeCommand(command, context)`

### The Rename: Select → Activate

Recently (as noted in the issue), `Select` was renamed to `Activate` because:

- **Ambiguity**: "Select" conflated state changes (checkbox toggle) with preparatory actions (button focus)
- **Clarity**: "Activate" better describes preparing a view for interaction
- **Propagation**: The rename set the stage for targeted propagation with `PropagateActivating`

---

## Current State Analysis

### How `Command.Accept` Propagation Works Today

The special-case propagation logic is in `View.RaiseAccepting()` (View.Command.cs:127-182):

```csharp
protected bool? RaiseAccepting(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };

    // CWP: Virtual method first
    args.Handled = OnAccepting(args) || args.Handled;

    // CWP: Event second
    if (!args.Handled && Accepting is { })
    {
        Accepting?.Invoke(this, args);
    }

    // CWP: Raise non-cancellable "Accepted" event if handled
    if (args.Handled)
    {
        RaiseAccepted(ctx);
    }

    // **SPECIAL CASE**: Accept propagation logic
    if (!args.Handled)
    {
        // 1. Check for IsDefault peer button
        View? isDefaultView = SuperView?.GetSubViews(includePadding: true)
            .FirstOrDefault(v => v is Button { IsDefault: true });

        if (isDefaultView != this && isDefaultView is Button { IsDefault: true } button)
        {
            bool? handled = isDefaultView.InvokeCommand(Command.Accept, ctx);
            if (handled == true)
            {
                return true;
            }
        }

        // 2. Bubble to SuperView
        if (SuperView is { })
        {
            return SuperView?.InvokeCommand(Command.Accept, ctx);
        }
    }

    return args.Handled;
}
```

**Key Points**:
1. Follows CWP for `OnAccepting` and `Accepting` event
2. Adds **hard-coded** propagation logic after CWP phase
3. First tries peer `Button` with `IsDefault == true`
4. Then bubbles to `SuperView`
5. This logic is **ONLY** in `RaiseAccepting` - no other command has it

### How `Command.Activate` Works Today

The current implementation of `RaiseActivating()` (View.Command.cs:268-284):

```csharp
protected virtual bool? RaiseActivating(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };

    // CWP: Virtual method first
    if (OnActivating(args) || args.Handled)
    {
        return true;
    }

    // CWP: Event second
    Activating?.Invoke(this, args);

    // NO propagation logic
    return Activating is null ? null : args.Handled;
}
```

**Key Points**:
1. Follows CWP pattern correctly
2. **Does NOT propagate** - stops at the view that handles it
3. **This is the problem** for hierarchical views like `MenuBar` - Many features are BROKEN TODAY BECAUSE OF THIS.

### How MenuBar Currently Works Around This

Since `Command.Activate` doesn't propagate, `MenuBar` and `PopoverMenu` use a **workaround** via view-specific events:

#### In `Menu.cs`:
```csharp
internal void RaiseSelectedMenuItemChanged(MenuItem? selected)
{
    SelectedMenuItem = selected;
    OnSelectedMenuItemChanged(selected);
    SelectedMenuItemChanged?.Invoke(this, selected);
}

public event EventHandler<MenuItem?>? SelectedMenuItemChanged;
```

#### In `PopoverMenu.cs`:
```csharp
private void MenuOnSelectedMenuItemChanged(object? sender, MenuItem? e)
{
    ShowSubMenu(e);
}

// During initialization:
menu.SelectedMenuItemChanged += MenuOnSelectedMenuItemChanged;
```

#### In `MenuBar.cs`:
```csharp
protected override void OnSelectedMenuItemChanged(MenuItem? selected)
{
    if (IsOpen() && selected is MenuBarItem { PopoverMenuOpen: false } selectedMenuBarItem)
    {
        ShowItem(selectedMenuBarItem);
    }
}
```

**Problems with This Approach**:
1. **Tight Coupling**: Subviews (`Menu`) must know their superviews need notifications
2. **Not Generalizable**: Custom event per use case (`SelectedMenuItemChanged`)
3. **Violation of CWP**: Circumvents the standard command/event system
4. **Fragile**: Adding new hierarchical views requires new custom events

---

## Problems Identified

### Problem 1: Hard-Coded, Command-Specific Propagation

**Current State**: Only `Command.Accept` has propagation logic, hard-coded in `RaiseAccepting()`.

**Issues**:
- **Not generalizable**: Other commands can't propagate (e.g., `Command.Activate`)
- **Maintenance burden**: Adding propagation to other commands requires duplicating logic
- **Inconsistent**: Different commands behave differently with no clear pattern

**Evidence**: Lines 154-179 in View.Command.cs show the special-case logic only exists for `Accept`.

### Problem 2: Missing `Command.Activate` Propagation

**Current State**: `Command.Activate` does NOT propagate to superviews.

**Issues**:
- **MenuBar broken**: `MenuBar` can't respond to `MenuItem` activations to show popovers
- **Workarounds required**: Custom events like `SelectedMenuItemChanged` needed
- **Limits composability**: New hierarchical views can't leverage activation

**Evidence**: 
- View.Command.cs:268-284 shows `RaiseActivating` lacks propagation
- PopoverMenu.cs:670-674 shows the workaround event handler
- MenuBar.cs:303-311 shows `OnSelectedMenuItemChanged` override

### Problem 3: Tight Coupling via View-Specific Events

**Current State**: `Menu` raises `SelectedMenuItemChanged` that `MenuBar` and `PopoverMenu` subscribe to.

**Issues**:
- **Coupling**: Subviews know about superview needs (violates separation of concerns)
- **Not scalable**: Each hierarchical view pair needs custom events
- **Bypasses CWP**: Custom events circumvent the standard command system
- **Hard to discover**: Developers must know about these special events

**Evidence**:
- Menu.cs:168-173 defines and raises `SelectedMenuItemChanged`
- PopoverMenu.cs:342 subscribes to it
- MenuBar.cs:303 overrides to handle it

### Problem 4: Unclear Ownership of Propagation Behavior

**Current State**: Propagation logic is in the "raising" view (subview), not the "receiving" view (superview).

**Issues**:
- **Inverted responsibility**: Subviews shouldn't dictate how superviews handle commands
- **Inflexible**: Can't change propagation behavior per superview instance
- **Not opt-in**: Superviews can't choose which commands to receive

**Evidence**: The propagation code in `RaiseAccepting()` (View.Command.cs:154-179) is in the base `View` class, applying to all views universally.

---

## Shortcut + CommandView Scenario Analysis

### Overview

A critical use case for command propagation is when a `Shortcut` contains a custom `CommandView` such as a `CheckBox`. This section validates that the proposed `PropagatedCommands` design correctly supports this scenario.

### View Hierarchy

```
SuperView (e.g., StatusBar, Bar, or custom container)
  └── Shortcut
        └── CheckBox (as CommandView)
```

### How Shortcut Handles CommandView Events

`Shortcut` acts as a **command aggregator** for its `CommandView`. When setting the `CommandView` property, Shortcut attaches event handlers:

```csharp
_commandView.Activating += CommandViewOnActivating;
_commandView.Accepting += CommandViewOnAccepted;

void CommandViewOnAccepted (object? sender, CommandEventArgs e)
{
    e.Handled = true;  // Eat CommandView's Accept
}

void CommandViewOnActivating (object? sender, CommandEventArgs e)
{
    if (/* context indicates direct activation */)
    {
        InvokeCommand (Command.Activate, e);  // Forward to Shortcut
    }
    e.Handled = true;  // Eat CommandView's Activating
}
```

**Key Behavior**: Shortcut intercepts and "eats" all CommandView events, then raises its own events through `DispatchCommand`.

### Command Flow in DispatchCommand

When a command is dispatched on a Shortcut:

1. **Activate CommandView**: `CommandView.InvokeCommand(Command.Activate)` - toggles CheckBox
2. **Raise Activating**: `RaiseActivating(ctx)` - if handled, **stops here**
3. **Set Focus**: If focusable, sets focus
4. **Raise Accepting**: `RaiseAccepting(ctx)` - if handled, **stops here**
5. **Invoke Action**: If `Action` is set, invokes it

### Validation: PropagatedCommands Works for This Scenario

The proposed design works correctly:

1. **CheckBox state changes**: Step 1 invokes Activate on CommandView, toggling the checkbox
2. **Activating can propagate**: Step 2 calls `RaiseActivating` on Shortcut, which (with the proposed changes) will propagate to SuperView if in `PropagatedCommands`
3. **Accepting can propagate**: Step 4 calls `RaiseAccepting`, which already propagates by default

### Important Behavior: Handled Commands Stop the Chain

If `RaiseActivating` returns `true` (handled locally or via propagation), `DispatchCommand` returns early and:
- **`Accepting` is NOT raised**
- **`Action` is NOT invoked**

This is intentional CWP behavior but should be documented clearly.

---

## FlagSelector in MenuItem in MenuBar Scenario

### Overview

This is the most complex propagation scenario and represents a critical use case that is **BROKEN TODAY**. It involves:

- `FlagSelector` (contains `CheckBox` SubViews) as the `CommandView` of a `MenuItem`
- That `MenuItem` is in a `Menu` which is the `Root` of a `PopoverMenu`
- That `PopoverMenu` belongs to a `MenuBarItem`
- That `MenuBarItem` is in a `MenuBar`

### Class Hierarchy Understanding

```
View
  └─ Bar (horizontal/vertical container for Shortcuts)
      └─ Menu (vertical bar of MenuItems)
          └─ MenuBar (horizontal bar of MenuBarItems)

View
  └─ Shortcut
      └─ MenuItem (Shortcut with optional SubMenu)
          └─ MenuBarItem (MenuItem with PopoverMenu)

View
  └─ PopoverBaseImpl
      └─ PopoverMenu (displays cascading Menus)
```

**Key relationships:**
- `MenuBar` IS-A `Menu` (inheritance, not containment)
- `MenuBarItem` IS-A `MenuItem` IS-A `Shortcut`
- `PopoverMenu` is NOT a normal View container - it dynamically adds/removes `Menu` SubViews

### Complete View Hierarchy

```
MenuBar (contains MenuBarItems as SubViews)
  └─ MenuBarItem (owns PopoverMenu, NOT as SubView)
      └─ PopoverMenu (dynamically manages Menu SubViews)
          └─ Menu (Root - contains MenuItems as SubViews)
              └─ MenuItem (Shortcut subclass)
                  └─ FlagSelector (as CommandView, NOT as SubView)
                      └─ CheckBox (SubView of FlagSelector)
```

### Current Event Flow (BROKEN)

When a user activates a CheckBox in this hierarchy:

```
1. User presses Space on focused CheckBox
   ↓
2. CheckBox.Command.Activate handler runs
   ├─ Advances CheckState
   ├─ RaiseActivating(ctx) called
   ↓
3. FlagSelector.OnCheckboxOnActivating intercepts
   ├─ InvokeCommand(Command.Activate, ctx) on FlagSelector
   ├─ args.Handled = true (consumes CheckBox's event)
   ↓
4. FlagSelector.RaiseActivating(ctx)
   ├─ OnActivating virtual method
   ├─ Activating event
   ↓
5. MenuItem.CommandViewOnActivating intercepts (FlagSelector is CommandView)
   ├─ InvokeCommand(Command.Activate, ctx) on MenuItem
   ├─ args.Handled = true (consumes FlagSelector's event)
   ↓
6. MenuItem.DispatchCommand(ctx)
   ├─ RaiseActivating(ctx) on MenuItem
   ↓
7. **STOPS HERE** - Command.Activate does NOT propagate!
   ├─ Menu never sees the Activate
   ├─ MenuBar never sees the Activate
   └─ SelectedMenuItemChanged workaround doesn't help for CommandView activation
```

**The Problem**: The activation event from the CheckBox never reaches MenuBar. The `SelectedMenuItemChanged` workaround only fires when `SelectedMenuItem` changes (focus-based), not when a CommandView inside a MenuItem is activated.

### Required Event Flow (With PropagatedCommands)

```
1-6. Same as above through MenuItem.RaiseActivating(ctx)
   ↓
7. MenuItem.RaiseActivating checks propagation
   ├─ Menu.PropagatedCommands contains Command.Activate? YES
   ├─ Menu.InvokeCommand(Command.Activate, ctx)
   ↓
8. Menu.RaiseActivating(ctx)
   ├─ OnActivating virtual method
   ├─ Activating event
   ├─ PopoverMenu.PropagatedCommands contains Command.Activate? YES
   ├─ (Note: Menu's SuperView during display is PopoverMenu)
   ↓
9. PopoverMenu forwards to MenuBarItem (owner)
   ├─ MenuBarItem.PropagatedCommands contains Command.Activate? YES
   ↓
10. MenuBar.RaiseActivating(ctx)
    ├─ OnActivating receives ctx.SourceId = "AutoSave", ctx.Value = CheckState.Checked
    ├─ Can identify the originating view and respond appropriately
    └─ HANDLED - MenuBar can now react to deep activations
```

### Source Tracking Through the Chain

The `ctx.SourceId` and `ctx.Value` properties are critical for this scenario:

| Step | View Processing | `ctx.SourceId` | `ctx.Value` | `this` / `sender` |
|------|-----------------|----------------|-------------|-------------------|
| 2 | CheckBox | "AutoSave" | CheckState.Checked | CheckBox |
| 4 | FlagSelector | "AutoSave" | CheckState.Checked | FlagSelector |
| 6 | MenuItem | "AutoSave" | CheckState.Checked | MenuItem |
| 8 | Menu | "AutoSave" | CheckState.Checked | Menu |
| 10 | MenuBar | "AutoSave" | CheckState.Checked | MenuBar |

**`ctx.SourceId` and `ctx.Value` remain constant** throughout propagation, while `this`/`sender` changes at each step. This allows MenuBar's handler to:
- Identify which view triggered the command via `SourceId` (the user-defined `View.Id`)
- Access the relevant data via `Value` (from views implementing `IValue<T>`)

**Why SourceId instead of Source (View reference)?** The original design used `ctx.Source` (a View reference), but this creates a **lifecycle issue**: when a superview receives a propagated event, the Source reference may be invalid if an intermediate view has removed or disposed the original subview. Using `SourceId` (string) and `Value` (object?) eliminates this issue:
- **SourceId** is a stable identifier (the `View.Id` string)
- **Value** is the data captured at invocation time (from `IValue<T>.GetValue()`)
- No dangling references - handlers don't need the View itself

### Accept Flow (Similar Pattern)

For `Command.Accept` (e.g., user double-clicks the CheckBox):

```
CheckBox.Accept → FlagSelector.Accept → MenuItem.Accept
  → Menu.Accept → MenuBar.Accept
```

`Command.Accept` already propagates by default, but the event interception patterns in FlagSelector and Shortcut may interfere. The design must ensure:

1. FlagSelector forwards Accept to itself (already does via `OnCheckboxOnAccepting`)
2. MenuItem (as Shortcut) forwards Accept from CommandView (already does via `CommandViewOnAccepted`)
3. Propagation continues from MenuItem → Menu → MenuBar

### Developer Experience for MenuBar

#### Current (Problematic) Approach

Today (demo code from in MenuBar.EnableForDesign; abbreviated) - **This approach has issues:**

```cs
public MenuBar CreateMenuBar()
{
    var autoSaveCb = new CheckBox
    {
        Title = "_Auto Save"
    };

    var mutuallyExclusiveOptionsSelector = new OptionSelector
    {
        Labels = ["G_ood", "_Bad", "U_gly"],
        Value = 0
    };

    var menuBgColorCp = new ColorPicker
    {
        Width = 30
    };
    menuBgColorCp.ColorChanged += (sender, args) =>
                                    {
                                        // BUGBUG: This is weird.
                                        SetScheme (
                                                    GetScheme () with
                                                    {
                                                        Normal = new (
                                                                    GetAttributeForRole (VisualRole.Normal).Foreground,
                                                                    args.Result,
                                                                    GetAttributeForRole (VisualRole.Normal).Style)
                                                    });
                                    };

    Add (new MenuBarItem (Strings.menuFile,
        [
            // Demonstrates propagation based on Accept of Command.New
            new MenuItem (targetView as View, Command.New),
            // Demonstrates propagation based on Activate in a CheckBox (doesn't actually work)
            new MenuItem
            {
                Title = "_File Options",
                SubMenu = new (
                                [
                                    new ()
                                    {
                                        Id = "AutoSave",
                                        Text = "(no Command)",
                                        Key = Key.F10,
                                        CommandView = autoSaveCb
                                    }
                                ]
                                )
            },
            // Demonstrates a submenu
            new MenuItem
            {
                Title = "_Preferences",
                SubMenu = new (
                                [
                                    // Demonstrates nested propagation based on Activate in a OptionSelector  (doesn't actually work)
                                    new MenuItem
                                    {
                                        HelpText = "3 Mutually Exclusive Options",
                                        CommandView = mutuallyExclusiveOptionsSelector,
                                        Key = Key.F7
                                    },
                                    // Demonstrates nested propagation based on Activate in a CheckBox (doesn't actually work)
                                    new MenuItem
                                    {
                                        HelpText = "MenuBar BG Color",
                                        CommandView = menuBgColorCp,
                                        Key = Key.F8
                                    }
                                ]
                                )
            }
        ]
        )
     );
}

// Usage
menuBar.OnAccepted += (sender, args) =>
{
    // menuBgColorCp is already handeled via ColorChanged event above

    // Handle AutoSave CheckBox
    if (args.Context?.Source is CheckBox cb && cb.Id == "AutoSave")
    {
        _autoSave = cb.CheckState == CheckState.Checked;
    }

    // Handle MutuallyExclusiveOptionsSelector OptionSelector
    if (args.Context?.Source is OptionSelector os && os.Id == "MutuallyExclusiveOptionsSelector")
    {
        _selectedOption = os.Value;
    }
};

```

**Problems with this approach:**
1. **Lifecycle issue**: `args.Context?.Source` is a View reference that could become invalid if an intermediate view disposes the subview during propagation
2. **Casting required**: Must cast to specific types (`CheckBox`, `OptionSelector`) to access properties
3. **Separate event subscriptions**: Need to subscribe to `ColorChanged` event separately for `ColorPicker`
4. **Verbose**: Repeated type checking and casting patterns

#### Proposed (Improved) Approach

No need to subscribe to events on CommandViews. Just set up the MenuBar and MenuItems as usual:
```csharp
MenuBar menuBar = new ()
{
    PropagatedCommands = [Command.Accept, Command.Activate],
    MenuBarItems =
    [
        new MenuBarItem (
            "File",
            [
                new MenuItem (targetView as View, Command.New),
                new ()
                {
                    Title = "File Options",
                    SubMenu = new Menu (
                        [
                            new ()
                            {
                                Id = "AutoSave",
                                Text = "(no Command)",
                                Key = Key.F10,
                                CommandView = new CheckBox
                                {
                                    Title = "_Auto Save",
                                    Id = "AutoSave"
                                }
                            }
                        ]
                    )
                },
                new ()
                {
                    Title = "Preferences",
                    SubMenu = new Menu (
                        [
                            new ()
                            {
                                HelpText = "3 Mutually Exclusive Options",
                                CommandView = new OptionSelector
                                {
                                    Id = "MutuallyExclusiveOptionsSelector",
                                    Labels = ["G_ood", "_Bad", "U_gly"],
                                    Value = 0
                                },
                                Key = Key.F7
                            },
                            new ()
                            {
                                HelpText = "MenuBar BG Color",
                                CommandView = new ColorPicker
                                {
                                    Id = "MenuBgColorCp",
                                    Width = 30,
                                    Value = Color.Blue
                                },
                                Key = Key.F8
                            }
                        ]
                    )
                }
            ]
        )
    ]
};

// Handle Accepting (cancellable) - the "New" menuitem is handled automatically via Command.New binding
menuBar.Accepting += (sender, args) =>
{
    switch (args.Context?.SourceId)
    {
        case "AutoSave":
            _autoSave = args.Context?.Value is CheckState checkState && checkState == CheckState.Checked;
            break;

        case "MutuallyExclusiveOptionsSelector":
            _selectedOption = args.Context?.Value is int optionValue ? optionValue : 0;
            break;

        case "MenuBgColorCp":
            if (args.Context?.Value is Color color)
            {
                menuBar.SetScheme (
                    menuBar.GetScheme () with
                    {
                        Normal = new (
                            menuBar.GetAttributeForRole (VisualRole.Normal).Foreground,
                            color,
                            menuBar.GetAttributeForRole (VisualRole.Normal).Style)
                    });
            }
            break;
    }
};
```

#### Alternative: Direct Subscription on CommandView

For some scenarios, you may want to handle the activation directly on the CommandView rather than at the MenuBar level. This is useful when the handling logic is specific to that view and doesn't need centralized management:

```cs
// Create a LinearRange with direct event subscription
LinearRange zoomRange = new ()
{
    Id = "ZoomLevel",
    Minimum = 10,
    Maximum = 200,
    Value = 100
};

// Subscribe directly to the CommandView's Activating event
zoomRange.Activating += (sender, args) =>
{
    // Handle the activation locally - no propagation needed
    if (args.Context?.Value is int zoomLevel)
    {
        _currentZoom = zoomLevel;
        UpdateZoomDisplay (zoomLevel);
    }
    args.Handled = true;  // Prevent further propagation if desired
};

// Add the MenuItem with the CommandView
MenuBarItem viewMenu = new (
    "View",
    [
        new MenuItem
        {
            Title = "Zoom",
            HelpText = "Adjust zoom level",
            CommandView = zoomRange,
            Key = Key.F9
        }
    ]
);
```

**When to use direct subscription vs MenuBar-level handling:**
- **Direct subscription**: When the logic is self-contained and specific to that CommandView
- **MenuBar-level handling**: When you want centralized handling of multiple CommandViews, or when the response requires access to MenuBar state

**Note**: If `args.Handled = true` is set in the direct handler, the event will NOT propagate to the MenuBar's Accepting handler. This gives you control over whether events bubble up or are handled locally.

#### Handling at MenuItem Level

You can handle events directly on a MenuItem, which is useful when you want to respond before the event propagates further up the hierarchy:

```cs
MenuItem settingsMenuItem = new ()
{
    Id = "SettingsItem",
    Title = "Settings",
    CommandView = new CheckBox
    {
        Id = "DarkMode",
        Title = "_Dark Mode"
    }
};

// Handle Accepting at the MenuItem level (cancellable)
settingsMenuItem.Accepting += (sender, args) =>
{
    // args.Context?.SourceId = "DarkMode" (the CheckBox that was activated)
    // args.Context?.Value = CheckState from the CheckBox
    if (args.Context?.Value is CheckState state)
    {
        ApplyTheme (state == CheckState.Checked ? Theme.Dark : Theme.Light);
    }
    // Don't set args.Handled = true if you want it to continue propagating to Menu/MenuBar
};
```

#### Handling at Menu Level

Handling at the Menu level lets you respond to any MenuItem within that menu:

```cs
Menu fileMenu = new (
    [
        new MenuItem { Id = "New", Title = "_New", Key = Key.N.WithCtrl },
        new MenuItem { Id = "Open", Title = "_Open", Key = Key.O.WithCtrl },
        new MenuItem { Id = "Save", Title = "_Save", Key = Key.S.WithCtrl }
    ]
);

// Handle Accepting at the Menu level (cancellable)
fileMenu.Accepting += (sender, args) =>
{
    switch (args.Context?.SourceId)
    {
        case "New":
            CreateNewDocument ();
            break;
        case "Open":
            OpenDocument ();
            break;
        case "Save":
            SaveDocument ();
            break;
    }
    // args.Handled = true; // Set if you want to stop propagation to MenuBar
};
```

#### Handling at MenuBarItem Level

MenuBarItem is useful for handling all items within a top-level menu category:

```cs
MenuBarItem editMenuBarItem = new (
    "Edit",
    [
        new MenuItem { Id = "Cut", Title = "Cu_t", Key = Key.X.WithCtrl },
        new MenuItem { Id = "Copy", Title = "_Copy", Key = Key.C.WithCtrl },
        new MenuItem { Id = "Paste", Title = "_Paste", Key = Key.V.WithCtrl }
    ]
);

// Handle Accepting at the MenuBarItem level (cancellable)
editMenuBarItem.Accepting += (sender, args) =>
{
    switch (args.Context?.SourceId)
    {
        case "Cut":
            CutToClipboard ();
            break;
        case "Copy":
            CopyToClipboard ();
            break;
        case "Paste":
            PasteFromClipboard ();
            break;
    }
};
```

#### Summary: Event Handling Levels

| Level | Use Case | SourceId Contains |
|-------|----------|-------------------|
| **CommandView** | Self-contained logic for a specific control | N/A (you have the view directly) |
| **MenuItem** | Handle a specific menu item and its CommandView | CommandView's Id (if present) or MenuItem's Id |
| **Menu** | Handle all items in a submenu | The originating MenuItem or CommandView Id |
| **MenuBarItem** | Handle all items under a top-level menu | The originating MenuItem or CommandView Id |
| **MenuBar** | Centralized handling for entire menu system | The originating MenuItem or CommandView Id |

**Propagation order**: CommandView → MenuItem → Menu → MenuBarItem → MenuBar

Set `args.Handled = true` at any level to stop propagation to higher levels.

### Views That Must Opt-In to Propagation

For this scenario to work, the following views must include `Command.Activate` in their `PropagatedCommands`:

| View | Default PropagatedCommands | Required for This Scenario |
|------|---------------------------|---------------------------|
| Menu | `[Command.Accept]` | `[Command.Accept, Command.Activate]` |
| PopoverMenu | `[Command.Accept]` | `[Command.Accept, Command.Activate]` |
| MenuBar | `[Command.Accept]` | `[Command.Accept, Command.Activate]` |

### Implementation Considerations

1. **PopoverMenu is not a normal SuperView**: When a Menu is displayed inside a PopoverMenu, the Menu's SuperView is temporarily set to PopoverMenu. Propagation must work through this dynamic relationship.

2. **MenuBarItem owns PopoverMenu but is not its SuperView**: The PopoverMenu must explicitly forward propagated commands to its owning MenuBarItem, or MenuBarItem must subscribe to PopoverMenu's events.

3. **Event interception must not block propagation**: FlagSelector and Shortcut both intercept and consume events from their subviews. The design relies on them correctly forwarding commands via `InvokeCommand()`, which then triggers propagation.

### Test Cases for This Scenario

1. **Activate propagation through full hierarchy**:
   - Create MenuBar with MenuBarItem containing PopoverMenu with Menu with MenuItem
   - Set MenuItem.CommandView = FlagSelector with CheckBox (Id = "AutoSave")
   - Activate CheckBox
   - Verify MenuBar.OnActivating receives event with `ctx.SourceId` = "AutoSave" and `ctx.Value` = CheckState

2. **Accept propagation through full hierarchy**:
   - Same setup as above
   - Accept (double-click) CheckBox
   - Verify MenuBar.OnAccepting receives event with `ctx.SourceId` = "AutoSave" and `ctx.Value` = CheckState

3. **SourceId and Value preservation**:
   - Verify `ctx.SourceId` and `ctx.Value` remain constant at every level (FlagSelector, MenuItem, Menu, MenuBar)
   - Verify `ctx.Binding` is preserved (KeyBinding or MouseBinding from original input)

4. **Handled at intermediate level**:
   - Menu.OnActivating sets `args.Handled = true`
   - Verify MenuBar.OnActivating is NOT called
   - Verify CWP semantics are respected

---

## Design Requirements

### Functional Requirements

1. **Generalized Propagation**: Support propagation for multiple commands, not just `Accept`
2. **Opt-In Model**: Superviews explicitly declare which commands propagate to them
3. **Backward Compatibility**: Existing `Command.Accept` propagation behavior must be preserved
4. **CWP Compliance**: Maintain the Cancellable Work Pattern structure
5. **Decoupling**: Subviews remain ignorant of superview propagation needs

### Non-Functional Requirements

1. **Performance**: Minimal overhead (avoid reflection, keep checks simple)
2. **Discoverability**: Clear, documented API
3. **Consistency**: All commands follow the same propagation rules
4. **Extensibility**: Easy to add propagation to new commands

### Constraints

1. **Alpha Status**: Terminal.Gui v2 is in alpha; breaking changes are acceptable before beta
2. **.NET 8 Compatible**: Use available C# 12 features
3. **Follows Conventions**: Adhere to Terminal.Gui coding standards
4. **Binding Refactor Complete**: ✅ The [command_binding_refactor_plan.md](./command_binding_refactor_plan.md) is complete - non-generic `CommandContext`, `InputBinding`, and `IInputBinding.Source` are all available

---

## Proposed Solution

### Overview

Introduce a **propagation registry** that allows superviews to declare which commands should propagate to them from unhandled subviews. The mechanism:

1. **Property**: `IReadOnlyList<Command> PropagatedCommands { get; set; }`
2. **Default**: `new List<Command> { Command.Accept }` (preserves current behavior)
3. **Propagation Logic**: Moved to a **helper method** used by all `Raise*` methods
4. **Opt-In**: Superviews set `PropagatedCommands` to include `Command.Activate` (or others)

### Detailed Design

#### 1. Add `PropagatedCommands` Property to `View`

```csharp
/// <summary>
///     Gets or sets the list of commands that should propagate to this View from unhandled subviews.
///     When a subview raises a command that is not handled (e.g., via <see cref="Accepting"/> or <see cref="Activating"/>),
///     and the command is in the SuperView's <see cref="PropagatedCommands"/> list, the command will be invoked on the SuperView.
/// </summary>
/// <remarks>
///     <para>
///         By default, only <see cref="Command.Accept"/> propagates to maintain backward compatibility.
///         To enable <see cref="Command.Activate"/> propagation (e.g., for <see cref="MenuBar"/> managing popover visibility),
///         set this property to include <see cref="Command.Activate"/>:
///     </para>
///     <code>
///         menuBar.PropagatedCommands = new List&lt;Command&gt; { Command.Accept, Command.Activate };
///     </code>
///     <para>
///         This design decouples subviews from superviews, allowing superviews to opt-in to command propagation
///         without requiring subviews to know about superview needs.
///     </para>
/// </remarks>
public IReadOnlyList<Command> PropagatedCommands { get; set; } = new List<Command> { Command.Accept };
```

#### 2. Update `CommandContext` to Use SourceId and Value

Replace `View? Source` with `string? SourceId` and add `object? Value`:

```csharp
public record struct CommandContext : ICommandContext
{
    public Command Command { get; set; }
    public string? SourceId { get; set; }       // Replaces View? Source - uses View.Id
    public object? Value { get; set; }          // Value from IValue<T>.GetValue() if available
    public IInputBinding? Binding { get; set; }
}
```

**Why this change?**
- **Lifecycle safety**: View references can become invalid if intermediate views dispose subviews during propagation
- **Developer experience**: `SourceId` enables meaningful switch/case patterns (`case "AutoSave":`)
- **Data access**: `Value` provides the relevant data (CheckState, selected option, color, etc.) without needing the View

#### 3. Add Non-Generic `IValue` Interface

To support boxing values from generic `IValue<T>` implementations:

```csharp
public interface IValue
{
    object? GetValue();
}

public interface IValue<TValue> : IValue
{
    TValue? Value { get; set; }
    object? IValue.GetValue() => Value;  // Default implementation
}
```

#### 4. Create a Propagation Helper Method

```csharp
/// <summary>
///     Propagates a command to the SuperView if the command is in the SuperView's <see cref="PropagatedCommands"/> list.
///     Handles the special case of invoking the command on a peer IsDefault button for <see cref="Command.Accept"/>.
/// </summary>
/// <param name="command">The command to propagate.</param>
/// <param name="ctx">The command context.</param>
/// <param name="handled">Whether the command was already handled by this view.</param>
/// <returns>
///     <see langword="true"/> if the command was handled by propagation; otherwise, <see langword="false"/>.
/// </returns>
protected bool? PropagateCommand(Command command, ICommandContext? ctx, bool handled)
{
    // Only propagate if not already handled
    if (handled)
    {
        return true;
    }

    // Special case for Command.Accept: check for IsDefault peer button first
    if (command == Command.Accept)
    {
        View? isDefaultView = SuperView?.GetSubViews(includePadding: true)
            .FirstOrDefault(v => v is Button { IsDefault: true });

        if (isDefaultView != this && isDefaultView is Button { IsDefault: true })
        {
            bool? buttonHandled = isDefaultView.InvokeCommand(Command.Accept, ctx);
            if (buttonHandled == true)
            {
                return true;
            }
        }
    }

    // Check if SuperView wants this command propagated
    if (SuperView?.PropagatedCommands?.Contains(command) == true)
    {
        return SuperView.InvokeCommand(command, ctx);
    }

    return handled;
}
```

#### 3. Update `RaiseAccepting` to Use Helper

```csharp
protected bool? RaiseAccepting(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };

    // CWP: Virtual method first
    args.Handled = OnAccepting(args) || args.Handled;

    // CWP: Event second
    if (!args.Handled && Accepting is { })
    {
        Accepting?.Invoke(this, args);
    }

    // CWP: Raise non-cancellable "Accepted" event if handled
    if (args.Handled)
    {
        RaiseAccepted(ctx);
    }

    // NEW: Use propagation helper (maintains backward compatibility)
    return PropagateCommand(Command.Accept, ctx, args.Handled);
}
```

#### 4. Update `RaiseActivating` to Use Helper

```csharp
protected virtual bool? RaiseActivating(ICommandContext? ctx)
{
    CommandEventArgs args = new() { Context = ctx };

    // CWP: Virtual method first
    if (OnActivating(args) || args.Handled)
    {
        return true;
    }

    // CWP: Event second
    Activating?.Invoke(this, args);

    // NEW: Use propagation helper (enables opt-in propagation)
    return PropagateCommand(Command.Activate, ctx, args.Handled ?? false);
}
```

#### 5. Update `MenuBar` to Opt-In to `Command.Activate` Propagation

```csharp
public MenuBar(IEnumerable<MenuBarItem> menuBarItems) : base(menuBarItems)
{
    // ... existing initialization ...

    // NEW: Opt-in to Command.Activate propagation for popover management
    PropagatedCommands = new List<Command> { Command.Accept, Command.Activate };

    // ... rest of initialization ...
}
```

#### 6. Handle `Command.Activate` in `MenuBar`

Add a new override or update the existing `Command.Activate` handler in `MenuBar`:

```csharp
protected override bool OnActivating(CommandEventArgs args)
{
    // Look up the MenuBarItem by SourceId (direct SubViews only - lifecycle safe)
    MenuBarItem? menuBarItem = SubViews.OfType<MenuBarItem>()
        .FirstOrDefault(m => m.Id == args.Context?.SourceId);

    // If a MenuBarItem is being activated, show its popover if applicable
    if (menuBarItem is { PopoverMenuOpen: false })
    {
        if (!CanFocus)
        {
            Active = true;
        }

        ShowItem(menuBarItem);

        if (!menuBarItem.HasFocus)
        {
            menuBarItem.SetFocus();
        }

        return true; // Handled
    }

    return base.OnActivating(args);
}
```

### Identifying the Originating View in Handlers

During command propagation, handlers need to identify which subview initiated the command. Several properties are available:

| Property | Meaning | Changes During Propagation? |
|----------|---------|----------------------------|
| `sender` parameter | The View currently raising the event | **Yes** - changes at each propagation step |
| `this` in virtual override | The View currently processing | **Yes** - changes at each propagation step |
| `args.Context?.SourceId` | The `Id` of the View that originally invoked | **No** - remains constant |
| `args.Context?.Value` | The value from the source (if `IValue<T>`) | **No** - remains constant |
| `args.Context?.Binding` | The original binding (`KeyBinding`, `MouseBinding`, etc.) | **No** - passed unchanged |

**For propagation handlers**, use `args.Context?.SourceId` and `args.Context?.Value` to identify and process the originating view's data:

```csharp
// Option 1: Switch on SourceId for multiple handlers (cancellable)
menuBar.Accepting += (sender, args) =>
{
    switch (args.Context?.SourceId)
    {
        case "AutoSave":
            _autoSave = args.Context?.Value is CheckState.Checked;
            break;
        case "MenuBgColorCp":
            if (args.Context?.Value is Color color)
                SetBackgroundColor (color);
            break;
    }
};

// Option 2: Override in subclass
protected override bool OnActivating (CommandEventArgs args)
{
    // Use SourceId to identify, Value to get the data
    if (args.Context?.SourceId == "settings" && args.Context?.Value is bool enabled)
    {
        ApplySettings (enabled);
        return true;
    }
    return base.OnActivating (args);
}
```

**Important**: The binding type (`KeyBinding`, `MouseBinding`, or future `InputBinding`) is preserved during propagation. When `PropagateCommand` calls `SuperView.InvokeCommand(command, ctx)`, the original `ICommandContext` (including its binding) is passed unchanged. This means:

- Pattern matching on `args.Context?.Binding` works correctly at any propagation level
- The binding's `Data` and type-specific properties remain accessible
- Propagation does **not** create a new binding - it forwards the existing one

**Note on SourceId uniqueness**: `SourceId` uses the `View.Id` property, which is developer-defined (like HTML element ids). Uniqueness within your application is your responsibility. If two views have the same `Id`, only one will match in switch statements.

### Migration Path

Since Terminal.Gui v2 is in alpha and breaking changes are acceptable before beta, we can directly remove legacy patterns rather than deprecating them.

#### Remove Custom Events

Remove `SelectedMenuItemChanged` and related workarounds:

1. **Remove `SelectedMenuItemChanged` event** from `Menu.cs`
2. **Remove `RaiseSelectedMenuItemChanged` method** from `Menu.cs`
3. **Remove event subscriptions** in `PopoverMenu` and `MenuBar`
4. **Remove `OnSelectedMenuItemChanged` overrides** - replace with `OnActivating` using propagation
5. **Update documentation** to reflect the new pattern

These custom events were workarounds for missing `Command.Activate` propagation. With `PropagatedCommands`, the standard command system handles this cleanly.

---

## Implementation Impact

### Files to Modify

1. **Terminal.Gui/ViewBase/View.Command.cs**:
   - Add `PropagatedCommands` property
   - Add `PropagateCommand()` helper method
   - Update `RaiseAccepting()` to use helper
   - Update `RaiseActivating()` to use helper
   - Update XML documentation

2. **Terminal.Gui/Views/Menu/MenuBar.cs**:
   - Set `PropagatedCommands = [Command.Accept, Command.Activate]` in constructor
   - Add/update `OnActivating()` override to handle propagated activations
   - Remove `OnSelectedMenuItemChanged` override (replaced by propagation)

3. **Terminal.Gui/Views/Menu/Menu.cs**:
   - Set `PropagatedCommands = [Command.Accept, Command.Activate]` in constructor
   - Remove `SelectedMenuItemChanged` event
   - Remove `RaiseSelectedMenuItemChanged` method

4. **Terminal.Gui/Views/Menu/PopoverMenu.cs**:
   - Set `PropagatedCommands = [Command.Accept, Command.Activate]` in constructor
   - Remove `MenuOnSelectedMenuItemChanged` handler
   - Remove event subscription in `Root` setter
   - Add logic to forward propagated commands to owning MenuBarItem (PopoverMenu → MenuBarItem is ownership, not SuperView)

5. **Terminal.Gui/Views/Shortcut.cs**:
   - Verify `CommandViewOnActivating` correctly forwards context (including `Source`)
   - Update XML documentation to clarify command aggregation behavior
   - Document that handled `Activating` prevents `Accepting` from being raised

6. **Terminal.Gui/Views/Selectors/SelectorBase.cs** (FlagSelector/OptionSelector):
   - Verify `OnCheckboxOnActivating` correctly forwards context (including `Source`)
   - Ensure `InvokeCommand` is called with the original context, not a new one

### New Tests Required

1. **PropagatedCommands Default Behavior**:
   - Verify `Command.Accept` propagates by default
   - Verify `Command.Activate` does NOT propagate by default

2. **PropagatedCommands Opt-In**:
   - Verify setting `PropagatedCommands` enables propagation
   - Verify clearing `PropagatedCommands` disables all propagation

3. **MenuBar Integration**:
   - Verify `MenuItem` activation propagates to `MenuBar`
   - Verify `MenuBar` shows popover on `Command.Activate`
   - Verify backward compatibility with `Command.Accept`

4. **Core Functionality**:
   - Verify `Button.IsDefault` behavior works with generalized propagation
   - Verify `Dialog` accept behavior works with generalized propagation

5. **Shortcut + CheckBox Propagation**:
   - Verify CheckBox toggles when Shortcut receives Activate
   - Verify Shortcut.Activating propagates when SuperView opts-in
   - Verify Shortcut.Accepting propagates when not handled
   - Verify that if SuperView handles Activating, Accepting is NOT raised

6. **Shortcut Command Chain Behavior**:
   - Verify handled Activating prevents Accepting from being raised
   - Verify handled Accepting prevents Action from being invoked

7. **FlagSelector in MenuItem in MenuBar (Critical Scenario)**:
   - Create hierarchy: MenuBar → MenuBarItem → PopoverMenu → Menu → MenuItem → FlagSelector → CheckBox
   - **Activate propagation**: Activate CheckBox, verify MenuBar.OnActivating receives event
   - **Accept propagation**: Accept CheckBox, verify MenuBar.OnAccepting receives event
   - **SourceId/Value preservation**: Verify `ctx.SourceId` and `ctx.Value` remain constant at every level (FlagSelector, MenuItem, Menu, MenuBar)
   - **Binding preservation**: Verify `ctx.Binding` is the original KeyBinding/MouseBinding at every level
   - **Intermediate handling**: Menu.OnActivating handles → verify MenuBar.OnActivating NOT called
   - **Event interception**: Verify FlagSelector and Shortcut correctly forward (not block) commands

8. **PopoverMenu Propagation** (Special case - not normal SuperView relationship):
   - Verify Menu inside PopoverMenu can propagate to PopoverMenu
   - Verify PopoverMenu can forward propagated commands to its owning MenuBarItem
   - Verify dynamic SuperView assignment (Menu added/removed from PopoverMenu) doesn't break propagation

### Documentation Updates

1. **command.md**: Add section on command propagation
2. **events.md**: Update to reference `PropagatedCommands`
3. **View.md**: Document `PropagatedCommands` property
4. **API docs**: Ensure XML comments are complete

---

## Alternative Designs Considered

### Alternative 1: Per-Command Propagation Methods

**Design**: Add `ShouldPropagate(Command)` virtual method.

**Pros**:
- Flexible per-command control
- No property to set

**Cons**:
- Requires overriding in every view
- Less discoverable
- **Rejected**: Too verbose, violates "explicit is better than implicit"

### Alternative 2: Event-Based Propagation

**Design**: Raise a `CommandPropagating` event before propagating.

**Pros**:
- Consistent with CWP
- External subscribers can intercept

**Cons**:
- Performance overhead
- Complicates the workflow
- **Rejected**: Adds complexity without clear benefit

### Alternative 3: Attribute-Based Propagation

**Design**: Use `[Propagated]` attribute on command handlers.

**Pros**:
- Declarative
- Clear intent

**Cons**:
- Requires reflection (performance hit)
- Not idiomatic for Terminal.Gui
- **Rejected**: Performance concerns, complexity

---

## Open Questions

1. **Should `PropagatedCommands` be instance-specific or type-specific?**
   - **Decision**: Instance-specific (property, not static) for flexibility
   - **Rationale**: Different instances may need different propagation (e.g., `MenuBar` in different contexts)

2. **Should propagation be recursive or single-level?**
   - **Decision**: Recursive (current `Accept` behavior)
   - **Rationale**: Maintains backward compatibility; hierarchical views expect it

3. **Should there be a maximum propagation depth?**
   - **Decision**: No explicit limit; rely on `Handled` flag
   - **Rationale**: Avoids infinite loops via CWP; adding depth limit adds complexity

4. **Should `PropagatedCommands` be settable or collection-mutable?**
   - **Decision**: Settable (replace entire list)
   - **Rationale**: Simpler API; most use cases set once during initialization

5. **Should Activating handled by SuperView prevent Accepting on Shortcut?**
   - **Current Design**: Yes - if `RaiseActivating` returns `true`, `DispatchCommand` exits early
   - **Rationale**: Consistent with CWP "cancelable" semantics
   - **Alternative**: Could add `Activated` event (non-cancelable) like `Accepted`, allowing observation without cancellation

---

## Success Criteria

### Must Have

1. ✅ `Command.Accept` propagation via `PropagatedCommands` (generalizes existing behavior)
2. ✅ `Command.Activate` can propagate when `PropagatedCommands` includes it
3. ✅ `MenuBar` can respond to `MenuItem` activations via `Command.Activate` propagation
4. ✅ All existing tests pass (update tests that rely on removed APIs)
5. ✅ New tests cover propagation scenarios
6. ✅ `SelectedMenuItemChanged` and related workarounds removed

### Should Have

1. ⚠️ Documentation updated across all relevant files
2. ⚠️ UICatalog scenario demonstrates propagation

### Could Have

1. ⏸️ Apply propagation to other commands (e.g., `Command.HotKey`)

---

## References

### Code Files Analyzed

- `Terminal.Gui/ViewBase/View.Command.cs` (lines 1-400)
- `Terminal.Gui/Views/Shortcut.cs` (lines 1-300)
- `Terminal.Gui/Views/Menu/Menu.cs`
- `Terminal.Gui/Views/Menu/MenuBar.cs`
- `Terminal.Gui/Views/Menu/MenuBarItem.cs`
- `Terminal.Gui/Views/Menu/MenuItem.cs`
- `Terminal.Gui/Views/Menu/PopoverMenu.cs`
- `Terminal.Gui/Views/Selectors/SelectorBase.cs`
- `Terminal.Gui/Views/Selectors/FlagSelector.cs`
- `Terminal.Gui/Views/Selectors/OptionSelector.cs`
- `Terminal.Gui/Views/CheckBox.cs`
- `Terminal.Gui/Input/Command.cs`

### Documentation Reviewed

- `docfx/docs/View.md` (View Deep Dive)
- `docfx/docs/cancellable-work-pattern.md` (CWP definition)
- `docfx/docs/events.md` (Event patterns)
- `docfx/docs/command.md` (Command system overview)
- `CONTRIBUTING.md` (Contribution guidelines)

### Related Issues

- Issue #XXXXX: Menu etc. with Selectors are broken - Formalize/Fix Command propagation
- Issue #3925: (Referenced in events.md - propagation hacks)
- Issue #4050: (Referenced in events.md - PropagatedCommands proposal)

---

## Revision History

| Date       | Author          | Changes                          |
|------------|-----------------|----------------------------------|
| 2026-01-09 | GitHub Copilot  | Initial analysis document created |
| 2026-01-20 | Claude Opus 4.5 | Added "Identifying the Originating View in Handlers" section; clarified binding preservation during propagation; updated for alpha status (breaking changes OK); added binding refactor dependency |
| 2026-01-22 | Claude Opus 4.5 | Updated constraints: binding refactor is now complete; ready for implementation |
| 2026-01-22 | Claude Opus 4.5 | Added "FlagSelector in MenuItem in MenuBar Scenario" section; expanded test cases and files to modify; documented class hierarchy (MenuBar IS-A Menu, MenuBarItem IS-A MenuItem IS-A Shortcut) |
| 2026-01-21 | Claude Opus 4.5 | **Major design change**: Replaced `ctx.Source` (View?) with `ctx.SourceId` (string) and `ctx.Value` (object?) to fix lifecycle issues; added non-generic `IValue` interface; updated all handler examples to use switch/case on SourceId |
| 2026-01-21 | Claude Opus 4.5 | Fixed code style in all examples: explicit types (no `var`), `new ()` syntax, space before `()`, changed `Accepted` to `Accepting` (cancellable) |

