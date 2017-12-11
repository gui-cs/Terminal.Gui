using System;
using System.Collections.Generic;

namespace Terminal {
    public struct Rect {
        public int X, Y, Width, Height;

        public Rect (int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public override string ToString() => $"[{X},{Y}:{Width},{Height}]";
    }

    public class View {
        public static ConsoleDriver Driver = Application.Driver;
        View [] subviews;
        public View [] Subviews => subviews == null ? Array.Empty<View> () : subviews;

        Rect frame;

        public View (Rect frame)
        {
            this.frame = frame;
        }
    }

    public class Window : View {
        public Window (Rect frame) : base (frame)
        {
        }

        public static Window Toplevel () 
        {
            return new Window (new Rect (0, 0, Driver.Cols, Driver.Rows));
        }
    }

    public class Application {
        public static ConsoleDriver Driver = new CursesDriver ();

        public void Init ()
        {

        }
    }
}