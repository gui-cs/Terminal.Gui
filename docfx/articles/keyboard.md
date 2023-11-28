Keyboard Event Processing
=========================

**Terminal.Gui** respects common Linux, Mac, and Windows keyboard idioms. For example, clipboard operations use the familiar `Control/Command-C, X, V` model. `CTRL-Q` is used for exiting views (and apps).

The input handling of **Terminal.Gui** is similar in some ways to Emacs and the Midnight Commander, so you can expect some of the special key combinations to be active.

The key `ESC` can act as an Alt modifier (or Meta in Emacs parlance), to allow input on terminals that do not have an alt key. So to produce the sequence `Alt-F`, you can press either `Alt-F`, or `ESC` followed by the key `F`.

To enter the key `ESC`, you can either press `ESC` and wait 100 milliseconds, or you can press `ESC` twice.

`ESC-0`, and `ESC-1` through `ESC-9` have a special meaning, they map to `F10`, and `F1` to `F9` respectively.

[Views](~/api/Terminal.Gui/Terminal.Gui.View.yml) can be configured to use different key bindings by using [View.AddKeyBinding](~/api/Terminal.Gui/Terminal.Gui.View.yml#Terminal_Gui_View_AddKeyBinding_Terminal_Gui_Key_Terminal_Gui_Command___).

Apps can change default key bindings using [View.AddKeyBinding](~/api/Terminal.Gui/Terminal.Gui.View.yml#Terminal_Gui_View_AddKeyBinding_Terminal_Gui_Key_Terminal_Gui_Command___). 

Flow
----
Keyboard events are retrieved from [Console Drivers](drivers.md) and passed on 
to the [Application](~/api/Terminal.Gui/Terminal.Gui.Application.yml) class by the [Main Loop](mainloop.md). 

[Application](~/api/Terminal.Gui/Terminal.Gui.Application.yml) then determines the current [Toplevel](~/api/Terminal.Gui/Terminal.Gui.Toplevel.yml#Terminal_Gui_Application_Init_Terminal_Gui_ConsoleDriver_) view
(either the default created by calling [Application.Init](~/api/Terminal.Gui/Terminal.Gui.Application.yml), or the one set by calling [Application.Run(Toplevel)](~/api/Terminal.Gui/Terminal.Gui.Toplevel.yml#Terminal_Gui_Application_Run_Terminal_Gui_Toplevel_System_Func_System_Exception_System_Boolean__)). The mouse event, using [Bounds-relative coordinates](~/api/Terminal.Gui/Terminal.Gui.View.yml#Terminal_Gui_View_Bounds) is then passed to the [ProcessKeyPressed](~/api/Terminal.Gui/Terminal.Gui.View.yml#Terminal_Gui_View_ProcessKeyPressed_Terminal_Gui_KeyEventArgs_) method of the current [Toplevel](~/api/Terminal.Gui/Terminal.Gui.Toplevel.yml) view. 

If the view is enabled [ProcessKeyPressed](~/api/Terminal.Gui/Terminal.Gui.View.yml#Terminal_Gui_View_ProcessKeyPressed_Terminal_Gui_KeyEventArgs_) will 

1) If the view has a subview that has focus, [ProcessHotKey](~/api/Terminal.Gui/Terminal.Gui.View.yml#Terminal_Gui_View_ProcessHotKey_Terminal_Gui_KeyEventArgs_) on the focused view will be called. If the focused view handles the key press, processing stops.
2) If there is no focused sub-view, or the focused sub-view does not handle the key press, [OnKeyPressed](~/api/Terminal.Gui/Terminal.Gui.View.yml#Terminal_Gui_View_OnKeyPressed_Terminal_Gui_KeyEventArgs_) will be called. If the view handles the key press, processing stops.
3) If the view does not handle the key press, [OnInvokeKeyBindings](~/api/Terminal.Gui/Terminal.Gui.View.yml#Terminal_Gui_View_OnInvokeKeyBindings_Terminal_Gui_KeyEventArgs_) will be called. This method calls[InvokeKeyBindings](~/api/Terminal.Gui/Terminal.Gui.View.yml#Terminal_Gui_View_InvokeKeyBindings_Terminal_Gui_KeyEventArgs_) to invoke any keys bound to commands. If the key is bound and handled, processing stops.

Key Bindings
-------------------
**Terminal.Gui** supports rebinding keys. For example, the default key
for activating a button is Space. You can change this using the 
`ClearKeybinding` and `AddKeybinding` methods:

```csharp
var btn = new Button ("Press Me");
btn.ClearKeybinding (Command.Accept);
btn.AddKeyBinding (Key.B, Command.Accept);
```

The `Command` enum lists generic operations that are implemented by views.
For example `Command.Accept` in a Button results in the `Clicked` event 
firing while in `TableView` it is bound to `CellActivated`. Not all commands
are implemented by all views (e.g. you cannot scroll in a Button). To see
which commands are implemented by a View you can use the `GetSupportedCommands()`
method.


Global Key Handler
--------------------
To define global key handling logic for an entire application, regardless of what View has focus, use the `Application.KeyPressed` event.

Low-Level Key Handling
----------------------
To handle keys at a lower level, override the `OnKeyPressed` and/or `OnInvokingKeyBindings`. These methods are called before any of the other key handling methods. Return `true` from these methods and no further processing will be done on the key. 

Key Up/Down Events
------------------
**Terminal.Gui** supports key up/down events, but not all [Console Drivers](drivers.md) do. To receive key up/down events, you must use a driver that supports them (e.g. `WindowsDriver`).