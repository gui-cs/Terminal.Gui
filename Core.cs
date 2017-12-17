//
// 
// Pending:
//   - Check for NeedDisplay on the hierarchy and repaint
//   - Layout support
//
// Optimziations
//   - Add rendering limitation to the exposed area
using System;
using System.Collections;
using System.Collections.Generic;

namespace Terminal {

    public class Responder {
        public virtual bool CanFocus { get; set; }
        public bool HasFocus { get; internal set; }

        // Key handling
        public virtual void KeyDown (Event.Key kb) { }

        // Mouse events
        public virtual void MouseEvent (Event.Mouse me) { }
    }

    public class View : Responder, IEnumerable {
        View container = null;
        View focused = null;
        public static ConsoleDriver Driver = Application.Driver;
        public static IList<View> empty = new List<View> (0).AsReadOnly ();
        List<View> subviews;
        public IList<View> Subviews => subviews == null ? empty : subviews.AsReadOnly ();
        internal bool NeedDisplay { get; private set; } = true;

        // The frame for the object
        Rect frame;

        // The frame for this view
        public Rect Frame {
            get => frame;
            set {
                frame = value;
                SetNeedsDisplay ();
            }
        }

        public IEnumerator GetEnumerator ()
        {
            foreach (var v in subviews)
                yield return v;
        }

        public Rect Bounds {
            get => new Rect (Point.Empty, Frame.Size);
            set {
                Frame = new Rect (frame.Location, value.Size);
            }
        }

        public View (Rect frame)
        {
            this.Frame = frame;
            CanFocus = false;
        }

        /// <summary>
        /// Invoke to flag that this view needs to be redisplayed, by any code
        /// that alters the state of the view.
        /// </summary>
        public void SetNeedsDisplay ()
        {
            NeedDisplay = true;
            if (container != null)
                container.SetNeedsDisplay ();
        }

        /// <summary>
        ///   Adds a subview to this view.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public virtual void Add (View view)
        {
            if (view == null)
                return;
            if (subviews == null)
                subviews = new List<View> ();
            subviews.Add (view);
            view.container = this;
            if (view.CanFocus)
                CanFocus = true;
        }

        /// <summary>
        ///   Removes all the widgets from this container.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public virtual void RemoveAll ()
        {
            if (subviews == null)
                return;

            while (subviews.Count > 0) {
                var view = subviews [0];
                Remove (view);
                subviews.RemoveAt (0);
            }
        }

        /// <summary>
        ///   Removes a widget from this container.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public virtual void Remove (View view)
        {
            if (view == null)
                return;

            subviews.Remove (view);
            view.container = null;

            if (subviews.Count < 1)
                this.CanFocus = false;
        }

        /// <summary>
        ///   Clears the view region with the current color.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This clears the entire region used by this view.
        ///   </para>
        /// </remarks>
        public void Clear ()
        {
            var h = Frame.Height;
            var w = Frame.Width;
            for (int line = 0; line < h; line++) {
                Move (0, line);
                for (int col = 0; col < w; col++)
                    Driver.AddCh (' ');
            }
        }

        /// <summary>
        /// Converts the (col,row) position from the view into a screen (col,row).  The values are clamped to (0..ScreenDim-1)
        /// </summary>
        /// <param name="col">View-based column.</param>
        /// <param name="row">View-based row.</param>
        /// <param name="rcol">Absolute column, display relative.</param>
        /// <param name="rrow">Absolute row, display relative.</param>
        internal void ViewToScreen (int col, int row, out int rcol, out int rrow, bool clipped = true)
        {
            // Computes the real row, col relative to the screen.
            rrow = row + frame.Y;
            rcol = col + frame.X;
            var ccontainer = container;
            while (ccontainer != null) {
                rrow += ccontainer.frame.Y;
                rcol += ccontainer.frame.X;
                ccontainer = ccontainer.container;
            }

            // The following ensures that the cursor is always in the screen boundaries.
            if (clipped) {
                rrow = Math.Max (0, Math.Min (rrow, Driver.Rows - 1));
                rcol = Math.Max (0, Math.Min (rcol, Driver.Cols - 1));
            }
        }

