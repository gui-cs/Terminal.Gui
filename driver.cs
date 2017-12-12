using System;
using System.Collections.Generic;
using C=Unix.Terminal;

namespace Terminal {

    public abstract class ConsoleDriver {
        public abstract int Cols {get;}
        public abstract int Rows {get;}
        public abstract void Init ();
    }

    public class CursesDriver : ConsoleDriver {
        public override int Cols => C.Curses.Cols;
        public override int Rows => C.Curses.Lines;

        public C.Curses.Window window;

        public override void Init()
        {
            if (window != null)
                return;

            try {
                window = C.Curses.initscr ();
            } catch (Exception e){
                Console.WriteLine ("Curses failed to initialize, the exception is: " + e);
            }
            C.Curses.raw ();
            C.Curses.noecho ();
            C.Curses.Window.Standard.keypad (true);
        
            if (C.Curses.HasColors){
                C.Curses.StartColor ();
                C.Curses.UseDefaultColors ();
            }
        }
    }
}