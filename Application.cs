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
        public virtual bool CanFocus => true;
        public bool HasFocus { get; internal set; }

        // Key handling
        public virtual void KeyDown (Event.Key kb) {}

        // Mouse events
        public virtual void MouseEvent (Event.Mouse me) {}
    }

    public class View : Responder {
        View container = null;
        View focused = null;
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

        public virtual void PositionCursor ()
        {
            if (focused != null)
                focused.PositionCursor ();
            else
                Move (frame.X, frame.Y);
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
        
        public void SetFocus (View view)
        {
            if (view == null)
                return;
            if (!view.CanFocus)
                return;
            if (focused == view)
                return;
            if (focused != null)
                focused.HasFocus = false;
            focused = view;
            view.HasFocus = true;
            if (view != null)
                view.EnsureFocus ();
            focused.PositionCursor ();
        }

        public void EnsureFocus ()
        {
            if (focused == null)
                FocusFirst ();
        }

        public void FocusFirst ()
        {
            foreach (var view in subviews){
                if (view.CanFocus){
                    SetFocus (view);
                    return;
                }
            }
        }

        public void FocusLast ()
        {
            for (int i = subviews.Count; i > 0; ){
                i--;

                View v = subviews [i];
                if (v.CanFocus){
                    SetFocus (v);
                    return;
                }
            }
        }

        public bool FocusPrev ()
        {
            if (focused == null){
                FocusLast ();
                return true;
            }
            int focused_idx = -1;
            for (int i = subviews.Count; i > 0; ){
                i--;
                View w = subviews [i];

                if (w.HasFocus){
                    if (w.FocusPrev ())
                            return true;
                    focused_idx = i;
                    continue;
                }
                if (w.CanFocus && focused_idx != -1){
                    focused.HasFocus = false;

                    if (w.CanFocus)
                        w.FocusLast (); 
                
                    SetFocus (w);
                    return true;
                }
            }
            if (focused != null){
                focused.HasFocus = false;
                focused = null;
            }
            return false;
        }
        public bool FocusNext ()
        {       
            if (focused == null){
                FocusFirst (); 
                return focused != null;
            }
            int n = subviews.Count;
            int focused_idx = -1;
            for (int i = 0; i < n; i++){
                    View w = subviews [i];
                    
                    if (w.HasFocus){
                            if (w.FocusNext ())
                                return true;
                            focused_idx = i;
                            continue;
                    }
                    if (w.CanFocus && focused_idx != -1){
                        focused.HasFocus = false;

                        if (w != null && w.CanFocus)
                            w.FocusFirst ();
                        
                        SetFocus (w);
                        return true;
                    }
            }
            if (focused != null){
                    focused.HasFocus = false;
                    focused = null;
            }
            return false;
        }

        public virtual void LayoutSubviews ()
        {
        }
    }

    public class Toplevel : View {

        public Toplevel (Rect frame) : base (frame)
        {
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
        public static Toplevel Top { get; private set; }
        public static Mono.Terminal.MainLoop MainLoop { get; private set; }

        static Stack<View> toplevels = new Stack<View> ();
        static Responder focus;

        public static void MakeFirstResponder (Responder newResponder)
        {
            if (newResponder == null)
                throw new ArgumentNullException ();

            throw new NotImplementedException ();
        }

        public static void Init ()
        {
            if (Top != null)
                return;

            MainLoop = new Mono.Terminal.MainLoop ();
            Top = Toplevel.Create ();  
            focus = Top;

            MainLoop.AddWatch (0, Mono.Terminal.MainLoop.Condition.PollIn, x => {
                //ProcessChar ();

				return true;
			});
        }

        public class RunState : IDisposable {
            internal RunState (View view)
            {
                View = view;
            }
            internal View View;

            public void Dispose ()
            {
                Dispose (true);
                GC.SuppressFinalize(this);
            }

            public virtual void Dispose (bool disposing)
            {
                if (View != null){
                    Application.End (View);
                    View = null;
                }
            }
        }

        public void Run ()
        {
            Run (Top);
        }

        static public RunState Begin (View view)
        {
            if (view == null)
                    throw new ArgumentNullException ("view");
            var rs = new RunState (view);

            Init ();
            Driver.PrepareToRun ();

            toplevels.Push (view);

            view.LayoutSubviews ();
            view.FocusFirst ();
            Redraw (view);
            view.PositionCursor ();
            Driver.Refresh ();
            

            return rs;
        }

        static public void End (RunState rs)
        {
            if (rs == null)
                throw new ArgumentNullException (nameof (rs));
            rs.Dispose ();
        }

        static void Shutdown ()
        {
            Driver.End ();
        }

        static void Redraw (View view)
        {
            view.Redraw ();
            Driver.Refresh ();
        }

        static void Refresh (View view)
        {
            view.Redraw ();
            Driver.Refresh ();
        }

        public static void Refresh ()
        {
            Driver.RedrawTop ();
            View last = null;
            foreach (var v in toplevels){
                v.Redraw ();
                last = v;
            }
            if (last != null)
                last.PositionCursor ();
            Driver.Refresh ();
        }

        internal static void End (View view)
        {
            if (toplevels.Peek () != view)
                throw new ArgumentException ("The view that you end with must be balanced");
            toplevels.Pop ();
            if (toplevels.Count == 0)
                Shutdown ();
            else
                Refresh ();
        }

        public void Run (View view)
        {
            
        }
    }
}