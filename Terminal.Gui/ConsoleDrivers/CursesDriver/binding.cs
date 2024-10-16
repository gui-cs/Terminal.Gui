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
using Terminal.Gui;

namespace Unix.Terminal;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public partial class Curses
{
    // We encode ESC + char (what Alt-char generates) as 0x2000 + char
    public const int KeyAlt = 0x2000;
    private static nint curses_handle, curscr_ptr, lines_ptr, cols_ptr;

    // If true, uses the DllImport into "ncurses", otherwise "libncursesw.so.5"
    //static bool use_naked_driver;
    private static UnmanagedLibrary curses_library;
    private static int lines, cols;
    private static Window main_window;
    private static NativeMethods methods;
    private static char [] r = new char [1];
    private static nint stdscr;
    public static int ColorPairs => methods.COLOR_PAIRS ();

    public static int Cols
    {
        get => cols;
        internal set =>

            // For unit tests
            cols = value;
    }

    public static bool HasColors => methods.has_colors ();

    public static int Lines
    {
        get => lines;
        internal set =>

            // For unit tests
            lines = value;
    }

    //
    // Have to wrap the native addch, as it can not
    // display unicode characters, we have to use addstr
    // for that.   but we need addch to render special ACS
    // characters
    //
    public static int addch (int ch)
    {
        if (ch < 127 || ch > 0xffff)
        {
            return methods.addch (ch);
        }

        var c = (char)ch;

        return addwstr (new string (c, 1));
    }

    public static int addstr (string format, params object [] args)
    {
        string s = string.Format (format, args);

        return addwstr (s);
    }

    public static int addwstr (string s) { return methods.addwstr (s); }
    public static int attroff (int attrs) { return methods.attroff (attrs); }

    //static public int wechochar (IntPtr win, int ch) => methods.wechochar (win, ch);
    public static int attron (int attrs) { return methods.attron (attrs); }
    public static int attrset (int attrs) { return methods.attrset (attrs); }
    public static int cbreak () { return methods.cbreak (); }

    //
    // Returns true if the window changed since the last invocation, as a
    // side effect, the Lines and Cols properties are updated
    //
    public static bool CheckWinChange ()
    {
        int l, c;

        console_sharp_get_dims (out l, out c);

        if (l < 1)
        {
            l = 1;
        }

        if (l != lines || c != cols)
        {
            lines = l;
            cols = c;

            return true;
        }

        return false;
    }

    public static int clearok (nint win, bool bf) { return methods.clearok (win, bf); }
    public static int COLOR_PAIRS () { return methods.COLOR_PAIRS (); }
    public static int curs_set (int visibility) { return methods.curs_set (visibility); }

    public static string curses_version ()
    {
        nint v = methods.curses_version ();

        return $"{Marshal.PtrToStringAnsi (v)}, {curses_library.LibraryPath}";
    }

    public static int def_prog_mode () { return methods.def_prog_mode (); }
    public static int def_shell_mode () { return methods.def_shell_mode (); }
    public static int doupdate () { return methods.doupdate (); }
    public static int echo () { return methods.echo (); }

    //static public int addch (int ch) => methods.addch (ch);
    public static int echochar (int ch) { return methods.echochar (ch); }

    //
    // The proxy methods to call into each version
    //
    public static int endwin () { return methods.endwin (); }
    public static int flushinp () { return methods.flushinp (); }
    public static int get_wch (out int sequence) { return methods.get_wch (out sequence); }
    public static int getch () { return methods.getch (); }
    public static uint getmouse (out MouseEvent ev) { return methods.getmouse (out ev); }
    public static int halfdelay (int t) { return methods.halfdelay (t); }
    public static bool has_colors () { return methods.has_colors (); }
    public static void idcok (nint win, bool bf) { methods.idcok (win, bf); }
    public static int idlok (nint win, bool bf) { return methods.idlok (win, bf); }
    public static void immedok (nint win, bool bf) { methods.immedok (win, bf); }
    public static int init_pair (short pair, short f, short b) { return methods.init_pair (pair, f, b); }

