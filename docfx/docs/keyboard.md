# Keyboard Deep Dive

## See Also

* [Cancellable Work Pattern](cancellable-work-pattern.md)
* [Command Deep Dive](command.md)
* [Mouse Deep Dive](mouse.md)
* [Lexicon & Taxonomy](lexicon.md)

## Tenets for Terminal.Gui Keyboard Handling (Unless you know better ones...)

Tenets higher in the list have precedence over tenets lower in the list.

* **Users Have Control** - *Terminal.Gui* provides default key bindings consistent with these tenets, but those defaults are configurable by the user. For example, @Terminal.Gui.ConfigurationManager allows users to redefine key bindings for the system, a user, or an application.

* **More Editor than Command Line** - Once a *Terminal.Gui* app starts, the user is no longer using the command line. Users expect keyboard idioms in TUI apps to be consistent with GUI apps (such as VS Code, Vim, and Emacs). For example, in almost all GUI apps, `Ctrl+V` is `Paste`. But the Linux shells often use `Shift+Insert`. *Terminal.Gui* binds `Ctrl+V` by default.

* **Be Consistent With the User's Platform** - Users get to choose the platform they run *Terminal.Gui* apps on and those apps should respond to keyboard input in a way that is consistent with the platform. For example, on Windows to erase a word to the left, users press `Ctrl+Backspace`. But on Linux, `Ctrl+W` is used.

* **The Source of Truth is Wikipedia** - We use this [Wikipedia article](https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts) as our guide for default key bindings.

* **If It's Hot, It Works** - If a View with a @Terminal.Gui.View.HotKey is visible, and the HotKey is visible, the user should be able to press that HotKey and whatever behavior is defined for it should work. For example, in v1, when a Modal view was active, the HotKeys on MenuBar continued to show "hot". In v2 we strive to ensure this doesn't happen.

## Keyboard APIs

*Terminal.Gui* provides the following APIs for handling keyboard input:

* **Key** - @Terminal.Gui.Key provides a platform-independent abstraction for common keyboard operations. It is used for processing keyboard input and raising keyboard events. This class provides a high-level abstraction with helper methods and properties for common keyboard operations. Use this class instead of the low-level `KeyCode` enum when possible.
* **Key Bindings** - Key Bindings provide a declarative method for handling keyboard input in View implementations. The View calls @Terminal.Gui.View.AddCommand(Terminal.Gui.Command,System.Func{System.Nullable{System.Boolean}}) to declare it supports a particular command and then uses @Terminal.Gui.KeyBindings to indicate which key presses will invoke the command. 
* **Key Events** - The Key Bindings API is rich enough to support the vast majority of use-cases. However, in some cases subscribing directly to key events is needed (e.g. when capturing arbitrary typing by a user). Use @Terminal.Gui.View.KeyDown and related events in these cases.

Each of these APIs are described more fully below.

### **[Key Bindings](~/api/Terminal.Gui.Input.KeyBindings.yml)**

Key Bindings is the preferred way of handling keyboard input in View implementations. The View calls @Terminal.Gui.View.AddCommand(Terminal.Gui.Command,System.Func{System.Nullable{System.Boolean}}) to declare it supports a particular command and then uses @Terminal.Gui.KeyBindings to indicate which key presses will invoke the command. For example, if a View wants to respond to the user pressing the up arrow key to scroll up it would do this

```cs
public MyView : View
{
  AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
  KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
}
```

The `Character Map` Scenario includes a View called `CharMap` that is a good example of the Key Bindings API. 

The [Command](~/api/Terminal.Gui.Input.Command.yml) enum lists generic operations that are implemented by views. For example `Command.Accept` in a `Button` results in the `Accepting` event 
firing while in `TableView` it is bound to `CellActivated`. Not all commands
are implemented by all views (e.g. you cannot scroll in a `Button`). Use the @Terminal.Gui.View.GetSupportedCommands method to determine which commands are implemented by a `View`. 

The default key for activating a button is `Space`. You can change this using 
`KeyBindings.ReplaceKey()`:

```csharp
var btn = new Button () { Title = "Press me" };
btn.KeyBindings.ReplaceKey (btn.KeyBindings.GetKeyFromCommands (Command.Accept));
```

Key Bindings can be added at the `Application` or `View` level. 

For **Application-scoped Key Bindings** there are two categories of Application-scoped Key Bindings:

1) **Application Command Key Bindings** - Bindings for `Command`s supported by @Terminal.Gui.Application. For example, @Terminal.Gui.Application.QuitKey, which is bound to `Command.Quit` and results in @Terminal.Gui.Application.RequestStop(Terminal.Gui.Runnable) being called.
2) **Application Key Bindings** - Bindings for `Command`s supported on arbitrary `Views` that are meant to be invoked regardless of which part of the application is visible/active. 