        Rect RectToScreen (Rect rect)
        {
            ViewToScreen (rect.X, rect.Y, out var x, out var y, clipped: false);
            return new Rect (x, y, rect.Width, rect.Height);
        }

        Rect ScreenClip (Rect rect)
        {
            var x = rect.X < 0 ? 0 : rect.X;
            var y = rect.Y < 0 ? 0 : rect.Y;
            var w = rect.X + rect.Width >= Driver.Cols ? Driver.Cols - rect.X : rect.Width;
            var h = rect.Y + rect.Height >= Driver.Rows ? Driver.Rows - rect.Y : rect.Height;

            return new Rect (x, y, w, h);
        }

        /// <summary>
        /// Draws a frame in the current view, clipped by the boundary of this view
        /// </summary>
        /// <param name="rect">Rectangular region for the frame to be drawn.</param>
        /// <param name="fill">If set to <c>true</c> it fill will the contents.</param>
        public void DrawFrame (Rect rect, bool fill = false)
        {
            var scrRect = RectToScreen (rect);
            var savedClip = Driver.Clip;
            Driver.Clip = ScreenClip (RectToScreen (Bounds));
            Driver.DrawFrame (scrRect, fill);
            Driver.Clip = savedClip;
        }

        /// <summary>
        /// This moves the cursor to the specified column and row in the view.
        /// </summary>
        /// <returns>The move.</returns>
        /// <param name="col">Col.</param>
        /// <param name="row">Row.</param>
        public void Move (int col, int row)
        {
            ViewToScreen (col, row, out var rcol, out var rrow);
            Driver.Move (rcol, rrow);
        }

        /// <summary>
        ///   Positions the cursor in the right position based on the currently focused view in the chain.
        /// </summary>
        public virtual void PositionCursor ()
        {
            if (focused != null)
                focused.PositionCursor ();
            else
                Move (frame.X, frame.Y);
        }

        /// <summary>
        /// Displays the specified character in the specified column and row.
        /// </summary>
        /// <param name="col">Col.</param>
        /// <param name="row">Row.</param>
        /// <param name="ch">Ch.</param>
        public void AddCh (int col, int row, int ch)
        {
            if (row < 0 || col < 0)
                return;
            if (row > frame.Height - 1 || col > frame.Width - 1)
                return;
            Move (col, row);
            Driver.AddCh (ch);
        }

        /// <summary>
        /// Performs a redraw of this view and its subviews, only redraws the views that have been flagged for a re-display.
        /// </summary>
        public virtual void Redraw (Rect region)
        {
            var clipRect = new Rect (Point.Empty, frame.Size);

            if (subviews != null) {
                foreach (var view in subviews) {
                    if (view.NeedDisplay) {
                        if (view.Frame.IntersectsWith (clipRect) && view.Frame.IntersectsWith (region)) {

                            // TODO: optimize this by computing the intersection of region and view.Bounds
                            view.Redraw (view.Bounds);
                        }
                        view.NeedDisplay = false;
                    }
                }
            }
            NeedDisplay = false;
        }

        /// <summary>
        /// Focuses the specified sub-view.
        /// </summary>
        /// <param name="view">View.</param>
        public void SetFocus (View view)
        {
            if (view == null)
                return;
            if (!view.CanFocus)
                return;
            if (focused == view)
                return;

            // Make sure that this view is a subview
            View c;
            for (c = view.container; c != null; c = c.container)
                if (c == this)
                    break;
            if (c == null)
                throw new ArgumentException ("the specified view is not part of the hierarchy of this view");

            if (focused != null)
                focused.HasFocus = false;
            focused = view;
            view.HasFocus = true;
            if (view != null)
                view.EnsureFocus ();
            focused.PositionCursor ();
        }

