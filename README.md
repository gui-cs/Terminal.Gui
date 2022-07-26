![.NET Core](https://github.com/gui-cs/Terminal.Gui/workflows/.NET%20Core/badge.svg?branch=master)
![Code scanning - action](https://github.com/gui-cs/Terminal.Gui/workflows/Code%20scanning%20-%20action/badge.svg)
[![Version](https://img.shields.io/nuget/v/Terminal.Gui.svg)](https://www.nuget.org/packages/Terminal.Gui)
![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/migueldeicaza/90ef67a684cb71db1817921a970f8d27/raw/code-coverage.json)
[![Downloads](https://img.shields.io/nuget/dt/Terminal.Gui)](https://www.nuget.org/packages/Terminal.Gui)
[![License](https://img.shields.io/github/license/migueldeicaza/gui.cs.svg)](LICENSE)
![Bugs](https://img.shields.io/github/issues/migueldeicaza/gui.cs/bug)

# Terminal.Gui - Cross Platform Terminal UI toolkit for .NET

A toolkit for building rich console apps for .NET, .NET Core, and Mono that works on Windows, the Mac, and Linux/Unix.

![Sample app](https://raw.githubusercontent.com/gui-cs/Terminal.Gui/docfx/sample.gif)

## Controls and Views

*Terminal.Gui* provides a rich set of views and controls for building terminal user interfaces:

* [Button](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.Button.html) - A View that provides an item that invokes an System.Action when activated by the user.
* [CheckBox](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.CheckBox.html) - Shows an on/off toggle that the user can set.
* [ColorPicker](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.ColorPicker.html) - Enables to user to pick a color.
* [ComboBox](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.ComboBox.html) - Provides a drop-down list of items the user can select from.
* [Dialog](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.Dialog.html) - A pop-up Window that contains one or more Buttons.
  * [OpenDialog](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.OpenDialog.html) - A Dialog providing an interactive pop-up Window for users to select files or directories.
  * [SaveDialog](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.SaveDialog.html) - A Dialog providing an interactive pop-up Window for users to save files.
* [FrameView](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.FrameView.html) - A container View that draws a frame around its contents. Similar to a GroupBox in Windows.
* [GraphView](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.GraphView.html) - A View for rendering graphs (bar, scatter etc).
* [Hex viewer/editor](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.HexView.html) - A hex viewer and editor that operates over a file stream. 
* [Label](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.Label.html) - Displays a string at a given position and supports multiple lines.
* [ListView](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.ListView.html) - Displays a scrollable list of data where each item can be activated to perform an action.
* [MenuBar](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.MenuBar.html) - Provides a menu bar with drop-down and cascading menus.
* [MessageBox](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.MessageBox.html) - Displays a modal (pup-up) message to the user, with a title, a message and a series of options that the user can choose from. 
* [ProgressBar](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.ProgressBar.html) - Displays a progress Bar indicating progress of an activity.
* [RadioGroup](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.RadioGroup.html) - Displays a group of labels each with a selected indicator. Only one of those can be selected at a given time
* [ScrollView](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.ScrollView.html) - Present a window into a virtual space where subviews are added. Similar to the iOS UIScrollView.
* [ScrollBarView](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.ScrollBarView.html) - display a 1-character scrollbar, either horizontal or vertical.
* [StatusBar](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.StatusBar.html) - A View that snaps to the bottom of a Toplevel displaying set of status items. Includes support for global app keyboard shortcuts.
* [TableView](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.TableView.html) - A View for tabular data based on a System.Data.DataTable. 
* [TimeField](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.TimeField.html) & [DateField](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.TimeField.html) - Enables structured editing of dates and times.
* [TextField](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.TextField.html) - Provides a single-line text entry.
* [TextValidateField](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.TextValidateField.html) - Text field that validates input through a ITextValidateProvider.
* [TextView](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.TextView.html)- A multi-line text editing View supporting word-wrap, auto-complete, context menus, undo/redo, and clipboard operations, 
* [TopLevel](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.Toplevel.html) - The base class for modal/pop-up Windows.
* [TreeView](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.TreeView.html) - A hierarchical tree view with expandable branches. Branch objects are dynamically determined when expanded using a user defined ITreeBuilder.
* [View](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.View.html) - The base class for all views on the screen and represents a visible element that can render itself and contains zero or more nested views.
* [Window](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.Window.html) - A Toplevel view that draws a border around its Frame with a title at the top.
* [Wizard](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.Wizard.html) - Provides navigation and a user interface to collect related data across multiple steps.

### Features

* **Cross Platform** - Windows, Mac, and Linux. Terminal drivers for Curses, [Windows Console](https://github.com/gui-cs/Terminal.Gui/issues/27), and the .NET Console mean apps will work well on both color and monochrome terminals. 
* **Keyboard and Mouse Input** - Both keyboard and mouse input are supported, including support for drag & drop.
* **[Flexible Layout](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#layout)** - Supports both *Absolute layout* and an innovative *Computed Layout* system. *Computed Layout* makes it easy to layout controls relative to each other and enables dynamic terminal UIs.
* **Clipboard support** - Cut, Copy, and Paste of text provided through the [`Clipboard`](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.Clipboard.html) class.
* **[Arbitrary Views](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.View.html)** - All visible UI elements are subclasses of the `View` class, and these in turn can contain an arbitrary number of sub-views.
* **Advanced App Features** - The [Mainloop](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.MainLoop.html) supports processing events, idle handlers, timers, and monitoring file
descriptors. Most classes are safe for threading.
* **Reactive Extensions** - Use [reactive extensions](https://github.com/dotnet/reactive) and benefit from increased code readability, and the ability to apply the MVVM pattern and [ReactiveUI](https://www.reactiveui.net/) data bindings. See the [source code](https://github.com/gui-cs/Terminal.Gui/tree/master/ReactiveExample) of a sample app in order to learn how to achieve this.

### Keyboard Input Handling

**Terminal.Gui** respects common Linux, Mac, and Windows keyboard idioms. For example, clipboard operations use the familiar `Control/Command-C, X, V` model. `CTRL-Q` is used for exiting views (and apps).

The input handling of **Terminal.Gui** is similar in some ways to Emacs and the Midnight Commander, so you can expect some of the special key combinations to be active.

The key `ESC` can act as an Alt modifier (or Meta in Emacs parlance), to allow input on terminals that do not have an alt key. So to produce the sequence `Alt-F`, you can press either `Alt-F`, or `ESC` followed by the key `F`.

To enter the key `ESC`, you can either press `ESC` and wait 100 milliseconds, or you can press `ESC` twice.

`ESC-0`, and `ESC-1` through `ESC-9` have a special meaning, they map to `F10`, and `F1` to `F9` respectively.

Apps can change key bindings using the `AddKeyBinding` API. 

### Driver Model

**Terminal.Gui** has support for [ncurses](https://github.com/gui-cs/Terminal.Gui/blob/master/Terminal.Gui/ConsoleDrivers/CursesDriver/CursesDriver.cs), [`System.Console`](https://github.com/gui-cs/Terminal.Gui/blob/master/Terminal.Gui/ConsoleDrivers/NetDriver.cs), and a full [Win32 Console](https://github.com/gui-cs/Terminal.Gui/blob/master/Terminal.Gui/ConsoleDrivers/WindowsDriver.cs) front-end.

`ncurses` is used on Mac/Linux/Unix with color support based on what your library is compiled with; the Windows driver supports full color and mouse, and an easy-to-debug `System.Console` can be used on Windows and Unix, but lacks mouse support.

You can force the use of `System.Console` on Unix as well; see `Core.cs`.

## Showcase & Examples

* **[UI Catalog](https://github.com/gui-cs/Terminal.Gui/tree/master/UICatalog)** - The UI Catalog project provides an easy to use and extend sample illustrating the capabilities of **Terminal.Gui**. Run `dotnet run --project UICatalog` to run the UI Catalog.
* **[Reactive Example](https://github.com/gui-cs/Terminal.Gui/tree/master/ReactiveExample)** - A sample app that shows how to use `System.Reactive` and `ReactiveUI` with `Terminal.Gui`. The app uses the MVVM architecture that may seem familiar to folks coming from WPF, Xamarin Forms, UWP, Avalonia, or Windows Forms. In this app, we implement the data bindings using ReactiveUI `WhenAnyValue` syntax and [Pharmacist](https://github.com/reactiveui/pharmacist) — a tool that converts all events in a NuGet package into observable wrappers.
* **[Example (aka `demo.cs`)](https://github.com/gui-cs/Terminal.Gui/tree/master/Example)** - Run `dotnet run` in the `Example` directory to run the simple demo.
* **[Standalone Example](https://github.com/gui-cs/Terminal.Gui/tree/master/StandaloneExample)** - A trivial .NET core sample application can be found in the `StandaloneExample` directory. Run `dotnet run` in directory to test.
* **[F# Example](https://github.com/gui-cs/Terminal.Gui/tree/master/FSharpExample)** - An example showing how to build a Terminal.Gui app using F#.
* **[PowerShell's `Out-ConsoleGridView`](https://github.com/PowerShell/GraphicalTools/blob/master/docs/Microsoft.PowerShell.ConsoleGuiTools/Out-ConsoleGridView.md)** - `OCGV` sends the output from a command to  an interactive table. 
* **[PoshRedisViewer](https://github.com/En3Tho/PoshRedisViewer)** - A compact Redis viewer module for PowerShell written in F# and Gui.cs
* **[TerminalGuiDesigner](https://github.com/tznind/TerminalGuiDesigner)** - Cross platform view designer for building Terminal.Gui applications.

## Documentation

* [Overview](https://gui-cs.github.io/Terminal.Gui/articles/overview.html)
* [Conceptual Documentation](https://gui-cs.github.io/Terminal.Gui/articles/index.html)
* [API Documentation](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui/Terminal.Gui.html)

See the [`Terminal.Gui/` README](https://github.com/gui-cs/Terminal.Gui/tree/master/Terminal.Gui) for an overview of how the library is structured. The [Conceptual Documentation](https://gui-cs.github.io/Terminal.Gui/articles/index.html) provides insight into core concepts.

### Sample Usage
(This code uses C# 9.0 [Top-level statements](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9#top-level-statements).) 
```csharp
using Terminal.Gui;
using NStack;

Application.Init();
var top = Application.Top;

// Creates the top-level window to show
var win = new Window("MyApp")
{
	X = 0,
	Y = 1, // Leave one row for the toplevel menu

	// By using Dim.Fill(), it will automatically resize without manual intervention
	Width = Dim.Fill(),
	Height = Dim.Fill()
};

top.Add(win);

// Creates a menubar, the item "New" has a help menu.
var menu = new MenuBar(new MenuBarItem[] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_New", "Creates new file", null),
				new MenuItem ("_Close", "",null),
				new MenuItem ("_Quit", "", () => { if (Quit ()) top.Running = false; })
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", null),
				new MenuItem ("C_ut", "", null),
				new MenuItem ("_Paste", "", null)
			})
		});
top.Add(menu);

static bool Quit()
{
	var n = MessageBox.Query(50, 7, "Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
	return n == 0;
}

var login = new Label("Login: ") { X = 3, Y = 2 };
var password = new Label("Password: ")
{
	X = Pos.Left(login),
	Y = Pos.Top(login) + 1
};
var loginText = new TextField("")
{
	X = Pos.Right(password),
	Y = Pos.Top(login),
	Width = 40
};
var passText = new TextField("")
{
	Secret = true,
	X = Pos.Left(loginText),
	Y = Pos.Top(password),
	Width = Dim.Width(loginText)
};

// Add some controls, 
win.Add(
	// The ones with my favorite layout system, Computed
	login, password, loginText, passText,

	// The ones laid out like an australopithecus, with Absolute positions:
	new CheckBox(3, 6, "Remember me"),
	new RadioGroup(3, 8, new ustring[] { "_Personal", "_Company" }, 0),
	new Button(3, 14, "Ok"),
	new Button(10, 14, "Cancel"),
	new Label(3, 18, "Press F9 or ESC plus 9 to activate the menubar")
);

Application.Run();
Application.Shutdown();
```

The example above shows adding views using both styles of layout supported by **Terminal.Gui**: **Absolute layout** and **[Computed layout](https://gui-cs.github.io/Terminal.Gui/articles/overview.html#layout)**.

Alternatively, you can encapsulate the app behavior in a new `Window`-derived class, say `App.cs` containing the code above, and simplify your `Main` method to:

```csharp
using Terminal.Gui;

class Demo {
	static void Main ()
	{
		Application.Run<App> ();
		Application.Shutdown ();
	}
}
```

## Installing

Use NuGet to install the `Terminal.Gui` NuGet package: https://www.nuget.org/packages/Terminal.Gui

### Installation in .NET Core Projects

To install Terminal.Gui into a .NET Core project, use the `dotnet` CLI tool with following command.

```
dotnet add package Terminal.Gui
```

## Running and Building

* Windows, Mac, and Linux - Build and run using the .NET SDK command line tools (`dotnet build` in the root directory). Run `UICatalog` with `dotnet run --project UICatalog`.
* Windows - Open `Terminal.Gui.sln` with Visual Studio 2019.

## Contributing

See [CONTRIBUTING.md](https://github.com/gui-cs/Terminal.Gui/blob/master/CONTRIBUTING.md).

Debates on architecture and design can be found in Issues tagged with [design](https://github.com/gui-cs/Terminal.Gui/issues?q=is%3Aopen+is%3Aissue+label%3Adesign).

## History

This is an updated version of [gui.cs](http://tirania.org/blog/archive/2007/Apr-16.html) that Miguel wrote for [mono-curses](https://github.com/mono/mono-curses) in 2007.

The original **gui.cs** was a UI toolkit in a single file and tied to curses. This version tries to be console-agnostic and instead of having a container/widget model, only uses Views (which can contain subviews) and changes the rendering model to rely on damage regions instead of burdening each view with the details.

A presentation of this was part of the [Retro.NET](https://channel9.msdn.com/Events/dotnetConf/2018/S313) talk at .NET Conf 2018 [Slides](https://tirania.org/Retro.pdf)

Release history can be found in the [Terminal.Gui.csproj](https://github.com/gui-cs/Terminal.Gui/blob/master/Terminal.Gui/Terminal.Gui.csproj) file.

In 2019, 2020, and 2021, Charlie Kindel (https://github.com/tig), @BDisp (https://github.com/BDisp), and Thomas Nind (https://github.com/tznind) vastly extended, improved, polished and fixed gui.cs to what it is today.