Use @Terminal.Gui.Application.Keyboard.KeyBindings to add or modify Application-scoped Key Bindings. For backward compatibility, @Terminal.Gui.Application.KeyBindings also provides access to the same key bindings.

**View-scoped Key Bindings** also have two categories:

1) **HotKey Bindings** - These bind to `Command`s that will be invoked regardless of whether the View has focus or not. The most common use-case for `HotKey` bindings is @Terminal.Gui.View.HotKey. For example, a `Button` with a `Title` of `_OK`, the user can press `Alt-O` and the button will be accepted regardless of whether it has focus or not. Add and modify HotKey bindings with @Terminal.Gui.View.HotKeyBindings.
2) **Focused Bindings** - These bind to `Command`s that will be invoked only when the View has focus. Focused Key Bindings are the easiest way to enable a View to support responding to key events. Add and modify Focused bindings with @Terminal.Gui.View.KeyBindings.

**Application-Scoped** Key Bindings 

### HotKey

A **HotKey** is a key press that selects a visible UI item. For selecting items across `View`s (e.g. a `Button` in a `Dialog`) the key press must have the `Alt` modifier. For selecting items within a `View` that are not `View`s themselves, the key press can be key without the `Alt` modifier.  For example, in a `Dialog`, a `Button` with the text of "_Text" can be selected with `Alt+T`. Or, in a `Menu` with "_File _Edit", `Alt+F` will select (show) the "_File" menu. If the "_File" menu has a sub-menu of "_New" `Alt+N` or `N` will ONLY select the "_New" sub-menu if the "_File" menu is already opened.

By default, the `Text` of a `View` is used to determine the `HotKey` by looking for the first occurrence of the @Terminal.Gui.View.HotKeySpecifier (which is underscore (`_`) by default). The character following the underscore is the `HotKey`. If the `HotKeySpecifier` is not found in `Text`, the first character of `Text` is used as the `HotKey`. The `Text` of a `View` can be changed at runtime, and the `HotKey` will be updated accordingly. @"Terminal.Gui.View.HotKey" is `virtual` enabling this behavior to be customized.

### **Shortcut**

A **Shortcut** is an opinionated (visually & API) View for displaying a command, help text, key key press that invokes a [Command](~/api/Terminal.Gui.Input.Command.yml).

The Command can be invoked even if the `View` that defines them is not focused or visible (but the `View` must be enabled). Shortcuts can be any key press; `Key.A`, `Key.A.WithCtrl`, `Key.A.WithCtrl.WithAlt`, `Key.Del`, and `Key.F1`, are all valid. 

`Shortcuts` are used to define application-wide actions or actions that are not visible (e.g. `Copy`).

[MenuBar](~/api/Terminal.Gui.Views.MenuBar.yml), [PopoverMenu](~/api/Terminal.Gui.Views.PopoverMenu.yml), and [StatusBar](~/api/Terminal.Gui.Views.StatusBar.yml) support `Shortcut`s. 

### **Key Events**

Keyboard events are retrieved from [Drivers](drivers.md) each iteration of the [Application](~/api/Terminal.Gui.App.Application.yml) [Main Loop](multitasking.md). The driver raises the @Terminal.Gui.IDriver.KeyDown event which invokes @Terminal.Gui.Application.RaiseKeyDownEvent* respectively.

> [!NOTE]
> Most drivers/platforms do not support sensing distinct KeyUp events, therefore Terminal.Gui does not support them.

@Terminal.Gui.Application.RaiseKeyDownEvent* raises @Terminal.Gui.Application.KeyDown and then calls @Terminal.Gui.View.NewKeyDownEvent* on all runnable Views. If no View handles the key event, any Application-scoped key bindings will be invoked. Application-scoped key bindings are managed through @Terminal.Gui.Application.Keyboard.KeyBindings.

If a view is enabled, the @Terminal.Gui.View.NewKeyDownEvent* method will do the following: 

1) If the view has a subview that has focus, 'NewKeyDown' on the focused view will be called. This is recursive. If the most-focused view handles the key press, processing stops.
2) If there is no most-focused sub-view, or a most-focused sub-view does not handle the key press, @Terminal.Gui.View.OnKeyDown* will be called. If the view handles the key press, processing stops.
3) If @Terminal.Gui.View.OnKeyDown* does not handle the event. @Terminal.Gui.View.KeyDown will be raised.
4) If the view does not handle the key down event, any bindings for the key will be invoked (see the @Terminal.Gui.View.KeyBindings property). If the key is bound and any of it's command handlers return true, processing stops.
5) If the key is not bound, or the bound command handlers do not return true, @Terminal.Gui.View.OnKeyDownNotHandled* is called. 

