//
// 
// Pending:
//   - Check for NeedDisplay on the hierarchy and repaint
//   - Layout support
//
// Optimziations
//   - Add rendering limitation to the exposed area
using System;
using System.Collections.Generic;

namespace Terminal {

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
        View container = null;
        public static ConsoleDriver Driver = Application.Driver;
        public static IList<View> empty = new List<View>(0).AsReadOnly ();
        List<View> subviews;
        public IList<View> Subviews  => subviews == null ? empty : subviews.AsReadOnly ();
        internal bool NeedDisplay { get; private set; } = true;

        // The frame for the object
        Rect frame;

        // The offset of the first child view inside the view
        Point offset;

        // The frame for this view
        public Rect Frame { 
            get => frame;
            set {
                frame = value;
                SetNeedsDisplay ();
            }
        }

        public View (Rect frame)
        {
            this.Frame = frame;
        }

        public void SetNeedsDisplay ()
        {
            NeedDisplay = true;
        }

        public void AddSubview (View view)
        {
            if (view == null)
                return;
            if (subviews == null)
                subviews = new List<View> ();
            subviews.Add (view);
        }

        public void GetRealRowCol (int col, int row, out int rcol, out int rrow)
        {
            // Computes the real row, col relative to the screen.
            rrow = row;
            rcol = col;
            var ccontainer = container;
            while (ccontainer != null){
                rrow += container.frame.Y;
                rcol += container.frame.X;
                ccontainer = ccontainer.container;
            }

            // The following ensures that the cursor is always in the screen boundaries.
            rrow = Math.Max (0, Math.Min (rrow, Driver.Rows-1));
            rcol = Math.Max (0, Math.Min (rcol, Driver.Cols-1));
        }

        public void Move (int col, int row)
        {
            GetRealRowCol (col, row, out var rcol, out var rrow);
            Driver.Move (rcol, rrow);
        }

        public void AddCh (int col, int row, int ch)
        {
            if (row < 0 || col < 0)
                return;
            if (row > frame.Height-1 || col > frame.Width-1)
                return;
            Move (col, row);
            Driver.AddCh (ch);
        }

        public virtual void Redraw ()
        {
            var clipRect = new Rect (offset, frame.Size);

            foreach (var view in subviews){
                if (view.NeedDisplay){
                    if (view.Frame.IntersectsWith (clipRect)){
                        view.Redraw ();
                    }
                    view.NeedDisplay = false;
                }
            }
            NeedDisplay = false;
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

    public class Toplevel : View {
        public ViewController RootViewController;
        public bool ShowFrame;
        

        public Toplevel (Rect frame) : base (frame)
        {
        }

        public override void BecomeFirstResponder() 
        {
            Application.MakeFirstResponder (this);
        }

        public static Toplevel Create () 
        {
            return new Window (new Rect (0, 0, Driver.Cols, Driver.Rows));
        }
    }

    public class Window : Toplevel {
        View contentView;
        string title;

        public string Title {
            get => title;
            set {
                title = value;
                SetNeedsDisplay ();
            }
        }

        public Window (Rect frame, string title = null) : base (frame)
        {
            frame.Inflate (-1, -1);
            contentView = new View (frame);
            AddSubview (contentView);
        }

        public override void Redraw ()
        {

            base.Redraw ();
        }
    }

    public class Application {
        public static ConsoleDriver Driver = new CursesDriver ();
        public Toplevel Top { get; private set; }
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
            if (Top != null)
                return;

            MainLoop = new Mono.Terminal.MainLoop ();
            Top = Toplevel.Create ();  
            responder = Top;

            MainLoop.AddWatch (0, Mono.Terminal.MainLoop.Condition.PollIn, x => {
                //ProcessChar ();

				return true;
			});

        }
    }
}