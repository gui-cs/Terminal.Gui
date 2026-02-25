# Menus Deep Dive

Terminal.Gui provides a comprehensive, hierarchical menu system built on top of the @Terminal.Gui.Shortcut and @Terminal.Gui.Bar classes. This deep dive covers the architecture, class relationships, command routing, and interactions between the menu components.

## Table of Contents

- [Overview](#overview)
- [Class Hierarchy](#class-hierarchy)
- [Component Descriptions](#component-descriptions)
- [Architecture](#architecture)
- [Command Routing](#command-routing)
- [Interactions](#interactions)
- [Usage Examples](#usage-examples)
- [Keyboard Navigation](#keyboard-navigation)
- [Event Flow](#event-flow)

---

## Overview

The menu system in Terminal.Gui consists of the following key components:

| Component | Description |
|-----------|-------------|
| @Terminal.Gui.Shortcut | Base class for displaying a command, help text, and key binding |
| @Terminal.Gui.Bar | Container for `Shortcut` items, supports horizontal/vertical orientation |
| @Terminal.Gui.MenuItem | A `Shortcut`-derived item for use in menus, supports submenus |
| @Terminal.Gui.Menu | A vertically-oriented `Bar` that contains `MenuItem` items |
| @Terminal.Gui.MenuBarItem | A `MenuItem` that holds a `PopoverMenu` instead of a `SubMenu` |
| @Terminal.Gui.MenuBar | A horizontal `Menu` that contains `MenuBarItem` items |
| @Terminal.Gui.PopoverMenu | A `PopoverBaseImpl`-derived view that hosts cascading menus |

---

## Class Hierarchy

The menu system builds upon a layered class hierarchy:

```
View
├── Shortcut                    // Command + HelpText + Key display
│   └── MenuItem                // Menu-specific Shortcut with SubMenu support
│       └── MenuBarItem         // MenuItem that uses PopoverMenu instead of SubMenu
│
├── Bar                         // Container for Shortcuts (horizontal/vertical)
│   └── Menu                    // Vertical Bar for MenuItems
│       └── MenuBar             // Horizontal Menu for MenuBarItems
│
└── PopoverBaseImpl             // Base for popover views
    └── PopoverMenu             // Cascading menu popover
```

### Inheritance Details

**Shortcut → MenuItem → MenuBarItem:**
- `Shortcut` displays command text, help text, and a key binding
- `MenuItem` extends `Shortcut` to add `SubMenu` support for nested menus
- `MenuBarItem` extends `MenuItem` but replaces `SubMenu` with `PopoverMenu`

**Bar → Menu → MenuBar:**
- `Bar` is a generic container for `Shortcut` views with orientation support
- `Menu` is a vertical `Bar` specialized for `MenuItem` items
- `MenuBar` is a horizontal `Menu` specialized for `MenuBarItem` items

For completeness, here's how `StatusBar` fits in:

**Bar → StatusBar:**
- `StatusBar` is a horizontal `Bar` specialized for `Shortcuts` items

---

## Component Descriptions

### Shortcut

@Terminal.Gui.Shortcut is the foundational building block. It displays three elements:

1. **CommandView** - The command text (left side by default)
2. **HelpView** - Help text (middle)
3. **KeyView** - Key binding display (right side)

```csharp
Shortcut shortcut = new ()
{
    Title = "_Save",           // CommandView text with hotkey
    HelpText = "Save the file",
    Key = Key.S.WithCtrl,
    Action = () => SaveFile ()
};
```

**IMPORTANT:** The `CommandView`, `HelpView`, and `KeyView` are subviews of the shortcut. But how they are managed is an implementation detail and `shortcut.SubViews` should not be used to try to access them.

Key features:
- Supports `Action` for direct invocation
- `BindKeyToApplication` enables application-wide key bindings
- `AlignmentModes` controls element ordering (start-to-end or end-to-start)
- `CommandView` can be replaced with custom views (e.g., `CheckBox`)
- Uses relay dispatch (`ConsumeDispatch=false`): commands dispatched to `CommandView` complete normally, then `Shortcut` is notified via a deferred callback
- Sets `CommandsToBubbleUp = [Command.Activate, Command.Accept]`

### Bar

@Terminal.Gui.Bar is a container that arranges `Shortcut` items either horizontally or vertically:

```csharp
Bar statusBar = new ()
{
    Orientation = Orientation.Horizontal,
    Y = Pos.AnchorEnd ()
};

statusBar.Add (new Shortcut { Title = "_Help", Key = Key.F1 });
statusBar.Add (new Shortcut { Title = "_Quit", Key = Key.Q.WithCtrl });
```

Key features:
- `Orientation` property controls layout direction
- `AlignmentModes` property controls item alignment
- Supports mouse wheel navigation
- Auto-sizes based on content (`Dim.Auto`)

### MenuItem

@Terminal.Gui.MenuItem extends `Shortcut` for use in menus:

```csharp
MenuItem menuItem = new ()
{
    Title = "_Open...",
    HelpText = "Open a file",
    Key = Key.O.WithCtrl,
    Action = () => OpenFile ()
};

// Or bind to a command on a target view
MenuItem boundItem = new (myView, Command.Save);
```

Key features:
- `SubMenu` property holds nested @Terminal.Gui.Menu for cascading menus
- `TargetView` and `Command` enable command binding to other views
- Automatically gets focus on mouse enter
- Displays right-arrow glyph when it has a submenu
- When `SubMenu` is set, a @Terminal.Gui.Input.CommandBridge connects the SubMenu back to this MenuItem (bridging `Activate` and `Accept` commands across the non-containment boundary)

### Menu

@Terminal.Gui.Menu is a vertical `Bar` specialized for menu items:

```csharp
Menu fileMenu = new ([
    new MenuItem ("_New", Key.N.WithCtrl, () => NewFile ()),
    new MenuItem ("_Open...", Key.O.WithCtrl, () => OpenFile ()),
    new Line (),  // Separator
    new MenuItem ("_Save", Key.S.WithCtrl, () => SaveFile ()),
    new MenuItem ("Save _As...", () => SaveAs ())
]);
```

Key features:
- Vertical orientation by default
- `SuperMenuItem` property links back to parent `MenuItem`
- `SelectedMenuItem` tracks current selection
- Supports `Line` separators between items
- Uses `Schemes.Menu` color scheme by default
- Sets `CommandsToBubbleUp = [Command.Accept, Command.Activate]` — enables command propagation up through the PopoverMenu hierarchy
- Overrides `OnActivating` to dispatch `Activate` to the focused `MenuItem` (manual dispatch, not the `GetDispatchTarget` pattern)
- `ShowMenu()` / `HideMenu()` control visibility and handle initialization; `HideMenu` cascades to visible SubMenus
- `GetAllSubMenus()` performs depth-first traversal of the SubMenu hierarchy
- `GetMenuItemsOfAllSubMenus()` collects all `MenuItem`s across the hierarchy, with optional predicate
- `OnSelectedMenuItemChanged()` handles SubMenu display: hides peer SubMenus, shows the selected item's SubMenu, and performs basic positioning

### MenuBarItem

@Terminal.Gui.MenuBarItem extends `MenuItem` for use in @Terminal.Gui.MenuBar:

```csharp
MenuBarItem fileMenuBarItem = new ("_File", [
    new MenuItem ("_New", Key.N.WithCtrl, () => NewFile ()),
    new MenuItem ("_Open...", Key.O.WithCtrl, () => OpenFile ()),
    new Line (),
    new MenuItem ("_Quit", Application.QuitKey, () => Application.RequestStop ())
]);
```

**Important:** `MenuBarItem` uses `PopoverMenu` instead of `SubMenu`. Attempting to set `SubMenu` will throw `InvalidOperationException`.

Key features:
- `PopoverMenu` property holds the dropdown menu
- `PopoverMenuOpen` tracks whether the popover is visible (CWP property with `PopoverMenuOpenChanging`/`PopoverMenuOpenChanged` events)
- When `PopoverMenu` is set, a @Terminal.Gui.Input.CommandBridge connects the PopoverMenu back to this MenuBarItem, bridging `Activate` commands across the non-containment boundary
- Overrides `OnActivating` to toggle `PopoverMenuOpen`, with a guard that ignores `Bridged` commands (which are notifications from PopoverMenu internals, not toggle requests)
- Has a custom `HotKey` handler that skips `SetFocus` before invoking `Activate`, preventing premature popover opening during MenuBarItem switching

### MenuBar

@Terminal.Gui.MenuBar is a horizontal menu bar typically placed at the top of a window:

```csharp
MenuBar menuBar = new ([
    new MenuBarItem ("_File", [
        new MenuItem ("_New", Key.N.WithCtrl, () => NewFile ()),
        new MenuItem ("_Open...", Key.O.WithCtrl, () => OpenFile ()),
        new Line (),
        new MenuItem ("E_xit", Application.QuitKey, () => Application.RequestStop ())
    ]),
    new MenuBarItem ("_Edit", [
        new MenuItem ("_Cut", Key.X.WithCtrl, () => Cut ()),
        new MenuItem ("_Copy", Key.C.WithCtrl, () => Copy ()),
        new MenuItem ("_Paste", Key.V.WithCtrl, () => Paste ())
    ]),
    new MenuBarItem ("_Help", [
        new MenuItem ("_About...", () => ShowAbout ())
    ])
]);

// Add to window
window.Add (menuBar);
```

Key features:
- `Key` property defines the activation key (default: `F9`)
- `Active` property controls whether the MenuBar is active — when `Active` changes, it drives `CanFocus` and hides any open PopoverMenus on deactivation
- `IsOpen()` returns whether any popover menu is visible
- `DefaultBorderStyle` configurable via themes
- Automatically positions at top with `Width = Dim.Fill ()`
- Uses consume dispatch (`ConsumeDispatch=true`, `GetDispatchTarget => Focused`) — the MenuBar owns activation state for its MenuBarItems
- Blocks activation when `!Visible || !Enabled`
- Registers custom command handlers for `Command.HotKey`, `Command.Quit`, `Command.Right`, and `Command.Left`

### PopoverMenu

@Terminal.Gui.PopoverMenu is a popover that hosts cascading menus:

```csharp
// Create a context menu
PopoverMenu contextMenu = new ([
    new MenuItem (targetView, Command.Cut),
    new MenuItem (targetView, Command.Copy),
    new MenuItem (targetView, Command.Paste),
    new Line (),
    new MenuItem (targetView, Command.SelectAll)
]);

// Register with application (required!)
Application.Popover?.Register (contextMenu);

// Show at mouse position
contextMenu.MakeVisible ();

// Or show at specific position
contextMenu.MakeVisible (new Point (10, 5));
```

Key features:
- `Root` property holds the top-level @Terminal.Gui.Menu
- `Key` property for activation (default: `Shift+F10`)
- `MouseFlags` property defines mouse button to show menu (default: right-click)
- **Must be registered** with `Application.Popover` before calling `MakeVisible`
- Auto-positions to ensure visibility on screen
- SubMenu show/hide is handled by `Menu.OnSelectedMenuItemChanged()`; PopoverMenu's subscriber only adjusts positioning for screen boundaries via `GetMostVisibleLocationForSubMenu()`
- Registers custom command handlers for `Command.Right` (enter submenu), `Command.Left` (leave submenu), and `Command.Quit` (close menu)

**Important:** See the [Popovers Deep Dive](Popovers.md) for complete details on popover lifecycle and requirements.

---

## Architecture

### Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                              Window                                  │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                           MenuBar                              │  │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐            │  │
│  │  │ MenuBarItem │  │ MenuBarItem │  │ MenuBarItem │   ...      │  │
│  │  │   "File"    │  │   "Edit"    │  │   "Help"    │            │  │
│  │  └──────┬──────┘  └─────────────┘  └─────────────┘            │  │
│  └─────────│─────────────────────────────────────────────────────┘  │
│            │                                                         │
│            │ owns (+ CommandBridge)                                   │
│            ▼                                                         │
│  ┌──────────────────┐                                               │
│  │   PopoverMenu    │ ◄─── Registered with Application.Popover      │
│  │  ┌────────────┐  │                                               │
│  │  │    Menu    │  │ ◄─── Root Menu                                │
│  │  │ (Root)     │  │                                               │
│  │  │ ┌────────┐ │  │                                               │
│  │  │ │MenuItem│─┼──┼──► SubMenu ──► Menu ──► MenuItem ──► SubMenu  │
│  │  │ │MenuItem│ │  │         ▲  (CommandBridge)                    │
│  │  │ │  Line  │ │  │         │                                     │
│  │  │ │MenuItem│ │  │     Cascading                                 │
│  │  │ └────────┘ │  │     Hierarchy                                 │
│  │  └────────────┘  │                                               │
│  └──────────────────┘                                               │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Relationships

1. **MenuBar contains MenuBarItems:**
   - `MenuBar` is a horizontal `Menu` containing `MenuBarItem` subviews
   - Each `MenuBarItem` owns a `PopoverMenu`

2. **MenuBarItem owns PopoverMenu (cross-boundary):**
   - `MenuBarItem.PopoverMenu` property holds the dropdown
   - A `CommandBridge` connects PopoverMenu back to MenuBarItem, bridging `Activate` commands
   - `PopoverMenuOpenChanged` event fires when visibility changes

3. **PopoverMenu contains Root Menu:**
   - `PopoverMenu.Root` is the top-level `Menu`
   - `Menu` self-manages SubMenu show/hide and basic positioning via `OnSelectedMenuItemChanged()`
   - `PopoverMenu` adjusts positioning for screen boundaries and manages its own visibility lifecycle

4. **Menu contains MenuItems:**
   - `Menu.SubViews` contains `MenuItem` instances
   - `Menu.SelectedMenuItem` tracks the focused item
   - `Menu.CommandsToBubbleUp = [Command.Accept, Command.Activate]` enables propagation

5. **MenuItem may contain SubMenu (cross-boundary):**
   - `MenuItem.SubMenu` holds a nested `Menu` for cascading
   - `Menu.SuperMenuItem` links back to the parent `MenuItem`
   - A `CommandBridge` connects SubMenu back to its MenuItem, bridging `Activate` and `Accept`

---

## Command Routing

The menu system uses the [Command Deep Dive](command.md) infrastructure extensively. Understanding command routing is essential for working with menus.

### Dispatch Patterns

Each menu component uses a specific command dispatch pattern:

| Component | `GetDispatchTarget` | `ConsumeDispatch` | Pattern |
|-----------|--------------------|--------------------|---------|
| **MenuBar** | `Focused` (the focused MenuBarItem) | `true` | Consume: MenuBar owns activation state |
| **Menu** | Not overridden (manual dispatch in `OnActivating`) | Not overridden | Dispatches `Activate` to focused MenuItem directly |
| **MenuItem** | Inherited from Shortcut (`CommandView`) | `false` | Relay: CommandView completes normally, then MenuItem is notified |
| **MenuBarItem** | Inherited from MenuItem/Shortcut | `false` | Relay (inherited), plus custom `OnActivating` for PopoverMenu toggle |

### CommandBridge (Cross-Boundary Routing)

The menu system has two non-containment boundaries that require @Terminal.Gui.Input.CommandBridge:

1. **MenuBarItem ↔ PopoverMenu:** `PopoverMenu` is not a SubView of `MenuBarItem` — it is registered with `Application.Popover` and lives outside the view hierarchy. The bridge brings `Activate` events from the PopoverMenu back to the MenuBarItem so they can bubble up through the MenuBar.

2. **MenuItem ↔ SubMenu:** `SubMenu` is not a SubView of `MenuItem` — it is managed by the PopoverMenu's cascading infrastructure. The bridge brings `Activate` and `Accept` events from the SubMenu back to the MenuItem so they can bubble up through the Menu.

### Routing Modes in Menu Context

| Mode | When It Occurs | Effect |
|------|---------------|--------|
| **Direct** | User presses `F9`, or programmatic `InvokeCommand` | MenuBar toggles `Active` on/off |
| **BubblingUp** | MenuBarItem activation bubbles to MenuBar | MenuBar identifies the source MenuBarItem and shows/hides its PopoverMenu |
| **Bridged** | MenuItem activation inside PopoverMenu bridges to MenuBarItem | MenuBarItem ignores the command (notification only — no PopoverMenu toggle) |

### Command Bubbling

`Menu` sets `CommandsToBubbleUp = [Command.Accept, Command.Activate]`. This means:

1. When a `MenuItem` fires `Accept` or `Activate`, the command bubbles up through the Menu to its SuperView
2. For Root Menus inside a `PopoverMenu`, the command reaches the PopoverMenu
3. The `CommandBridge` on MenuBarItem detects the `Activate` event on PopoverMenu and relays it to MenuBarItem
4. The MenuBarItem's activation then bubbles to the MenuBar via normal SuperView bubbling

---

## Interactions

### MenuBar Activation Flow

1. User presses `F9` (default) or clicks on `MenuBar`
2. MenuBar's HotKey handler fires — for direct activation, this calls `InvokeCommand (Command.Activate)`
3. `MenuBar.OnActivating` runs:
   - If `!Visible || !Enabled`: activation is blocked
   - If already `Active`: toggles off (`Active = false`)
   - Otherwise: sets `Active = true` and calls `ShowItem` on the first MenuBarItem with a PopoverMenu
4. `Active = true` sets `CanFocus = true`
5. `ShowItem` focuses the MenuBarItem and sets `PopoverMenuOpen = true`
6. `PopoverMenuOpen` setter calls `PopoverMenu.MakeVisible` with the calculated screen position

### MenuBarItem HotKey Activation

When a MenuBarItem's HotKey (e.g., `Alt+F` for "\_File") is pressed:

1. The HotKey is processed on the MenuBarItem, which has a custom HotKey handler
2. The handler skips `SetFocus` (to prevent premature popover opening) and directly invokes `Command.Activate`
3. `MenuBarItem.OnActivating` toggles `PopoverMenuOpen`
4. The activation bubbles up to `MenuBar.OnActivating` with `BubblingUp` routing
5. MenuBar identifies the source MenuBarItem and either:
   - Activates the MenuBar and shows the source item's PopoverMenu (if opening)
   - Deactivates the MenuBar (if the PopoverMenu is closing)

### Switching Between MenuBarItems

When the MenuBar is active and a PopoverMenu is open:

- **Arrow keys:** `Command.Right`/`Command.Left` advance focus to the next/previous MenuBarItem. The `OnSelectedMenuItemChanged` callback detects the focus change and, while in _popover browsing mode_, calls `ShowItem` on the newly focused item.
- **Mouse hover:** Moving the mouse over a different MenuBarItem triggers `OnMouseEnter`, which sets focus. If in browsing mode, the new item's PopoverMenu opens automatically.
- **HotKey:** Pressing another MenuBarItem's HotKey directly invokes `Activate` on that item, causing a switch.

The `_isSwitchingItem` guard prevents premature deactivation during the brief interval when the old popover closes before the new one opens. The `_popoverBrowsingMode` flag tracks whether any popover is open, enabling auto-open behavior during navigation.

### PopoverMenu Display Flow

1. `MakeVisible()` is called (optionally with a position)
2. `SetPosition()` calculates a visible location on screen
3. `Application.Popover.Show()` is invoked
4. `OnVisibleChanged()` adds and lays out the `Root` menu
5. First `MenuItem` receives focus

**Prerequisite:** The `PopoverMenu` must be registered with `Application.Popover` before `MakeVisible` is called. For `MenuBarItem`, registration happens automatically in `EndInit`. For standalone context menus, call `Application.Popover?.Register (contextMenu)` explicitly.

### Menu Selection Flow

1. User navigates with arrow keys or mouse
2. `Menu.Focused` changes to new `MenuItem`
3. `Menu.OnSelectedMenuItemChanged()` runs: hides peer SubMenus, shows selected item's SubMenu with basic positioning
4. `Menu.SelectedMenuItemChanged` event fires
5. When inside a `PopoverMenu`, the subscriber adjusts SubMenu positioning for screen boundaries

### MenuItem Acceptance Flow

When a user presses Enter or clicks a `MenuItem`:

1. `Command.Accept` is invoked on the focused `MenuItem`
2. `MenuItem.RaiseAccepting` fires the cancellable `Accepting` event
3. If not cancelled, `Shortcut.OnAccepted` runs:
   - If `TargetView` and `Command` are set: invokes the command on the target view
   - If `Action` is set: invokes the action
4. `Accepted` fires on the MenuItem
5. Because `Menu.CommandsToBubbleUp` includes `Accept`, the command bubbles up:
   - MenuItem → Menu → PopoverMenu
6. PopoverMenu hides (closes) in response to the accepted command
7. The `CommandBridge` on MenuBarItem brings the event into the containment hierarchy
8. MenuBar deactivates

### Keyboard Navigation

| Key | Action |
|-----|--------|
| `F9` | Toggle MenuBar activation |
| `Shift+F10` | Show context PopoverMenu |
| `↑` / `↓` | Navigate within Menu |
| `←` / `→` | Navigate MenuBar items / Expand-collapse submenus |
| `Enter` | Accept selected MenuItem |
| `Space` | Activate selected MenuItem (e.g., toggle a CheckBox CommandView) |
| `Escape` / `QuitKey` | Close menu / Deactivate MenuBar |
| Hotkey (e.g., `Alt+F`) | Activate/toggle specific MenuBarItem |
| Hotkey in Menu | Jump to MenuItem with matching hotkey |

---

## Usage Examples

### Basic MenuBar

```csharp
using Terminal.Gui;

Application.Init ();

Window mainWindow = new () { Title = "Menu Demo" };

MenuBar menuBar = new ([
    new MenuBarItem ("_File", [
        new MenuItem ("_New", "", () => MessageBox.Query ("New", "Create new file?", "OK", "Cancel")),
        new MenuItem ("_Open...", "", () => MessageBox.Query ("Open", "Open file dialog", "OK")),
        new Line (),
        new MenuItem ("E_xit", Application.QuitKey, () => Application.RequestStop ())
    ]),
    new MenuBarItem ("_Edit", [
        new MenuItem ("_Undo", Key.Z.WithCtrl, () => { }),
        new Line (),
        new MenuItem ("Cu_t", Key.X.WithCtrl, () => { }),
        new MenuItem ("_Copy", Key.C.WithCtrl, () => { }),
        new MenuItem ("_Paste", Key.V.WithCtrl, () => { })
    ])
]);

mainWindow.Add (menuBar);

Application.Run (mainWindow);
Application.Shutdown ();
```

### Nested Submenus

```csharp
MenuBarItem optionsMenu = new ("_Options", [
    new MenuItem
    {
        Title = "_Preferences",
        SubMenu = new Menu ([
            new MenuItem { Title = "_General", Action = () => ShowGeneralPrefs () },
            new MenuItem { Title = "_Editor", Action = () => ShowEditorPrefs () },
            new MenuItem
            {
                Title = "_Advanced",
                SubMenu = new Menu ([
                    new MenuItem { Title = "_Debug Mode", Action = () => ToggleDebug () },
                    new MenuItem { Title = "_Experimental", Action = () => ToggleExperimental () }
                ])
            }
        ])
    },
    new Line (),
    new MenuItem { Title = "_Reset to Defaults", Action = () => ResetDefaults () }
]);
```

### Command Binding

```csharp
// Bind menu items to commands on a target view
TextView editor = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill () };

MenuBar menuBar = new ([
    new MenuBarItem ("_Edit", [
        new MenuItem (editor, Command.Cut),      // Uses editor's Cut command
        new MenuItem (editor, Command.Copy),     // Uses editor's Copy command
        new MenuItem (editor, Command.Paste),    // Uses editor's Paste command
        new Line (),
        new MenuItem (editor, Command.SelectAll)
    ])
]);
```

### CheckBox in Menu

```csharp
CheckBox wordWrapCheckBox = new () { Title = "_Word Wrap" };
wordWrapCheckBox.CheckedStateChanging += (_, e) =>
{
    editor.WordWrap = e.NewValue == CheckState.Checked;
};

MenuBarItem viewMenu = new ("_View", [
    new MenuItem { CommandView = wordWrapCheckBox },
    new MenuItem
    {
        CommandView = new CheckBox { Title = "_Line Numbers" },
        Key = Key.L.WithCtrl
    }
]);
```

### Context Menu (PopoverMenu)

```csharp
PopoverMenu contextMenu = new ([
    new MenuItem (editor, Command.Cut),
    new MenuItem (editor, Command.Copy),
    new MenuItem (editor, Command.Paste),
    new Line (),
    new MenuItem { Title = "_Properties...", Action = () => ShowProperties () }
]);

Application.Popover?.Register (contextMenu);

// Show on right-click
editor.MouseClick += (_, e) =>
{
    if (e.Flags.HasFlag (MouseFlags.RightButtonClicked))
    {
        contextMenu.MakeVisible (e.ScreenPosition);
        e.Handled = true;
    }
};
```

---

## Event Flow

### Command Propagation Through the Menu Hierarchy

When a `MenuItem` is activated or accepted, commands propagate through the hierarchy using two mechanisms: **command bubbling** (within the containment hierarchy) and **CommandBridge** (across non-containment boundaries).

#### Accept Flow (e.g., user presses Enter on a MenuItem)

```
MenuItem
  ├─ Accepting event (cancellable)
  ├─ Accepted event
  ↓ (bubbles via CommandsToBubbleUp)
Menu (Root)
  ├─ Accepting event
  ├─ Accepted event
  ↓ (PopoverMenu receives via containment)
PopoverMenu
  ├─ Closes (Visible = false)
  ↓ (CommandBridge bridges to MenuBarItem)
MenuBarItem
  ├─ PopoverMenuOpen = false (via VisibleChanged)
  ↓ (bubbles to SuperView)
MenuBar
  ├─ Active = false
  └─ Deactivates
```

#### Activate Flow (e.g., user presses Space to toggle a CheckBox in a MenuItem)

```
MenuItem's CommandView (e.g., CheckBox)
  ├─ Activating / Activated events
  ↓ (relay dispatch from Shortcut)
MenuItem
  ├─ Activating / Activated events
  ↓ (bubbles via CommandsToBubbleUp)
Menu (Root)
  ↓ (CommandBridge bridges to MenuBarItem)
MenuBarItem
  ├─ OnActivating sees Bridged routing → ignores (no toggle)
  ↓ (bubbles to SuperView)
MenuBar
  ├─ OnActivating sees BubblingUp routing → notification only
```

### Selection Change Events

```
User navigates → Menu.Focused changes
                        ↓
              Menu.OnFocusedChanged ()
                        ↓
              SelectedMenuItem updated
                        ↓
              Menu.OnSelectedMenuItemChanged ()
                ├─ Hides peer SubMenus
                ├─ Shows selected SubMenu (with basic positioning)
                        ↓
              SelectedMenuItemChanged event
                        ↓
              PopoverMenu adjusts SubMenu positioning (screen boundaries)
```

### Key Event Processing

Key events are processed depth-first through the view hierarchy:

```
NewKeyDownEvent (key)
  ├─ If has Focused SubView → recurse into Focused
  ├─ RaiseKeyDown (key) — OnKeyDown + KeyDown event
  ├─ InvokeCommandsBoundToKey (key) — KeyBindings lookup
  ├─ InvokeCommandsBoundToHotKey (key) — HotKeyBindings (this + SubViews)
  └─ RaiseKeyDownNotHandled (key) — OnKeyDownNotHandled + event
```

For menus specifically:
- `MenuBar` binds `F9` to `Command.HotKey` (via `HotKeyBindings`)
- `MenuBar` binds `F9` and `Application.QuitKey` to `Command.Quit` (via `KeyBindings`)
- `MenuBar` binds arrow keys to `Command.Right`/`Command.Left`
- `PopoverMenu` binds arrow keys to `Command.Right`/`Command.Left` for submenu navigation
- `PopoverMenu` binds `Escape`/`QuitKey` to `Command.Quit`

---

## Configuration

Menu appearance can be customized via themes:

```csharp
// Set default border style for menus
Menu.DefaultBorderStyle = LineStyle.Single;

// Set default border style for menu bars
MenuBar.DefaultBorderStyle = LineStyle.None;

// Set default activation key for menu bars
MenuBar.DefaultKey = Key.F10;

// Set default activation key for popover menus
PopoverMenu.DefaultKey = Key.F10.WithShift;
```

These can also be configured in `config.json`:

```json
{
  "Themes": {
    "Default": {
      "Menu.DefaultBorderStyle": "Single",
      "MenuBar.DefaultBorderStyle": "None"
    }
  },
  "Settings": {
    "MenuBar.DefaultKey": "F9",
    "PopoverMenu.DefaultKey": "Shift+F10"
  }
}
```

---

## See Also

- [Popovers Deep Dive](Popovers.md) - Complete details on popover lifecycle
- [Command Deep Dive](command.md) - Command binding, dispatch, and routing
- [Keyboard Deep Dive](keyboard.md) - Key binding system
- [Events Deep Dive](events.md) - Event handling patterns
- @Terminal.Gui.MenuBar API Reference
- @Terminal.Gui.PopoverMenu API Reference
- @Terminal.Gui.MenuItem API Reference
