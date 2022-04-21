//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// NOTE: Window is functionally identical to FrameView with the following exceptions. 
//  - Window is a Toplevel
//  - FrameView Does not support padding (but should)
//  - FrameView Does not support mouse dragging
//  - FrameView Does not support IEnumerable
// Any udpates done here should probably be done in FrameView as well; TODO: Merge these classes

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
	public class Window : Toplevel {
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

		/// <inheritdoc/>
		public override Border Border {
			get => base.Border;
			set {
				if (base.Border != null && base.Border.Child != null && value.Child == null) {
					value.Child = base.Border.Child;
				}
				base.Border = value;
				if (value == null) {
					return;
				}
				Rect frame;
				if (contentView != null && (contentView.Width is Dim || contentView.Height is Dim)) {
					frame = Rect.Empty;
				} else {
					frame = Frame;
				}
				AdjustContentView (frame);

				Border.BorderChanged += Border_BorderChanged;
			}
		}

		void Border_BorderChanged (Border border)
		{
			Rect frame;
			if (contentView != null && (contentView.Width is Dim || contentView.Height is Dim)) {
				frame = Rect.Empty;
			} else {
				frame = Frame;
			}
			AdjustContentView (frame);
		}


		/// <summary>
		/// ContentView is an internal implementation detail of Window. It is used to host Views added with <see cref="Add(View)"/>. 
		/// Its ONLY reason for being is to provide a simple way for Window to expose to those SubViews that the Window's Bounds 
		/// are actually deflated due to the border. 
		/// </summary>
		class ContentView : View {
			Window instance;

			public ContentView (Rect frame, Window instance) : base (frame)
			{
				this.instance = instance;
			}
			public ContentView (Window instance) : base ()
			{
				this.instance = instance;
			}

			public override void OnCanFocusChanged ()
			{
				if (MostFocused == null && CanFocus && Visible) {
					EnsureFocus ();
				}

				base.OnCanFocusChanged ();
			}

			public override bool OnMouseEvent (MouseEvent mouseEvent)
			{
				return instance.OnMouseEvent (mouseEvent);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.Window"/> class with an optional title using <see cref="LayoutStyle.Absolute"/> positioning.
		/// </summary>
		/// <param name="frame">Superview-relative rectangle specifying the location and size</param>
		/// <param name="title">Title</param>
		/// <remarks>
		/// This constructor initializes a Window with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Absolute"/>. Use constructors
		/// that do not take <c>Rect</c> parameters to initialize a Window with <see cref="LayoutStyle.Computed"/>. 
		/// </remarks>
		public Window (Rect frame, ustring title = null) : this (frame, title, padding: 0, border: null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class with an optional title using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <remarks>
		///   This constructor initializes a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/>. 
		///   Use <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> properties to dynamically control the size and location of the view.
		/// </remarks>
		public Window (ustring title = null) : this (title, padding: 0, border: null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> class using <see cref="LayoutStyle.Computed"/> positioning.
		/// </summary>
		public Window () : this (title: null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> using <see cref="LayoutStyle.Absolute"/> positioning with the specified frame for its location, with the specified frame padding,
		/// and an optional title.
		/// </summary>
		/// <param name="frame">Superview-relative rectangle specifying the location and size</param>
		/// <param name="title">Title</param>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		/// <remarks>
		/// This constructor initializes a Window with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Absolute"/>. Use constructors
		/// that do not take <c>Rect</c> parameters to initialize a Window with  <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/> 
		/// </remarks>
		public Window (Rect frame, ustring title = null, int padding = 0, Border border = null) : base (frame)
		{
			Initialize (title, frame, padding, border);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Window"/> using <see cref="LayoutStyle.Computed"/> positioning,
		/// and an optional title.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <param name="padding">Number of characters to use for padding of the drawn frame.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		/// <remarks>
		///   This constructor initializes a View with a <see cref="LayoutStyle"/> of <see cref="LayoutStyle.Computed"/>. 
		///   Use <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> properties to dynamically control the size and location of the view.
		/// </remarks>
		public Window (ustring title = null, int padding = 0, Border border = null) : base ()
		{
			Initialize (title, Rect.Empty, padding, border);
		}

		void Initialize (ustring title, Rect frame, int padding = 0, Border border = null)
		{
			CanFocus = true;
			ColorScheme = Colors.Base;
			Title = title;
			if (border == null) {
				Border = new Border () {
					BorderStyle = BorderStyle.Single,
					Padding = new Thickness (padding),
					BorderBrush = ColorScheme.Normal.Background
				};
			} else {
				Border = border;
			}
		}

		void AdjustContentView (Rect frame)
		{
			var borderLength = Border.DrawMarginFrame ? 1 : 0;
			var sumPadding = Border.GetSumThickness ();
			var wb = new Size ();
			if (frame == Rect.Empty) {
				wb.Width = borderLength + sumPadding.Right;
				wb.Height = borderLength + sumPadding.Bottom;
				if (contentView == null) {
					contentView = new ContentView (this) {
						X = borderLength + sumPadding.Left,
						Y = borderLength + sumPadding.Top,
						Width = Dim.Fill (wb.Width),
						Height = Dim.Fill (wb.Height)
					};
				} else {
					contentView.X = borderLength + sumPadding.Left;
					contentView.Y = borderLength + sumPadding.Top;
					contentView.Width = Dim.Fill (wb.Width);
					contentView.Height = Dim.Fill (wb.Height);
				}
			} else {
				wb.Width = (2 * borderLength) + sumPadding.Right + sumPadding.Left;
				wb.Height = (2 * borderLength) + sumPadding.Bottom + sumPadding.Top;
				var cFrame = new Rect (borderLength + sumPadding.Left, borderLength + sumPadding.Top, frame.Width - wb.Width, frame.Height - wb.Height);
				if (contentView == null) {
					contentView = new ContentView (cFrame, this);
				} else {
					contentView.Frame = cFrame;
				}
			}
			base.Add (contentView);
			Border.Child = contentView;
		}

		///// <summary>
		///// Enumerates the various <see cref="View"/>s in the embedded <see cref="ContentView"/>.
		///// </summary>
		///// <returns>The enumerator.</returns>
		//public new IEnumerator GetEnumerator ()
		//{
		//	return contentView.GetEnumerator ();
		//}

		/// <inheritdoc/>
		public override void Add (View view)
		{
			contentView.Add (view);
			if (view.CanFocus) {
				CanFocus = true;
			}
			AddMenuStatusBar (view);
		}


		/// <inheritdoc/>
		public override void Remove (View view)
		{
			if (view == null) {
				return;
			}

			SetNeedsDisplay ();
			contentView.Remove (view);

			if (contentView.InternalSubviews.Count < 1) {
				CanFocus = false;
			}
			RemoveMenuStatusBar (view);
			if (view != contentView && Focused == null) {
				FocusFirst ();
			}
		}

		/// <inheritdoc/>
		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			var padding = Border.GetSumThickness ();
			var scrRect = ViewToScreen (new Rect (0, 0, Frame.Width, Frame.Height));
			//var borderLength = Border.DrawMarginFrame ? 1 : 0;

			// BUGBUG: Why do we draw the frame twice? This call is here to clear the content area, I think. Why not just clear that area?
			if (!NeedDisplay.IsEmpty) {
				Driver.SetAttribute (GetNormalColor ());
				//Driver.DrawWindowFrame (scrRect, padding.Left + borderLength, padding.Top + borderLength, padding.Right + borderLength, padding.Bottom + borderLength,
				//	Border.BorderStyle != BorderStyle.None, fill: true, Border);
				Border.DrawContent ();
			}
			var savedClip = contentView.ClipToBounds ();

			// Redraw our contentView
			// TODO: smartly constrict contentView.Bounds to just be what intersects with the 'bounds' we were passed
			contentView.Redraw (contentView.Bounds);
			Driver.Clip = savedClip;

			ClearLayoutNeeded ();
			ClearNeedsDisplay ();
			if (Border.BorderStyle != BorderStyle.None) {
				Driver.SetAttribute (GetNormalColor ());
				//Driver.DrawWindowFrame (scrRect, padding.Left + borderLength, padding.Top + borderLength, padding.Right + borderLength, padding.Bottom + borderLength,
				//	Border.BorderStyle != BorderStyle.None, fill: true, Border.BorderStyle);
				if (HasFocus)
					Driver.SetAttribute (ColorScheme.HotNormal);
				Driver.DrawWindowTitle (scrRect, Title, padding.Left, padding.Top, padding.Right, padding.Bottom);
			}
			Driver.SetAttribute (GetNormalColor ());

			// Checks if there are any SuperView view which intersect with this window.
			if (SuperView != null) {
				SuperView.SetNeedsLayout ();
				SuperView.SetNeedsDisplay ();
			}
		}

		/// <inheritdoc/>
		public override void OnCanFocusChanged ()
		{
			if (contentView != null) {
				contentView.CanFocus = CanFocus;
			}
			base.OnCanFocusChanged ();
		}

		/// <summary>
		///   The text displayed by the <see cref="Label"/>.
		/// </summary>
		public override ustring Text {
			get => contentView.Text;
			set {
				base.Text = value;
				if (contentView != null) {
					contentView.Text = value;
				}
			}
		}

		/// <summary>
		/// Controls the text-alignment property of the label, changing it will redisplay the <see cref="Label"/>.
		/// </summary>
		/// <value>The text alignment.</value>
		public override TextAlignment TextAlignment {
			get => contentView.TextAlignment;
			set {
				base.TextAlignment = contentView.TextAlignment = value;
			}
		}
	}
}