    /// <summary>
    ///     The init_pair routine changes the definition of a color-pair.It takes three arguments: the number of the
    ///     color-pair to be changed, the  fore- ground color number, and the background color number.For portable ap-
    ///     plications: o The first argument must be a legal color pair  value.If  default colors are used (see
    ///     use_default_colors(3x)) the upper limit is ad- justed to allow for extra pairs which use a default color in  fore-
    ///     ground and/or background. o The second and third arguments must be legal color values. If the  color-pair was
    ///     previously initialized, the screen is refreshed and all occurrences of that color-pair are changed to the new
    ///     defini- tion. As an  extension,  ncurses allows you to set color pair 0 via the as- sume_default_colors (3x)
    ///     routine, or to specify the use of default  col- ors (color number  -1) if you first invoke the use_default_colors
    ///     (3x) routine.
    /// </summary>
    /// <param name="pair"></param>
    /// <param name="foreground"></param>
    /// <param name="background"></param>
    /// <returns></returns>
    public static int InitColorPair (short pair, short foreground, short background) { return methods.init_pair (pair, foreground, background); }

    public static Window initscr ()
    {
        setlocale (LC_ALL, "");
        FindNCurses ();

        // Prevents the terminal from being locked after exiting.
        reset_shell_mode ();

        main_window = new Window (methods.initscr ());

        try
        {
            console_sharp_get_dims (out lines, out cols);
        }
        catch (DllNotFoundException)
        {
            endwin ();

            Console.Error.WriteLine (
                                     "Unable to find the @MONO_CURSES@ native library\n"
                                     + "this is different than the managed mono-curses.dll\n\n"
                                     + "Typically you need to install to a LD_LIBRARY_PATH directory\n"
                                     + "or DYLD_LIBRARY_PATH directory or run /sbin/ldconfig"
                                    );
            Environment.Exit (1);
        }

        //Console.Error.WriteLine ($"using curses {Curses.curses_version ()}");

        return main_window;
    }

    public static int intrflush (nint win, bool bf) { return methods.intrflush (win, bf); }
    public static bool is_term_resized (int lines, int columns) { return methods.is_term_resized (lines, columns); }

    public static int IsAlt (int key)
    {
        if ((key & KeyAlt) != 0)
        {
            return key & ~KeyAlt;
        }

        return 0;
    }

    public static bool isendwin () { return methods.isendwin (); }
    public static int keypad (nint win, bool bf) { return methods.keypad (win, bf); }
    public static int leaveok (nint win, bool bf) { return methods.leaveok (win, bf); }
    public static int meta (nint win, bool bf) { return methods.meta (win, bf); }
    public static int mouseinterval (int interval) { return methods.mouseinterval (interval); }

    public static Event mousemask (Event newmask, out Event oldmask)
    {
        nint e;
        var ret = (Event)methods.mousemask ((nint)newmask, out e);
        oldmask = (Event)e;

        return ret;
    }

    public static int move (int line, int col) { return methods.move (line, col); }

    public static int mvaddch (int y, int x, int ch)
    {
        if (ch < 127 || ch > 0xffff)
        {
            return methods.mvaddch (y, x, ch);
        }

        var c = (char)ch;

        return mvaddwstr (y, x, new string (c, 1));
    }

