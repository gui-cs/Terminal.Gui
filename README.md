[![Build Status](https://travis-ci.org/migueldeicaza/gui.cs.svg?branch=master)](https://travis-ci.org/migueldeicaza/gui.cs)
# Gui.cs - Terminal UI toolkit for .NET

This is a simple UI toolkit for .NET, .NET Core and Mono and works on
both Windows and Linux/Unix.

![Sample app](https://raw.githubusercontent.com/migueldeicaza/gui.cs/master/docfx/sample.png)

The toolkit contains various controls for building text user interfaces:

* [Buttons](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Button.html) 
* [Labels](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Label.html)
* [Text entry](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TextField.html)
* [Text view](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.TextView.html)
* [Radio buttons](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.RadioGroup.html)
* [Checkboxes](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.CheckBox.html)
* [Dialog boxes](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Dialog.html)
  * [Message boxes](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.MessageBox.html)
* [Windows](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.Window.html)
* [Menus](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.MenuBar.html)
* [ListViews](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ListView.html)
* [Frames](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.FrameView.html)
* [ProgressBars](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ProgressBar.html)
* [Scroll views](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ScrollView.html) and [Scrollbars](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.ScrollBarView.html)
* Hexadecimal viewer/editor (HexView)

All visible UI elements are subclasses of the
[View](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui/Terminal.Gui.View.html),
and these in turn can contain an arbitrary number of subviews.   

It comes with a
[mainloop](https://migueldeicaza.github.io/gui.cs/api/Mono.Terminal/Mono.Terminal.MainLoop.html)
to process events, process idle handlers, timers and monitoring file
descriptors.

It is designed to work on Curses and the [Windows Console](https://github.com/migueldeicaza/gui.cs/issues/27), 
works well on both color and monochrome terminals and has mouse support on
terminal emulators that support it.

# Documentation

* [API documentation](https://migueldeicaza.github.io/gui.cs/api/Terminal.Gui.html) for details.

* [Overview](https://migueldeicaza.github.io/gui.cs/articles/overview.html) contains the conceptual
  documentation and a walkthrough of the core concepts of `gui.cs`

# Sample Usage

```csharp
using Terminal.Gui;

class Demo {
    static void Main ()
    {
        Application.Init ();
        var top = Application.Top;

	// Creates the top-level window to show
        var win = new Window (new Rect (0, 1, top.Frame.Width, top.Frame.Height-1), "MyApp");
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

	// Add some controls
	win.Add (
            new Label (3, 2, "Login: "),
            new TextField (14, 2, 40, ""),
            new Label (3, 4, "Password: "),
            new TextField (14, 4, 40, "") { Secret = true },
            new CheckBox (3, 6, "Remember me"),
            new RadioGroup (3, 8, new [] { "_Personal", "_Company" }),
            new Button (3, 14, "Ok"),
            new Button (10, 14, "Cancel"),
            new Label (3, 18, "Press ESC and 9 to activate the menubar"));

        Application.Run ();
    }
}
```

# Installing it

If you want to try Gui.cs, use NuGet to install the `Terminal.Gui` NuGet package:

https://www.nuget.org/packages/Terminal.Gui

# Running and Building

You can find a trivial .NET core sample application in the
"StandaloneExample" directory.   You can execute it by running
`dotnet core` in that directory.

That sample relies on the distributed NuGet package, if you want to
to use the code on GitHub, you can open the Example program which 
references the library built out of this tree.

# Input Handling

The input handling of gui.cs is similar in some ways to Emacs and the
Midnight Commander, so you can expect some of the special key
combinations to be active.

The key ESC can act as an Alt modifier (or Meta in Emacs parlance), to
allow input on terminals that do not have an alt key.  So to produce
the sequence Alt-F, you can press either Alt-F, or ESC folowed by the key F.

To enter the key ESC, you can either press ESC and wait 100
milliseconds, or you can press ESC twice.

ESC-0, and ESC_1 through ESC-9 have a special meaning, they map to
F10, and F1 to F9 respectively.

# Driver model

Currently gui.cs has support for both ncurses and the `System.Console`
front-ends.  ncurses is used on Unix, while `System.Console` is used
on Windows, but you can force the use of `System.Console` on Unix as
well, see `Core.cs`.

# Tasks

There are some tasks in the github issues, and some others are being
tracked in the TODO.md file.

# History

This is an updated version of
[gui.cs](http://tirania.org/blog/archive/2007/Apr-16.html) that
I wrote for [mono-curses](https://github.com/mono/mono-curses) in 2007.

The original gui.cs was a UI toolkit in a single file and tied to
curses.  This version tries to be console-agnostic and instead of
having a container/widget model, only uses Views (which can contain
subviews) and changes the rendering model to rely on damage regions
instead of burderning each view with the details.

