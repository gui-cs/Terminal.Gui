
# Cross-Platform Driver Model

**These are the v1 API docs**. The v2 API docs are [here](https://gui-cs.github.io/Terminal.GuiV2Docs/).

**Terminal.Gui** has support for [ncurses](https://github.com/gui-cs/Terminal.Gui/blob/master/Terminal.Gui/ConsoleDrivers/CursesDriver/CursesDriver.cs), [`System.Console`](https://github.com/gui-cs/Terminal.Gui/blob/master/Terminal.Gui/ConsoleDrivers/NetDriver.cs), and a full [Win32 Console](https://github.com/gui-cs/Terminal.Gui/blob/master/Terminal.Gui/ConsoleDrivers/WindowsDriver.cs) front-end.

`ncurses` is used on Mac/Linux/Unix with color support based on what your library is compiled with; the Windows driver supports full color and mouse, and an easy-to-debug `System.Console` can be used on Windows and Unix, but lacks mouse support.

You can force the use of `System.Console` on Unix as well; see `Core.cs`.