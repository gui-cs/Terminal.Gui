using System;
using System.Collections.Generic;
using Mono.Terminal;

namespace Terminal {

    public abstract class ConsoleDriver {
        public virtual int Cols {get;}
        public virtual int Rows {get;}
    }

    public class CursesDriver : ConsoleDriver {
        public override int Cols => Curses.Cols;
        public override int Rows => Curses.Lines;
    }
}