## **Application Key Handling**

To define application key handling logic for an entire application in cases where the methods listed above are not suitable, use the @Terminal.Gui.Application.KeyDown event. 

## **Key Down/Up Events**

*Terminal.Gui* supports key down events only via @Terminal.Gui.View.OnKeyDown*.

# General input model

- Key Down and Up events are generated by the driver. 
- `IApplication` implementations subscribe to driver KeyDown/Up events and forwards them to the most-focused `Runnable` view using `View.NewKeyDownEvent`.
- The base (`View`) implementation of `NewKeyDownEvent` follows a pattern of "Before", "During", and "After" processing:
  - **Before**
    - If `Enabled == false` that view should *never* see keyboard (or mouse input).
    - `NewKeyDownEvent` is called on the most-focused SubView (if any) that has focus. If that call returns true, the method returns.
    - Calls `OnKeyDown`.
  - **During**
    - Assuming `OnKeyDown` call returns false (indicating the key wasn't handled) any commands bound to the key will be invoked.
  - **After**
    - Assuming no keybinding was found or all invoked commands were not handled:
       - `OnKeyDownNotHandled` is called to process the key.
       - `KeyDownNotHandled` is raised.

- Subclasses of `View` can (rarely) override `OnKeyDown` (or subscribe to `KeyDown`) to see keys before they are processed 
- Subclasses of `View` can (often) override `OnKeyDownNotHandled` to do key processing for keys that were not previously handled. `TextField` and `TextView` are examples.

## Application

* Implements support for `KeyBindingScope.Application`.
* Keyboard functionality is now encapsulated in the @Terminal.Gui.IKeyboard interface, accessed via @Terminal.Gui.Application.Keyboard.
* @Terminal.Gui.Application.Keyboard provides access to @Terminal.Gui.KeyBindings, key binding configuration (QuitKey, ArrangeKey, navigation keys), and keyboard event handling.
* For backward compatibility, @Terminal.Gui.Application still exposes static properties/methods that delegate to @Terminal.Gui.Application.Keyboard (e.g., `IApplication.KeyBindings`, `IApplication.RaiseKeyDownEvent`, `IApplication.QuitKey`).
* Exposes cancelable `KeyDown` events (via `Handled = true`). The `RaiseKeyDownEvent` method is public and can be used to simulate keyboard input, although it is preferred to use `InputInjector` for testing.
* The @Terminal.Gui.IKeyboard interface enables testability with isolated keyboard instances that don't depend on static Application state.

## View

* Implements support for `KeyBindings` and `HotKeyBindings`.
* Exposes cancelable non-virtual method for a new key event: `NewKeyDownEvent`.
* Exposes cancelable virtual methods for a new key event: `OnKeyDown`. This method is called by `NewKeyDownEvent` and can be overridden to handle keyboard input.

## IKeyboard Architecture

The @Terminal.Gui.IKeyboard interface provides a decoupled, testable architecture for keyboard handling in Terminal.Gui. This design allows for:

### Key Features

1. **Decoupled State** - All keyboard-related state (key bindings, navigation keys, events) is encapsulated in @Terminal.Gui.IKeyboard, separate from the static @Terminal.Gui.Application class.

2. **Dependency Injection** - The @Terminal.Gui.Keyboard implementation receives an @Terminal.Gui.IApplication reference, enabling it to interact with application state without static dependencies.

3. **Testability** - Unit tests can create isolated @Terminal.Gui.IKeyboard instances with mock @Terminal.Gui.IApplication references, enabling parallel test execution without interference.

4. **Backward Compatibility** - All existing @Terminal.Gui.Application keyboard APIs (e.g., `Application.KeyBindings`, `Application.RaiseKeyDownEvent`, `Application.QuitKey`) remain available and delegate to `Application.Keyboard`.

### Usage Examples

**Accessing keyboard functionality:**

```csharp
// Modern approach - using IKeyboard
App.Keyboard.KeyBindings.Add(Key.F1, Command.HotKey);
App.Keyboard.RaiseKeyDownEvent(Key.Enter);
App.Keyboard.QuitKey = Key.Q.WithCtrl;

// Legacy approach - still works (delegates to Application.Keyboard)
Application.KeyBindings.Add(Key.F1, Command.HotKey);
Application.RaiseKeyDownEvent(Key.Enter);
Application.QuitKey = Key.Q.WithCtrl;
```

**Testing with isolated keyboard instances:**

```csharp
// Create independent keyboard instances for parallel tests
var keyboard1 = new Keyboard();
keyboard1.QuitKey = Key.Q.WithCtrl;
keyboard1.KeyBindings.Add(Key.F1, Command.HotKey);

var keyboard2 = new Keyboard();
keyboard2.QuitKey = Key.X.WithCtrl;
keyboard2.KeyBindings.Add(Key.F2, Command.Accept);

// keyboard1 and keyboard2 maintain completely separate state
Assert.Equal(Key.Q.WithCtrl, keyboard1.QuitKey);
Assert.Equal(Key.X.WithCtrl, keyboard2.QuitKey);
```

**Accessing application context from views:**

```csharp
public class MyView : View
{
    protected override bool OnKeyDown(Key key)
    {
        // Use View.App instead of static Application
        if (key == Key.F1)
        {
            App?.Keyboard?.KeyBindings.Add(Key.F2, Command.Accept);
            return true;
        }
        return base.OnKeyDown(key);
    }
}
```

### Architecture Benefits

- **Parallel Testing**: Multiple test methods can create and use separate @Terminal.Gui.IKeyboard instances simultaneously without state interference.
- **Dependency Inversion**: @Terminal.Gui.Keyboard depends on @Terminal.Gui.IApplication interface rather than static @Terminal.Gui.Application class.
- **Cleaner Code**: Keyboard functionality is organized in a dedicated interface rather than scattered across @Terminal.Gui.Application partial classes.
- **Mockability**: Tests can provide mock @Terminal.Gui.IApplication implementations to test keyboard behavior in isolation.

### Implementation Details

The @Terminal.Gui.Keyboard class implements @Terminal.Gui.IKeyboard and maintains:

- **KeyBindings**: Application-scoped key binding dictionary
- **Navigation Keys**: QuitKey, ArrangeKey, NextTabKey, PrevTabKey, NextTabGroupKey, PrevTabGroupKey
- **Events**: KeyDown event for application-level keyboard monitoring
- **Command Implementations**: Handlers for Application-scoped commands (Quit, Suspend, Navigation, Refresh, Arrange)

The @Terminal.Gui.IApplication implementations create and manage the @Terminal.Gui.IKeyboard instance, setting its `IApplication` property to `this` to provide the necessary @Terminal.Gui.IApplication reference.

## Testing Keyboard Input

> **For comprehensive documentation on testing,** see **[Input Injection](input-injection.md)**.

Terminal.Gui provides a sophisticated input injection system for testing keyboard behavior without requiring actual keyboard hardware. Here's a quick overview:

### Quick Test Example

```csharp
// Create application with virtual time for deterministic testing
VirtualTimeProvider time = new();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

// Subscribe to key events
app.Keyboard.KeyDown += (s, e) => Console.WriteLine($"Key: {e}");

// Inject keys
app.InjectKey(Key.A);
app.InjectKey(Key.Enter);
app.InjectKey(Key.Esc);
```

### Testing Key Commands

```csharp
VirtualTimeProvider time = new();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

Button button = new() { Text = "_Click Me" };
bool acceptingCalled = false;
button.Accepting += (s, e) => acceptingCalled = true;

IRunnable runnable = new Runnable();
(runnable as View)?.Add(button);
app.Begin(runnable);

// Inject hotkey (Alt+C)
app.InjectKey(Key.C.WithAlt);

Assert.True(acceptingCalled);
```

### Testing Escape Sequences with Pipeline Mode

```csharp
// Pipeline mode tests full ANSI encoding/decoding
VirtualTimeProvider time = new();
using IApplication app = Application.Create(time);
app.Init(DriverRegistry.Names.ANSI);

IInputInjector injector = app.GetInputInjector();
InputInjectionOptions options = new() { Mode = InputInjectionMode.Pipeline };

// This encodes Key.F1 ? "\x1b[OP", injects chars, parses back to Key.F1
injector.InjectKey(Key.F1, options);
```

### Key Testing Features

- **Virtual Time Control** - Deterministic timing for escape sequence handling
- **Single-Call Injection** - `app.InjectKey(key)` handles everything
- **No Real Delays** - Tests run instantly using virtual time
- **Two Modes** - Direct (default, fast) and Pipeline (full ANSI encoding)
- **Escape Sequence Handling** - Automatic release of stale escapes

**Learn More:** See **[Input Injection](input-injection.md)** for complete documentation including:
- Architecture and design
- Testing patterns and best practices
- Advanced scenarios (modifier keys, function keys, special keys)
- Troubleshooting guide
