//
// TODO:
// * FindNCurses needs to remove the old probing code
// * Removal of that proxy code
// * Need to implement reading pointers with the new API
// * Can remove the manual Dlopen features
// * initscr() diagnostics based on DLL can be fixed
//
// binding.cs.in: Core binding for curses.
//
// This file attempts to call into ncurses without relying on Mono's
// dllmap, so it will work with .NET Core.  This means that it needs
// two sets of bindings, one for "ncurses" which works on OSX, and one
// that works against "libncursesw.so.5" which is what you find on
// assorted Linux systems.
//
// Additionally, I do not want to rely on an external native library
// which is why all this pain to bind two separate ncurses is here.
//
// Authors:
//   Miguel de Icaza (miguel.de.icaza@gmail.com)
//
// Copyright (C) 2007 Novell (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System.Runtime.InteropServices;

namespace Unix.Terminal {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

	//[StructLayout (LayoutKind.Sequential)]
	//public struct winsize {
	//	public ushort ws_row;
	//	public ushort ws_col;
	//	public ushort ws_xpixel;   /* unused */
	//	public ushort ws_ypixel;   /* unused */
	//};

	[StructLayout (LayoutKind.Sequential)]
	public struct CursesMouseEvent {
		public short ID;
		public int X, Y, Z;
		public Curses.Event ButtonState;
	}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.

}
