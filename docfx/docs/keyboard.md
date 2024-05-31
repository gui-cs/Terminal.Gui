# Keyboard Events

## Tenets for Terminal.Gui Keyboard Handling (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Users Have Control** - *Terminal.Gui* provides default key bindings consistent with these tenets, but those defaults are configurable by the user. For example, `ConfigurationManager` allows users to redefine key bindings for the system, a user, or an application.

* **More Editor than Command Line** - Once a *Terminal.Gui* app starts, the user is no longer using the command line. Users expect keyboard idioms in TUI apps to be consistent with GUI apps (such as VS Code, Vim, and Emacs). For example, in almost all GUI apps, `Ctrl-V` is `Paste`. But the Linux shells often use `Shift-Insert`. *Terminal.Gui* binds `Ctrl-V` by default.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to keyboard input in a way that is consistent with the platform. For example, on Windows to erase a word to the left, users press `Ctrl-Backspace`. But on Linux, `Ctrl-W` is used.

* **The Source of Truth is Wikipedia** - We use this [Wikipedia article](https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts) as our guide for default key bindings.

## Keyboard API

### **[Key](~/api/Terminal.Gui.Key.yml)**

The `Key` class provides a platform-independent abstraction for common keyboard operations. It is used for processing keyboard input and raising keyboard events. This class provides a high-level abstraction with helper methods and properties for common keyboard operations. Use this class instead of the low-level `KeyCode` enum when possible.

See [Key](~/api/Terminal.Gui.Key.yml) for more details.

### **[Key Bindings](~/api/Terminal.Gui.KeyBindings.yml)**

The default keys for activating a button are `Space`, `Enter`, and the HotKey. You can change this using `Keybindings.Clear` and `Keybinding.Add` methods.

```csharp
var btn = new Button () { Title = "Press 'b' to CLick Me" };
btn.Keybinding.Remove (Command.Accept);
btn.KeyBinding.Add (Key.B, Command.Accept);
```

The [Command](~/api/Terminal.Gui.Command.yml) enum lists generic operations that are implemented by views. For example `Command.Accept` in a `Button` results in the `Clicked` event firing while in `TableView` it is bound to `CellActivated`. Not all commands are implemented by all views (e.g. you cannot scroll in a `Button`). Use the `GetSupportedCommands()` method to determine which commands are implemented by a `View`. 

Keys can be bound to a View using one of three scopes: Focused, HotKey, and Appliication.

- `Focused` - Means "Bind this key to me so that when I have focus nobody but me sees the key, but the Command is invoked on me when it's pressed". This is the default behavior.
- `HotKey` - Means "Bind this key to me so that regardless of whether I have focus or not, as long as my Superview heirchy has focus, I'll get the command invocation if pressed." By default, the key is automatcially bound to the Hotkey and the `Command.Hotkey` command, which is, effectively the "Default command" for a view.
- `Application` - Means "Bind this key to me so that I'll always get the command invocation if pressed." This enables a view to register an application-wide shortcut Key/Command.

### **[HotKey](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_HotKey)** 

A **HotKey** is a keypress that changes the focus to a visible View and invokes that View's default command. For selecting items across `View`s (e.g. a `Button` in a `Dialog`) the keypress must have the `Alt` modifier. For selecting items within a `View` that are not `View`s themselves, the keypress can be key without the `Alt` modifier.  For example, in a `Dialog`, a `Button` with the text of "_Text" can be selected with `Alt-T`. Or, in a `Menu` with "_File _Edit", `Alt-F` will select (show) the "_File" menu. If the "_File" menu has a sub-menu of "_New" `Alt-N` or `N` will ONLY select the "_New" sub-menu if the "_File" menu is already opened.

By default, the `Text` of a `View` is used to determine the `HotKey` by looking for the first occurrence of the [HotKeySpecifier](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_HotKeySpecifier) (which is underscore (`_`) by default). The character following the underscore is the `HotKey`. If the `HotKeySpecifier` is not found in `Text`, the first character of `Text` is used as the `HotKey`. The `Text` of a `View` can be changed at runtime, and the `HotKey` will be updated accordingly. 

Define a HotKey by setting the [HotKey](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_HotKey) property on a View. `HotKey` is `virtual` enabling this behavior to be customized. The default Command for a view is `Command.HotKey` and the default behavior is for the View to take focus an the `Accept` event to be fired.

### **[Shortcut](~/api/Terminal.Gui.MenuItem.yml#Terminal_Gui_MenuItem_Shortcut)** 

`Shortcuts` are used to define application-wide actions (e.g. `Quit`), or actions that are not visible (e.g. `Copy`).

A **Shortcut** is a keypress that invokes a [Command](~/api/Terminal.Gui.Command.yml) or `View`-defined action regardless of whether the `View` that defines them is visible (but the `View` must be enabled) or focused. Shortcuts can be any keypress; `Key.A`, `Key.A | Key.Ctrl`, `Key.A | Key.Ctrl | Key.Alt`, `Key.Del`, and `Key.F1`, are all valid. 

Not all built-in `Views` support `Shortcut`s. [MenuBar](~/api/Terminal.Gui.MenuBar.yml), [ContextMenu](~/api/Terminal.Gui.ContextMenu.yml), and [StatusBar](~/api/Terminal.Gui.StatusBar.yml) support `Shortcut`s. However, the `Button` class does not. 

