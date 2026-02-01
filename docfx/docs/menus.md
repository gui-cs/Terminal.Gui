# Menus Deep Dive

Terminal.Gui provides a comprehensive, hierarchical menu system built on top of the @Terminal.Gui.Shortcut and @Terminal.Gui.Bar classes. This deep dive covers the architecture, class relationships, and interactions between the menu components.

## Table of Contents

- [Overview](#overview)
- [Class Hierarchy](#class-hierarchy)
- [Component Descriptions](#component-descriptions)
- [Architecture](#architecture)
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
- `SubMenu` property holds nested @Terminal.Gui.Menu
- `TargetView` and `Command` enable command binding
- Automatically gets focus on mouse enter
- Displays right-arrow glyph when it has a submenu

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
- `PopoverMenuOpen` tracks whether the popover is visible
- `PopoverMenuOpenChanged` event fires when visibility changes

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
- `Active` property indicates whether the menu bar is active
- `IsOpen()` returns whether any popover menu is visible
- `DefaultBorderStyle` configurable via themes
- Automatically positions at top with `Width = Dim.Fill ()`

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
- Auto-positions to ensure visibility on screen
- Cascading submenus shown automatically on selection

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
│            │ owns                                                    │
│            ▼                                                         │
│  ┌──────────────────┐                                               │
│  │   PopoverMenu    │ ◄─── Registered with Application.Popover      │
│  │  ┌────────────┐  │                                               │
│  │  │    Menu    │  │ ◄─── Root Menu                                │
│  │  │ (Root)     │  │                                               │
│  │  │ ┌────────┐ │  │                                               │
│  │  │ │MenuItem│─┼──┼──► SubMenu ──► Menu ──► MenuItem ──► SubMenu  │
│  │  │ │MenuItem│ │  │                    ▲                          │
│  │  │ │  Line  │ │  │     Cascading      │                          │
│  │  │ │MenuItem│ │  │     Hierarchy ─────┘                          │
│  │  │ └────────┘ │  │                                               │
│  │  └────────────┘  │                                               │
│  └──────────────────┘                                               │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Relationships

1. **MenuBar contains MenuBarItems:**
   - `MenuBar` is a horizontal `Menu` containing `MenuBarItem` subviews
   - Each `MenuBarItem` owns a `PopoverMenu`

2. **MenuBarItem owns PopoverMenu:**
   - `MenuBarItem.PopoverMenu` property holds the dropdown
   - Events are wired up automatically for visibility and acceptance

3. **PopoverMenu contains Root Menu:**
   - `PopoverMenu.Root` is the top-level `Menu`
   - The `PopoverMenu` manages showing/hiding of cascading menus

4. **Menu contains MenuItems:**
   - `Menu.SubViews` contains `MenuItem` instances
   - `Menu.SelectedMenuItem` tracks the focused item

5. **MenuItem may contain SubMenu:**
   - `MenuItem.SubMenu` holds a nested `Menu` for cascading
   - `Menu.SuperMenuItem` links back to the parent `MenuItem`

---

## Interactions

### MenuBar Activation Flow

1. User presses `F9` (default) or clicks on `MenuBar`
2. `MenuBar.Active` is set to `true`
3. `MenuBar.CanFocus` becomes `true`
4. First `MenuBarItem` with a `PopoverMenu` is selected
5. `PopoverMenu.MakeVisible()` is called

### PopoverMenu Display Flow

1. `MakeVisible()` is called (optionally with position)
2. `SetPosition()` calculates visible location
3. `Application.Popover.Show()` is invoked
4. `OnVisibleChanged()` adds and shows the `Root` menu
5. First `MenuItem` receives focus

### Menu Selection Flow

1. User navigates with arrow keys or mouse
2. `Menu.Focused` changes to new `MenuItem`
3. `Menu.SelectedMenuItemChanged` event fires
4. If new item has `SubMenu`, `PopoverMenu.ShowSubMenu()` is called
5. Previous peer submenus are hidden

### MenuItem Acceptance Flow

1. User presses Enter or clicks on `MenuItem`
2. `MenuItem.DispatchCommand()` is called
3. If `TargetView` exists, command is invoked on target
4. Otherwise, `Action` is invoked
5. `Accepting` and `Accepted` events propagate up
6. `PopoverMenu` hides (unless item has submenu)

### Keyboard Navigation

| Key | Action |
|-----|--------|
| `F9` | Toggle MenuBar activation |
| `Shift+F10` | Show context PopoverMenu |
| `↑` / `↓` | Navigate within Menu |
| `←` / `→` | Navigate MenuBar items / Expand-collapse submenus |
| `Enter` | Accept selected MenuItem |
| `Escape` | Close menu / Deactivate MenuBar |
| Hotkey | Jump to MenuItem with matching hotkey |

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
wordWrapCheckBox.CheckedStateChanging += (s, e) =>
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
editor.MouseClick += (s, e) =>
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

### Acceptance Event Propagation

When a `MenuItem` is accepted, events propagate through the hierarchy:

```
MenuItem.Accepting → MenuItem.Accepted
        ↓                    ↓
Menu.Accepting    →    Menu.Accepted
        ↓                    ↓
PopoverMenu.Accepting → PopoverMenu.Accepted
        ↓                    ↓
MenuBarItem.Accepting → MenuBarItem.Accepted
        ↓                    ↓
MenuBar.Accepting  →  MenuBar.Accepted
```

### Selection Change Events

```
User navigates → Menu.Focused changes
                        ↓
              Menu.OnFocusedChanged ()
                        ↓
              SelectedMenuItem updated
                        ↓
              SelectedMenuItemChanged event
                        ↓
              PopoverMenu shows/hides submenus
```

### Key Binding Resolution

1. Check `KeyBindings` on focused `MenuItem`
2. Check `HotKeyBindings` on `Menu`
3. Check `KeyBindings` on `PopoverMenu`
4. Check `KeyBindings` on `MenuBar`
5. Check `Application.Keyboard.KeyBindings`

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
- [Command Deep Dive](command.md) - Command binding and dispatch
- [Keyboard Deep Dive](keyboard.md) - Key binding system
- [Events Deep Dive](events.md) - Event handling patterns
- @Terminal.Gui.MenuBar API Reference
- @Terminal.Gui.PopoverMenu API Reference
- @Terminal.Gui.MenuItem API Reference
