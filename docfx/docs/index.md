# Terminal.Gui v2 Overview

 A toolkit for building rich Terminal User Interface (TUI) apps with .NET that run on Windows, the Mac, and Linux/Unix.

 (This is the v2 API documentation. For v1 go here: https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui.html)

## Features

* **[Cross Platform](drivers.md)** - Windows, Mac, and Linux. Terminal drivers for Curses, Windows, and the .NET Console mean apps will work well on both color and monochrome terminals. Apps also work over SSH.
* **[Templates](getting-started.md)** - The `dotnet new` command can be used to create a new Terminal.Gui app.
* **[Extensible UI](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.View.html)** - All visible UI elements are subclasses of the `View` class, and these in turn can contain an arbitrary number of sub-views. Dozens of [Built-in Views](views.md) are provided.
* **[Keyboard](keyboard.md) and [Mouse](mouse.md) Input** - The library handles all the details of input processing and provides a simple event-based API for applications to consume.
* **[Powerful Layout Engine](layout.md)** - The layout engine makes it easy to lay out controls relative to each other and enables dynamic terminal UIs. 
* **[Machine, User, and App-Level Configuration](config.md)** - Persistent configuration settings, including overriding default look & feel with Themes, keyboard bindings, and more via the [ConfigurationManager](~/api/Terminal.Gui.ConfigurationManager.yml) class.
* **[Clipboard support](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.Clipboard.html)** - Cut, Copy, and Paste is provided through the [`Clipboard`] class.
* **Multi-tasking** - The [Mainloop](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.MainLoop.html) supports processing events, idle handlers, and timers. Most classes are safe for threading.
* **[Reactive Extensions](https://github.com/dotnet/reactive)** - Use reactive extensions and benefit from increased code readability, and the ability to apply the MVVM pattern and [ReactiveUI](https://www.reactiveui.net/) data bindings. See the [source code](https://github.com/gui-cs/Terminal.GuiV2Docs/tree/master/ReactiveExample) of a sample app.

See [What's New in V2 For more](newinv2.md).

## Conceptual Documentation

* [Arrangement API](arrangement.md)
* [Configuration and Theme Manager](config.md)
* [Cursor Deep Dive](cursor.md)
* [Cross-platform Driver Model](drivers.md)
* [Dim.Auto Deep Dive](dimauto.md)
* [Drawing](drawing.md)
* [Events Deep Dive](events.md)
* [Keyboard API](keyboard.md)
* [Layout Engine](layout.md)
* [Migrating from Terminal.Gui v1](migratingfromv1.md)
* [Mouse API](mouse.md)
* [Multi-tasking and the Application Main Loop](mainloop.md)
* [Navigation](navigation.md)
* [View Deep Dive](View.md)
* [Views](views.md)
* [Scrolling Deep Dive](scrolling.md)
* [TableView Deep Dive](tableview.md)
* [TreeView Deep Dive](treeview.md)

The simplest application looks like this:

```csharp
using Terminal.Gui;
Application.Init ();
var n = MessageBox.Query (50, 5, "Question", "Do you like TUI apps?", "Yes", "No");
Application.Shutdown ();
return n;
```

This example shows a prompt and returns an integer value depending on which value was selected by the user.

More interesting user interfaces can be created by composing some of the various `View` classes that are included. 

In the example above, @Terminal.Gui.Application.Init(Terminal.Gui.IConsoleDriver,System.String) sets up the environment, initializes the color schemes, and clears the screen to start the application.

The [Application](~/api/Terminal.Gui.Application.yml) class additionally creates an instance of the [Toplevel](~/api/Terminal.Gui.Toplevel.yml) View available in the `Application.Top` property, and can be used like this:

```csharp
using Terminal.Gui;
Application.Init ();

var label = new Label () {
    Title = "Hello World",
    X = Pos.Center (),
    Y = Pos.Center (),
    Height = 1,
};

var app = new Toplevel ();
app.Add (label);
Application.Run (app);
app.Dispose ();
Application.Shutdown ();
```

This example includes a menu bar at the top of the screen and a button that shows a message box when clicked:

```csharp
using Terminal.Gui;

Application.Init ();
var menu = new MenuBar (new MenuBarItem [] {
    new MenuBarItem ("_File", new MenuItem [] {
        new MenuItem ("_Quit", "", () => { 
            Application.RequestStop (); 
        })
    }),
});

var button = new Button () {
    Title = "_Hello",
    X = 0,
    Y = Pos.Bottom (menu),
    Width = Dim.Fill (),
    Height = Dim.Fill () - 1
};
button.Accepting += () => {
    MessageBox.Query (50, 5, "Hi", "Hello World! This is a message box", "Ok");
};

var app = new Toplevel ();
// Add both menu and win in a single call
top.Add (menu, button);
Application.Run (top);
top.Dispose ();
Application.Shutdown ();
```

## Views

All visible elements in a Terminal.Gui application are implemented as
[Views](~/api/Terminal.Gui.View.yml). Views are self-contained objects that take care of displaying themselves, can receive keyboard and mouse input and participate in the focus mechanism.

See the full list of [Views provided by the Terminal.Gui library here](views.md).

Every view can contain an arbitrary number of child views, called `SubViews`. Call @Terminal.Gui.View.Add(View) to add a couple of buttons to a UI:

```csharp
void SetupMyView (View myView)
{
    var label = new Label () {
        Title = "_Username:"
        X = 1,
        Y = 1,
        Width = 20,
        Height = 1
    };
    myView.Add (label);

    var username = new TextField () {
        X = Pos.Right (label) + 1,
        Y = 2,
        Width = 30,
        Height = 1
    };
    myView.Add (username);
}
```

The container of a given view is called the `SuperView` and it is a property of every View.

## Modal Views

Views can either be Modal or Non-modal. Modal views take over all user input until the user closes the View. Examples of Modal Views are Toplevel, Dialog, and Wizard. Non-modal views can be used to create a new experience in your application, one where you would have a new top-level menu for example. Setting the `Modal` property on a View to `true` makes it modal.

To run any View (but especially Dialogs, Windows, or Toplevels) modally, invoke the `Application.Run` method on a Toplevel. Use the `Application.RequestStop()` method to terminate the modal execution.

```csharp

```

There is no return value from running modally, so the modal view must have a mechanism to indicate the reason the modal was closed. In the case above, the `okpressed` value is set to true if the user pressed or selected the `Ok` button.

## Windows

[Window](~/api/Terminal.Gui.Window.yml) is a view used in `Overlapped` layouts, providing a frame and a title - and can be moved and sized with the keyboard or mouse.

## Dialogs

[Dialogs](~/api/Terminal.Gui.Dialog.yml) are Modal [Windows](~/api/Terminal.Gui.Window.yml) that are centered in the middle of the screen and are intended to be used modally - that is, they run, and they are expected to return a result before resuming execution of the application.

Dialogs expose an API for adding buttons and managing the layout such that buttons are at the bottom of the dialog (e.g. [`AddButton`](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui.Dialog.yml#Terminal_Gui_Dialog_AddButton_Terminal_Gui_Button_)).

Example:
```csharp
bool okpressed = false;
var ok = new Button() { Title = "Ok" };
var cancel = new Button() { Title = "Cancel" };
var dialog = new Dialog () { Text = "Are you sure you want to quit?", Title = "Quit", Buttons = { ok, cancel } };
```

Which will show something like this:

```
+- Quit -----------------------------------------------+
|            Are you sure you want to quit?            |
|                                                      |
|                  [ Ok ] [ Cancel ]                   |
+------------------------------------------------------+
```

## Wizards

[Wizards](~/api/Terminal.Gui.Wizard.yml) are Dialogs that let users step through a series of steps to complete a task. 

```
╔╡Gandolf - The last step╞════════════════════════════════════╗
║                                     The wizard is complete! ║
║☐ Enable Final Final Step                                    ║
║                                     Press the Finish        ║
║                                     button to continue.     ║
║                                                             ║
║                                     Pressing ESC will       ║
║                                     cancel the wizard.      ║
║                                                             ║
║                                                             ║
║─────────────────────────────────────────────────────────────║
║⟦ Back ⟧                                         ⟦► Finish ◄⟧║
╚═════════════════════════════════════════════════════════════╝
```
