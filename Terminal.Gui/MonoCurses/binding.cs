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
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Unix.Terminal {
	internal class Delegates {
		public delegate IntPtr initscr ();
		public delegate int endwin ();
		public delegate bool isendwin ();
		public delegate int cbreak ();
		public delegate int nocbreak ();
		public delegate int echo ();
		public delegate int noecho ();
		public delegate int halfdelay (int t);
		public delegate int raw ();
		public delegate int noraw ();
		public delegate void noqiflush ();
		public delegate void qiflush ();
		public delegate int typeahead (IntPtr fd);
		public delegate int timeout (int delay);
		public delegate int wtimeout (IntPtr win, int delay);
		public delegate int notimeout (IntPtr win, bool bf);
		public delegate int keypad (IntPtr win, bool bf);
		public delegate int meta (IntPtr win, bool bf);
		public delegate int intrflush (IntPtr win, bool bf);
		public delegate int clearok (IntPtr win, bool bf);
		public delegate int idlok (IntPtr win, bool bf);
		public delegate void idcok (IntPtr win, bool bf);
		public delegate void immedok (IntPtr win, bool bf);
		public delegate int leaveok (IntPtr win, bool bf);
		public delegate int wsetscrreg (IntPtr win, int top, int bot);
		public delegate int scrollok (IntPtr win, bool bf);
		public delegate int nl();
		public delegate int nonl();
		public delegate int setscrreg (int top, int bot);
		public delegate int refresh ();
		public delegate int doupdate();
		public delegate int wrefresh (IntPtr win);
		public delegate int redrawwin (IntPtr win);
		public delegate int wredrawwin (IntPtr win, int beg_line, int num_lines);
		public delegate int wnoutrefresh (IntPtr win);
		public delegate int move (int line, int col);
		public delegate int addch (int ch);
		public delegate int addstr (string s);
		public delegate int wmove (IntPtr win, int line, int col);
		public delegate int waddch (IntPtr win, int ch);
		public delegate int attron (int attrs);
		public delegate int attroff (int attrs);
		public delegate int attrset (int attrs);
		public delegate int getch ();
		public delegate int get_wch (out int sequence);
		public delegate int ungetch (int ch);
		public delegate int mvgetch (int y, int x);
		public delegate bool has_colors ();
		public delegate int start_color ();
		public delegate int init_pair (short pair, short f, short b);
		public delegate int use_default_colors ();
		public delegate int COLOR_PAIRS();
		public delegate uint getmouse (out MouseEvent ev);
		public delegate uint ungetmouse (ref MouseEvent ev);
		public delegate int mouseinterval (int interval);
	}

	internal class NativeMethods {
		public readonly Delegates.initscr initscr;
		public readonly Delegates.endwin endwin;
		public readonly Delegates.isendwin isendwin;
		public readonly Delegates.cbreak cbreak;
		public readonly Delegates.nocbreak nocbreak;
		public readonly Delegates.echo echo;
		public readonly Delegates.noecho noecho;
		public readonly Delegates.halfdelay halfdelay;
		public readonly Delegates.raw raw;
		public readonly Delegates.noraw noraw;
		public readonly Delegates.noqiflush noqiflush;
		public readonly Delegates.qiflush qiflush;
		public readonly Delegates.typeahead typeahead;
		public readonly Delegates.timeout timeout;
		public readonly Delegates.wtimeout wtimeout;
		public readonly Delegates.notimeout notimeout;
		public readonly Delegates.keypad keypad;
		public readonly Delegates.meta meta;
		public readonly Delegates.intrflush intrflush;
		public readonly Delegates.clearok clearok;
		public readonly Delegates.idlok idlok;
		public readonly Delegates.idcok idcok;
		public readonly Delegates.immedok immedok;
		public readonly Delegates.leaveok leaveok;
		public readonly Delegates.wsetscrreg wsetscrreg;
		public readonly Delegates.scrollok scrollok;
		public readonly Delegates.nl nl;
		public readonly Delegates.nonl nonl;
		public readonly Delegates.setscrreg setscrreg;
		public readonly Delegates.refresh refresh;
		public readonly Delegates.doupdate doupdate;
		public readonly Delegates.wrefresh wrefresh;
		public readonly Delegates.redrawwin redrawwin;
		public readonly Delegates.wredrawwin wredrawwin;
		public readonly Delegates.wnoutrefresh wnoutrefresh;
		public readonly Delegates.move move;
		public readonly Delegates.addch addch;
		public readonly Delegates.addstr addstr;
		public readonly Delegates.wmove wmove;
		public readonly Delegates.waddch waddch;
		public readonly Delegates.attron attron;
		public readonly Delegates.attroff attroff;
		public readonly Delegates.attrset attrset;
		public readonly Delegates.getch getch;
		public readonly Delegates.get_wch get_wch;
		public readonly Delegates.ungetch ungetch;
		public readonly Delegates.mvgetch mvgetch;
		public readonly Delegates.has_colors has_colors;
		public readonly Delegates.start_color start_color;
		public readonly Delegates.init_pair init_pair;
		public readonly Delegates.use_default_colors use_default_colors;
		public readonly Delegates.COLOR_PAIR COLOR_PAIR;
		public readonly Delegates.getmouse getmouse;
		public readonly Delegates.ungetmouse ungetmouse;
		public readonly Delegates.mouseinterval mouseinterval;

		public void NativeMethods (UnmanagedLibrary lib)
		{
			initscr = lib.GetMethodDelegate<Delegates.initscr> ("initscr");
			endwin = lib.GetMethodDelegate<Delegates.endwin> ("endwin");
			isendwin = lib.GetMethodDelegate<Delegates.isendwin> ("isendwin");
			cbreak = lib.GetMethodDelegate<Delegates.cbreak> ("cbreak");
			nocbreak = lib.GetMethodDelegate<Delegates.nocbreak> ("nocbreak");
			echo = lib.GetMethodDelegate<Delegates.echo> ("echo");
			noecho = lib.GetMethodDelegate<Delegates.noecho> ("noecho");
			halfdelay = lib.GetMethodDelegate<Delegates.halfdelay> ("halfdelay");
			raw = lib.GetMethodDelegate<Delegates.raw> ("raw");
			noraw = lib.GetMethodDelegate<Delegates.noraw> ("noraw");
			noqiflush = lib.GetMethodDelegate<Delegates.noqiflush> ("noqiflush");
			qiflush = lib.GetMethodDelegate<Delegates.qiflush> ("qiflush");
			typeahead = lib.GetMethodDelegate<Delegates.typeahead> ("typeahead");
			timeout = lib.GetMethodDelegate<Delegates.timeout> ("timeout");
			wtimeout = lib.GetMethodDelegate<Delegates.wtimeout> ("wtimeout");
			notimeout = lib.GetMethodDelegate<Delegates.notimeout> ("notimeout");
			keypad = lib.GetMethodDelegate<Delegates.keypad> ("keypad");
			meta = lib.GetMethodDelegate<Delegates.meta> ("meta");
			intrflush = lib.GetMethodDelegate<Delegates.intrflush> ("intrflush");
			clearok = lib.GetMethodDelegate<Delegates.clearok> ("clearok");
			idlok = lib.GetMethodDelegate<Delegates.idlok> ("idlok");
			idcok = lib.GetMethodDelegate<Delegates.idcok> ("idcok");
			immedok = lib.GetMethodDelegate<Delegates.immedok> ("immedok");
			leaveok = lib.GetMethodDelegate<Delegates.leaveok> ("leaveok");
			wsetscrreg = lib.GetMethodDelegate<Delegates.wsetscrreg> ("wsetscrreg");
			scrollok = lib.GetMethodDelegate<Delegates.scrollok> ("scrollok");
			nl = lib.GetMethodDelegate<Delegates.nl> ("nl");
			nonl = lib.GetMethodDelegate<Delegates.nonl> ("nonl");
			setscrreg = lib.GetMethodDelegate<Delegates.setscrreg> ("setscrreg");
			refresh = lib.GetMethodDelegate<Delegates.refresh> ("refresh");
			doupdate = lib.GetMethodDelegate<Delegates.doupdate> ("doupdate");
			wrefresh = lib.GetMethodDelegate<Delegates.wrefresh> ("wrefresh");
			redrawwin = lib.GetMethodDelegate<Delegates.redrawwin> ("redrawwin");
			wredrawwin = lib.GetMethodDelegate<Delegates.wredrawwin> ("wredrawwin");
			wnoutrefresh = lib.GetMethodDelegate<Delegates.wnoutrefresh> ("wnoutrefresh");
			move = lib.GetMethodDelegate<Delegates.move> ("move");
			addch = lib.GetMethodDelegate<Delegates.addch> ("addch");
			addstr = lib.GetMethodDelegate<Delegates.addstr> ("addstr");
			wmove = lib.GetMethodDelegate<Delegates.wmove> ("wmove");
			waddch = lib.GetMethodDelegate<Delegates.waddch> ("waddch");
			attron = lib.GetMethodDelegate<Delegates.attron> ("attron");
			attroff = lib.GetMethodDelegate<Delegates.attroff> ("attroff");
			attrset = lib.GetMethodDelegate<Delegates.attrset> ("attrset");
			getch = lib.GetMethodDelegate<Delegates.getch> ("getch");
			get_wch = lib.GetMethodDelegate<Delegates.get_wch> ("get_wch");
			ungetch = lib.GetMethodDelegate<Delegates.ungetch> ("ungetch");
			mvgetch = lib.GetMethodDelegate<Delegates.mvgetch> ("mvgetch");
			has_colors = lib.GetMethodDelegate<Delegates.has_colors> ("has_colors");
			start_color = lib.GetMethodDelegate<Delegates.start_color> ("start_color");
			init_pair = lib.GetMethodDelegate<Delegates.init_pair> ("init_pair");
			use_default_colors = lib.GetMethodDelegate<Delegates.use_default_colors> ("use_default_colors");
			COLOR_PAIR = lib.GetMethodDelegate<Delegates.COLOR_PAIR> ("COLOR_PAIR");
			getmouse = lib.GetMethodDelegate<Delegates.getmouse> ("getmouse");
			ungetmouse = lib.GetMethodDelegate<Delegates.ungetmouse> ("ungetmouse");
			mouseinterval = lib.GetMethodDelegate<Delegates.mouseinterval> ("mouseinterval");
		}
	}

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

		NativeMethods methods;
		
		static void LoadMethods ()
		{
			var libs = UnmanagedLibrary.IsMacOSPlatform ? new string [] { "libncurses.dylib" } : new string { "libncursesw.so.6", "libncursesw.so.5" };
			var lib = new UnmanagedLibrary (libs);
			methods = new NativeMethods (lib);
		}
		
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
			
			main_window = new Window (methods.initscr ());
			try {
				console_sharp_get_dims (out lines, out bcols);
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
			var ret = (Event) (methods.call_mousemask ((IntPtr) newmask, out e));
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
		public static int StartColor () => methods.start_color ();
		public static bool HasColors => methods.has_colors ();
		public static int InitColorPair (short pair, short foreground, short background) => methods.init_pair (pair, foreground, background);
		public static int UseDefaultColors () => methods.use_default_colors ();
		public static int ColorPairs => methods.COLOR_PAIRS();

		//
		// The proxy methods to call into each version
		//
		static public IntPtr real_initscr () => methods.real_initscr ();
		static public int endwin () => methods.endwin ();
		static public bool isendwin () => methods.isendwin ();
		static public int cbreak () => methods.cbreak ();
		static public int nocbreak () => methods.nocbreak ();
		static public int echo () => methods.echo ();
		static public int noecho () => methods.noecho ();
		static public int halfdelay (int t) => methods.halfdelay (t);
		static public int raw () => methods.raw ();
		static public int noraw () => methods.noraw ();
		static public void noqiflush () => { methods.noqiflush (); };
		static public void qiflush () => { methods.qiflush (); };
		static public int typeahead (IntPtr fd) => methods.typeahead (fd);
		static public int timeout (int delay) => methods.timeout (delay);
		static public int wtimeout (IntPtr win, int delay) => methods.wtimeout (win, delay);
		static public int notimeout (IntPtr win, bool bf) => methods.notimeout (win, bf);
		static public int keypad (IntPtr win, bool bf) => methods.keypad (win, bf);
		static public int meta (IntPtr win, bool bf) => methods.meta (win, bf);
		static public int intrflush (IntPtr win, bool bf) => methods.intrflush (win, bf);
		static public int clearok (IntPtr win, bool bf) => methods.clearok (win, bf);
		static public int idlok (IntPtr win, bool bf) => methods.idlok (win, bf);
		static public void idcok (IntPtr win, bool bf) { if (use_naked_driver) RegularCurses.idcok (win, bf); else CursesLinux.idcok (win, bf);}
		static public void immedok (IntPtr win, bool bf) { if (use_naked_driver) RegularCurses.immedok (win, bf); else CursesLinux.immedok (win, bf);}
		static public int leaveok (IntPtr win, bool bf) => methods.leaveok (win, bf);
		static public int wsetscrreg (IntPtr win, int top, int bot) => methods.wsetscrreg (win, top, bot);
		static public int scrollok (IntPtr win, bool bf) => methods.scrollok (win, bf);
		static public int nl() => methods.nl();
		static public int nonl() => methods.nonl();
		static public int setscrreg (int top, int bot) => methods.setscrreg (top, bot);
		static public int refresh () => methods.refresh ();
		static public int doupdate() => methods.doupdate();
		static public int wrefresh (IntPtr win) => methods.wrefresh (win);
		static public int redrawwin (IntPtr win) => methods.redrawwin (win);
		static public int wredrawwin (IntPtr win, int beg_line, int num_lines) => methods.wredrawwin (win, beg_line, num_lines);
		static public int wnoutrefresh (IntPtr win) => methods.wnoutrefresh (win);
		static public int move (int line, int col) => methods.move (line, col);
		static public int _addch (int ch) => methods._addch (ch);
		static public int addstr (string s) => methods.addstr (s);
		static public int wmove (IntPtr win, int line, int col) => methods.wmove (win, line, col);
		static public int waddch (IntPtr win, int ch) => methods.waddch (win, ch);
		static public int attron (int attrs) => methods.attron (attrs);
		static public int attroff (int attrs) => methods.attroff (attrs);
		static public int attrset (int attrs) => methods.attrset (attrs);
		static public int getch () => methods.getch ();
		static public int get_wch (out int sequence) => methods.get_wch (out sequence);
		static public int ungetch (int ch) => methods.ungetch (ch);
		static public int mvgetch (int y, int x) => methods.mvgetch (y, x);
		static public bool has_colors () => methods.has_colors ();
		static public int start_color () => methods.start_color ();
		static public int init_pair (short pair, short f, short b) => methods.init_pair (pair, f, b);
		static public int use_default_colors () => methods.use_default_colors ();
		static public int COLOR_PAIRS() => methods.COLOR_PAIRS();
		static public uint getmouse (out MouseEvent ev) => methods.getmouse (out ev);
		static public uint ungetmouse (ref MouseEvent ev) => methods.ungetmouse (ref ev);
		static public int mouseinterval (int interval) => methods.mouseinterval (interval);
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
