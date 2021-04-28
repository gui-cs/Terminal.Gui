![.NET Core](https://github.com/migueldeicaza/gui.cs/workflows/.NET%20Core/badge.svg?branch=master)
![Code scanning - action](https://github.com/migueldeicaza/gui.cs/workflows/Code%20scanning%20-%20action/badge.svg)
[![Version](https://img.shields.io/nuget/v/Terminal.Gui.svg)](https://www.nuget.org/packages/Terminal.Gui)
![Code Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/migueldeicaza/90ef67a684cb71db1817921a970f8d27/raw/code-coverage.json)
[![Downloads](https://img.shields.io/nuget/dt/Terminal.Gui)](https://www.nuget.org/packages/Terminal.Gui)
[![License](https://img.shields.io/github/license/migueldeicaza/gui.cs.svg)](LICENSE)
![Bugs](https://img.shields.io/github/issues/migueldeicaza/gui.cs/bug)

# Terminal.Gui - Cross Platform Terminal GUI toolkit for .NET

A toolkit for building console GUI apps for .NET, .NET Core, and Mono that works on Windows, the Mac, and Linux/Unix.

![Sample app](https://raw.githubusercontent.com/migueldeicaza/gui.cs/master/docfx/sample.gif)

## Controls & Features

*Terminal.Gui* contains various controls for building text user interfaces:

* [Button](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Button.html) 
* [CheckBox](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.CheckBox.html)
* [ComboBox](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ComboBox.html)
* [Dialog](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Dialog.html)
  * [OpenDialog](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.OpenDialog.html)
  * [SaveDialog](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.SaveDialog.html)
* [FrameView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.FrameView.html)
* [GraphView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.GraphView.html)
* [Hex viewer/editor](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.HexView.html)
* [Label](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Label.html)
* [ListView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ListView.html)
* [Menu](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.MenuBar.html)
* [MessageBox](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.MessageBox.html)
* [ProgressBar](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ProgressBar.html)
* [Radio buttons](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.RadioGroup.html)
* [TableView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TableView.html)
* [Time & Date Fields](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TimeField.html)
* [TextField](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TextField.html)
* [TextValidateField](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TextValidateField.html)
* [TextView (Text Editor)](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TextView.html)
* [TreeView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TreeView.html)
* [ScrollView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ScrollView.html)
* [ScrollBarView](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ScrollBarView.html)
* [StatusBar](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.StatusBar.html)
* [Window](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Window.html)

In addition, a complete Xterm/Vt100 terminal emulator that you can embed is now part of [XtermSharp](https://github.com/migueldeicaza/XtermSharp/blob/master/GuiCsHost/TerminalView.cs) - you just need to pull [`TerminalView.cs`](https://github.com/migueldeicaza/XtermSharp/blob/master/GuiCsHost/TerminalView.cs) into your project.

### Features

* **Cross Platform** - Works on Windows, Mac, and Linux. Terminal drivers for Curses, [Windows Console](https://github.com/migueldeicaza/gui.cs/issues/27), and the .NET Console mean **Terminal.Gui** works well on both color and monochrome terminals and has mouse support on terminal emulators that support it.
* **Keyboard and Mouse Input** - Both keyboard and mouse input are supported, including limited support for drag & drop.
* **[Flexible Layout](https://migueldeicaza.github.io/gui.cs/articles/overview.html#layout)** - **Terminal.Gui** supports both *Absolute layout* and an innovative UI layout system referred to as *Computed Layout*. *Computed Layout* makes it easy to layout controls relative to each other and enables dynamic console GUIs.
* **Clipboard support** - Cut, Copy, and Paste of text provided through the [`Clipboard`](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Clipboard.html) class.
* **[Arbitrary Views](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.View.html)** - All visible UI elements are subclasses of the `View` class, and these in turn can contain an arbitrary number of sub-views.
* **Advanced App Features** - The [Mainloop](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Mono.Terminal.MainLoop.html) supports processing events, idle handlers, timers, and monitoring file
descriptors.
* **Reactive Extensions Support** - Use [reactive extensions](https://github.com/dotnet/reactive) and benefit from increased code readability, and the ability to apply the MVVM pattern and [ReactiveUI](https://www.reactiveui.net/) data bindings. See the [source code](https://github.com/migueldeicaza/gui.cs/tree/master/ReactiveExample) of a sample app in order to learn how to achieve this.

### Keyboard Input Handling

The input handling of **Terminal.Gui** is similar in some ways to Emacs and the Midnight Commander, so you can expect some of the special key combinations to be active.

The key `ESC` can act as an Alt modifier (or Meta in Emacs parlance), to allow input on terminals that do not have an alt key.  So to produce the sequence `Alt-F`, you can press either `Alt-F`, or `ESC` followed by the key `F`.

To enter the key `ESC`, you can either press `ESC` and wait 100 milliseconds, or you can press `ESC` twice.

`ESC-0`, and `ESC-1` through `ESC-9` have a special meaning, they map to `F10`, and `F1` to `F9` respectively.

**Terminal.Gui** respects common Mac and Windows keyboard idoms as well. For example, clipboard operations use the familiar `Control/Command-C, X, V` model.

`CTRL-Q` is used for exiting views (and apps).

### Driver model

**Terminal.Gui** has support for [ncurses](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/ConsoleDrivers/CursesDriver/CursesDriver.cs), [`System.Console`](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/ConsoleDrivers/NetDriver.cs), and a full [Win32 Console](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/ConsoleDrivers/WindowsDriver.cs) front-end.

`ncurses` is used on Mac/Linux/Unix with color support based on what your library is compiled with; the Windows driver supports full color and mouse, and an easy-to-debug `System.Console` can be used on Windows and Unix, but lacks mouse support.

You can force the use of `System.Console` on Unix as well; see `Core.cs`.

## Showcase & Examples

* **[UI Catalog](https://github.com/migueldeicaza/gui.cs/tree/master/UICatalog)** - The UI Catalog project provides an easy to use and extend sample illustrating the capabilities of **Terminal.Gui**. Run `dotnet run --project UICatalog` to run the UI Catalog.
* **[Reactive Example](https://github.com/migueldeicaza/gui.cs/tree/master/ReactiveExample)** - A sample app that shows how to use `System.Reactive` and `ReactiveUI` with `Terminal.Gui`. The app uses the MVVM architecture that may seem familiar to folks coming from WPF, Xamarin Forms, UWP, Avalonia, or Windows Forms. In this app, we implement the data bindings using ReactiveUI `WhenAnyValue` syntax and [Pharmacist](https://github.com/reactiveui/pharmacist) — a tool that converts all events in a NuGet package into observable wrappers.
* **[Example (aka `demo.cs`)](https://github.com/migueldeicaza/gui.cs/tree/master/Example)** - Run `dotnet run` in the `Example` directory to run the simple demo.
* **[Standalone Example](https://github.com/migueldeicaza/gui.cs/tree/master/StandaloneExample)** - A trivial .NET core sample application can be found in the `StandaloneExample` directory. Run `dotnet run` in directory to test.
* **[F# Example](https://github.com/migueldeicaza/gui.cs/tree/master/FSharpExample)** - An example showing how to build a Terminal.Gui app using F#.
* **[Powershell Sample]()** - (Coming soon! See PR #952. Shows how to build Terminal.Gui apps using Powershell.
* **PowerShell's Out-ConsoleGridView** - The [`Out-ConsoleGridView` PowerShell Cmdlet](https://github.com/PowerShell/GraphicalTools/blob/master/docs/Microsoft.PowerShell.ConsoleGuiTools/Out-ConsoleGridView.md) sends the output from a command to a grid view window where the output is displayed in an interactive table. sends the output from a command to a grid view window where the output is displayed in an interactive table, using Terminal.Gui. 

## Documentation

* [Overview](https://migueldeicaza.github.io/gui.cs/articles/overview.html)
* [Conceptual Documentation](https://migueldeicaza.github.io/gui.cs/articles/index.html)
* [API Documentation](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.html)

See the [`Terminal.Gui/` README](https://github.com/migueldeicaza/gui.cs/tree/master/Terminal.Gui) for an overview of how the library is structured. The [Conceptual Documentation](https://migueldeicaza.github.io/gui.cs/articles/index.html) provides insight into core concepts.

### Sample Usage
The code below is done with the new [Top-level statements](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-9#top-level-statements) in C# 9.0.  
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

### Installation in .NET Core Projects

To install Terminal.Gui into a .NET Core project, use the `dotnet` CLI tool with following command.

```
dotnet add package Terminal.Gui
```

## Running and Building

* Windows, Mac, and Linux - Build and run using the .NET SDK command line tools (`dotnet build` in the root directory). Run `UICatalog` with `dotnet run --project ./UICatalog` or by directly executing `./UICatalog/bin/Debug/net5.0/UICatalog.exe`.
* Windows - Open `Terminal.Gui.sln` with Visual Studio 2019.

## Contributing

See [CONTRIBUTING.md](https://github.com/migueldeicaza/gui.cs/blob/master/CONTRIBUTING.md).

Debates on architecture and design can be found in Issues tagged with [design](https://github.com/migueldeicaza/gui.cs/issues?q=is%3Aopen+is%3Aissue+label%3Adesign).

## History

This is an updated version of [gui.cs](http://tirania.org/blog/archive/2007/Apr-16.html) that Miguel wrote for [mono-curses](https://github.com/mono/mono-curses) in 2007.

The original **gui.cs** was a UI toolkit in a single file and tied to curses. This version tries to be console-agnostic and instead of having a container/widget model, only uses Views (which can contain subviews) and changes the rendering model to rely on damage regions instead of burdening each view with the details.

A presentation of this was part of the [Retro.NET](https://channel9.msdn.com/Events/dotnetConf/2018/S313) talk at .NET Conf 2018 [Slides](https://tirania.org/Retro.pdf)

Release history can be found in the [Terminal.Gui.csproj](https://github.com/migueldeicaza/gui.cs/blob/master/Terminal.Gui/Terminal.Gui.csproj) file.

In 2019, 2020, and 2021, Charlie Kindel (https://github.com/tig), @BDisp (https://github.com/BDisp), and Thomas Nind (https://github.com/tznind) vastly extended, improved, polished and fixed gui.cs to what it is today.
