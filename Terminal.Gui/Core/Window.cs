//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System.Collections;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// A <see cref="Toplevel"/> <see cref="View"/> that draws a border around its <see cref="View.Frame"/> with a <see cref="Title"/> at the top.
	/// </summary>
	/// <remarks>
	/// The 'client area' of a <see cref="Window"/> is a rectangle deflated by one or more rows/columns from <see cref="View.Bounds"/>. A this time there is no
	/// API to determine this rectangle.
	/// </remarks>
	public class Window : Toplevel, IEnumerable {
		View contentView;
		ustring title;

		/// <summary>
		/// The title to be displayed for this window.
		/// </summary>
		/// <value>The title</value>
		public ustring Title {
			get => title;
			set {
				title = value;
				SetNeedsDisplay ();
			}
		}


		/// <summary>
		/// ContentView is an internal implementation detail of Window. It is used to host Views added with <see cref="Add(View)"/>. 
		/// Its ONLY reason for being is to provide a simple way for Window to expose to those SubViews that the Window's Bounds 
		/// are actually deflated due to the border. 
		/// </summary>
		class ContentView : View {
			public ContentView (Rect frame) : base (frame) { }
			public ContentView () : base () { }
#if false
			public override void Redraw (Rect bounds)
			{
				Driver.SetAttribute (ColorScheme.Focus);

				for (int y = 0; y < Frame.Height; y++) {
					Move (0, y);
					for (int x = 0; x < Frame.Width; x++) {

						Driver.AddRune ('x');
					}
				}
				base.Redraw (region);
			}
#endif
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.Window"/> class with an optional title using <see cref="LayoutStyle.Absolute"/> positioning.
		/// </summary>
		/// <param name="frame">Superview-relatie rectangle specifying the location and size</param>
		/// <param name="title">Title</param>
		/// <remarks>
		/// This constructor intitalizes a Window with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Absolute"/>. Use constructors
		/// that do not take <c>Rect</c> parameters to initialize a Window with <see cref="LayoutStyle.Computed"/>. 
		/// </remarks>
		public Window (Rect frame, ustring title = null) : this (frame, title, padding: 0)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class with an optional title using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <remarks>
		///   This constructor intitalize a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/>. 
		///   Use <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> properties to dynamically control the size and location of the view.
		/// </remarks>
		public Window (ustring title = null) : this (title, padding: 0)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public Window () : this (title: null) { }

		int padding;
		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> using <see cref="LayoutStyle.Absolute"/> positioning with the specified frame for its location, with the specified frame padding,
		/// and an optional title.
		/// </summary>
		/// <param name="frame">Superview-relatie rectangle specifying the location and size</param>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="title">Title</param>
		/// <remarks>
		/// This constructor intitalizes a Window with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Absolute"/>. Use constructors
		/// that do not take <c>Rect</c> parameters to initialize a Window with  <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/> 
		/// </remarks>
		public Window (Rect frame, ustring title = null, int padding = 0) : base (frame)
		{
			this.Title = title;
			int wb = 2 * (1 + padding);
			this.padding = padding;
			var cFrame = new Rect (1 + padding, 1 + padding, frame.Width - wb, frame.Height - wb);
			contentView = new ContentView (cFrame);
			base.Add (contentView);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> using <see cref="LayoutStyle.Absolute"/> positioning with the specified frame for its location, with the specified frame padding,
		/// and an optional title.
		/// </summary>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="title">Title.</param>
		/// <remarks>
		///   This constructor intitalize a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/>. 
		///   Use <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> properties to dynamically control the size and location of the view.
		/// </remarks>
		public Window (ustring title = null, int padding = 0) : base ()
		{
			this.Title = title;
			int wb = 1 + padding;
			this.padding = padding;
			contentView = new ContentView () {
				X = wb,
				Y = wb,
				Width = Dim.Fill (wb),
				Height = Dim.Fill (wb)
			};
			base.Add (contentView);
		}

		/// <summary>
		/// Enumerates the various <see cref="View"/>s in the embedded <see cref="ContentView"/>.
		/// </summary>
		/// <returns>The enumerator.</returns>
		public new IEnumerator GetEnumerator ()
		{
			return contentView.GetEnumerator ();
		}

		/// <inheritdoc/>
		public override void Add (View view)
		{
			contentView.Add (view);
			if (view.CanFocus)
				CanFocus = true;
		}


		/// <inheritdoc/>
		public override void Remove (View view)
		{
			if (view == null)
				return;

			SetNeedsDisplay ();
			var touched = view.Frame;
			contentView.Remove (view);

			if (contentView.InternalSubviews.Count < 1)
				this.CanFocus = false;
		}

		/// <inheritdoc/>
		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			//var padding = 0;
			Application.CurrentView = this;
			var scrRect = ViewToScreen (new Rect (0, 0, Frame.Width, Frame.Height));

			// BUGBUG: Why do we draw the frame twice? This call is here to clear the content area, I think. Why not just clear that area?
			if (NeedDisplay != null && !NeedDisplay.IsEmpty) {
				Driver.SetAttribute (ColorScheme.Normal);
				Driver.DrawWindowFrame (scrRect, padding + 1, padding + 1, padding + 1, padding + 1, border: true, fill: true);
			}

			var savedClip = ClipToBounds ();

			// Redraw our contenetView
			// TODO: smartly constrict contentView.Bounds to just be what intersects with the 'bounds' we were passed
			contentView.Redraw (contentView.Bounds);
			Driver.Clip = savedClip;

			ClearNeedsDisplay ();
			Driver.SetAttribute (ColorScheme.Normal);
			Driver.DrawWindowFrame (scrRect, padding + 1, padding + 1, padding + 1, padding + 1, border: true, fill: false);

			if (HasFocus)
				Driver.SetAttribute (ColorScheme.HotNormal);
			Driver.DrawWindowTitle (scrRect, Title, padding, padding, padding, padding);
			Driver.SetAttribute (ColorScheme.Normal);
		}

		//
		// FIXED:It does not look like the event is raised on clicked-drag
		// need to figure that out.
		//
		internal static Point? dragPosition;
		Point start;

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent mouseEvent)
		{
			// FIXED:The code is currently disabled, because the
			// Driver.UncookMouse does not seem to have an effect if there is
			// a pending mouse event activated.

			int nx, ny;
			if ((mouseEvent.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) ||
				mouseEvent.Flags == MouseFlags.Button3Pressed)) {
				if (dragPosition.HasValue) {
					if (SuperView == null) {
						Application.Top.SetNeedsDisplay (Frame);
						// Redraw the entire app window using just our Frame. Since we are 
						// Application.Top, and our Frame always == our Bounds (Location is always (0,0))
						// our Frame is actually view-relative (which is what Redraw takes).
						Application.Top.Redraw (Frame);
					} else {
						SuperView.SetNeedsDisplay (Frame);
					}
					EnsureVisibleBounds (this, mouseEvent.X + mouseEvent.OfX - start.X,
						mouseEvent.Y + mouseEvent.OfY, out nx, out ny);

					dragPosition = new Point (nx, ny);
					Frame = new Rect (nx, ny, Frame.Width, Frame.Height);
					X = nx;
					Y = ny;
					//Demo.ml2.Text = $"{dx},{dy}";

					// FIXED: optimize, only SetNeedsDisplay on the before/after regions.
					SetNeedsDisplay ();
					return true;
				} else {
					// Only start grabbing if the user clicks on the title bar.
					if (mouseEvent.Y == 0) {
						start = new Point (mouseEvent.X, mouseEvent.Y);
						dragPosition = new Point ();
						nx = mouseEvent.X - mouseEvent.OfX;
						ny = mouseEvent.Y - mouseEvent.OfY;
						dragPosition = new Point (nx, ny);
						Application.GrabMouse (this);
					}

					//Demo.ml2.Text = $"Starting at {dragPosition}";
					return true;
				}
			}

			if (mouseEvent.Flags == MouseFlags.Button1Released && dragPosition.HasValue) {
				Application.UngrabMouse ();
				Driver.UncookMouse ();
				dragPosition = null;
			}

			//Demo.ml.Text = me.ToString ();
			return false;
		}
	}
}
