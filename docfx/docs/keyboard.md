# Keyboard Events

## Tenets for Terminal.Gui Keyboard Handling (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Users Have Control** - *Terminal.Gui* provides default key bindings consistent with these tenets, but those defaults are configurable by the user. For example, @Terminal.Gui.ConfigurationManager allows users to redefine key bindings for the system, a user, or an application.

* **More Editor than Command Line** - Once a *Terminal.Gui* app starts, the user is no longer using the command line. Users expect keyboard idioms in TUI apps to be consistent with GUI apps (such as VS Code, Vim, and Emacs). For example, in almost all GUI apps, `Ctrl+V` is `Paste`. But the Linux shells often use `Shift+Insert`. *Terminal.Gui* binds `Ctrl+V` by default.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to keyboard input in a way that is consistent with the platform. For example, on Windows to erase a word to the left, users press `Ctrl+Backspace`. But on Linux, `Ctrl+W` is used.

* **The Source of Truth is Wikipedia** - We use this [Wikipedia article](https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts) as our guide for default key bindings.

* **If It's Hot, It Works** - If a View with a @Terminal.Gui.View.HotKey is visible, and the HotKey is visible, the user should be able to press that HotKey and whatever behavior is defined for it should work. For example, in v1, when a Modal view was active, the HotKeys on MenuBar continued to show "hot". In v2 we strive to ensure this doesn't happen.


## Keyboard APIs

*Terminal.Gui* provides the following APIs for handling keyboard input:

### **[Key](~/api/Terminal.Gui.Key.yml)**

The `Key` class provides a platform-independent abstraction for common keyboard operations. It is used for processing keyboard input and raising keyboard events. This class provides a high-level abstraction with helper methods and properties for common keyboard operations. Use this class instead of the low-level `KeyCode` enum when possible.

See [Key](~/api/Terminal.Gui.Key.yml) for more details.

### **[Key Bindings](~/api/Terminal.Gui.KeyBindings.yml)**

The default key for activating a button is `Space`. You can change this using 
`KeyBindings.ReplaceKey()`:

```csharp
var btn = new Button () { Title = "Press me" };
btn.KeyBindings.ReplaceKey (btn.KeyBindings.GetKeyFromCommands (Command.Accept));
```

The [Command](~/api/Terminal.Gui.Command.yml) enum lists generic operations that are implemented by views. For example `Command.Accept` in a `Button` results in the `Clicked` event 
firing while in `TableView` it is bound to `CellActivated`. Not all commands
are implemented by all views (e.g. you cannot scroll in a `Button`). Use the @Terminal.Gui.View.GetSupportedCommands() method to determine which commands are implemented by a `View`. 

Key Bindings can be added at the `Application` or `View` level. For Application-scoped Key Bindings see @Terminal.Gui.Application.Navigation. For View-scoped Key Bindings see @Terminal.Gui.View.KeyBindings.

### **@"Terminal.Gui.View.HotKey"** 

A **HotKey** is a key press that selects a visible UI item. For selecting items across `View`s (e.g. a `Button` in a `Dialog`) the key press must have the `Alt` modifier. For selecting items within a `View` that are not `View`s themselves, the key press can be key without the `Alt` modifier.  For example, in a `Dialog`, a `Button` with the text of "_Text" can be selected with `Alt+T`. Or, in a `Menu` with "_File _Edit", `Alt+F` will select (show) the "_File" menu. If the "_File" menu has a sub-menu of "_New" `Alt+N` or `N` will ONLY select the "_New" sub-menu if the "_File" menu is already opened.

By default, the `Text` of a `View` is used to determine the `HotKey` by looking for the first occurrence of the @Terminal.Gui.View.HotKeySpecifier (which is underscore (`_`) by default). The character following the underscore is the `HotKey`. If the `HotKeySpecifier` is not found in `Text`, the first character of `Text` is used as the `HotKey`. The `Text` of a `View` can be changed at runtime, and the `HotKey` will be updated accordingly. @"Terminal.Gui.View.HotKey" is `virtual` enabling this behavior to be customized.

