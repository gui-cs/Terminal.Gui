//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// NOTE: FrameView is functionally identical to Window with the following exceptions. 
//  - Is not a Toplevel
//  - Does not support mouse dragging
//  - Does not support padding (but should)
//  - Does not support IEnumerable
// Any udpates done here should probably be done in Window as well; TODO: Merge these classes

using System;
using System.Linq;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// The FrameView is a container frame that draws a frame around the contents. It is similar to
	/// a GroupBox in Windows.
	/// </summary>
	public class FrameView : View {
		View contentView;
		ustring title;

		/// <summary>
		/// The title to be displayed for this <see cref="FrameView"/>.
		/// </summary>
		/// <value>The title.</value>
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
			public ContentView (Rect frame) : base (frame) { }
			public ContentView () : base () { }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.FrameView"/> class using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <param name="frame">Frame.</param>
		/// <param name="title">Title.</param>
		/// <param name="views">Views.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		public FrameView (Rect frame, ustring title = null, View [] views = null, Border border = null) : base (frame)
		{
			//var cFrame = new Rect (1, 1, Math.Max (frame.Width - 2, 0), Math.Max (frame.Height - 2, 0));
			Initialize (frame, title, views, border);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.FrameView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="title">Title.</param>
		/// <param name="border">The <see cref="Border"/>.</param>
		public FrameView (ustring title, Border border = null)
		{
			Initialize (Rect.Empty, title, null, border);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Gui.FrameView"/> class using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public FrameView () : this (title: string.Empty) { }

		void Initialize (Rect frame, ustring title, View [] views = null, Border border = null)
		{
			this.title = title;
			if (border == null) {
				Border = new Border () {
					BorderStyle = BorderStyle.Single
				};
			} else {
				Border = border;
			}
			AdjustContentView (frame, views);
		}

		void AdjustContentView (Rect frame, View [] views = null)
		{
			var borderLength = Border.DrawMarginFrame ? 1 : 0;
			var sumPadding = Border.GetSumThickness ();
			var wb = new Size ();
			if (frame == Rect.Empty) {
				wb.Width = borderLength + sumPadding.Right;
				wb.Height = borderLength + sumPadding.Bottom;
				if (contentView == null) {
					contentView = new ContentView () {
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
					contentView = new ContentView (cFrame);
				} else {
					contentView.Frame = cFrame;
				}
			}
			if (views != null) {
				foreach (var view in views) {
					contentView.Add (view);
				}
			}
			if (Subviews?.Count == 0) {
				base.Add (contentView);
				contentView.Text = base.Text;
			}
			Border.Child = contentView;
		}

		void DrawFrame ()
		{
			DrawFrame (new Rect (0, 0, Frame.Width, Frame.Height), 0, fill: true);
		}

		/// <summary>
		/// Add the specified <see cref="View"/> to this container.
		/// </summary>
		/// <param name="view"><see cref="View"/> to add to this container</param>
		public override void Add (View view)
		{
			contentView.Add (view);
			if (view.CanFocus)
				CanFocus = true;
		}


		/// <summary>
		///   Removes a <see cref="View"/> from this container.
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
		///   Removes all <see cref="View"/>s from this container.
		/// </summary>
		/// <remarks>
		/// </remarks>
		public override void RemoveAll ()
		{
			contentView.RemoveAll ();
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			var padding = Border.GetSumThickness ();
			var scrRect = ViewToScreen (new Rect (0, 0, Frame.Width, Frame.Height));

			if (!NeedDisplay.IsEmpty) {
				Driver.SetAttribute (GetNormalColor ());
				//Driver.DrawWindowFrame (scrRect, padding + 1, padding + 1, padding + 1, padding + 1, border: true, fill: true);
				Border.DrawContent ();
			}

			var savedClip = contentView.ClipToBounds ();
			contentView.Redraw (contentView.Bounds);
			Driver.Clip = savedClip;

			ClearNeedsDisplay ();
			if (Border.BorderStyle != BorderStyle.None) {
				Driver.SetAttribute (GetNormalColor ());
				//Driver.DrawWindowFrame (scrRect, padding + 1, padding + 1, padding + 1, padding + 1, border: true, fill: false);
				if (HasFocus)
					Driver.SetAttribute (ColorScheme.HotNormal);
				//Driver.DrawWindowTitle (scrRect, Title, padding, padding, padding, padding);
				Driver.DrawWindowTitle (scrRect, Title, padding.Left, padding.Top, padding.Right, padding.Bottom);
			}
			Driver.SetAttribute (GetNormalColor ());
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

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			if (Subviews.Count == 0 || !Subviews.Any (subview => subview.CanFocus)) {
				Application.Driver?.SetCursorVisibility (CursorVisibility.Invisible);
			}

			return base.OnEnter (view);
		}

		/// <inheritdoc/>
		public override void OnCanFocusChanged ()
		{
			if (contentView != null) {
				contentView.CanFocus = CanFocus;
			}
			base.OnCanFocusChanged ();
		}
	}
}
