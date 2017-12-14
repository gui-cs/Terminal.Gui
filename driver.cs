using System;
using System.Collections.Generic;
using Unix.Terminal;

namespace Terminal {
    
    public class ColorScheme {
        public int Normal;
        public int Focus;
        public int HotNormal;
        public int HotFocus;
        public int Marked => HotNormal;
        public int MarkedSelected => HotFocus;

    }

    public abstract class ConsoleDriver {
        public abstract int Cols {get;}
        public abstract int Rows {get;}
        public abstract void Init ();
        public abstract void Move (int line, int col);
        public abstract void AddCh (int ch);
        public abstract void PrepareToRun ();
        public abstract void Refresh ();
        public abstract void End ();
        public abstract void RedrawTop ();
        
        // Colors used for widgets
        public static ColorScheme ColorBase, ColorDialog, ColorMenu, ColorError;
    }

    public class CursesDriver : ConsoleDriver {
        public override int Cols => Curses.Cols;
        public override int Rows => Curses.Lines;

        public override void Move(int col, int row) => Curses.move (row, col);
        public override void AddCh(int ch) => Curses.addch (ch);
        public override void Refresh() => Curses.refresh ();
        public override void End() => Curses.endwin ();
        public override void RedrawTop() => window.redrawwin ();
        public Curses.Window window;

        static short last_color_pair;
        static int MakeColor (short f, short b)
        {
            Curses.InitColorPair (++last_color_pair, f, b);
            return Curses.ColorPair (last_color_pair);
        }

        public override void PrepareToRun()
        {
            Curses.timeout (-1);
        }

        public override void Init()
        {
            if (window != null)
                return;

            try {
                window = Curses.initscr ();
            } catch (Exception e){
                Console.WriteLine ("Curses failed to initialize, the exception is: " + e);
            }
            Curses.raw ();
            Curses.noecho ();
            Curses.Window.Standard.keypad (true);
        
            ColorBase = new ColorScheme ();
            ColorDialog = new ColorScheme ();
            ColorMenu = new ColorScheme ();
            ColorError = new ColorScheme ();

            if (Curses.HasColors){
                Curses.StartColor ();
                Curses.UseDefaultColors ();

                ColorBase.Normal = MakeColor (Curses.COLOR_WHITE, Curses.COLOR_BLUE);
                ColorBase.Focus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
                ColorBase.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLUE);
                ColorBase.HotFocus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);

                ColorMenu.Normal = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_CYAN);
                ColorMenu.Focus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);
                ColorMenu.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_BLACK);
                ColorMenu.HotFocus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLACK);
                ColorDialog.Normal    = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
                ColorDialog.Focus     = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
                ColorDialog.HotNormal = MakeColor (Curses.COLOR_BLUE,  Curses.COLOR_WHITE);
                ColorDialog.HotFocus  = MakeColor (Curses.COLOR_BLUE,  Curses.COLOR_CYAN);

                ColorError.Normal = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_RED);
                ColorError.Focus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
                ColorError.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_RED);
                ColorError.HotFocus = ColorError.HotNormal;
            } else {
                ColorBase.Normal = Curses.A_NORMAL;
                ColorBase.Focus = Curses.A_REVERSE;
                ColorBase.HotNormal = Curses.A_BOLD;
                ColorBase.HotFocus = Curses.A_BOLD | Curses.A_REVERSE;

                ColorMenu.Normal = Curses.A_REVERSE;
                ColorMenu.Focus = Curses.A_NORMAL;
                ColorMenu.HotNormal = Curses.A_BOLD;
                ColorMenu.HotFocus = Curses.A_NORMAL;

                ColorDialog.Normal    = Curses.A_REVERSE;
                ColorDialog.Focus     = Curses.A_NORMAL;
                ColorDialog.HotNormal = Curses.A_BOLD;
                ColorDialog.HotFocus  = Curses.A_NORMAL;

                ColorError.Normal = Curses.A_BOLD;
                ColorError.Focus = Curses.A_BOLD | Curses.A_REVERSE;
                ColorError.HotNormal = Curses.A_BOLD | Curses.A_REVERSE;
                ColorError.HotFocus = Curses.A_REVERSE;
            }
        }
    }
}