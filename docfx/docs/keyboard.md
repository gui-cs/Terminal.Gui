# Keyboard Event Processing

## Tenets for Terminal.Gui Key Bindings (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Users Have Control** - *Terminal.Gui* provides default key bindings consistent with these tenets, but those defaults are configurable by the user. For example, `ConfigurationManager` allows users to redefine key bindings for the system, a user, or an application.

* **More Editor than Command Line** - Once a *Terminal.Gui* app starts, the user is no longer using the command line. Users expect keyboard idioms in TUI apps to be consistent with GUI apps (such as VS Code, Vim, and Emacs). For example, in almost all GUI apps, `Ctrl-V` is `Paste`. But the Linux shells often use `Shift-Insert`. *Terminal.Gui* binds `Ctrl-V` by default.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to keyboard input in a way that is consistent with the platform. For example, on Windows to erase a word to the left, users press `Ctrl-Backspace`. But on Linux, `Ctrl-W` is used.

* **The Source of Truth is Wikipedia** - We use this [Wikipedia article](https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts) as our guide for default key bindings.

## Keyboard APIs

*Terminal.Gui* provides the following APIs for handling keyboard input:

### **[Key Bindings](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_AddKeyBinding_Terminal_Gui_Key_Terminal_Gui_Command___)**

The default key for activating a button is `Space`. You can change this using the 
`ClearKeybinding` and `AddKeybinding` methods:

```csharp
var btn = new Button ("Press Me");
btn.ClearKeybinding (Command.Accept);
btn.AddKeyBinding (Key.B, Command.Accept);
```

The [Command](~/api/Terminal.Gui.Command.yml) enum lists generic operations that are implemented by views. For example `Command.Accept` in a `Button` results in the `Clicked` event 
firing while in `TableView` it is bound to `CellActivated`. Not all commands
are implemented by all views (e.g. you cannot scroll in a `Button`). Use the `GetSupportedCommands()`
method to determine which commands are implemented by a `View`. 

### **[HotKey](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_HotKey)** 

A **HotKey** is a keypress that selects a visible UI item. For selecting items across `View`s (e.g. a `Button` in a `Dialog`) the keypress must have the `Alt` modifier. For selecting items within a `View` that are not `View`s themselves, the keypress can be key without the `Alt` modifier.  For example, in a `Dialog`, a `Button` with the text of "_Text" can be selected with `Alt-T`. Or, in a `Menu` with "_File _Edit", `Alt-F` will select (show) the "_File" menu. If the "_File" menu has a sub-menu of "_New" `Alt-N` or `N` will ONLY select the "_New" sub-menu if the "_File" menu is already opened.

By default, the `Text` of a `View` is used to determine the `HotKey` by looking for the first occurrence of the [HotKeySpecifier](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_HotKeySpecifier) (which is underscore (`_`) by default). The character following the underscore is the `HotKey`. If the `HotKeySpecifier` is not found in `Text`, the first character of `Text` is used as the `HotKey`. The `Text` of a `View` can be changed at runtime, and the `HotKey` will be updated accordingly. [HotKey](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_HotKey) is `virtual` enabling this behavior to be customized.

### **[Shortcut](~/api/Terminal.Gui.MenuItem.yml#Terminal_Gui_MenuItem_Shortcut)** 

A **Shortcut** is a keypress that invokes a [Command](~/api/Terminal.Gui.Command.yml) or `View`-defined action regardless of whether the `View` that defines them is visible (but the `View` must be enabled). Shortcuts can be any keypress; `Key.A`, `Key.A | Key.Ctrl`, `Key.A | Key.Ctrl | Key.Alt`, `Key.Del`, and `Key.F1`, are all valid. 

`Shortcuts` are used to define application-wide actions (e.g. `Quit`), or actions that are not visible (e.g. `Copy`).

Not all `Views` support `Shortcut`s. [MenuBar](~/api/Terminal.Gui.MenuBar.yml), [ContextMenu](~/api/Terminal.Gui.ContextMenu.yml), and [StatusBar](~/api/Terminal.Gui.StatusBar.yml) support `Shortcut`s. However, the `Button` class does not. 

