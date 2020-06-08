![.NET Core](https://github.com/migueldeicaza/gui.cs/workflows/.NET%20Core/badge.svg?branch=master)
[![Version](https://img.shields.io/nuget/v/Terminal.Gui.svg)](https://www.nuget.org/packages/Terminal.Gui)
[![Downloads](https://img.shields.io/nuget/dt/Terminal.Gui)](https://www.nuget.org/packages/Terminal.Gui)
[![License](https://img.shields.io/github/license/migueldeicaza/gui.cs.svg)](LICENSE)
[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/mono/mono?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) - The Mono Channel room

# Terminal.Gui - Terminal GUI toolkit for .NET

A simple toolkit for buiding console GUI apps for .NET, .NET Core, and Mono that works on Windows, the Mac, and Linux/Unix.

![Sample app](https://raw.githubusercontent.com/migueldeicaza/gui.cs/master/docfx/sample.gif)

## IMPORTANT RELEASE INFO

We are actively converging on a major update to Terminal.Gui. The most recent released Nuget package is version `0.81` which is way behind `master`. This README and the API Documentation refers to the latest build from `master`. If you want the latest and greatest functionality, clone and build locally. Otherwise `0.81` is quite stable, but the documentation may not match.

## Controls & Features

The *Terminal.Gui* toolkit contains various controls for building text user interfaces:

* [Button](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Button.html) 
* [CheckBox](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.CheckBox.html)
* [ComboBox](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ComboBox.html)
* [Dialog](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Dialog.html)
	* [OpenDialog](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.OpenDialog.html)
	* [SaveDialog](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.SaveDialog.html)
* [FrameView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.FrameView.html)
* [Hex viewer/editor](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.HexView.html)
* [Label](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Label.html)
* [ListView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ListView.html)
* [Menu](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.MenuBar.html)
* [MessageBox](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.MessageBox.html)
* [ProgressBar](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ProgressBar.html)
* [Radio buttons](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.RadioGroup.html)
* [Time & Date Fields](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TimeField.html)
* [TextField](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TextField.html)
* [Text Editor](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TextView.html)
* [ScrollView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ScrollView.html)
* [ScrollBarView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ScrollBarView.html)
* [StatusBar](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.StatusBar.html)
* [Window](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Window.html)

In addition, a complete Xterm/Vt100 terminal emulator that you can embed is now part of [XtermSharp](https://github.com/migueldeicaza/XtermSharp/blob/master/GuiCsHost/TerminalView.cs) - you just need to pull [`TerminalView.cs`](https://github.com/migueldeicaza/XtermSharp/blob/master/GuiCsHost/TerminalView.cs) into your project.

### Features

* **Cross Platform** - Terminal drivers for Curses, [Windows Console](https://github.com/migueldeicaza/gui.cs/issues/27), and the .NET Console mean **Terminal.Gui** works well on both color and monochrome terminals and has mouse support on terminal emulators that support it.
* **Keyboard and Mouse Input** - Both keyboard and mouse input are supported, including limited support for drag & drop.
* **[Flexible Layout](https://migueldeicaza.github.io/gui.cs/articles/overview.html#layout)** - **Terminal.Gui** supports both *Absolute layout* and an innovative UI layout system referred to as *Computed Layout*. *Computed Layout* makes it easy to layout controls relative to each other and enables dynamic console GUIs.
* **Clipboard support** - Cut, Copy, and Paste of text provided through the [`Clipboard`](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Clipboard.html) class.
* **[Arbitrary Views](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.View.html)** - All visible UI elements are subclasses of the `View` class, and these in turn can contain an arbitrary number of sub-views.
* **Advanced App Features** - The [Mainloop](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Mono.Terminal.MainLoop.html) supports processing events, idle handlers, timers, and monitoring file
descriptors.

### Keyboard Input Handling

The input handling of **Terminal.Gui** is similar in some ways to Emacs and the Midnight Commander, so you can expect some of the special key combinations to be active.

The key `ESC` can act as an Alt modifier (or Meta in Emacs parlance), to allow input on terminals that do not have an alt key.  So to produce the sequence `Alt-F`, you can press either `Alt-F`, or `ESC` followed by the key `F`.

To enter the key `ESC`, you can either press `ESC` and wait 100 milliseconds, or you can press `ESC` twice.

`ESC-0`, and `ESC-1` through `ESC-9` have a special meaning, they map to `F10`, and `F1` to `F9` respectively.

**Terminal.Gui** respects common Mac and Windows keyboard idoms as well. For example, clipboard operations use the familiar `Control/Command-C, X, V` model.

`CTRL-Q` is used for exiting views (and apps).

### Driver model

Currently **Terminal.Gui** has support for [ncurses](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/Drivers/CursesDriver.cs), [`System.Console`](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/Drivers/NetDriver.cs), and a full [Win32 Console](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/Drivers/WindowsDriver.cs) front-end.

`ncurses` is used on Mac/Linux/Unix with color support based on what your library is compiled with; the Windows driver supports full color and mouse, and an easy-to-debug `System.Console` can be used on Windows and Unix, but lacks mouse support.

You can force the use of `System.Console` on Unix as well; see `Core.cs`.

### Design

See the [`Terminal.Gui/` README](https://github.com/migueldeicaza/gui.cs/tree/master/Terminal.Gui) for an overview of how the library is structured. The [Conceptual Documentation](https://migueldeicaza.github.io/gui.cs/articles/index.html) provides insight into core concepts.

Debates on architecture and design can be found in Issues tagged with [design](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3Adesign).

## Showcase & Examples

* **UI Catalog** - The [UI Catalog project](https://github.com/migueldeicaza/gui.cs/tree/master/UICatalog) provides an easy to use and extend sample illustrating the capabilities of **Terminal.Gui**. Run `dotnet run` in the `UICatalog` directory to run the UI Catalog.
* **Example (aka `demo.cs`)** - Run `dotnet run` in the `Example` directory to run the simple demo.
* **Standalone Example** - A trivial .NET core sample application can be found in the `StandaloneExample` directory. Run `dotnet run` in directory to test.

## Documentation

* [Overview](https://migueldeicaza.github.io/gui.cs/articles/overview.html)
* [Conceptual Documenation](https://migueldeicaza.github.io/gui.cs/articles/index.html)
* [API Documentation](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.html)

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

*We are actively converging on a major update to Terminal.Gui. The most recent released Nuget package is version `0.81` which is way behind `master`. This README and the API Documentation refers to the latest build from `master`. If you want the latest and greatest functionality, clone and build locally. Otherwise `0.81` is quite stable, but the documentation may not match.*

Use NuGet to install the `Terminal.Gui` NuGet package: https://www.nuget.org/packages/Terminal.Gui

## Running and Building

* Windows, Mac, and Linux - Build and run using the .NET SDK command line tools (`dotnet build` in the root directory) 
* Windows - Open `Terminal.Gui.sln` with Visual Studio 2019.

## Contributing

See [CONTRIBUTING.md](https://github.com/migueldeicaza/gui.cs/blob/master/CONTRIBUTING.md).

## History

This is an updated version of [gui.cs](http://tirania.org/blog/archive/2007/Apr-16.html) that Miguel wrote for [mono-curses](https://github.com/mono/mono-curses) in 2007.

The original **gui.cs** was a UI toolkit in a single file and tied to curses. This version tries to be console-agnostic and instead of having a container/widget model, only uses Views (which can contain subviews) and changes the rendering model to rely on damage regions instead of burdening each view with the details.

A presentation of this was part of the [Retro.NET](https://channel9.msdn.com/Events/dotnetConf/2018/S313) talk at .NET Conf 2018 [Slides](https://tirania.org/Retro.pdf)

Release history can be found in the [Terminal.Gui.csproj](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/Terminal.Gui.csproj) file.

In 2019 and 2020, Charlie Kindel (https://github.com/tig) and @BDisp (https://github.com/BDisp) vastly extended, improved, polished and fixed gui.cs to what it is today.
