[![Build Status](https://travis-ci.org/migueldeicaza/gui.cs.svg?branch=master)](https://travis-ci.org/migueldeicaza/gui.cs)
[![Version](https://img.shields.io/nuget/v/Terminal.Gui.svg)](https://www.nuget.org/packages/Terminal.Gui)
[![Downloads](https://img.shields.io/nuget/dt/Terminal.Gui)](https://www.nuget.org/packages/Terminal.Gui)
[![License](https://img.shields.io/github/license/migueldeicaza/gui.cs.svg)](LICENSE)

[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/mono/mono?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) - This is the Mono Channel room

# Terminal.Gui - Terminal UI toolkit for .NET

A simple UI toolkit for .NET, .NET Core, and Mono that works on Windows, the Mac, and Linux/Unix.

![Sample app](https://raw.githubusercontent.com/migueldeicaza/gui.cs/master/docfx/sample.png)

A presentation of this was part of the [Retro.NET](https://channel9.msdn.com/Events/dotnetConf/2018/S313) talk at .NET Conf 2018 [Slides](https://tirania.org/Retro.pdf)

## Controls 
The toolkit contains various controls for building text user interfaces:

* [Buttons](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Button.html) 
* [Checkboxes](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.CheckBox.html)
* [Dialog boxes](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Dialog.html)
* [Frames](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.FrameView.html)
* [Hex viewer/editor](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.HexView.html)
* [Labels](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Label.html)
* [ListViews](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ListView.html)
* [Menus](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.MenuBar.html)
* [Message boxes](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.MessageBox.html)
* [ProgressBars](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ProgressBar.html)
* [Time editing field](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TimeField.html)
* [Text entry](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TextField.html)
* [Text view](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TextView.html)
* [Scroll views](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ScrollView.html)
* [Scrollbars](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ScrollBarView.html)
* [Status bars]()
* [Radio buttons](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.RadioGroup.html)
* [Windows](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Window.html)

In addition, a complete Xterm/Vt100 terminal emulator that you can embed is now part of [XtermSharp](https://github.com/migueldeicaza/XtermSharp/blob/master/GuiCsHost/TerminalView.cs) - you just need to pull the `TerminalView` linked here into your project.

## Features

* **Cross Platform** - Terminal drivers for Curses, [Windows Console](https://github.com/migueldeicaza/gui.cs/issues/27), and the .NET Console mean **Terminal.Gui** works well on both color and monochrome terminals and has mouse support on terminal emulators that support it.
* **Keyboard and Mouse Input** - Both keyboard and mouse input are supported, including limited support for drag & drop.
* **[Flexible Layout](https://migueldeicaza.github.io/gui.cs/articles/overview.html#layout)** - **Terminal.Gui** supports both *Absolute layout* and an innovative UI layout system referred to as *Computed Layout*. *Computed Layout* makes it easy to layout controls relative to each other and enables dynamic console GUIs.
* **Clipboard support** - Cut, Copy, and Paste of text provided through the `[Clipboard](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Clipboard.html)` class.
* **[Arbitrary Views](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.View.html)** - All visible UI elements are subclasses of the `View` class, and these in turn can contain an arbitrary number of sub-views.
* **Advanced App Features** - The [Mainloop](https://migueldeicaza.github.io/gui.cs/api/Mono.Terminal/Mono.Terminal.MainLoop.html) supports processing events, idle handlers, timers, and monitoring file
descriptors.

### Keyboard Input Handling

The input handling of **Terminal.Gui** is similar in some ways to Emacs and the Midnight Commander, so you can expect some of the special key combinations to be active.

The key `ESC` can act as an Alt modifier (or Meta in Emacs parlance), to allow input on terminals that do not have an alt key.  So to produce the sequence `Alt-F`, you can press either `Alt-F`, or `ESC` followed by the key `F`.

To enter the key `ESC`, you can either press `ESC` and wait 100 milliseconds, or you can press `ESC` twice.

`ESC-0`, and `ESC-1` through `ESC-9` have a special meaning, they map to `F10`, and `F1` to `F9` respectively.

**Terminal.Gui** respects common Mac and Windows keyboard idoms as well. For example, clipboard operations use the familiar `Control/Command-C, X, V` model.

`CTRL-Q` is used for exiting views (and apps).

### Driver model

Currently **Terminal.Gui** has support for `[ncurses](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/Drivers/CursesDriver.cs)`, [`System.Console`](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/Drivers/NetDriver.cs), and a full [Win32 Console](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/Drivers/WindowsDriver.cs) front-end.

`ncurses` is used on Mac/Linux/Unix with color support based on what your library is compiled with; the Windows driver supports full color and mouse, and an easy-to-debug `System.Console` can be used on Windows and Unix, but lacks mouse support.

You can force the use of `System.Console` on Unix as well; see `Core.cs`.

## Showcase

The [UI Catalog project](https://github.com/migueldeicaza/gui.cs/tree/master/UICatalog) provides an easy to use and extend sample illustrating the capabilities of **Terminal.Gui**.

## Documentation

* [API documentation](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui.html)

* [Overview](https://migueldeicaza.github.io/gui.cs/articles/overview.html) contains the conceptual documentation and a walkthrough of the core concepts of **Terminal.Gui**.

### Sample Usage

```csharp
using Terminal.Gui;

class Demo {
    static void Main ()
    {
        Application.Init ();
        var top = Application.Top;

	// Creates the top-level window to show
        var win = new Window ("MyApp") {
	    X = 0,
	    Y = 1, // Leave one row for the toplevel menu

	    // By using Dim.Fill(), it will automatically resize without manual intervention
	    Width = Dim.Fill (),
	    Height = Dim.Fill ()
	};
        top.Add (win);

	// Creates a menubar, the item "New" has a help menu.
        var menu = new MenuBar (new MenuBarItem [] {
            new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem ("_New", "Creates new file", NewFile),
                new MenuItem ("_Close", "", () => Close ()),
                new MenuItem ("_Quit", "", () => { if (Quit ()) top.Running = false; })
            }),
            new MenuBarItem ("_Edit", new MenuItem [] {
                new MenuItem ("_Copy", "", null),
                new MenuItem ("C_ut", "", null),
                new MenuItem ("_Paste", "", null)
            })
        });
        top.Add (menu);

	var login = new Label ("Login: ") { X = 3, Y = 2 };
	var password = new Label ("Password: ") {
	    	X = Pos.Left (login),
		Y = Pos.Top (login) + 1
        };
	var loginText = new TextField ("") {
                X = Pos.Right (password),
                Y = Pos.Top (login),
                Width = 40
        };
        var passText = new TextField ("") {
                Secret = true,
                X = Pos.Left (loginText),
                Y = Pos.Top (password),
                Width = Dim.Width (loginText)
        };
	
	// Add some controls, 
	win.Add (
	    // The ones with my favorite layout system
  	    login, password, loginText, passText,

	    // The ones laid out like an australopithecus, with absolute positions:
            new CheckBox (3, 6, "Remember me"),
            new RadioGroup (3, 8, new [] { "_Personal", "_Company" }),
            new Button (3, 14, "Ok"),
            new Button (10, 14, "Cancel"),
            new Label (3, 18, "Press F9 or ESC plus 9 to activate the menubar"));

        Application.Run ();
    }
}
```

Alternatively, you can encapsulate the app behavior in a new `Window`-derived class, say `App.cs` containing the code above, and simplify your `Main` method to:

```csharp
using Terminal.Gui;

class Demo {
    static void Main ()
    {
        Application.Run<App> ();
    }
}
```

The example above shows how to add views using both styles of layout supported by **Terminal.Gui**: **Absolute layout** and **[Computed layout](https://migueldeicaza.github.io/gui.cs/articles/overview.html#layout)**.

## Installing

Use NuGet to install the `Terminal.Gui` NuGet package: https://www.nuget.org/packages/Terminal.Gui

## Running and Building

* *`Terminal.Gui`* - Build and run using the .NET SDK command line tools (`doetnet build` in the root directory) or with Visual Studio 2019.
* *UI Catalog* - Run `dotnet run` in the `UICatalog` directory to run the UI Catalog.
* *Example (aka `demo.cs`)* - Run `dotnet run` in the `Example` directory to run the simple demo.
* *Standalone Example* - A trivial .NET core sample application can be found in the `StandaloneExample` directory. Run `dotnet run` in directory to test.

## Contributing

See [Issues](https://github.com/migueldeicaza/gui.cs/issues) for a list of open bugs and enhancements.

## History

This is an updated version of [gui.cs](http://tirania.org/blog/archive/2007/Apr-16.html) that Miguel wrote for [mono-curses](https://github.com/mono/mono-curses) in 2007.

The original **gui.cs** was a UI toolkit in a single file and tied to curses. This version tries to be console-agnostic and instead of having a container/widget model, only uses Views (which can contain subviews) and changes the rendering model to rely on damage regions instead of burdening each view with the details.

Release history can be found in the [Terminal.Gui.csproj](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/Terminal.Gui.csproj) file.
