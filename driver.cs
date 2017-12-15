using System;
using System.Collections.Generic;
using Unix.Terminal;

namespace Terminal {
    
    public struct Color {
        internal int value;
        public Color (int v)
        {
            value = v;
        }

        public static implicit operator int (Color c) => c.value;
        public static implicit operator Color (int v) => new Color (v);
    }

    public class ColorScheme {
        public Color Normal;
        public Color Focus;
        public Color HotNormal;
        public Color HotFocus;
        public Color Marked => HotNormal;
        public Color MarkedSelected => HotFocus;
    }

    public static class Colors {
        public static ColorScheme Base, Dialog, Menu, Error;
    }

    public abstract class ConsoleDriver {
        public abstract int Cols {get;}
        public abstract int Rows {get;}
        public abstract void Init ();
        public abstract void Move (int col, int row);
        public abstract void AddCh (int ch);
        public abstract void AddStr (string str);
        public abstract void PrepareToRun ();
        public abstract void Refresh ();
        public abstract void End ();
        public abstract void RedrawTop ();
        public abstract void SetColor (Color c);
        public abstract void DrawFrame (Rect region, bool fill);

        // Colors used for widgets
    }

    public class CursesDriver : ConsoleDriver {
        public override int Cols => Curses.Cols;
        public override int Rows => Curses.Lines;

        public override void Move(int col, int row) => Curses.move (row, col);
        public override void AddCh(int ch) => Curses.addch (ch);
        public override void AddStr (string str) => Curses.addstr (str);
        public override void Refresh() => Curses.refresh ();
        public override void End() => Curses.endwin ();
        public override void RedrawTop() => window.redrawwin ();
        public override void SetColor (Color c) => Curses.attrset (c.value);
        public Curses.Window window;

        static short last_color_pair;
        static Color MakeColor (short f, short b)
        {
            Curses.InitColorPair (++last_color_pair, f, b);
            return new Color () { value = Curses.ColorPair (last_color_pair) };
        }

        public override void PrepareToRun()
        {
            Curses.timeout (-1);
        }

        public override void DrawFrame (Rect region, bool fill)
        {
            int width = region.Width;
            int height = region.Height;
            int b;

            Curses.move (region.Y, region.X);
            Curses.addch (Curses.ACS_ULCORNER);
            for (b = 0; b < width - 2; b++)
                Curses.addch (Curses.ACS_HLINE);
            Curses.addch (Curses.ACS_URCORNER);

            for (b = 1; b < height - 1; b++) {
                Curses.move (region.Y + b, region.X);
                Curses.addch (Curses.ACS_VLINE);
                if (fill) {
                    for (int x = 1; x < width - 1; x++)
                        Curses.addch (' ');
                } else
                    Curses.move (region.Y + b, region.X + width - 1);
                Curses.addch (Curses.ACS_VLINE);
            }
            Curses.move (region.Y + height - 1, region.X);
            Curses.addch (Curses.ACS_LLCORNER);
            for (b = 0; b < width - 2; b++)
                Curses.addch (Curses.ACS_HLINE);
            Curses.addch (Curses.ACS_LRCORNER);
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
        
            Colors.Base = new ColorScheme ();
            Colors.Dialog = new ColorScheme ();
            Colors.Menu = new ColorScheme ();
            Colors.Error = new ColorScheme ();

            if (Curses.HasColors){
                Curses.StartColor ();
                Curses.UseDefaultColors ();

                Colors.Base.Normal = MakeColor (Curses.COLOR_WHITE, Curses.COLOR_BLUE);
                Colors.Base.Focus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
                Colors.Base.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLUE);
                Colors.Base.HotFocus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);
                Colors.Menu.Normal = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_CYAN);
                Colors.Menu.Focus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);
                Colors.Menu.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_BLACK);
                Colors.Menu.HotFocus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLACK);
                Colors.Dialog.Normal    = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
                Colors.Dialog.Focus     = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
                Colors.Dialog.HotNormal = MakeColor (Curses.COLOR_BLUE,  Curses.COLOR_WHITE);
                Colors.Dialog.HotFocus  = MakeColor (Curses.COLOR_BLUE,  Curses.COLOR_CYAN);

                Colors.Error.Normal = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_RED);
                Colors.Error.Focus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
                Colors.Error.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_RED);
                Colors.Error.HotFocus = Colors.Error.HotNormal;
            } else {
                Colors.Base.Normal = Curses.A_NORMAL;
                Colors.Base.Focus = Curses.A_REVERSE;
                Colors.Base.HotNormal = Curses.A_BOLD;
                Colors.Base.HotFocus = Curses.A_BOLD | Curses.A_REVERSE;
                Colors.Menu.Normal = Curses.A_REVERSE;
                Colors.Menu.Focus = Curses.A_NORMAL;
                Colors.Menu.HotNormal = Curses.A_BOLD;
                Colors.Menu.HotFocus = Curses.A_NORMAL;
                Colors.Dialog.Normal    = Curses.A_REVERSE;
                Colors.Dialog.Focus     = Curses.A_NORMAL;
                Colors.Dialog.HotNormal = Curses.A_BOLD;
                Colors.Dialog.HotFocus  = Curses.A_NORMAL;
                Colors.Error.Normal = Curses.A_BOLD;
                Colors.Error.Focus = Curses.A_BOLD | Curses.A_REVERSE;
                Colors.Error.HotNormal = Curses.A_BOLD | Curses.A_REVERSE;
                Colors.Error.HotFocus = Curses.A_REVERSE;
            }
        }
    }
}