### **[Shortcut](~/api/Terminal.Gui.Shortcut.yml)**

A **Shortcut** is an opinionated (visually & API) View for displaying a command, help text, key key press that invokes a [Command](~/api/Terminal.Gui.Command.yml).

The Command can be invoked even if the `View` that defines them is not focused or visible (but the `View` must be enabled). Shortcuts can be any key press; `Key.A`, `Key.A.WithCtrl`, `Key.A.WithCtrl.WithAlt`, `Key.Del`, and `Key.F1`, are all valid. 

`Shortcuts` are used to define application-wide actions or actions that are not visible (e.g. `Copy`).

[MenuBar](~/api/Terminal.Gui.MenuBar.yml), [ContextMenu](~/api/Terminal.Gui.ContextMenu.yml), and [StatusBar](~/api/Terminal.Gui.StatusBar.yml) support `Shortcut`s. 

### **Handling Keyboard Events**

Keyboard events are retrieved from [Console Drivers](drivers.md) and passed on 
to the [Application](~/api/Terminal.Gui.Application.yml) class by the [Main Loop](mainloop.md). 

[Application](~/api/Terminal.Gui.Application.yml) then determines the current [Toplevel](~/api/Terminal.Gui.Toplevel.yml) view
(either the default created by calling @Terminal.Gui.Application.Init(Terminal.Gui.ConsoleDriver,System.String), or the one set by calling `Application.Run`). The mouse event, using [Viewport-relative coordinates](xref:Terminal.Gui.View.Viewport) is then passed to the @Terminal.Gui.View.NewKeyDownEvent(Terminal.Gui.Key) method of the current [Toplevel](~/api/Terminal.Gui.Toplevel.yml) view. 

If the view is enabled, the @Terminal.Gui.View.NewKeyDownEvent(Terminal.Gui.Key) method will do the following: 

1) If the view has a subview that has focus, 'ProcessKeyDown' on the focused view will be called. If the focused view handles the key press, processing stops.
2) If there is no focused sub-view, or the focused sub-view does not handle the key press, @Terminal.Gui.View.OnKeyDown(Terminal.Gui.Key) will be called. If the view handles the key press, processing stops.
3) If the view does not handle the key press, @Terminal.Gui.TextField.OnInvokingKeyBindings(Terminal.Gui.Key,Terminal.Gui.KeyBindingScope) will be called. This method calls @Terminal.Gui.View.InvokeKeyBindings(Terminal.Gui.Key,Terminal.Gui.KeyBindingScope) to invoke any keys bound to commands. If the key is bound and any of it's command handlers return true, processing stops.
4) If the key is not bound, or the bound command handlers do not return true, @Terminal.Gui.View.OnProcessKeyDown(Terminal.Gui.Key) is called. If the view handles the key press, processing stops.

## **Application Key Handling**

To define application key handling logic for an entire application in cases where the methods listed above are not suitable, use the `Application.OnKeyDown` event. 

## **Key Down/Up Events**

*Terminal.Gui* supports key up/down events with @Terminal.Gui.View.OnKeyDown(Terminal.Gui.Key) and @Terminal.Gui.View.OnKeyUp(Terminal.Gui.Key), but not all [Console Drivers](drivers.md) do. To receive these key down and key up events, you must use a driver that supports them (e.g. `WindowsDriver`).

# General input model

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
* Exposes @Terminal.Gui.Application.KeyBindings.
* Exposes cancelable `KeyDown/Up` events (via `Handled = true`). The `OnKey/Down/Up/` methods are public and can be used to simulate keyboard input.

## View

* Implements support for `KeyBindingScope.View` and `KeyBindingScope.HotKey`.
* Exposes cancelable non-virtual methods for a new key event: `NewKeyDownEvent` and `NewKeyUpEvent`. These methods are called by `Application` can be called to simulate keyboard input.
* Exposes cancelable virtual methods for a new key event: `OnKeyDown` and `OnKeyUp`. These methods are called by `NewKeyDownEvent` and `NewKeyUpEvent` and can be overridden to handle keyboard input.

  