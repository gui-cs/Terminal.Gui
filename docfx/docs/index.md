# Terminal.Gui v2 Overview

 A toolkit for building rich Terminal User Interface (TUI) apps with .NET that run on Windows, the Mac, and Linux/Unix.

## Features

* **[Cross Platform](drivers.md)** - Windows, Mac, and Linux. Terminal drivers for Curses, Windows, and the .NET Console mean apps will work well on both color and monochrome terminals. Apps also work over SSH.
* **[Templates](getting-started.md)** - The `dotnet new` command can be used to create a new Terminal.Gui app.
* **[Keyboard](keyboard.md) and [Mouse](mouse.md) Input** - The library handles all the details of input processing and provides a simple event-based API for applications to consume.
* **[Extensible Widgets](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.View.html)** - All visible UI elements are subclasses of the `View` class, and these in turn can contain an arbitrary number of sub-views. Dozens of [Built-in Views](views.md) are provided.
* **[Flexible Layout](layout.md)** - *Computed Layout* makes it easy to lay out controls relative to each other and enables dynamic terminal UIs. *Absolute Layout* allows for precise control over the position and size of controls.
* **[Clipboard support](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.Clipboard.html)** - Cut, Copy, and Paste is provided through the [`Clipboard`] class.
* **Advanced App Features** - The [Mainloop](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.MainLoop.html) supports processing events, idle handlers, and timers. Most classes are safe for threading.
* **[Reactive Extensions](https://github.com/dotnet/reactive)** - Use reactive extensions and benefit from increased code readability, and the ability to apply the MVVM pattern and [ReactiveUI](https://www.reactiveui.net/) data bindings. See the [source code](https://github.com/gui-cs/Terminal.GuiV2Docs/tree/master/ReactiveExample) of a sample app.

## Conceptual Documentation

* [List of Views](views.md)
* [Keyboard Event Processing](keyboard.md)
* [Event Processing and the Application Main Loop](mainloop.md)
* [Cross-platform Driver Model](drivers.md)
* [Configuration and Theme Manager](config.md)
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

This example shows a prompt and returns an integer value depending on
which value was selected by the user.

More interesting user interfaces can be created by composing some of
the various `View` classes that are included. In the following sections, you
will see how applications are put together.

In the example above, you can see that we have initialized the runtime by calling [Applicaton.Init](~/api/Terminal.Gui.Application.yml#Terminal_Gui_Application_Init_Terminal_Gui_ConsoleDriver_) - this sets up the environment, initializes the color schemes, and clears the screen to start the application.

The [Application](~/api/Terminal.Gui.Application.yml) class additionally creates an instance of the [Toplevel](~/api/Terminal.Gui.Toplevel.yml) View available in the `Application.Top` property, and can be used like this:

```csharp
using Terminal.Gui;
Application.Init ();

var label = new Label ("Hello World") {
    X = Pos.Center (),
    Y = Pos.Center (),
    Height = 1,
};

Application.Top.Add (label);
Application.Run ();
Application.Shutdown ();
```

Typically, you will want your application to have more than a label, you might
want a menu and a button for example. the following code does this:

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

var button = new Button ("_Hello") {
    X = 0,
    Y = Pos.Bottom (menu),
    Width = Dim.Fill (),
    Height = Dim.Fill () - 1
};
button.Clicked += () => {
    MessageBox.Query (50, 5, "Hi", "Hello World! This is a message box", "Ok");
};

// Add both menu and win in a single call
Application.Top.Add (menu, button);
Application.Run ();
Application.Shutdown ();
```

## Views

All visible elements in a Terminal.Gui application are implemented as
[Views](~/api/Terminal.Gui.View.yml). Views are self-contained objects that take care of displaying themselves, can receive keyboard and mouse input and participate in the focus mechanism.

See the full list of [Views provided by the Terminal.Gui library here](views.md).

Every view can contain an arbitrary number of children views, called `SubViews`.Call the
[View.Add](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_Add_Terminal_Gui_View_) method to add a couple of buttons to a UI:

```csharp
void SetupMyView (View myView)
{
    var label = new Label ("Username: ") {
        X = 1,
        Y = 1,
        Width = 20,
        Height = 1
    };
    myView.Add (label);

    var username = new TextField ("") {
        X = 1,
        Y = 2,
        Width = 30,
        Height = 1
    };
    myView.Add (username);
}
```

The container of a given view is called the `SuperView` and it is a property of every
View.

## Layout

Terminal.Gui v2 supports the following View layout systems (controlled by the [View.LayoutStyle](~/api/Terminal.Gui.LayoutStyle.yml)):

* **Absolute** - Used to have the View positioned exactly in a location, with a fixed size. Absolute layout is accomplished by constructing a View with an argument of type [Rect](~/api/Terminal.Gui.Rect.yml) or directly changing the `Frame` property on the View.
* **Computed** - The Computed Layout system provides automatic aligning of Views with other Views, automatic centering, and automatic sizing. To use Computed layout set the 
 `X`, `Y`, `Width` and `Height` properties after the object has been created. Views laid out using the Computed Layout system can be resized with the mouse or keyboard, enabling tiled window managers and dynamic terminal UIs.
* **Overlapped** - New in V2 (But not yet) - Overlapped layout enables views to be positioned on top of each other. Overlapped Views are movable and sizable with both the keyboard and the mouse.

See the full [Layout documentation here](layout.md).

## Modal Views

Views can either be Modal or Non-modal. Modal views take over all user input until the user closes the View. Examples of Modal Views are Toplevel, Dialog, and Wizard. Non-modal views can be used to create a new experience in your application, one where you would have a new top-level menu for example. Setting the `Modal` property on a View to `true` makes it modal.

### Windows

[Window](~/api/Terminal.Gui.Window.yml) is a view used in Overlapped layouts, providing a frame and a title - and can be moved and sized with the keyboard or mouse.

### Dialogs

[Dialogs](~/api/Terminal.Gui.Dialog.yml) are Modal [Windows](~/api/Terminal.Gui.Window.yml) that are centered in the middle of the screen and are intended to be used modally - that is, they run, and they are expected to return a result before resuming execution of the application.

Dialogs expose the 
[`AddButton`](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui.Dialog.yml#Terminal_Gui_Dialog_AddButton_Terminal_Gui_Button_) API which manages the layout
of any button passed to it, ensuring that the buttons are at the bottom of the dialog.

Example:
```csharp
bool okpressed = false;
var ok = new Button("Ok");
var cancel = new Button("Cancel");
var dialog = new Dialog ("Quit", 60, 7, ok, cancel);
```

Which will show something like this:
```
+- Quit -----------------------------------------------+
|                                                      |
|                                                      |
|                  [ Ok ] [ Cancel ]                   |
+------------------------------------------------------+
```

### Wizards

[Wizards](~/api/Terminal.Gui.Wizard.yml) are Dialogs that let users step through a series of steps to complete a task. 

### Running Modally

To run any View (but especially Dialogs, Windows, or Toplevels) modally, invoke the `Application.Run` method on a Toplevel. Use the `Application.RequestStop()` method to terminate the modal execution.

```csharp
bool okpressed = false;
var ok = new Button(3, 14, "Ok") { 
    Clicked = () => { Application.RequestStop (); okpressed = true; }
};
var cancel = new Button(10, 14, "Cancel") {
    Clicked = () => Application.RequestStop () 
};
var dialog = new Dialog ("Login", 60, 18, ok, cancel);

var entry = new TextField () {
    X = 1, 
    Y = 1,
    Width = Dim.Fill (),
    Height = 1
};
dialog.Add (entry);
Application.Run (dialog);
if (okpressed)
    Console.WriteLine ("The user entered: " + entry.Text);
```

There is no return value from running modally, so the modal view must have a mechanism
of indicating the reason the modal was closed. In the 
case above, the `okpressed` value is set to true if the user pressed or selected the Ok button.

## Input Handling

Every view has a focused view, and if that view has nested SubViews, one of those is 
the focused view. This is called the focus chain, and at any given time, only one
View has the [Focus](). 

The library provides a default focus mechanism that can be used to navigate the focus chain. The default focus mechanism is based on the Tab key, and the Shift-Tab key combination

Keyboard processing details are available on the [Keyboard Event Processing](keyboard.md) document.

## Colors and Color Schemes

All views have been configured with a color scheme that will work both in color
terminals as well as the more limited black and white terminals. 

The various styles are captured in the [Colors](~/api/Terminal.Gui.Colors.yml) class which defines color schemes for Toplevel, the normal views (Base), the menu bar, dialog boxes, and error UI::

* `Colors.Toplevel`
* `Colors.Base`
* `Colors.Menu`
* `Colors.Dialog`
* `Colors.Error`

You can use them for example like this to set the colors for a new Window:

```
var w = new Window ("Hello");
w.ColorScheme = Colors.Error
```

ColorSchemes can be configured with the [Configuration and Theme Manager](config.md). 

The [ColorScheme](~/api/Terminal.Gui.ColorScheme.yml) represents
four values, the color used for Normal text, the color used for normal text when
a view is focused an the colors for the hot-keys both in focused and unfocused modes.

By using `ColorSchemes` you ensure that your application will work correctbly both
in color and black and white terminals.

Some views support setting individual color attributes, you create an
attribute for a particular pair of Foreground/Background like this:

```
var myColor = Application.Driver.MakeAttribute (Color.Blue, Color.Red);
var label = new Label (...);
label.TextColor = myColor
```

Learn more about colors in the [Color](color.md) overview.

## MainLoop, Threads and Input Handling

The Main Loop, threading, and timers are described on the [Event Processing and the Application Main Loop](~/docs/mainloop.md) document.

## Cross-Platform Drivers

See [Cross-platform Driver Model](drivers.md).