    public static int mvaddwstr (int y, int x, string s) { return methods.mvaddwstr (y, x, s); }
    public static int mvgetch (int y, int x) { return methods.mvgetch (y, x); }
    public static int nl () { return methods.nl (); }
    public static int nocbreak () { return methods.nocbreak (); }
    public static int noecho () { return methods.noecho (); }
    public static int nonl () { return methods.nonl (); }
    public static void noqiflush () { methods.noqiflush (); }
    public static int noraw () { return methods.noraw (); }
    public static int notimeout (nint win, bool bf) { return methods.notimeout (win, bf); }
    public static void qiflush () { methods.qiflush (); }
    public static int raw () { return methods.raw (); }
    public static int redrawwin (nint win) { return methods.redrawwin (win); }
    public static int refresh () { return methods.refresh (); }
    public static int reset_prog_mode () { return methods.reset_prog_mode (); }
    public static int reset_shell_mode () { return methods.reset_shell_mode (); }
    public static int resetty () { return methods.resetty (); }
    public static int resize_term (int lines, int columns) { return methods.resize_term (lines, columns); }
    public static int resizeterm (int lines, int columns) { return methods.resizeterm (lines, columns); }
    public static int savetty () { return methods.savetty (); }
    public static int scrollok (nint win, bool bf) { return methods.scrollok (win, bf); }
    public static int set_escdelay (int size) { return methods.set_escdelay (size); }

    [DllImport ("libc")]
    public static extern int setlocale (int cate, [MarshalAs (UnmanagedType.LPStr)] string locale);

    public static int setscrreg (int top, int bot) { return methods.setscrreg (top, bot); }
    public static int start_color () { return methods.start_color (); }
    public static int StartColor () { return methods.start_color (); }
    public static int timeout (int delay) { return methods.timeout (delay); }
    public static int typeahead (nint fd) { return methods.typeahead (fd); }
    public static int ungetch (int ch) { return methods.ungetch (ch); }
    public static uint ungetmouse (ref MouseEvent ev) { return methods.ungetmouse (ref ev); }
    public static int use_default_colors () { return methods.use_default_colors (); }
    public static void use_env (bool f) { methods.use_env (f); }

    // TODO: Upgrade to ncurses 6.1 and use the extended version
    //public static int InitExtendedPair (int pair, int foreground, int background) => methods.init_extended_pair (pair, foreground, background);
    public static int UseDefaultColors () { return methods.use_default_colors (); }
    public static int waddch (nint win, int ch) { return methods.waddch (win, ch); }
    public static int wmove (nint win, int line, int col) { return methods.wmove (win, line, col); }

    //static public int wredrawwin (IntPtr win, int beg_line, int num_lines) => methods.wredrawwin (win, beg_line, num_lines);
    public static int wnoutrefresh (nint win) { return methods.wnoutrefresh (win); }
    public static int wrefresh (nint win) { return methods.wrefresh (win); }
    public static int wsetscrreg (nint win, int top, int bot) { return methods.wsetscrreg (win, top, bot); }
    public static int wtimeout (nint win, int delay) { return methods.wtimeout (win, delay); }
    internal static nint console_sharp_get_curscr () { return Marshal.ReadIntPtr (curscr_ptr); }

    internal static void console_sharp_get_dims (out int lines, out int cols)
    {
        lines = Marshal.ReadInt32 (lines_ptr);
        cols = Marshal.ReadInt32 (cols_ptr);

        //int cmd;
        //if (UnmanagedLibrary.IsMacOSPlatform) {
        //	cmd = TIOCGWINSZ_MAC;
        //} else {
        //	cmd = TIOCGWINSZ;
        //}

        //if (ioctl (1, cmd, out winsize ws) == 0) {
        //	lines = ws.ws_row;
        //	cols = ws.ws_col;

        //	if (lines == Lines && cols == Cols) {
        //		return;
        //	}

        //	resizeterm (lines, cols);
        //} else {
        //	lines = Lines;
        //	cols = Cols;
        //}
    }

    internal static nint console_sharp_get_stdscr () { return stdscr; }

    internal static nint read_static_ptr (string key)
    {
        nint ptr = get_ptr (key);

        return Marshal.ReadIntPtr (ptr);
    }

    private static void FindNCurses ()
    {
        LoadMethods ();
        curses_handle = methods.UnmanagedLibrary.NativeLibraryHandle;

        stdscr = read_static_ptr ("stdscr");
        curscr_ptr = get_ptr ("curscr");
        lines_ptr = get_ptr ("LINES");
        cols_ptr = get_ptr ("COLS");
    }