Define a shortcut by calling `KeyBindings.Add` and specifying the `KeyBindingScope.Application` flag.

# General input model

## **[Keyboard Event Model](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_KeyDown)**

Keyboard events are retrieved from [Console Drivers](drivers.md) and passed on 
to the [Application](~/api/Terminal.Gui.Application.yml) class by the [Main Loop](mainloop.md). 

[Application](~/api/Terminal.Gui.Application.yml) then determines the current [Toplevel](~/api/Terminal.Gui.Toplevel.yml) view
(either the default created by calling `Application.Init`, or the one set by calling `Application.Run`). The mouse event, using [Bounds-relative coordinates](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_Bounds) is then passed to the [NewKeyDownEvent](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_NewKeyDownEvent_Terminal_Gui_Key_) method of the current [Toplevel](~/api/Terminal.Gui.Toplevel.yml) view. 

If the view is enabled, the [NewKeyDownEvent](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_NewKeyDownEvent_Terminal_Gui_Key_) method will do the following: 

1) If the view has a subview that has focus, 'ProcessKeyDown' on the focused view will be called. If the focused view handles the keypress, processing stops.
2) If there is no focused sub-view, or the focused sub-view does not handle the keypress, [OnKeyDown](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnKeyDown_Terminal_Gui_Key_) will be called. If the view handles the keypress, processing stops.
3) If the view does not handle the keypress, [OnInvokingKeyBindings](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnInvokingKeyBindings_Terminal_Gui_Key_) will be called. This method calls[InvokeKeyBindings](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_InvokeKeyBindings_Terminal_Gui_Key_) to invoke any keys bound to commands. If the key is bound and any of it's command handlers return true, processing stops.
4) If the key is not bound, or the bound command handlers do not return true, [OnProcessKeyDow](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnProcessKeyDown_Terminal_Gui_Key_) is called. If the view handles the keypress, processing stops.

## **[Application Key Handling](~/api/Terminal.Gui.Application.yml#Terminal_Gui_Application_OnKeyDown_Terminal_Gui_Key_)**

To define application key handling logic for an entire application in cases where the methods listed above are not suitable, use the `Application.OnKeyDown` event. 

## **[Key Down/Up Events](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_KeyDown)**

*Terminal.Gui* supports key up/down events with [OnKeyDown](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnKeyDown_Terminal_Gui_Key_) and [OnKeyUp](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_OnKeyUp_Terminal_Gui_Key_), but not all [Console Drivers](drivers.md) do. To receive these key down and key up events, you must use a driver that supports them (e.g. `WindowsDriver`).

- Key Down and Up events are generated by `ConsoleDriver`. 
- `Application` subscribes to `ConsoleDriver.Down/Up` events and forwards them to the most-focused `TopLevel` view using `View.NewKeyDownEvent` and `View.NewKeyUpEvent`.
- The base (`View`) implementation of `NewKeyDownEvent` follows a pattern of "Before", "During", and "After" processing:
  - **Before**
    - If `Enabled == false` that view should *never* see keyboard (or mouse input).
    - `NewKeyDownEvent` is called on the most-focused SubView (if any) that has focus. If that call returns true, the method returns.
    - Calls `OnKeyDown`.
  - **During**
    - Assuming `OnKeyDown` call returns false (indicating the key wasn't handled)
       - `OnInvokingKeyBindings` is called to invoke any bound commands.
       - `OnInvokingKeyBindings` fires the `InvokingKeyBindings` event
  - **After**
    - Assuming `OnInvokingKeyBindings` returns false (indicating the key wasn't handled)
       - `OnProcessKeyDown` is called to process the key.
       - `OnProcessKeyDown` fires the `ProcessKeyDown` event

- Subclasses of `View` can (rarely) override `OnKeyDown` to see keys before they are processed by `OnInvokingKeyBindings` and `OnProcessKeyDown
- Subclasses of `View` can (rarely) override `OnInvokingKeyBindings` to see keys before they are processed by `OnProcessKeyDown`
- Subclasses of `View` can (often) override `OnProcessKeyDown` to do normal key processing.

## ConsoleDriver

* No concept of `Command` or `KeyBindings`
* Use the low-level `KeyCode` enum.
* Exposes non-cancelable `KeyDown/Up` events. The `OnKey/Down/Up` methods are public and can be used to simulate keyboard input (in addition to SendKeys).

## Application

* Implements support for `KeyBindingScope.Application`.
* Exposes cancelable `KeyDown/Up` events (via `Handled = true`). The `OnKey/Down/Up/` methods are public and can be used to simulate keyboard input.

## View

* Implements support for `KeyBindingScope.Focused` and `KeyBindingScope.HotKey`.
* Exposes cancelable non-virtual methods for a new key event: `NewKeyDownEvent` and `NewKeyUpEvent`. These methods are called by `Application` can be called to simulate keyboard input.
* Exposes cancelable virtual methods for a new key event: `OnKeyDown` and `OnKeyUp`. These methods are called by `NewKeyDownEvent` and `NewKeyUpEvent` and can be overridden to handle keyboard input.

  