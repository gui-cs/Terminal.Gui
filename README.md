# Gui.cs - Terminal UI toolkit for .NET

This is a simple UI toolkit for .NET.

It is an updated version of
[gui.cs](https://github.com/mono/mono-curses/blob/master/gui.cs) that
I wrote for [mono-curses](https://github.com/mono/mono-curses)

# API Documentation

Go to the [API documentation](https://migueldeicaza.github.io/gui.cs/api/Terminal.html) for details.

# Running and Building

To run this, you will need a peer checkout of mono-ncurses to this
directory.   I should make a NuGet package at some point.

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

Currently gui.cs is built on top of curses, but the console driver has
been abstracted, an implementation that uses `System.Console` is
possible, but would have to emulate some of the behavior of curses,
namely that operations are performed on the buffer, and the Refresh
call reflects the contents of an internal buffer into the screen and
position the cursor in the last set position at the end.

# Tasks

There are some tasks in the github issues, and some others are being
tracked in the TODO.md file.

# History

The original gui.cs was a UI toolkit in a single file and tied to
curses.  This version tries to be console-agnostic (but currently only
has a curses backend, go figure) and instead of having a
container/widget model, only uses Views (which can contain subviews)
and changes the rendering model to rely on damage regions instead of 
burderning each view with the details.