The `Shortcut` is provided by setting the [Shortcut](~/api/Terminal.Gui.MenuItem.yml#Terminal_Gui_MenuItem_Shortcut) property on either a [MenuItem](~/api/Terminal.Gui.MenuItem.yml) or [StatusItem](~/api/Terminal.Gui.StatusItem.yml). 

The [ShortcutDelimiter](~/api/Terminal.Gui.MenuBar.yml#Terminal_Gui_MenuBar_ShortcutDelimiter) (`+` by default) is used to separate the `Shortcut` from the `Text` of the `MenuItem` or `StatusItem`. For example, the `Shortcut` for `Quit` is `Ctrl+Q` and the `Text` is `Quit`. 

### **[Handling Keyboard Events](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_KeyDown)**

Keyboard events are retrieved from [Console Drivers](drivers.md) and passed on 
to the [Application](~/api/Terminal.Gui.Application.yml) class by the [Main Loop](mainloop.md). 

[Application](~/api/Terminal.Gui.Application.yml) then determines the current [Toplevel](~/api/Terminal.Gui.Toplevel.yml) view
(either the default created by calling `Application.Init`, or the one set by calling `Application.Run`). The mouse event, using [Bounds-relative coordinates](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_Bounds) is then passed to the [ProcessKeyPress](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_ProcessKeyPress_Terminal_Gui_KeyEventArgs_) method of the current [Toplevel](~/api/Terminal.Gui.Toplevel.yml) view. 

If the view is enabled [ProcessKeyDown](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_ProcessKeyDown_Terminal_Gui_KeyEventArgs_) the following will happen: 

1) If the view has a subview that has focus, 'ProcessKeyDown' on the focused view will be called. If the focused view handles the keypress, processing stops.
2) If there is no focused sub-view, or the focused sub-view does not handle the keypress, [OnKeyDown](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnKeyDown_Terminal_Gui_KeyEventArgs_) will be called. If the view handles the keypress, processing stops.
3) If the view does not handle the keypress, [OnInvokingKeyBindings](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnInvokingKeyBindings_Terminal_Gui_KeyEventArgs_) will be called. This method calls[InvokeKeyBindings](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_InvokeKeyBindings_Terminal_Gui_KeyEventArgs_) to invoke any keys bound to commands. If the key is bound and any of it's command handlers return true, processing stops.

## **[Global Key Handling](~/api/Terminal.Gui.Application.yml#Terminal_Gui_Application_KeyPress)**

To define global key handling logic for an entire application in cases where the methods listed above are not suitable, use the `Application.KeyPress` event. 


## **[Key Down/Up Events](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_KeyDown)**

*Terminal.Gui* supports key up/down events with [OnKeyDown](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnKeyDown_Terminal_Gui_KeyEventArgs_) and [OnKeyUp](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnKeyUp_Terminal_Gui_KeyEventArgs_), but not all [Console Drivers](drivers.md) do. To receive these key down and key up events, you must use a driver that supports them (e.g. `WindowsDriver`).

# General input model

- Key Down and Up events are generated by `ConsoleDriver`. 
- `Application` subscribes to `ConsoleDriver.Down/Up` events and forwards them to the most-focused `TopLevel` view. 
- The base (`View`) implementation of `virtual bool OnKeyDown`:
  - We firm up the definition of `View.Enabled` - if `Enabled == false` that view should *never* see keyboard (or mouse input) 
    - (this is not the case today; `Application.OnKeyPress` forwards the event to ALL `TopLevels`.
  - Invokes `KeyPress`. If `Handled == true` on return, the method returns.
  - Assuming the key wasn't handled by the event handler, the most-focused View is identified and `OnKeyPress` is called on it.
  - Assuming THAT call returns false (indicating the key wasn't handled)
     - `InvokeKeyBindings` is called to invoke any bound commands.

- Subclasses of `View` can (rarely) override `OnKeyPress` 
  - If they do override, they MUST call `base.OnKeyPress` and honor the result before returning. If they don't keybindings won't work and `KeyPress` events won't fire.
  - This gives every SubView a chance to see the key and set `Handled = true`. 
  - This enables container Views to control what keys it's SubViews see. 
  - This enables super-classes to control what keys base classes see (e.g. DateField prevents TextField from seeing non-numeric input)

- Subclasses of `View` can Subscribe to `KeyPress` (instead of overriding `OnKeyPress`)
  - If they do override, they MUST invoke `base.KeyPress` and honor the result before returning.
  - This gives every SubView a chance to see the key and set `Handled = true`. 
  - This enables container Views to control what keys it's SubViews see. 
  - This enables super-classes to control what keys base classes see (e.g. DateField prevents TextField from seeing non-numeric input)

## ConsoleDriver

* No concept of `Command` or `KeyBindings`
* Exposes non-cancelable `KeyDown/Up/Press` events. The `OnKey/Down/Up/Press` methods are public and can be used to simulate keyboard input (in addition to SendKeys).

## Application

* No concept of `Command` or `KeyBindings`
  * A future refinement could support `Command` and `KeyBindings` - Replace existing `AlternateForward/BackKey` with a `Command` and `KeyBinding` for Tab/Ctrl-Tab
* Exposes cancelable `KeyDown/Up/Press` events (via `Handled = true`). The `OnKey/Down/Up/Press` methods are public and can be used to simulate keyboard input.

  