    private static nint get_ptr (string key)
    {
        nint ptr = curses_library.LoadSymbol (key);

        if (ptr == nint.Zero)
        {
            throw new Exception ("Could not load the key " + key);
        }

        return ptr;
    }

    //[DllImport ("libc")]
    //public extern static int ioctl (int fd, int cmd, out winsize argp);

    private static void LoadMethods ()
    {
        string [] libs = OperatingSystem.IsMacOS()
                             ? ["libncurses.dylib"]
                             : ["libncursesw.so.6", "libncursesw.so.5"];
        var attempts = 1;

        while (true)
        {
            try
            {
                curses_library = new UnmanagedLibrary (libs, false);
                methods = new NativeMethods (curses_library);

                break;
            }
            catch (Exception ex)
            {
                if (attempts == 1)
                {
                    attempts++;

                    (int exitCode, string result) =
                        ClipboardProcessRunner.Bash ("cat /etc/os-release", waitForOutput: true);

                    if (exitCode == 0 && result.Contains ("opensuse"))
                    {
                        libs [0] = "libncursesw.so.5";
                    }
                }
                else
                {
                    throw ex.GetBaseException ();
                }
            }
        }
    }

    //[StructLayout (LayoutKind.Sequential)]
    //public struct winsize {
    //	public ushort ws_row;
    //	public ushort ws_col;
    //	public ushort ws_xpixel;   /* unused */
    //	public ushort ws_ypixel;   /* unused */
    //};

    [StructLayout (LayoutKind.Sequential)]
    public struct MouseEvent
    {
        public short ID;
        public int X, Y, Z;
        public Event ButtonState;
    }
}

#pragma warning disable RCS1102 // Make class static.'
internal class Delegates
{
#pragma warning restore RCS1102 // Make class static.
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
    public delegate nint initscr ();

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

    public delegate int typeahead (nint fd);

    public delegate int timeout (int delay);

    public delegate int wtimeout (nint win, int delay);

    public delegate int notimeout (nint win, bool bf);

    public delegate int keypad (nint win, bool bf);

    public delegate int meta (nint win, bool bf);

    public delegate int intrflush (nint win, bool bf);

    public delegate int clearok (nint win, bool bf);

    public delegate int idlok (nint win, bool bf);

    public delegate void idcok (nint win, bool bf);

    public delegate void immedok (nint win, bool bf);

    public delegate int leaveok (nint win, bool bf);

    public delegate int wsetscrreg (nint win, int top, int bot);

    public delegate int scrollok (nint win, bool bf);

    public delegate int nl ();

    public delegate int nonl ();

    public delegate int setscrreg (int top, int bot);

    public delegate int refresh ();

    public delegate int doupdate ();

    public delegate int wrefresh (nint win);

    public delegate int redrawwin (nint win);

    //public delegate int wredrawwin (IntPtr win, int beg_line, int num_lines);
    public delegate int wnoutrefresh (nint win);

    public delegate int move (int line, int col);

    public delegate int curs_set (int visibility);

    public delegate int addch (int ch);

    public delegate int echochar (int ch);

    public delegate int mvaddch (int y, int x, int ch);

    public delegate int addwstr ([MarshalAs (UnmanagedType.LPWStr)] string s);

    public delegate int mvaddwstr (int y, int x, [MarshalAs (UnmanagedType.LPWStr)] string s);

    public delegate int wmove (nint win, int line, int col);

    public delegate int waddch (nint win, int ch);

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

    public delegate int COLOR_PAIRS ();

    public delegate uint getmouse (out Curses.MouseEvent ev);

    public delegate uint ungetmouse (ref Curses.MouseEvent ev);

    public delegate int mouseinterval (int interval);

    public delegate nint mousemask (nint newmask, out nint oldMask);

    public delegate bool is_term_resized (int lines, int columns);

    public delegate int resize_term (int lines, int columns);

    public delegate int resizeterm (int lines, int columns);

