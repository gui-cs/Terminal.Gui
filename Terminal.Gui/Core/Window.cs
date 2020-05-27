//
// Core.cs: The core engine for gui.cs
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// Pending:
//   - Check for NeedDisplay on the hierarchy and repaint
//   - Layout support
//   - "Colors" type or "Attributes" type?
//   - What to surface as "BackgroundCOlor" when clearing a window, an attribute or colors?
//
// Optimziations
//   - Add rendering limitation to the exposed area
using System.Collections;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// A <see cref="Toplevel"/> <see cref="View"/> that draws a frame around its region and has a "ContentView" subview where the contents are added.
	/// </summary>
	public class Window : Toplevel, IEnumerable {
		View contentView;
		ustring title;

		/// <summary>
		/// The title to be displayed for this window.
		/// </summary>
		/// <value>The title.</value>
		public ustring Title {
			get => title;
			set {
				title = value;
				SetNeedsDisplay ();
			}
		}

		class ContentView : View {
			public ContentView (Rect frame) : base (frame) { }
			public ContentView () : base () { }
#if false
			public override void Redraw (Rect region)
			{
				Driver.SetAttribute (ColorScheme.Focus);

				for (int y = 0; y < Frame.Height; y++) {
					Move (0, y);
					for (int x = 0; x < Frame.Width; x++) {

						Driver.AddRune ('x');
					}
				}
			}
#endif
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.Window"/> class with an optional title and a set frame.
		/// </summary>
		/// <param name="frame">Frame.</param>
		/// <param name="title">Title.</param>
		public Window (Rect frame, ustring title = null) : this (frame, title, padding: 0)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class with an optional title.
		/// </summary>
		/// <param name="title">Title.</param>
		public Window (ustring title = null) : this (title, padding: 0)
		{
		}

		int padding;
		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> with
		/// the specified frame for its location, with the specified border
		/// an optional title.
		/// </summary>
		/// <param name="frame">Frame.</param>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="title">Title.</param>
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
		/// Initializes a new instance of the <see cref="Window"/> with
		/// the specified frame for its location, with the specified border
		/// an optional title.
		/// </summary>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="title">Title.</param>
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

		/// <summary>
		/// Add the specified view to the <see cref="ContentView"/>.
		/// </summary>
		/// <param name="view">View to add to the window.</param>
		public override void Add (View view)
		{
			contentView.Add (view);
			if (view.CanFocus)
				CanFocus = true;
		}


		/// <summary>
		///   Removes a widget from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
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

		/// <summary>
		///   Removes all widgets from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		///<inheritdoc cref="Redraw"/>
		public override void Redraw (Rect bounds)
		{
			Application.CurrentView = this;
			var scrRect = RectToScreen (new Rect (0, 0, Frame.Width, Frame.Height));
			var savedClip = Driver.Clip;
			Driver.Clip = ScreenClip (RectToScreen (Bounds));

			if (NeedDisplay != null && !NeedDisplay.IsEmpty) {
				Driver.SetAttribute (ColorScheme.Normal);
				Driver.DrawFrame (scrRect, padding, true);
			}
			contentView.Redraw (contentView.Bounds);
			ClearNeedsDisplay ();
			Driver.SetAttribute (ColorScheme.Normal);
			Driver.DrawFrame (scrRect, padding, false);

			if (HasFocus)
				Driver.SetAttribute (ColorScheme.HotNormal);
			Driver.DrawWindowTitle (scrRect, Title, padding, padding, padding, padding);
			Driver.Clip = savedClip;
			Driver.SetAttribute (ColorScheme.Normal);
		}

		//
		// FIXED:It does not look like the event is raised on clicked-drag
		// need to figure that out.
		//
		internal static Point? dragPosition;
		Point start;
		///<inheritdoc cref="MouseEvent(Gui.MouseEvent)"/>
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