        /// <summary>
        /// Finds the first view in the hierarchy that wants to get the focus if nothing is currently focused, otherwise, it does nothing.
        /// </summary>
        public void EnsureFocus ()
        {
            if (focused == null)
                FocusFirst ();
        }

        /// <summary>
        /// Focuses the first focusable subview if one exists.
        /// </summary>
        public void FocusFirst ()
        {
            foreach (var view in subviews) {
                if (view.CanFocus) {
                    SetFocus (view);
                    return;
                }
            }
        }

        /// <summary>
        /// Focuses the last focusable subview if one exists.
        /// </summary>
        public void FocusLast ()
        {
            for (int i = subviews.Count; i > 0;) {
                i--;

                View v = subviews [i];
                if (v.CanFocus) {
                    SetFocus (v);
                    return;
                }
            }
        }

        /// <summary>
        /// Focuses the previous view.
        /// </summary>
        /// <returns><c>true</c>, if previous was focused, <c>false</c> otherwise.</returns>
        public bool FocusPrev ()
        {
            if (focused == null) {
                FocusLast ();
                return true;
            }
            int focused_idx = -1;
            for (int i = subviews.Count; i > 0;) {
                i--;
                View w = subviews [i];

                if (w.HasFocus) {
                    if (w.FocusPrev ())
                        return true;
                    focused_idx = i;
                    continue;
                }
                if (w.CanFocus && focused_idx != -1) {
                    focused.HasFocus = false;

                    if (w.CanFocus)
                        w.FocusLast ();

                    SetFocus (w);
                    return true;
                }
            }
            if (focused != null) {
                focused.HasFocus = false;
                focused = null;
            }
            return false;
        }

        /// <summary>
        /// Focuses the next view.
        /// </summary>
        /// <returns><c>true</c>, if next was focused, <c>false</c> otherwise.</returns>
        public bool FocusNext ()
        {
            if (focused == null) {
                FocusFirst ();
                return focused != null;
            }
            int n = subviews.Count;
            int focused_idx = -1;
            for (int i = 0; i < n; i++) {
                View w = subviews [i];

                if (w.HasFocus) {
                    if (w.FocusNext ())
                        return true;
                    focused_idx = i;
                    continue;
                }
                if (w.CanFocus && focused_idx != -1) {
                    focused.HasFocus = false;

                    if (w != null && w.CanFocus)
                        w.FocusFirst ();

                    SetFocus (w);
                    return true;
                }
            }
            if (focused != null) {
                focused.HasFocus = false;
                focused = null;
            }
            return false;
        }

        public virtual void LayoutSubviews ()
        {
        }
    }

    /// <summary>
    /// Toplevel views can be modally executed.
    /// </summary>
    public class Toplevel : View {
        public bool Running;

        public Toplevel (Rect frame) : base (frame)
        {
        }

        public static Toplevel Create ()
        {
            return new Toplevel (new Rect (0, 0, Driver.Cols, Driver.Rows));
        }

#if false
        public override void Redraw ()
        {
            base.Redraw ();
            for (int i = 0; i < Driver.Cols; i++) {
                Driver.Move (0, i);
                Driver.AddStr ("Line: " + i);
            }
        }
#endif  
    }

    /// <summary>
    /// A toplevel view that draws a frame around its region
    /// </summary>
    public class Window : Toplevel, IEnumerable {
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
            this.Title = title;
            frame.Inflate (-1, -1);
            contentView = new View (frame);
            base.Add(contentView);
        }

        public IEnumerator GetEnumerator ()
        {
            return contentView.GetEnumerator ();
        }

        void DrawFrame ()
        {
            DrawFrame (new Rect(0, 0, Frame.Width, Frame.Height), true);
        }

        public override void Add (View view)
        {
            contentView.Add (view);
        }

