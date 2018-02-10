
//
// binding.cs.in: Core binding for curses.
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
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Unix.Terminal {

	internal partial class Curses {

		[StructLayout (LayoutKind.Sequential)]
		internal struct MouseEvent {
			public short ID;
			public int X, Y, Z;
			public Event ButtonState;
		}

#region Screen initialization

		[DllImport ("ncurses", EntryPoint="initscr")]
		extern static internal IntPtr real_initscr ();
		static int lines, cols;

		static Window main_window;
		static IntPtr curses_handle, curscr_ptr, lines_ptr, cols_ptr;

		static void FindNCurses ()
		{
			if (File.Exists ("/usr/lib/libncurses.dylib"))
				curses_handle = dlopen ("libncurses.dylib", 1);
			else
				curses_handle = dlopen ("libncurses.so", 1);
			
			if (curses_handle == IntPtr.Zero)
				throw new Exception ("Could not dlopen ncurses");

			stdscr = read_static_ptr ("stdscr");
			curscr_ptr = get_ptr ("curscr");
			lines_ptr = get_ptr ("LINES");
			cols_ptr = get_ptr ("COLS");
		}
		
		static public Window initscr ()
		{
			FindNCurses ();
			
			main_window = new Window (real_initscr ());
			try {
				console_sharp_get_dims (out lines, out cols);
			} catch (DllNotFoundException){
				endwin ();
				Console.Error.WriteLine ("Unable to find the @MONO_CURSES@ native library\n" + 
							 "this is different than the managed mono-curses.dll\n\n" +
							 "Typically you need to install to a LD_LIBRARY_PATH directory\n" +
							 "or DYLD_LIBRARY_PATH directory or run /sbin/ldconfig");
				Environment.Exit (1);
			}
			return main_window;
		}

		public static int Lines {	
			get {
				return lines;
			}
		}

		public static int Cols {
			get {
				return cols;
			}
		}

		//
		// Returns true if the window changed since the last invocation, as a
		// side effect, the Lines and Cols properties are updated
		//
		public static bool CheckWinChange ()
		{
			int l, c;
			
			console_sharp_get_dims (out l, out c);
			if (l != lines || c != cols){
				lines = l;
				cols = c;
				return true;
			}
			return false;
		}
		
		[DllImport ("ncurses")]
		extern static public int endwin ();

		[DllImport ("ncurses")]
		extern static public bool isendwin ();

		//
		// Screen operations are flagged as internal, as we need to
		// catch all changes so we can update newscr, curscr, stdscr
		//
		[DllImport ("ncurses")]
		extern static public IntPtr internal_newterm (string type, IntPtr file_outfd, IntPtr file_infd);

		[DllImport ("ncurses")]
		extern static public IntPtr internal_set_term (IntPtr newscreen);

		[DllImport ("ncurses")]
	        extern static internal void internal_delscreen (IntPtr sp);
#endregion

#region Input Options
		[DllImport ("ncurses")]
		extern static public int cbreak ();
		
		[DllImport ("ncurses")]
		extern static public int nocbreak ();
		
		[DllImport ("ncurses")]
		extern static public int echo ();
		
		[DllImport ("ncurses")]
		extern static public int noecho ();
		
		[DllImport ("ncurses")]
		extern static public int halfdelay (int t);

		[DllImport ("ncurses")]
		extern static public int raw ();

		[DllImport ("ncurses")]
		extern static public int noraw ();
		
		[DllImport ("ncurses")]
		extern static public void noqiflush ();
		
		[DllImport ("ncurses")]
		extern static public void qiflush ();

		[DllImport ("ncurses")]
		extern static public int typeahead (IntPtr fd);

		[DllImport ("ncurses")]
		extern static public int timeout (int delay);

		//
		// Internal, as they are exposed in Window
		//
		[DllImport ("ncurses")]
		extern static internal int wtimeout (IntPtr win, int delay);
	       
		[DllImport ("ncurses")]
		extern static internal int notimeout (IntPtr win, bool bf);

		[DllImport ("ncurses")]
		extern static internal int keypad (IntPtr win, bool bf);
		
		[DllImport ("ncurses")]
		extern static internal int meta (IntPtr win, bool bf);
		
		[DllImport ("ncurses")]
		extern static internal int intrflush (IntPtr win, bool bf);
#endregion

#region Output Options
		[DllImport ("ncurses")]
		extern internal static int clearok (IntPtr win, bool bf);
		[DllImport ("ncurses")]
		extern internal static int idlok (IntPtr win, bool bf);
		[DllImport ("ncurses")]
		extern internal static void idcok (IntPtr win, bool bf);
		[DllImport ("ncurses")]
		extern internal static void immedok (IntPtr win, bool bf);
		[DllImport ("ncurses")]
		extern internal static int leaveok (IntPtr win, bool bf);
		[DllImport ("ncurses")]
		extern internal static int wsetscrreg (IntPtr win, int top, int bot);
		[DllImport ("ncurses")]
		extern internal static int scrollok (IntPtr win, bool bf);
		
		[DllImport ("ncurses")]
		extern public static int nl();
		[DllImport ("ncurses")]
		extern public static int nonl();
		[DllImport ("ncurses")]
		extern public static int setscrreg (int top, int bot);
		
#endregion

#region refresh functions

		[DllImport ("ncurses")]
		extern public static int refresh ();
		[DllImport ("ncurses")]
		extern public static int doupdate();

		[DllImport ("ncurses")]
		extern internal static int wrefresh (IntPtr win);
		[DllImport ("ncurses")]
		extern internal static int redrawwin (IntPtr win);
		[DllImport ("ncurses")]
		extern internal static int wredrawwin (IntPtr win, int beg_line, int num_lines);
		[DllImport ("ncurses")]
		extern internal static int wnoutrefresh (IntPtr win);
#endregion

#region Output
		[DllImport ("ncurses")]
		extern public static int move (int line, int col);

		[DllImport ("ncurses", EntryPoint="addch")]
		extern internal static int _addch (int ch);
		
		[DllImport ("ncurses")]
		extern public static int addstr (string s);

		public static int addstr (string format, params object [] args)
		{
			var s = string.Format (format, args);
			return addstr (s);
		}

		static char [] r = new char [1];

		//
		// Have to wrap the native addch, as it can not
		// display unicode characters, we have to use addstr
		// for that.   but we need addch to render special ACS
		// characters
		//
		public static int addch (int ch)
		{
			if (ch < 127 || ch > 0xffff )
				return _addch (ch);
			char c = (char) ch;
			return addstr (new String (c, 1));
		}
		
		[DllImport ("ncurses")]
		extern internal static int wmove (IntPtr win, int line, int col);

		[DllImport ("ncurses")]
		extern internal static int waddch (IntPtr win, int ch);
#endregion

#region Attributes
		[DllImport ("ncurses")]
		extern public static int attron (int attrs);
		[DllImport ("ncurses")]
		extern public static int attroff (int attrs);
		[DllImport ("ncurses")]
		extern public static int attrset (int attrs);
#endregion

#region Input
		[DllImport ("ncurses")]
		extern public static int getch ();
		
		[DllImport ("ncurses")]
		extern public static int get_wch (out int sequence);

		[DllImport ("ncurses")]
		extern public static int ungetch (int ch);

		[DllImport ("ncurses")]
		extern public static int mvgetch (int y, int x);
#endregion
		
#region Colors
		[DllImport ("ncurses")]
		extern internal static bool has_colors ();
		public static bool HasColors => has_colors ();

		[DllImport ("ncurses")]
		extern internal static int start_color ();
		public static int StartColor () => start_color ();

		[DllImport ("ncurses")]
		extern internal static int init_pair (short pair, short f, short b);
		public static int InitColorPair (short pair, short foreground, short background) => init_pair (pair, foreground, background);

		[DllImport ("ncurses")]
		extern internal static int use_default_colors ();
		public static int UseDefaultColors () => use_default_colors ();

		[DllImport ("ncurses")]
		extern internal static int COLOR_PAIRS();
		public static int ColorPairs => COLOR_PAIRS();
		
		
#endregion
		
		[DllImport ("dl")]
		extern static IntPtr dlopen (string file, int mode);

		[DllImport ("dl")]
		extern static IntPtr dlsym (IntPtr handle, string symbol);

		static IntPtr stdscr;

		static IntPtr get_ptr (string key)
		{
			var ptr = dlsym (curses_handle, key);
			if (ptr == IntPtr.Zero)
				throw new Exception ("Could not load the key " + key);
			return ptr;
		}
		
		internal static IntPtr read_static_ptr (string key)
		{
			var ptr = get_ptr (key);
			return Marshal.ReadIntPtr (ptr);
		}

		internal static IntPtr console_sharp_get_stdscr () => stdscr;
		
		
#region Helpers
		internal static IntPtr console_sharp_get_curscr ()
		{
			return Marshal.ReadIntPtr (curscr_ptr);
		}

		internal static void console_sharp_get_dims (out int lines, out int cols)
		{
			lines = Marshal.ReadInt32 (lines_ptr);
			cols = Marshal.ReadInt32 (cols_ptr);
		}

		[DllImport ("ncurses", EntryPoint="mousemask")]
		extern static IntPtr call_mousemask (IntPtr newmask, out IntPtr oldmask);
		
		public static Event mousemask (Event newmask, out Event oldmask)
		{
			IntPtr e;
			var ret = (Event) call_mousemask ((IntPtr) newmask, out e);
			oldmask = (Event) e;
			return ret;
		}

		[DllImport ("ncurses")]
		public extern static uint getmouse (out MouseEvent ev);

		[DllImport ("ncurses")]
		public extern static uint ungetmouse (ref MouseEvent ev);

		[DllImport ("ncurses")]
		public extern static int mouseinterval (int interval);
#endregion

		// We encode ESC + char (what Alt-char generates) as 0x2000 + char
		public const int KeyAlt = 0x2000;

		static public int IsAlt (int key)
		{
			if ((key & KeyAlt) != 0)
				return key & ~KeyAlt;
			return 0;
		}
	}
}
