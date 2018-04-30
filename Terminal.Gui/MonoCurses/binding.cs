
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

		static int lines, cols;
		static Window main_window;
		static IntPtr curses_handle, curscr_ptr, lines_ptr, cols_ptr;

		// If true, uses the DllImport into "ncurses", otherwise "libncursesw.so.5"
		static bool use_naked_driver;

		//
		// Ugly hack to P/Invoke into either libc, or libdl, again, because
		// we can not have nice things - .NET Core in this day and age still
		// does not have <dllmap>
		//
		static IntPtr DlOpen (string path)
		{
			if (!uselibc){
				try {
					var handle = dlopen (path, 1);
					return handle;
				} catch (DllNotFoundException){
					uselibc = true;
					return DlOpen (path);
				}
			} else {
				return libc_dlopen (path, 1);
			}
		}
		
		static void FindNCurses ()
		{
			if (File.Exists ("/usr/lib/libncurses.dylib")){
				curses_handle = DlOpen ("libncurses.dylib");
				use_naked_driver = true;
			} else 
				curses_handle = DlOpen ("libncursesw.so.5");

			if (curses_handle == IntPtr.Zero) {
				Console.WriteLine ("It is not possible to open the dynamic library ncurses, tried looking for libncurses.dylib on Mac, and libncursesw.so.5 on Linux");
				Environment.Exit (1);
			}
			
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
		
		[DllImport ("dl")]
		extern static IntPtr dlopen (string file, int mode);

		[DllImport ("dl")]
		extern static IntPtr dlsym (IntPtr handle, string symbol);

		[DllImport ("libc", EntryPoint="dlopen")]
		extern static IntPtr libc_dlopen (string file, int mode);

		[DllImport ("libc", EntryPoint ="dlsym")]
		extern static IntPtr libc_dlsym (IntPtr handle, string symbol);

		static bool uselibc;

		static IntPtr stdscr;

		static IntPtr get_ptr (string key)
		{
			var ptr = uselibc ? libc_dlsym (curses_handle, key) : dlsym (curses_handle, key);

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
		
		
		internal static IntPtr console_sharp_get_curscr ()
		{
			return Marshal.ReadIntPtr (curscr_ptr);
		}

		internal static void console_sharp_get_dims (out int lines, out int cols)
		{
			lines = Marshal.ReadInt32 (lines_ptr);
			cols = Marshal.ReadInt32 (cols_ptr);
		}

		public static Event mousemask (Event newmask, out Event oldmask)
		{
			IntPtr e;
			var ret = (Event) (use_naked_driver ? RegularCurses.call_mousemask ((IntPtr) newmask, out e) : CursesLinux.call_mousemask ((IntPtr) newmask, out e));
			oldmask = (Event) e;
			return ret;
		}


		// We encode ESC + char (what Alt-char generates) as 0x2000 + char
		public const int KeyAlt = 0x2000;

		static public int IsAlt (int key)
		{
			if ((key & KeyAlt) != 0)
				return key & ~KeyAlt;
			return 0;
		}
		public static int StartColor () => start_color ();
		public static bool HasColors => has_colors ();
		public static int InitColorPair (short pair, short foreground, short background) => init_pair (pair, foreground, background);
		public static int UseDefaultColors () => use_default_colors ();
		public static int ColorPairs => COLOR_PAIRS();


		//
		// The proxy methods to call into each version
		//
		static public IntPtr real_initscr () => use_naked_driver ? RegularCurses.real_initscr () : CursesLinux.real_initscr ();
		static public int endwin () => use_naked_driver ? RegularCurses.endwin () : CursesLinux.endwin ();
		static public bool isendwin () => use_naked_driver ? RegularCurses.isendwin () : CursesLinux.isendwin ();
		static public IntPtr internal_newterm (string type, IntPtr file_outfd, IntPtr file_infd) => use_naked_driver ? RegularCurses.internal_newterm (type, file_outfd, file_infd) : CursesLinux.internal_newterm (type, file_outfd, file_infd);
		static public IntPtr internal_set_term (IntPtr newscreen) => use_naked_driver ? RegularCurses.internal_set_term (newscreen) : CursesLinux.internal_set_term (newscreen);
		static public void internal_delscreen (IntPtr sp) { if (use_naked_driver) RegularCurses.internal_delscreen (sp); else CursesLinux.internal_delscreen (sp); }
		static public int cbreak () => use_naked_driver ? RegularCurses.cbreak () : CursesLinux.cbreak ();
		static public int nocbreak () => use_naked_driver ? RegularCurses.nocbreak () : CursesLinux.nocbreak ();
		static public int echo () => use_naked_driver ? RegularCurses.echo () : CursesLinux.echo ();
		static public int noecho () => use_naked_driver ? RegularCurses.noecho () : CursesLinux.noecho ();
		static public int halfdelay (int t) => use_naked_driver ? RegularCurses.halfdelay (t) : CursesLinux.halfdelay (t);
		static public int raw () => use_naked_driver ? RegularCurses.raw () : CursesLinux.raw ();
		static public int noraw () => use_naked_driver ? RegularCurses.noraw () : CursesLinux.noraw ();
		static public void noqiflush () { if (use_naked_driver) RegularCurses.noqiflush (); else CursesLinux.noqiflush (); }
		static public void qiflush () { if (use_naked_driver) RegularCurses.qiflush (); else CursesLinux.qiflush (); }
		static public int typeahead (IntPtr fd) => use_naked_driver ? RegularCurses.typeahead (fd) : CursesLinux.typeahead (fd);
		static public int timeout (int delay) => use_naked_driver ? RegularCurses.timeout (delay) : CursesLinux.timeout (delay);
		static public int wtimeout (IntPtr win, int delay) => use_naked_driver ? RegularCurses.wtimeout (win, delay) : CursesLinux.wtimeout (win, delay);
		static public int notimeout (IntPtr win, bool bf) => use_naked_driver ? RegularCurses.notimeout (win, bf) : CursesLinux.notimeout (win, bf);
		static public int keypad (IntPtr win, bool bf) => use_naked_driver ? RegularCurses.keypad (win, bf) : CursesLinux.keypad (win, bf);
		static public int meta (IntPtr win, bool bf) => use_naked_driver ? RegularCurses.meta (win, bf) : CursesLinux.meta (win, bf);
		static public int intrflush (IntPtr win, bool bf) => use_naked_driver ? RegularCurses.intrflush (win, bf) : CursesLinux.intrflush (win, bf);
		static public int clearok (IntPtr win, bool bf) => use_naked_driver ? RegularCurses.clearok (win, bf) : CursesLinux.clearok (win, bf);
		static public int idlok (IntPtr win, bool bf) => use_naked_driver ? RegularCurses.idlok (win, bf) : CursesLinux.idlok (win, bf);
		static public void idcok (IntPtr win, bool bf) { if (use_naked_driver) RegularCurses.idcok (win, bf); else CursesLinux.idcok (win, bf);}
		static public void immedok (IntPtr win, bool bf) { if (use_naked_driver) RegularCurses.immedok (win, bf); else CursesLinux.immedok (win, bf);}
		static public int leaveok (IntPtr win, bool bf) => use_naked_driver ? RegularCurses.leaveok (win, bf) : CursesLinux.leaveok (win, bf);
		static public int wsetscrreg (IntPtr win, int top, int bot) => use_naked_driver ? RegularCurses.wsetscrreg (win, top, bot) : CursesLinux.wsetscrreg (win, top, bot);
		static public int scrollok (IntPtr win, bool bf) => use_naked_driver ? RegularCurses.scrollok (win, bf) : CursesLinux.scrollok (win, bf);
		static public int nl() => use_naked_driver ? RegularCurses.nl() : CursesLinux.nl();
		static public int nonl() => use_naked_driver ? RegularCurses.nonl() : CursesLinux.nonl();
		static public int setscrreg (int top, int bot) => use_naked_driver ? RegularCurses.setscrreg (top, bot) : CursesLinux.setscrreg (top, bot);
		static public int refresh () => use_naked_driver ? RegularCurses.refresh () : CursesLinux.refresh ();
		static public int doupdate() => use_naked_driver ? RegularCurses.doupdate() : CursesLinux.doupdate();
		static public int wrefresh (IntPtr win) => use_naked_driver ? RegularCurses.wrefresh (win) : CursesLinux.wrefresh (win);
		static public int redrawwin (IntPtr win) => use_naked_driver ? RegularCurses.redrawwin (win) : CursesLinux.redrawwin (win);
		static public int wredrawwin (IntPtr win, int beg_line, int num_lines) => use_naked_driver ? RegularCurses.wredrawwin (win, beg_line, num_lines) : CursesLinux.wredrawwin (win, beg_line, lines);
		static public int wnoutrefresh (IntPtr win) => use_naked_driver ? RegularCurses.wnoutrefresh (win) : CursesLinux.wnoutrefresh (win);
		static public int move (int line, int col) => use_naked_driver ? RegularCurses.move (line, col) : CursesLinux.move (line, col);
		static public int _addch (int ch) => use_naked_driver ? RegularCurses._addch (ch) : CursesLinux._addch (ch);
		static public int addstr (string s) => use_naked_driver ? RegularCurses.addstr (s) : CursesLinux.addstr (s);
		static public int wmove (IntPtr win, int line, int col) => use_naked_driver ? RegularCurses.wmove (win, line, col) : CursesLinux.wmove (win, line, col);
		static public int waddch (IntPtr win, int ch) => use_naked_driver ? RegularCurses.waddch (win, ch) : CursesLinux.waddch (win, ch);
		static public int attron (int attrs) => use_naked_driver ? RegularCurses.attron (attrs) : CursesLinux.attron (attrs);
		static public int attroff (int attrs) => use_naked_driver ? RegularCurses.attroff (attrs) : CursesLinux.attroff (attrs);
		static public int attrset (int attrs) => use_naked_driver ? RegularCurses.attrset (attrs) : CursesLinux.attrset (attrs);
		static public int getch () => use_naked_driver ? RegularCurses.getch () : CursesLinux.getch ();
		static public int get_wch (out int sequence) => use_naked_driver ? RegularCurses.get_wch (out sequence) : CursesLinux.get_wch (out sequence);
		static public int ungetch (int ch) => use_naked_driver ? RegularCurses.ungetch (ch) : CursesLinux.ungetch (ch);
		static public int mvgetch (int y, int x) => use_naked_driver ? RegularCurses.mvgetch (y, x) : CursesLinux.mvgetch (y, x);
		static public bool has_colors () => use_naked_driver ? RegularCurses.has_colors () : CursesLinux.has_colors ();
		static public int start_color () => use_naked_driver ? RegularCurses.start_color () : CursesLinux.start_color ();
		static public int init_pair (short pair, short f, short b) => use_naked_driver ? RegularCurses.init_pair (pair, f, b) : CursesLinux.init_pair (pair, f, b);
		static public int use_default_colors () => use_naked_driver ? RegularCurses.use_default_colors () : CursesLinux.use_default_colors ();
		static public int COLOR_PAIRS() => use_naked_driver ? RegularCurses.COLOR_PAIRS() : CursesLinux.COLOR_PAIRS();
		static public uint getmouse (out MouseEvent ev) => use_naked_driver ? RegularCurses.getmouse (out ev) : CursesLinux.getmouse (out ev);
		static public uint ungetmouse (ref MouseEvent ev) => use_naked_driver ? RegularCurses.ungetmouse (ref ev) : CursesLinux.ungetmouse (ref ev);
		static public int mouseinterval (int interval) => use_naked_driver ? RegularCurses.mouseinterval (interval) : CursesLinux.mouseinterval (interval);
	}
	
	//
	// P/Invoke definitions for looking up symbols in the "ncurses" library, as resolved
	// by the dynamic linker, different than CursesLinux that looksup by "libncursesw.so.5"
	//
	internal class RegularCurses {
		[DllImport ("ncurses", EntryPoint="initscr")]
		extern static internal IntPtr real_initscr ();

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

		[DllImport ("ncurses")]
		extern public static int move (int line, int col);

		[DllImport ("ncurses", EntryPoint="addch")]
		extern internal static int _addch (int ch);
		
		[DllImport ("ncurses")]
		extern public static int addstr (string s);

		[DllImport ("ncurses")]
		extern internal static int wmove (IntPtr win, int line, int col);

		[DllImport ("ncurses")]
		extern internal static int waddch (IntPtr win, int ch);

		[DllImport ("ncurses")]
		extern public static int attron (int attrs);
		[DllImport ("ncurses")]
		extern public static int attroff (int attrs);
		[DllImport ("ncurses")]
		extern public static int attrset (int attrs);

		[DllImport ("ncurses")]
		extern public static int getch ();
		
		[DllImport ("ncurses")]
		extern public static int get_wch (out int sequence);

		[DllImport ("ncurses")]
		extern public static int ungetch (int ch);

		[DllImport ("ncurses")]
		extern public static int mvgetch (int y, int x);

		[DllImport ("ncurses")]
		extern internal static bool has_colors ();

		[DllImport ("ncurses")]
		extern internal static int start_color ();

		[DllImport ("ncurses")]
		extern internal static int init_pair (short pair, short f, short b);

		[DllImport ("ncurses")]
		extern internal static int use_default_colors ();

		[DllImport ("ncurses")]
		extern internal static int COLOR_PAIRS();
		
		[DllImport ("ncurses")]
		public extern static uint getmouse (out Curses.MouseEvent ev);

		[DllImport ("ncurses")]
		public extern static uint ungetmouse (ref Curses.MouseEvent ev);

		[DllImport ("ncurses")]
		public extern static int mouseinterval (int interval);
		
		[DllImport ("ncurses", EntryPoint="mousemask")]
		public extern static IntPtr call_mousemask (IntPtr newmask, out IntPtr oldmask);
		
	}
	
	//
	// P/Invoke definitions for looking up symbols in the "libncursesw.so.5" library, as resolved
	// by the dynamic linker, different than RegularCurses that looksup by "ncurses"
	//
	internal class CursesLinux {
		[DllImport ("libncursesw.so.5", EntryPoint="mousemask")]
		public extern static IntPtr call_mousemask (IntPtr newmask, out IntPtr oldmask);
		
		[DllImport ("libncursesw.so.5", EntryPoint="initscr")]
		extern static internal IntPtr real_initscr ();

		[DllImport ("libncursesw.so.5")]
		extern static public int endwin ();

		[DllImport ("libncursesw.so.5")]
		extern static public bool isendwin ();

		//
		// Screen operations are flagged as internal, as we need to
		// catch all changes so we can update newscr, curscr, stdscr
		//
		[DllImport ("libncursesw.so.5")]
		extern static public IntPtr internal_newterm (string type, IntPtr file_outfd, IntPtr file_infd);

		[DllImport ("libncursesw.so.5")]
		extern static public IntPtr internal_set_term (IntPtr newscreen);

		[DllImport ("libncursesw.so.5")]
	        extern static internal void internal_delscreen (IntPtr sp);

		[DllImport ("libncursesw.so.5")]
		extern static public int cbreak ();
		
		[DllImport ("libncursesw.so.5")]
		extern static public int nocbreak ();
		
		[DllImport ("libncursesw.so.5")]
		extern static public int echo ();
		
		[DllImport ("libncursesw.so.5")]
		extern static public int noecho ();
		
		[DllImport ("libncursesw.so.5")]
		extern static public int halfdelay (int t);

		[DllImport ("libncursesw.so.5")]
		extern static public int raw ();

		[DllImport ("libncursesw.so.5")]
		extern static public int noraw ();
		
		[DllImport ("libncursesw.so.5")]
		extern static public void noqiflush ();
		
		[DllImport ("libncursesw.so.5")]
		extern static public void qiflush ();

		[DllImport ("libncursesw.so.5")]
		extern static public int typeahead (IntPtr fd);

		[DllImport ("libncursesw.so.5")]
		extern static public int timeout (int delay);

		//
		// Internal, as they are exposed in Window
		//
		[DllImport ("libncursesw.so.5")]
		extern static internal int wtimeout (IntPtr win, int delay);
	       
		[DllImport ("libncursesw.so.5")]
		extern static internal int notimeout (IntPtr win, bool bf);

		[DllImport ("libncursesw.so.5")]
		extern static internal int keypad (IntPtr win, bool bf);
		
		[DllImport ("libncursesw.so.5")]
		extern static internal int meta (IntPtr win, bool bf);
		
		[DllImport ("libncursesw.so.5")]
		extern static internal int intrflush (IntPtr win, bool bf);

		[DllImport ("libncursesw.so.5")]
		extern internal static int clearok (IntPtr win, bool bf);
		[DllImport ("libncursesw.so.5")]
		extern internal static int idlok (IntPtr win, bool bf);
		[DllImport ("libncursesw.so.5")]
		extern internal static void idcok (IntPtr win, bool bf);
		[DllImport ("libncursesw.so.5")]
		extern internal static void immedok (IntPtr win, bool bf);
		[DllImport ("libncursesw.so.5")]
		extern internal static int leaveok (IntPtr win, bool bf);
		[DllImport ("libncursesw.so.5")]
		extern internal static int wsetscrreg (IntPtr win, int top, int bot);
		[DllImport ("libncursesw.so.5")]
		extern internal static int scrollok (IntPtr win, bool bf);
		
		[DllImport ("libncursesw.so.5")]
		extern public static int nl();
		[DllImport ("libncursesw.so.5")]
		extern public static int nonl();
		[DllImport ("libncursesw.so.5")]
		extern public static int setscrreg (int top, int bot);
		

		[DllImport ("libncursesw.so.5")]
		extern public static int refresh ();
		[DllImport ("libncursesw.so.5")]
		extern public static int doupdate();

		[DllImport ("libncursesw.so.5")]
		extern internal static int wrefresh (IntPtr win);
		[DllImport ("libncursesw.so.5")]
		extern internal static int redrawwin (IntPtr win);
		[DllImport ("libncursesw.so.5")]
		extern internal static int wredrawwin (IntPtr win, int beg_line, int num_lines);
		[DllImport ("libncursesw.so.5")]
		extern internal static int wnoutrefresh (IntPtr win);

		[DllImport ("libncursesw.so.5")]
		extern public static int move (int line, int col);

		[DllImport ("libncursesw.so.5", EntryPoint="addch")]
		extern internal static int _addch (int ch);
		
		[DllImport ("libncursesw.so.5")]
		extern public static int addstr (string s);

		[DllImport ("libncursesw.so.5")]
		extern internal static int wmove (IntPtr win, int line, int col);

		[DllImport ("libncursesw.so.5")]
		extern internal static int waddch (IntPtr win, int ch);

		[DllImport ("libncursesw.so.5")]
		extern public static int attron (int attrs);
		[DllImport ("libncursesw.so.5")]
		extern public static int attroff (int attrs);
		[DllImport ("libncursesw.so.5")]
		extern public static int attrset (int attrs);

		[DllImport ("libncursesw.so.5")]
		extern public static int getch ();
		
		[DllImport ("libncursesw.so.5")]
		extern public static int get_wch (out int sequence);

		[DllImport ("libncursesw.so.5")]
		extern public static int ungetch (int ch);

		[DllImport ("libncursesw.so.5")]
		extern public static int mvgetch (int y, int x);

		[DllImport ("libncursesw.so.5")]
		extern internal static bool has_colors ();

		[DllImport ("libncursesw.so.5")]
		extern internal static int start_color ();

		[DllImport ("libncursesw.so.5")]
		extern internal static int init_pair (short pair, short f, short b);

		[DllImport ("libncursesw.so.5")]
		extern internal static int use_default_colors ();

		[DllImport ("libncursesw.so.5")]
		extern internal static int COLOR_PAIRS();
		
		[DllImport ("libncursesw.so.5")]
		public extern static uint getmouse (out Curses.MouseEvent ev);

		[DllImport ("libncursesw.so.5")]
		public extern static uint ungetmouse (ref Curses.MouseEvent ev);

		[DllImport ("libncursesw.so.5")]
		public extern static int mouseinterval (int interval);
	}
	
}