        public override void Redraw (Rect bounds)
        {
            Driver.SetColor (Colors.Base.Normal);
            DrawFrame ();
            if (HasFocus)
                Driver.SetColor (Colors.Dialog.Normal);
            var width = Frame.Width;
            if (Title != null && width > 4) {
                Move (1, 0);
                Driver.AddCh (' ');
                var str = Title.Length > width ? Title.Substring (0, width - 4) : Title;
                Driver.AddStr (str);
                Driver.AddCh (' ');
            }
            Driver.SetColor (Colors.Dialog.Normal);
            contentView.Redraw (contentView.Bounds);
        }
    }

    public class Application {
        public static ConsoleDriver Driver = new CursesDriver ();
        public static Toplevel Top { get; private set; }
        public static Mono.Terminal.MainLoop MainLoop { get; private set; }

        static Stack<View> toplevels = new Stack<View> ();
        static Responder focus;

        /// <summary>
        ///   This event is raised on each iteration of the
        ///   main loop. 
        /// </summary>
        /// <remarks>
        ///   See also <see cref="Timeout"/>
        /// </remarks>
        static public event EventHandler Iteration;

        public static void MakeFirstResponder (Responder newResponder)
        {
            if (newResponder == null)
                throw new ArgumentNullException ();

            throw new NotImplementedException ();
        }

        /// <summary>
        /// Initializes the Application
        /// </summary>
        public static void Init ()
        {
            if (Top != null)
                return;

            Driver.Init ();
            MainLoop = new Mono.Terminal.MainLoop ();
            Top = Toplevel.Create ();  
            focus = Top;

            MainLoop.AddWatch (0, Mono.Terminal.MainLoop.Condition.PollIn, x => {
                //ProcessChar ();

				return true;
			});
        }

        public class RunState : IDisposable {
            internal RunState (Toplevel view)
            {
                Toplevel = view;
            }
            internal Toplevel Toplevel;

            public void Dispose ()
            {
                Dispose (true);
                GC.SuppressFinalize(this);
            }

            public virtual void Dispose (bool disposing)
            {
                if (Toplevel != null){
                    Application.End (Toplevel);
                    Toplevel = null;
                }
            }
        }

        static public RunState Begin (Toplevel toplevel)
        {
            if (toplevel == null)
                throw new ArgumentNullException (nameof(toplevel));
            var rs = new RunState (toplevel);

            Init ();
            Driver.PrepareToRun ();

            toplevels.Push (toplevel);

            toplevel.LayoutSubviews ();
            toplevel.FocusFirst ();
            Redraw (toplevel);
            toplevel.PositionCursor ();
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
            view.Redraw (view.Bounds);
            Driver.Refresh ();
        }

        static void Refresh (View view)
        {
            view.Redraw (view.Bounds);
            Driver.Refresh ();
        }

        public static void Refresh ()
        {
            Driver.RedrawTop ();
            View last = null;
            foreach (var v in toplevels){
                v.Redraw (v.Bounds);
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

        /// <summary>
        ///   Runs the main loop for the created dialog
        /// </summary>
        /// <remarks>
        ///   Use the wait parameter to control whether this is a
        ///   blocking or non-blocking call.
        /// </remarks>
        public static void RunLoop(RunState state, bool wait = true)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.Toplevel == null)
                throw new ObjectDisposedException("state");

            for (state.Toplevel.Running = true; state.Toplevel.Running;) {
                if (MainLoop.EventsPending(wait)){
                    MainLoop.MainIteration();
                    if (Iteration != null)
                        Iteration(null, EventArgs.Empty);
                } else if (wait == false)
                    return;
                if (state.Toplevel.NeedDisplay)
                    state.Toplevel.Redraw (state.Toplevel.Bounds);
            }
        }

        public static void Run ()
        {
            Run (Top);
        }

        /// <summary>
        ///   Runs the main loop on the given container.
        /// </summary>
        /// <remarks>
        ///   This method is used to start processing events
        ///   for the main application, but it is also used to
        ///   run modal dialog boxes.
        /// </remarks>
        public static void Run (Toplevel view)
        {
            var runToken = Begin (view);
            RunLoop (runToken);
            End (runToken);
        }
    }
}