    public delegate void use_env (bool f);

    public delegate int flushinp ();

    public delegate int def_prog_mode ();

    public delegate int def_shell_mode ();

    public delegate int reset_prog_mode ();

    public delegate int reset_shell_mode ();

    public delegate int savetty ();

    public delegate int resetty ();

    public delegate int set_escdelay (int size);

    public delegate nint curses_version ();
}

internal class NativeMethods
{
    public readonly Delegates.addch addch;
    public readonly Delegates.addwstr addwstr;
    public readonly Delegates.attroff attroff;

    //public readonly Delegates.wechochar wechochar;
    public readonly Delegates.attron attron;
    public readonly Delegates.attrset attrset;
    public readonly Delegates.cbreak cbreak;
    public readonly Delegates.clearok clearok;
    public readonly Delegates.COLOR_PAIRS COLOR_PAIRS;
    public readonly Delegates.curs_set curs_set;
    public readonly Delegates.curses_version curses_version;
    public readonly Delegates.def_prog_mode def_prog_mode;
    public readonly Delegates.def_shell_mode def_shell_mode;
    public readonly Delegates.doupdate doupdate;
    public readonly Delegates.echo echo;
    public readonly Delegates.echochar echochar;
    public readonly Delegates.endwin endwin;
    public readonly Delegates.flushinp flushinp;
    public readonly Delegates.get_wch get_wch;
    public readonly Delegates.getch getch;
    public readonly Delegates.getmouse getmouse;
    public readonly Delegates.halfdelay halfdelay;
    public readonly Delegates.has_colors has_colors;
    public readonly Delegates.idcok idcok;
    public readonly Delegates.idlok idlok;
    public readonly Delegates.immedok immedok;
    public readonly Delegates.init_pair init_pair;
    public readonly Delegates.initscr initscr;
    public readonly Delegates.intrflush intrflush;
    public readonly Delegates.is_term_resized is_term_resized;
    public readonly Delegates.isendwin isendwin;
    public readonly Delegates.keypad keypad;
    public readonly Delegates.leaveok leaveok;
    public readonly Delegates.meta meta;
    public readonly Delegates.mouseinterval mouseinterval;
    public readonly Delegates.mousemask mousemask;
    public readonly Delegates.move move;
    public readonly Delegates.mvaddch mvaddch;
    public readonly Delegates.mvaddwstr mvaddwstr;
    public readonly Delegates.mvgetch mvgetch;
    public readonly Delegates.nl nl;
    public readonly Delegates.nocbreak nocbreak;
    public readonly Delegates.noecho noecho;
    public readonly Delegates.nonl nonl;
    public readonly Delegates.noqiflush noqiflush;
    public readonly Delegates.noraw noraw;
    public readonly Delegates.notimeout notimeout;
    public readonly Delegates.qiflush qiflush;
    public readonly Delegates.raw raw;
    public readonly Delegates.redrawwin redrawwin;
    public readonly Delegates.refresh refresh;
    public readonly Delegates.reset_prog_mode reset_prog_mode;
    public readonly Delegates.reset_shell_mode reset_shell_mode;
    public readonly Delegates.resetty resetty;
    public readonly Delegates.resize_term resize_term;
    public readonly Delegates.resizeterm resizeterm;
    public readonly Delegates.savetty savetty;
    public readonly Delegates.scrollok scrollok;
    public readonly Delegates.set_escdelay set_escdelay;
    public readonly Delegates.setscrreg setscrreg;
    public readonly Delegates.start_color start_color;
    public readonly Delegates.timeout timeout;
    public readonly Delegates.typeahead typeahead;
    public readonly Delegates.ungetch ungetch;
    public readonly Delegates.ungetmouse ungetmouse;
    public readonly Delegates.use_default_colors use_default_colors;
    public readonly Delegates.use_env use_env;
    public readonly Delegates.waddch waddch;
    public readonly Delegates.wmove wmove;

