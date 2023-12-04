Keyboard Event Processing
=========================

**These are the v1 API docs**. The v2 API docs are [here](https://gui-cs.github.io/Terminal.GuiV2Docs/).

**Terminal.Gui** respects common Linux, Mac, and Windows keyboard idioms. For example, clipboard operations use the familiar `Control/Command-C, X, V` model. `CTRL-Q` is used for exiting views (and apps).

The input handling of **Terminal.Gui** is similar in some ways to Emacs and the Midnight Commander, so you can expect some of the special key combinations to be active.

The key `ESC` can act as an Alt modifier (or Meta in Emacs parlance), to allow input on terminals that do not have an alt key. So to produce the sequence `Alt-F`, you can press either `Alt-F`, or `ESC` followed by the key `F`.

To enter the key `ESC`, you can either press `ESC` and wait 100 milliseconds, or you can press `ESC` twice.

`ESC-0`, and `ESC-1` through `ESC-9` have a special meaning, they map to `F10`, and `F1` to `F9` respectively.

Apps can change key bindings using the `AddKeyBinding` API. 

Keyboard events are sent by the [Main Loop](mainloop.md) to the
Application class for processing. The keyboard events are sent
exclusively to the current `Toplevel`, this being either the default
that is created when you call `Application.Init`, or one that you
created and passed to `Application.Run(Toplevel)`. 

Flow
----

Keystrokes are first processes as hotkeys, then as regular keys, and
there is a final cold post-processing event that is invoked if no view
processed the key.

HotKey Processing
-----------------

Events are first send to all views as a "HotKey", this means that the
`View.ProcessHotKey` method is invoked on the current toplevel, which
in turns propagates this to all the views in the hierarchy. If any
view decides to process the event, no further processing takes place.

This is how hotkeys for buttons are implemented. For example, the
keystroke "Alt-A" is handled by Buttons that have a hot-letter "A" to
activate the button.

Regular Processing
------------------

Unlike the hotkey processing, the regular processing is only sent to
the currently focused view in the focus chain.

The regular key processing is only invoked if no hotkey was caught.

Cold-key Processing
-------------------

This stage only is executed if the focused view did not process the
event, and is broadcast to all the views in the Toplevel.

This method can be overwritten by views that want to provide
accelerator functionality (Alt-key for example), but without
interfering with normal ProcessKey behavior.

Key Bindings
-------------------
**Terminal.Gui** supports rebinding keys. For example the default key
for activating a button is Enter. You can change this using the 
`ClearKeybinding` and `AddKeybinding` methods:

```csharp
var btn = new Button ("Press Me");
btn.ClearKeybinding (Command.Accept);
btn.AddKeyBinding (Key.b, Command.Accept);
```

The `Command` enum lists generic operations that are implemented by views.
For example `Command.Accept` in a Button results in the `Clicked` event 
firing while in `TableView` it is bound to `CellActivated`. Not all commands
are implemented by all views (e.g. you cannot scroll in a Button). To see
which commands are implemented by a View you can use the `GetSupportedCommands()`
method.

Not all controls have the same key bound for a given command, for example
`Command.Accept` defaults to `Key.Enter` in a `Button` but defaults to `Key.Space`
in `RadioGroup`.

Global Key Handler
--------------------
Sometimes you may want to define global key handling logic for your entire 
application that is invoked regardless of what Window/View has focus. This can
be achieved by using the `Application.RootKeyEvent` event.
