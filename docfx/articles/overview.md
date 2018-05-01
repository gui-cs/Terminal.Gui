Overview
========

`Terminal.Gui` is a library intended to create console-based
applications using C#.  The framework has been designed to make it
easy to write applications that will work on monochrome terminals, as
well as modern color terminals with mouse support.

This library works across Windows, Linux and MacOS.

This library provides a text-based toolkit as works in a way similar
to graphic toolkits.   There are many controls that can be used to
create your applications and it is event based, meaning that you
create the user interface, hook up various events and then let the
a processing loop run your application, and your code is invoked via
one or more callbacks.

The simplest application looks like this:

```
using Terminal.Gui;

class Demo {
    static int Main ()
    {
        Application.Init ();

	var n = MessageBox.Query (50, 7, "Question", "Do you like console apps?", "Yes", "No");

	return n;
    }
}
```

This example shows a prompt and returns an integer value depending on
which value was selected by the user (Yes, No, or if they use chose
not to make a decision and instead pressed the ESC key).

More interesting user interfaces can be created by composing some of
the various views that are included.   In the following sections, you
will see how applications are put together.

In the example above, you can see that we have initialized the runtime by calling the 
[`Init`](../api/Terminal.Gui/Terminal.Gui.Application.html#Terminal_Gui_Application_Init) method in the Application class - this sets up the environment, initializes the color
schemes available for your application and clears the screen to start your application.

The [`Application`](../api/Terminal.Gui/Terminal.Gui.Application.html) class, additionally creates an instance of the [Toplevel]((../api/Terminal.Gui/Terminal.Gui.Toplevel.html) class that is ready to be consumed, 
this instance is available in the `Application.Top` property, and can be used like this:

```
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
    }
}
```

Typically, you will want your application to have more than a label, you might
want a menu, and a region for your application to live in, the following code
does this:

```
using Terminal.Gui;

class Demo {
    static int Main ()
    {
        Application.Init ();
        var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Quit", "", () => { Application.Top.Running = false; })
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
    }
}
```

Views
=====

All visible elements on a Terminal.Gui application are implemented as
[Views](../api/Terminal.Gui/Terminal.Gui.View.html).   Views are self-contained
objects that take care of displaying themselves, can receive keyboard and mouse
input and participate in the focus mechanism.

Every view can contain an arbitrary number of children views.   These are called
the Subviews.   You can add a view to an existing view, by calling the 
[`Add`](../api/Terminal.Gui/Terminal.Gui.View.html#Terminal_Gui_View_Add_Terminal_Gui_View_) method, for example, to add a couple of buttons to a UI, you can do this:

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
    }
    myView.Add (username);
}
```

The container of a given view is called the `SuperView` and it is a property of every
View.

Among the many kinds of views, you typically will create a [Toplevel](../api/Terminal.Gui/Terminal.Gui.Toplevel.html) view or a [Window]
(../api/Terminal.Gui/Terminal.Gui.Window.html) which are special kinds of views
that can be executed modally - that is, the view can take over all input and returns
only when the user chooses to complete their work there.   

Modal views take over all the event processing, and do not let other views
receive any events while they are running.

There are many views that you can use to spice up your application:

* [Buttons](../api/Terminal.Gui/Terminal.Gui.Button.html) 
* [Labels](../api/Terminal.Gui/Terminal.Gui.Label.html)
* [Text entry](../api/Terminal.Gui/Terminal.Gui.TextField.html)
* [Text view](../api/Terminal.Gui/Terminal.Gui.TextView.html)
* [Radio buttons](../api/Terminal.Gui/Terminal.Gui.RadioGroup.html)
* [Checkboxes](../api/Terminal.Gui/Terminal.Gui.CheckBox.html)
* [Dialog boxes](../api/Terminal.Gui/Terminal.Gui.Dialog.html)
  * [Message boxes](../api/Terminal.Gui/Terminal.Gui.MessageBox.html)
* [Windows](../api/Terminal.Gui/Terminal.Gui.Window.html)
* [Menus](../api/Terminal.Gui/Terminal.Gui.MenuBar.html)
* [ListViews](../api/Terminal.Gui/Terminal.Gui.ListView.html)
* [Frames](../api/Terminal.Gui/Terminal.Gui.FrameView.html)
* [ProgressBars](../api/Terminal.Gui/Terminal.Gui.ProgressBar.html)
* [Scroll views](../api/Terminal.Gui/Terminal.Gui.ScrollView.html) and [Scrollbars](../api/Terminal.Gui/Terminal.Gui.ScrollBarView.html)

Dialogs
=======

Dialogs are instances of a Window that are centered in the screen, and are intended
to be used modally - that is, they run, and they are expected to return a result 
before resuming execution of your application.

Dialogs are a subclass of `Window` and additionally expose the 
[`AddButton`](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Dialog.html#Terminal_Gui_Dialog_AddButton_Terminal_Gui_Button_) API which manages the layout
of any button passed to it, ensuring that the buttons are at the bottom of the dialog.


Input Handling
==============

Every view has a focused view, and if that view has nested views, one of those is 
the focused view.   This is called the focus chain, and at any given time, only one
View has the focus.   

The library binds the key Tab to focus the next logical view,
and the Shift-Tab combination to focus the previous logical view.   

Keyboard processing is divided in three stages: HotKey processing, regular processing and
cold key processing.   

* Hot key processing happens first, and it gives all the views in the current
  toplevel a chance to monitor whether the key needs to be treated specially.  This
  for example handles the scenarios where the user pressed Alt-o, and a view with a 
  highlighted "o" is being displayed.

* If no view processed the hotkey, then the key is sent to the currently focused
  view.

* If the key was not processed by the normal processing, all views are given 
  a chance to process the keystroke in their cold processing stage.  Examples
  include the processing of the "return" key in a dialog when a button in the
  dialog has been flagged as the "default" action.

The most common case is the normal processing, which sends the keystrokes to the
currently focused view.

Mouse events are processed in visual order, and the event will be sent to the
view on the screen.   The only exception is that no mouse events are delivered
to background views when a modal view is running.   

Color Schemes
=============

All views have been configured with a color scheme that will work both in color
terminals as well as the more limited black and white terminals.   

The various styles are captured in the [`Colors`](../api/Terminal.Gui/Terminal.Gui.Colors.html) class which defined color schemes for
the normal views, the menu bar, popup dialog boxes and error dialog boxes.

The [`ColorScheme`](../api/Terminal.Gui/Terminal.Gui.ColorScheme.html) represents
four values, the color used for Normal text, the color used for normal text when
a view is focused an the colors for the hot-keys both in focused and unfocused modes.

By using `ColorSchemes` you ensure that your application will work correctbly both
in color and black and white terminals.