    //public readonly Delegates.wredrawwin wredrawwin;
    public readonly Delegates.wnoutrefresh wnoutrefresh;
    public readonly Delegates.wrefresh wrefresh;
    public readonly Delegates.wsetscrreg wsetscrreg;
    public readonly Delegates.wtimeout wtimeout;
    public UnmanagedLibrary UnmanagedLibrary;

    public NativeMethods (UnmanagedLibrary lib)
    {
        UnmanagedLibrary = lib;
        initscr = lib.GetNativeMethodDelegate<Delegates.initscr> ("initscr");
        endwin = lib.GetNativeMethodDelegate<Delegates.endwin> ("endwin");
        isendwin = lib.GetNativeMethodDelegate<Delegates.isendwin> ("isendwin");
        cbreak = lib.GetNativeMethodDelegate<Delegates.cbreak> ("cbreak");
        nocbreak = lib.GetNativeMethodDelegate<Delegates.nocbreak> ("nocbreak");
        echo = lib.GetNativeMethodDelegate<Delegates.echo> ("echo");
        noecho = lib.GetNativeMethodDelegate<Delegates.noecho> ("noecho");
        halfdelay = lib.GetNativeMethodDelegate<Delegates.halfdelay> ("halfdelay");
        raw = lib.GetNativeMethodDelegate<Delegates.raw> ("raw");
        noraw = lib.GetNativeMethodDelegate<Delegates.noraw> ("noraw");
        noqiflush = lib.GetNativeMethodDelegate<Delegates.noqiflush> ("noqiflush");
        qiflush = lib.GetNativeMethodDelegate<Delegates.qiflush> ("qiflush");
        typeahead = lib.GetNativeMethodDelegate<Delegates.typeahead> ("typeahead");
        timeout = lib.GetNativeMethodDelegate<Delegates.timeout> ("timeout");
        wtimeout = lib.GetNativeMethodDelegate<Delegates.wtimeout> ("wtimeout");
        notimeout = lib.GetNativeMethodDelegate<Delegates.notimeout> ("notimeout");
        keypad = lib.GetNativeMethodDelegate<Delegates.keypad> ("keypad");
        meta = lib.GetNativeMethodDelegate<Delegates.meta> ("meta");
        intrflush = lib.GetNativeMethodDelegate<Delegates.intrflush> ("intrflush");
        clearok = lib.GetNativeMethodDelegate<Delegates.clearok> ("clearok");
        idlok = lib.GetNativeMethodDelegate<Delegates.idlok> ("idlok");
        idcok = lib.GetNativeMethodDelegate<Delegates.idcok> ("idcok");
        immedok = lib.GetNativeMethodDelegate<Delegates.immedok> ("immedok");
        leaveok = lib.GetNativeMethodDelegate<Delegates.leaveok> ("leaveok");
        wsetscrreg = lib.GetNativeMethodDelegate<Delegates.wsetscrreg> ("wsetscrreg");
        scrollok = lib.GetNativeMethodDelegate<Delegates.scrollok> ("scrollok");
        nl = lib.GetNativeMethodDelegate<Delegates.nl> ("nl");
        nonl = lib.GetNativeMethodDelegate<Delegates.nonl> ("nonl");
        setscrreg = lib.GetNativeMethodDelegate<Delegates.setscrreg> ("setscrreg");
        refresh = lib.GetNativeMethodDelegate<Delegates.refresh> ("refresh");
        doupdate = lib.GetNativeMethodDelegate<Delegates.doupdate> ("doupdate");
        wrefresh = lib.GetNativeMethodDelegate<Delegates.wrefresh> ("wrefresh");
        redrawwin = lib.GetNativeMethodDelegate<Delegates.redrawwin> ("redrawwin");

        //wredrawwin = lib.GetNativeMethodDelegate<Delegates.wredrawwin> ("wredrawwin");
        wnoutrefresh = lib.GetNativeMethodDelegate<Delegates.wnoutrefresh> ("wnoutrefresh");
        move = lib.GetNativeMethodDelegate<Delegates.move> ("move");
        curs_set = lib.GetNativeMethodDelegate<Delegates.curs_set> ("curs_set");
        addch = lib.GetNativeMethodDelegate<Delegates.addch> ("addch");
        echochar = lib.GetNativeMethodDelegate<Delegates.echochar> ("echochar");
        mvaddch = lib.GetNativeMethodDelegate<Delegates.mvaddch> ("mvaddch");
        addwstr = lib.GetNativeMethodDelegate<Delegates.addwstr> ("addwstr");
        mvaddwstr = lib.GetNativeMethodDelegate<Delegates.mvaddwstr> ("mvaddwstr");
        wmove = lib.GetNativeMethodDelegate<Delegates.wmove> ("wmove");
        waddch = lib.GetNativeMethodDelegate<Delegates.waddch> ("waddch");
        attron = lib.GetNativeMethodDelegate<Delegates.attron> ("attron");
        attroff = lib.GetNativeMethodDelegate<Delegates.attroff> ("attroff");
        attrset = lib.GetNativeMethodDelegate<Delegates.attrset> ("attrset");
        getch = lib.GetNativeMethodDelegate<Delegates.getch> ("getch");
        get_wch = lib.GetNativeMethodDelegate<Delegates.get_wch> ("get_wch");
        ungetch = lib.GetNativeMethodDelegate<Delegates.ungetch> ("ungetch");
        mvgetch = lib.GetNativeMethodDelegate<Delegates.mvgetch> ("mvgetch");
        has_colors = lib.GetNativeMethodDelegate<Delegates.has_colors> ("has_colors");
        start_color = lib.GetNativeMethodDelegate<Delegates.start_color> ("start_color");
        init_pair = lib.GetNativeMethodDelegate<Delegates.init_pair> ("init_pair");
        use_default_colors = lib.GetNativeMethodDelegate<Delegates.use_default_colors> ("use_default_colors");
        COLOR_PAIRS = lib.GetNativeMethodDelegate<Delegates.COLOR_PAIRS> ("COLOR_PAIRS");
        getmouse = lib.GetNativeMethodDelegate<Delegates.getmouse> ("getmouse");
        ungetmouse = lib.GetNativeMethodDelegate<Delegates.ungetmouse> ("ungetmouse");
        mouseinterval = lib.GetNativeMethodDelegate<Delegates.mouseinterval> ("mouseinterval");
        mousemask = lib.GetNativeMethodDelegate<Delegates.mousemask> ("mousemask");
        is_term_resized = lib.GetNativeMethodDelegate<Delegates.is_term_resized> ("is_term_resized");
        resize_term = lib.GetNativeMethodDelegate<Delegates.resize_term> ("resize_term");
        resizeterm = lib.GetNativeMethodDelegate<Delegates.resizeterm> ("resizeterm");
        use_env = lib.GetNativeMethodDelegate<Delegates.use_env> ("use_env");
        flushinp = lib.GetNativeMethodDelegate<Delegates.flushinp> ("flushinp");
        def_prog_mode = lib.GetNativeMethodDelegate<Delegates.def_prog_mode> ("def_prog_mode");
        def_shell_mode = lib.GetNativeMethodDelegate<Delegates.def_shell_mode> ("def_shell_mode");
        reset_prog_mode = lib.GetNativeMethodDelegate<Delegates.reset_prog_mode> ("reset_prog_mode");
        reset_shell_mode = lib.GetNativeMethodDelegate<Delegates.reset_shell_mode> ("reset_shell_mode");
        savetty = lib.GetNativeMethodDelegate<Delegates.savetty> ("savetty");
        resetty = lib.GetNativeMethodDelegate<Delegates.resetty> ("resetty");
        set_escdelay = lib.GetNativeMethodDelegate<Delegates.set_escdelay> ("set_escdelay");
        curses_version = lib.GetNativeMethodDelegate<Delegates.curses_version> ("curses_version");
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
