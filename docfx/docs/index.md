# Terminal.Gui v2 Overview

 A toolkit for building rich console apps for .NET, .NET Core, and Mono that works on Windows, the Mac, and Linux/Unix.

## Conceptual Documentation

* [List of Views](views.md)
* [Keyboard Event Processing](keyboard.md)
* [Event Processing and the Application Main Loop](mainloop.md)
* [Cross-platform Driver Model](drivers.md)
* [Configuration and Theme Manager](config.md)
* [TableView Deep Dive](tableview.md)
* [TreeView Deep Dive](treeview.md)

## Features

* **Cross Platform** - Windows, Mac, and Linux. Terminal drivers for Curses, [Windows Console](https://github.com/gui-cs/Terminal.GuiV2Docs/issues/27), and the .NET Console mean apps will work well on both color and monochrome terminals. 
* **Keyboard and Mouse Input** - Both keyboard and mouse input are supported, including support for drag & drop.
* **[Flexible Layout](https://gui-cs.github.io/Terminal.GuiV2Docs/docs/overview.html#layout)** - Supports both *Absolute layout* and an innovative *Computed Layout* system. *Computed Layout* makes it easy to lay out controls relative to each other and enables dynamic terminal UIs.
* **Clipboard support** - Cut, Copy, and Paste of text provided through the [`Clipboard`](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.Clipboard.html) class.
* **[Arbitrary Views](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.View.html)** - All visible UI elements are subclasses of the `View` class, and these in turn can contain an arbitrary number of sub-views.
* **Advanced App Features** - The [Mainloop](https://gui-cs.github.io/Terminal.GuiV2Docs/api/Terminal.Gui.MainLoop.html) supports processing events, idle handlers, timers, and monitoring file
descriptors. Most classes are safe for threading.
* **Reactive Extensions** - Use [reactive extensions](https://github.com/dotnet/reactive) and benefit from increased code readability, and the ability to apply the MVVM pattern and [ReactiveUI](https://www.reactiveui.net/) data bindings. See the [source code](https://github.com/gui-cs/Terminal.GuiV2Docs/tree/master/ReactiveExample) of a sample app in order to learn how to achieve this.



`Terminal.Gui` is a library intended to create console-based
applications using C#. The framework has been designed to make it
easy to write applications that will work on monochrome terminals, as
well as modern color terminals with mouse support.

This library works across Windows, Linux and MacOS.

This library provides a text-based toolkit as works in a way similar
to graphic toolkits. There are many controls that can be used to
create your applications and it is event based, meaning that you
create the user interface, hook up various events and then let the
a processing loop run your application, and your code is invoked via
one or more callbacks.

The simplest application looks like this:

```csharp
using Terminal.Gui;

class Demo {
    static int Main ()
    {
        Application.Init ();

        var n = MessageBox.Query (50, 7, 
            "Question", "Do you like console apps?", "Yes", "No");
            
		Application.Shutdown ();
        return n;
    }
}
```

This example shows a prompt and returns an integer value depending on
which value was selected by the user (Yes, No, or if they use chose
not to make a decision and instead pressed the ESC key).

More interesting user interfaces can be created by composing some of
the various views that are included. In the following sections, you
will see how applications are put together.

In the example above, you can see that we have initialized the runtime by calling  
[Applicaton.Init](~/api/Terminal.Gui.Application.yml#Terminal_Gui_Application_Init_Terminal_Gui_ConsoleDriver_) method in the Application class - this sets up the environment, initializes the color
schemes available for your application and clears the screen to start your application.

The [Application](~/api/Terminal.Gui.Application.yml) class, additionally creates an instance of the [Toplevel](~/api/Terminal.Gui.Toplevel.yml) class that is ready to be consumed, 
this instance is available in the `Application.Top` property, and can be used like this:

```csharp
using Terminal.Gui;

class Demo {
    static int Main ()
    {
        Application.Init ();

        var label = new Label ("Hello World") {
            X = Pos.Center (),
            Y = Pos.Center (),
            Height = 1,
        };
        Application.Top.Add (label);
        Application.Run ();
        Application.Shutdown ();
    }
}
```

Typically, you will want your application to have more than a label, you might
want a menu, and a region for your application to live in, the following code
does this:

```csharp
using Terminal.Gui;

class Demo {
    static int Main ()
    {
        Application.Init ();
        var menu = new MenuBar (new MenuBarItem [] {
            new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem ("_Quit", "", () => { 
                    Application.RequestStop (); 
                })
            }),
        });
        
        var win = new Window ("Hello") {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 1
        };

        // Add both menu and win in a single call
        Application.Top.Add (menu, win);
        Application.Run ();
        Application.Shutdown ();
    }
}
```

## Views

All visible elements on a Terminal.Gui application are implemented as
[Views](~/api/Terminal.Gui.View.yml). Views are self-contained objects that take care of displaying themselves, can receive keyboard and mouse input and participate in the focus mechanism.

See the full list of [Views provided by the Terminal.Gui library here](views.md).

Every view can contain an arbitrary number of children views. These are called
the Subviews. You can add a view to an existing view, by calling the 
[Add](~/api/Terminal.Gui.View.yml#Terminal_Gui_View_Add_Terminal_Gui_View_) method, for example, to add a couple of buttons to a UI, you can do this:

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

Terminal.Gui supports two different layout systems, absolute and computed \
(controlled by the [LayoutStyle](~/api/Terminal.Gui.LayoutStyle.yml)
property on the view.

The absolute system is used when you want the view to be positioned exactly in
one location and want to manually control where the view is. This is done
by invoking your View constructor with an argument of type [Rect](~/api/Terminal.Gui.Rect.yml). When you do this, to change the position of the View, you can change the `Frame` property on the View.

The computed layout system offers a few additional capabilities, like automatic
centering, expanding of dimensions and a handful of other features. To use
this you construct your object without an initial `Frame`, but set the 
 `X`, `Y`, `Width` and `Height` properties after the object has been created.

Examples:

```csharp

// Dynamically computed
var label = new Label ("Hello") {
    X = 1,
    Y = Pos.Center (),
    Width = Dim.Fill (),
    Height = 1
};

// Absolute position using the provided rectangle
var label2 = new Label (new Rect (1, 2, 20, 1), "World")
```

The computed layout system does not take integers, instead the `X` and `Y` properties are of type [Pos](~/api/Terminal.Gui.Pos.yml) and the `Width` and `Height` properties are of type [Dim](~/api/Terminal.Gui.Dim.yml) both which can be created implicitly from integer values.

### The `Pos` Type

The `Pos` type on `X` and `Y` offers a few options:
* Absolute position, by passing an integer
* Percentage of the parent's view size - `Pos.Percent(n)`
* Anchored from the end of the dimension - `AnchorEnd(int margin=0)`
* Centered, using `Center()`
* Reference the Left (X), Top (Y), Bottom, Right positions of another view

The `Pos` values can be added or subtracted, like this:

```csharp
// Set the X coordinate to 10 characters left from the center
view.X = Pos.Center () - 10;

view.Y = Pos.Percent (20);

anotherView.X = AnchorEnd (10);
anotherView.Width = 9;

myView.X = Pos.X (view);
myView.Y = Pos.Bottom (anotherView);
```

### The `Dim` Type

The `Dim` type is used for the `Width` and `Height` properties on the View and offers
the following options:

* Absolute size, by passing an integer
* Percentage of the parent's view size - `Dim.Percent(n)`
* Fill to the end - `Dim.Fill ()`
* Reference the Width or Height of another view

Like, `Pos`, objects of type `Dim` can be added an subtracted, like this:


```csharp
// Set the Width to be 10 characters less than filling 
// the remaining portion of the screen
view.Width = Dim.Fill () - 10;

view.Height = Dim.Percent(20) - 1;

anotherView.Height = Dim.Height (view)+1
```

## TopLevels, Windows and Dialogs.

Among the many kinds of views, you typically will create a [Toplevel](~/api/Terminal.Gui.Toplevel.yml) view (or any of its subclasses), like [Window](~/api/Terminal.Gui.Window.yml) or [Dialog](~/api/Terminal.Gui.Dialog.yml) which is special kind of views that can be executed modally - that is, the view can take over all input and returns
only when the user chooses to complete their work there. 

The following sections cover the differences.

### TopLevel Views

[Toplevel](~/api/Terminal.Gui.Toplevel.yml) views have no visible user interface elements and occupy an arbitrary portion of the screen.

You would use a toplevel Modal view for example to launch an entire new experience in your application, one where you would have a new top-level menu for example. You 
typically would add a Menu and a Window to your Toplevel, it would look like this:

```csharp
using Terminal.Gui;

class Demo {
    static void Edit (string filename)
    {
        var top = new Toplevel () { 
            X = 0, 
            Y = 0, 
            Width = Dim.Fill (), 
            Height = Dim.Fill () 
        };
        var menu = new MenuBar (new MenuBarItem [] {
            new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem ("_Close", "", () => { 
                    Application.RequestStop ();
                })
            }),
        });
        
        // nest a window for the editor
        var win = new Window (filename) {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 1
        };

        var editor = new TextView () {
            X = 0, 
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        editor.Text = System.IO.File.ReadAllText (filename);
        win.Add (editor);

        // Add both menu and win in a single call
        top.Add (win, menu);
        Application.Run (top);
        Application.Shutdown ();
    }
}
```

### Window Views

[Window](~/api/Terminal.Gui.Window.yml) views extend the Toplevel view by providing a frame and a title around the toplevel - and can be moved on the screen with the mouse (caveat: code is currently disabled)

From a user interface perspective, you might have more than one Window on the screen at a given time.

### Dialogs

[Dialog](~/api/Terminal.Gui.Dialog.yml) are [Window](~/api/Terminal.Gui.Window.yml) objects that happen to be centered in the middle of the screen.

Dialogs are instances of a Window that are centered in the screen, and are intended
to be used modally - that is, they run, and they are expected to return a result 
before resuming execution of your application.

Dialogs are a subclass of `Window` and additionally expose the 
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

### Running Modally

To run your Dialog, Window or Toplevel modally, you will invoke the `Application.Run`
method on the toplevel. It is up to your code and event handlers to invoke the `Application.RequestStop()` method to terminate the modal execution.

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

There is no return value from running modally, so your code will need to have a mechanism
of indicating the reason that the execution of the modal dialog was completed, in the 
case above, the `okpressed` value is set to true if the user pressed or selected the Ok button.

## Input Handling

Every view has a focused view, and if that view has nested views, one of those is 
the focused view. This is called the focus chain, and at any given time, only one
View has the focus. 

The library binds the key Tab to focus the next logical view, and the Shift-Tab combination to focus the previous logical view. 

Keyboard processing details are available on the [Keyboard Event Processing](keyboard.md) document.

## Colors and Color Schemes

All views have been configured with a color scheme that will work both in color
terminals as well as the more limited black and white terminals. 

The various styles are captured in the [Colors](~/api/Terminal.Gui.Colors.yml) class which defined color schemes for
the toplevel, the normal views, the menu bar, popup dialog boxes and error dialog boxes, that you can use like this:

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

## MainLoop, Threads and Input Handling

Detailed description of the mainloop is described on the [Event Processing and the Application Main Loop](~/docs/mainloop.md) document.

## Cross-Platform Drivers

See [Cross-platform Driver Model](drivers.md).
