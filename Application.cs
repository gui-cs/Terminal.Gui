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

    public class Responder {
        public virtual Responder Next { get; set; }
        public virtual bool IsFirstResponder => true;
        public virtual bool CanBecomeFirstResponder => true;
        public virtual bool CanResignFirstResponder => true;
        public virtual void BecomeFirstResponder () {}
        public virtual void ResignFirstResponder () {}

        // Key handling
        public virtual void KeyDown (Event.Key kb) {}

        // Mouse events
        public virtual void MouseEvent (Event.Mouse me) {}
    }

    public class View : Responder {
        public static ConsoleDriver Driver = Application.Driver;
        public static IList<View> empty = new List<View>(0).AsReadOnly ();
        List<View> subviews;
        public IList<View> Subviews  => subviews == null ? empty : subviews.AsReadOnly ();

        Rect frame;

        public View (Rect frame)
        {
            this.frame = frame;
        }

        public void AddSubview (View view)
        {
            if (view == null)
                return;
            if (subviews == null)
                subviews = new List<View> ();
            subviews.Add (view);
        }

        
    }

    public class ViewController : Responder {
        View view;
        public View View => view;

        public ViewController (View startup)
        {
            view = startup;
        }
    }

    public class Window : View {
        public ViewController RootViewController;

        public Window (Rect frame) : base (frame)
        {
        }

        public override void BecomeFirstResponder() 
        {
            Application.MakeFirstResponder (this);
        }

        public static Window Toplevel () 
        {
            return new Window (new Rect (0, 0, Driver.Cols, Driver.Rows));
        }
    }

    public class Application {
        public static ConsoleDriver Driver = new CursesDriver ();
        public Window MainWindow { get; private set; }
        public Mono.Terminal.MainLoop MainLoop { get; private set; }

        static Stack<Responder> responders = new Stack<Responder> ();
        static Responder responder;

        public static void MakeFirstResponder (Responder newResponder)
        {
            if (newResponder == null)
                throw new ArgumentNullException ();

            responders.Push (responder);
            responder = newResponder;
        }

        public void Init ()
        {
            if (MainWindow != null)
                return;

            MainLoop = new Mono.Terminal.MainLoop ();
            MainWindow = Window.Toplevel ();  
            responder = MainWindow;

            MainLoop.AddWatch (0, Mono.Terminal.MainLoop.Condition.PollIn, x => {
                //ProcessChar ();

				return true;
			});

        }
    }
}